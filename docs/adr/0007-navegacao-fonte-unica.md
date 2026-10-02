# ADR-0007 — Navegação como fonte única e não redundância

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Requisito: o sistema não pode ter várias telas fazendo a mesma coisa; também é preciso localizar telas por palavra-chave.

## Decisão
`Navegacao.Telas()` define cada função uma única vez (título, módulo, palavras-chave, atalho, permissão). O menu lateral, os atalhos, o localizador (Ctrl+K) e a cor do módulo das janelas são gerados dela. A tela inicial é limpa; indicadores ficam no Painel gerencial.

## Consequências
- Um ponto de entrada por função; testes verificam ausência de entradas repetidas e ajuda para cada entrada.
- Nova tela exige entrada em `Navegacao` e tópico em `Ajuda`.

## Alternativas consideradas
Menus e atalhos montados em vários lugares (gerou duplicidades na v1.0).
