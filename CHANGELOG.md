# Histórico de versões

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e a numeração segue [SemVer](https://semver.org/lang/pt-BR/).

## 1.2.0 — 2026-10-02

### Adicionado
- **Telas embutidas em abas**: as opções do menu abrem no espaço à direita, não em janelas soltas (× fecha, Ctrl+Tab alterna, Ctrl+W fecha).
- **Log de atividades** de cada ação (acessos, telas, botões, confirmações, mensagens, gravações, erros) em banco SQLite separado (`loja_log.db`), com tela de consulta para administradores e retenção configurável (migração 12).
- **Atalho na área de trabalho** criado na primeira execução.
- **Continuar de onde parou**: a tela inicial e o localizador mostram as telas usadas por último; o menu lateral destaca a tela ativa.
- **Troca obrigatória da senha padrão** no primeiro acesso.
- Grades explicam quando estão vazias, destacam a linha sob o mouse e mostram cabeçalhos longos com reticências.
- Botão "Gerar pedido de compra" a partir da sugestão de estoque; total vencido no rodapé do Financeiro; localizador entende frases ("como cadastrar cliente").
- **69 cenários de uso automatizados** escritos por agentes de teste exploratório (um por área), executados na bateria: 898 verificações.
- Documentação: histórico literal das solicitações ([13](docs/13-historico-de-solicitacoes.md)), relatório dos testes exploratórios ([14](docs/14-testes-exploratorios.md)), ADRs 0016–0018, novas regras RN-LOG, RN-NAV, RN-INS.

### Alterado
- **PDV, pagamento e simulador TEF com fundo branco.**
- No pagamento do PDV, as teclas 1–5 escolhem a forma de pagamento (Enter = dinheiro).
- Aviso de usuário sem e-mail no login virou notificação (não exige OK).
- Validações mais rígidas em cadastros, notas, cobrança, fretes, produção e configurações (ver [04](docs/04-regras-de-negocio.md)).

### Corrigido
- **72 falhas** encontradas pelos testes exploratórios — destaques: F6 do PDV removia o item errado; PIX gravado como dinheiro; boleto continuava ativo após cancelar a conta; retorno bancário importado duas vezes contava em dobro; inventário com saldo desatualizado; mesma NF lançada duas vezes no estoque; CFOP incoerente emitido; sistema podia ficar sem administrador; produção acima do planejado deixava insumo negativo; frete com autônomo seguia viagem sem CIOT. Lista completa em [14](docs/14-testes-exploratorios.md).

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
