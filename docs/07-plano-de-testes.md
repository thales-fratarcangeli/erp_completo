# 07 — Plano de testes

| | |
|---|---|
| **Documento** | Plano de testes (estrutura inspirada na ISO/IEC/IEEE 29119-3) |
| **Sistema** | ERP Loja 1.2.0 (banco v12) |
| **Resultado da última execução** | [08 — Relatório de testes](08-relatorio-de-testes.md) (gerado automaticamente) |

## 1. Objetivo

Garantir, a cada alteração e antes de cada publicação, que:
1. as regras de negócio ([04](04-regras-de-negocio.md)) produzem os resultados esperados;
2. bancos de versões anteriores migram sem perda de dados;
3. as integrações (Resend, Pamcard, TEF, CNAB 240, XML, impressão) montam e interpretam as mensagens corretamente;
4. todas as telas abrem, têm ajuda própria e funcionam no **executável publicado e ofuscado**.

## 2. Escopo

| Dentro | Fora |
|---|---|
| Regras de negócio, migrações, integrações com transportes falsos, abertura de todas as telas, geração de documentação, executável ofuscado | Comunicação real com SEFAZ, Pamcard em produção, provedores TEF reais, bancos (dependem de credenciamento/homologação externos) |

## 3. Níveis de teste

| Nível | O que cobre | Como | Quando |
|---|---|---|---|
| 1. Regras e cálculos | Totais, rateios, custo médio, parcelas, DV (CPF/CNPJ, EAN, chave NF-e, módulos 10/11), fator de vencimento, CNAB, ESC/POS, IBS/CBS | Bateria console, asserções sobre valores | Toda alteração |
| 2. Integração com o banco | Operações completas em banco real temporário (venda → estoque → contas → TEF; OP → insumos → custo; frete → CIOT → contas; boleto → remessa → retorno → baixa) | Bateria console sobre `loja.db` criado do zero | Toda alteração |
| 3b. Cenários de uso | Pessoas operando as telas (PDV, pedidos, compras, financeiro, produção, fretes, notas, cadastros, configurações, navegação): digitam, teclam, clicam e respondem janelas | `testes/ErpLoja.Testes/Cenarios/*.cs` ([ADR-0018](adr/0018-cenarios-de-uso-automatizados.md)), um banco com dados de demonstração por cenário | Toda alteração |
| 3. Migrações | Bancos legados reais: v1 (sem controle), v2, v4 com dados (unificação de pessoas), banco de versão futura | Fixture `testes/ErpLoja.Testes/dados/banco_v1.db` + bancos montados pelo teste | Toda alteração no esquema |
| Nível 4 | Interface no executável publicado e ofuscado | UI Automation (`testes/interface/teste-interface.ps1`) | Toda publicação |

### Nível 4

Teste de fumaça da interface, executado sobre o `ErpLoja.exe` **publicado em Release (ofuscado)**, porque a ofuscação
renomeia tipos e pode quebrar código que depende de nomes em tempo de execução (ligação de grades, P/Invoke, JSON).

Roteiro automatizado:
1. Cria uma pasta temporária com o exe e um banco (`--criar-banco`, `--dados-demo`) e isola as preferências (`ERPLOJA_PREFERENCIAS`).
2. Entra com admin/admin.
3. Abre o PDV pelo menu, aciona F5 (painel), Ctrl+N, F6, F7.
4. Abre Orçamentos → Novo (diálogo modal).
5. Ctrl+K, digita "produtos" e abre o resultado.
6. F1 abre a ajuda da tela atual.
   - Caixas de mensagem informativas que aparecerem são fechadas sozinhas (OK) e registradas, para o teste nunca travar.
   - As telas precisam abrir **embutidas** na janela principal (o roteiro reprova se o PDV abrir como janela solta).
7. Imprime `RESULTADO: OK` (ou a etapa que falhou) e encerra o processo que ele mesmo abriu.

Comprovação complementar da ofuscação: inspecionar a DLL extraída do pacote e confirmar ausência de nomes de métodos e de
textos SQL legíveis.

### Cenários de uso (nível 3b)

