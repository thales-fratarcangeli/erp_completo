# ADR-0004 — Migrations versionadas

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
O banco evolui a cada versão e os clientes já têm dados.

## Decisão
Lista ordenada `Migracoes.Todas`; cada migração roda uma vez, em transação própria, registrada em `__migracoes`; backup antes de migrar; banco mais novo que o executável é recusado; bancos anteriores ao controle são reconhecidos (legado). Migrações que reconstroem tabelas desligam as FKs e verificam a integridade antes de confirmar.

## Consequências
- Atualização automática e segura ao abrir.
- Migração publicada é imutável: correções viram novas migrações.
- Testes migram bancos legados reais (v1, v2, v4 com dados).

## Alternativas consideradas
Script manual de atualização (sujeito a erro humano); recriar o banco (perda de dados).
