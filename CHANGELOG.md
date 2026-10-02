# Histórico de versões

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e a numeração segue [SemVer](https://semver.org/lang/pt-BR/).

## 1.1.0 — 2026-10-02

### Adicionado
- **Produção:** fichas técnicas (insumos, perdas, rendimento, custo adicional) e ordens de produção com baixa de insumos e custo real.
- **Logística:** cadastro de veículos, fretes (próprio/contratado), contas do frete e **CIOT** por administradora (Pamcard, simulador; demais preparadas).
- **TEF:** pagamento por cartão/PIX no PDV em duas fases (pendente → confirmada/desfeita), simulador visual, estorno no cancelamento da venda.
- **Cobrança bancária genérica:** boleto Febraban para qualquer banco (campo livre por modelo), CNAB 240 (remessa P/Q/R, retorno T/U), validador, simulador de retorno e roteiro de homologação.
- **Reforma tributária:** IBS (UF/Município), CBS e Imposto Seletivo por produto/item/nota, parametrizados (padrão 2026).
- **Impressoras:** cupom pelo Windows ou ESC/POS (corte, gaveta, acentos CP850/860), impressora de documentos, página de teste.
- **Localizador de telas** (Ctrl+K) por palavra-chave, sem acentos.
- **Dados de demonstração** (botão em Configurações e `--dados-demo`).
- **Painel gerencial** em tela própria (F5).
- Cores por módulo, animações curtas e avisos não bloqueantes.
- Documentação de engenharia completa ([docs/](docs/README.md)): visão, requisitos, casos de uso, regras, arquitetura, ADRs, modelo de dados, plano e relatório de testes, implantação, falhas, glossário, manual.
- Teste de interface automatizado do executável publicado.

### Alterado
- **Clientes e fornecedores em um único cadastro de pessoas** com papéis (migração automática que une registros de mesmo CPF/CNPJ).
- **PDV redesenhado** com aparência de frente de caixa, operação total pelo teclado.
- Tela inicial limpa; menu lateral por módulos.
- Botão "?" mostra apenas a explicação da tela atual.
- Nova logo.
- Executável publicado **ofuscado**.

### Corrigido
- Erro ao abrir o editor de nota fiscal (coluna somente leitura da grade).

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