Cada classe que implementa `ICenario` é executada automaticamente pela bateria, num banco novo. A classe `Usuario`
simula a pessoa: `AbrirTela`, `Digitar`, `Teclar` (F2, F10, Esc...), `Clicar`, `QuandoAbrir<T>` (o que fazer quando abrir
uma janela modal). Mensagens não abrem caixa (ficam registradas em `Mensagens`); confirmações recebem a resposta
configurada; uma janela modal que o cenário não esperava é cancelada após 2 segundos e reprova o cenário — **nada trava
esperando clique**, o que permite rodar a bateria sem supervisão.

Os cenários foram escritos por agentes de teste exploratório trabalhando em paralelo, cada um numa área, que corrigiram
as falhas encontradas (registro em [10 — Falhas e resoluções](10-falhas-e-resolucoes.md)).

## 4. Ambiente

| Item | Valor |
|---|---|
| SO | Windows 10/11 x64 com sessão interativa (os testes abrem janelas) |
| SDK | .NET 10 |
| Ferramentas | `dotnet tool restore` (Obfuscar), Windows PowerShell 5.1 para o teste de interface |
| Dados | Bancos temporários em `%TEMP%`; nada é gravado no banco da loja |
| Serviços externos | Substituídos: transporte HTTP falso (Resend, Pamcard), simuladores (TEF, retorno bancário, CIOT) |

## 5. Técnicas de projeto de casos

| Técnica | Aplicação |
|---|---|
| Valor-limite | Desconto no limite do parâmetro; parcelas com centavos (33,33/33,33/33,34); fator de vencimento na virada de 22/02/2025; 5ª tentativa do código de recuperação |
| Partição de equivalência | Vendedor comum × administrador; pessoa física × jurídica (CIOT obrigatório só para autônomo contratado) |
| Tabela de decisão | Venda sem estoque (perguntar/permitir/bloquear); produção sem insumo; obrigatoriedade do CIOT |
| Transição de estados | Orçamento → pedido → faturado/cancelado; OP aberta → em produção → concluída; frete; boleto; TEF pendente → confirmada/desfeita |
| Exemplos de referência | DV da chave de acesso (manual da NF-e); código de barras e linha digitável (exemplo Febraban) |
| Teste de propriedade | Dados de demonstração: CPF/CNPJ válidos, FKs íntegras, insumos nunca negativos |

## 6. Rastreabilidade

A matriz requisito × regra × teste está na [especificação de requisitos](02-especificacao-de-requisitos.md); cada linha do
[relatório de testes](08-relatorio-de-testes.md) descreve a regra verificada em linguagem de negócio.

## 7. Critérios

| Critério | Definição |
|---|---|
| Entrada | Compilação sem erros nem avisos de segurança de pacotes |
| Aprovação | 100% das verificações aprovadas (código de saída 0) **e** `RESULTADO: OK` no teste de interface do executável publicado |
| Suspensão | Falha de ambiente (sem sessão interativa, executável em uso) — corrigir e repetir |
| Regressão | Toda falha corrigida ganha verificação na bateria |

## 8. Execução

```powershell
dotnet run --project testes/ErpLoja.Testes
dotnet publish ErpLoja/ErpLoja.csproj -c Release -o <pasta>
powershell -ExecutionPolicy Bypass -File testes/interface/teste-interface.ps1 -Pasta <pasta>
```

A bateria regenera `docs/06-modelo-de-dados.md`, `docs/08-relatorio-de-testes.md` e `docs/12-manual-do-usuario.md`;
diferenças nesses arquivos no `git diff` indicam mudança de esquema, de testes ou de ajuda.

## 9. Riscos de teste

| Risco | Mitigação |
|---|---|
| Integrações reais diferentes dos simuladores | Homologação com cada parceiro antes de produção (CNAB com o banco, CIOT com a Pamcard, TEF com o provedor) |
| Ofuscação quebrar algo não coberto pelo roteiro de interface | Regras de exclusão documentadas ([ADR-0010](adr/0010-ofuscacao.md)); bateria cobre a camada de dados; `-p:Ofuscar=false` para isolar |
| Diferenças de impressoras | Página de teste na tela de configurações; tipo WINDOWS como alternativa ao ESC/POS |
