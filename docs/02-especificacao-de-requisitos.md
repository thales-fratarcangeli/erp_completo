# 02 — Especificação de requisitos de software (ERS)

| | |
|---|---|
| **Documento** | Especificação de requisitos (estrutura inspirada na ISO/IEC/IEEE 29148) |
| **Sistema** | ERP Loja 1.5.0 (banco v16) |
| **Status** | Vigente |
| **Convenções** | RF = requisito funcional; RNF = não funcional; RI = interface externa; prioridade **E** (essencial), **I** (importante), **D** (desejável); RN = regra de negócio ([04](04-regras-de-negocio.md)); UC = caso de uso ([03](03-casos-de-uso.md)) |

## 1. Introdução

### 1.1 Propósito
Especificar o comportamento observável do ERP Loja para orientar desenvolvimento, testes, implantação e evolução.

### 1.2 Escopo
Conforme o [documento de visão](01-documento-de-visao.md), seção 5. Itens fora do escopo na seção 6 da visão.

### 1.3 Referências
[Regras de negócio](04-regras-de-negocio.md) · [Arquitetura](05-arquitetura.md) · [Modelo de dados](06-modelo-de-dados.md) ·
[Plano de testes](07-plano-de-testes.md) · [Segurança — OWASP](15-seguranca-owasp.md) · [Análise do ERP da gráfica](16-analise-erp-grafica.md) ·
[App para celular (estudo)](17-app-celular.md) · Leiaute Febraban CNAB 240 · Manual de orientação do contribuinte NF-e · EC 132/2023 e LC 214/2025 ·
API Messages da Anthropic · API `/api/chat` do Ollama · OWASP Top 10 para aplicações com LLM.

## 2. Descrição geral

- **Perspectiva**: aplicação desktop autônoma (Windows Forms, .NET 10) com banco SQLite em arquivo (`loja.db`) e banco de log separado (`loja_log.db`); integrações opcionais por HTTPS (Resend, Pamcard, API da Anthropic para o assistente), por HTTP local (Ollama, no próprio computador), por arquivos (XML de NF-e, CNAB 240) ou bibliotecas locais (TEF, impressoras).
- **Classes de usuário**: administrador e usuário comum (ver RN-USU-04).
- **Ambiente operacional**: Windows 10/11 x64; impressoras instaladas no Windows (driver ou ESC/POS).

## 3. Requisitos funcionais

### 3.1 Sistema e acesso

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-SIS-01 | O sistema deve funcionar a partir de um único executável copiado para a máquina, sem instalação. | E | UC-01 |
| RF-SIS-02 | Ao abrir, deve localizar o banco automaticamente e, se não encontrar, oferecer criar um banco novo ou indicar o arquivo. | E | RN-BD-01, UC-01 |
| RF-SIS-03 | Deve atualizar bancos antigos por migrations versionadas, com backup prévio, recusando bancos mais novos que o executável. | E | RN-BD-02/03 |
| RF-SIS-04 | Deve autenticar por username e senha e permitir recuperar a senha por código enviado ao e-mail. | E | RN-USU, UC-02 |
| RF-SIS-05 | Deve oferecer uma única tela de configurações com todos os parâmetros do sistema. | E | RN-PAR |
| RF-SIS-06 | Toda tela deve ter um botão "?" (e F1) que mostre a explicação daquela tela. | E | UC-14 |
| RF-SIS-07 | Deve permitir localizar telas por palavra-chave (Ctrl+K), buscando em títulos, palavras-chave e ajuda. | I | UC-14 |
| RF-SIS-08 | Cada função deve ter um único ponto de entrada no menu (sem telas redundantes). | E | — |
| RF-SIS-09 | Deve fazer backup diário automático e permitir backup manual e troca de banco. | I | RN-BD-04 |
| RF-SIS-10 | Deve gerar dados fictícios de demonstração sob demanda (somente administrador, com backup prévio). | D | — |
| RF-SIS-11 | As telas do menu devem abrir no espaço à direita do menu, uma por vez (sem abas e sem janelas soltas), avisando quando houver dados não salvos. | E | RN-NAV-01..03, ADR-0019 |
| RF-SIS-12 | Toda ação deve gerar registro de log em banco SQLite separado, consultável por administradores. | E | RN-LOG |
| RF-SIS-13 | Na primeira execução, deve criar um atalho na área de trabalho. | I | RN-INS |
| RF-SIS-14 | O primeiro acesso com a senha padrão deve exigir a troca da senha. | E | RN-USU |
| RF-SIS-15 | A tela inicial e o localizador devem oferecer as telas usadas por último. | D | RN-NAV |
| RF-SIS-16 | Assistente de dúvidas por IA (painel ao lado da tela, Ctrl+I ou "✦ Assistente"), restrito à ajuda do sistema, sem dados da loja e com máscara de dados pessoais; ligado, configurado e testado ("Testar assistente") pelo administrador em Configurações > Assistente (IA). | D | RN-IA, ADR-0021, UC-15 |
| RF-SIS-17 | Tabelas de todas as telas com rolagem horizontal e ordenação por coluna. | E | RN-NAV-09 |
| RF-SIS-18 | Tela inicial com alertas do que precisa de atenção, abrindo a tela já filtrada. | I | RN-NAV-10, RN-OS-07, RN-FIN-07, UC-20 |
| RF-SIS-19 | Campos de data devem aceitar somente datas plausíveis (01/01/2000 até hoje + 10 anos), na tela e na camada de dados. | E | RN-SEG-09 |

