using System.Text.Json.Serialization;

namespace FinancasAPI.Models
{
    public class ComprehendAnalysisResult
    {
        [JsonPropertyName("entities")]
        public List<DetectedEntity> Entities { get; set; } = new();

        [JsonPropertyName("keyPhrases")]
        public List<KeyPhrase> KeyPhrases { get; set; } = new();

        [JsonPropertyName("sentiment")]
        public SentimentAnalysis? Sentiment { get; set; }
    }

    public class DetectedEntity
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("beginOffset")]
        public int BeginOffset { get; set; }

        [JsonPropertyName("endOffset")]
        public int EndOffset { get; set; }
    }

    public class KeyPhrase
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("beginOffset")]
        public int BeginOffset { get; set; }

        [JsonPropertyName("endOffset")]
        public int EndOffset { get; set; }
    }

    public class SentimentAnalysis
    {
        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; } = string.Empty;

        [JsonPropertyName("sentimentScore")]
        public SentimentScore? SentimentScore { get; set; }
    }

    public class SentimentScore
    {
        [JsonPropertyName("positive")]
        public double Positive { get; set; }

        [JsonPropertyName("negative")]
        public double Negative { get; set; }

        [JsonPropertyName("neutral")]
        public double Neutral { get; set; }

        [JsonPropertyName("mixed")]
        public double Mixed { get; set; }
    }

    public class ExtractedExpenseInfo
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Store { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public ComprehendAnalysisResult? RawAnalysis { get; set; }
    }
}