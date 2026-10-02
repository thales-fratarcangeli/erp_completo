# ADR-0002 — SQLite em arquivo local

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
O produto deve ser instalado copiando um executável, sem servidor de banco de dados.

## Decisão
Usar SQLite em um único arquivo (`loja.db`) com chaves estrangeiras ligadas e transações para operações compostas.

## Consequências
- Zero instalação; backup = cópia de arquivo (feito com a API de backup online).
- Concorrência de escrita limitada: adequado a um caixa/poucos acessos; vários caixas em rede exigem evolução para banco cliente-servidor.
- Tipagem dinâmica: `DbSessao.Query` deduz os tipos das colunas.

## Alternativas consideradas
SQL Server Express/PostgreSQL (instalação e administração inviáveis para o público-alvo).
