using System.Text.Json;
using FinancasAPI.Models;

namespace FinancasAPI.LocalTesting
{
    public class LocalTestRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Testando FinancasAPI Localmente");
            Console.WriteLine("==================================");

            // Simulador local (sem dependências AWS)
            var localAnalyzer = new LocalExpenseAnalyzer();

            // Casos de teste
            var testCases = new[]
            {
                "Paguei 37 reais no Uber para o trabalho",
                "iFood pizza 89 reais entrega",
                "Supermercado Extra compras 156,50",
                "Farmácia São Paulo remédio 25 reais",
                "Netflix assinatura mensal 29,90",
                "Gasolina posto Shell 95 reais",
                "McDonald's lanche BigMac 18,50"
            };

            Console.WriteLine("\n📊 Testando Análise de Gastos:");
            Console.WriteLine("==============================");

            foreach (var description in testCases)
            {
                Console.WriteLine($"\n📝 Entrada: \"{description}\"");
                
                var result = await localAnalyzer.AnalyzeExpenseAsync(description);
                
                Console.WriteLine($"💰 Valor: R$ {result.Amount:F2}");
                Console.WriteLine($"📂 Categoria: {result.Category}");
                Console.WriteLine($"🏪 Loja: {result.Store}");
                Console.WriteLine($"🏷️ Tipo: {result.Type}");
                Console.WriteLine($"🎯 Confiança: {result.Confidence:P1}");
                Console.WriteLine($"{"".PadRight(50, '-')}");
            }

            // Teste interativo
            Console.WriteLine("\n\n🎮 Modo Interativo - Digite suas próprias descrições:");
            Console.WriteLine("=====================================================");
            Console.WriteLine("(Digite 'sair' para terminar)");

            while (true)
            {
                Console.Write("\n📝 Digite a descrição do gasto: ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) || input.ToLower() == "sair")
                    break;

                try
                {
                    var result = await localAnalyzer.AnalyzeExpenseAsync(input);
                    
                    Console.WriteLine("\n✅ Resultado:");
                    Console.WriteLine($"   💰 Valor: R$ {result.Amount:F2}");
                    Console.WriteLine($"   📂 Categoria: {result.Category}");
                    Console.WriteLine($"   🏪 Loja: {result.Store}");
                    Console.WriteLine($"   🏷️ Tipo: {result.Type}");
                    Console.WriteLine($"   🎯 Confiança: {result.Confidence:P1}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro: {ex.Message}");
                }
            }

            Console.WriteLine("\n👋 Teste finalizado!");
        }
    }
}