# 14 — Testes exploratórios por agentes (versão 1.2.0)

| | |
|---|---|
| **Documento** | Relatório consolidado dos testes exploratórios que simularam usuários reais |
| **Sistema** | ERP Loja 1.2.0 |
| **Método** | [ADR-0018](adr/0018-cenarios-de-uso-automatizados.md): agentes paralelos, um por área, cada um numa cópia isolada do repositório, operando as telas por cenários automatizados (`testes/ErpLoja.Testes/Cenarios`), corrigindo as falhas e deixando o cenário como teste de regressão |

Cada seção traz: falhas encontradas (sintoma → causa → correção), cenários criados, problemas conhecidos não corrigidos
e sugestões de usabilidade. As regras de negócio afetadas foram atualizadas em [04](04-regras-de-negocio.md) e a ajuda
das telas em `ErpLoja/UI/Ajuda.cs` (manual em [12](12-manual-do-usuario.md)).

---

## Produção e logística

Personas: encarregado da produção, responsável pela logística montando viagens, dono conferindo custos.

### Falhas encontradas e corrigidas

| # | Sintoma (como o usuário via) | Causa | Correção |
|---|---|---|---|
| P1 | Produzir mais que o planejado deixava insumo **negativo**, mesmo com `producao_sem_estoque = BLOQUEAR` | A falta era conferida antes de pedir a quantidade e usava o previsto da OP | `Producao.Faltas(op, produzido)` proporcional ao produzido; a tela pede a quantidade antes; `Concluir` também bloqueia na camada de dados |
| P2 | Ficha técnica **circular** aceita (massa usa torta, torta usa massa) | Só era barrado o produto como insumo de si mesmo | Verificação recursiva ao salvar a ficha, citando o insumo |
| P3 | Perda negativa aceita (custo e necessidade menores que o real) | Sem limite | Perda entre 0 e 100% |
| P4 | Fichas técnicas **descartava edição sem aviso** ao trocar de produto ou fechar | Sem controle de alterações | Confirmação de alteração não salva; "Não" volta para o produto em edição |
| L1 | Frete com autônomo ia para **em trânsito/entregue sem CIOT** (ou com CIOT cancelado) | Status sem validação | `Fretes.MudarStatus` bloqueia e orienta |
| L2 | Frete **cancelado** gerava contas e aceitava declarar CIOT | Sem validação do status | Bloqueado |
| L3 | Frete sem valor dizia "0 conta(s) gerada(s)" e travava a geração para sempre | Marcava como gerado mesmo sem contas | Erro orientando a informar o valor |
| L4 | Resposta de sucesso da Pamcard **sem número de CIOT** deixava o frete DECLARADO sem CIOT | Interpretação aceitava sucesso vazio | Tratado como falha com mensagem |
| L5 | Motivo do cancelamento do CIOT validado só na tela | Regra fora da camada de dados | Validação (mín. 15 caracteres) também em `Transporte.cs` |
| L6 | Editor do frete permitia mudar valores/transportador **depois de gerar as contas**; rota editável com CIOT declarado; pessoa/veículo desativado **sumia** e o vínculo era apagado ao salvar | Campos sem trava; listas só com ativos | Campos travados com nota explicativa; listas mantêm o vínculo atual |
| L7 | Placa com hífen duplicava veículo ("ABC-1D23" e "ABC1D23"); RENAVAM e RNTRC aceitavam qualquer texto | Sem validação de formato | Placa sem hífen; RENAVAM 9–11 dígitos; RNTRC 8–9 dígitos (veículo e pessoa) |
| L8 | Botões **não faziam nada** sem linha selecionada (abrir, imprimir, cancelar CIOT, imprimir OP) | Retorno silencioso | Aviso "Selecione…" |

Melhorias: confirmação de peso acima da capacidade do veículo; lembrete de encerrar o CIOT ao marcar "Entregue".

### Cenários criados
- `ProducaoCenarios.cs`: `ProducaoEncarregadoMontaFicha`, `ProducaoFichaCircular`, `ProducaoEncarregadoConduzOrdem`, `ProducaoFichaNaoSalvaAvisa`.
- `LogisticaCenarios.cs`: `LogisticaMontaViagemAutonomo`, `LogisticaCancelamentos`, `LogisticaPamcard` (transporte HTTP falso, sem internet), `LogisticaCadastraVeiculo`, `LogisticaEditorFrete`, `LogisticaBotoesSemSelecao`.

### Conhecidos, não corrigidos
- A placa com hífen é recusada, mas não normalizada automaticamente (exigiria normalização genérica no `CrudForm`).
- RENAVAM sem conferência do dígito verificador.
- CNH vencida do motorista não é verificada ao declarar o CIOT.
- Editar o frete após cancelar o CIOT perde o registro de que houve CIOT cancelado (exige histórico — mudança de esquema).
- Alterar a ficha técnica não recalcula OPs já planejadas.

### Sugestões de usabilidade (backlog)
- Botão "Liberar viagem" (declara o CIOT se preciso e marca em trânsito); destacar na lista os fretes com CIOT pendente.
- OP: após concluir, oferecer "ver OP concluída"; prévia de consumo/falta enquanto digita a quantidade.
- Ficha técnica: perda editável na linha de entrada; margem também em R$.
- Veículos: campo proprietário só quando "Frota própria" estiver desmarcado.
