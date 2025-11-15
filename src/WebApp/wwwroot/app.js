// 💰 Analisador de Gastos - JavaScript
class ExpenseAnalyzer {
    constructor() {
        this.initializeElements();
        this.setupEventListeners();
        this.loadHistory();
        this.updateStats();
    }

    initializeElements() {
        this.expenseInput = document.getElementById('expenseInput');
        this.analyzeBtn = document.getElementById('analyzeBtn');
        this.clearBtn = document.getElementById('clearBtn');
        this.reportBtn = document.getElementById('reportBtn');
        this.clearHistoryBtn = document.getElementById('clearHistoryBtn');
        this.loading = document.getElementById('loading');
        this.result = document.getElementById('result');
        this.resultContent = document.getElementById('resultContent');
        this.history = document.getElementById('history');
        this.alertContainer = document.getElementById('alertContainer');
        this.totalExpenses = document.getElementById('totalExpenses');
        this.monthlyTotal = document.getElementById('monthlyTotal');
        this.avgExpense = document.getElementById('avgExpense');
    }

    setupEventListeners() {
        this.analyzeBtn.addEventListener('click', () => this.analyzeExpense());
        this.clearBtn.addEventListener('click', () => this.clearInput());
        this.reportBtn.addEventListener('click', () => this.showReport());
        this.clearHistoryBtn.addEventListener('click', () => this.clearHistory());
        
        // Enter para analisar
        this.expenseInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.analyzeExpense();
            }
        });

        // Auto-resize textarea
        this.expenseInput.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
    }

    async analyzeExpense() {
        const description = this.expenseInput.value.trim();
        
        if (!description) {
            this.showAlert('⚠️ Por favor, descreva seu gasto primeiro!', 'error');
            return;
        }

        this.setLoading(true);
        this.clearAlert();

        try {
            const response = await fetch('/api/analyze', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ description })
            });

            const data = await response.json();

            if (response.ok) {
                this.showResult(data);
                this.clearInput();
                await this.loadHistory();
                await this.updateStats();
                this.showAlert('✅ Gasto analisado e salvo com sucesso!', 'success');
            } else {
                this.showAlert(`❌ Erro: ${data.message || 'Falha na análise'}`, 'error');
            }
        } catch (error) {
            console.error('Erro:', error);
            this.showAlert('❌ Erro de conexão. Verifique se o servidor está rodando.', 'error');
        } finally {
            this.setLoading(false);
        }
    }

    showResult(data) {
        const confidence = Math.round((data.confianca || 0) * 100);
        
        this.resultContent.innerHTML = `
            <div class="result-item">
                <div class="icon">💰</div>
                <div class="label">Valor</div>
                <div class="value">R$ ${data.valor.toFixed(2)}</div>
            </div>
            <div class="result-item">
                <div class="icon">📂</div>
                <div class="label">Categoria</div>
                <div class="value">${data.categoria}</div>
            </div>
            <div class="result-item">
                <div class="icon">🏪</div>
                <div class="label">Loja</div>
                <div class="value">${data.loja}</div>
            </div>
            <div class="result-item">
                <div class="icon">🏷️</div>
                <div class="label">Tipo</div>
                <div class="value">${data.tipo}</div>
            </div>
            <div class="result-item">
                <div class="icon">🎯</div>
                <div class="label">Confiança</div>
                <div class="value">${confidence}%</div>
            </div>
        `;

        this.result.style.display = 'block';
        this.result.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    async loadHistory() {
        try {
            const response = await fetch('/api/history');
            const history = await response.json();

            if (history.length === 0) {
                this.history.innerHTML = `
                    <p style="text-align: center; color: #666; padding: 20px;">
                        📝 Nenhum gasto registrado ainda. Comece analisando um gasto!
                    </p>
                `;
                return;
            }

            // Mostrar últimos 10 gastos
            const recent = history.slice(-10).reverse();
            
            this.history.innerHTML = recent.map(expense => {
                const date = new Date(expense.createdAt).toLocaleDateString('pt-BR');
                const time = new Date(expense.createdAt).toLocaleTimeString('pt-BR', { 
                    hour: '2-digit', 
                    minute: '2-digit' 
                });

                return `
                    <div class="history-item">
                        <div class="amount">R$ ${expense.amount.toFixed(2)}</div>
                        <div class="description">${expense.description}</div>
                        <div class="details">
                            ${this.getCategoryIcon(expense.category)} ${expense.category} • 
                            🏪 ${expense.store} • 
                            🏷️ ${expense.type} • 
                            📅 ${date} ${time}
                        </div>
                    </div>
                `;
            }).join('');

        } catch (error) {
            console.error('Erro ao carregar histórico:', error);
            this.history.innerHTML = `
                <div class="alert alert-error">
                    ❌ Erro ao carregar histórico
                </div>
            `;
        }
    }

    async updateStats() {
        try {
            const response = await fetch('/api/history');
            const history = await response.json();

            if (history.length === 0) {
                this.totalExpenses.textContent = '0';
                this.monthlyTotal.textContent = 'R$ 0';
                this.avgExpense.textContent = 'R$ 0';
                return;
            }

            // Estatísticas gerais
            const total = history.reduce((sum, expense) => sum + expense.amount, 0);
            const avg = total / history.length;

            // Gastos do mês atual
            const currentMonth = new Date().toISOString().substring(0, 7);
            const monthlyExpenses = history.filter(expense => 
                expense.createdAt.substring(0, 7) === currentMonth
            );
            const monthlyTotal = monthlyExpenses.reduce((sum, expense) => sum + expense.amount, 0);

            this.totalExpenses.textContent = history.length.toString();
            this.monthlyTotal.textContent = `R$ ${monthlyTotal.toFixed(2)}`;
            this.avgExpense.textContent = `R$ ${avg.toFixed(2)}`;

        } catch (error) {
            console.error('Erro ao atualizar estatísticas:', error);
        }
    }

    async showReport() {
        const currentMonth = new Date().toISOString().substring(0, 7);
        
        try {
            const response = await fetch(`/api/report/${currentMonth}`);
            const report = await response.json();

            let reportHtml = `
                <div class="alert alert-success">
                    <h3>📊 Relatório de ${this.formatMonth(report.month)}</h3>
                    <p><strong>💰 Total gasto:</strong> R$ ${report.totalGastos.toFixed(2)}</p>
                    <p><strong>🧾 Total de transações:</strong> ${report.transacoes}</p>
                </div>
            `;

            if (Object.keys(report.categorias).length > 0) {
                reportHtml += '<h4>📂 Por Categoria:</h4>';
                for (const [categoria, dados] of Object.entries(report.categorias)) {
                    reportHtml += `
                        <div class="history-item">
                            ${this.getCategoryIcon(categoria)} <strong>${categoria}:</strong> 
                            R$ ${dados.total.toFixed(2)} (${dados.percentage}%) - ${dados.count} transações
                        </div>
                    `;
                }
            }

            if (Object.keys(report.lojas).length > 0) {
                reportHtml += '<h4 style="margin-top: 20px;">🏪 Por Loja:</h4>';
                const sortedStores = Object.entries(report.lojas)
                    .sort(([,a], [,b]) => b - a)
                    .slice(0, 10);

                for (const [loja, valor] of sortedStores) {
                    reportHtml += `
                        <div class="history-item">
                            🏪 <strong>${loja}:</strong> R$ ${valor.toFixed(2)}
                        </div>
                    `;
                }
            }

            this.showAlert(reportHtml, 'success');

        } catch (error) {
            console.error('Erro ao gerar relatório:', error);
            this.showAlert('❌ Erro ao gerar relatório', 'error');
        }
    }

    async clearHistory() {
        if (!confirm('🗑️ Tem certeza que deseja limpar todo o histórico?')) {
            return;
        }

        try {
            const response = await fetch('/api/history', { method: 'DELETE' });
            
            if (response.ok) {
                await this.loadHistory();
                await this.updateStats();
                this.showAlert('✅ Histórico limpo com sucesso!', 'success');
            } else {
                this.showAlert('❌ Erro ao limpar histórico', 'error');
            }
        } catch (error) {
            console.error('Erro:', error);
            this.showAlert('❌ Erro de conexão', 'error');
        }
    }

    getCategoryIcon(category) {
        const icons = {
            'Transporte': '🚗',
            'Alimentação': '🍕',
            'Supermercado': '🛒',
            'Saúde': '💊',
            'Educação': '📚',
            'Entretenimento': '🎬',
            'Casa': '🏠',
            'Roupas': '👕',
            'Tecnologia': '💻',
            'Compras Online': '📦',
            'Outros': '📝'
        };
        return icons[category] || '📝';
    }

    formatMonth(monthStr) {
        const [year, month] = monthStr.split('-');
        const months = [
            'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
            'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
        ];
        return `${months[parseInt(month) - 1]} de ${year}`;
    }

    setLoading(loading) {
        this.loading.classList.toggle('active', loading);
        this.analyzeBtn.disabled = loading;
        
        if (loading) {
            this.result.style.display = 'none';
        }
    }

    clearInput() {
        this.expenseInput.value = '';
        this.expenseInput.style.height = 'auto';
        this.expenseInput.focus();
    }

    showAlert(message, type) {
        const alertClass = type === 'error' ? 'alert-error' : 'alert-success';
        this.alertContainer.innerHTML = `
            <div class="alert ${alertClass}">
                ${message}
            </div>
        `;
        
        // Auto-hide após 5 segundos para alertas de sucesso
        if (type === 'success') {
            setTimeout(() => this.clearAlert(), 5000);
        }
    }

    clearAlert() {
        this.alertContainer.innerHTML = '';
    }
}

// Função global para exemplos
function setExample(text) {
    const analyzer = window.expenseAnalyzer;
    analyzer.expenseInput.value = text;
    analyzer.expenseInput.focus();
    analyzer.expenseInput.style.height = 'auto';
    analyzer.expenseInput.style.height = (analyzer.expenseInput.scrollHeight) + 'px';
}

// Inicializar quando a página carregar
document.addEventListener('DOMContentLoaded', () => {
    window.expenseAnalyzer = new ExpenseAnalyzer();
    console.log('💰 Analisador de Gastos Web carregado!');
});