### 3.2 Cadastros

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-CAD-01 | Manter, na mesma tela, clientes, fornecedores, transportadores e motoristas (papéis acumuláveis). | E | RN-CAD-01 |
| RF-CAD-02 | Validar CPF/CNPJ pelos dígitos verificadores. | E | RN-CAD-02 |
| RF-CAD-03 | Manter produtos (tipo, preços, estoque mínimo, dados fiscais e da reforma tributária), categorias, veículos, contas bancárias e usuários. | E | RN-CAD |
| RF-CAD-04 | Impedir exclusão de registros com histórico, oferecendo inativação. | E | RN-CAD-04 |

### 3.3 Vendas

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-VEN-01 | Registrar vendas no PDV pelo teclado (código de barras, multiplicador, busca, quantidade, desconto, cliente). | E | UC-03, RN-VEN |
| RF-VEN-02 | Receber em dinheiro (com troco), PIX, débito, crédito (parcelado) e a prazo. | E | RN-VEN-05/06 |
| RF-VEN-03 | Integrar TEF em duas fases (confirmar/desfazer) para cartão e PIX. | I | RN-TEF, UC-03 |
| RF-VEN-04 | Aplicar regras de estoque insuficiente, desconto máximo e limite de crédito. | E | RN-VEN-02/03/08 |
| RF-VEN-05 | Imprimir comprovante não fiscal e comprovantes TEF; abrir gaveta. | I | RN-VEN-11, RN-IMP |
| RF-VEN-06 | Manter orçamentos (aprovar, recusar, duplicar, imprimir) e pedidos de venda (faturar). | E | RN-PED, UC-04 |
| RF-VEN-07 | Consultar vendas por período, reimprimir comprovante e cancelar venda (administrador). | E | RN-VEN-10 |
| RF-VEN-08 | Manter ordens de serviço com etapas, prazo obrigatório, histórico, faturamento e impressão. | E | RN-OS, UC-16, UC-17 |

### 3.4 Compras e estoque

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-COM-01 | Manter pedidos de compra e receber mercadoria (com rateio de frete/desconto no custo). | E | RN-COM, RN-CUS-04 |
| RF-COM-02 | Importar XML de NF-e do fornecedor com vínculo de produtos, fator de conversão e duplicatas. | E | RN-XML, UC-05 |
| RF-EST-01 | Manter o saldo por movimentações com histórico, custo médio, situação e sugestão de compra. | E | RN-EST, RN-CUS |
| RF-EST-02 | Ajustar estoque (entrada, saída, inventário) com motivo. | E | RN-EST-05 |

### 3.5 Produção

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-PRO-01 | Manter fichas técnicas com rendimento, perdas e custo adicional, mostrando custo e margem estimados. | E | RN-PRO-01, UC-06 |
| RF-PRO-02 | Planejar, iniciar, concluir, cancelar e imprimir ordens de produção, com baixa de insumos e custo real. | E | RN-PRO, UC-06 |

