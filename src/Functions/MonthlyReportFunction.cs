using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using FinancasAPI.Models;
using FinancasAPI.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinancasAPI.Functions
{
    public class MonthlyReportFunction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyReportFunction> _logger;

        public MonthlyReportFunction()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<MonthlyReportFunction>>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando geração de relatório mensal");

                // Verificar método HTTP
                if (request.HttpMethod != "GET" && request.HttpMethod != "POST")
                {
                    return CreateResponse(405, new { message = "Método não permitido. Use GET ou POST." });
                }

                string userId;
                string month;

                // Extrair parâmetros baseado no método HTTP
                if (request.HttpMethod == "GET")
                {
                    // Parâmetros via query string
                    if (request.QueryStringParameters == null ||
                        !request.QueryStringParameters.TryGetValue("userId", out userId!) ||
                        string.IsNullOrWhiteSpace(userId))
                    {
                        return CreateResponse(400, new { message = "Parâmetro 'userId' é obrigatório" });
                    }

                    // Month é opcional, default para mês atual
                    request.QueryStringParameters.TryGetValue("month", out month!);
                    month = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
                }
                else
                {
                    // Parâmetros via body JSON
                    if (string.IsNullOrEmpty(request.Body))
                    {
                        return CreateResponse(400, new { message = "Body da requisição é obrigatório para POST" });
                    }

                    var reportRequest = JsonSerializer.Deserialize<MonthlyReportRequest>(request.Body);
                    
                    if (reportRequest == null || string.IsNullOrWhiteSpace(reportRequest.UserId))
                    {
                        return CreateResponse(400, new { message = "UserId é obrigatório" });
                    }

                    userId = reportRequest.UserId;
                    month = string.IsNullOrWhiteSpace(reportRequest.Month) ? 
                           DateTime.UtcNow.ToString("yyyy-MM") : 
                           reportRequest.Month;
                }

                _logger.LogInformation("Gerando relatório para usuário: {UserId}, mês: {Month}", userId, month);

                // Buscar transações do mês
                var repository = _serviceProvider.GetRequiredService<ITransactionRepository>();
                var transactions = await repository.GetTransactionsByUserAndMonthAsync(userId, month);

                if (!transactions.Any())
                {
                    _logger.LogInformation("Nenhuma transação encontrada para {UserId} no mês {Month}", userId, month);
                    
                    var emptyReport = new MonthlyReportResponse
                    {
                        UserId = userId,
                        Month = month,
                        TotalGastos = 0,
                        Transacoes = 0,
                        Categorias = new Dictionary<string, CategorySummary>(),
                        Lojas = new Dictionary<string, decimal>()
                    };

                    return CreateResponse(200, emptyReport);
                }

                // Calcular estatísticas
                var totalGastos = transactions.Sum(t => t.Amount);
                var totalTransacoes = transactions.Count;

                // Agrupar por categoria
                var categorias = transactions
                    .GroupBy(t => t.Category)
                    .ToDictionary(g => g.Key, g => new CategorySummary
                    {
                        Total = g.Sum(t => t.Amount),
                        Count = g.Count(),
                        Percentage = Math.Round((double)(g.Sum(t => t.Amount) / totalGastos) * 100, 2)
                    });

                // Agrupar por loja
                var lojas = transactions
                    .GroupBy(t => t.Store)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                // Criar response
                var response = new MonthlyReportResponse
                {
                    UserId = userId,
                    Month = month,
                    TotalGastos = totalGastos,
                    Transacoes = totalTransacoes,
                    Categorias = categorias,
                    Lojas = lojas
                };

                _logger.LogInformation(
                    "Relatório gerado: {Transacoes} transações, total: R$ {Total}", 
                    totalTransacoes, 
                    totalGastos
                );

                return CreateResponse(200, response);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar JSON");
                return CreateResponse(400, new { message = "Formato JSON inválido", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao gerar relatório");
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
            services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();

            // Application Services
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
                    ["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token",
                    ["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
                }
            };
        }
    }
}