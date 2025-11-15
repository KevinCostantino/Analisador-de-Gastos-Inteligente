using System.Text.RegularExpressions;
using FinancasAPI.Models;

namespace FinancasAPI.LocalTesting
{
    public class LocalExpenseAnalyzer
    {
        private readonly Dictionary<string, string> _categoryKeywords;
        private readonly Dictionary<string, string> _typeKeywords;
        private readonly Dictionary<string, string> _storeKeywords;

        public LocalExpenseAnalyzer()
        {
            _categoryKeywords = InitializeCategoryKeywords();
            _typeKeywords = InitializeTypeKeywords();
            _storeKeywords = InitializeStoreKeywords();
        }

        public Task<ExtractedExpenseInfo> AnalyzeExpenseAsync(string description)
        {
            var amount = ExtractAmount(description);
            var store = ExtractStore(description);
            var category = ExtractCategory(description, store);
            var type = ExtractType(description, category, store);
            var confidence = CalculateConfidence(amount > 0, !string.IsNullOrEmpty(store) && store != "Não identificado");

            var result = new ExtractedExpenseInfo
            {
                Amount = amount,
                Store = store,
                Category = category,
                Type = type,
                Confidence = confidence
            };

            return Task.FromResult(result);
        }

        private decimal ExtractAmount(string description)
        {
            var text = description.ToLower();
            
            // Padrões para valores monetários brasileiros
            var patterns = new[]
            {
                @"r\$\s*(\d+)(?:,(\d{1,2}))?", // R$ 123,45 ou R$ 123
                @"(\d+)(?:,(\d{1,2}))?\s*reais?", // 123,45 reais ou 123 reais
                @"(\d+)(?:,(\d{1,2}))?\s*(?:no|na|do|da)", // 123,45 no/na
                @"\b(\d+)(?:,(\d{1,2}))?\b" // qualquer número com ou sem centavos
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var reaisStr = match.Groups[1].Value;
                    var centavosStr = match.Groups[2].Value;
                    
                    if (int.TryParse(reaisStr, out int reais))
                    {
                        decimal amount = reais;
                        
                        // Adicionar centavos se existir
                        if (!string.IsNullOrEmpty(centavosStr))
                        {
                            if (int.TryParse(centavosStr.PadRight(2, '0'), out int centavos))
                            {
                                amount += centavos / 100.0m;
                            }
                        }
                        
                        return amount;
                    }
                }
            }

            return 0;
        }

        private string ExtractStore(string description)
        {
            var text = description.ToLower();

            // Buscar por lojas conhecidas
            foreach (var kvp in _storeKeywords)
            {
                if (text.Contains(kvp.Key))
                    return kvp.Value;
            }

            // Tentar extrair nomes próprios (maiúscula após espaço)
            var words = description.Split(' ');
            foreach (var word in words)
            {
                if (word.Length > 3 && char.IsUpper(word[0]) && 
                    !_categoryKeywords.Values.Any(v => v.Contains(word.ToLower())))
                {
                    return word;
                }
            }

            return "Não identificado";
        }

        private string ExtractCategory(string description, string store)
        {
            var text = description.ToLower();

            // Verificar palavras-chave de categoria
            foreach (var kvp in _categoryKeywords)
            {
                var keywords = kvp.Value.Split(',').Select(k => k.Trim());
                if (keywords.Any(keyword => text.Contains(keyword)))
                    return kvp.Key;
            }

            // Categorizar baseado na loja
            var storeCategories = new Dictionary<string, string>
            {
                { "uber", "Transporte" },
                { "ifood", "Alimentação" },
                { "mcdonalds", "Alimentação" },
                { "extra", "Supermercado" },
                { "carrefour", "Supermercado" },
                { "farmacia", "Saúde" },
                { "netflix", "Entretenimento" },
                { "amazon", "Compras Online" }
            };

            var storeLower = store.ToLower();
            foreach (var kvp in storeCategories)
            {
                if (storeLower.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Outros";
        }

        private string ExtractType(string description, string category, string store)
        {
            var text = description.ToLower();

            // Verificar palavras-chave de tipo
            foreach (var kvp in _typeKeywords)
            {
                var keywords = kvp.Value.Split(',').Select(k => k.Trim());
                if (keywords.Any(keyword => text.Contains(keyword)))
                    return kvp.Key;
            }

            // Tipo baseado na categoria
            return category switch
            {
                "Transporte" => "Serviço",
                "Alimentação" => "Produto",
                "Saúde" => text.Contains("consulta") ? "Serviço" : "Produto",
                "Entretenimento" => "Serviço",
                "Educação" => "Serviço",
                _ => "Produto"
            };
        }

        private double CalculateConfidence(bool hasAmount, bool hasStore)
        {
            double confidence = 0.3; // Base

            if (hasAmount) confidence += 0.4;
            if (hasStore) confidence += 0.3;

            return Math.Min(confidence, 1.0);
        }

        private Dictionary<string, string> InitializeCategoryKeywords()
        {
            return new Dictionary<string, string>
            {
                { "Transporte", "uber,taxi,onibus,metro,trem,gasolina,combustivel,posto" },
                { "Alimentação", "ifood,comida,restaurante,lanche,pizza,hamburguer,mcdonalds,subway,acai" },
                { "Supermercado", "mercado,supermercado,carrefour,extra,compras,feira" },
                { "Saúde", "farmacia,medico,hospital,consulta,remedio,exame" },
                { "Educação", "escola,universidade,curso,livro,faculdade" },
                { "Entretenimento", "cinema,netflix,spotify,show,festa,bar" },
                { "Casa", "aluguel,agua,luz,gas,internet,telefone" },
                { "Compras Online", "amazon,mercado livre,online" }
            };
        }

        private Dictionary<string, string> InitializeTypeKeywords()
        {
            return new Dictionary<string, string>
            {
                { "Serviço", "servico,consulta,uber,taxi,cinema,netflix" },
                { "Produto", "compra,produto,comida,remedio,gasolina" },
                { "Assinatura", "assinatura,mensalidade,netflix,spotify,plano" }
            };
        }

        private Dictionary<string, string> InitializeStoreKeywords()
        {
            return new Dictionary<string, string>
            {
                { "uber", "Uber" },
                { "ifood", "iFood" },
                { "mcdonalds", "McDonald's" },
                { "subway", "Subway" },
                { "extra", "Extra" },
                { "carrefour", "Carrefour" },
                { "amazon", "Amazon" },
                { "netflix", "Netflix" },
                { "spotify", "Spotify" },
                { "shell", "Shell" },
                { "petrobras", "Petrobras" },
                { "farmacia", "Farmácia" }
            };
        }
    }
}