### 3.6 Logística

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-LOG-01 | Manter fretes (rota, carga, valores, envolvidos) e o ciclo da viagem. | E | RN-TRA-01/06, UC-07 |
| RF-LOG-02 | Indicar a obrigatoriedade do CIOT e declarar, encerrar e cancelar o CIOT pela administradora configurada (foco Pamcard; simulador). | E | RN-TRA-02..05 |
| RF-LOG-03 | Gerar as contas a pagar do frete (adiantamento e saldo). | I | RN-TRA-07 |

### 3.7 Financeiro e cobrança

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-FIN-01 | Manter contas a pagar/receber (automáticas e manuais), com parcelamento, baixa, estorno e cancelamento. | E | RN-FIN |
| RF-FIN-02 | Emitir boletos para qualquer banco (modelo de campo livre configurável), visualizar e imprimir com código de barras. | E | RN-COB-01..06, UC-08 |
| RF-FIN-03 | Gerar remessa CNAB 240 e processar retorno, liquidando automaticamente as contas pagas. | E | RN-COB-07..09 |
| RF-FIN-04 | Apoiar a homologação bancária: boletos de teste, validador CNAB 240 e simulador de retorno. | I | RN-COB-10, UC-08 |
| RF-FIN-05 | Baixar várias contas de uma vez (tudo ou nada). | I | RN-FIN-05, UC-18 |
| RF-FIN-06 | Registrar contatos de cobrança e promessas de pagamento, com filtro e alerta de promessa vencida. | I | RN-FIN-06, RN-FIN-07, UC-19 |

### 3.8 Fiscal

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-FIS-01 | Montar notas de saída/entrada a partir de vendas/compras ou avulsas, com CFOP automático e tributos por item. | E | RN-FIS, UC-09 |
| RF-FIS-02 | Validar, numerar, gerar chave de acesso e DANFE; cancelar notas. | E | RN-FIS-05..08 |
| RF-FIS-03 | Calcular e destacar IBS, CBS e Imposto Seletivo conforme parâmetros da reforma tributária. | E | RN-REF |

### 3.9 Gestão

| ID | Requisito | Prior. | Origem |
|---|---|:---:|---|
| RF-GES-01 | Exibir painel gerencial com indicadores comparados ao período anterior e gráficos. | I | UC-10 |
| RF-GES-02 | Emitir relatórios tabulares com totais e exportação CSV. | I | UC-10 |

## 4. Requisitos de interface externa

| ID | Interface | Descrição | Situação |
|---|---|---|---|
| RI-01 | Resend (HTTPS/JSON) | Envio do código de recuperação de senha | Implementado (requer chave e domínio verificado) |
| RI-02 | Pamcard (HTTPS) | Declaração/encerramento/cancelamento de CIOT | Adaptador pronto; layout a confirmar na homologação com a Pamcard |
| RI-03 | Bancos (arquivos CNAB 240) | Remessa e retorno de cobrança | Implementado (padrão Febraban, configurável por banco) |
| RI-04 | Fornecedores (XML NF-e) | Importação de notas de entrada | Implementado |
| RI-05 | TEF (SiTef/PayGo) | Pagamento com cartão/PIX | Arquitetura + simulador; provedores reais não homologados |
| RI-06 | Impressoras | Windows (driver) e térmicas ESC/POS (RAW pelo spooler) | Implementado |
| RI-07 | SEFAZ | Autorização de NF-e/NFC-e | Fora do escopo desta versão |
| RI-08 | API da Anthropic (HTTPS/JSON, SDK oficial) | Assistente de dúvidas, provedor `CLAUDE`: API Messages com modelo `ia_modelo` (padrão `claude-opus-5-5`), até 4.000 tokens de resposta, esforço baixo, instruções + manual das telas em bloco de cache e modelo reserva automático do servidor em caso de recusa (`server-side-fallback`). Envia somente instruções, ajuda das telas, a tela atual e a conversa já mascarada; recebe só texto. Chave `ia_chave_api` cifrada (DPAPI). Erros tratados: chave ausente ou recusada (401), limite de uso da conta, erro da API, sem conexão, recusa do modelo, resposta sem texto | Implementado (requer chave da loja); desligado por padrão |
| RI-09 | Ollama (HTTP local, `POST /api/chat`) | Assistente de dúvidas, provedor `OLLAMA`: mesmo conteúdo da RI-08, enviado a um modelo instalado no computador (`ia_ollama_modelo`), sem streaming, tempo limite de 3 minutos. Endereço `ia_ollama_url` (padrão `http://localhost:11434`) aceito somente como `https://` ou `http://` local (loopback). Nada sai da máquina | Implementado (requer Ollama instalado e o modelo baixado) |
| RI-10 | App para celular (API HTTPS local no exe) | Proposta de [ADR-0022](adr/0022-app-celular-api-local.md) e [17](17-app-celular.md) | Não implementado (estudo) |

