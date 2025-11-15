#!/bin/bash
# 🚀 Deploy Script para FinancasAPI na AWS

echo "🚀 Iniciando deploy da FinancasAPI na AWS..."

# Verificar se as credenciais AWS estão configuradas
echo "🔐 Verificando credenciais AWS..."
if ! aws sts get-caller-identity > /dev/null 2>&1; then
    echo "❌ Credenciais AWS não configuradas!"
    echo "Execute: aws configure"
    echo "Você precisará de:"
    echo "- AWS Access Key ID"
    echo "- AWS Secret Access Key" 
    echo "- Região (ex: us-east-1, sa-east-1)"
    exit 1
fi

echo "✅ Credenciais AWS verificadas!"

# Verificar se SAM CLI está instalado
if ! command -v sam &> /dev/null; then
    echo "❌ SAM CLI não encontrado!"
    echo "Instalando via pip..."
    pip install aws-sam-cli
    if [ $? -ne 0 ]; then
        echo "❌ Falha ao instalar SAM CLI"
        exit 1
    fi
fi

echo "✅ SAM CLI verificado!"

# Build das funções Lambda
echo "🔨 Building aplicação..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Falha no build da aplicação"
    exit 1
fi

# SAM Build
echo "📦 SAM Build..."
sam build

if [ $? -ne 0 ]; then
    echo "❌ Falha no SAM build"
    exit 1
fi

# SAM Deploy
echo "🚀 Fazendo deploy..."
sam deploy --guided

echo "🎉 Deploy concluído!"
echo ""
echo "📍 Endpoints disponíveis:"
echo "• Análise de Gastos: [URL-da-API]/analyze-expense"
echo "• Relatório Mensal: [URL-da-API]/monthly-report"
echo ""
echo "💡 Teste sua API usando:"
echo "curl -X POST [URL-da-API]/analyze-expense -d '{\"description\":\"Paguei 50 reais no supermercado\"}'"