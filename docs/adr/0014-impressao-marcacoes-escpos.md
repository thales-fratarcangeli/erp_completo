# ADR-0014 — Impressão por marcações e ESC/POS

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Comprovantes saem em térmicas não fiscais variadas (Elgin, Bematech, Epson...) e documentos em impressoras comuns.

## Decisão
Documentos são texto com marcações (`[C]`, `[N]`, `[G]`). A impressora de cupom pode ser WINDOWS (desenho pelo driver) ou ESCPOS (bytes RAW pelo spooler: alinhamento, negrito, corte, gaveta, página de código 850/860).

## Consequências
- Funciona com qualquer impressora instalada no Windows.
- Recursos avançados (logotipo gráfico, QR nativo) ficam para evolução.

## Alternativas consideradas
Drivers/SDKs específicos de cada fabricante.
