# 01 — Documento de visão

| | |
|---|---|
| **Documento** | Visão do produto |
| **Sistema** | ERP Loja 1.5.0 (banco v16) |
| **Status** | Vigente |
| **Público** | Proprietário do produto, equipe de desenvolvimento, implantação e suporte |

## 1. Propósito

Este documento define **o problema que o ERP Loja resolve, para quem, e os limites do produto**.
Ele orienta as decisões de escopo; os detalhes estão na [especificação de requisitos](02-especificacao-de-requisitos.md).

## 2. Problema

| O problema de | gestão do dia a dia de lojas de varejo e pequenos comércios com produção e entrega próprias |
|---|---|
| afeta | proprietários, operadores de caixa, compradores, responsáveis pelo financeiro e pela logística |
| cujo impacto é | controles dispersos (planilhas, caderno, sistemas avulsos), estoque e custo errados, contas a pagar/receber sem visão, serviços sob encomenda entregues fora do prazo sem que ninguém perceba, clientes cobrados sem registro do que foi combinado, retrabalho na emissão de documentos e na cobrança |
| uma solução bem-sucedida | concentra vendas, ordens de serviço, compras, produção, estoque, financeiro, cobrança, logística e fiscal num único sistema de instalação simples, com regras consistentes e documentadas, que mostra logo na abertura o que precisa de atenção no dia |

## 3. Posicionamento

Para **lojas de pequeno e médio porte** que **precisam controlar a operação sem infraestrutura de TI**, o ERP Loja é um
**sistema de gestão desktop autossuficiente** que **funciona copiando um único executável**, com banco local, sem servidor
e sem instalação de dependências. Diferente de ERPs em nuvem, **funciona sem internet** (exceto integrações externas) e
mantém os dados no próprio computador da loja.

A partir da 1.5.0, o sistema também **orienta o uso do dia a dia**: a tela inicial lista os alertas do que precisa de
atenção (OS atrasadas, contas vencidas, promessas de pagamento não cumpridas, estoque baixo) e um **assistente de
dúvidas por IA**, opcional e desligado por padrão, explica como usar cada tela — sem receber dados da loja
([ADR-0021](adr/0021-assistente-ia-somente-documentacao.md)). As ordens de serviço e o acompanhamento de cobrança
vieram da análise de outro ERP em uso numa gráfica ([16](16-analise-erp-grafica.md)).

## 4. Partes interessadas e usuários

| Parte | Papel | Interesses principais |
|---|---|---|
| Proprietário / gestor | Usuário administrador | Indicadores, lucro, controle de acesso, configurações |
| Operador de caixa | Usuário comum | PDV rápido pelo teclado, TEF, comprovante |
| Atendimento / serviços | Usuário comum | Ordens de serviço com prazo, etapas, entrega e faturamento |
| Comprador / estoquista | Usuário comum | Pedidos de compra, importação de XML, estoque, inventário |
| Produção | Usuário comum | Fichas técnicas, ordens de produção, consumo de insumos |
| Logística | Usuário comum | Fretes, veículos, CIOT |
| Financeiro | Usuário comum/admin | Contas, baixa em lote, acompanhamento de cobrança (contatos e promessas), cobrança bancária, conciliação de retorno |
| Todos os usuários | Usuário comum/admin | Alertas da tela inicial; tirar dúvidas sobre o uso com o assistente (quando ligado) |
| Contador (externo) | Consulta | Notas, tributos (inclusive reforma tributária), relatórios |
| Equipe de desenvolvimento | Mantenedores | Arquitetura simples, testes automatizados, migrations, documentação |

## 5. Escopo do produto (capacidades)

| # | Capacidade | Módulo |
|---|---|---|
| C1 | Frente de caixa (PDV) operada pelo teclado, com TEF e impressão térmica | Vendas |
| C2 | Orçamentos → pedidos de venda → faturamento | Vendas |
| C2a | Ordens de serviço: etapas (Aberta → Aprovada → Em execução → Pronta → Entregue), prazo obrigatório, situação do prazo, histórico, faturamento em contas a receber, ficha impressa e indicador de entregas no prazo | Vendas |
| C3 | Pedidos de compra, recebimento e importação do XML da NF-e do fornecedor | Compras |
| C4 | Fichas técnicas e ordens de produção com custo real | Produção |
| C5 | Fretes, veículos, motoristas e CIOT (Pamcard) | Logística |
| C6 | Estoque com custo médio, inventário e histórico | Estoque |
| C7 | Contas a pagar/receber, parcelamento, baixa (individual ou em lote, tudo ou nada) e fluxo de caixa | Financeiro |
| C7a | Acompanhamento de cobrança: contatos com o cliente, promessas de pagamento e filtro de promessa vencida | Financeiro |
| C8 | Cobrança bancária genérica (boleto Febraban, CNAB 240) e homologação com o banco | Financeiro |
| C9 | Notas fiscais (NF-e/NFC-e) com tributos atuais e da reforma tributária (IBS/CBS/IS) | Fiscal |
| C10 | Painel gerencial e relatórios exportáveis | Gestão |
| C11 | Cadastro único de pessoas, produtos, categorias, veículos, contas bancárias e usuários | Cadastros |
| C12 | Configuração centralizada, usuários com recuperação de senha, backups, migrations, ajuda em cada tela | Sistema |
| C13 | Tela inicial com alertas do que precisa de atenção (OS, contas vencidas, promessas, estoque baixo), cada um abrindo a tela já filtrada | Sistema |
| C14 | Assistente de dúvidas por IA (Claude pela API da Anthropic ou modelo local via Ollama), restrito à ajuda do sistema, opcional e desligado por padrão | Sistema |
| C15 | Tabelas de todas as telas com rolagem horizontal e ordenação por coluna; campos de data só com datas plausíveis | Todos |

