using System.Text.Json.Serialization;

namespace FinancasAPI.Models
{
    public class ExpenseAnalysisRequest
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
    }

    public class ExpenseAnalysisResponse
    {
        [JsonPropertyName("categoria")]
        public string Categoria { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }

        [JsonPropertyName("loja")]
        public string Loja { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("confianca")]
        public double? Confianca { get; set; }

        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }
    }

    public class MonthlyReportRequest
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("month")]
        public string Month { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");
    }

    public class MonthlyReportResponse
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("month")]
        public string Month { get; set; } = string.Empty;

        [JsonPropertyName("totalGastos")]
        public decimal TotalGastos { get; set; }

        [JsonPropertyName("transacoes")]
        public int Transacoes { get; set; }

        [JsonPropertyName("categorias")]
        public Dictionary<string, CategorySummary> Categorias { get; set; } = new();

        [JsonPropertyName("lojas")]
        public Dictionary<string, decimal> Lojas { get; set; } = new();
    }

    public class CategorySummary
    {
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }
    }
}