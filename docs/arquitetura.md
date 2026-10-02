# Arquitetura

Guia para quem vai manter ou evoluir o ERP Loja.

## Visão geral

- **Aplicativo desktop Windows Forms** (.NET 10), publicado como **um único executável autocontido** (`PublishSingleFile` + `SelfContained` + compressão). Não precisa de .NET instalado nem de instalador.
- **Banco SQLite** em um único arquivo (`loja.db`), acessado com `Microsoft.Data.Sqlite` (sem ORM).
- Sem servidor: cada estação abre o arquivo do banco diretamente.

```
ErpLoja/
  Program.cs            inicialização: localizar banco → migrations → backup diário → login → janela principal
  Seguranca.cs          sessão do usuário e hash de senhas (PBKDF2)
  Data/
    Db.cs               conexão, DbSessao (comandos com @p0..@pN, transação), DataTable com tipos deduzidos
    Migracoes.cs        migrations versionadas (ver abaixo)
    LocalBanco.cs       localização/validação do arquivo do banco + Backups
    Parametros.cs       definição única dos parâmetros (padrões, tela de configurações, leitura tipada)
    Operacoes.cs        estoque, vendas, compras, cancelamentos, contas
    Pedidos.cs          orçamentos e pedidos (salvar, aprovar, faturar, receber)
    Fiscal.cs           notas fiscais (montagem, impostos, numeração, chave, DANFE)
    ImportacaoXml.cs    leitura do XML da NF-e e importação
    Email.cs            envio via Resend + recuperação de senha
  UI/
    Ui.cs               estilo, grades, botões, formatação, CSV, ícone
    Dialogo.cs          PainelCampos e diálogos padrão (com "?" na barra de título)
    Ajuda.cs            tópicos de ajuda (fonte única do botão "?" e do manual) + janela de ajuda
    Grafico.cs          gráfico de barras/colunas desenhado em GDI+
  Forms/                telas (uma classe por tela)
  Recursos/             logo.ico / logo.png (embutidos no executável)
testes/ErpLoja.Testes/  bateria de testes de ponta a ponta (console)
ferramentas/            gerar-logo.ps1
```

### Camadas

- **Data** contém as regras de negócio e todo o SQL de escrita. As operações que mexem em várias tabelas
  (venda, compra, cancelamentos, faturamento, importação) rodam **numa única transação** (`DbSessao(transacao: true)`):
  ou tudo é gravado, ou nada.
- **Forms** só montam a tela, validam a entrada do usuário e chamam a camada Data. Consultas de listagem (SELECT) ficam nas telas.
- **UI** são componentes reutilizados por todas as telas.

### Convenções

- Parâmetros SQL posicionais `@p0, @p1...` (`Db.Query/Exec/Scalar`). `decimal` é gravado como REAL, `DateTime` como texto `yyyy-MM-dd HH:mm:ss`, datas sem hora como `yyyy-MM-dd`.
- `DbSessao.Query` monta o `DataTable` deduzindo o tipo de cada coluna pelos valores (o SQLite tem tipagem dinâmica), para a grade formatar e ordenar números corretamente.
- Colunas `id` ficam ocultas nas grades; os aliases das consultas (`AS "Nome"`) são o cabeçalho exibido.
- Cada tela chama `Ui.Estilizar(...)`, que aplica o visual, o ícone e instala o botão **?** (e F1) com a chave de ajuda da tela.
  Diálogos usam `new Dialogo(titulo, chaveDeAjuda)`. Toda chave precisa existir em `Ajuda.Topicos` (os testes conferem).
- **Sem telas redundantes**: cada função tem um único ponto de entrada no menu lateral. Antes de criar uma tela, confira se a função já existe.

## Banco de dados

### Localização (`LocalBanco`)

1. Caminho lembrado em `%LOCALAPPDATA%\ErpLoja\banco.txt` (gravado sempre que um banco é aberto com sucesso);
2. `loja.db` na pasta do executável;
3. senão, a tela `BancoForm` oferece criar banco novo ou indicar o arquivo.

`Db.NovaConexao()` abre o arquivo em modo **ReadWrite** (não cria): só `LocalBanco.CriarNovo` cria arquivos. Isso evita
criar um banco vazio por engano quando o caminho lembrado fica indisponível.

Linha de comando: `ErpLoja.exe --criar-banco [caminho]` cria/atualiza o banco sem abrir a interface.

