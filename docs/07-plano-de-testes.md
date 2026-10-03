# 07 — Plano de testes

| | |
|---|---|
| **Documento** | Plano de testes (estrutura inspirada na ISO/IEC/IEEE 29119-3) |
| **Sistema** | ERP Loja 1.5.0 (banco v16) |
| **Resultado da última execução** | [08 — Relatório de testes](08-relatorio-de-testes.md) (gerado automaticamente): 1.015 verificações, 1.015 aprovadas |

## 1. Objetivo

Garantir, a cada alteração e antes de cada publicação, que:
1. as regras de negócio ([04](04-regras-de-negocio.md)) produzem os resultados esperados;
2. bancos de versões anteriores migram sem perda de dados;
3. as integrações (Resend, Pamcard, TEF, CNAB 240, XML, impressão, assistente de IA) montam e interpretam as mensagens corretamente, **sem acessar a rede**;
4. todas as telas abrem, têm ajuda própria, campos com formato, tabelas com rolagem e ordenação, e funcionam no **executável publicado e ofuscado**.

## 2. Escopo

| Dentro | Fora |
|---|---|
| Regras de negócio, migrações, integrações com transportes falsos (inclusive a API da Anthropic usada pelo assistente), abertura de todas as telas, tabelas de todas as telas, alertas da tela inicial, geração de documentação, executável ofuscado | Comunicação real com SEFAZ, Pamcard em produção, provedores TEF reais, bancos (dependem de credenciamento/homologação externos); chamadas reais à API da Anthropic; o provedor Ollama local (ver [riscos](#9-riscos-de-teste)) |

## 3. Níveis de teste

| Nível | O que cobre | Como | Quando |
|---|---|---|---|
| 1. Regras e cálculos | Totais, rateios, custo médio, parcelas, DV (CPF/CNPJ, EAN, chave NF-e, módulos 10/11), fator de vencimento, CNAB, ESC/POS, IBS/CBS | Bateria console, asserções sobre valores | Toda alteração |
| 2. Integração com o banco | Operações completas em banco real temporário (venda → estoque → contas → TEF; OP → insumos → custo; frete → CIOT → contas; boleto → remessa → retorno → baixa) | Bateria console sobre `loja.db` criado do zero | Toda alteração |
| 3. Migrações | Bancos legados reais: v1 (sem controle), v2, v4 com dados (unificação de pessoas), banco de versão futura; o legado v1 precisa chegar à versão atual (migrações 2 a 16) | Fixture `testes/ErpLoja.Testes/dados/banco_v1.db` + bancos montados pelo teste | Toda alteração no esquema |
| 3b. Cenários de uso | Pessoas operando as telas (PDV, pedidos, compras, financeiro, produção, fretes, notas, cadastros, configurações, navegação, ordens de serviço, assistente, tabelas): digitam, teclam, clicam e respondem janelas | `testes/ErpLoja.Testes/Cenarios/*.cs` ([ADR-0018](adr/0018-cenarios-de-uso-automatizados.md)), um banco novo por cenário (com ou sem dados de demonstração) | Toda alteração |
| 4. Interface do executável | Interface no executável publicado e ofuscado | UI Automation (`testes/interface/teste-interface.ps1`) | Toda publicação |

### Verificações gerais da bateria (níveis 1 a 3)

Além das regras, a bateria (`testes/ErpLoja.Testes/Program.cs`) faz verificações que valem para o sistema inteiro e
reprovam quem esquecer um passo ao criar uma tela (ver o checklist no [CONTRIBUTING](../CONTRIBUTING.md#tela-nova-checklist)):

| Verificação | Como funciona |
|---|---|
| **Todas as telas do sistema foram abertas no teste** | `Program.cs` tem uma lista explícita de telas (`var telas = new (string, Func<Form>)[] { ... }`); cada uma é aberta, fotografada e conferida. Depois, a bateria lista por reflexão todas as classes `Form` do `ErpLoja` (exceto `Dialogo` e `Toast`) e reprova se alguma não foi aberta (“faltou …”). **Toda tela nova precisa entrar nessa lista** — na 1.5.0 entrou `OrdensServicoForm` |
| Botão de ajuda visível | Cada tela da lista precisa mostrar o “?” (ou ser a própria ajuda) |
| Toda tela aberta tem tópico de ajuda | Chaves usadas por `Ajuda.Instalar` (via `Ui.Estilizar`) precisam existir em `Ajuda.Topicos`; os diálogos `new Dialogo(titulo, "chave", ...)` também |
| Campos com formato | Todo `TextBox` editável das telas abertas precisa ter formato (`Validacao.Caixa`/`Validacao.*`); o código-fonte também é varrido atrás de `new TextBox` sem formato |
| Documentos gerados | `docs/06`, `docs/08` e `docs/12` são regravados ao final da execução completa |

### Nível 4

Teste de fumaça da interface, executado sobre o `ErpLoja.exe` **publicado em Release (ofuscado)**, porque a ofuscação
renomeia tipos e pode quebrar código que depende de nomes em tempo de execução (ligação de grades, P/Invoke, JSON).

Pré-requisito: a pasta informada em `-Pasta` contém o `ErpLoja.exe` e um `loja.db` com dados (por exemplo,
`ErpLoja.exe --dados-demo <pasta>\loja.db`).

Roteiro automatizado:
1. Isola as preferências (`ERPLOJA_PREFERENCIAS` = `<pasta>\prefs`) e abre o exe da pasta.
2. Entra com admin/admin; se o sistema exigir a troca da senha padrão no primeiro acesso, define uma senha nova.
3. Abre o PDV pelo menu lateral e confere que ele abriu **embutido** na janela principal (reprova se abrir como janela solta).
4. Aciona os atalhos F5 (painel), Ctrl+N (notas), F6 (estoque) e F7 (financeiro).
5. Abre Orçamentos → Novo (editor modal com grade ligada a classe).
6. Ctrl+K, digita "produtos" e abre o resultado; F1 abre a ajuda da tela.
   - Caixas de mensagem informativas ("ERP Loja") que aparecerem são fechadas sozinhas (OK) e registradas, para o teste nunca travar.
   - Janela "Atenção" ou de erro do .NET reprova o roteiro.
7. Imprime `RESULTADO: OK` (ou a etapa que falhou), encerra o processo que ele mesmo abriu e sai com código 1 em caso de falha.

Comprovação complementar da ofuscação: inspecionar a DLL extraída do pacote e confirmar ausência de nomes de métodos e de
textos SQL legíveis.

### Cenários de uso (nível 3b)

Cada classe que implementa `ICenario` é executada automaticamente pela bateria, em ordem alfabética do nome da classe,
num banco novo (`ComDadosDemo` decide se recebe os dados de demonstração antes). A classe `Usuario`
(`Cenarios/Infra.cs`) simula a pessoa: `AbrirTela`, `Digitar`, `Teclar` (F2, F10, Esc...), `Clicar`, `QuandoAbrir<T>` e
`QuandoAbrirJanela("título", ...)` (o que fazer quando abrir uma janela modal). Mensagens não abrem caixa (ficam
registradas em `Mensagens`, via `Ui.Interceptar`); confirmações recebem a resposta configurada (`RespostaConfirmacao` ou
a fila `RespostasConfirmacao`); uma janela modal que o cenário não esperava é cancelada após 2 segundos e reprova o
cenário; janela agendada que não abriu também reprova (`ConferirJanelas`) — **nada trava esperando clique**, o que
permite rodar a bateria sem supervisão.

Os cenários das versões 1.2.0 a 1.4.0 foram escritos por agentes de teste exploratório trabalhando em paralelo, cada um
numa área, que corrigiram as falhas encontradas (registro em [10 — Falhas e resoluções](10-falhas-e-resolucoes.md) e
[14 — Testes exploratórios](14-testes-exploratorios.md)). A bateria tem hoje **79 cenários**.

#### Cenários novos da 1.5.0

| Arquivo / classe | Área no relatório | O que cobre |
|---|---|---|
| `AssistenteIA.cs` / `AssistenteDeDuvidas` | Assistente | Desligado por padrão (avisa como ligar e não chama a API); sem chave orienta a configurar; chave gravada cifrada; CPF, e-mail e telefone mascarados antes de sair do computador; tela atual enviada como contexto; modelo e chave configurados; instruções com o manual das telas e proteção contra injeção em bloco de cache; **nenhum dado da loja** (nome da loja, clientes) na requisição; modelo reserva e esforço baixo; recusa do modelo e chave recusada (401) viram mensagens claras; limite de perguntas por hora; perguntas no log já mascaradas; Ctrl+I abre o painel ao lado da tela sem fechá-la; conversar não marca a tela como alterada; o assistente aparece no Ctrl+K |
| `InterfaceTabelas.cs` / `InterfaceTabelasRolamEOrdenam` | Tabelas | Abre **todas as telas do menu** (exceto Configurações e Localizar tela) e reprova grade sem rolagem horizontal, coluna visível com largura mínima < 50 ou coluna de texto não ordenável (exceto o PDV); em Produtos: tela estreita mostra a barra horizontal sem espremer colunas, clicar no título ordena de A a Z e de novo inverte (com a seta), a ordem continua depois de pesquisar/recarregar, colunas de valor ordenam como número; lista de itens (`BindingList`) ordena por texto e número mantendo o item selecionado; itens do PDV não reordenam |
| `ServicosEFinanceiroInsights.cs` / `ServicosOrdemDeServicoDoPedidoAoFaturamento` | Ordens de serviço | Nova OS pela tela (prazo sugerido pelo parâmetro, data plausível, serviço obrigatório, várias linhas); avançar etapa; voltar etapa exige motivo; histórico; prazo antes da abertura recusado; OS atrasada com filtro, dias de atraso e **alerta grave da tela inicial**; clicar no alerta abre a tela já filtrada; entrega com atraso; faturamento em 3 parcelas para o cliente da OS; travas (faturar de novo, alterar valor, cancelar com contas abertas); ficha para imprimir; cancelamento com motivo; indicador de entregas no prazo |
| `ServicosEFinanceiroInsights.cs` / `FinanceiroBaixaEmLoteEAcompanhamentoDeCobranca` | Financeiro | Baixa em lote pela tela (exige duas ou mais contas, mostra quantidade e total, forma de pagamento) e **tudo ou nada** quando uma conta do lote já está paga; contato de cobrança pela tela com promessa; última conversa e promessa na lista; contatos inválidos recusados (promessa no passado, conta paga, sem resumo); promessa vencida no filtro e no alerta; alerta de contas vencidas abre o Financeiro filtrado |

Cenários anteriores que continuam valendo como regressão das versões 1.3.0 e 1.4.0: `ValidacaoCampos.cs`
(`ValidacaoFormatosDeCampos`), `SegurancaOwasp.cs` (`SegurancaOwaspTop10`) e `InterfaceRevisao.cs`
(`InterfaceRevisaoUsabilidade`).

#### Integrações externas sem rede

Toda integração com serviço externo expõe um ponto de troca do transporte HTTP (`TransporteParaTestes`, do tipo
`HttpMessageHandler`). O teste instala um transporte falso, que guarda a requisição e devolve uma resposta no formato
do serviço, e o remove no `finally`:

| Integração | Ponto de troca | Transporte falso |
|---|---|---|
| E-mail (Resend) | `Email.TransporteParaTestes` | `ResendFalso` (`Program.cs`), também usado em `SistemaConfiguracoes.cs` |
| CIOT (Pamcard) | `PamcardAdministradora.TransporteParaTestes` | transporte falso em `Program.cs` e `LogisticaCenarios.cs` |
| Assistente (API da Anthropic) | `ProvedorClaude.TransporteParaTestes` | `AnthropicFalsa` (`AssistenteIA.cs`) |

O provedor do assistente usa o SDK oficial da Anthropic; com o transporte falso, o cliente é criado com
`MaxRetries = 0`, e o teste confere o **corpo JSON** que o SDK montou (modelo, `system` com `cache_control`,
`fallbacks`, `output_config.effort`, mensagem mascarada) e o cabeçalho `x-api-key`. A resposta falsa pode simular
sucesso, recusa (`stop_reason = "refusal"`) ou erro HTTP (ex.: 401). Ao ler o corpo, compare valores já
desserializados (`GetValue<string>()`): o JSON escapa acentos, e procurar o texto cru falha (falha 52 em
[10](10-falhas-e-resolucoes.md)). TEF, retorno bancário e CIOT de homologação usam **simuladores** do próprio sistema.

#### Interface sem janela em primeiro plano

A bateria roda numa sessão interativa, mas não garante que suas janelas estejam em primeiro plano. Mensagens de mouse do
Windows (clique simulado por `SendMessage`/`PostMessage`) só têm efeito com a janela em primeiro plano, porque o Windows
confere a janela sob o ponteiro (falha 54). Por isso os cenários acionam os tratadores da própria classe — por exemplo,
o clique no título da coluna chama `OnColumnHeaderMouseClick` da `DataGridView` por reflexão, o mesmo caminho do clique
do mouse; as teclas vão por `ProcessCmdKey`/`OnKeyDown` (`Usuario.Teclar`).

#### Rodar só alguns cenários (desenvolvimento)

```powershell
$env:ERP_CENARIO = "Tabelas"          # parte do nome da classe, sem diferenciar maiúsculas
dotnet run --project testes/ErpLoja.Testes
Remove-Item Env:ERP_CENARIO
```

Com `ERP_CENARIO` definida, a bateria roda **só os cenários cujo nome de classe contém o texto** (não o nome do
arquivo) e pula as verificações gerais, as migrações e a abertura de todas as telas; **não regrava** `docs/06`,
`docs/08` nem `docs/12`. O código de saída continua sendo o número de falhas. Serve para iterar num cenário
(a bateria inteira leva cerca de 10 minutos); **não substitui** a execução completa antes do commit.

## 4. Ambiente

| Item | Valor |
|---|---|
| SO | Windows 10/11 x64 com sessão interativa (os testes abrem janelas; não precisam estar em primeiro plano) |
| SDK | .NET 10 |
| Ferramentas | `dotnet tool restore` (Obfuscar), Windows PowerShell 5.1 para o teste de interface |
| Dados | Bancos temporários em `%TEMP%\erp_testes_<guid>` (um por cenário em `cenarios\<Classe>\loja.db`); preferências isoladas; nada é gravado no banco da loja |
| Rede | Não é necessária: nenhum teste acessa a internet |
| Serviços externos | Substituídos: transporte HTTP falso (Resend, Pamcard, API da Anthropic), simuladores (TEF, retorno bancário, CIOT) |
| Variáveis | `ERP_CENARIO` (opcional, filtro de cenários); `ERPLOJA_PREFERENCIAS` (definida pelo teste de interface) |

## 5. Técnicas de projeto de casos

| Técnica | Aplicação |
|---|---|
| Valor-limite | Desconto no limite do parâmetro; parcelas com centavos (33,33/33,33/33,34); fator de vencimento na virada de 22/02/2025; 5ª tentativa do código de recuperação; limite de perguntas por hora do assistente; datas plausíveis (01/01/2000 a hoje + 10 anos); largura mínima das colunas |
| Partição de equivalência | Vendedor comum × administrador; pessoa física × jurídica (CIOT obrigatório só para autônomo contratado); lote com uma conta × várias contas |
| Tabela de decisão | Venda sem estoque (perguntar/permitir/bloquear); produção sem insumo; obrigatoriedade do CIOT; resposta do assistente (desligado, sem chave, sucesso, recusa, chave recusada) |
| Transição de estados | Orçamento → pedido → faturado/cancelado; OP aberta → em produção → concluída; OS Aberta → Aprovada → Em execução → Pronta → Entregue (voltar exige motivo); frete; boleto; TEF pendente → confirmada/desfeita |
| Exemplos de referência | DV da chave de acesso (manual da NF-e); código de barras e linha digitável (exemplo Febraban) |
| Teste de propriedade | Dados de demonstração: CPF/CNPJ válidos, FKs íntegras, insumos nunca negativos; todas as tabelas rolam e ordenam; nenhum dado da loja na requisição do assistente |
| Atomicidade | Baixa em lote: com uma conta inválida, nenhuma é baixada |

## 6. Rastreabilidade

A matriz requisito × regra × teste está na [especificação de requisitos](02-especificacao-de-requisitos.md); cada linha do
[relatório de testes](08-relatorio-de-testes.md) descreve a regra verificada em linguagem de negócio, com a área do
cenário entre colchetes (ex.: `[Ordens de serviço]`, `[Assistente]`, `[Tabelas]`).

## 7. Critérios

| Critério | Definição |
|---|---|
| Entrada | Compilação sem erros nem avisos de segurança de pacotes |
| Aprovação | 100% das verificações aprovadas na **execução completa** (sem `ERP_CENARIO`, código de saída 0) **e** `RESULTADO: OK` no teste de interface do executável publicado |
| Cobertura de telas | Toda classe `Form` do sistema aberta pela lista de telas do `Program.cs`, com “?” e tópico de ajuda; toda tela do menu com tabelas que rolam e ordenam; todo campo de texto com formato |
| Integrações | Nenhuma verificação depende de rede; toda integração nova tem transporte falso ou simulador e cobre sucesso e falha |
| Suspensão | Falha de ambiente (sem sessão interativa, executável em uso) — corrigir e repetir |
| Regressão | Toda falha corrigida ganha verificação na bateria |

## 8. Execução

```powershell
dotnet run --project testes/ErpLoja.Testes
dotnet publish ErpLoja/ErpLoja.csproj -c Release -o <pasta>
<pasta>\ErpLoja.exe --dados-demo <pasta>\loja.db
powershell -ExecutionPolicy Bypass -File testes/interface/teste-interface.ps1 -Pasta <pasta>
```

A bateria completa regenera `docs/06-modelo-de-dados.md`, `docs/08-relatorio-de-testes.md` e
`docs/12-manual-do-usuario.md`; diferenças nesses arquivos no `git diff` indicam mudança de esquema, de testes ou de
ajuda. Para rodar só um cenário, ver [Rodar só alguns cenários](#rodar-só-alguns-cenários-desenvolvimento).

## 9. Riscos de teste

| Risco | Mitigação |
|---|---|
| Integrações reais diferentes dos simuladores | Homologação com cada parceiro antes de produção (CNAB com o banco, CIOT com a Pamcard, TEF com o provedor) |
| API da Anthropic mudar o formato ou o SDK mudar a requisição | O teste confere o corpo montado pelo SDK, não um JSON escrito à mão; botão "Testar assistente" em Configurações para conferir com a chave real |
| Provedor Ollama sem teste automatizado | Ainda não tem transporte substituível; testar manualmente com o "Testar assistente" |
| Clique simulado sem efeito com a janela fora de primeiro plano | Acionar o tratador da própria grade/tela (ver [Interface sem janela em primeiro plano](#interface-sem-janela-em-primeiro-plano)) |
| Ofuscação quebrar algo não coberto pelo roteiro de interface | Regras de exclusão documentadas ([ADR-0010](adr/0010-ofuscacao.md)); bateria cobre a camada de dados; `-p:Ofuscar=false` para isolar |
| Diferenças de impressoras | Página de teste na tela de configurações; tipo WINDOWS como alternativa ao ESC/POS |
