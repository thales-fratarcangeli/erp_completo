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

---

## Vendas (PDV, pagamento, TEF, orçamentos e pedidos)

Personas: operadora de caixa apressada, gerente, vendedor novato que erra.

### Falhas encontradas e corrigidas

| # | Sintoma (como o usuário via) | Causa | Correção |
|---|---|---|---|
| V1 | F10 e depois "2" (PIX) gravava a venda **em dinheiro**, sem TEF: o "2" virava R$ 2,00 recebido | A janela de pagamento abria com o cursor no valor recebido | Abre sem cursor no campo: Enter = dinheiro, 2–5 escolhem a forma, 1 leva ao valor recebido (com dica na tela) |
| V2 | **F6 removia o item errado** (o primeiro, não o destacado) | O item lançado só ficava "selecionado"; a remoção usa a linha atual | O item lançado vira a linha atual da grade |
| V3 | Quantidade absurda ("9999…*código") **derrubava o PDV** | `decimal.Parse` sem validação | `TryParse` com limite de 0,001 a 99.999 e mensagem |
| V4 | Desconto acima do limite passava **removendo itens depois** do F5 (venda com 100% de desconto por usuário comum) | Limite só conferido no F5 | Conferido de novo no F10 |
| V5 | Faturar pedido a prazo **ignorava o limite de crédito** | Regra só no PDV | `Operacoes.ConferirLimiteCredito`, usada pelo PDV e pelos pedidos |
| V6 | Administrador vendia acima do limite sem perceber | Liberação silenciosa | Confirmação explícita "Liberar a venda acima do limite (administrador)?" |
| V7 | Pedido faturado **no boleto** gerava a conta **já paga** e não exigia cliente | Só "A prazo" era tratado como pagamento posterior | `Operacoes.RecebeDepois` (a prazo ou boleto): parcelas em aberto, cliente obrigatório, limite de crédito |
| V8 | Comprovante sem a linha do **frete** (a conta não fechava) | Linha ausente | Linha "Frete" no comprovante |
| V9 | Desconto do item **maior que o item** aceito no orçamento/pedido | Sem validação por item | Recusado ao salvar, com os dois valores |
| V10 | TEF acionado para cobrar **R$ 0,00** | Sem verificação do total | TEF não é acionado com total zero |
| V11 | Produto **sem preço** vendido de graça | Sem validação | Recusado no PDV, com orientação para cadastrar o preço |

Melhoria: ao cancelar venda/compra a prazo ou no boleto com parcela já baixada, a confirmação informa o valor já recebido.

### Cenários criados
- `VendasPdv.cs`: `VendasCaixaApressadaTeclado`, `VendasTefSimulador`, `VendasDescontoLimite`, `VendasAPrazoLimiteCredito`, `VendasSemEstoque`, `VendasProdutoSemPreco`, `VendasTotalZeroSemTef`.
- `VendasPedidos.cs`: `VendasOrcamentoAteFaturar`, `VendasOrcamentoVencido`, `VendasComprovanteComFrete`, `VendasNovatoDescontoDoItem`, `VendasPedidoNoBoleto`, `VendasCancelarAPrazoComParcelaRecebida`.

### Conhecidos, não corrigidos
- Pedido de compra "no boleto" ainda gera a conta a pagar como paga.
- Cancelar venda a prazo com parcela recebida também cancela a parcela paga (agora com aviso; estorno exige regra nova).
- Estorno de TEF recusado pelo provedor real não é tentado de novo.
- Faturar pedido no cartão/PIX não passa pelo TEF (só o PDV).
- Troco não é gravado nem impresso (exige coluna nova).

### Sugestões de usabilidade (backlog)
- Crédito disponível do cliente visível no PDV após o F2; total de vendas do dia na barra do PDV.
- Botões de notas (R$ 50 / R$ 100) no pagamento em dinheiro.
- Percentual equivalente e limite do usuário na janela de desconto.
- Ao duplicar orçamento antigo, oferecer atualizar para os preços atuais.

---

## Compras e estoque

Personas: comprador, estoquista no inventário, dono que erra digitação.

### Falhas encontradas e corrigidas

