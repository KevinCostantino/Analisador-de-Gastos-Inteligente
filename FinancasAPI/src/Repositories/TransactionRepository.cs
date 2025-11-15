using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using FinancasAPI.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinancasAPI.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<Transaction?> GetTransactionAsync(string id);
        Task<List<Transaction>> GetTransactionsByUserAsync(string userId, int limit = 50);
        Task<List<Transaction>> GetTransactionsByUserAndMonthAsync(string userId, string month);
        Task<bool> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(string id);
    }

    public class TransactionRepository : ITransactionRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly ILogger<TransactionRepository> _logger;
        private readonly string _tableName;

        public TransactionRepository(IAmazonDynamoDB dynamoDbClient, ILogger<TransactionRepository> logger)
        {
            _dynamoDbClient = dynamoDbClient;
            _logger = logger;
            _tableName = Environment.GetEnvironmentVariable("TRANSACTIONS_TABLE") ?? "FinancasAPI-Transactions";
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            try
            {
                _logger.LogInformation("Criando transação para usuário: {UserId}", transaction.UserId);

                var item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { S = transaction.Id },
                    ["UserId"] = new AttributeValue { S = transaction.UserId },
                    ["Description"] = new AttributeValue { S = transaction.Description },
                    ["Category"] = new AttributeValue { S = transaction.Category },
                    ["Amount"] = new AttributeValue { N = transaction.Amount.ToString() },
                    ["Store"] = new AttributeValue { S = transaction.Store },
                    ["Type"] = new AttributeValue { S = transaction.Type },
                    ["CreatedAt"] = new AttributeValue { S = transaction.CreatedAt.ToString("O") },
                    ["Month"] = new AttributeValue { S = transaction.Month }
                };

                if (transaction.Confidence.HasValue)
                {
                    item["Confidence"] = new AttributeValue { N = transaction.Confidence.Value.ToString() };
                }

                if (transaction.RawAnalysis != null)
                {
                    item["RawAnalysis"] = new AttributeValue { S = JsonSerializer.Serialize(transaction.RawAnalysis) };
                }

                var request = new PutItemRequest
                {
                    TableName = _tableName,
                    Item = item
                };

                await _dynamoDbClient.PutItemAsync(request);

                _logger.LogInformation("Transação criada com sucesso: {TransactionId}", transaction.Id);
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar transação: {TransactionId}", transaction.Id);
                throw;
            }
        }

        public async Task<Transaction?> GetTransactionAsync(string id)
        {
            try
            {
                _logger.LogInformation("Buscando transação: {TransactionId}", id);

                var request = new GetItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["Id"] = new AttributeValue { S = id }
                    }
                };

                var response = await _dynamoDbClient.GetItemAsync(request);

                if (!response.IsItemSet)
                {
                    _logger.LogWarning("Transação não encontrada: {TransactionId}", id);
                    return null;
                }

                return MapItemToTransaction(response.Item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar transação: {TransactionId}", id);
                throw;
            }
        }

        public async Task<List<Transaction>> GetTransactionsByUserAsync(string userId, int limit = 50)
        {
            try
            {
                _logger.LogInformation("Buscando transações do usuário: {UserId}", userId);

                var request = new QueryRequest
                {
                    TableName = _tableName,
                    IndexName = "UserId-CreatedAt-Index", // GSI necessário
                    KeyConditionExpression = "UserId = :userId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":userId"] = new AttributeValue { S = userId }
                    },
                    ScanIndexForward = false, // Ordem decrescente por data
                    Limit = limit
                };

                var response = await _dynamoDbClient.QueryAsync(request);
                var transactions = response.Items.Select(MapItemToTransaction).ToList();

                _logger.LogInformation("Encontradas {Count} transações para o usuário {UserId}", transactions.Count, userId);
                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar transações do usuário: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Transaction>> GetTransactionsByUserAndMonthAsync(string userId, string month)
        {
            try
            {
                _logger.LogInformation("Buscando transações do usuário {UserId} para o mês {Month}", userId, month);

                var request = new QueryRequest
                {
                    TableName = _tableName,
                    IndexName = "UserId-Month-Index", // GSI necessário
                    KeyConditionExpression = "UserId = :userId AND #month = :month",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#month"] = "Month"
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":userId"] = new AttributeValue { S = userId },
                        [":month"] = new AttributeValue { S = month }
                    }
                };

                var response = await _dynamoDbClient.QueryAsync(request);
                var transactions = response.Items.Select(MapItemToTransaction).ToList();

                _logger.LogInformation("Encontradas {Count} transações para {UserId} no mês {Month}", transactions.Count, userId, month);
                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar transações do usuário {UserId} no mês {Month}", userId, month);
                throw;
            }
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            try
            {
                _logger.LogInformation("Atualizando transação: {TransactionId}", transaction.Id);

                var request = new UpdateItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["Id"] = new AttributeValue { S = transaction.Id }
                    },
                    UpdateExpression = "SET Description = :description, Category = :category, Amount = :amount, Store = :store, #type = :type, Confidence = :confidence",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#type"] = "Type"
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":description"] = new AttributeValue { S = transaction.Description },
                        [":category"] = new AttributeValue { S = transaction.Category },
                        [":amount"] = new AttributeValue { N = transaction.Amount.ToString() },
                        [":store"] = new AttributeValue { S = transaction.Store },
                        [":type"] = new AttributeValue { S = transaction.Type },
                        [":confidence"] = new AttributeValue { N = (transaction.Confidence ?? 0).ToString() }
                    }
                };

                await _dynamoDbClient.UpdateItemAsync(request);

                _logger.LogInformation("Transação atualizada com sucesso: {TransactionId}", transaction.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar transação: {TransactionId}", transaction.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTransactionAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deletando transação: {TransactionId}", id);

                var request = new DeleteItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["Id"] = new AttributeValue { S = id }
                    }
                };

                await _dynamoDbClient.DeleteItemAsync(request);

                _logger.LogInformation("Transação deletada com sucesso: {TransactionId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar transação: {TransactionId}", id);
                return false;
            }
        }

        private Transaction MapItemToTransaction(Dictionary<string, AttributeValue> item)
        {
            var transaction = new Transaction
            {
                Id = item["Id"].S,
                UserId = item["UserId"].S,
                Description = item["Description"].S,
                Category = item["Category"].S,
                Amount = decimal.Parse(item["Amount"].N),
                Store = item["Store"].S,
                Type = item["Type"].S,
                CreatedAt = DateTime.Parse(item["CreatedAt"].S),
                Month = item["Month"].S
            };

            if (item.ContainsKey("Confidence") && !string.IsNullOrEmpty(item["Confidence"].N))
            {
                transaction.Confidence = double.Parse(item["Confidence"].N);
            }

            if (item.ContainsKey("RawAnalysis") && !string.IsNullOrEmpty(item["RawAnalysis"].S))
            {
                try
                {
                    transaction.RawAnalysis = JsonSerializer.Deserialize<object>(item["RawAnalysis"].S);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao deserializar RawAnalysis da transação {TransactionId}", transaction.Id);
                }
            }

            return transaction;
        }
    }
}