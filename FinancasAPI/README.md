# 💰 FinancasAPI - Analisador Inteligente de Gastos

> **Análise automática de gastos com IA + AWS DynamoDB**

Transforme descrições como `"Paguei 89 reais na farmácia"` em dados estruturados automaticamente.

## 🚀 Quick Start

### 1. Rodar Localmente (Recomendado)
```bash
# Navegar para o projeto
cd C:/Users/Kevin/Desktop/C#AWS/FinancasAPI

# Iniciar servidor
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5000"

# Acessar interface
http://localhost:5000
```

### 2. Rodar com AWS DynamoDB (Híbrido)
```bash
# Configurar ambiente para AWS
export ASPNETCORE_ENVIRONMENT=Production

# Iniciar com DynamoDB na nuvem
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5000"
```

## 🎯 Como Funciona

**Input:** `"Comprei 120 reais de roupas na Renner"`

**Output:**
```json
{
  "success": true,
  "analysis": {
    "amount": 120.00,
    "category": "Roupas",
    "store": "Renner", 
    "type": "Pessoal",
    "confidence": 0.95
  }
}
```

## 🛠 Tecnologias

- **.NET 8** - Framework principal
- **ASP.NET Core** - Web API
- **AWS DynamoDB** - Banco de dados (opcional)
- **Regex + IA Local** - Análise de texto
- **HTML/CSS/JS** - Interface web

## 📁 Estrutura

```
src/
├── Models/           # DTOs e modelos de dados
├── Services/         # Serviços AWS e análise local
├── Functions/        # Funções Lambda (para deploy AWS)
├── Repositories/     # Acesso a dados
├── LocalTesting/     # Testes locais
└── WebApp/          # Interface web + API REST
```

## 🧪 Testando a API

### Via Interface Web
1. Acesse: http://localhost:5000
2. Digite: `"Gastei 45 reais no Uber"`
3. Veja a análise automática!

### Via cURL
```bash
curl -X POST http://localhost:5000/api/analyze \
  -H "Content-Type: application/json" \
  -d '{"description":"Paguei 75 reais no supermercado"}'
```

### Via PowerShell
```powershell
$body = @{description="Comprei 50 reais de pizza"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/api/analyze" -Method POST -Body $body -ContentType "application/json"
```

## ☁️ Deploy na AWS (Opcional)

### Pré-requisitos AWS
1. **Conta AWS** com Free Tier
2. **Credenciais configuradas:**
   ```bash
   aws configure
   ```

### Deploy Automático
```bash
# PowerShell (Windows)
./deploy-aws.ps1

# Bash (Linux/Mac)
./deploy-aws.sh
```

### Recursos AWS Criados
- **DynamoDB**: Tabela de transações
- **Lambda**: Funções de análise
- **API Gateway**: Endpoints REST
- **IAM**: Roles e policies

## 🔧 Configuração

### Ambiente Local (Padrão)
- **Banco**: Arquivo JSON local
- **Análise**: Regex + categorização local
- **Custo**: Gratuito

### Ambiente AWS (Production)
- **Banco**: DynamoDB na nuvem
- **Análise**: Local + persistência AWS
- **Custo**: ~$0.25-$2/mês (Free Tier)

## 📊 Categorias Suportadas

- 🍔 **Alimentação**: Restaurantes, fast-food, delivery
- 🚗 **Transporte**: Uber, táxi, combustível
- 🛒 **Compras**: Supermercado, shopping, online
- 👕 **Roupas**: Lojas de vestuário
- 💊 **Saúde**: Farmácia, consultas médicas
- 📱 **Tecnologia**: Eletrônicos, software
- 🎮 **Entretenimento**: Cinema, jogos, streaming
- 🏠 **Casa**: Móveis, decoração, limpeza

## ❓ Troubleshooting

### Erro de porta ocupada
```bash
# Mudar porta
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5001"
```

### Erro AWS credentials
```bash
# Verificar configuração
aws sts get-caller-identity

# Reconfigurar se necessário
aws configure
```

### Build error
```bash
# Limpar e rebuildar
dotnet clean
dotnet build
```

## 📈 Roadmap

- ✅ Análise local de gastos
- ✅ Interface web moderna  
- ✅ Integração AWS DynamoDB
- 🔄 Deploy Lambda completo
- 📱 App mobile
- 🤖 Bot WhatsApp
- 📊 Dashboard analytics

## 📝 Licença

MIT License - Uso livre para projetos pessoais e comerciais.

---

**🎉 Sua API de análise financeira está pronta!**