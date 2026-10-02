<p align="center"><img src="ErpLoja/Recursos/logo.png" width="96" alt="ERP Loja"></p>

# ERP Loja

ERP para loja de varejo, em **C# (.NET 10, Windows Forms)** com banco **SQLite**.
Vendas (PDV, orçamentos, pedidos), compras (pedidos e importação de XML de NF-e), estoque com custo médio,
financeiro, notas fiscais, relatórios e painel gerencial.

## Instalação

Não há instalador: **copie o `ErpLoja.exe` para a máquina e execute.**

- O executável é autocontido (já inclui o .NET e o SQLite). Funciona no Windows 10 e 11 (64 bits).
- Ao abrir, o sistema procura o banco de dados automaticamente:
  1. o último banco usado neste computador (o caminho fica salvo em `%LOCALAPPDATA%\ErpLoja\banco.txt`);
  2. o arquivo `loja.db` na mesma pasta do executável.
- Se não encontrar, ele pergunta se você quer **criar um banco novo (zerado)** ao lado do executável ou **indicar o arquivo** manualmente.
- Bancos de versões anteriores são atualizados sozinhos (**migrations**), com backup automático antes.

Primeiro acesso em banco novo: usuário **admin**, senha **admin**. Troque em *Configurações > Minha conta*.

> Como o executável não é assinado digitalmente, o Windows pode exibir "O Windows protegeu o computador".
> Clique em **Mais informações > Executar assim mesmo**.

O executável pronto fica na página de [Releases](../../releases) do repositório.

## Funcionalidades

| Área | O que faz |
|---|---|
| Vendas | PDV com leitor de código de barras, troco, a prazo parcelado e comprovante; orçamentos (aprovar vira pedido); pedidos de venda (faturar gera a venda) |
| Compras | Pedidos de compra (receber dá entrada no estoque); importação do XML da NF-e do fornecedor com vínculo de produtos e fator de conversão |
| Estoque | Saldo, situação, sugestão de compra, histórico de movimentações, ajuste/inventário, custo médio ponderado |
| Financeiro | Contas a receber/pagar geradas automaticamente, lançamentos manuais, baixa, estorno, vencidas |
| Fiscal | NF-e/NFC-e de saída e entrada com NCM, CFOP automático, CST/CSOSN, ICMS/IPI/PIS/COFINS, numeração, chave de acesso e DANFE (**sem transmissão à SEFAZ** — ver limitações) |
| Gestão | Painel com indicadores comparados ao período anterior e gráficos; relatórios com exportação CSV |
| Sistema | Tela única de configurações, usuários com recuperação de senha por e-mail (Resend), backups, ajuda em todas as telas (botão **?** / F1) |

## Documentação

| Documento | Para quem |
|---|---|
| [Manual do usuário](docs/manual-do-usuario.md) | Usuários da loja (é o mesmo conteúdo do botão **?** de cada tela) |
| [Regras de negócio](docs/regras-de-negocio.md) | Todos: como o sistema calcula e decide cada coisa |
| [Arquitetura e migrations](docs/arquitetura.md) | Desenvolvedores |
| [Falhas e resoluções](docs/falhas-e-resolucoes.md) | Suporte e desenvolvedores: problemas conhecidos e como resolver |
| [Histórico de versões](CHANGELOG.md) | Todos |

## Desenvolvimento

Requisitos: .NET SDK 10 (Windows).

```powershell
dotnet build ErpLoja.sln
dotnet run --project testes/ErpLoja.Testes        # bateria de testes (saída 0 = tudo certo)
dotnet publish ErpLoja/ErpLoja.csproj -c Release -o dist   # gera dist/ErpLoja.exe (arquivo único)
```

A bateria de testes também regenera `docs/manual-do-usuario.md` a partir da ajuda do sistema.

```
ErpLoja/              aplicação
  Data/               acesso a dados, migrations, regras de negócio (vendas, compras, fiscal, pedidos, e-mail)
  Forms/              telas
  UI/                 componentes visuais, gráficos, ajuda
  Recursos/           logo (ícone)
testes/ErpLoja.Testes bateria de testes de ponta a ponta
docs/                 documentação
ferramentas/          script que gera a logo
```

## Limitações conhecidas

- **Notas fiscais não são transmitidas à SEFAZ.** A numeração, a chave de acesso e o DANFE são gerados, mas a assinatura com certificado digital A1 e a autorização ainda não foram implementadas, por isso o DANFE sai marcado "sem valor fiscal".
- O SQLite é um banco de arquivo: ideal para um computador (ou poucos acessos simultâneos). Para vários caixas em rede, veja [Falhas e resoluções](docs/falhas-e-resolucoes.md#banco-em-pasta-de-rede).