| # | Sintoma | Causa | Correção |
|---|---|---|---|
| C1 | Compra de R$ 31,00 (itens + frete) gerava contas de **R$ 30,99** | Total = soma dos itens arredondados após o rateio | Total do documento passado ao registrar a compra |
| C2 | Desconto de item maior que o item gerava **custo médio negativo** | Sem validação | `Pedidos.ValidarItens` (quantidade > 0, preço/desconto ≥ 0, desconto ≤ valor) para todos os tipos de pedido |
| C3 | XML importado com vínculo/fator errado **não podia ser reimportado** nem após cancelar a compra | Chave única e verificação de duplicidade sem considerar cancelamento | Nota de compra cancelada libera a chave; mensagem explica como corrigir |
| C4 | XML inválido (vazio, truncado, PDF renomeado) mostrava **erro técnico** | `XmlException` sem tratamento | Mensagem em português com orientação |
| C5 | Inventário ("Definir saldo") gravava **saldo errado** se houve venda com a tela aberta | Diferença calculada com o saldo da grade | `Operacoes.DefinirSaldo` lê o saldo dentro da transação |
| C6 | Saída manual maior que o saldo negativava o estoque **sem perguntar** | Sem confirmação | Confirmação |
| C7 | Cancelar compra **não avisava** que produtos já vendidos ficariam negativos e que parcelas pagas seriam canceladas | Confirmação genérica | `AvisosCancelamentoCompra` na confirmação |
| C8 | A **mesma NF entrava duas vezes** no estoque (pedido + XML, ou dois pedidos) | Sem verificação | `CompraComMesmaNota` (mesmo fornecedor e número); confirmação no recebimento e "Lançar compra" desmarcado no XML |
| C9 | Conferência do XML sugeria **produto inativo** (erro de valor inválido na grade) | Vínculos sem filtro de ativo | Apenas produtos ativos |
| C10 | Resumo da tela Estoque **ignorava a pesquisa** | Somava tudo | Resumo das linhas filtradas |
| C11 | Item com custo zero derrubava o custo médio **sem aviso** | Sem confirmação | Pergunta se é bonificação |

Nova função: **"Gerar pedido de compra"** na tela Estoque, a partir da sugestão (mínimo − saldo) dos produtos listados.

### Cenários criados
`ComprasPedidos.cs`, `ComprasXml.cs`, `ComprasCancelamento.cs`, `ComprasNotaEmDobro.cs`, `ComprasEditarPedido.cs`, `ComprasItemSemCusto.cs`, `EstoqueInventario.cs`, `EstoqueSugestaoCompra.cs` (utilitários em `ComprasUtil.cs`).

### Conhecidos, não corrigidos
- Nota importada só como registro não pode ser reimportada para lançar estoque (regra a decidir).
- Cancelar compra cancela também contas já pagas (mantido, agora com aviso).
- `Operacoes.CancelarCompra` não verifica administrador na camada de dados (só a tela).
- Pedido só com itens de custo zero + frete: o frete não entra no custo.
- XML sem duplicatas usa a soma dos itens, não o vNF da nota.
- Código de barras pode repetir com produto inativo (unicidade exige migração).

### Sugestões de usabilidade (backlog)
- Validar o desconto do item na própria grade, durante a digitação.
- Sugestão de compra agrupada por fornecedor da última compra.
- Na conferência do XML, mostrar o custo unitário resultante e a variação sobre o custo médio.
- Inventário em lote (várias contagens numa grade).

---

## Financeiro e cobrança bancária

Personas: financeiro no fechamento do mês, operador homologando com o banco, dono conferindo inadimplência.

### Falhas encontradas e corrigidas

| # | Sintoma | Causa | Correção |
|---|---|---|---|
| F1 | Cancelar conta (ou venda a prazo) com boleto deixava o **boleto ativo**: ia na remessa e cobrava o cliente | Cancelamento só alterava `contas` | `Cobranca.EncerrarBoletosDaConta` na mesma transação (cancela o emitido; pede baixa do registrado) |
| F2 | Baixa no caixa de conta com boleto registrado: **cliente podia pagar duas vezes** | Boleto continuava no banco | Baixa pede a baixa do boleto e avisa |
| F3 | Baixa com data futura, valor zero e **baixa dupla** aceitas | Sem validação | Validação na tela e nos dados |
| F4 | Estorno não limpava a forma de pagamento (RN-FIN-03) | Campo esquecido | Corrigido |
| F5 | Boleto de conta sem cliente ou valor zero aceito pela camada de dados | Só a tela filtrava | Validação nos dados |
| F6 | Boleto de conta vencida saía com **vencimento passado** (recusado pelo banco) | Sem tratamento | Exige novo vencimento; a tela pede a data |
| F7 | Retorno importado duas vezes **contava o valor de novo** | Sem controle de duplicidade | Mesmo retorno recusado; liquidação repetida ignorada |
| F8 | Retorno liquidava sem aviso conta já recebida no caixa ou cancelada | Sem verificação | Conta preservada e aviso de possível duplicidade |
| F9 | Simulador respondia "confirmado" a pedidos de baixa (boleto nunca BAIXADO) | Movimento da remessa ignorado | Responde 09 aos pedidos de baixa |
| F10 | Simulação de retorno funcionava em **conta de produção** (baixaria contas sem dinheiro) | Sem trava | Só em HOMOLOGACAO |
| F11 | Remessa de produção levava **boletos de teste** esquecidos | Sem trava | Recusada, listando os nossos números |
| F12 | Conta bancária aceitava modelo sem nosso número, espécie inválida, juros/multa fora de faixa, letras em agência/conta, modelo de tamanho absurdo (exceção) | Sem validação | `Cobranca.ValidarContaBancaria` no cadastro |
| F13 | Validador CNAB 240 **estourava exceção** com campo numérico em branco/letras | `FormatException` | Vira apontamento "Linha N: … deveria ser numérico" |
| F14 | "Gerar boletos de teste" com modelo inválido deixava **contas órfãs** | Validação tardia | Modelo validado antes |
| F15 | Depois do estorno de conta liquidada pelo banco era **impossível emitir boleto novo** | Boleto PAGO contava como ativo | Só EMITIDO/ENVIADO/REGISTRADO são ativos; estorno pede confirmação |

