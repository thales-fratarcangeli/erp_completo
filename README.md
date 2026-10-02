<p align="center"><img src="ErpLoja/Recursos/logo.png" width="96" alt="ERP Loja"></p>

# ERP Loja

ERP para loja de varejo, em **C# (.NET 10, Windows Forms)** com banco **SQLite**: PDV com TEF, orçamentos e pedidos,
compras com importação de XML, produção, estoque com custo médio, financeiro com cobrança bancária (CNAB 240),
fretes com CIOT, notas fiscais com a reforma tributária, painel gerencial e relatórios.

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
| Vendas | PDV de teclado com leitor de código de barras, TEF (cartão/PIX), troco, a prazo e comprovante; orçamentos → pedidos → faturamento |
| Compras | Pedidos de compra; importação do XML da NF-e com vínculo de produtos e fator de conversão |
| Produção | Fichas técnicas e ordens de produção com custo real |
| Estoque | Saldo, situação, sugestão de compra, histórico, inventário, custo médio ponderado |
| Financeiro | Contas a pagar/receber, baixa, estorno; **cobrança bancária** genérica (boleto Febraban, CNAB 240, homologação) |
| Logística | Veículos, motoristas, fretes e **CIOT** (Pamcard) |
| Fiscal | NF-e/NFC-e com CFOP automático, ICMS/IPI/PIS/COFINS e **IBS/CBS/IS** da reforma tributária; chave de acesso e DANFE (sem transmissão à SEFAZ) |
| Gestão | Painel gerencial com comparação ao período anterior; relatórios exportáveis |
| Sistema | Uma tela por vez à direita do menu (listagem ao meio, opções à direita), aviso de dados não salvos, modo claro/escuro, "continuar de onde parou", localizador de telas (Ctrl+K), ajuda em cada tela (**?**/F1), log de atividades em banco separado, cadastro único de pessoas, configurações centralizadas, usuários com recuperação de senha (Resend), backups, migrations, impressoras (Windows/ESC/POS), atalho na área de trabalho, dados de demonstração |

## Documentação

Índice completo em [docs/](docs/README.md). Principais:

| Documento | Para quem |
|---|---|
| [Manual do usuário](docs/12-manual-do-usuario.md) | Usuários (mesmo conteúdo do botão **?**) |
| [Regras de negócio](docs/04-regras-de-negocio.md) | Todos |
| [Implantação e operação](docs/09-implantacao-e-operacao.md) | Implantação e suporte |
| [Falhas e resoluções](docs/10-falhas-e-resolucoes.md) | Suporte e desenvolvimento |
| [Arquitetura](docs/05-arquitetura.md) e [ADRs](docs/adr/) | Desenvolvimento |
| [Testes exploratórios](docs/14-testes-exploratorios.md) e [Histórico de solicitações](docs/13-historico-de-solicitacoes.md) | Todos |
| [Como contribuir](CONTRIBUTING.md) · [Segurança](SECURITY.md) · [Histórico](CHANGELOG.md) | Desenvolvimento |

## Desenvolvimento

Requisitos: .NET SDK 10 (Windows).

```powershell
dotnet build ErpLoja.sln
dotnet run --project testes/ErpLoja.Testes                 # bateria de testes (saída 0 = tudo certo; regenera docs 06, 08 e 12)
dotnet tool restore                                        # Obfuscar (ferramenta local)
dotnet publish ErpLoja/ErpLoja.csproj -c Release -o dist   # dist/ErpLoja.exe (arquivo único, ofuscado)
powershell -File testes/interface/teste-interface.ps1 -Pasta dist   # teste de interface do executável
```

```
ErpLoja/              aplicação
  Data/               acesso a dados, migrations, parâmetros e regras de negócio
  Forms/              telas
  UI/                 componentes visuais, navegação, ajuda, animações
  Recursos/           logo
testes/ErpLoja.Testes bateria de testes de ponta a ponta (+ geração de documentação)
  Cenarios/           cenários de uso: simulam pessoas operando as telas
testes/interface      teste de interface por UI Automation
docs/                 documentação de engenharia
ferramentas/          script que gera a logo
```

## Limitações conhecidas

- **TEF real (SiTef/PayGo) e Pamcard em produção** dependem de credenciamento/homologação com cada parceiro; há simuladores para treino e testes.
- **Notas fiscais não são transmitidas à SEFAZ.** A numeração, a chave de acesso e o DANFE são gerados, mas a assinatura com certificado digital A1 e a autorização ainda não foram implementadas, por isso o DANFE sai marcado "sem valor fiscal".
- O SQLite é um banco de arquivo: ideal para um computador (ou poucos acessos simultâneos). Para vários caixas em rede, veja [Falhas e resoluções](docs/10-falhas-e-resolucoes.md#banco-em-pasta-de-rede).
