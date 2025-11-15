using System;

namespace FinancasAPI.LocalTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🔍 TESTE DO BUG FIX - DECIMAL PARSING");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            var analyzer = new LocalExpenseAnalyzer();

            // Teste específico do Netflix que estava com problema
            var netflixTest = "Netflix assinatura mensal 29,90";
            var result = analyzer.AnalyzeExpenseAsync(netflixTest).Result;
            
            Console.WriteLine($"📝 Descrição: {netflixTest}");
            Console.WriteLine($"💰 Valor Extraído: R$ {result.Amount:F2}");
            Console.WriteLine($"🏪 Estabelecimento: {result.Store}");
            Console.WriteLine($"📂 Categoria: {result.Category}");
            Console.WriteLine();
            
            if (result.Amount == 29.90m)
            {
                Console.WriteLine("✅ BUG CORRIGIDO! Valor está correto (29,90)");
            }
            else
            {
                Console.WriteLine($"❌ BUG AINDA EXISTE! Valor incorreto: {result.Amount} (esperado: 29,90)");
            }
            Console.WriteLine();

            // Outros testes para verificar diferentes formatos
            string[] testCases = {
                "Uber viagem 15,50",
                "Mercado compras R$ 87,30", 
                "Academia mensalidade 120,00",
                "Farmacia remedios 45,90",
                "Gasolina posto 180,25",
                "Combustivel 95,50 reais",
                "Padaria pao doce 8,75"
            };

            Console.WriteLine("🧪 TESTES ADICIONAIS - FORMATOS DECIMAIS");
            Console.WriteLine("=========================================");
            
            foreach (var testCase in testCases)
            {
                var testResult = analyzer.AnalyzeExpenseAsync(testCase).Result;
                Console.WriteLine($"📝 {testCase} → R$ {testResult.Amount:F2}");
            }

            Console.WriteLine();
            Console.WriteLine("✅ Análise concluída!");
        }
    }
}