## 5. Requisitos não funcionais

| ID | Categoria | Requisito | Verificação |
|---|---|---|---|
| RNF-01 | Portabilidade | Executável único autocontido (.NET embutido), Windows 10/11 x64. | Publicação e teste do executável |
| RNF-02 | Confiabilidade | Operações compostas (venda, compra, faturamento, produção, importação, migração) atômicas (transação única). | Testes de regra + revisão |
| RNF-03 | Confiabilidade | Chaves estrangeiras ativas; migrações que reconstroem tabelas verificam integridade antes de confirmar. | Teste de migração v4→v11 |
| RNF-04 | Recuperabilidade | Backup automático antes de migrar e diário; restauração por cópia de arquivo. | Testes de migração/backup |
| RNF-05 | Segurança | Senhas e códigos só em hash PBKDF2-SHA256; recuperação limitada no tempo e em tentativas. | Testes de recuperação |
| RNF-06 | Segurança | Código publicado ofuscado (nomes e strings ilegíveis). | Inspeção da DLL publicada + teste de interface |
| RNF-07 | Usabilidade | Ajuda contextual em todas as telas; localizador de telas; atalhos de teclado; PDV operável sem mouse. | Teste "toda tela tem ajuda" |
| RNF-08 | Usabilidade | Retorno visual: cores por módulo, animações curtas (≤ 300 ms), avisos não bloqueantes. | Inspeção visual |
| RNF-09 | Desempenho | Geração de 6 meses de movimento de demonstração (≈1.300 vendas) em menos de 10 s; telas abrem em menos de 1 s com esse volume. | Bateria de testes (tempo registrado) |
| RNF-10 | Manutenibilidade | Alterações de banco somente por migrations; documentação de modelo de dados, manual e relatório de testes gerados automaticamente. | Revisão de código, testes |
| RNF-11 | Localização | Português do Brasil, moeda e datas no padrão brasileiro. | Inspeção |
| RNF-12 | Privacidade | O assistente não recebe dados do banco (nem nome da loja, clientes ou valores): o contexto é só a ajuda das telas, a lista de telas e atalhos, a tela atual e a conversa. CPF, CNPJ, números de cartão (13 a 19 dígitos), e-mails e telefones digitados são trocados por marcadores ([CPF], [CNPJ], [NÚMERO], [E-MAIL], [TELEFONE]) antes de sair do computador e antes de ir ao log. | Cenário `[Assistente]` (máscara, nenhum dado da loja na requisição) |
| RNF-13 | Segurança | O assistente não executa ações (sem ferramentas, só devolve texto exibido como texto simples); instruções de sistema mandam ignorar pedidos para mudar as regras ou revelar o texto (injeção de instruções); chave da API cifrada e nunca registrada no log. | Cenário `[Assistente]`; [15](15-seguranca-owasp.md) |
| RNF-14 | Consumo controlado | Pergunta de até 1.000 caracteres; histórico de até 10 mensagens anteriores; resposta de até 4.000 tokens; limite de `ia_limite_hora` perguntas por hora por usuário (padrão 30), contado pelo log. | Cenário `[Assistente]` (limite por hora) |
| RNF-15 | Disponibilidade | Falha do assistente (sem internet, chave recusada, limite da API, recusa do modelo, Ollama fechado) vira mensagem clara no painel e nunca derruba a tela nem o sistema; falha ao montar os alertas não impede abrir a tela inicial. | Cenário `[Assistente]` (chave recusada, recusa) |
| RNF-16 | Rastreabilidade | Perguntas e respostas do assistente (mascaradas), abertura e fechamento do painel, alertas clicados, ordenação de listas de itens, mudanças de etapa da OS e contatos de cobrança ficam registrados (log de atividades ou histórico da OS / da conta). | Cenários `[Assistente]`, `[Ordens de serviço]`, `[Financeiro]` |
| RNF-17 | Usabilidade / desempenho | Tabelas: cada coluna tem largura mínima (pelo título e pelo tipo de dado, entre 50 e 240 px) e a grade mostra rolagem horizontal em vez de espremer; ordenar pelo título é feito na memória, sem nova consulta ao banco; a ordem escolhida é reaplicada ao recarregar; números e datas ordenam pelo valor. | Cenário `[Tabelas]` (todas as telas) |
| RNF-18 | Usabilidade | Painel do assistente abre ao lado da tela atual sem substituí-la e sem marcá-la como alterada (não dispara o aviso de dados não salvos). | Cenário `[Assistente]` |

