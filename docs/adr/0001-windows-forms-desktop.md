# ADR-0001 — Aplicativo desktop Windows Forms

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Lojas operam em computadores Windows, muitas vezes sem TI e com internet instável. O PDV precisa ser rápido, operado pelo teclado e funcionar offline.

## Decisão
Construir um aplicativo desktop em .NET 10 com Windows Forms.

## Consequências
- Interface nativa, responsiva e operável por teclado; acesso direto a impressoras e bibliotecas locais (TEF, spooler).
- Restrito a Windows.
- Sem designer visual: telas montadas em código (mais fáceis de revisar e versionar).

## Alternativas consideradas
Aplicação web (exigiria servidor/hospedagem e conectividade); WPF/MAUI (maior complexidade sem ganho para o caso).