### Migrations (`Migracoes.cs`)

- Lista ordenada `Migracoes.Todas` de `Migracao(Versao, Nome, Aplicar)`.
- Tabela `__migracoes(versao, nome, aplicada_em)` registra o que já foi aplicado; `PRAGMA user_version` acompanha a versão.
- `Migracoes.Atualizar()` (chamado em toda abertura):
  1. cria `__migracoes` se não existir e registra bancos **legados** (anteriores ao controle de versões: `user_version` 0 → versão 1; 2 → versões 1 e 2);
  2. recusa banco com versão maior que a do executável;
  3. se há pendências num banco existente, faz **backup** em `backups/`;
  4. aplica cada migração pendente **em sua própria transação** e a registra. Se uma falhar, ela é desfeita inteira e o erro informa o backup.

| Versão | Conteúdo |
|---|---|
| 1 | Estrutura inicial: usuários, categorias, clientes, fornecedores, produtos, vendas, compras, movimentações, contas; usuário admin |
| 2 | Pedidos/orçamentos, notas fiscais, vínculo produto × fornecedor, campos fiscais em produtos/clientes/fornecedores, frete em vendas |
| 3 | `usuarios.login` → `username`, `usuarios.email` (único), tabela `recuperacao_senha` |
| 4 | Parâmetros do sistema com valores padrão |

#### Como criar uma migração

1. Acrescente no **fim** de `Migracoes.Todas`: `new(5, "Descrição curta", M005)`.
2. Escreva `static void M005(DbSessao s)` com o SQL (`s.Exec(...)`). Use os auxiliares `AdicionarColuna` (idempotente) e `Config` para parâmetros.
3. **Nunca altere uma migração já publicada** — bancos de clientes já a executaram. Corrija com uma nova migração.
4. Novos parâmetros: acrescente em `Parametros.Definicoes` **e** grave o padrão numa migração nova (a 4 já foi aplicada nos bancos existentes).
5. Rode os testes: eles migram um banco legado real (`testes/ErpLoja.Testes/dados/banco_v1.db`) até a versão atual.

O SQLite tem limitações de `ALTER TABLE` (não remove/altera colunas com restrições). Para mudanças estruturais grandes,
use o padrão: criar tabela nova → copiar dados → apagar a antiga → renomear, tudo dentro da migração.

### Backups

`Backups.Criar(motivo)` usa a API de backup online do SQLite (cópia consistente mesmo com o banco aberto) e grava em
`<pasta do banco>\backups\`. Usado antes de migrations, no backup diário e no botão "Fazer backup agora".

## Segurança

- Senhas e códigos de recuperação: PBKDF2-SHA256, 100.000 iterações, salt de 16 bytes, comparação em tempo constante.
- A chave da API do Resend fica na tabela `configuracoes` **em texto** e aparece mascarada na tela. Quem tiver acesso ao
  arquivo `loja.db` tem acesso a ela: proteja a pasta do banco e use uma chave do Resend com permissão só de envio.
- O arquivo do banco não é criptografado; o controle de acesso é o do sistema operacional (permissões da pasta).

## Ajuda e manual

`UI/Ajuda.cs` é a **fonte única** do texto de ajuda. O botão **?** de cada tela mostra o tópico da tela, e a bateria de testes
regenera `docs/manual-do-usuario.md` a partir dos mesmos tópicos. Para mudar o manual, edite `Ajuda.cs` e rode os testes.

## Testes

`dotnet run --project testes/ErpLoja.Testes` executa a bateria (código de saída = número de falhas). Ela cobre:
regras de estoque/custo/vendas/compras/financeiro, pedidos e orçamentos, notas fiscais (numeração, chave, DV, DANFE),
importação de XML, migrations (bancos legados v1 e v2, banco mais novo), localização do banco, recuperação de senha
(com o Resend substituído por um falso), parâmetros, e abre **todas as telas** conferindo que cada uma tem ajuda e botão "?".
Capturas de cada tela ficam em `testes/ErpLoja.Testes/bin/.../telas/`.

## Publicação

```powershell
dotnet publish ErpLoja/ErpLoja.csproj -c Release -o dist
```

Gera `dist/ErpLoja.exe` (~48 MB). O SQLite nativo vai embutido e é extraído automaticamente na primeira execução.
A versão do produto fica em `<Version>` no `ErpLoja.csproj` e aparece na barra de status.
