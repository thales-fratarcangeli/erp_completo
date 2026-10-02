# ADR-0019 — Uma tela por vez, modelo padrão e proteção de dados não salvos

| | |
|---|---|
| **Status** | Aceita (substitui a [ADR-0017](0017-telas-embutidas-em-abas.md)) |
| **Data** | 2026-10-02 |

## Contexto
Pedido do usuário: "não quero aquelas abas acima ali tipo navegador; toda vez que a pessoa escrever ou preencher algum
campo na tela e for sair, tem que exibir um aviso" e "todas as telas têm que seguir o modelo: listagem ao meio, na
direita opções em forma de lista com barra de rolagem e na esquerda o menu".

## Decisão
- A janela principal mostra **uma tela por vez** à direita do menu; abrir outra substitui a atual. O cabeçalho mostra
  "Área › Tela" e o botão ×.
- `UI/Alteracoes` detecta, de forma genérica, o que a pessoa digita/escolhe/marca/edita (pesquisa e filtros não contam) e
  pergunta antes de sair: *Salvar e sair* / *Descartar e sair* / *Continuar editando*. Botões de gravar que terminam sem
  mensagem de erro deixam a tela "limpa". Telas com confirmação própria (PDV, editor de nota, ficha técnica) a mantêm.
- `UI/ModeloDeTela` aplica o modelo às telas do menu: os botões da barra superior viram a lista "Opções" à direita, com
  rolagem; filtros ficam em cima; a listagem fica no meio. As telas continuam montadas com `Ui.Barra()`.

## Consequências
- Comportamento uniforme sem reescrever cada tela; telas novas ganham o modelo e a proteção automaticamente.
- Não há mais várias telas abertas ao mesmo tempo (troca rápida passa pelo menu, atalhos, Ctrl+K e "continuar de onde parou").

## Alternativas consideradas
Manter abas (rejeitado pelo usuário); confirmação implementada tela a tela (inconsistente e fácil de esquecer).
