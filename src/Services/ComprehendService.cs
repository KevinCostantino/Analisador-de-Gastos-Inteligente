using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using FinancasAPI.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinancasAPI.Services
{
    public interface IComprehendService
    {
        Task<ExtractedExpenseInfo> AnalyzeExpenseAsync(string description);
    }

    public class ComprehendService : IComprehendService
    {
        private readonly IAmazonComprehend _comprehendClient;
        private readonly ILogger<ComprehendService> _logger;
        private readonly Dictionary<string, string> _categoryKeywords;
        private readonly Dictionary<string, string> _typeKeywords;

        public ComprehendService(IAmazonComprehend comprehendClient, ILogger<ComprehendService> logger)
        {
            _comprehendClient = comprehendClient;
            _logger = logger;
            _categoryKeywords = InitializeCategoryKeywords();
            _typeKeywords = InitializeTypeKeywords();
        }

        public async Task<ExtractedExpenseInfo> AnalyzeExpenseAsync(string description)
        {
            try
            {
                _logger.LogInformation("Analisando descrição: {Description}", description);

                // Análise paralela com AWS Comprehend
                var entitiesTask = DetectEntitiesAsync(description);
                var keyPhrasesTask = DetectKeyPhrasesAsync(description);
                var sentimentTask = DetectSentimentAsync(description);

                await Task.WhenAll(entitiesTask, keyPhrasesTask, sentimentTask);

                var entities = entitiesTask.Result;
                var keyPhrases = keyPhrasesTask.Result;
                var sentiment = sentimentTask.Result;

                // Construir resultado da análise
                var analysisResult = new ComprehendAnalysisResult
                {
                    Entities = entities,
                    KeyPhrases = keyPhrases,
                    Sentiment = sentiment
                };

                // Extrair informações do gasto
                var expenseInfo = ExtractExpenseInformation(description, analysisResult);

                _logger.LogInformation("Análise concluída com sucesso");
                return expenseInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao analisar descrição: {Description}", description);
                
                // Fallback para análise básica por regex
                return PerformBasicAnalysis(description);
            }
        }

        private async Task<List<DetectedEntity>> DetectEntitiesAsync(string text)
        {
            try
            {
                var request = new DetectEntitiesRequest
                {
                    Text = text,
                    LanguageCode = "pt"
                };

                var response = await _comprehendClient.DetectEntitiesAsync(request);
                
                return response.Entities.Select(e => new DetectedEntity
                {
                    Text = e.Text,
                    Type = e.Type.Value,
                    Score = e.Score,
                    BeginOffset = e.BeginOffset,
                    EndOffset = e.EndOffset
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao detectar entidades");
                return new List<DetectedEntity>();
            }
        }

        private async Task<List<KeyPhrase>> DetectKeyPhrasesAsync(string text)
        {
            try
            {
                var request = new DetectKeyPhrasesRequest
                {
                    Text = text,
                    LanguageCode = "pt"
                };

                var response = await _comprehendClient.DetectKeyPhrasesAsync(request);
                
                return response.KeyPhrases.Select(kp => new KeyPhrase
                {
                    Text = kp.Text,
                    Score = kp.Score,
                    BeginOffset = kp.BeginOffset,
                    EndOffset = kp.EndOffset
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao detectar frases-chave");
                return new List<KeyPhrase>();
            }
        }

        private async Task<SentimentAnalysis?> DetectSentimentAsync(string text)
        {
            try
            {
                var request = new DetectSentimentRequest
                {
                    Text = text,
                    LanguageCode = "pt"
                };

                var response = await _comprehendClient.DetectSentimentAsync(request);
                
                return new SentimentAnalysis
                {
                    Sentiment = response.Sentiment.Value,
                    SentimentScore = new SentimentScore
                    {
                        Positive = response.SentimentScore.Positive,
                        Negative = response.SentimentScore.Negative,
                        Neutral = response.SentimentScore.Neutral,
                        Mixed = response.SentimentScore.Mixed
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao detectar sentimento");
                return null;
            }
        }

        private ExtractedExpenseInfo ExtractExpenseInformation(string description, ComprehendAnalysisResult analysis)
        {
            var amount = ExtractAmount(description, analysis);
            var store = ExtractStore(description, analysis);
            var category = ExtractCategory(description, analysis, store);
            var type = ExtractType(description, analysis, category, store);
            var confidence = CalculateConfidence(analysis, amount > 0, !string.IsNullOrEmpty(store));

            return new ExtractedExpenseInfo
            {
                Amount = amount,
                Store = store,
                Category = category,
                Type = type,
                Confidence = confidence,
                RawAnalysis = analysis
            };
        }

        private decimal ExtractAmount(string description, ComprehendAnalysisResult analysis)
        {
            // Procurar por entidades do tipo QUANTITY ou números no texto
            var quantityEntities = analysis.Entities
                .Where(e => e.Type == "QUANTITY" || e.Type == "OTHER")
                .ToList();

            foreach (var entity in quantityEntities)
            {
                if (TryParseAmount(entity.Text, out decimal amount))
                    return amount;
            }

            // Fallback: usar regex para encontrar valores
            var amountRegex = new Regex(@"(\d+(?:,\d{2})?)\s*(?:reais?|R\$|$)", RegexOptions.IgnoreCase);
            var match = amountRegex.Match(description);
            
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", "."), out decimal regexAmount))
            {
                return regexAmount;
            }

            // Procurar números isolados
            var numberRegex = new Regex(@"\b(\d+(?:,\d{2})?)\b");
            var numberMatch = numberRegex.Match(description);
            
            if (numberMatch.Success && decimal.TryParse(numberMatch.Groups[1].Value.Replace(",", "."), out decimal numberAmount))
            {
                return numberAmount;
            }

            return 0;
        }

        private string ExtractStore(string description, ComprehendAnalysisResult analysis)
        {
            // Procurar por entidades de organizações
            var orgEntities = analysis.Entities
                .Where(e => e.Type == "ORGANIZATION" || e.Type == "COMMERCIAL_ITEM")
                .OrderByDescending(e => e.Score)
                .ToList();

            if (orgEntities.Any())
                return orgEntities.First().Text;

            // Procurar por marcas conhecidas nas frases-chave
            var knownBrands = new[] { "uber", "ifood", "mcdonalds", "subway", "amazon", "mercado livre", "shopee", "magalu", "carrefour", "extra", "pao de acucar" };
            
            foreach (var phrase in analysis.KeyPhrases.OrderByDescending(kp => kp.Score))
            {
                foreach (var brand in knownBrands)
                {
                    if (phrase.Text.ToLower().Contains(brand))
                        return brand.ToTitleCase();
                }
            }

            return "Não identificado";
        }

        private string ExtractCategory(string description, ComprehendAnalysisResult analysis, string store)
        {
            var text = description.ToLower();
            
            // Verificar palavras-chave diretas
            foreach (var kvp in _categoryKeywords)
            {
                if (kvp.Value.Split(',').Any(keyword => text.Contains(keyword.Trim())))
                {
                    return kvp.Key;
                }
            }

            // Baseado na loja
            var storeCategories = new Dictionary<string, string>
            {
                { "uber", "Transporte" },
                { "ifood", "Alimentação" },
                { "mcdonalds", "Alimentação" },
                { "subway", "Alimentação" },
                { "amazon", "Compras Online" },
                { "mercado", "Supermercado" },
                { "farmacia", "Saúde" },
                { "posto", "Combustível" }
            };

            foreach (var kvp in storeCategories)
            {
                if (store.ToLower().Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Outros";
        }

        private string ExtractType(string description, ComprehendAnalysisResult analysis, string category, string store)
        {
            var text = description.ToLower();

            // Verificar palavras-chave diretas
            foreach (var kvp in _typeKeywords)
            {
                if (kvp.Value.Split(',').Any(keyword => text.Contains(keyword.Trim())))
                {
                    return kvp.Key;
                }
            }

            // Baseado na categoria
            var categoryTypes = new Dictionary<string, string>
            {
                { "Transporte", "Serviço" },
                { "Alimentação", "Produto" },
                { "Saúde", "Serviço" },
                { "Educação", "Serviço" },
                { "Entretenimento", "Serviço" },
                { "Compras Online", "Produto" },
                { "Supermercado", "Produto" }
            };

            return categoryTypes.GetValueOrDefault(category, "Produto");
        }

        private double CalculateConfidence(ComprehendAnalysisResult analysis, bool hasAmount, bool hasStore)
        {
            double confidence = 0.3; // Base confidence

            // Adicionar confiança baseada nas entidades detectadas
            if (analysis.Entities.Any())
                confidence += 0.2;

            // Adicionar confiança se valor foi detectado
            if (hasAmount)
                confidence += 0.3;

            // Adicionar confiança se loja foi detectada
            if (hasStore)
                confidence += 0.2;

            // Confidence das entidades
            var avgEntityScore = analysis.Entities.Any() ? analysis.Entities.Average(e => e.Score) : 0;
            confidence += avgEntityScore * 0.1;

            return Math.Min(confidence, 1.0);
        }

        private ExtractedExpenseInfo PerformBasicAnalysis(string description)
        {
            _logger.LogInformation("Realizando análise básica por fallback");

            var amount = ExtractAmountBasic(description);
            var category = ExtractCategoryBasic(description);
            var store = ExtractStoreBasic(description);
            var type = ExtractTypeBasic(description, category);

            return new ExtractedExpenseInfo
            {
                Amount = amount,
                Store = store,
                Category = category,
                Type = type,
                Confidence = 0.5 // Confidence menor para análise básica
            };
        }

        private decimal ExtractAmountBasic(string description)
        {
            var amountRegex = new Regex(@"(\d+(?:,\d{2})?)\s*(?:reais?|R\$|$)", RegexOptions.IgnoreCase);
            var match = amountRegex.Match(description);
            
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", "."), out decimal amount))
            {
                return amount;
            }

            var numberRegex = new Regex(@"\b(\d+)\b");
            var numberMatch = numberRegex.Match(description);
            
            if (numberMatch.Success && decimal.TryParse(numberMatch.Groups[1].Value, out decimal numberAmount))
            {
                return numberAmount;
            }

            return 0;
        }

        private string ExtractCategoryBasic(string description)
        {
            var text = description.ToLower();
            
            foreach (var kvp in _categoryKeywords)
            {
                if (kvp.Value.Split(',').Any(keyword => text.Contains(keyword.Trim())))
                {
                    return kvp.Key;
                }
            }

            return "Outros";
        }

        private string ExtractStoreBasic(string description)
        {
            var knownBrands = new[] { "uber", "ifood", "mcdonalds", "subway", "amazon", "mercado", "carrefour" };
            var text = description.ToLower();
            
            foreach (var brand in knownBrands)
            {
                if (text.Contains(brand))
                    return brand.ToTitleCase();
            }

            return "Não identificado";
        }

        private string ExtractTypeBasic(string description, string category)
        {
            var text = description.ToLower();

            foreach (var kvp in _typeKeywords)
            {
                if (kvp.Value.Split(',').Any(keyword => text.Contains(keyword.Trim())))
                {
                    return kvp.Key;
                }
            }

            return category switch
            {
                "Transporte" => "Serviço",
                "Alimentação" => "Produto",
                "Saúde" => "Serviço",
                "Educação" => "Serviço",
                _ => "Produto"
            };
        }

        private bool TryParseAmount(string text, out decimal amount)
        {
            amount = 0;
            
            // Remove caracteres não numéricos exceto vírgulas e pontos
            var cleanText = Regex.Replace(text, @"[^\d,.]", "");
            
            if (string.IsNullOrEmpty(cleanText))
                return false;
                
            // Tenta diferentes formatos
            var formats = new[] { cleanText, cleanText.Replace(",", "."), cleanText.Replace(".", ",") };
            
            foreach (var format in formats)
            {
                if (decimal.TryParse(format, out amount))
                    return true;
            }

            return false;
        }

        private Dictionary<string, string> InitializeCategoryKeywords()
        {
            return new Dictionary<string, string>
            {
                { "Transporte", "uber,taxi,onibus,metro,trem,gasolina,combustivel,carro,moto,viagem" },
                { "Alimentação", "comida,restaurante,lanche,pizza,hamburguer,ifood,mcdonalds,subway,padaria,acai" },
                { "Supermercado", "mercado,supermercado,carrefour,extra,pao de acucar,compras,feira" },
                { "Saúde", "farmacia,medico,hospital,consulta,remedio,exame,dentista" },
                { "Educação", "escola,universidade,curso,livro,faculdade,estudo" },
                { "Entretenimento", "cinema,teatro,show,festa,bar,balada,netflix,spotify" },
                { "Casa", "aluguel,condominio,agua,luz,gas,internet,telefone,limpeza" },
                { "Roupas", "roupa,sapato,tenis,camisa,calca,vestido,loja" },
                { "Tecnologia", "celular,computador,notebook,tablet,eletronicos" },
                { "Compras Online", "amazon,mercado livre,shopee,magalu,online" }
            };
        }

        private Dictionary<string, string> InitializeTypeKeywords()
        {
            return new Dictionary<string, string>
            {
                { "Serviço", "servico,consulta,aula,curso,uber,taxi,cinema,bar,restaurante" },
                { "Produto", "compra,comprei,produto,item,roupa,comida,lanche,remedio" },
                { "Assinatura", "assinatura,mensalidade,netflix,spotify,academia,plano" },
                { "Taxa", "taxa,tarifa,multa,juros,anuidade" }
            };
        }
    }

    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }
}