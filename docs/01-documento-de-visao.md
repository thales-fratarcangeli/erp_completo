# 01 — Documento de visão

| | |
|---|---|
| **Documento** | Visão do produto |
| **Sistema** | ERP Loja 1.2.0 |
| **Status** | Vigente |
| **Público** | Proprietário do produto, equipe de desenvolvimento, implantação e suporte |

## 1. Propósito

Este documento define **o problema que o ERP Loja resolve, para quem, e os limites do produto**.
Ele orienta as decisões de escopo; os detalhes estão na [especificação de requisitos](02-especificacao-de-requisitos.md).

## 2. Problema

| O problema de | gestão do dia a dia de lojas de varejo e pequenos comércios com produção e entrega próprias |
|---|---|
| afeta | proprietários, operadores de caixa, compradores, responsáveis pelo financeiro e pela logística |
| cujo impacto é | controles dispersos (planilhas, caderno, sistemas avulsos), estoque e custo errados, contas a pagar/receber sem visão, retrabalho na emissão de documentos e na cobrança |
| uma solução bem-sucedida | concentra vendas, compras, produção, estoque, financeiro, cobrança, logística e fiscal num único sistema de instalação simples, com regras consistentes e documentadas |

## 3. Posicionamento

Para **lojas de pequeno e médio porte** que **precisam controlar a operação sem infraestrutura de TI**, o ERP Loja é um
**sistema de gestão desktop autossuficiente** que **funciona copiando um único executável**, com banco local, sem servidor
e sem instalação de dependências. Diferente de ERPs em nuvem, **funciona sem internet** (exceto integrações externas) e
mantém os dados no próprio computador da loja.

## 4. Partes interessadas e usuários

| Parte | Papel | Interesses principais |
|---|---|---|
| Proprietário / gestor | Usuário administrador | Indicadores, lucro, controle de acesso, configurações |
| Operador de caixa | Usuário comum | PDV rápido pelo teclado, TEF, comprovante |
| Comprador / estoquista | Usuário comum | Pedidos de compra, importação de XML, estoque, inventário |
| Produção | Usuário comum | Fichas técnicas, ordens de produção, consumo de insumos |
| Logística | Usuário comum | Fretes, veículos, CIOT |
| Financeiro | Usuário comum/admin | Contas, cobrança bancária, conciliação de retorno |
| Contador (externo) | Consulta | Notas, tributos (inclusive reforma tributária), relatórios |
| Equipe de desenvolvimento | Mantenedores | Arquitetura simples, testes automatizados, migrations, documentação |

## 5. Escopo do produto (capacidades)

| # | Capacidade | Módulo |
|---|---|---|
| C1 | Frente de caixa (PDV) operada pelo teclado, com TEF e impressão térmica | Vendas |
| C2 | Orçamentos → pedidos de venda → faturamento | Vendas |
| C3 | Pedidos de compra, recebimento e importação do XML da NF-e do fornecedor | Compras |
| C4 | Fichas técnicas e ordens de produção com custo real | Produção |
| C5 | Fretes, veículos, motoristas e CIOT (Pamcard) | Logística |
| C6 | Estoque com custo médio, inventário e histórico | Estoque |
| C7 | Contas a pagar/receber, parcelamento, baixa e fluxo de caixa | Financeiro |
| C8 | Cobrança bancária genérica (boleto Febraban, CNAB 240) e homologação com o banco | Financeiro |
| C9 | Notas fiscais (NF-e/NFC-e) com tributos atuais e da reforma tributária (IBS/CBS/IS) | Fiscal |
| C10 | Painel gerencial e relatórios exportáveis | Gestão |
| C11 | Cadastro único de pessoas, produtos, categorias, veículos, contas bancárias e usuários | Cadastros |
| C12 | Configuração centralizada, usuários com recuperação de senha, backups, migrations, ajuda em cada tela | Sistema |

## 6. Fora do escopo (desta versão)

| Item | Motivo / situação |
|---|---|
| Transmissão de NF-e/NFC-e à SEFAZ | Requer certificado digital A1 e homologação; a nota é numerada e controlada no sistema, DANFE marcado "sem valor fiscal" |
| TEF real (SiTef, PayGo) | Requer cliente TEF do provedor e homologação; existe arquitetura e simulador |
| Integração Pamcard em produção | Estrutura pronta; o layout definitivo depende do manual técnico e credenciais do credenciamento |
| Uso simultâneo por vários caixas em rede | SQLite é banco de arquivo (ver [ADR-0002](adr/0002-sqlite-banco-local.md)) |
| Contabilidade, folha de pagamento, CRM, e-commerce | Fora do domínio desta versão |
| Multiempresa/multifilial | Um banco por empresa |

## 7. Restrições

- Plataforma: Windows 10/11 64 bits, aplicativo desktop.
- Distribuição: executável único, sem instalador e sem pré-requisitos.
- Dados: arquivo SQLite local; nenhuma dependência de servidor.
- Idioma e formatos: português do Brasil (moeda, datas, separadores).
- Proteção do código: executável publicado com ofuscação ([ADR-0010](adr/0010-ofuscacao.md)).

## 8. Premissas e dependências

| Premissa | Impacto se falsa |
|---|---|
| A loja tem um computador por caixa ou um único ponto de operação | Concorrência de escrita no SQLite |
| O administrador configura dados fiscais e parâmetros antes de emitir documentos | Notas sem chave de acesso; impostos zerados |
| Serviços externos (Resend, Pamcard, bancos) são contratados pela loja | Recuperação de senha, CIOT e cobrança indisponíveis (há simuladores) |
| Alíquotas da reforma tributária são confirmadas com o contador | Valores de IBS/CBS incorretos |

## 9. Critérios de qualidade do produto

| Atributo | Meta |
|---|---|
| Instalação | Copiar 1 arquivo; primeira abertura cria ou localiza o banco |
| Integridade | Operações compostas em transação única; chaves estrangeiras ativas; backup antes de migrar |
| Usabilidade | Ajuda em todas as telas (F1/?), localizador de telas, atalhos, sem telas redundantes |
| Verificabilidade | Bateria automatizada (235+ verificações) e teste de interface do executável publicado |
| Manutenibilidade | Regras em camada de dados, migrations versionadas, documentação gerada do código (manual, modelo de dados, relatório de testes) |
