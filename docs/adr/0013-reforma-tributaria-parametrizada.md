# ADR-0013 — Reforma tributária parametrizada

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
EC 132/2023 e LC 214/2025 introduzem IBS, CBS e Imposto Seletivo com transição de 2026 a 2033; as alíquotas mudam ano a ano.

## Decisão
Campos por produto (CST IBS/CBS, cClassTrib, reduções, IS) e por item/nota; alíquotas e padrões como parâmetros (padrão 2026: CBS 0,9%, IBS 0,1%); destaque sem somar ao total, conforme o ano de teste.

## Consequências
- Mudança de alíquota no ano = ajuste de parâmetro, sem nova versão.
- Mudança de regra de cálculo (ex.: base, soma ao total a partir de 2027) exigirá ajuste de código e de testes.

## Alternativas consideradas
Alíquotas fixas no código.
