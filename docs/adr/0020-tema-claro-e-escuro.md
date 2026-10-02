# ADR-0020 — Tema claro e escuro por mapeamento de cores

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-02 |

## Contexto
Pedido: "opção nas configurações de alternar entre modo claro e escuro". As telas definem cores claras diretamente.

## Decisão
`UI/Tema` guarda as cores originais (claras) de cada controle e, no modo escuro, troca cada cor conhecida pela
equivalente escura (fundos e textos em tabelas separadas); grades, botões, listas e barras de status recebem
tratamento próprio; barra de título (DWM) e barras de rolagem (tema "DarkMode_Explorer") do Windows também escurecem.
A preferência é por usuário (`usuarios.tema`, migração 14) e a troca vale na hora para todas as janelas abertas.

## Consequências
- Nenhuma tela precisou ser alterada; cores novas que não estejam na tabela permanecem como estão (fáceis de incluir).
- Alguns controles nativos do Windows (calendário do seletor de data, caixa de marcação) mantêm o desenho claro.

## Alternativas consideradas
Paleta por tela (muito código repetido); pacotes de temas de terceiros (dependência e ofuscação).
