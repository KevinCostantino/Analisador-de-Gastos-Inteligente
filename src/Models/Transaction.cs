using System.Text.Json.Serialization;

namespace FinancasAPI.Models
{
    public class Transaction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("store")]
        public string Store { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("month")]
        public string Month { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("rawAnalysis")]
        public object? RawAnalysis { get; set; }
    }
}