Melhorias: botões sem conta bancária selecionada explicam o que fazer; total vencido a receber no rodapé do Financeiro; mudar as datas atualiza a lista; baixa com valor muito diferente da conta pede confirmação.

### Cenários criados
`FinanceiroCenarios.cs`, `FinanceiroBaixaValorDigitadoErrado.cs`, `CobrancaCenarios.cs`, `CobrancaInadimplencia.cs`, `CobrancaArquivosInvalidos.cs`, `CobrancaHomologacaoAteProducao.cs`, `CobrancaVendaCanceladaComBoleto.cs`.

### Conhecidos, não corrigidos
- Ambiente de emissão do boleto (teste/produção) não é gravado (exige coluna `boletos.homologacao`).
- Não há baixa parcial nem prorrogação de vencimento/valor.
- Totais do rodapé com "Todas"/"Canceladas" somam canceladas pelo valor nominal.
- Importar retorno não trata erro de leitura do arquivo.

### Sugestões de usabilidade (backlog)
- Pesquisa por cliente/fornecedor e relatório de inadimplência por faixa de atraso.
- Ações "Prorrogar vencimento" e "Baixa parcial"; coluna "Boleto" no Financeiro.
- Juros e multa sugeridos na baixa de conta vencida.
- Contas recorrentes ("duplicar para os próximos N meses").

---

## Fiscal e cadastros

Personas: auxiliar fiscal, atendente cadastrando às pressas com dados errados, administrador gerenciando usuários.

### Falhas encontradas e corrigidas

| # | Sintoma | Causa | Correção |
|---|---|---|---|
| X1 | Nota de saída emitida com **CFOP de entrada** (1102) ou 5102 para cliente de outro estado | Só o tamanho do CFOP era conferido | Verificação de origem e UF (RN-FIS-10), com o CFOP sugerido na mensagem |
| X2 | Erros de digitação emitidos: CPF/CNPJ inválido, valores e descontos negativos/maiores que o item, NFC-e de entrada, nota de venda cancelada | Validação incompleta | Barrados na emissão |
| X3 | **Segunda nota** para a mesma venda/compra | Regra só na tela | Recusada ao salvar |
| X4 | **Qualquer usuário cancelava** nota emitida; cancelar não conferia a situação | Sem verificação | Só administrador (RN-FIS-11); só nota EMITIDA |
| X5 | Série errada ao trocar o modelo para NFC-e | Série fixa | Série do modelo |
| X6 | Fechar o editor de nota **perdia a digitação** sem aviso | Sem controle | Confirmação |
| K1 | **Pessoa duplicada** (mesmo CPF/CNPJ com outra pontuação) | Sem verificação | Recusada, indicando o cadastro existente (RN-CAD-10) |
| K2 | Produto com EAN de DV errado ou repetido, NCM curto, CST com letra, redução > 100% | Sem validação | Recusados (RN-CAD-11) |
| K3 | Salvar produto **arredondava o custo médio** (5,1234 → 5,12) e o custo era editável | Campo com 2 casas | 4 casas; custo só como custo inicial sem movimentação |
| K4 | O sistema podia **ficar sem administrador** | Sem verificação | Bloqueado (RN-USU-07) |
| K5 | Senha de 1 caractere aceita | A validação rodava sobre o hash | Mínimo de 4 sobre a senha digitada |
| K6 | "Valor repetido" sem dizer qual campo | Mensagem genérica | Nome do campo e foco nele |
| K7 | Excluir categoria em uso: mensagem vaga | — | Quantos produtos e o que fazer |

### Cenários criados
`CadastrosApoio.cs` (utilitários), `CadastrosAtendenteCadastraPessoa`, `CadastrosProdutoComDadosErrados`, `CadastrosAdministradorGerenciaUsuarios`, `CadastrosCategoriasECustoDoProduto`, `FiscalAuxiliarEmiteNotaDeVenda`, `FiscalNotaAvulsaComErros`, `FiscalEditorTrocaModelo`, `FiscalEditorBuscaComF3`, `FiscalRascunhoDeVendaCancelada`.

