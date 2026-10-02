# 11 — Glossário

| Termo | Definição |
|---|---|
| **ADR** | *Architecture Decision Record*: registro curto de uma decisão de arquitetura, com contexto, decisão e consequências ([adr/](adr/)) |
| **ANTT** | Agência Nacional de Transportes Terrestres; regula o CIOT e o RNTRC |
| **Baixa** | Registro de pagamento/recebimento de uma conta; também a instrução ao banco para cancelar um boleto |
| **CBS** | Contribuição sobre Bens e Serviços (federal), criada pela reforma tributária (EC 132/2023, LC 214/2025) |
| **cClassTrib** | Código de classificação tributária do IBS/CBS (6 dígitos) |
| **CFOP** | Código Fiscal de Operações e Prestações; 5xxx dentro do estado, 6xxx fora (saídas); 1xxx/2xxx (entradas) |
| **Chave de acesso** | Identificador de 44 dígitos da NF-e/NFC-e, com DV módulo 11 |
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
| **Fator de vencimento** | 4 dígitos do código de barras com os dias desde a data-base; reiniciou em 1000 em 22/02/2025 |
| **Faturar** | Transformar um pedido de venda em venda (estoque, contas, nota) |
| **Ficha técnica** | Receita do produto produzido: insumos, quantidades, perdas, rendimento e custos adicionais |
| **FK** | *Foreign key*: chave estrangeira, garante que uma referência aponte para um registro existente |
| **Homologação** | Fase de testes com um parceiro (banco, administradora, provedor TEF) antes da produção |
| **IBS** | Imposto sobre Bens e Serviços (estadual + municipal), criado pela reforma tributária |
| **IS** | Imposto Seletivo, sobre bens e serviços prejudiciais à saúde/meio ambiente |
| **Linha digitável** | Representação numérica de 47 dígitos do código de barras do boleto |
| **Migração (migration)** | Alteração versionada do esquema do banco, aplicada uma única vez e automaticamente |
| **Módulo 10 / 11** | Algoritmos de dígito verificador usados em boletos, chaves e documentos |
| **NCM** | Nomenclatura Comum do Mercosul, classificação fiscal do produto (8 dígitos) |
| **NF-e / NFC-e** | Nota fiscal eletrônica (modelo 55) / nota fiscal ao consumidor eletrônica (modelo 65) |
| **Nosso número** | Identificação do boleto no banco |
| **NSU** | Número sequencial único da transação TEF |
| **Ofuscação** | Transformação do código compilado que o torna difícil de entender sem alterar o funcionamento |
| **OP** | Ordem de produção |
| **Pamcard** | Administradora de pagamento de frete e CIOT habilitada pela ANTT |
| **PDV** | Ponto de venda (frente de caixa) |
| **Pessoa** | Cadastro único com papéis: cliente, fornecedor, transportador, motorista |
| **Remessa / Retorno** | Arquivo enviado ao banco com boletos / arquivo devolvido pelo banco com ocorrências |
| **Resend** | Serviço de e-mail transacional usado para a recuperação de senha |
| **RN** | Regra de negócio (identificadores em [04](04-regras-de-negocio.md)) |
| **RNTRC** | Registro Nacional de Transportadores Rodoviários de Cargas |
| **SEFAZ** | Secretaria da Fazenda, autoriza notas fiscais eletrônicas |
| **SQLite** | Banco de dados relacional em arquivo único |
| **TAC** | Transportador autônomo de cargas (pessoa física) |
| **TEF** | Transferência eletrônica de fundos: integração do PDV com a maquininha/adquirente |
| **UI Automation** | API do Windows para automatizar interfaces, usada no teste do executável |
