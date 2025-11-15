# 🔧 Guia de Operação - FinancasAPI

## ⚡ Início Rápido (30 segundos)

```bash
# 1. Navegar para o projeto
cd C:/Users/Kevin/Desktop/C#AWS/FinancasAPI

# 2. Iniciar servidor
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5000"

# 3. Abrir navegador
http://localhost:5000
```

## 🧪 Testando a API

### Teste 1: Interface Web
- **URL**: http://localhost:5000
- **Input**: `"Comprei 89 reais na farmácia"`
- **Resultado**: Análise automática + histórico

### Teste 2: API REST
```bash
curl -X POST http://localhost:5000/api/analyze \
  -H "Content-Type: application/json" \
  -d '{"description":"Paguei 120 reais no iFood"}'
```

### Teste 3: PowerShell
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/analyze" -Method POST -Body '{"description":"Gastei 45 reais no Uber"}' -ContentType "application/json"
$response | ConvertTo-Json -Depth 3
```

## 🎯 Casos de Teste

| Descrição | Categoria Esperada | Valor | Loja |
|-----------|-------------------|--------|------|
| "Paguei 50 reais no supermercado" | Alimentação | 50.00 | Supermercado |
| "Gastei 120 na Renner" | Roupas | 120.00 | Renner |
| "Uber de 25 reais" | Transporte | 25.00 | Uber |
| "Pizza 45 reais iFood" | Alimentação | 45.00 | iFood |
| "Remédio 80 reais farmácia" | Saúde | 80.00 | Farmácia |

## ☁️ Modo AWS (Opcional)

### Ativar DynamoDB na AWS
```bash
# Configurar credenciais
aws configure

# Criar tabela (já criada se seguiu tutorial)
aws dynamodb describe-table --table-name FinancasAPI-Transactions --region sa-east-1

# Rodar em modo AWS
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5000"
```

## 🛠 Comandos Úteis

### Build e Teste
```bash
# Build completo
dotnet build

# Limpar cache
dotnet clean

# Executar testes
dotnet test

# Rodar em porta diferente
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5001"
```

### Verificar Logs
```bash
# Logs do servidor aparecem automaticamente no terminal
# Para debug detalhado, edite appsettings.json:
# "LogLevel": { "Default": "Debug" }
```

## 🔍 Troubleshooting

### Problema: Porta ocupada
```bash
# Solução: Usar porta diferente
dotnet run --project src/WebApp/WebApp.csproj --urls="http://localhost:5001"
```

### Problema: Build error
```bash
# Solução: Limpar e rebuildar
dotnet clean
dotnet restore
dotnet build
```

### Problema: AWS credentials
```bash
# Verificar configuração
aws sts get-caller-identity

# Reconfigurar se necessário
aws configure
```

## 📊 Monitoramento

### Verificar Saúde da API
```bash
curl http://localhost:5000/health
```

### Ver Histórico (Local)
- Arquivo: `src/WebApp/history.json`
- Interface: http://localhost:5000 → Seção "Histórico"

### Ver Dados AWS (Se configurado)
```bash
aws dynamodb scan --table-name FinancasAPI-Transactions --region sa-east-1
```

## 🎛 Configurações

### Arquivo: `src/WebApp/appsettings.json` (Local)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Arquivo: `src/WebApp/appsettings.Production.json` (AWS)
```json
{
  "AWS": {
    "Region": "sa-east-1"
  },
  "DynamoDB": {
    "TableName": "FinancasAPI-Transactions",
    "UseAWS": true
  }
}
```

## 🔄 Deploy Serverless (Avançado)

```bash
# Deploy completo na AWS
./deploy.sh

# Deploy PowerShell
./deploy-aws.ps1
```

---

**💡 Dica**: Para desenvolvimento, use sempre o modo local primeiro. AWS apenas para produção!