### Conhecidos, não corrigidos
- Desconto da venda não é rateado nos itens da nota (impostos sobre o valor bruto).
- Emissão sem chave de acesso (empresa sem CNPJ/UF) não avisa.
- Numeração por modelo, não por série.
- Exportação CSV arredonda quantidades a 2 casas; estoque mínimo com 2 casas.

### Sugestões de usabilidade (backlog)
- Confirmar ao trocar de registro com edição pendente nos cadastros.
- Máscaras de CPF/CNPJ/CEP; busca sem acento nos cadastros.
- "Emitir NF" direto em Vendas realizadas; células com erro destacadas no editor de nota.

---

## Sistema, navegação e gestão

Personas: dono que abre muitas telas, funcionário novo que não sabe onde fica nada, administrador configurando a loja.

### Falhas encontradas e corrigidas

| # | Sintoma | Causa | Correção |
|---|---|---|---|
| S1 | Tela de administrador (Log) **abria para usuário comum** por chamada direta | `MainForm.Abrir` não verificava o perfil | Bloqueio com mensagem e registro de ACESSO no log |
| S2 | Configurações gravavam **valores inválidos** (texto em dias de log, número 0, nome da loja em branco) e o cursor ia para aba escondida | Validação incompleta e na tela | `Parametros.Validar` na camada de dados; a tela abre a aba do campo inválido |
| S3 | E-mail de teste sem chave pedia a coisa errada | Ordem das verificações | Verifica chave e remetente primeiro |
| S4 | Reenvio de código que falhava **invalidava o código já recebido** | Código gravado antes do envio | Gravado só após envio bem-sucedido |
| S5 | Datas invertidas em Relatórios: relatório vazio **sem aviso** | Sem validação | Mensagem e aviso no rodapé |
| S6 | Nome da loja não mudava no cabeçalho após salvar | Sem notificação | Evento `ParametrosSalvos` |
| S7 | Localizador não entendia frases ("como cadastrar cliente", "trocar minha senha"); "vender" deixava o PDV em 12º | Todos os termos tinham de casar literalmente | Ignora palavras de ligação; radicais/plurais; ranking por termos casados |
| S8 | Localizador listava a si mesmo | — | Removido da lista |
| S9 | "Produtos sem venda" listava **insumos** | Sem filtro | Insumos excluídos |
| S10 | Troca de senha em Minha conta não aparecia como ACESSO no log | Só DADOS | Registrada como ACESSO |

### Cenários criados
`SistemaNavegacao.cs` (`SistemaDonoAbreMuitasTelas`, `SistemaFecharComVendaNoPdv`, `SistemaUsuarioComumNaoVeTelasDeAdmin`, `SistemaFuncionarioProcuraTela`, `SistemaAjudaEmTodasAsTelas`), `SistemaBuscaComoFala.cs`, `SistemaConfiguracoes.cs` (`SistemaAdminConfiguraLoja`, `SistemaFuncionarioMinhaConta`, `SistemaLoginERecuperacao`, `SistemaRecuperacaoDeSenha`), `SistemaLogDeAtividades.cs`, `GestaoPainelERelatorios.cs` (`GestaoDonoConfereOPainel`, `GestaoLojaNovaSemDados`, `GestaoGerenteTiraRelatorios`).

### Conhecidos, não corrigidos
- Ctrl+Tab dentro de Configurações troca a aba da janela principal (Ctrl+PageDown troca as abas de Configurações).
- Ao fechar a janela principal, abas anteriores ao PDV já foram fechadas quando a venda em andamento impede o fechamento.
- Sem bloqueio após várias senhas erradas; a recuperação revela se o usuário existe (decisão de segurança).

### Sugestões de usabilidade (backlog)
- Obrigar a troca da senha padrão do admin no primeiro acesso (**implementado na revisão de interface**, ver abaixo).
- Telas recentes na tela inicial e no localizador vazio (**implementado**).
- Log: atualizar ao trocar filtro; "o que este usuário fez hoje".
- Painel: clicar no cartão abre o relatório do período.
- Configurações: marcar abas com alterações não salvas.

---

## Totais

| Área | Falhas corrigidas | Cenários |
|---|---:|---:|
| Produção e logística | 12 | 10 |
| Vendas | 11 | 13 |
| Compras e estoque | 11 | 12 |
| Financeiro e cobrança | 15 | 12 |
| Fiscal e cadastros | 13 | 8 |
| Sistema, navegação e gestão | 10 | 14 |
| **Total** | **72** | **69** (+1 cenário de exemplo) |

Bateria após a integração: **898 verificações, 898 aprovadas**; com a revisão de interface da versão 1.2.0: **905 verificações, 905 aprovadas** ([08](08-relatorio-de-testes.md)).
