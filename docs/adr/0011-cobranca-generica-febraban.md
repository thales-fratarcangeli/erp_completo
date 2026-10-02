# ADR-0011 — Cobrança genérica Febraban / CNAB 240

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
A loja pode usar qualquer banco; cada banco define o campo livre do boleto e detalhes do CNAB.

## Decisão
Implementar o que é padrão Febraban (código de barras, DV, fator de vencimento com a virada de 22/02/2025, linha digitável, CNAB 240 segmentos P/Q/R e T/U) e tornar o específico configurável: o campo livre é montado por um modelo de marcações por conta bancária. Validador estrutural e simulador de retorno apoiam a homologação.

## Consequências
- Novo banco = cadastrar a conta e ajustar o modelo conforme o manual do banco.
- Particularidades além do campo livre (posições reservadas, versões de leiaute) podem exigir ajuste pontual após a homologação.

## Alternativas consideradas
Biblioteca de boletos de terceiros (dependência externa); CNAB 400 (específico por banco).
