#!/bin/bash
# 🚀 Deploy simplificado da FinancasAPI

set -e

echo "🚀 Iniciando deployment..."

# Verificar pré-requisitos
if ! aws sts get-caller-identity > /dev/null 2>&1; then
    echo "❌ Configure AWS: aws configure"
    exit 1
fi

if ! command -v sam > /dev/null 2>&1; then
    echo "❌ Instale SAM CLI: pip install aws-sam-cli"
    exit 1
fi

echo "✅ Pré-requisitos OK"

# Build e Deploy
echo "🔨 Building..."
dotnet build

echo "📦 SAM Build..."
sam build

echo "🚀 Deploy..."
sam deploy --guided

echo ""
echo "🎉 Deploy concluído!"
echo "💡 Teste: curl -X POST [API-URL]/analyze-expense -d '{\"description\":\"Gastei 50 reais\"}'"