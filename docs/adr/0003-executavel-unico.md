# ADR-0003 — Executável único autocontido

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
A instalação precisa ser "jogar o executável na máquina".

## Decisão
Publicar com `SelfContained`, `PublishSingleFile`, `IncludeNativeLibrariesForSelfExtract` e compressão.

## Consequências
- Um arquivo (~50 MB) com o runtime .NET e o SQLite nativo; nenhum pré-requisito.
- Executável não assinado: o SmartScreen pode alertar (ver [implantação](../09-implantacao-e-operacao.md)).

## Alternativas consideradas
Instalador MSI (contraria a exigência); executável dependente do runtime instalado (exige pré-requisito).
