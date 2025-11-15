using FinancasAPI.Services;
using FinancasAPI.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Xunit;

namespace FinancasAPI.Tests.Services
{
    public class ComprehendServiceTests
    {
        private readonly Mock<IAmazonComprehend> _mockComprehendClient;
        private readonly Mock<ILogger<ComprehendService>> _mockLogger;
        private readonly ComprehendService _service;

        public ComprehendServiceTests()
        {
            _mockComprehendClient = new Mock<IAmazonComprehend>();
            _mockLogger = new Mock<ILogger<ComprehendService>>();
            _service = new ComprehendService(_mockComprehendClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeExpenseAsync_ShouldReturnValidResult_WhenDescriptionIsValid()
        {
            // Arrange
            var description = "Paguei 37 reais no Uber para o trabalho";
            
            SetupMockComprehendResponses();

            // Act
            var result = await _service.AnalyzeExpenseAsync(description);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Amount > 0);
            Assert.NotEmpty(result.Category);
            Assert.NotEmpty(result.Store);
            Assert.NotEmpty(result.Type);
            Assert.True(result.Confidence > 0);
        }

        [Theory]
        [InlineData("Paguei 50 reais no iFood", "Alimentação")]
        [InlineData("Uber 25 reais", "Transporte")]
        [InlineData("Compras no supermercado 80 reais", "Supermercado")]
        [InlineData("Farmácia 15 reais", "Saúde")]
        public async Task AnalyzeExpenseAsync_ShouldCategorizeProperly_BasedOnKeywords(
            string description, 
            string expectedCategory)
        {
            // Arrange
            SetupMockComprehendResponses();

            // Act
            var result = await _service.AnalyzeExpenseAsync(description);

            // Assert
            Assert.Equal(expectedCategory, result.Category);
        }

        [Fact]
        public async Task AnalyzeExpenseAsync_ShouldExtractAmountCorrectly_FromDifferentFormats()
        {
            // Arrange
            var testCases = new[]
            {
                ("Paguei 37 reais", 37m),
                ("Gastei R$ 125,50", 125.50m),
                ("150 no mercado", 150m),
                ("Conta de 89,99 reais", 89.99m)
            };

            SetupMockComprehendResponses();

            foreach (var (description, expectedAmount) in testCases)
            {
                // Act
                var result = await _service.AnalyzeExpenseAsync(description);

                // Assert
                Assert.Equal(expectedAmount, result.Amount);
            }
        }

        [Fact]
        public async Task AnalyzeExpenseAsync_ShouldHandleComprehendFailure_WithFallbackAnalysis()
        {
            // Arrange
            var description = "Paguei 37 reais no Uber";
            
            _mockComprehendClient
                .Setup(x => x.DetectEntitiesAsync(It.IsAny<DetectEntitiesRequest>(), default))
                .ThrowsAsync(new AmazonComprehendException("Service unavailable"));

            // Act
            var result = await _service.AnalyzeExpenseAsync(description);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(37m, result.Amount);
            Assert.Equal(0.5, result.Confidence); // Fallback confidence
        }

        private void SetupMockComprehendResponses()
        {
            // Mock DetectEntities
            _mockComprehendClient
                .Setup(x => x.DetectEntitiesAsync(It.IsAny<DetectEntitiesRequest>(), default))
                .ReturnsAsync(new DetectEntitiesResponse
                {
                    Entities = new List<Entity>
                    {
                        new Entity
                        {
                            Text = "37",
                            Type = EntityType.QUANTITY,
                            Score = 0.9f,
                            BeginOffset = 0,
                            EndOffset = 2
                        },
                        new Entity
                        {
                            Text = "Uber",
                            Type = EntityType.ORGANIZATION,
                            Score = 0.95f,
                            BeginOffset = 10,
                            EndOffset = 14
                        }
                    }
                });

            // Mock DetectKeyPhrases
            _mockComprehendClient
                .Setup(x => x.DetectKeyPhrasesAsync(It.IsAny<DetectKeyPhrasesRequest>(), default))
                .ReturnsAsync(new DetectKeyPhrasesResponse
                {
                    KeyPhrases = new List<KeyPhrase>
                    {
                        new KeyPhrase
                        {
                            Text = "Uber",
                            Score = 0.9f,
                            BeginOffset = 10,
                            EndOffset = 14
                        }
                    }
                });

            // Mock DetectSentiment
            _mockComprehendClient
                .Setup(x => x.DetectSentimentAsync(It.IsAny<DetectSentimentRequest>(), default))
                .ReturnsAsync(new DetectSentimentResponse
                {
                    Sentiment = SentimentType.NEUTRAL,
                    SentimentScore = new SentimentScore
                    {
                        Positive = 0.1f,
                        Negative = 0.1f,
                        Neutral = 0.8f,
                        Mixed = 0.0f
                    }
                });
        }
    }
}