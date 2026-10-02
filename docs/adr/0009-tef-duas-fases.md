# ADR-0009 — TEF em duas fases com provedor abstrato

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
O TEF real depende do provedor contratado (SiTef, PayGo...) e de homologação; o PDV precisa ser testável sem maquininha.

## Decisão
Interface `IProvedorTef`; orquestração em `Tef` com duas fases (pendente → confirmada após gravar a venda, ou desfeita se a gravação falhar); toda transação registrada em `transacoes_tef`. Simulador visual para treino e testes; provedores reais não instalados recusam com explicação (não simulam).

## Consequências
- Sem cobrança sem venda nem venda sem cobrança.
- Integrar um provedor = implementar `IProvedorTef` e homologar.

## Alternativas consideradas
Chamar o TEF depois de gravar a venda (risco de venda gravada com pagamento negado).
