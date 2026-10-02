# ADR-0012 — CIOT por administradora abstrata (Pamcard primeiro)

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
O CIOT é gerado por administradoras habilitadas pela ANTT; a loja escolhe a sua; foco solicitado na Pamcard.

## Decisão
Interface `IAdministradoraCiot` com `CiotSimulador`, `PamcardAdministradora` (credenciais em configurações; montagem e leitura das mensagens isoladas) e `AdministradoraNaoIntegrada` para as demais. Regras do frete (obrigatoriedade, validações, travas, contas) independem da administradora.

## Consequências
- O layout definitivo da Pamcard é confirmado com o manual técnico no credenciamento; só o adaptador muda.
- Outras administradoras = nova implementação da interface.

## Alternativas consideradas
Acoplar o frete diretamente à API de uma administradora.
