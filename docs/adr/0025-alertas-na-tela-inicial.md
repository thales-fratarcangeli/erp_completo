# ADR-0025 — Alertas na tela inicial com abertura da tela já filtrada

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-03 |

## Contexto
A [análise do outro ERP](../16-analise-erp-grafica.md) mostrou atrasos recorrentes sem nenhum aviso (45% das entregas
depois do prazo). Com as ordens de serviço ([ADR-0023](0023-ordens-de-servico-genericas.md)) e o acompanhamento de
cobrança, passou a haver pendências diárias que a pessoa precisa ver sem ter de lembrar de abrir cada tela: OS
atrasadas ou com entrega hoje, OS prontas ou entregues sem faturar, contas a receber vencidas, promessas de pagamento
não cumpridas e estoque baixo.

## Decisão
- `Data/Alertas.Listar()` calcula os alertas **na hora em que a tela inicial aparece** (ao abrir o sistema e sempre que
  a pessoa volta ao Início), com consultas de contagem sobre os dados atuais. Nada é gravado: não há tabela de alertas.
- Cada alerta é um `record Alerta(Texto, Tela, Filtro, Grave)`: o texto com a quantidade, a chave da tela em
  `Navegacao`, um código de filtro e se é grave. Os graves vêm primeiro e ganham ponto vermelho; os demais, âmbar.
- Clicar no alerta abre a tela pelo caminho normal (`MainForm.Abrir`, com a proteção de dados não salvos) e, se ela
  implementa `IFiltroInicial`, chama `AplicarFiltro(filtro)`. Implementam a interface: `OrdensServicoForm`,
  `FinanceiroForm` e `EstoqueForm`. A abertura pelo alerta fica no log de atividades.
- Erro de banco ao calcular os alertas não impede usar o sistema (a lista fica vazia). Sem pendências, o bloco some.

## Consequências
- A pessoa vê o que precisa de atenção hoje e chega à lista que resolve em um clique.
- Telas novas entram nos alertas implementando `IFiltroInicial` e acrescentando o cálculo em `Alertas.Listar()`.
- Os números não se atualizam sozinhos enquanto uma tela está aberta; refletem o momento em que o Início foi mostrado.
- As consultas rodam a cada volta ao Início; com índices em `ordens_servico(etapa, prazo)` e o volume de uma loja, o
  custo é pequeno, mas deve ser revisto se a base crescer muito.

## Alternativas consideradas
- **Notificações do Windows / avisos periódicos em segundo plano**: interrompem o trabalho, exigem temporizador e
  controle de "já avisado", e um operador de caixa não precisa ver alertas financeiros.
- **Painel de alertas em tela própria** (ou dentro do painel gerencial): mais um lugar para lembrar de abrir; o
  objetivo era justamente mostrar sem pedir. O painel gerencial (F5) continua com os indicadores.
- **Alertas gravados em tabela** (gerados por eventos e marcados como lidos): mais estrutura, risco de ficar
  desatualizado em relação aos dados; calcular na hora mantém uma única fonte de verdade.
- **Abrir a tela sem filtro**: a pessoa teria de repetir o filtro manualmente; a interface `IFiltroInicial` mantém o
  conhecimento de cada filtro dentro da própria tela.
