using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using FinancasAPI.Models;
using FinancasAPI.Services;
using FinancasAPI.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FinancasAPI.Functions
{
    public class ExpenseAnalyzerFunction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpenseAnalyzerFunction> _logger;

        public ExpenseAnalyzerFunction()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<ExpenseAnalyzerFunction>>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando análise de gasto");

                // Verificar método HTTP
                if (request.HttpMethod != "POST")
                {
                    return CreateResponse(405, new { message = "Método não permitido" });
                }

                // Deserializar request
                if (string.IsNullOrEmpty(request.Body))
                {
                    return CreateResponse(400, new { message = "Body da requisição é obrigatório" });
                }

                var analysisRequest = JsonSerializer.Deserialize<ExpenseAnalysisRequest>(request.Body);
                
                if (analysisRequest == null || string.IsNullOrWhiteSpace(analysisRequest.Description))
                {
                    return CreateResponse(400, new { message = "Descrição é obrigatória" });
                }

                // Extrair userId dos headers ou query parameters
                var userId = analysisRequest.UserId ?? 
                            (request.Headers?.TryGetValue("X-User-Id", out var userIdHeader) == true ? userIdHeader : null) ?? 
                            (request.QueryStringParameters?.TryGetValue("userId", out var userIdQuery) == true ? userIdQuery : null) ?? 
                            "default-user";

                _logger.LogInformation("Analisando gasto para usuário: {UserId}", userId);

                // Executar análise usando AWS Comprehend
                var comprehendService = _serviceProvider.GetRequiredService<IComprehendService>();
                var expenseInfo = await comprehendService.AnalyzeExpenseAsync(analysisRequest.Description);

                // Criar transação
                var transaction = new Transaction
                {
                    UserId = userId,
                    Description = analysisRequest.Description,
                    Category = expenseInfo.Category,
                    Amount = expenseInfo.Amount,
                    Store = expenseInfo.Store,
                    Type = expenseInfo.Type,
                    Confidence = expenseInfo.Confidence,
                    RawAnalysis = expenseInfo.RawAnalysis
                };

                // Salvar no DynamoDB
                var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
                var savedTransaction = await repository.CreateTransactionAsync(transaction);

                // Criar response
                var response = new ExpenseAnalysisResponse
                {
                    Categoria = savedTransaction.Category,
                    Valor = savedTransaction.Amount,
                    Loja = savedTransaction.Store,
                    Tipo = savedTransaction.Type,
                    Confianca = savedTransaction.Confidence,
                    TransactionId = savedTransaction.Id
                };

                _logger.LogInformation("Análise concluída com sucesso. TransactionId: {TransactionId}", savedTransaction.Id);

                return CreateResponse(200, response);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar JSON");
                return CreateResponse(400, new { message = "Formato JSON inválido", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao analisar gasto");
                return CreateResponse(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // AWS Services
            services.AddAWSService<Amazon.Comprehend.IAmazonComprehend>();
            services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();

            // Application Services
            services.AddScoped<IComprehendService, ComprehendService>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
        }

        private APIGatewayProxyResponse CreateResponse(int statusCode, object body)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = JsonSerializer.Serialize(body),
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,X-User-Id",
                    ["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
                }
            };
        }
    }
}