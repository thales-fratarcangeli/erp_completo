# 02 — Especificação de requisitos de software (ERS)

| | |
|---|---|
| **Documento** | Especificação de requisitos (estrutura inspirada na ISO/IEC/IEEE 29148) |
| **Sistema** | ERP Loja 1.2.0 (banco v12) |
| **Status** | Vigente |
| **Convenções** | RF = requisito funcional; RNF = não funcional; RI = interface externa; prioridade **E** (essencial), **I** (importante), **D** (desejável); RN = regra de negócio ([04](04-regras-de-negocio.md)); UC = caso de uso ([03](03-casos-de-uso.md)) |

## 1. Introdução

### 1.1 Propósito
Especificar o comportamento observável do ERP Loja para orientar desenvolvimento, testes, implantação e evolução.

### 1.2 Escopo
Conforme o [documento de visão](01-documento-de-visao.md), seção 5. Itens fora do escopo na seção 6 da visão.

### 1.3 Referências
[Regras de negócio](04-regras-de-negocio.md) · [Arquitetura](05-arquitetura.md) · [Modelo de dados](06-modelo-de-dados.md) ·
[Plano de testes](07-plano-de-testes.md) · Leiaute Febraban CNAB 240 · Manual de orientação do contribuinte NF-e · EC 132/2023 e LC 214/2025.

## 2. Descrição geral

- **Perspectiva**: aplicação desktop autônoma (Windows Forms, .NET 10) com banco SQLite em arquivo; integrações opcionais por HTTPS (Resend, Pamcard) e por arquivos (XML de NF-e, CNAB 240) ou bibliotecas locais (TEF, impressoras).
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
| RF-SIS-11 | As telas do menu devem abrir no espaço à direita do menu, em abas, sem janelas soltas. | E | RN-NAV-01..03 |
| RF-SIS-12 | Toda ação deve gerar registro de log em banco SQLite separado, consultável por administradores. | E | RN-LOG |
| RF-SIS-13 | Na primeira execução, deve criar um atalho na área de trabalho. | I | RN-INS |
| RF-SIS-14 | O primeiro acesso com a senha padrão deve exigir a troca da senha. | E | RN-USU |
| RF-SIS-15 | A tela inicial e o localizador devem oferecer as telas usadas por último. | D | RN-NAV |
| RF-SIS-16 | Assistente de dúvidas por IA, restrito à ajuda do sistema, sem dados da loja e com máscara de dados pessoais. | D | RN-IA, ADR-0021 |
| RF-SIS-17 | Tabelas de todas as telas com rolagem horizontal e ordenação por coluna. | E | RN-NAV-09 |
| RF-SIS-18 | Tela inicial com alertas do que precisa de atenção, abrindo a tela já filtrada. | I | RN-NAV-10, RN-OS-07, RN-FIN-07 |

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
| RF-VEN-08 | Manter ordens de serviço com etapas, prazo obrigatório, histórico, faturamento e impressão. | E | RN-OS |

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
| RF-FIN-05 | Baixar várias contas de uma vez (tudo ou nada). | I | RN-FIN-05 |
| RF-FIN-06 | Registrar contatos de cobrança e promessas de pagamento, com filtro e alerta de promessa vencida. | I | RN-FIN-06, RN-FIN-07 |

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

## 6. Matriz de rastreabilidade (resumo)

| Requisito | Regras | Caso de uso | Verificação (ver [08](08-relatorio-de-testes.md)) |
|---|---|---|---|
| RF-SIS-02/03 | RN-BD-01..03 | UC-01 | "legado v1", "banco v4 com dados", "banco novo", "banco lembrado" |
| RF-SIS-04 | RN-USU-06 | UC-02 | "código enviado", "código errado", "código expirado", "5 tentativas" |
| RF-SIS-06/07 | — | UC-14 | "toda tela aberta tem tópico", "o ? de cada tela abre só…", "buscar 'boleto'…" |
| RF-SIS-11 | RN-NAV | UC-14 | "a tela abre embutida…", "[Sistema] …abas…", teste de interface ("embutida: PDV…") |
| RF-SIS-12 | RN-LOG | — | "log gravado em banco separado", "venda gera log de dados", "gravação desfeita…", "[Sistema] …log…" |
| RF-SIS-13 | RN-INS | UC-01 | "atalho .lnk do Windows criado" |
| RF-SIS-14/15 | RN-USU, RN-NAV | UC-02, UC-14 | "[Interface] primeiro acesso…", "[Interface] …continuar de onde parou…" |
| Cenários de uso | todas | todos | 69 cenários `[Área]` (ver [14](14-testes-exploratorios.md)) |
| RF-CAD-01/02 | RN-CAD-01/02 | UC-11 | "clientes e fornecedores viraram pessoas", "validação de CPF/CNPJ" |
| RF-VEN-01..04 | RN-VEN, RN-TEF | UC-03 | "total da venda", "desconto acima de 10%", "crédito disponível", "TEF simulador" |
| RF-VEN-06 | RN-PED | UC-04 | "orçamento…", "aprovar orçamento…", "faturar…" |
| RF-COM-01/02 | RN-COM, RN-XML | UC-05 | "custo médio…", "XML lido…", "vínculo…", "fator 10…" |
| RF-PRO-01/02 | RN-PRO | UC-06 | "OP calcula a necessidade…", "concluir OP…" |
| RF-LOG-01..03 | RN-TRA | UC-07 | "CIOT obrigatório…", "Pamcard…", "contas do frete…" |
| RF-FIN-02..04 | RN-COB | UC-08 | "fator de vencimento…", "DV do código de barras…", "remessa CNAB 240…", "retorno…" |
| RF-FIS-01..03 | RN-FIS, RN-REF | UC-09 | "emissão…", "chave de acesso…", "IBS/CBS…" |
| RNF-06 | — | — | Inspeção da DLL publicada e teste de interface ([07](07-plano-de-testes.md#nível-4)) |