## 6. Matriz de rastreabilidade (resumo)

| Requisito | Regras | Caso de uso | Verificação (ver [08](08-relatorio-de-testes.md)) |
|---|---|---|---|
| RF-SIS-02/03 | RN-BD-01..03 | UC-01 | "legado v1", "banco v4 com dados", "banco novo", "banco lembrado" |
| RF-SIS-04 | RN-USU-06 | UC-02 | "código enviado", "código errado", "código expirado", "5 tentativas" |
| RF-SIS-06/07 | — | UC-14 | "toda tela aberta tem tópico", "o ? de cada tela abre só…", "buscar 'boleto'…" |
| RF-SIS-11 | RN-NAV | UC-14 | "a tela abre embutida…", "abrir outra tela substitui a atual (uma tela por vez, sem abas)", "[Sistema] …dados não salvos…", teste de interface ("embutida: PDV…") |
| RF-SIS-12 | RN-LOG | — | "log gravado em banco separado", "venda gera log de dados", "gravação desfeita…", "[Sistema] …log…" |
| RF-SIS-13 | RN-INS | UC-01 | "atalho .lnk do Windows criado" |
| RF-SIS-14/15 | RN-USU, RN-NAV | UC-02, UC-14 | "[Interface] primeiro acesso…", "[Interface] …continuar de onde parou…" |
| RF-SIS-16, RNF-12..15, RNF-18, RI-08 | RN-IA-01..04, RN-SEG-05 | UC-15 | Cenário `AssistenteDeDuvidas` (`Cenarios/AssistenteIA.cs`): "[Assistente] desligado: avisa como ligar e não chama a API", "…sem chave: orienta a configurar", "…chave da API gravada cifrada", "…CPF, e-mail e telefone mascarados antes de sair do computador", "…nenhum dado da loja vai na requisição (nome da loja, clientes)", "…instruções com o manual das telas e proteção contra injeção, em bloco de cache", "…modelo reserva em caso de recusa e esforço baixo…", "…recusa do modelo vira mensagem para reformular", "…chave recusada pela API: mensagem clara", "…limite de perguntas por hora por usuário", "…perguntas e respostas no log (já mascaradas)", "…Ctrl+I abre o assistente ao lado da tela (sem fechá-la)", "…conversar com o assistente não marca a tela como alterada", "…o assistente aparece no Buscar tela (Ctrl+K)" |
| RI-09 | RN-IA-01 | UC-15 | Validação do parâmetro `ia_ollama_url` (https ou http local); sem cenário automatizado com Ollama real |
| RF-SIS-17, RNF-17 | RN-NAV-09 | — | Cenário `InterfaceTabelasRolamEOrdenam` (`Cenarios/InterfaceTabelas.cs`): "[Tabelas] todas as tabelas têm rolagem horizontal e colunas com largura mínima", "…todas as colunas de texto das tabelas podem ser ordenadas (exceto itens do PDV)", "…tela estreita: … barra de rolagem horizontal", "…clicar no título ordena de A a Z; clicar de novo inverte…", "…a ordem escolhida continua depois de pesquisar e recarregar a lista", "…colunas de valor ordenam como número…", "…lista de itens também ordena pelo título…", "…o item selecionado continua selecionado depois de ordenar", "…no PDV a lista de itens não reordena…" |
| RF-SIS-18 | RN-NAV-10, RN-OS-07, RN-FIN-07 | UC-20 | "[Ordens de serviço] OS atrasada vira alerta grave da tela inicial", "…a tela inicial mostra o alerta das OS atrasadas", "…clicar no alerta abre as OS já filtradas nas atrasadas", "[Financeiro] promessa não cumprida vira alerta na tela inicial", "[Financeiro] alerta de contas vencidas abre o Financeiro filtrado nas vencidas a receber" |
| RF-SIS-19 | RN-SEG-09 | — | "[Ordens de serviço] prazo sugerido pelo parâmetro (dias) e campo de data só aceita datas plausíveis", "…datas implausíveis (ano 201, daqui a 30 anos) são recusadas", "[Financeiro] promessa com data passada: recusado" |
| RF-VEN-08 | RN-OS-01..06 | UC-16, UC-17 | Cenário `ServicosOrdemDeServicoDoPedidoAoFaturamento` (`Cenarios/ServicosEFinanceiroInsights.cs`): "[Ordens de serviço] sem descrever o serviço a OS não é aberta", "…OS aberta pela tela…", "…\"Avançar etapa\" passa para a próxima…", "…voltar para uma etapa anterior exige o motivo", "…histórico com cada mudança de etapa…", "…prazo de entrega antes da abertura é recusado", "…OS atrasada: filtro \"Atrasadas\", situação ATRASADA e dias de atraso", "…entregar grava a data da entrega…", "…faturar gera as contas a receber da OS…", "…faturar de novo: recusado…", "…mudar o valor da OS faturada: recusado…", "…cancelar OS faturada com contas abertas: recusado…", "…ficha da OS para imprimir", "…cancelar OS exige o motivo", "…indicador de entregas no prazo nos últimos 90 dias…" |
| RF-FIN-05 | RN-FIN-05, RN-FIN-03 | UC-18 | Cenário `FinanceiroBaixaEmLoteEAcompanhamentoDeCobranca`: "[Financeiro] baixa em lote com uma conta só orienta a usar \"Baixar\"", "…a janela mostra quantas contas e o total", "…as duas contas baixadas, cada uma pelo próprio valor, com a forma escolhida", "…lote com uma conta já paga: nada é baixado (tudo ou nada) e a mensagem diz qual conta" |
| RF-FIN-06 | RN-FIN-06, RN-FIN-07 | UC-19 | "[Financeiro] contato registrado pela tela: meio, conversa e data da promessa", "…a lista mostra o último contato e a promessa", "…promessa com data passada: recusado", "…contato numa conta já paga: recusado", "…contato sem dizer o que foi conversado: recusado", "…filtro \"Promessa vencida\" mostra quem prometeu e não pagou" |
| Cenários de uso | todas | todos | 79 cenários `[Área]` (ver [14](14-testes-exploratorios.md)); `ERP_CENARIO=<nome>` roda só os escolhidos |
| RF-CAD-01/02 | RN-CAD-01/02 | UC-11 | "clientes e fornecedores viraram pessoas", "validação de CPF/CNPJ" |
| RF-VEN-01..04 | RN-VEN, RN-TEF | UC-03 | "total da venda", "desconto acima de 10%", "crédito disponível", "TEF simulador" |
| RF-VEN-06 | RN-PED | UC-04 | "orçamento…", "aprovar orçamento…", "faturar…" |
| RF-COM-01/02 | RN-COM, RN-XML | UC-05 | "custo médio…", "XML lido…", "vínculo…", "fator 10…" |
| RF-PRO-01/02 | RN-PRO | UC-06 | "OP calcula a necessidade…", "concluir OP…" |
| RF-LOG-01..03 | RN-TRA | UC-07 | "CIOT obrigatório…", "Pamcard…", "contas do frete…" |
| RF-FIN-02..04 | RN-COB | UC-08 | "fator de vencimento…", "DV do código de barras…", "remessa CNAB 240…", "retorno…" |
| RF-FIS-01..03 | RN-FIS, RN-REF | UC-09 | "emissão…", "chave de acesso…", "IBS/CBS…" |
| RNF-06 | — | — | Inspeção da DLL publicada e teste de interface ([07](07-plano-de-testes.md#nível-4)) |
