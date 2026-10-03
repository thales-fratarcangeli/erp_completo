# Como contribuir

## Ambiente

- Windows 10/11 x64, .NET SDK 10.
- `dotnet tool restore` instala o Obfuscar local (usado só na publicação Release).
- Edite os fontes com ferramentas que preservam UTF-8 (o PowerShell 5.1 corrompe acentos ao regravar arquivos sem BOM).

## Fluxo

1. Crie um ramo a partir de `main`.
2. Implemente a mudança seguindo as regras abaixo.
3. Durante o desenvolvimento, rode só os cenários da sua área com `ERP_CENARIO` (ver abaixo).
4. Antes do commit, rode a bateria **completa**: `dotnet run --project testes/ErpLoja.Testes` (saída 0) e confira as diferenças nos documentos gerados (06, 08, 12).
5. Se a mudança afeta a interface ou a publicação, publique e rode o [teste de interface](docs/07-plano-de-testes.md#nível-4) no executável ofuscado.
6. Atualize a documentação afetada e o [CHANGELOG](CHANGELOG.md).
7. Abra o pull request descrevendo o que mudou, por quê e como foi testado.

### Rodar só um cenário

A bateria inteira leva cerca de 10 minutos. Para iterar num cenário, defina `ERP_CENARIO` com parte do **nome da
classe** do cenário (sem diferenciar maiúsculas):

```powershell
$env:ERP_CENARIO = "AssistenteDeDuvidas"   # ou "Tabelas", "OrdemDeServico", "BaixaEmLote"...
dotnet run --project testes/ErpLoja.Testes
Remove-Item Env:ERP_CENARIO
```

Nesse modo a bateria roda só os cenários correspondentes: pula migrações, regras, a abertura de todas as telas e as
demais verificações gerais, e **não regrava** os documentos 06, 08 e 12. Por isso não vale como aprovação — a
execução completa continua obrigatória antes do commit.

## Regras do código

| Assunto | Regra |
|---|---|
| Camadas | Regras e SQL de escrita em `ErpLoja/Data`; telas em `Forms`; componentes em `UI` ([arquitetura](docs/05-arquitetura.md)) |
| Banco | Só SQL parametrizado; operações compostas numa única `DbSessao(transacao: true)` |
| Esquema | Nunca altere migração publicada; crie a próxima em `Migracoes.Todas` (ver [migrations](docs/05-arquitetura.md#migrations)) |
| Parâmetros | Novo parâmetro em `Parametros.Definicoes` **e** numa migração nova |
| Telas | Toda tela nova entra em `Navegacao.Telas()` (menu, atalho, busca) e ganha tópico em `Ajuda.Topicos`; `Ui.Estilizar(this, ..., nameof(MinhaTela))` |
| Redundância | Uma função, um lugar: não crie segundo ponto de entrada para algo existente |
| Ofuscação | Classe ligada a grade por nome de propriedade ou usada por reflexão/P/Invoke: `[Obfuscation(Exclude = true, ApplyToMembers = true)]`. Não use tipos anônimos para JSON. Chaves por nome de tipo: `nameof` |
| Integrações | Atrás de interface (`IProvedorTef`, `IAdministradoraCiot`, `IProvedorIA`) e com transporte HTTP substituível para testes (`TransporteParaTestes`) |
| Mensagens | Português, dizendo o que aconteceu e o que fazer; sempre por `Ui.Erro`/`Ui.Info`/`Ui.Confirmar` (nunca `MessageBox` direto), para os testes interceptarem |

## Tela nova (checklist)

A bateria reprova se algum destes passos faltar ([plano de testes](docs/07-plano-de-testes.md#verificações-gerais-da-bateria-níveis-1-a-3)).

| Passo | Onde | O que a bateria confere |
|---|---|---|
| Entrada no menu, atalho e localizador | `Navegacao.Telas()` (`ErpLoja/UI/Navegacao.cs`): chave, título, área, palavras-chave, `() => new MinhaTela()`; `SoAdmin: true` se for de administrador | O cenário de tabelas abre toda tela do menu |
| Aparência e ajuda | `Ui.Estilizar(this, "Título", largura, altura, nameof(MinhaTela))` no construtor | Botão “?” visível |
| Tópico de ajuda | `Ajuda.Topicos` (`ErpLoja/UI/Ajuda.cs`) com a mesma chave; diálogos `new Dialogo(titulo, "chave", "Botão")` também precisam de tópico | “toda tela aberta tem tópico de ajuda” e “todos os diálogos têm ajuda”; o manual ([12](docs/12-manual-do-usuario.md)) é gerado daí |
| Lista de telas do teste | `var telas = new (string, Func<Form>)[] { ... }` em `testes/ErpLoja.Testes/Program.cs` (e, se fizer sentido, a lista “(demo)” logo abaixo) | “todas as telas do sistema foram abertas no teste” — toda classe `Form` do `ErpLoja`, exceto `Dialogo` e `Toast` |
| Tabelas | Sempre `Ui.Grid()` (nunca `new DataGridView`): já vem com rolagem horizontal, largura mínima, ordenação pelo título, estado vazio e validação das células. Lista cuja ordem importa (ex.: itens do PDV): colunas `NotSortable` | “todas as tabelas têm rolagem horizontal...” e “todas as colunas de texto... podem ser ordenadas” |
| Campos | Sempre `Validacao.Caixa(Formatos.X, ...)` (ou outro `Validacao.*`); `ReadOnly = true` para campos só de leitura | “todos os campos de texto editáveis das telas têm formato” e varredura do fonte atrás de `new TextBox` sem formato |
| Alerta na tela inicial | Se a tela tiver alerta: incluí-lo em `Alertas.Listar()` com a chave da tela e um filtro, e a tela implementa `IFiltroInicial.AplicarFiltro(filtro)` (sem isso o alerta abre a tela sem filtro) | Cobrir no cenário da tela (`f.AplicarFiltro(...)` e o botão do alerta na `MainForm`) |
| Banco | Tabelas/colunas novas numa **migração nova** em `Migracoes.Todas`; parâmetro novo em `Parametros.Definicoes` **e** na migração | Migração do banco legado v1 até a versão atual; `docs/06` regenerado |
| Cenário de uso | Um `ICenario` operando a tela (ver abaixo) | — |

## Como escrever um cenário

Crie uma classe em `testes/ErpLoja.Testes/Cenarios/` que implemente `ICenario`; a bateria a encontra sozinha por
reflexão e a roda num banco novo, como administrador.

```csharp
public class MinhaAreaFuncionarioFazAlgo : ICenario
{
    public string Area => "Minha área";      // aparece entre colchetes no relatório (docs/08)
    public bool ComDadosDemo => true;         // false = banco vazio

    public void Executar(Usuario u)
    {
        var f = u.AbrirTela("MinhaTela");     // chave de Navegacao.Telas()
        u.QuandoAbrirJanela("Nova coisa", d => // agende ANTES de clicar no botão que abre o modal
        {
            Usuario.Achar<TextBox>(d).Text = "...";
            u.Clicar(d, "Salvar");
        });
        u.Clicar(f, "Nova");
        u.ConferirJanelas();                  // reprova se a janela agendada não abriu
        u.Ok(u.Viu("INFO", "gravad"), "o que a pessoa espera ver, em linguagem de negócio");
        u.RespostaConfirmacao = true;
        f.Close();
        f.Dispose();
    }
}
```

| Recurso | Para quê |
|---|---|
| `u.AbrirTela`, `u.Clicar`, `u.Digitar`, `u.Teclar` | Operar a tela como a pessoa (botão desabilitado gera erro; teclas passam por `ProcessCmdKey`/`OnKeyDown`) |
| `Usuario.Achar<T>`, `Usuario.Botao`, `Usuario.Todos`, `Usuario.Esperar` | Localizar controles e deixar a fila de mensagens andar |
| `u.QuandoAbrir<T>` / `u.QuandoAbrirJanela("título", ...)` | Responder janelas modais (diálogos, editores); a ação precisa preencher **e fechar** a janela |
| `u.Mensagens`, `u.Viu(tipo, trecho)`, `u.UltimaMensagem(tipo)` | Conferir `Ui.Erro`/`Ui.Info`/`Ui.Confirmar` (tipos `ERRO`, `INFO`, `CONFIRMAR`) |
| `u.RespostaConfirmacao`, `u.RespostasConfirmacao` (fila) | Resposta às confirmações (Sim/Não) |
| `u.Ok(condição, descrição)` | Registrar a verificação no relatório |

Regras:
- **Nada pode travar em caixa de mensagem.** O `Usuario` define `Ui.Interceptar`, então `Ui.Erro/Info/Confirmar` não abrem
  caixa; por isso o código do sistema nunca deve chamar `MessageBox.Show` direto. Modal não agendado é cancelado em 2
  segundos e reprova o cenário.
- Não use mensagens de mouse do Windows (`SendMessage`/`PostMessage`) para clicar: só funcionam com a janela da bateria
  em primeiro plano, o que não é garantido. Acione o tratador da própria classe (ex.: `OnColumnHeaderMouseClick` da grade,
  por reflexão — ver `InterfaceTabelas.cs`).
- Feche e descarte as telas que abrir; o `Usuario` descarta as que sobrarem ao final.
- Utilitários compartilhados ficam nos arquivos de apoio (`SistemaUtil` em `SistemaNavegacao.cs`, `FinanceiroUtil` em
  `FinanceiroCenarios.cs`, `ComprasUtil.cs`, `CadastrosApoio.cs`); reaproveite antes de criar outro.

## Código que chama serviço externo

Nenhum teste pode acessar a rede. Toda integração HTTP expõe uma propriedade estática
`public static HttpMessageHandler? TransporteParaTestes` (como `Email`, `PamcardAdministradora` e `ProvedorClaude`)
e, quando ela estiver definida, usa `new HttpClient(TransporteParaTestes)` em vez da rede.

No teste:
1. Escreva um `HttpMessageHandler` falso que **guarde a requisição** (corpo, cabeçalhos) e devolva uma resposta no
   formato do serviço — ver `AnthropicFalsa` em `Cenarios/AssistenteIA.cs` e `ResendFalso` em `Program.cs`.
2. Instale-o antes e **remova-o no `finally`** (`TransporteParaTestes = null`), para não vazar para outros cenários.
3. Cubra sucesso **e** falhas: erro HTTP (ex.: 401), resposta sem o dado esperado, recusa do serviço.
4. Confira o que sai do computador (o corpo que o SDK/cliente montou), não só o que volta — por exemplo, que dados
   pessoais foram mascarados e que nenhum dado da loja foi enviado.
5. Compare valores do JSON já lidos (`JsonNode` + `GetValue<string>()`), não o texto cru: o JSON escapa acentos.

## Commits

Mensagens no imperativo, em português, explicando o porquê. Nunca versione bancos da loja (`*.db`), exceto as fixtures
de `testes/ErpLoja.Testes/dados`.

## Publicação

1. Atualize `<Version>` em `ErpLoja/ErpLoja.csproj` e o CHANGELOG.
2. `dotnet publish ErpLoja/ErpLoja.csproj -c Release -o dist`.
3. Teste de interface no `dist` → `RESULTADO: OK`.
4. Crie a tag `vX.Y.Z` e a release com o `ErpLoja.exe`.
