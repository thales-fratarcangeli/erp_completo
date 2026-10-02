# ADR-0005 — SQL parametrizado sem ORM

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Modelo relacional simples, consultas de relatório específicas e necessidade de controle fino das transações.

## Decisão
Usar Microsoft.Data.Sqlite com SQL escrito à mão, parâmetros posicionais (`@p0..`) e `DbSessao` para transação.

## Consequências
- Sem dependências pesadas; SQL visível e auditável; desempenho previsível.
- Mapeamento manual (DataTable/DataRow) e responsabilidade do desenvolvedor sobre o SQL.

## Alternativas consideradas
Entity Framework Core (migrações próprias, mais peso e menos controle no SQLite).
