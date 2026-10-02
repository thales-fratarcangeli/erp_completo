# 04 — Regras de negócio

| | |
|---|---|
| **Documento** | Catálogo de regras de negócio (RN) |
| **Sistema** | ERP Loja 1.2.0 (banco v12) |
| **Status** | Vigente |
| **Rastreabilidade** | Requisitos em [02](02-especificacao-de-requisitos.md); casos de uso em [03](03-casos-de-uso.md); verificação em [08](08-relatorio-de-testes.md) |

Este documento é a **especificação do comportamento** do sistema: como ele decide e calcula. Se o sistema divergir daqui,
um dos dois está errado e deve ser corrigido. Valores entre crases (`assim`) são parâmetros configuráveis
(ver [RN-PAR](#parâmetros-rn-par)).

Sumário: [Cadastros](#cadastros-rn-cad) · [Estoque](#estoque-rn-est) · [Custo](#custo-rn-cus) · [Vendas/PDV](#vendas-e-pdv-rn-ven) ·
[TEF](#tef-rn-tef) · [Orçamentos e pedidos](#orçamentos-e-pedidos-rn-ped) · [Compras](#compras-rn-com) · [XML de NF-e](#importação-de-xml-rn-xml) ·
[Produção](#produção-rn-pro) · [Transporte e CIOT](#transporte-e-ciot-rn-tra) · [Financeiro](#financeiro-rn-fin) ·
[Cobrança bancária](#cobrança-bancária-rn-cob) · [Fiscal](#fiscal-rn-fis) · [Reforma tributária](#reforma-tributária-rn-ref) ·
[Impressão](#impressão-rn-imp) · [Usuários e acesso](#usuários-e-acesso-rn-usu) · [Banco de dados](#banco-de-dados-rn-bd) · [Log de atividades](#log-de-atividades-rn-log) ·
[Navegação e interface](#navegação-e-interface-rn-nav) · [Instalação](#instalação-rn-ins) · [Parâmetros](#parâmetros-rn-par)

---

## Cadastros (RN-CAD)

| ID | Regra |
|---|---|
| RN-CAD-01 | Clientes, fornecedores, transportadores e motoristas são **um único cadastro de pessoas**. Uma pessoa pode acumular papéis (ex.: cliente e fornecedor); pelo menos um papel é obrigatório. |
| RN-CAD-02 | CPF (11 dígitos) e CNPJ (14 dígitos) são validados pelos dígitos verificadores; sequências repetidas (ex.: 111.111.111-11) são inválidas. O campo é opcional. |
| RN-CAD-03 | Motorista exige CPF e CNH; validade da CNH no formato dd/mm/aaaa. Transportador deve ter RNTRC para declarar CIOT. |
| RN-CAD-04 | Registros com histórico (vendas, compras, notas, fretes, movimentações) **não são excluídos**: o sistema oferece **inativar**. Inativos somem das listas de seleção e permanecem nos históricos. |
| RN-CAD-05 | Código de produto, username, e-mail de usuário e placa de veículo são únicos (sem diferenciar maiúsculas). |
| RN-CAD-06 | Produto tem tipo: **Mercadoria** (revenda), **Produto acabado** (fabricado) ou **Insumo** (matéria-prima). Salvar uma ficha técnica torna a mercadoria um produto acabado. |
| RN-CAD-07 | Saldo e custo do produto **não são editáveis** no cadastro (ver RN-EST e RN-CUS); o custo só pode ser informado como custo inicial enquanto o produto não tem movimentação de estoque, e é exibido/gravado com 4 casas (editar outro campo não o arredonda). Categoria com produtos não é excluída. CST padrão de produto novo = `cst_padrao`; CST IBS/CBS = `cst_ibscbs_padrao`; cClassTrib = `cclass_trib_padrao`. |
| RN-CAD-08 | Veículo de terceiro exige proprietário (pessoa com papel transportador). Placa no padrão Mercosul (ABC1D23) ou antigo (ABC1234). |
| RN-CAD-09 | Limite de crédito do cliente: 0 = sem limite (ver RN-VEN-08). |
| RN-CAD-10 | CPF/CNPJ não se repete entre pessoas (comparação só pelos dígitos); o aviso diz qual cadastro já usa o documento. |
| RN-CAD-11 | Código de barras do produto: EAN/GTIN de 8, 12, 13 ou 14 dígitos com DV módulo 10 válido, sem repetir o código de barras (ou o código interno) de outro produto. NCM, se informado, tem 8 dígitos; CST com 2 dígitos ou CSOSN com 3; reduções de IBS/CBS de 0 a 100%. |

## Estoque (RN-EST)

| ID | Regra |
|---|---|
| RN-EST-01 | O saldo só muda por **movimentação**. Toda movimentação grava data/hora, tipo, quantidade (positiva), **saldo resultante**, origem e usuário. |
| RN-EST-02 | Efeitos: venda (SAIDA −), cancelamento de venda (ENTRADA +), compra/XML (ENTRADA + qtd × fator), cancelamento de compra (SAIDA −), consumo de OP (SAIDA −), produção concluída (ENTRADA +), ajuste entrada/saída (±), inventário (AJUSTE = contado − saldo). |
| RN-EST-03 | Saldo arredondado em 3 casas (venda fracionada, ex.: kg). |
| RN-EST-04 | O saldo **pode ficar negativo** quando a operação sem estoque é permitida/confirmada (RN-VEN-03, RN-PRO-05) ou ao cancelar compra já vendida. |
| RN-EST-05 | Ajuste exige motivo; ajuste que não altera o saldo não é gravado. |
| RN-EST-06 | Situação: SEM ESTOQUE (saldo ≤ 0), BAIXO (saldo ≤ mínimo), OK. Sugestão de compra = máx(mínimo − saldo, 0). Valor em estoque = máx(saldo, 0) × custo médio. |

## Custo (RN-CUS)

| ID | Regra |
|---|---|
| RN-CUS-01 | O custo do produto é o **custo médio ponderado**: se o saldo antes da entrada é > 0, `(saldo × custo + qtd × custo_entrada) ÷ (saldo + qtd)` (4 casas); senão, o custo da entrada. Vale para compras e produção. |
| RN-CUS-02 | Cancelar compra **não** recalcula o custo retroativamente. |
| RN-CUS-03 | Cada item vendido guarda o custo médio do momento; o lucro bruto usa esse custo. |
| RN-CUS-04 | Pedido de compra: frete e desconto geral são rateados pelo valor dos itens: `custo unit. = total do item × (total do pedido ÷ soma dos itens) ÷ qtd`. |
| RN-CUS-05 | XML: custo efetivo do item = vProd − vDesc + vFrete + vSeg + vOutro + vIPI + vICMSST, dividido por qCom × fator. |
| RN-CUS-06 | Produção: custo real = Σ(consumo × custo médio do insumo) + custo adicional da ficha × (produzido ÷ rendimento). |
| RN-CUS-07 | Margem exibida = (preço − custo) ÷ custo × 100. |

## Vendas e PDV (RN-VEN)

| ID | Regra |
|---|---|
| RN-VEN-01 | Item: `total = arredondar(qtd × preço, 2)`. Total da venda = máx(subtotal − desconto + frete, 0); desconto ≤ subtotal. |
| RN-VEN-02 | **Desconto máximo**: usuário comum até `desconto_maximo_pct`% do subtotal; acima, só administrador. Vale para PDV, orçamentos e pedidos de venda. |
| RN-VEN-03 | **Estoque insuficiente** (quantidade acumulada do produto na venda > saldo) segue `venda_sem_estoque`: PERGUNTAR, PERMITIR ou BLOQUEAR. |
| RN-VEN-04 | Entrada "N*código" (ou "Nxcódigo") lança N unidades; repetir o produto soma na mesma linha. |
| RN-VEN-05 | Dinheiro: valor recebido, se informado, ≥ total; troco = recebido − total. |
| RN-VEN-06 | À vista (dinheiro, PIX, cartões): conta a receber já **PAGA** na data. A prazo: parcelas (RN-FIN-02); 1º vencimento = hoje + `prazo_vencimento_dias`; até `max_parcelas`. |
| RN-VEN-07 | Venda a prazo exige cliente identificado. |
| RN-VEN-08 | **Limite de crédito**: com limite > 0, venda a prazo só se `total ≤ limite − contas a receber em aberto do cliente`; acima disso só administrador. |
| RN-VEN-09 | Venda com total zero não gera conta. O comprovante é não fiscal. |
| RN-VEN-10 | **Cancelamento** (só administrador, só venda FINALIZADA): devolve itens ao estoque, cancela as contas a receber da venda, reabre o pedido de origem e estorna as transações TEF confirmadas. A nota fiscal não é cancelada automaticamente. |
| RN-VEN-11 | Impressão do comprovante ao finalizar segue `imprimir_cupom` (PERGUNTAR, SEMPRE, NUNCA); em dinheiro, abre a gaveta se `impressora_gaveta` = SIM. Falha de impressão não desfaz a venda. |

## TEF (RN-TEF)

| ID | Regra |
|---|---|
| RN-TEF-01 | Com `tef_ativo` = SIM, pagamentos em débito, crédito e PIX passam pelo provedor `tef_provedor` antes de gravar a venda. |
| RN-TEF-02 | **Duas fases**: transação aprovada fica PENDENTE; com a venda gravada vira CONFIRMADA; se a gravação falhar, vira DESFEITA. Negadas ficam NEGADA. Toda transação é registrada. |
| RN-TEF-03 | Provedores reais não instalados/homologados **não simulam**: recusam com a explicação do que falta. |
| RN-TEF-04 | Cancelamento da venda estorna as transações confirmadas (CANCELADA). |
| RN-TEF-05 | Comprovantes: via do cliente sempre impressa (quando há impressão); via da loja se `tef_imprimir_via_loja` = SIM. Simulador imprime "TRANSAÇÃO SIMULADA - SEM VALOR". |

## Orçamentos e pedidos (RN-PED)

| ID | Regra |
|---|---|
| RN-PED-01 | Item: `total = arredondar(qtd × preço − desconto do item, 2)`. Total = máx(subtotal − descontos − desconto geral + frete, 0); descontos não podem zerar um total com itens. |
| RN-PED-02 | Só documentos ABERTOS podem ser alterados. Orçamento ABERTO com validade vencida é exibido como VENCIDO (não gravado) e pode ser aprovado com confirmação. |
| RN-PED-03 | Transições: Orçamento ABERTO → APROVADO (gera pedido de venda) / RECUSADO / CANCELADO. Pedido de venda ABERTO → FATURADO (gera venda) / CANCELADO. Pedido de compra ABERTO → RECEBIDO (gera compra) / CANCELADO. Cancelar a venda/compra gerada reabre o pedido. |
| RN-PED-04 | Faturar: aplica RN-VEN-03; venda com desconto = descontos dos itens + geral, e frete. "Salvar e faturar"/"Salvar e receber" concluem a partir do editor. |
| RN-PED-05 | Orçamentos e pedidos não mexem em estoque nem financeiro até faturar/receber. Validade padrão = hoje + `validade_orcamento_dias`. |

## Compras (RN-COM)

| ID | Regra |
|---|---|
| RN-COM-01 | Toda entrada de mercadoria acontece por **pedido de compra recebido** ou **importação de XML** (um ponto de entrada por origem). |
| RN-COM-02 | Entrada gera estoque, recálculo do custo médio e contas a pagar (à vista: paga; a prazo: parcelas ou duplicatas). |
| RN-COM-03 | Cancelamento (só administrador): retira o estoque, cancela as contas a pagar e reabre o pedido de origem. |

## Importação de XML (RN-XML)

| ID | Regra |
|---|---|
| RN-XML-01 | Aceita NF-e/NFC-e (com ou sem `nfeProc`). A chave de acesso identifica a nota: **não importa duas vezes**. |
| RN-XML-02 | Fornecedor localizado pelo CNPJ (só dígitos); inexistente é cadastrado; existente recebe o papel fornecedor. |
| RN-XML-03 | Vínculo de cada item: (1) vínculo memorizado fornecedor+código, com fator; (2) EAN/GTIN válido (8, 12, 13 ou 14 dígitos; "SEM GTIN" ignorado) igual ao código de barras ou código do produto; (3) escolha do usuário ou "cadastrar novo". |
| RN-XML-04 | Estoque entra `qCom × fator`. Custo conforme RN-CUS-05. |
| RN-XML-05 | Produto novo: código = EAN válido (se livre) ou `F{fornecedor}-{código}`; NCM e unidade da nota; CST 102; preço de venda zero (usuário avisado). |
| RN-XML-06 | Contas a pagar: com forma "A prazo" e duplicatas, uma conta por duplicata (vencimento e valor da nota); sem duplicatas, parcela única em hoje + `prazo_vencimento_dias`. |

## Produção (RN-PRO)

| ID | Regra |
|---|---|
| RN-PRO-01 | Ficha técnica: rendimento > 0; ao menos um insumo; quantidades > 0; sem insumo repetido; o produto não pode ser insumo de si mesmo. |
| RN-PRO-02 | Nova OP exige ficha técnica. Necessidade de cada insumo = `qtd da ficha × (qtd a produzir ÷ rendimento) × (1 + perda%)`. |
| RN-PRO-03 | Situações: PLANEJADA → EM_PRODUCAO → CONCLUIDA; PLANEJADA/EM_PRODUCAO → CANCELADA. OP concluída ou cancelada não muda mais. |
| RN-PRO-04 | Concluir: consumo real = previsto × (produzido ÷ planejado); baixa os insumos (SAIDA), entra o produto (ENTRADA), recalcula o custo médio com o custo real (RN-CUS-06) e grava custo total e unitário na OP. |
| RN-PRO-05 | Insumo insuficiente ao concluir segue `producao_sem_estoque` (PERGUNTAR, PERMITIR, BLOQUEAR). |

## Transporte e CIOT (RN-TRA)

| ID | Regra |
|---|---|
| RN-TRA-01 | Frete: modalidade PROPRIO (frota própria) ou CONTRATADO (transportador terceiro, obrigatório). Adiantamento ≤ frete. |
| RN-TRA-02 | **CIOT obrigatório** (regra simplificada ANTT): frete CONTRATADO com transportador pessoa física (CPF — TAC/equiparado). Transportadora (CNPJ) e frota própria: não obrigatório. |
| RN-TRA-03 | Declarar CIOT exige: transportador com CPF/CNPJ válido e RNTRC (dele ou do veículo); motorista com CPF válido; veículo; origem e destino (cidade/UF); valor > 0; adiantamento ≤ frete. |
| RN-TRA-04 | A declaração usa a administradora `ciot_administradora` (Pamcard ou simulador; demais listadas sem integração). Com CIOT declarado, valores e envolvidos ficam travados. |
| RN-TRA-05 | Situações do CIOT: NAO_SE_APLICA, PENDENTE → DECLARADO → ENCERRADO / CANCELADO (cancelamento com motivo de ao menos 15 caracteres). |
| RN-TRA-06 | Situações do frete: ABERTO → EM_TRANSITO → ENTREGUE; ABERTO/EM_TRANSITO → CANCELADO. Frete com CIOT declarado só é cancelado após cancelar o CIOT; cancelar o frete cancela as contas abertas dele. |
| RN-TRA-07 | Contas a pagar do frete contratado (uma única vez): adiantamento (vencimento na data do frete) e saldo = frete − adiantamento + pedágio + outras despesas (vencimento na previsão de entrega). |

## Financeiro (RN-FIN)

| ID | Regra |
|---|---|
| RN-FIN-01 | Contas A RECEBER / A PAGAR com situação ABERTA, PAGA ou CANCELADA; VENCIDA é calculada (aberta e vencimento < hoje) com dias de atraso. |
| RN-FIN-02 | **Parcelamento**: parcela = arredondar(total ÷ n, 2); a última recebe a diferença de centavos; vencimentos mensais a partir do 1º; descrição "… parcela k/n". |
| RN-FIN-03 | Baixa: só conta aberta; registra data, valor pago (pode diferir: juros/desconto) e forma. Estorno: só paga → aberta (limpa pagamento). Cancelamento: só aberta. |
| RN-FIN-04 | Fluxo realizado e "entradas × saídas" usam **valor pago** na **data do pagamento**; previsto usa contas abertas por vencimento. |

## Cobrança bancária (RN-COB)

| ID | Regra |
|---|---|
| RN-COB-01 | Boleto só para conta a receber **aberta** com cliente; no máximo um boleto ativo por conta. |
| RN-COB-02 | Nosso número = sequência da conta bancária + 1, com `nosso_numero_digitos` dígitos. |
| RN-COB-03 | **Código de barras** (44 posições, Febraban): banco(3) + moeda 9 + DV + fator de vencimento(4) + valor em centavos(10) + campo livre(25). DV geral por módulo 11 (pesos 2–9); resultados 0, 10 e 11 viram 1. |
| RN-COB-04 | **Fator de vencimento**: dias desde 07/10/1997 até 21/02/2025 (fator 9999); a partir de 22/02/2025 reinicia em 1000 (+1 por dia, ciclo de 9000). |
| RN-COB-05 | **Linha digitável** (47 dígitos): três campos do campo livre com DV módulo 10 (pesos 2/1), DV geral e fator+valor. |
| RN-COB-06 | **Campo livre** (específico de cada banco) montado pelo modelo da conta com marcações `{agencia:N}`, `{agenciadv:N}`, `{conta:N}`, `{contadv:N}`, `{carteira:N}`, `{convenio:N}`, `{nossonumero:N}`, `{zeros:N}` e dígitos fixos, totalizando 25. |
| RN-COB-07 | Remessa CNAB 240 Febraban: header de arquivo, header de lote, segmentos P/Q/R por título, trailers; linhas de 240 posições; sequência de remessa por conta; inclui boletos EMITIDOS (movimento 01) e pedidos de baixa (02). Arquivo de homologação recebe prefixo `TESTE_`. A remessa é validada antes de ser gravada. |
| RN-COB-08 | Cancelar boleto: EMITIDO → CANCELADO; ENVIADO/REGISTRADO → pedido de baixa na próxima remessa. |
| RN-COB-09 | Retorno CNAB 240 (segmentos T/U): 02 confirma registro; 03 rejeita (com motivos); 06/17 liquidam (boleto PAGO e conta a receber baixada com valor pago e data da ocorrência); 09 baixa. Boleto localizado pelo identificador da empresa ou pelo nosso número; retorno de outro banco é recusado. |
| RN-COB-10 | Validador CNAB 240: tamanho das linhas, ordem dos registros, código de banco, lotes, sequenciais, quantidades e totais dos trailers. |

## Fiscal (RN-FIS)

| ID | Regra |
|---|---|
| RN-FIS-01 | Uma venda/compra tem no máximo **uma** nota não cancelada. |
| RN-FIS-02 | CFOP padrão: saída 5102 (mesma UF) / 6102 (outra UF); entrada 1102 / 2102. Participante sem UF = mesma UF. |
| RN-FIS-03 | Impostos por item: `valor = arredondar(total do item × alíquota ÷ 100, 2)` (ICMS, IPI, PIS, COFINS). Total do item = qtd × unitário − desconto. |
| RN-FIS-04 | Total da nota = produtos − (descontos dos itens + desconto adicional) + frete + IPI. |
| RN-FIS-05 | Para emitir: itens; natureza da operação; destinatário (modelo 55); NCM 8 dígitos, CFOP 4 dígitos, CST/CSOSN e quantidade > 0 por item; com reforma ativa, CST IBS/CBS (3 dígitos) e cClassTrib (6 dígitos). |
| RN-FIS-06 | Numeração: próximo número do modelo (`proximo_numero_55`/`65`), pulando números usados na mesma série; modelo+série+número único entre notas próprias. |
| RN-FIS-07 | Chave de acesso (44 dígitos): cUF + AAMM + CNPJ + modelo + série(3) + número(9) + tpEmis(1) + código numérico aleatório(8) + DV módulo 11. Só gerada com UF e CNPJ da empresa configurados. |
| RN-FIS-08 | Nota emitida não é alterada; rascunho pode ser excluído; cancelar nota não desfaz venda/compra. |
| RN-FIS-09 | **Não há transmissão à SEFAZ**: o DANFE de nota própria é marcado "SEM VALOR FISCAL". |
| RN-FIS-10 | Para emitir, o CFOP deve ser coerente: saída começa por 5/6/7 e entrada por 1/2/3; com a UF da loja configurada, participante da mesma UF (ou sem UF) usa 5/1 e de outra UF usa 6/2 (exterior 7/3 sem conferência de UF). Também: CPF/CNPJ do participante válido (se informado), valor unitário ≥ 0, desconto do item entre 0 e qtd × unitário, desconto adicional ≤ valor dos itens, NFC-e (65) só em saída. |
| RN-FIS-11 | Cancelar nota emitida é exclusivo do administrador; só nota EMITIDA é cancelada (rascunho é excluído). |

## Reforma tributária (RN-REF)

Base legal: EC 132/2023 e LC 214/2025 (IBS, CBS e Imposto Seletivo). Valores padrão são de referência e **devem ser confirmados com o contador a cada ano da transição**.

| ID | Regra |
|---|---|
| RN-REF-01 | Com `reforma_ativa` = SIM, cada item recebe alíquotas de CBS (`aliq_cbs`), IBS estadual (`aliq_ibs_uf`) e IBS municipal (`aliq_ibs_mun`); padrão 2026 (ano de teste): CBS 0,90% e IBS 0,10%. |
| RN-REF-02 | Redução de alíquota do produto (regimes diferenciados): alíquota efetiva = alíquota × (1 − redução%/100). Imposto Seletivo pela alíquota do produto. |
| RN-REF-03 | Base de cálculo IBS/CBS = valor do item (qtd × unitário − desconto). Valor = arredondar(base × alíquota ÷ 100, 2). |
| RN-REF-04 | IBS, CBS e IS são **destacados** no item, nos totais da nota e no DANFE, e **não somam** ao total da nota (regra do ano de teste). |
| RN-REF-05 | CST IBS/CBS e cClassTrib vêm do produto (ou dos padrões) e são editáveis na nota. Desligar a reforma zera as alíquotas novas. |

## Impressão (RN-IMP)

| ID | Regra |
|---|---|
| RN-IMP-01 | Cupons (comprovante, TEF, página de teste) vão para a impressora de cupom; documentos (DANFE, pedidos, OP, frete, boletos) para a impressora de documentos. Em branco = impressora padrão do Windows. |
| RN-IMP-02 | Impressora de cupom tipo WINDOWS imprime pelo driver; tipo ESCPOS recebe comandos diretos: inicialização, página de código (850 ou 860), alinhamento, negrito, fonte grande, avanço, corte parcial (`impressora_cortar`) e pulso de gaveta. |
| RN-IMP-03 | O cupom usa `impressora_colunas` colunas (48 em 80 mm, 32 em 58 mm). Marcações `[C]`, `[N]`, `[G]` controlam centralização, negrito e fonte grande, e são removidas na tela. |

## Usuários e acesso (RN-USU)

| ID | Regra |
|---|---|
| RN-USU-01 | Entrada por username (3–30: letras, números, ponto, hífen, sublinhado) e senha; usuário inativo não entra. |
| RN-USU-02 | Senhas e códigos de recuperação guardados só como hash PBKDF2-SHA256 (100.000 iterações, salt de 16 bytes), comparados em tempo constante. |
| RN-USU-03 | E-mail obrigatório e único no cadastro; ao entrar sem e-mail o sistema avisa com uma notificação que some sozinha (não bloqueia a entrada). |
| RN-USU-04 | Administrador: usuários, todas as configurações, contas bancárias, cancelamentos, descontos e crédito acima do limite. Ninguém exclui o próprio usuário. |
| RN-USU-05 | Banco novo vem com admin/admin; a tela de entrada avisa enquanto a senha não for trocada. |
| RN-USU-06 | **Recuperação de senha**: código de 6 dígitos (gerador criptográfico) enviado ao e-mail pelo Resend; válido 15 min; 5 tentativas; um novo pedido invalida os anteriores; uso único; nova senha com ≥ 4 caracteres; e-mail exibido mascarado. |
| RN-USU-07 | O sistema nunca fica sem administrador ativo: o último não pode ser inativado, rebaixado nem excluído. Ninguém inativa o próprio usuário nem tira o próprio acesso de administrador. Senha do cadastro com ≥ 4 caracteres. |

## Banco de dados (RN-BD)

| ID | Regra |
|---|---|
| RN-BD-01 | Localização: (1) último banco usado nesta máquina; (2) `loja.db` ao lado do executável; senão o usuário cria um banco novo ou indica o arquivo. Criar nunca sobrescreve. |
| RN-BD-02 | Migrations numeradas, aplicadas uma vez, cada uma em transação, registradas em `__migracoes`; banco mais novo que o executável é recusado. |
| RN-BD-03 | Antes de migrar um banco existente, backup automático em `backups/`. Migrações que reconstroem tabelas rodam com chaves estrangeiras desligadas e verificam a integridade (`foreign_key_check`) antes de confirmar. |
| RN-BD-04 | Backup diário ao abrir (`backup_ao_iniciar`), um por dia, mantendo os `backups_manter` mais recentes. |

## Log de atividades (RN-LOG)

| ID | Regra |
|---|---|
| RN-LOG-01 | Toda ação gera um registro de log com data/hora (milissegundos), usuário, computador, tela, categoria, ação e detalhe. O log fica num **banco SQLite separado**, ao lado do banco da loja (`loja.db` → `loja_log.db`). |
| RN-LOG-02 | Categorias: **ACESSO** (entrada, saída, senha errada, usuário inativo, código de recuperação enviado/incorreto, senha redefinida), **TELA** (aberta/fechada), **AÇÃO** (botão clicado, confirmação respondida, mensagem exibida, exportação, operações do PDV), **DADOS** (gravações), **ERRO** (avisos de erro e erros inesperados com detalhes técnicos), **SISTEMA** (início, migrações, backups, atalho, dados de demonstração). |
| RN-LOG-03 | Gravações (DADOS) são registradas **somente quando confirmadas**: uma linha por operação, nomeada pela primeira tabela gravada (ex.: "Incluiu vendas"), com o detalhe das tabelas e quantidade de linhas e o id do registro incluído. Operação desfeita (rollback) não aparece. Migrações e a geração de dados de demonstração registram um resumo, não cada comando. |
| RN-LOG-04 | O log **nunca interrompe** a operação: falha ao gravar o log é ignorada. |
| RN-LOG-05 | Senhas, códigos e valores digitados não são gravados no log; o detalhe de gravações traz apenas tabelas e quantidades. |
| RN-LOG-06 | Ao abrir o sistema, registros mais antigos que `log_dias_manter` dias são apagados (0 = manter tudo). |
| RN-LOG-07 | A consulta (*Sistema > Log de atividades*) é exclusiva de administradores, filtra por período, categoria, usuário e texto, mostra até 5.000 registros mais recentes e exporta CSV. |

## Navegação e interface (RN-NAV)

| ID | Regra |
|---|---|
| RN-NAV-01 | As telas do menu abrem **embutidas no espaço à direita do menu**, uma aba por tela; não abrem janelas soltas. Janelas de edição, confirmações e escolhas continuam por cima (modais), porque exigem resposta. |
| RN-NAV-02 | Abrir uma tela já aberta traz a aba dela para a frente (nunca duplica). Ctrl+Tab/Ctrl+Shift+Tab alternam as abas; Ctrl+W, o × ou o botão do meio do mouse fecham a aba. |
| RN-NAV-03 | Fechar a janela principal fecha as abas uma a uma; se alguma recusar (ex.: venda em andamento no PDV), o fechamento é cancelado e a aba é mostrada. |
| RN-NAV-04 | Cada tela tem o seu "?" (e F1) com a explicação **apenas daquela tela**; o "?" da janela principal aparece só na tela inicial. |
| RN-NAV-05 | O PDV tem fundo branco e é operado pelo teclado; as teclas de função do PDV têm prioridade sobre os atalhos gerais enquanto ele está em primeiro plano. |
| RN-NAV-06 | Cada função existe em um único lugar do menu (ver [ADR-0007](adr/0007-navegacao-fonte-unica.md)); telas de administrador não aparecem para usuários comuns. |

## Instalação (RN-INS)

| ID | Regra |
|---|---|
| RN-INS-01 | Na **primeira execução** em cada usuário do Windows, o sistema cria o atalho "ERP Loja" na área de trabalho apontando para o executável. Se o usuário apagar o atalho, ele não é recriado (um marcador em `%LOCALAPPDATA%\ErpLoja` registra que já foi criado). |
| RN-INS-02 | Perfis isolados (variável `ERPLOJA_PREFERENCIAS`, usada em testes) não criam atalho. Falha ao criar o atalho é registrada no log e não impede o uso. |

## Parâmetros (RN-PAR)

Todos na tela *Configurações* (somente administrador), tabela `configuracoes`. A lista é a fonte única (`Parametros.Definicoes`).

| Grupo | Chave | Padrão | Uso |
|---|---|---|---|
| Empresa | `nome_loja`, `cnpj`, `ie`, `endereco`, `cidade`, `uf`, `telefone`, `email_loja` | — | Documentos, CFOP, chave de acesso |
| Fiscal | `regime` | 1 - Simples Nacional | Texto do DANFE |
| Fiscal | `serie_55`, `proximo_numero_55`, `serie_65`, `proximo_numero_65` | 1 | Numeração NF-e/NFC-e |
| Fiscal | `natureza_venda`, `natureza_compra`, `cst_padrao` | Venda de mercadoria / Compra para comercialização / 102 | Padrões |
| Reforma | `reforma_ativa`, `aliq_cbs`, `aliq_ibs_uf`, `aliq_ibs_mun`, `cst_ibscbs_padrao`, `cclass_trib_padrao` | SIM / 0,90 / 0,10 / 0,00 / 000 / 000001 | RN-REF |
| Vendas e compras | `venda_sem_estoque` | PERGUNTAR | RN-VEN-03 |
| Vendas e compras | `desconto_maximo_pct` | 10 | RN-VEN-02 |
| Vendas e compras | `validade_orcamento_dias`, `prazo_vencimento_dias`, `max_parcelas` | 15 / 30 / 12 | Prazos e parcelas |
| Vendas e compras | `caixa_numero`, `imprimir_cupom` | 01 / PERGUNTAR | PDV |
| Produção | `producao_sem_estoque` | PERGUNTAR | RN-PRO-05 |
| Transporte e CIOT | `rntrc_empresa`, `ciot_administradora`, `ciot_ambiente`, `pamcard_url`, `pamcard_usuario`, `pamcard_senha`, `pamcard_cnpj_contratante` | — / PAMCARD / HOMOLOGACAO | RN-TRA |
| TEF | `tef_ativo`, `tef_provedor`, `tef_imprimir_via_loja` | SIM / SIMULADOR / NAO | RN-TEF |
| Impressoras | `impressora_cupom`, `impressora_cupom_tipo`, `impressora_colunas`, `impressora_codepage`, `impressora_cortar`, `impressora_gaveta`, `impressora_documentos` | — / WINDOWS / 48 / 850 / SIM / NAO / — | RN-IMP |
| E-mail | `resend_api_key`, `resend_remetente` | — | RN-USU-06 |
| Banco de dados | `backup_ao_iniciar`, `backups_manter` | SIM / 15 | RN-BD-04 |
| Banco de dados | `log_dias_manter` | 365 | RN-LOG-06 |
