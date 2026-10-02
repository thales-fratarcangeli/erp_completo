# Histórico de versões

## 1.0.0 — 2026-10-01

Primeira versão publicada.

- **Vendas:** PDV com código de barras, troco e venda a prazo parcelada; orçamentos (aprovar → pedido); pedidos de venda (faturar → venda); comprovante não fiscal.
- **Compras:** pedidos de compra (receber → entrada no estoque); importação do XML da NF-e do fornecedor com vínculo de produtos memorizado, fator de conversão e custo com frete/IPI/ST.
- **Estoque:** movimentações com histórico, custo médio ponderado, situação, sugestão de compra e inventário.
- **Financeiro:** contas a receber/pagar automáticas e manuais, baixa, estorno, cancelamento, vencidas com dias de atraso.
- **Fiscal:** NF-e/NFC-e de saída e entrada, CFOP automático, impostos, numeração, chave de acesso e DANFE (sem transmissão à SEFAZ).
- **Gestão:** painel com indicadores comparados ao período anterior e gráficos; relatórios com exportação CSV.
- **Sistema:**
  - instalação por cópia do executável; localização automática do banco, com opção de criar novo ou indicar o arquivo;
  - migrations versionadas com backup automático antes de atualizar; backup diário;
  - tela única de configurações com todos os parâmetros;
  - usuários com username, senha e e-mail; recuperação de senha por código enviado pelo Resend;
  - ajuda em todas as telas (botão **?** e F1) e manual do usuário gerado a partir dela;
  - logo/ícone próprio;
  - revisão de redundância: um único ponto de entrada por função.
