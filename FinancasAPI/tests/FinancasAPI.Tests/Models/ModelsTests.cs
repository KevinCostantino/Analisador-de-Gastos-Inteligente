using FinancasAPI.Models;
using Xunit;

namespace FinancasAPI.Tests.Models
{
    public class TransactionTests
    {
        [Fact]
        public void Transaction_ShouldInitializeWithDefaultValues()
        {
            // Act
            var transaction = new Transaction();

            // Assert
            Assert.NotNull(transaction.Id);
            Assert.NotEmpty(transaction.Id);
            Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), transaction.Month);
            Assert.True((DateTime.UtcNow - transaction.CreatedAt).TotalSeconds < 5); // Created within last 5 seconds
        }

        [Fact]
        public void Transaction_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var expectedId = Guid.NewGuid().ToString();
            var expectedUserId = "user123";
            var expectedDescription = "Test transaction";
            var expectedCategory = "Test Category";
            var expectedAmount = 123.45m;
            var expectedStore = "Test Store";
            var expectedType = "Test Type";
            var expectedCreatedAt = DateTime.UtcNow.AddDays(-1);
            var expectedMonth = "2023-11";
            var expectedConfidence = 0.85;

            // Act
            var transaction = new Transaction
            {
                Id = expectedId,
                UserId = expectedUserId,
                Description = expectedDescription,
                Category = expectedCategory,
                Amount = expectedAmount,
                Store = expectedStore,
                Type = expectedType,
                CreatedAt = expectedCreatedAt,
                Month = expectedMonth,
                Confidence = expectedConfidence
            };

            // Assert
            Assert.Equal(expectedId, transaction.Id);
            Assert.Equal(expectedUserId, transaction.UserId);
            Assert.Equal(expectedDescription, transaction.Description);
            Assert.Equal(expectedCategory, transaction.Category);
            Assert.Equal(expectedAmount, transaction.Amount);
            Assert.Equal(expectedStore, transaction.Store);
            Assert.Equal(expectedType, transaction.Type);
            Assert.Equal(expectedCreatedAt, transaction.CreatedAt);
            Assert.Equal(expectedMonth, transaction.Month);
            Assert.Equal(expectedConfidence, transaction.Confidence);
        }

        [Theory]
        [InlineData("2023-01-15", "2023-01")]
        [InlineData("2023-12-31", "2023-12")]
        [InlineData("2024-06-15", "2024-06")]
        public void Transaction_MonthProperty_ShouldFormatCorrectly(string dateString, string expectedMonth)
        {
            // Arrange
            var date = DateTime.Parse(dateString);

            // Act
            var transaction = new Transaction
            {
                CreatedAt = date,
                Month = date.ToString("yyyy-MM")
            };

            // Assert
            Assert.Equal(expectedMonth, transaction.Month);
        }
    }

    public class ExpenseAnalysisRequestTests
    {
        [Fact]
        public void ExpenseAnalysisRequest_ShouldInitializeWithDefaultValues()
        {
            // Act
            var request = new ExpenseAnalysisRequest();

            // Assert
            Assert.Equal(string.Empty, request.Description);
            Assert.Null(request.UserId);
        }

        [Fact]
        public void ExpenseAnalysisRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var expectedDescription = "Test description";
            var expectedUserId = "user123";

            // Act
            var request = new ExpenseAnalysisRequest
            {
                Description = expectedDescription,
                UserId = expectedUserId
            };

            // Assert
            Assert.Equal(expectedDescription, request.Description);
            Assert.Equal(expectedUserId, request.UserId);
        }
    }

    public class ExpenseAnalysisResponseTests
    {
        [Fact]
        public void ExpenseAnalysisResponse_ShouldInitializeWithDefaultValues()
        {
            // Act
            var response = new ExpenseAnalysisResponse();

            // Assert
            Assert.Equal(string.Empty, response.Categoria);
            Assert.Equal(0, response.Valor);
            Assert.Equal(string.Empty, response.Loja);
            Assert.Equal(string.Empty, response.Tipo);
            Assert.Null(response.Confianca);
            Assert.Null(response.TransactionId);
        }

        [Fact]
        public void ExpenseAnalysisResponse_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var expectedCategoria = "Transporte";
            var expectedValor = 37m;
            var expectedLoja = "Uber";
            var expectedTipo = "Serviço";
            var expectedConfianca = 0.85;
            var expectedTransactionId = Guid.NewGuid().ToString();

            // Act
            var response = new ExpenseAnalysisResponse
            {
                Categoria = expectedCategoria,
                Valor = expectedValor,
                Loja = expectedLoja,
                Tipo = expectedTipo,
                Confianca = expectedConfianca,
                TransactionId = expectedTransactionId
            };

            // Assert
            Assert.Equal(expectedCategoria, response.Categoria);
            Assert.Equal(expectedValor, response.Valor);
            Assert.Equal(expectedLoja, response.Loja);
            Assert.Equal(expectedTipo, response.Tipo);
            Assert.Equal(expectedConfianca, response.Confianca);
            Assert.Equal(expectedTransactionId, response.TransactionId);
        }
    }
}