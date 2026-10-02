# ADR-0006 — Ajuda como fonte única do manual

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Cada tela precisa de explicação própria (botão "?") e a documentação do usuário não pode divergir do sistema.

## Decisão
Os textos ficam em `Ajuda.Topicos`, indexados pela chave da tela; o "?"/F1 mostra só o tópico da tela; a bateria de testes gera `docs/12-manual-do-usuario.md` a partir deles e falha se alguma tela ou diálogo não tiver tópico.

## Consequências
- Manual sempre igual à ajuda do sistema.
- Alterar o manual = alterar `Ajuda.cs`.

## Alternativas consideradas
Manual escrito à parte (desatualiza); ajuda com índice de todos os tópicos (rejeitado pelo usuário: uma explicação por tela).
