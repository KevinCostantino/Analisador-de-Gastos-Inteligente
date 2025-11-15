using FinancasAPI.LocalTesting;
using FinancasAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped<LocalExpenseAnalyzer>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowAll");
app.UseStaticFiles();

// Routes
app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapPost("/api/analyze", async ([FromBody] ExpenseAnalysisRequest request, LocalExpenseAnalyzer analyzer) =>
{
    if (string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest(new { message = "Descrição é obrigatória" });
    }

    try
    {
        var result = await analyzer.AnalyzeExpenseAsync(request.Description);
        
        // Salvar no histórico
        await SaveToHistory(request.Description, result);
        
        var response = new ExpenseAnalysisResponse
        {
            Categoria = result.Category,
            Valor = result.Amount,
            Loja = result.Store,
            Tipo = result.Type,
            Confianca = result.Confidence,
            TransactionId = Guid.NewGuid().ToString()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao analisar gasto: {ex.Message}");
    }
});

app.MapGet("/api/history", async () =>
{
    try
    {
        var history = await LoadHistory();
        return Results.Ok(history);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao carregar histórico: {ex.Message}");
    }
});

app.MapGet("/api/report/{month?}", async (string? month) =>
{
    try
    {
        month ??= DateTime.Now.ToString("yyyy-MM");
        var history = await LoadHistory();
        var monthlyTransactions = history.Where(t => t.CreatedAt.ToString("yyyy-MM") == month).ToList();

        if (!monthlyTransactions.Any())
        {
            return Results.Ok(new
            {
                month,
                totalGastos = 0m,
                transacoes = 0,
                categorias = new Dictionary<string, object>(),
                lojas = new Dictionary<string, decimal>()
            });
        }

        var totalGastos = monthlyTransactions.Sum(t => t.Amount);
        var categorias = monthlyTransactions
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => new
            {
                total = g.Sum(t => t.Amount),
                count = g.Count(),
                percentage = Math.Round((double)(g.Sum(t => t.Amount) / totalGastos) * 100, 2)
            });

        var lojas = monthlyTransactions
            .GroupBy(t => t.Store)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return Results.Ok(new
        {
            month,
            totalGastos,
            transacoes = monthlyTransactions.Count,
            categorias,
            lojas
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao gerar relatório: {ex.Message}");
    }
});

app.MapDelete("/api/history", async () =>
{
    try
    {
        var historyFile = "history.json";
        if (File.Exists(historyFile))
        {
            File.Delete(historyFile);
        }
        return Results.Ok(new { message = "Histórico limpo com sucesso" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao limpar histórico: {ex.Message}");
    }
});

Console.WriteLine("🚀 Analisador de Gastos Web iniciando...");
Console.WriteLine("📱 Acesse: http://localhost:5000");
Console.WriteLine("💰 Interface web disponível!");

app.Run("http://localhost:5000");

// Helper methods
async Task SaveToHistory(string description, ExtractedExpenseInfo result)
{
    var historyFile = "history.json";
    var history = await LoadHistory();
    
    var transaction = new Transaction
    {
        Id = Guid.NewGuid().ToString(),
        UserId = "web-user",
        Description = description,
        Category = result.Category,
        Amount = result.Amount,
        Store = result.Store,
        Type = result.Type,
        Confidence = result.Confidence,
        CreatedAt = DateTime.Now
    };
    
    history.Add(transaction);
    
    var json = JsonConvert.SerializeObject(history, Formatting.Indented);
    await File.WriteAllTextAsync(historyFile, json);
}

async Task<List<Transaction>> LoadHistory()
{
    var historyFile = "history.json";
    if (!File.Exists(historyFile))
    {
        return new List<Transaction>();
    }
    
    var json = await File.ReadAllTextAsync(historyFile);
    return JsonConvert.DeserializeObject<List<Transaction>>(json) ?? new List<Transaction>();
}