## 6. Fora do escopo (desta versão)

| Item | Motivo / situação |
|---|---|
| Transmissão de NF-e/NFC-e à SEFAZ | Requer certificado digital A1 e homologação; a nota é numerada e controlada no sistema, DANFE marcado "sem valor fiscal" |
| TEF real (SiTef, PayGo) | Requer cliente TEF do provedor e homologação; existe arquitetura e simulador |
| Integração Pamcard em produção | Estrutura pronta; o layout definitivo depende do manual técnico e credenciais do credenciamento |
| Uso simultâneo por vários caixas em rede | SQLite é banco de arquivo (ver [ADR-0002](adr/0002-sqlite-banco-local.md)) |
| Contabilidade, folha de pagamento, CRM, e-commerce | Fora do domínio desta versão |
| Multiempresa/multifilial | Um banco por empresa |
| App para celular | Apenas estudo e proposta ([17](17-app-celular.md), [ADR-0022](adr/0022-app-celular-api-local.md)): API HTTPS local no exe, app pareado por QR code. Evolução futura; nada implementado nesta versão |
| Assistente que consulta dados ou executa ações | Por decisão de segurança o assistente só conhece a ajuda das telas e só devolve texto ([ADR-0021](adr/0021-assistente-ia-somente-documentacao.md)) |
| Migração de dados de outro ERP (outro ERP) | Mapeada como habilitador no roteiro de [16](16-analise-erp-grafica.md); não implementada |

## 7. Restrições

- Plataforma: Windows 10/11 64 bits, aplicativo desktop.
- Distribuição: executável único, sem instalador e sem pré-requisitos.
- Dados: arquivos SQLite locais (`loja.db` e o log `loja_log.db`); nenhuma dependência de servidor.
- Internet: só para integrações opcionais (Resend, Pamcard, assistente com o provedor Claude). Com o provedor Ollama o assistente funciona sem internet.
- Idioma e formatos: português do Brasil (moeda, datas, separadores).
- Proteção do código: executável publicado com ofuscação ([ADR-0010](adr/0010-ofuscacao.md)).

## 8. Premissas e dependências

| Premissa | Impacto se falsa |
|---|---|
| A loja tem um computador por caixa ou um único ponto de operação | Concorrência de escrita no SQLite |
| O administrador configura dados fiscais e parâmetros antes de emitir documentos | Notas sem chave de acesso; impostos zerados |
| Serviços externos (Resend, Pamcard, bancos) são contratados pela loja | Recuperação de senha, CIOT e cobrança indisponíveis (há simuladores) |
| Alíquotas da reforma tributária são confirmadas com o contador | Valores de IBS/CBS incorretos |
| Para usar o assistente, a loja contrata a API da Anthropic (chave própria) ou instala o Ollama com um modelo | Assistente indisponível; o restante do sistema não é afetado (ajuda "?" continua) |
| Toda OS recebe prazo realista e as etapas são atualizadas pela equipe | Alertas e indicador de entregas no prazo deixam de refletir a realidade |

## 9. Critérios de qualidade do produto

| Atributo | Meta |
|---|---|
| Instalação | Copiar 1 arquivo; primeira abertura cria ou localiza o banco |
| Integridade | Operações compostas em transação única; chaves estrangeiras ativas; backup antes de migrar |
| Usabilidade | Ajuda em todas as telas (F1/?), localizador de telas, atalhos, sem telas redundantes, alertas na tela inicial, tabelas ordenáveis com rolagem horizontal, assistente de dúvidas opcional |
| Privacidade | O assistente não recebe dados do banco; CPF, CNPJ, cartão, e-mail e telefone digitados são mascarados antes do envio |
| Verificabilidade | Bateria automatizada (1.000+ verificações, 79 cenários de uso) e teste de interface do executável publicado |
| Manutenibilidade | Regras em camada de dados, migrations versionadas, documentação gerada do código (manual, modelo de dados, relatório de testes) |
