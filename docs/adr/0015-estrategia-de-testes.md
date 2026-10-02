# ADR-0015 — Testes ponta a ponta + teste de interface

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Regras são integradas (venda mexe em estoque, financeiro, TEF); migrações precisam ser comprovadas em bancos reais; a ofuscação pode quebrar a interface.

## Decisão
Bateria console (`testes/ErpLoja.Testes`) que exercita regras, migrações, integrações com transportes falsos, abre todas as telas e gera documentação; teste de interface por UI Automation (`testes/interface`) rodado sobre o executável publicado e ofuscado.

## Consequências
- Alta confiança de regressão com um comando.
- Os testes abrem janelas (precisam de sessão interativa do Windows).

## Alternativas consideradas
Somente testes unitários isolados (não pegariam falhas de integração e de ofuscação).
