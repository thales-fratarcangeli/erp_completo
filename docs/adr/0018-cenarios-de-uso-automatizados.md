# ADR-0018 — Cenários de uso automatizados e testes exploratórios por agentes

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-02 |

## Contexto
Pedido: "lance vários agentes paralelos testando funcionalidades do sistema como se fossem usuários achando falhas e consertando-as". Os testes precisavam operar as telas (não só as regras) e rodar sem supervisão, sem travar em caixas de mensagem.

## Decisão
Infraestrutura `testes/ErpLoja.Testes/Cenarios`: interface `ICenario` (descoberta por reflexão, um banco novo por cenário, com dados de demonstração) e a classe `Usuario`, que abre telas, digita, tecla (via `ProcessCmdKey`), clica, responde janelas modais agendadas (`QuandoAbrir`) e cancela/reprova modais inesperados após 2 s. `Ui.Interceptar` desvia as caixas de mensagem nos testes. Agentes independentes, cada um numa cópia isolada do repositório (worktree) e numa área do sistema, criam cenários próprios, corrigem as falhas e entregam relatório; as correções são integradas e a bateria inteira é executada de novo.

## Consequências
- Cenários viram testes de regressão permanentes.
- Execução sem supervisão (nada bloqueia esperando clique).
- A integração das correções paralelas exige revisão de conflitos; migrações ficam proibidas aos agentes para evitar colisão de numeração.

## Alternativas consideradas
Somente teste de interface por UI Automation (frágil com foco do teclado, não paralelizável); testes manuais.
