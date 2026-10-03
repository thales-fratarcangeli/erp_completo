# 11 — Glossário

| Termo | Definição |
|---|---|
| **Acompanhamento de cobrança** | Registro, no Financeiro, de cada contato com o cliente sobre uma conta a receber em aberto: quando, como (telefone, WhatsApp, e-mail...), o que foi conversado, quem registrou e a promessa de pagamento, se houver (*Registrar contato* / *Ver contatos*) |
| **ADR** | *Architecture Decision Record*: registro curto de uma decisão de arquitetura, com contexto, decisão e consequências ([adr/](adr/)) |
| **Alerta da tela inicial** | Aviso, na tela inicial, do que precisa de atenção hoje (OS atrasadas, com entrega hoje, prontas ou entregues sem faturar; contas a receber vencidas; promessas não cumpridas; estoque baixo). Os graves vêm primeiro; clicar abre a tela que resolve, já filtrada |
| **ANTT** | Agência Nacional de Transportes Terrestres; regula o CIOT e o RNTRC |
| **API local (app proposto)** | Servidor HTTPS que o `ErpLoja.exe` passaria a hospedar, desligado por padrão, para o app do celular conversar com o exe (o celular nunca abre os arquivos `.db`). **Proposta, não implementada** ([17](17-app-celular.md), [ADR-0022](adr/0022-app-celular-api-local.md)) |
| **Assistente de IA** | Painel de dúvidas sobre o uso do sistema (*✦ Assistente* ou Ctrl+I) que responde só com base na ajuda das telas; não recebe dados da loja e não executa ações. Desligado por padrão ([RN-IA](04-regras-de-negocio.md), [ADR-0021](adr/0021-assistente-ia-somente-documentacao.md)) |
| **Baixa** | Registro de pagamento/recebimento de uma conta; também a instrução ao banco para cancelar um boleto |
| **Baixa em lote** | Baixa de várias contas de uma vez no Financeiro (*Baixar selecionadas*): só a receber ou só a pagar, todas em aberto, cada uma pelo próprio valor, com a mesma data e forma. Tudo ou nada: se uma falhar, nenhuma é baixada |
| **CBS** | Contribuição sobre Bens e Serviços (federal), criada pela reforma tributária (EC 132/2023, LC 214/2025) |
| **cClassTrib** | Código de classificação tributária do IBS/CBS (6 dígitos) |
| **CFOP** | Código Fiscal de Operações e Prestações; 5xxx dentro do estado, 6xxx fora (saídas); 1xxx/2xxx (entradas) |
| **Chave de acesso** | Identificador de 44 dígitos da NF-e/NFC-e, com DV módulo 11 |
| **Chave de API** | Credencial secreta que identifica a conta da loja num serviço externo (Resend, Anthropic). Mascarada na tela, gravada cifrada (DPAPI) e revogável no painel do serviço |
| **CIOT** | Código Identificador da Operação de Transporte; obrigatório no frete contratado de transportador autônomo (TAC) |
| **CNAB 240** | Leiaute Febraban de arquivos de 240 posições trocados com o banco (remessa e retorno) |
| **Campo livre** | 25 posições do código de barras do boleto definidas por cada banco (agência, conta, carteira, nosso número...) |
| **CPF/CNPJ** | Documentos de pessoa física/jurídica, validados pelos dígitos verificadores |
| **CSOSN / CST** | Códigos de situação tributária (Simples Nacional / regime normal) |
| **Custo médio ponderado** | (saldo × custo atual + entrada × custo da entrada) ÷ (saldo + entrada) |
| **DANFE** | Documento auxiliar da NF-e, a representação impressa da nota |
| **Desfazimento (TEF)** | Cancelamento automático de uma transação pendente quando a venda não é gravada |
| **DV** | Dígito verificador |
| **EAN / GTIN** | Código de barras do produto (8, 12, 13 ou 14 dígitos) |
| **ESC/POS** | Linguagem de comandos de impressoras térmicas (Epson e compatíveis) |
| **Etapa (OS)** | Fase da ordem de serviço: Aberta → Aprovada → Em execução → Pronta → Entregue (ou Cancelada, só pelo cancelamento com motivo). Avançar é livre; voltar exige motivo; toda mudança fica no histórico da OS |
| **Fator de vencimento** | 4 dígitos do código de barras com os dias desde a data-base; reiniciou em 1000 em 22/02/2025 |
| **Faturar** | Transformar um pedido de venda em venda (estoque, contas, nota) |
| **Faturar OS** | Gerar as contas a receber da ordem de serviço (parcelas mensais a partir do 1º vencimento). Independe da etapa, exige valor maior que zero e é feito uma única vez |
| **Ficha técnica** | Receita do produto produzido: insumos, quantidades, perdas, rendimento e custos adicionais |
| **FK** | *Foreign key*: chave estrangeira, garante que uma referência aponte para um registro existente |
| **Homologação** | Fase de testes com um parceiro (banco, administradora, provedor TEF) antes da produção |
| **IBS** | Imposto sobre Bens e Serviços (estadual + municipal), criado pela reforma tributária |
| **IS** | Imposto Seletivo, sobre bens e serviços prejudiciais à saúde/meio ambiente |
| **Limite por hora** | Número máximo de perguntas ao assistente por usuário na última hora (`ia_limite_hora`, padrão 30); controla o uso e o custo |
| **Linha digitável** | Representação numérica de 47 dígitos do código de barras do boleto |
| **LLM** | *Large Language Model*: modelo de linguagem de grande porte, o tipo de IA que entende e gera texto (ex.: Claude, Llama) |
| **Mascaramento** | Troca, antes de a pergunta sair do computador, de CPF, CNPJ, números de cartão, e-mails e telefones digitados por marcadores (`[CPF]`, `[CNPJ]`, `[NÚMERO]`, `[E-MAIL]`, `[TELEFONE]`) |
| **Migração (migration)** | Alteração versionada do esquema do banco, aplicada uma única vez e automaticamente |
| **Módulo 10 / 11** | Algoritmos de dígito verificador usados em boletos, chaves e documentos |
| **NCM** | Nomenclatura Comum do Mercosul, classificação fiscal do produto (8 dígitos) |
| **NF-e / NFC-e** | Nota fiscal eletrônica (modelo 55) / nota fiscal ao consumidor eletrônica (modelo 65) |
| **Nosso número** | Identificação do boleto no banco |
| **NSU** | Número sequencial único da transação TEF |
| **Ofuscação** | Transformação do código compilado que o torna difícil de entender sem alterar o funcionamento |
| **Ollama** | Programa gratuito que roda modelos de linguagem no próprio computador; usado pelo provedor OLLAMA do assistente (com endereço `localhost`, nada sai da máquina) |
| **OP** | Ordem de produção |
| **Ordem de serviço (OS)** | Trabalho feito sob encomenda para um cliente, com descrição, prazo obrigatório, valor, responsável, etapa e histórico (*Vendas > Ordens de serviço*) |
| **Pamcard** | Administradora de pagamento de frete e CIOT habilitada pela ANTT |
| **Pareamento (app proposto)** | Liberação de um celular para usar a API local: QR code com código de uso único (vale 5 min) e aprovação do aparelho pelo administrador. **Proposta, não implementada** ([17](17-app-celular.md)) |
| **PDV** | Ponto de venda (frente de caixa) |
| **Pessoa** | Cadastro único com papéis: cliente, fornecedor, transportador, motorista |
| **Prazo / situação do prazo (OS)** | Prazo: data prometida de entrega (sugerida como hoje + `os_prazo_dias`). Situação: No prazo, Vence hoje, Atrasada, Entregue, Entregue com atraso ou Cancelada |
| **Promessa de pagamento** | Data em que o cliente disse que vai pagar, registrada num contato de cobrança (de hoje em diante). Se a conta continua aberta depois dela, é uma promessa vencida (filtro *Promessa vencida* e alerta da tela inicial) |
| **Provedor (de IA)** | Serviço que executa o modelo do assistente: CLAUDE (API da Anthropic, pela internet, cobrança por uso) ou OLLAMA (modelo local) |
| **Remessa / Retorno** | Arquivo enviado ao banco com boletos / arquivo devolvido pelo banco com ocorrências |
| **Resend** | Serviço de e-mail transacional usado para a recuperação de senha |
| **RN** | Regra de negócio (identificadores em [04](04-regras-de-negocio.md)) |
| **RNTRC** | Registro Nacional de Transportadores Rodoviários de Cargas |
| **SEFAZ** | Secretaria da Fazenda, autoriza notas fiscais eletrônicas |
| **SQLite** | Banco de dados relacional em arquivo único |
| **TAC** | Transportador autônomo de cargas (pessoa física) |
| **TEF** | Transferência eletrônica de fundos: integração do PDV com a maquininha/adquirente |
| **UI Automation** | API do Windows para automatizar interfaces, usada no teste do executável |
