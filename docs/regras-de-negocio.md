# Regras de negócio

Este documento descreve **como o sistema decide e calcula** cada coisa. Ele vale como especificação:
se o comportamento do sistema divergir daqui, um dos dois está errado. Os valores entre crases são
parâmetros configuráveis em *Configurações* (ver [Parâmetros](#parâmetros)).

Sumário: [Estoque](#estoque) · [Custo](#custo-dos-produtos) · [Vendas no PDV](#vendas-no-pdv) ·
[Orçamentos e pedidos](#orçamentos-e-pedidos) · [Compras](#compras) · [Importação de XML](#importação-do-xml-da-nf-e) ·
[Financeiro](#financeiro) · [Notas fiscais](#notas-fiscais) · [Cadastros](#cadastros) · [Usuários e acesso](#usuários-e-acesso) ·
[Recuperação de senha](#recuperação-de-senha) · [Parâmetros](#parâmetros) · [Banco de dados e backups](#banco-de-dados-e-backups)

---

## Estoque

- O saldo de um produto **só muda por movimentação**; não é editável no cadastro.
- Toda movimentação grava: data/hora, tipo, quantidade (sempre positiva), **saldo após**, origem (ex.: "Venda nº 12") e usuário.

| Evento | Tipo | Efeito no saldo |
|---|---|---|
| Venda (PDV ou pedido faturado) | SAIDA | − quantidade |
| Cancelamento de venda | ENTRADA | + quantidade |
| Compra (pedido recebido ou XML importado) | ENTRADA | + quantidade (× fator, no XML) |
| Cancelamento de compra | SAIDA | − quantidade |
| Ajuste: entrada / saída | ENTRADA / SAIDA | ± quantidade informada |
| Ajuste: definir saldo (inventário) | AJUSTE | + (contado − saldo atual) |

- Saldo arredondado em 3 casas decimais (permite venda fracionada, ex.: kg).
- O saldo **pode ficar negativo** quando a venda sem estoque é permitida/confirmada, ou ao cancelar uma compra cujos itens já foram vendidos.
- Ajuste exige motivo; ajuste que não muda o saldo não é gravado.
- Situação: **SEM ESTOQUE** se saldo ≤ 0; **BAIXO** se saldo ≤ estoque mínimo; senão **OK**.
- Sugestão de compra = máx(estoque mínimo − saldo, 0).
- Valor em estoque = máx(saldo, 0) × custo médio.

## Custo dos produtos

- O preço de custo do produto é o **custo médio ponderado**, recalculado a cada entrada de compra:
  - se o saldo antes da entrada é maior que zero: `novo custo = (saldo × custo atual + qtd × custo da entrada) ÷ (saldo + qtd)`, arredondado em 4 casas;
  - se o saldo é zero ou negativo: `novo custo = custo da entrada`.
- Cancelar uma compra **não** recalcula o custo retroativamente.
- Cada item de venda guarda o custo médio do momento da venda; o lucro bruto usa esse custo (não muda se o custo mudar depois).
- **Pedido de compra:** frete e desconto geral são distribuídos proporcionalmente ao valor dos itens:
  `custo unitário = total do item × (total do pedido ÷ soma dos itens) ÷ quantidade`.
- **XML de NF-e:** ver [Importação do XML](#importação-do-xml-da-nf-e).
- Margem exibida no cadastro = (preço de venda − custo) ÷ custo × 100.

## Vendas no PDV

- Item: `total = arredondar(quantidade × preço, 2)`. Subtotal = soma dos itens.
- Total = máx(subtotal − desconto, 0). O desconto não pode ser maior que o subtotal.
- **Desconto máximo:** usuário comum só pode dar até `desconto_maximo_pct`% do subtotal; acima disso apenas administrador.
- **Estoque insuficiente** (quantidade acumulada do produto na venda > saldo) segue `venda_sem_estoque`:
  PERGUNTAR (pede confirmação), PERMITIR (segue sem perguntar) ou BLOQUEAR (impede).
- Dinheiro: se o valor recebido for informado, precisa ser ≥ total; troco = recebido − total.
- Adicionar um produto que já está na venda soma a quantidade na mesma linha.
- **A prazo:** exige cliente identificado. Gera as parcelas (ver [Parcelamento](#parcelamento)). O 1º vencimento padrão é hoje + `prazo_vencimento_dias`; máximo de `max_parcelas` parcelas.
- **À vista** (dinheiro, PIX, cartões): gera uma conta a receber já **PAGA** na data de hoje, com a forma de pagamento.
- Venda com total zero (100% de desconto) não gera conta.
- O comprovante é **não fiscal**.

### Parcelamento

Usado em vendas, compras e lançamentos manuais a prazo:
- valor da parcela = arredondar(total ÷ n, 2); a **última parcela** recebe a diferença de centavos;
- vencimentos mensais a partir do 1º vencimento (mesmo dia nos meses seguintes);
- descrição: "Venda nº X - parcela 2/3".

### Cancelamento de venda

- Somente **administrador**; somente vendas FINALIZADAS.
- Devolve todos os itens ao estoque; cancela **todas** as contas a receber da venda (inclusive a já paga do à vista);
  se a venda veio de um pedido, o pedido volta para **ABERTO**.
- A nota fiscal da venda, se emitida, **não** é cancelada automaticamente: cancele em *Notas fiscais*.

## Orçamentos e pedidos

Os três tipos (ORÇAMENTO, PEDIDO DE VENDA, PEDIDO DE COMPRA) têm a mesma estrutura: itens com desconto por item,
desconto geral, frete, condição de pagamento e observações.

- Item: `total = arredondar(qtd × preço − desconto do item, 2)`.
- Total = máx(subtotal − descontos dos itens − desconto geral + frete, 0). Descontos não podem zerar um total com itens.
- O limite de desconto (`desconto_maximo_pct`) vale para orçamentos e pedidos de venda, sobre a soma dos descontos.
- Só documentos **ABERTOS** podem ser alterados; os demais abrem só para consulta.
- Duplicar cria um novo documento ABERTO com os mesmos itens (orçamento recebe validade nova: hoje + `validade_orcamento_dias`).

| Tipo | Situações | Transições |
|---|---|---|
| Orçamento | ABERTO, APROVADO, RECUSADO, CANCELADO (VENCIDO é exibido, não gravado) | Aprovar → cria pedido de venda; Recusar; Cancelar |
| Pedido de venda | ABERTO, FATURADO, CANCELADO | Faturar → cria venda; Cancelar; venda cancelada → volta a ABERTO |
| Pedido de compra | ABERTO, RECEBIDO, CANCELADO | Receber → cria compra; Cancelar; compra cancelada → volta a ABERTO |

- **Vencido**: orçamento ABERTO com validade anterior a hoje. Pode ser aprovado mediante confirmação (os preços são mantidos).
- Validade padrão do orçamento = hoje + `validade_orcamento_dias`.
- **Aprovar orçamento** cria um pedido de venda com os mesmos itens, descontos, frete e condição; o pedido guarda a origem (orçamento nº).
- **Faturar pedido de venda**: verifica estoque (regra `venda_sem_estoque`), pergunta a forma/parcelas/vencimento e gera a venda com
  desconto = descontos dos itens + desconto geral e com o frete. A venda registra "Pedido nº X". Pedido → FATURADO.
- **Receber pedido de compra**: exige fornecedor; pergunta forma/parcelas/vencimento e o nº da nota; gera a compra
  (estoque + custo médio + contas a pagar). Pedido → RECEBIDO.
- Orçamentos e pedidos **não** mexem em estoque nem em financeiro até serem faturados/recebidos.

## Compras

Toda entrada de mercadoria acontece por um destes caminhos (sem telas duplicadas):
1. **Pedido de compra** → Receber (ou "Salvar e receber" no próprio pedido, para entrada imediata);
2. **Importação do XML** da NF-e do fornecedor.

- Entrada no estoque, recálculo do custo médio e contas a pagar (à vista: paga hoje; a prazo: parcelas ou duplicatas do XML).
- **Cancelamento de compra:** somente administrador; retira os itens do estoque, cancela as contas a pagar da compra e reabre o pedido de origem.

## Importação do XML da NF-e

- Aceita o XML da NF-e/NFC-e (com ou sem `nfeProc`). A **chave de acesso** identifica a nota: a mesma nota não pode ser importada duas vezes.
- **Fornecedor**: localizado pelo CNPJ (comparando só os dígitos); se não existir, é cadastrado com os dados do emitente.
- **Vínculo de cada item com um produto**, nesta ordem:
  1. vínculo memorizado para o par (fornecedor, código do produto no fornecedor), com o seu fator;
  2. código de barras (EAN/GTIN válido: 8, 12, 13 ou 14 dígitos; "SEM GTIN" é ignorado) igual ao código de barras ou código de um produto;
  3. sem vínculo → o usuário escolhe um produto ou "(cadastrar novo)".
- **Fator de conversão**: unidades do seu estoque por unidade do fornecedor (ex.: caixa com 12 → 12). Estoque entra `qCom × fator`.
- **Custo efetivo do item** = vProd − vDesc + vFrete + vSeg + vOutro + vIPI + vICMSST; custo unitário = custo efetivo ÷ (qCom × fator).
- **Produto novo**: código = EAN válido (se ainda não usado) ou `F{fornecedor}-{código do fornecedor}`; descrição, NCM e unidade da nota;
  CST 102; **preço de venda zero** (o usuário é avisado para ajustar).
- Os vínculos e fatores usados são **memorizados** para as próximas notas do mesmo fornecedor.
- **Contas a pagar**: se a forma for "A prazo" e a nota tiver duplicatas (`cobr/dup`), uma conta por duplicata com vencimento e valor da nota;
  sem duplicatas, parcela única com vencimento em hoje + `prazo_vencimento_dias`; à vista, conta paga hoje.
- Opcionalmente a nota pode ser só registrada, sem lançar compra (sem estoque e sem financeiro).
- A nota importada aparece em *Notas fiscais* como "Fornecedor (XML)", com o XML original guardado (pode ser salvo de volta em arquivo).

## Financeiro

- Contas são do tipo **A RECEBER** ou **A PAGAR**, com situação ABERTA, PAGA ou CANCELADA. **VENCIDA** é calculada (aberta com vencimento < hoje) e mostra os dias de atraso.
- Geradas automaticamente por vendas, compras e importação de XML, ou lançadas manualmente (cliente/fornecedor opcional, com parcelamento).
- **Baixa**: só conta aberta; registra data, valor pago (pode diferir do valor da conta — juros/desconto) e forma.
- **Estorno**: só conta paga; volta para aberta e apaga data/valor/forma do pagamento.
- **Cancelamento**: só conta aberta.
- **Fluxo de caixa realizado** e o gráfico "entradas x saídas" usam o **valor pago** na **data do pagamento**.
- **Previsto**: contas abertas pelo vencimento.

## Notas fiscais

- Notas **próprias** (emitidas pela loja) de saída ou entrada, modelo 55 (NF-e) ou 65 (NFC-e), criadas a partir de uma venda/compra ou avulsas.
  Uma venda/compra só pode ter **uma** nota não cancelada (o sistema abre a existente).
- **CFOP padrão**: saída 5102 (mesma UF da loja) / 6102 (outra UF); entrada 1102 / 2102. Participante sem UF = mesma UF.
  Trocar a UF no editor atualiza os itens que estão com CFOP padrão.
- NCM, CST/CSOSN e alíquotas vêm do cadastro do produto e podem ser alterados na nota.
- Impostos por item: `valor = arredondar(total do item × alíquota ÷ 100, 2)` para ICMS, IPI, PIS e COFINS.
- Total do item = qtd × valor unitário − desconto do item.
- **Total da nota** = valor dos produtos − (descontos dos itens + desconto adicional) + frete + IPI.
- **Para emitir** a nota precisa: ter itens; natureza da operação; destinatário/remetente (modelo 55); em cada item NCM com 8 dígitos,
  CFOP com 4 dígitos, CST/CSOSN e quantidade > 0.
- **Numeração**: próximo número do modelo (`proximo_numero_55` / `proximo_numero_65`), pulando números já usados na mesma série; número + modelo + série é único entre as notas próprias. Após emitir, o próximo número avança.
- **Chave de acesso** (44 dígitos, layout NF-e): cUF + AAMM + CNPJ + modelo + série (3) + número (9) + tipo de emissão (1) + código numérico aleatório (8) + DV (módulo 11).
  Só é gerada se a UF e o CNPJ (14 dígitos) da empresa estiverem configurados.
- Nota emitida **não** pode ser alterada. Rascunho (DIGITADA) pode ser excluído. Cancelar a nota **não** desfaz a venda/compra.
- Regime Simples Nacional: o DANFE inclui "Documento emitido por ME ou EPP optante pelo Simples Nacional".
- **Não há transmissão à SEFAZ**: o DANFE de nota própria sai marcado "SEM VALOR FISCAL".

## Cadastros

- Campos obrigatórios marcados com *. Código de produto, username e e-mail de usuário são únicos (sem diferenciar maiúsculas).
- **Exclusão**: registros com histórico (vendas, compras, notas, movimentações) não podem ser excluídos; o sistema oferece **inativar**.
  Inativos somem das listas de seleção, mas continuam nos históricos e relatórios. Categoria em uso não pode ser excluída.
- O saldo e o custo do produto não são editados manualmente (ver [Estoque](#estoque) e [Custo](#custo-dos-produtos)).
- CST padrão de produto novo = `cst_padrao`.

## Usuários e acesso

- Entrada por **username** (3 a 30 caracteres: letras, números, ponto, hífen, sublinhado) e senha. Usuário inativo não entra.
- Senhas guardadas apenas como hash **PBKDF2-SHA256** (100.000 iterações, salt aleatório de 16 bytes).
- E-mail obrigatório no cadastro e único — é o destino da recuperação de senha. Ao entrar sem e-mail cadastrado, o sistema avisa.
- **Administrador**: cadastro de usuários, todas as abas de configurações, cancelamento de vendas/compras, descontos acima do limite.
- Ninguém pode excluir o próprio usuário. Ao editar um usuário, senha em branco mantém a atual (é assim que o administrador redefine a senha de alguém).
- Banco novo vem com o usuário **admin / admin** (administrador); a tela de entrada avisa enquanto essa senha não for trocada.

## Recuperação de senha

- Pelo link "Esqueci minha senha" na entrada, informando username ou e-mail.
- Exige usuário ativo com e-mail válido e o envio de e-mail configurado (`resend_api_key`, `resend_remetente`).
- Código de **6 dígitos**, aleatório (gerador criptográfico), guardado **somente como hash**, válido por **15 minutos**.
- Máximo de **5 tentativas** erradas; na 5ª o código é invalidado. Um novo pedido invalida os códigos anteriores. O código vale uma única vez.
- O e-mail é enviado pela API do **Resend**; a tela mostra o destino mascarado (ex.: `a****@loja.com`).
- Nova senha: mínimo de 4 caracteres.

## Parâmetros

Todos ficam na tela *Configurações* (somente administrador) e na tabela `configuracoes`.

| Chave | Padrão | Uso |
|---|---|---|
| `nome_loja`, `cnpj`, `ie`, `endereco`, `cidade`, `uf`, `telefone`, `email_loja` | — | Dados da empresa (comprovante, orçamentos, DANFE, CFOP, chave de acesso) |
| `regime` | 1 - Simples Nacional | Regime tributário (texto do DANFE) |
| `serie_55`, `proximo_numero_55` | 1, 1 | Série e numeração da NF-e |
| `serie_65`, `proximo_numero_65` | 1, 1 | Série e numeração da NFC-e |
| `natureza_venda`, `natureza_compra` | Venda de mercadoria / Compra para comercialização | Natureza padrão das notas |
| `cst_padrao` | 102 | CST/CSOSN de produto novo |
| `venda_sem_estoque` | PERGUNTAR | PERGUNTAR, PERMITIR ou BLOQUEAR |
| `desconto_maximo_pct` | 10 | % de desconto permitido a usuário comum |
| `validade_orcamento_dias` | 15 | Validade padrão de orçamento |
| `prazo_vencimento_dias` | 30 | 1º vencimento padrão a prazo |
| `max_parcelas` | 12 | Máximo de parcelas (1 a 60) |
| `resend_api_key`, `resend_remetente` | — | Envio de e-mail (recuperação de senha) |
| `backup_ao_iniciar` | SIM | Backup diário automático ao abrir |
| `backups_manter` | 15 | Quantos backups diários manter |

## Banco de dados e backups

- **Localização** na abertura: (1) último banco usado neste computador (`%LOCALAPPDATA%\ErpLoja\banco.txt`); (2) `loja.db` ao lado do executável.
  Sem nenhum dos dois, o usuário escolhe **criar banco novo** (ao lado do executável; se a pasta não permitir gravação, ele escolhe o local)
  ou **indicar o arquivo**. O arquivo indicado precisa ser um SQLite com as tabelas do sistema.
- Criar banco nunca sobrescreve um arquivo existente.
- **Migrations**: cada versão do banco é uma migração numerada aplicada uma única vez, em transação, registrada em `__migracoes`.
  Banco de versão **mais nova** que o executável é recusado (é preciso atualizar o executável).
- Antes de migrar um banco existente é feito um **backup automático** (`backups/<banco>_<data>_antes_migracao_vX_para_vY.db`).
- **Backup diário** ao abrir (se `backup_ao_iniciar` = SIM): no máximo um por dia, mantendo os `backups_manter` mais recentes.
- **Backup manual** e **troca de banco** em *Configurações > Banco de dados*.
