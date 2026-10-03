# ADR-0024 — Rolagem horizontal e ordenação de tabelas centralizadas em `Ui.Grid`

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-03 |

## Contexto
Nas telas com muitas colunas, a grade ocupava a largura disponível e espremia as colunas até ficarem ilegíveis. Também
não havia um jeito uniforme de ordenar: as listas vindas do banco (`DataTable`) ordenavam pelo clique no título, mas
perdiam a ordem ao pesquisar ou recarregar, e as listas de itens em edição (`BindingList` de itens do pedido, da nota e
da ficha técnica) não ordenavam, porque `BindingList` não implementa ordenação.

## Decisão
- O comportamento fica **em um só lugar**: `Ui.Grid()`, que todas as telas já usam para criar tabelas, chama
  `RolarNaHorizontal` e `Ordenacao`. Nenhuma tela precisou de código próprio.
- `RolarNaHorizontal`: barra de rolagem nas duas direções e largura mínima por coluna, calculada pelo título (com espaço
  para a seta de ordenação) e pelo tipo de dado (texto, data, número, caixa de marcação), entre 50 e 240 pixels. O ajuste
  roda depois que a grade termina de se montar (`BeginInvoke`), porque mudar largura durante `ColumnAdded` ou no meio de
  uma ordenação quebra a grade.
- `Ordenacao`: lembra a coluna e a direção escolhidas (`Sorted`) e as reaplica quando a tela recarrega um `DataTable`
  (`DataBindingComplete` com `Reset`). A ordem escolhida continua ao pesquisar ou mudar filtros.
- `OrdenarLista`: para `BindingList` (que diz não suportar ordenação), ordena os itens manualmente pela propriedade da
  coluna, recoloca-os na lista, acerta a seta do título, mantém o item selecionado e registra no log. Valores que não
  se comparam deixam a lista como está.
- **Exceção: PDV.** A grade de itens do PDV é própria (não usa `Ui.Grid`), usa `Ui.RolarNaHorizontal` e marca as colunas
  como `NotSortable`: a ordem dos itens é a ordem em que foram registrados na venda.

## Consequências
- Comportamento igual em todas as tabelas, inclusive em telas novas, sem código repetido.
- A ordenação de uma lista de itens muda a ordem real dos itens no documento em edição (e, portanto, a ordem em que são
  gravados), já que a `BindingList` é reorganizada e não apenas exibida em outra ordem.
- Grades que não são criadas por `Ui.Grid` (hoje só a do PDV) precisam chamar os métodos explicitamente se quiserem o
  mesmo comportamento.

## Alternativas consideradas
- **Implementar em cada tela**: código repetido em dezenas de telas e fácil de esquecer em telas novas.
- **Classe derivada de `DataGridView`** (ex.: `GradeErp`): exigiria trocar o tipo em todas as telas e no designer, sem
  ganho sobre a fábrica `Ui.Grid` que já existia.
- **Lista ordenável genérica** (`BindingList` com `ApplySortCore`, ou `BindingSource` sobre `DataView`): mudaria o tipo
  das listas de itens em `Pedidos`, `Fiscal` e `Producao`, usadas pelas regras e pela ligação de grades (tipos excluídos
  da ofuscação); ordenar a própria lista resolve sem mexer nesses tipos.
- **Colunas com largura automática pelo conteúdo** (`AllCells`): lento em listas grandes e muda a largura a cada
  pesquisa.
