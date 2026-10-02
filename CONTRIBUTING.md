# Como contribuir

## Ambiente

- Windows 10/11 x64, .NET SDK 10.
- `dotnet tool restore` instala o Obfuscar local (usado só na publicação Release).
- Edite os fontes com ferramentas que preservam UTF-8 (o PowerShell 5.1 corrompe acentos ao regravar arquivos sem BOM).

## Fluxo

1. Crie um ramo a partir de `main`.
2. Implemente a mudança seguindo as regras abaixo.
3. Rode a bateria: `dotnet run --project testes/ErpLoja.Testes` (saída 0) e confira as diferenças nos documentos gerados (06, 08, 12).
4. Se a mudança afeta a interface ou a publicação, publique e rode o [teste de interface](docs/07-plano-de-testes.md#nível-4) no executável ofuscado.
5. Atualize a documentação afetada e o [CHANGELOG](CHANGELOG.md).
6. Abra o pull request descrevendo o que mudou, por quê e como foi testado.

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
| Integrações | Atrás de interface (`IProvedorTef`, `IAdministradoraCiot`) e com transporte HTTP substituível para testes |
| Mensagens | Português, dizendo o que aconteceu e o que fazer |

## Commits

Mensagens no imperativo, em português, explicando o porquê. Nunca versione bancos da loja (`*.db`), exceto as fixtures
de `testes/ErpLoja.Testes/dados`.

## Publicação

1. Atualize `<Version>` em `ErpLoja/ErpLoja.csproj` e o CHANGELOG.
2. `dotnet publish ErpLoja/ErpLoja.csproj -c Release -o dist`.
3. Teste de interface no `dist` → `RESULTADO: OK`.
4. Crie a tag `vX.Y.Z` e a release com o `ErpLoja.exe`.
