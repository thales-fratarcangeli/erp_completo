# ADR-0023 — Ordens de serviço genéricas, faturamento separado da etapa e histórico

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-03 |

## Contexto
A [análise do ERP da gráfica (outro ERP)](../16-analise-erp-grafica.md) mostrou que a ordem de serviço é a função mais usada
do outro sistema (10.584 usos, 44.126 OS) e a maior lacuna do ERP Loja, que só tinha ordem de *produção* por ficha
técnica. Os dados também revelaram problemas de processo: 83% das OS sem situação, 45% das entregas depois do prazo,
63% sem registro de entrega e datas com anos como 201 ou 1201. A OS do outro ERP tem 113 colunas, muitas específicas de
gráfica (papel, cores frente/verso, acabamentos, pré-impressão) e várias nunca preenchidas.

## Decisão
- **OS genérica** (`Data/OrdensServico`, tela `OrdensServicoForm` em Vendas): cliente, descrição do serviço, prazo, valor,
  responsável e observações. **Sem cadastros específicos de gráfica** nesta versão; a especificação técnica vai na
  descrição. Serve para gráfica, assistência técnica, oficina ou qualquer trabalho sob encomenda.
- **Etapa sempre obrigatória**: Aberta → Aprovada → Em execução → Pronta → Entregue, mais Cancelada (só pelo
  cancelamento, com motivo). A coluna `etapa` tem `NOT NULL`, padrão `ABERTA` e `CHECK` com os valores válidos. Avançar é
  livre; voltar exige motivo. Entregar grava `entregue_em`, o que permite medir pontualidade.
- **Prazo obrigatório**, sugerido por `os_prazo_dias`, nunca antes da abertura e dentro das datas plausíveis (`Datas`). A
  situação do prazo (no prazo, vence hoje, atrasada, entregue com atraso) é **calculada** em SQL (`SqlSituacao`), não gravada.
- **Faturamento independente da etapa**: `Faturar` gera as contas a receber (parcelas mensais, `contas.os_id`) e grava
  `faturada_em`; pode acontecer antes ou depois da entrega, uma única vez. "Faturada" não é uma etapa. Depois de
  faturada, o valor não muda; para cancelar a OS, as contas dela precisam ser canceladas antes.
- **Histórico** (`os_historico`): abertura, cada mudança de etapa (com motivo), mudança de prazo, faturamento e
  cancelamento, com usuário e data/hora, gravados na mesma transação da mudança.
- Estrutura criada pela migração 16 (`ordens_servico`, `os_historico`, `contas.os_id`, parâmetro `os_prazo_dias`).
  Regras em RN-OS-01 a RN-OS-07.

## Consequências
- Atende o que a gráfica realmente usa (fluxo, prazos, entrega, faturamento, ficha impressa) sem copiar as 113 colunas.
- O painel de prazos e os alertas da tela inicial ([ADR-0025](0025-alertas-na-tela-inicial.md)) saem dos mesmos campos.
- Faturar na entrega, antes (sinal) ou depois (fechamento mensal) fica a critério da loja; o alerta "entregues sem
  faturar" evita esquecimentos.
- Ainda não há: itens/insumos da OS, entrega parcial, vínculo com nota fiscal ou boleto, vendedor e comissão, cadastros de
  papel/cores/acabamentos e importação do histórico do outro ERP. Ficam no roteiro de [16](../16-analise-erp-grafica.md).

## Alternativas consideradas
- **Copiar a OS gráfica do outro ERP** (cadastros de papel, cores, espécies, acabamentos): específica demais para o ERP de
  loja e com campos que a própria gráfica não preenche.
- **"Faturada" como última etapa do fluxo**: obrigaria entregar antes de faturar e misturaria situação do trabalho com
  situação financeira; com campo próprio, as duas coisas são consultadas separadamente.
- **Reaproveitar a ordem de produção**: ela baixa insumos por ficha técnica para fabricar estoque; não tem cliente,
  prazo de entrega nem faturamento.
- **Etapa opcional ou texto livre**: foi exatamente o que levou às 83% de OS sem situação no outro sistema.
- **Histórico só no log de atividades**: o log é técnico e tem retenção configurável; o histórico da OS faz parte do
  registro do negócio e aparece na própria tela.
