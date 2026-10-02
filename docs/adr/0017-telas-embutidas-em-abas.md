# ADR-0017 — Telas embutidas em abas na janela principal

| | |
|---|---|
| **Status** | Substituída pela [ADR-0019](0019-uma-tela-por-vez-e-dados-nao-salvos.md) (as abas foram removidas a pedido do usuário) |
| **Data** | 2026-10-02 |

## Contexto
Pedido: "nao quero que abra uma nova tela flutuante cada opção que eu clicar... e sim que abra no espaço à direita". Janelas soltas se perdiam atrás umas das outras.

## Decisão
Cada tela do menu é um `Form` convertido em conteúdo (`TopLevel = false`, sem borda, `Dock = Fill`) dentro do palco da `MainForm`, com uma aba própria (ponto na cor do módulo, título, ×, sublinhado animado). As telas continuam sendo `Form` (mesmo código, mesmos testes); diálogos de edição e confirmação continuam modais. Atalhos: Ctrl+Tab, Ctrl+Shift+Tab, Ctrl+W.

## Consequências
- Nenhuma tela precisou ser reescrita; a transparência de entrada (Opacity) só é usada em janelas soltas.
- As teclas de função de uma tela ativa (ex.: PDV) têm prioridade, porque `ProcessCmdKey` percorre do controle focado até a janela principal.
- O fechamento da janela principal consulta cada aba (venda em andamento no PDV impede o fechamento).

## Alternativas consideradas
MDI clássico (visual antigo, barras de título internas); `TabControl` nativo (aparência limitada e sem controle do fechamento por aba); converter telas em `UserControl` (reescrita de todas as telas).
