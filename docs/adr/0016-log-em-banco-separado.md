# ADR-0016 — Log de atividades em banco SQLite separado

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-02 |

## Contexto
Pedido: "cada ação tem que gerar log... os logs pode colocar dentro de um novo banco sqlite". O log cresce rápido e não pode deixar o banco da loja pesado nem atrapalhar backups e migrações.

## Decisão
Gravar o log em `<banco>_log.db`, ao lado do banco da loja, com uma única tabela `log` (estrutura criada sob demanda, modo WAL). Pontos de captura: `DbSessao` (gravações confirmadas, resumidas por transação, classificadas por expressão regular sobre o SQL), `Ui.Botao`/`Ui.Erro`/`Info`/`Confirmar`/`Feito`/`Aviso`, janela principal (telas), login/recuperação, inicialização, backups, migrações e exceções não tratadas. O log nunca lança exceção. Retenção por parâmetro.

## Consequências
- Cobertura ampla sem espalhar chamadas de log pelo código: telas novas que usam `Ui.Botao` e `DbSessao` já são registradas.
- Backups do banco da loja não incluem o log (o arquivo do log pode ser copiado à parte).
- O detalhe de gravações não contém valores (privacidade), apenas tabelas e quantidades.

## Alternativas consideradas
Tabela de log no próprio banco da loja (incha o banco e os backups); arquivo texto (difícil de filtrar e consultar); log por gatilhos SQL (não sabe usuário/tela e não registra ações sem gravação).
