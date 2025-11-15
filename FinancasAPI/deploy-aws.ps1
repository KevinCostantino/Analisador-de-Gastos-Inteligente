# 🚀 Deploy Script para FinancasAPI na AWS (PowerShell)

Write-Host "🚀 Iniciando deploy da FinancasAPI na AWS..." -ForegroundColor Green

# Verificar se as credenciais AWS estão configuradas
Write-Host "🔐 Verificando credenciais AWS..." -ForegroundColor Yellow
try {
    aws sts get-caller-identity | Out-Null
    Write-Host "✅ Credenciais AWS verificadas!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Credenciais AWS não configuradas!" -ForegroundColor Red
    Write-Host "Execute: aws configure" -ForegroundColor Yellow
    Write-Host "Você precisará de:" -ForegroundColor Yellow
    Write-Host "- AWS Access Key ID" -ForegroundColor Yellow
    Write-Host "- AWS Secret Access Key" -ForegroundColor Yellow
    Write-Host "- Região (ex: us-east-1, sa-east-1)" -ForegroundColor Yellow
    exit 1
}

# Verificar se SAM CLI está disponível  
Write-Host "🔧 Verificando SAM CLI..." -ForegroundColor Yellow
try {
    sam --version | Out-Null
    Write-Host "✅ SAM CLI verificado!" -ForegroundColor Green
}
catch {
    Write-Host "❌ SAM CLI não encontrado! Instalando..." -ForegroundColor Red
    pip install aws-sam-cli
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Falha ao instalar SAM CLI" -ForegroundColor Red
        exit 1
    }
}

# Build da aplicação .NET
Write-Host "🔨 Building aplicação .NET..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha no build da aplicação" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build concluído!" -ForegroundColor Green

# SAM Build
Write-Host "📦 Executando SAM Build..." -ForegroundColor Yellow
sam build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha no SAM build" -ForegroundColor Red
    exit 1
}

Write-Host "✅ SAM Build concluído!" -ForegroundColor Green

# SAM Deploy
Write-Host "🚀 Fazendo deploy na AWS..." -ForegroundColor Yellow
sam deploy --guided

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "🎉 Deploy concluído com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📍 Sua API está disponível nos seguintes endpoints:" -ForegroundColor Cyan
    Write-Host "• Análise de Gastos: [URL-da-API]/analyze-expense" -ForegroundColor White
    Write-Host "• Relatório Mensal: [URL-da-API]/monthly-report" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Teste sua API usando:" -ForegroundColor Yellow
    Write-Host 'curl -X POST [URL-da-API]/analyze-expense -H "Content-Type: application/json" -d "{\"description\":\"Paguei 50 reais no supermercado\"}"' -ForegroundColor White
    Write-Host ""
    Write-Host "🌐 Acesse o AWS Console para ver seus recursos criados!" -ForegroundColor Cyan
} else {
    Write-Host "❌ Falha no deploy" -ForegroundColor Red
    exit 1
}