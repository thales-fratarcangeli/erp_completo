using System.Text;

namespace ErpLoja.UI;

public record TopicoAjuda(string Chave, string Titulo, string Texto);

/// <summary>
/// Ajuda de cada tela (botão "?" e tecla F1). Fonte única: os mesmos tópicos geram docs/manual-do-usuario.md.
/// Formato do texto: "## " subtítulo, "- " item de lista, linha em branco separa parágrafos.
/// </summary>
public static class Ajuda
{
    public static readonly TopicoAjuda[] Topicos =
    [
        new("MainForm", "Início e navegação", """
            O ERP Loja controla vendas, compras, produção, logística, estoque, financeiro, cobrança bancária e notas fiscais.

            ## Menu lateral
            O menu é dividido por áreas, cada uma com sua cor (a mesma cor aparece na faixa superior das janelas daquela área). Clique no nome da área para abrir ou fechar a lista de telas.
            Cada função existe em um único lugar do menu — não há duas telas fazendo a mesma coisa.

            ## Buscar tela (Ctrl+K)
            Não sabe onde fica? Clique em "Buscar tela" ou tecle Ctrl+K e digite uma palavra (ex.: boleto, frete, senha, estoque). O sistema mostra todas as telas relacionadas, procurando nos nomes, nas palavras-chave e no texto de ajuda. Pode escrever do seu jeito (ex.: "como cadastrar cliente", "vender", "trocar minha senha"): palavras como "como", "onde" e "o" são ignoradas e verbos acham a tela pelo radical.

            ## Telas abertas (abas)
            As telas abrem no espaço à direita do menu, sem janelas soltas. Cada tela aberta vira uma aba no topo: clique na aba para voltar a ela e no × para fechá-la.
            - Ctrl+Tab passa para a próxima aba; Ctrl+W fecha a aba atual.
            - Abrir de novo uma tela que já está aberta apenas a traz para a frente.
            - Telas exclusivas de administrador (Usuários, Contas bancárias, Log de atividades) não aparecem para os demais usuários.
            - Ao sair com uma venda em andamento no PDV, o sistema pergunta antes; respondendo Não, o PDV volta para a frente.
            Janelas de edição (ex.: editar um pedido) e confirmações continuam aparecendo por cima, porque precisam de uma resposta antes de continuar.

            ## Continuar de onde parou
            Na tela inicial aparecem as telas que você usou por último, cada uma com o ponto da cor da sua área: um clique abre. No Buscar tela (Ctrl+K), com a busca vazia, elas também vêm primeiro.
            No menu lateral, a tela que está em primeiro plano fica destacada (em negrito, com a marca de cor cheia).

            ## Tela inicial
            Aparece quando nenhuma tela está aberta. Fica limpa de propósito: use o menu ou a busca. Indicadores e gráficos ficam em Gestão > Painel gerencial (F5).

            ## Atalhos
            - F2 PDV · F4 Pedidos de compra · F5 Painel gerencial · F6 Estoque · F7 Financeiro · F8 Relatórios
            - Ctrl+O Orçamentos · Ctrl+P Pedidos de venda · Ctrl+N Notas fiscais · Ctrl+K Buscar tela
            - F1 em qualquer tela abre a ajuda dela.
            """),

        new("DashboardForm", "Painel gerencial", """
            Indicadores do período escolhido — faturamento, lucro bruto, margem, número de vendas, ticket médio e itens vendidos — comparados com o período anterior de mesmo tamanho (▲ subiu, ▼ caiu).

            ## Gráficos
            - Faturamento por dia (ou por mês em períodos longos), produtos e categorias que mais venderam, formas de pagamento, entradas x saídas do caixa e movimento por hora.
            - Passe o mouse sobre uma barra para ver o valor exato.
            - Troque o período no topo; os números são recalculados na hora.
            """),

        new("BancoForm", "Banco de dados não encontrado", """
            O sistema guarda tudo em um único arquivo de banco de dados (loja.db). Ao abrir, ele procura automaticamente:
            - o último banco usado neste computador;
            - o arquivo loja.db na mesma pasta do ErpLoja.exe.

            Se não encontrar, você escolhe:
            - Criar banco novo: cria um loja.db zerado ao lado do executável (ou onde você indicar, se a pasta não permitir gravação). Acesso inicial: usuário admin, senha admin.
            - Indicar arquivo existente: aponte o loja.db (por exemplo, numa pasta de rede ou depois de trocar de computador). O caminho fica lembrado para as próximas vezes.

            Bancos de versões anteriores são atualizados automaticamente (migrations), com backup antes de qualquer alteração.
            """),

        new("login", "Acesso ao sistema", """
            Informe seu username e senha.

            ## Primeiro acesso
            Com a senha padrão (admin) — ou com a senha igual ao username — o sistema pede uma senha nova antes de entrar (mínimo de 8 caracteres, digitada duas vezes).

            ## Bloqueio por senhas erradas
            Depois de 5 senhas erradas seguidas, o usuário fica bloqueado por 15 minutos (proteção contra quem tenta adivinhar a senha). Para liberar antes, um administrador define uma senha nova em Cadastros > Usuários, ou a própria pessoa usa "Esqueci minha senha". Sem trocar, não entra: qualquer pessoa poderia adivinhar a senha padrão.

            ## Esqueci minha senha
            Clique em "Esqueci minha senha": o sistema envia um código de 6 dígitos para o e-mail cadastrado no seu usuário. O código vale 15 minutos e aceita até 5 tentativas.
            Se o seu usuário não tiver e-mail, um administrador pode definir uma nova senha em Cadastros > Usuários.
            """),

        new("RecuperarSenhaForm", "Recuperar senha", """
            1. Digite seu username (ou e-mail) e clique em Enviar código.
            2. Abra seu e-mail, copie o código de 6 dígitos e informe aqui junto com a nova senha.

            - O código vale 15 minutos e aceita até 5 tentativas. Um novo código enviado invalida o anterior (se o envio falhar, o código que você já recebeu continua valendo).
            - O envio usa o serviço Resend, configurado pelo administrador em Configurações > E-mail.
            """),

        new("PdvForm", "PDV - frente de caixa", """
            Tela de caixa em tela cheia, feita para ser usada pelo teclado.

            ## Lançar itens
            - Leia o código de barras ou digite o código e tecle Enter.
            - Para vender várias unidades: digite 3*código (ou 3xcódigo), ou tecle F4 antes para definir a quantidade.
            - F3 abre a busca por nome. O cartão "Último item" confirma o que entrou; o total aparece em destaque.

            ## Teclas
            - F2 cliente · F3 buscar · F4 quantidade · F5 desconto (em R$ ou %) · F6 remover item · F7 cancelar venda · F10 pagamento · Esc sair.

            ## Pagamento (F10)
            - Escolha a forma com as teclas 1 a 5: dinheiro, PIX, débito, crédito ou a prazo. A janela abre em dinheiro: Enter já confirma.
            - Para calcular o troco, tecle 1 (o cursor vai para o valor recebido), digite o valor entregue e tecle Enter.
            - Cartão e PIX acionam o TEF (se ativo em Configurações). Se a venda não puder ser gravada, a transação do cartão é desfeita automaticamente.
            - O comprovante é impresso conforme Configurações (perguntar, sempre ou nunca), na impressora de cupom; em dinheiro a gaveta pode abrir sozinha.

            ## Regras
            - A venda baixa o estoque e lança o recebimento no financeiro. A prazo exige cliente e respeita o limite de crédito dele (acima do limite, só um administrador libera, confirmando). Produto sem preço de venda não é lançado.
            - Estoque insuficiente segue a regra de Configurações (perguntar, permitir ou bloquear). Desconto acima do limite só com administrador.
            - Se, depois do desconto, itens forem removidos e o desconto passar do limite, o F10 avisa: ajuste o desconto com F5.
            - Quantidade em "N*código" vai de 0,001 a 99.999; um valor fora disso é recusado com aviso.
            """),

        new("PagamentoForm", "Pagamento e TEF", """
            ## Formas de pagamento
            - 1 Dinheiro (já vem escolhido; Enter confirma). Tecle 1 de novo para ir ao valor recebido e calcular o troco.
            - 2 PIX, 3 Débito, 4 Crédito: o TEF é acionado ao confirmar (se estiver ativo). No crédito, informe as parcelas.
            - 5 A prazo: gera parcelas no contas a receber do cliente (exige cliente identificado com F2).

            ## TEF
            O TEF conversa com a maquininha/adquirente. Com o provedor SIMULADOR (Configurações > TEF) a tela imita a maquininha: aprove, negue ou cancele para testar. A transação aprovada só é confirmada depois que a venda é gravada; se algo falhar, ela é desfeita. Cancelar a venda estorna as transações de cartão/PIX.
            """),
        new("BuscaProdutoForm", "Busca de produto", """
            Digite parte do nome, do código ou do código de barras. Use a seta para baixo para ir à lista, Enter ou duplo clique para escolher e Esc para fechar.
            Só aparecem produtos ativos.
            """),

        new("CupomForm", "Visualizar e imprimir documento", """
            Mostra o documento (comprovante de venda, orçamento, pedido ou DANFE) como será impresso. Clique em Imprimir para escolher a impressora.
            O comprovante de venda é não fiscal. O DANFE de nota própria sai marcado "sem valor fiscal" enquanto a transmissão à SEFAZ não estiver ativa.
            """),

        new("pedidos_ORCAMENTO", "Orçamentos", """
            Proposta de preço para o cliente, sem mexer em estoque nem financeiro.

            ## Fluxo
            - Novo: monte o orçamento (itens, descontos, frete, condição de pagamento e validade). A validade padrão vem de Configurações.
            - Imprimir: gera o documento para entregar ao cliente.
            - Aprovar: o orçamento vira um pedido de venda (aba Pedidos de venda), mantendo itens e preços.
            - Recusar ou Cancelar: encerra o orçamento.
            - Duplicar: cria um orçamento novo a partir de outro.

            ## Situações
            - ABERTO, VENCIDO (aberto e com validade passada), APROVADO, RECUSADO, CANCELADO.
            - Só documentos em aberto podem ser alterados.
            """),

        new("pedidos_VENDA", "Pedidos de venda", """
            Venda combinada com o cliente que ainda será separada/entregue.

            ## Fluxo
            - Novo (ou aprovação de um orçamento) → ABERTO.
            - Faturar: gera a venda — baixa o estoque e lança o contas a receber — e o pedido fica FATURADO. Em seguida o sistema oferece emitir a nota fiscal.
            - Cancelar: só para pedido em aberto. Se a venda gerada for cancelada depois, o pedido volta para ABERTO.

            ## Regras
            - Descontos do pedido (por item + geral) e frete passam para a venda.
            - Venda a prazo exige cliente e respeita o limite de crédito dele (acima do limite, só administrador). A regra de estoque insuficiente vale também aqui.
            """),

        new("pedidos_COMPRA", "Pedidos de compra", """
            Compra de mercadoria do fornecedor. É também a forma de dar entrada manual de mercadoria.

            ## Fluxo
            - Novo → ABERTO. Para entrada imediata use "Salvar e receber" no próprio pedido.
            - Receber mercadoria: dá entrada no estoque, recalcula o custo médio e lança o contas a pagar; o pedido fica RECEBIDO.
            - Cancelar: só para pedido em aberto. Se a compra gerada for cancelada depois, o pedido volta para ABERTO.
            - Nº da nota fiscal já usado em outra compra do mesmo fornecedor: o sistema pede confirmação (evita entrada em dobro).
            - Item com custo unitário zero: o sistema pergunta se é bonificação antes de receber (custo zero derruba o custo médio).

            ## Custo
            Frete e descontos do pedido são distribuídos proporcionalmente no custo dos itens. As contas a pagar somam exatamente o total do pedido.
            Para notas com XML, prefira Compras > Importar XML: cadastra produtos e lança tudo automaticamente.
            """),

        new("PedidoEditorForm", "Editar orçamento / pedido", """
            - Adicione itens pelo código (Enter) ou pela busca (F3). Edite quantidade, preço e desconto do item direto na grade. O desconto de um item não pode passar do valor dele.
            - Desconto geral e frete ficam no rodapé. O total é recalculado na hora.
            - Pagamento e parcelas são a condição combinada (pode ser alterada ao faturar/receber).
            - Salvar e faturar / Salvar e receber: atalho para concluir o pedido sem voltar à lista.
            - Documentos que não estão em aberto abrem somente para consulta.
            """),

        new("vendas", "Vendas realizadas", """
            Histórico de vendas (PDV e pedidos faturados) por período, com os itens e o lucro bruto de cada item.

            - Comprovante: reimprime o comprovante não fiscal.
            - Cancelar venda (somente administrador): devolve os produtos ao estoque, cancela as contas a receber da venda e reabre o pedido de origem, se houver. A nota fiscal emitida para a venda deve ser cancelada em Fiscal > Notas fiscais.
            - Pagamentos de cartão/PIX feitos pelo TEF são estornados junto. Se a venda era a prazo e alguma parcela já foi recebida, a confirmação avisa o valor: ele também é cancelado no financeiro e deve ser acertado com o cliente.
            """),

        new("compras", "Compras realizadas", """
            Histórico de compras (pedidos recebidos e notas importadas) por período, com os itens.

            - Cancelar compra (somente administrador): retira os produtos do estoque, cancela as contas a pagar e reabre o pedido de origem, se houver. O custo médio não é recalculado retroativamente.
            - Antes de cancelar, o sistema avisa quais produtos ficarão com estoque negativo (mercadoria já vendida) e se há contas da compra já pagas.
            - Nota importada por XML com vínculo ou fator errado: cancele a compra aqui e importe o XML de novo.
            """),

        new("ImportarXmlForm", "Importar XML da nota do fornecedor", """
            Lê o arquivo XML da NF-e recebida do fornecedor e lança a compra.

            ## Conferência
            - Fornecedor: localizado pelo CNPJ; se não existir, é cadastrado.
            - Produto no sistema: o item é ligado ao produto já usado antes para aquele fornecedor/código ou pelo código de barras. Escolha "(cadastrar novo)" para criar o produto.
            - Fator: quantas unidades suas vêm em cada unidade do fornecedor (ex.: caixa com 12 → 12).

            ## O que acontece ao importar
            - Estoque: entra quantidade × fator.
            - Custo: valor dos produtos − desconto + frete + seguro + outras despesas + IPI + ICMS-ST, dividido pela quantidade; recalcula o custo médio.
            - Contas a pagar: uma conta por duplicata da nota (com os vencimentos dela), ou conforme a forma escolhida.
            - Os vínculos e fatores ficam memorizados para as próximas notas do mesmo fornecedor.
            - A mesma nota (chave de acesso) não pode ser importada duas vezes. Importou com vínculo ou fator errado? Cancele a compra em Compras > Compras realizadas e importe o XML de novo (a importação anterior fica no histórico como cancelada).
            - Produtos novos entram com preço de venda zero: ajuste em Cadastros > Produtos.
            - Se a mesma NF (fornecedor + número) já entrou por um pedido de compra recebido, "Lançar compra" vem desmarcado para não dar entrada em dobro.
            """),

        new("FichasTecnicasForm", "Fichas técnicas", """
            A ficha técnica é a receita do produto fabricado: quais insumos e quanto de cada um.

            - Selecione o produto à esquerda (produtos acabados aparecem primeiro).
            - Rendimento: quanto a receita produz (ex.: 10 kg de pão). As quantidades dos insumos são para esse rendimento.
            - Perda %: quebra/desperdício do insumo no processo (soma na necessidade e no custo).
            - Custo adicional por lote: mão de obra, energia, embalagem.
            - O rodapé mostra o custo do lote, o custo unitário estimado e a margem sobre o preço de venda (em vermelho se negativa).
            - Digitar o código de um insumo que já está na ficha soma a quantidade na mesma linha. F3 abre a busca de produtos.
            - O produto não pode ser insumo de si mesmo, nem de forma indireta (ex.: a massa usa a torta e a torta usa a massa).
            - Trocar de produto ou fechar a tela com a ficha alterada e não salva pede confirmação antes de descartar.
            - "Nova ordem de produção" já abre a OP para o produto.
            """),
        new("OrdensProducaoForm", "Ordens de produção", """
            A ordem de produção (OP) planeja e registra a fabricação.

            ## Fluxo
            - Nova OP: escolha o produto (precisa de ficha técnica) e a quantidade; a necessidade de insumos é calculada pela ficha (proporção do rendimento + perdas).
            - Iniciar produção: marca a OP como em produção.
            - Concluir: informe a quantidade realmente produzida. Os insumos são baixados do estoque na proporção, o produto acabado entra no estoque e o custo médio dele é recalculado com o custo real.
            - Cancelar: só OP não concluída. Imprimir: ficha da OP para a produção.

            ## Insumos insuficientes
            A grade de baixo mostra previsto, consumido e saldo. Ao concluir, a falta é conferida com a quantidade realmente produzida (produzir mais que o planejado consome mais insumo). Faltando insumo, a regra de Configurações > Produção decide: perguntar, permitir ou bloquear.
            A OP concluída sai do filtro "Em aberto": use "Concluídas" ou "Todas" para vê-la.
            """),
        new("EstoqueForm", "Estoque", """
            Posição do estoque, valor investido, sugestão de compra e histórico de movimentações de cada produto.

            ## Situação
            - SEM ESTOQUE (saldo ≤ 0), BAIXO (saldo ≤ mínimo), OK.
            - Sugestão de compra = mínimo − saldo, quando positivo.
            - Gerar pedido de compra: monta um pedido com a sugestão dos produtos listados (respeita a pesquisa e "Somente estoque baixo"), pelo custo médio atual. Escolha o fornecedor, ajuste quantidades/preços e salve.

            ## Ajustar estoque
            - Entrada ou Saída (perda, quebra, uso interno) somam/subtraem do saldo.
            - Definir saldo (inventário) grava a diferença entre o contado e o saldo atual do momento (se houve venda durante a contagem, ela é considerada).
            - Saída maior que o saldo pede confirmação antes de deixar o estoque negativo.
            - Todo ajuste exige motivo e fica no histórico com data e usuário.
            """),

        new("FretesForm", "Fretes e CIOT", """
            Controle dos fretes: contratação, CIOT, contas a pagar ao transportador e situação da viagem.

            ## Frete
            - Modalidade: frota própria ou contratado. Informe transportador, motorista, veículo, rota, carga e valores (frete, pedágio, adiantamento).
            - Peso acima da capacidade cadastrada do veículo pede confirmação ao salvar.
            - Situação: ABERTO → EM TRÂNSITO → ENTREGUE (ou CANCELADO).
            - Gerar contas a pagar: adiantamento (na data do frete) e saldo (na previsão de entrega) para o transportador, uma única vez. Depois disso, valores e transportador ficam travados no editor. Frete cancelado ou sem valor não gera contas.

            ## CIOT
            - O Código Identificador da Operação de Transporte é obrigatório quando o transportador é autônomo (TAC, CPF) — o editor avisa. Sem o CIOT declarado (ou com ele cancelado), o frete não passa para EM TRÂNSITO nem ENTREGUE.
            - Declarar CIOT envia a operação à administradora configurada (Pamcard, ou SIMULADOR para testes) e grava o código. Depois de declarado, valores, envolvidos e rota (origem/destino) ficam travados.
            - Encerrar CIOT ao final da viagem (ao marcar Entregue com CIOT declarado, o sistema lembra); Cancelar CIOT (com motivo) se a viagem não acontecer — cancele o CIOT antes de cancelar o frete.
            - Configure a administradora e as credenciais em Configurações > Transporte e CIOT.
            """),
        new("FinanceiroForm", "Financeiro - contas a receber e a pagar", """
            Todas as contas geradas por vendas, compras e lançamentos manuais.

            - Filtros por tipo, situação e período de vencimento. "Vencidas" mostra as abertas com vencimento passado e os dias de atraso.
            - Nova conta: despesas e receitas avulsas (aluguel, energia, serviços), com parcelas mensais.
            - Baixar: registra o pagamento/recebimento (data, valor e forma). Juros ou descontos são lançados ajustando o valor. A data não pode ser futura.
            - Estornar baixa: volta a conta para aberta (limpa data, valor e forma do pagamento).
            - Cancelar conta: somente contas em aberto.
            - Conta com boleto em aberto: ao baixar ou cancelar, o boleto é cancelado (se ainda não foi ao banco) ou tem a baixa pedida na próxima remessa, para o cliente não pagar duas vezes.
            - O rodapé mostra o total em aberto a receber (com quanto já está vencido) e a pagar, o saldo previsto e o realizado no mês.
            - Com "Vencimento de" marcado, mudar as datas já atualiza a lista.
            """),

        new("CobrancaForm", "Cobrança bancária (boletos)", """
            Boletos, remessa e retorno no padrão CNAB 240 Febraban — serve para qualquer banco configurado em Financeiro > Contas bancárias.

            ## Boletos
            - Emitir boletos: escolha as contas a receber em aberto (de clientes identificados). O sistema gera nosso número, código de barras e linha digitável. Contas já vencidas recebem no boleto o novo vencimento informado na janela (o banco recusa vencimento passado).
            - Visualizar/imprimir: ficha de compensação com código de barras.
            - Cancelar: boleto ainda não enviado é cancelado; já enviado vira pedido de baixa na próxima remessa.

            ## Remessa e retorno
            - Gerar remessa: arquivo com os boletos novos e pedidos de baixa, para enviar ao banco.
            - Importar retorno: lê o arquivo do banco; confirma registros, aponta rejeições e liquida os pagos (a conta a receber é baixada sozinha).
            - O mesmo retorno não é importado duas vezes, e liquidação repetida é ignorada. Se o banco liquidar um boleto cuja conta já tinha sido recebida no caixa ou cancelada, o retorno avisa para conferir pagamento em duplicidade.
            - Conta a receber baixada ou cancelada no Financeiro encerra o boleto dela: o não enviado é cancelado; o já enviado ganha pedido de baixa na próxima remessa.

            ## Homologação
            Siga o roteiro da aba: boletos de teste, remessa de teste, validador CNAB 240 (confere tamanho das linhas, lotes, quantidades e totais) e simulação do retorno do banco (só no ambiente HOMOLOGACAO, para não baixar contas sem o dinheiro ter entrado). Aprovado pelo banco, mude a conta para PRODUCAO (antes, cancele os boletos de teste não enviados: a remessa de produção os recusa).
            """),
        new("LocalizarTelaForm", "Buscar tela", """
            Digite uma ou mais palavras (ex.: "boleto", "nota entrada", "senha"). Aparecem todas as telas relacionadas, ordenadas pela relevância — a busca considera o nome da tela, palavras-chave e o texto de ajuda dela.
            - Setas ↑ ↓ para escolher, Enter para abrir, Esc para fechar.
            - Atalho em qualquer lugar da janela principal: Ctrl+K.
            """),
        new("NotasForm", "Notas fiscais", """
            Notas de saída (vendas) e de entrada (compras), próprias ou importadas do fornecedor.

            ## Emissão
            - Nova NF de saída: a partir de uma venda sem nota, ou avulsa.
            - Nova NF de entrada: a partir de uma compra sem nota, ou avulsa.
            - Emitir: valida a nota, atribui o próximo número da série, gera a chave de acesso de 44 dígitos e mostra o DANFE. Depois de emitida a nota não pode ser alterada.
            - Cancelar: notas emitidas são canceladas (somente administrador); rascunhos em digitação são excluídos. Cancelar a nota não desfaz a venda/compra.
            - Uma venda/compra tem uma única nota não cancelada: pedir a nota de novo abre a existente.

            ## Importante
            A transmissão à SEFAZ (assinatura com certificado digital A1) ainda não está ativa: a numeração e o controle ficam no sistema e o DANFE sai marcado "sem valor fiscal".
            Notas de compra com XML são importadas em Compras > Importar XML e aparecem aqui como "Fornecedor (XML)".
            """),

        new("NotaEditorForm", "Editar nota fiscal", """
            - Destinatário/remetente: escolha do cadastro (preenche os dados) ou digite.
            - CFOP: 5102/6102 na saída e 1102/2102 na entrada, conforme a UF do participante ser igual ou diferente da UF da loja. Muda sozinho ao trocar a UF.
            - Fechar com alterações não salvas pede confirmação (para não perder a nota digitada).
            - Modelo: ao trocar entre NF-e (55) e NFC-e (65), a série passa a ser a configurada para o modelo (Configurações > Fiscal).
            - Ao emitir, o sistema confere o CFOP digitado: saída começa por 5/6/7 e entrada por 1/2/3; dentro do estado 5/1, outro estado 6/2 (exterior 7/3).
            - Também confere: venda/compra de origem não cancelada, CPF/CNPJ do participante (se informado), desconto do item entre zero e o valor do item, desconto adicional até o valor dos produtos e NFC-e (65) só em nota de saída.
            - NCM (8 dígitos), CST/CSOSN e alíquotas vêm do cadastro do produto e podem ser editados na grade.
            - Impostos: ICMS, IPI, PIS e COFINS = valor do item × alíquota. O IPI soma no total da nota.
            - Reforma tributária: IBS (estadual e municipal), CBS e Imposto Seletivo por item, com CST IBS/CBS e cClassTrib. Em 2026 ficam destacados sem somar ao total.
            - Total da nota = produtos − descontos + frete + IPI.
            - Salvar rascunho guarda sem numerar. Salvar e emitir valida e numera.
            """),

        new("RelatoriosForm", "Relatórios", """
            Relatórios em tabela, com totais e exportação para CSV (abre no Excel).

            - Vendas por dia, produtos mais vendidos (com custo e lucro), vendas por cliente e por vendedor.
            - Lucro bruto do período: faturamento líquido − custo das mercadorias vendidas (custo médio no momento da venda).
            - Fluxo de caixa realizado (contas pagas por dia) e previsto (contas em aberto por vencimento).
            - Produtos sem venda no período (insumos de produção ficam de fora, pois não são vendidos) e compras por fornecedor.

            Escolha o relatório à esquerda e o período no topo: o relatório é atualizado na hora (o botão Gerar refaz a consulta). Se a data final for anterior à inicial, o sistema avisa em vez de mostrar um relatório vazio.

            Visões que já existem em outras telas não se repetem aqui: gráficos no painel inicial, estoque baixo/inventário na tela Estoque, contas vencidas no Financeiro.
            """),

        new("ConfiguracoesForm", "Configurações", """
            Tela única de configuração. Administradores veem todas as abas; os demais usuários veem apenas Minha conta. Cada aba tem seu botão Salvar.

            - Empresa: dados que saem no comprovante, pedidos, boletos e DANFE. CNPJ e UF são necessários para a chave de acesso.
            - Fiscal: regime tributário, séries e próximos números da NF-e/NFC-e, naturezas de operação, CST padrão.
            - Reforma tributária: liga o cálculo de IBS/CBS e define as alíquotas do ano (padrão 2026: CBS 0,9% e IBS 0,1%, em caráter de teste). Confirme as alíquotas com o contador a cada ano da transição.
            - Vendas e compras: venda sem estoque, desconto máximo, validade do orçamento, prazos, parcelas, número do caixa e impressão do comprovante.
            - Produção: o que fazer quando falta insumo ao concluir uma OP.
            - Transporte e CIOT: administradora (Pamcard, simulador...), ambiente e credenciais.
            - TEF: liga o TEF no PDV e escolhe o provedor (SIMULADOR para testes).
            - Impressoras: impressora de cupom (Windows ou térmica ESC/POS com corte e gaveta), colunas, acentuação e impressora de documentos A4. Use "Imprimir página de teste".
            - E-mail: chave do Resend e remetente para a recuperação de senha. Use "Enviar e-mail de teste".
            - Banco de dados: arquivo em uso, migrações aplicadas, backups, dias de log mantidos, troca de banco e geração de dados de demonstração (apenas para testes).
            - Minha conta: seu e-mail de recuperação e troca de senha.

            Ao salvar, valores inválidos são recusados com uma mensagem e o cursor vai para o campo: números inteiros (séries, prazos, parcelas, backups, dias de log) não aceitam letras, pontos nem vírgulas; próximo número da nota e máximo de parcelas começam em 1; percentuais vão de 0 a 100; o nome da loja é obrigatório.
            """),

        new("LogForm", "Log de atividades", """
            Registro de tudo o que é feito no sistema: quem fez, o quê, em qual tela, quando e em qual computador. Somente administradores.

            ## Categorias
            - ACESSO: entradas, saídas, senhas erradas, troca e recuperação de senha e tentativas de abrir telas de administrador.
            - TELA: telas abertas e fechadas.
            - AÇÃO: botões clicados, confirmações (sim/não), mensagens exibidas, exportações e operações do PDV.
            - DADOS: gravações no banco (o que foi incluído, alterado ou excluído e em quais tabelas). Só aparecem gravações confirmadas.
            - ERRO: avisos de erro mostrados ao usuário e erros inesperados (com detalhes técnicos para o suporte).
            - SISTEMA: início do sistema, migrações, backups, atalho criado, dados de demonstração.

            ## Como usar
            Escolha o período e, se quiser, a categoria, o usuário ou um texto (ex.: nome de tela, "vendas", "Excluiu"). Clique numa linha para ver o detalhe completo embaixo. "Exportar CSV" gera planilha do que está na tela.

            ## Onde fica
            O log é gravado em um banco separado, ao lado do banco da loja (ex.: loja.db → loja_log.db). Senhas e valores digitados não são gravados no log. Registros mais antigos que o configurado em Configurações > Banco de dados (padrão 365 dias) são apagados ao abrir o sistema.
            """),

        new("cadastro_pessoas", "Clientes e fornecedores", """
            Cadastro único de pessoas e empresas. Uma mesma pessoa pode ter vários papéis — marque cliente, fornecedor, transportador e/ou motorista em vez de cadastrar duas vezes.

            - Use o filtro "Mostrar" para ver só clientes, fornecedores, transportadores, motoristas ou inativos.
            - CPF/CNPJ é conferido pelos dígitos verificadores e não pode repetir: se a pessoa já existe, o aviso diz qual é — edite-a e marque o papel que falta.
            - UF e inscrição estadual definem o CFOP das notas.
            - Limite de crédito: quanto o cliente pode ter em aberto em vendas a prazo (0 = sem limite).
            - Transportador: informe o RNTRC (exigido no CIOT). Motorista: CPF e CNH obrigatórios.
            - Quem tem histórico (vendas, compras, fretes) não pode ser excluído: o sistema oferece inativar.
            """),


        new("cadastro_produtos", "Cadastro de produtos", """
            Código e descrição são obrigatórios; o código e o código de barras são usados no PDV.

            - Tipo: Mercadoria (revenda), Produto acabado (fabricado com ficha técnica) ou Insumo (matéria-prima, não aparece para venda na ficha).
            - Preço de custo é o custo médio, atualizado a cada compra recebida ou produção concluída. A margem é calculada sobre o custo.
              Pode ser informado como custo inicial enquanto o produto não tiver movimentação de estoque; depois disso não é alterado à mão.
            - Estoque mínimo define a situação BAIXO e a sugestão de compra. O saldo muda só por vendas, compras, produção e ajustes.
            - Código de barras: EAN/GTIN de 8, 12, 13 ou 14 dígitos com dígito verificador correto, sem repetir o de outro produto. Deixe em branco se não houver.
            - Dados fiscais (NCM, CST/CSOSN, alíquotas) preenchem as notas automaticamente. NCM, se informado, tem 8 dígitos; CST tem 2 dígitos (regime normal) ou CSOSN 3 (Simples).
            - Reforma tributária: CST IBS/CBS, classificação tributária (cClassTrib), reduções de alíquota (regimes diferenciados) e Imposto Seletivo.
            - Produto com movimentação não pode ser excluído: o sistema oferece inativar.
            """),

        new("cadastro_veiculos", "Cadastro de veículos", """
            Veículos da frota própria e de terceiros usados nos fretes.

            - Placa no padrão Mercosul (ABC1D23) ou antigo (ABC1234), sem hífen.
            - RENAVAM: só números (9 a 11 dígitos). RNTRC: 8 ou 9 dígitos.
            - Veículo de terceiro precisa do proprietário (cadastrado como transportador) e, para o CIOT, do RNTRC.
            - Capacidade e tara ajudam a conferir o peso das cargas.
            """),
        new("cadastro_contas_bancarias", "Contas bancárias (cobrança)", """
            Conta usada para emitir boletos. Funciona com qualquer banco: os dados vêm do contrato de cobrança e do manual técnico do banco.

            ## Campo livre
            As 25 posições do código de barras que cada banco define. Monte o modelo com as marcações:
            - {agencia:N} {agenciadv:N} {conta:N} {contadv:N} {carteira:N} {convenio:N} {nossonumero:N} {zeros:N} e dígitos fixos.
            - N é o número de posições (zeros à esquerda). O total precisa somar 25 e o modelo tem que conter {nossonumero:N}.

            ## Demais campos
            - Dígitos do nosso número (1 a 20, no máximo o espaço reservado no modelo) e último usado: o próximo boleto usa o seguinte. O último usado não pode voltar abaixo de um nosso número já emitido.
            - Espécie do título: código numérico de 2 dígitos do manual do banco (02 = duplicata mercantil).
            - Multa, juros ao mês e dias para protesto vão na remessa e nas instruções do boleto.
            - Ambiente HOMOLOGACAO marca boletos e arquivos como teste; mude para PRODUCAO após a aprovação do banco.
            """),
        new("cadastro_categorias", "Cadastro de categorias", """
            Agrupam os produtos nos relatórios e no painel. Categoria em uso por produtos não pode ser excluída.
            """),

        new("cadastro_usuarios", "Cadastro de usuários", """
            - Username: usado para entrar (3 a 30 caracteres: letras, números, ponto, hífen ou sublinhado).
            - Senha: obrigatória no cadastro (mínimo 8 caracteres, sem senhas óbvias como 12345678); ao editar, deixe em branco para manter a atual (é assim que o administrador redefine a senha de alguém).
            - E-mail: obrigatório e único; é para onde vai o código de recuperação de senha.
            - Administrador: acessa usuários e configurações, cancela vendas/compras e concede descontos acima do limite.
            - Ativo: usuário inativo não consegue entrar. Você não pode excluir nem inativar o próprio usuário, nem tirar o seu acesso de administrador (peça a outro administrador).
            - O sistema nunca fica sem administrador ativo: antes de inativar, excluir ou rebaixar o último, marque outro usuário como administrador.
            """),

        new("ajuste_estoque", "Ajuste de estoque", """
            - Entrada: soma a quantidade ao saldo.
            - Saída: subtrai (perda, quebra, consumo interno).
            - Definir saldo (inventário): informe a quantidade contada; o sistema grava a diferença.
            O motivo é obrigatório e fica no histórico de movimentações.
            """),

        new("conta_nova", "Nova conta", """
            Lançamento manual de conta a pagar (fornecedor opcional) ou a receber (cliente opcional).
            Com mais de uma parcela, o valor é dividido igualmente e os vencimentos são mensais a partir do 1º vencimento; centavos de diferença ficam na última parcela.
            """),

        new("conta_baixa", "Baixa de conta", """
            Registra o recebimento/pagamento. Informe a data efetiva (hoje ou passada, nunca futura), o valor (ajuste para juros ou desconto) e a forma.
            Se a conta tiver boleto em aberto, ele é cancelado ou tem a baixa pedida ao banco na próxima remessa.
            Valor mais que o dobro ou menos que a metade do valor da conta pede confirmação (evita erro de digitação).
            O valor pago é o que entra no fluxo de caixa realizado e no gráfico de entradas x saídas.
            """),

        new("pedido_pagamento", "Condição de pagamento", """
            À vista: a conta já entra como paga na data de hoje.
            A prazo: gera as parcelas em aberto, mensais a partir do 1º vencimento.
            Venda a prazo exige cliente identificado.
            """),

        new("nota_origem", "Origem da nota", """
            Escolha a venda (ou compra) que a nota vai documentar: itens, valores e participante são copiados dela. Aparecem apenas documentos dos últimos 180 dias que ainda não têm nota.
            Escolha "nota avulsa" para digitar a nota do zero.
            """),
    ];

    static readonly HashSet<string> usadas = new();

    /// <summary>Chaves de ajuda usadas pelas telas abertas até agora (para os testes conferirem).</summary>
    public static IReadOnlyCollection<string> ChavesUsadas => usadas;

    public static bool Existe(string chave) => Topicos.Any(t => t.Chave == chave);

    /// <summary>Botão "?" no canto superior direito + F1.</summary>
    public static void Instalar(Form f, string chave)
    {
        usadas.Add(chave);
        var botao = new Button
        {
            Text = "?",
            Size = new Size(32, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Ui.Primaria,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 12F),
            Cursor = Cursors.Hand,
            TabStop = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            UseVisualStyleBackColor = false,
        };
        botao.FlatAppearance.BorderSize = 0;
        using (var circulo = new System.Drawing.Drawing2D.GraphicsPath())
        {
            circulo.AddEllipse(0, 0, 32, 32);
            botao.Region = new Region(circulo);
        }
        new ToolTip().SetToolTip(botao, "Ajuda sobre esta tela (F1)");
        botao.Click += (_, _) => Mostrar(f, chave);
        f.HelpRequested += (_, e) =>
        {
            e.Handled = true;
            Mostrar(f, chave);
        };
        f.Load += (_, _) =>
        {
            botao.Location = new Point(f.ClientSize.Width - botao.Width - 12, 7);
            f.Controls.Add(botao);
            botao.BringToFront();
        };
    }

    /// <summary>Diálogos: usa o "?" nativo da barra de título + F1.</summary>
    public static void InstalarEmDialogo(Form f, string chave)
    {
        usadas.Add(chave);
        f.HelpButton = true;
        f.HelpButtonClicked += (_, e) =>
        {
            e.Cancel = true;
            Mostrar(f, chave);
        };
        f.HelpRequested += (_, e) =>
        {
            e.Handled = true;
            Mostrar(f, chave);
        };
    }

    public static void Mostrar(IWin32Window? dono, string chave)
    {
        using var f = new AjudaForm(chave);
        f.ShowDialog(dono);
    }

    /// <summary>Gera o manual do usuário (Markdown) a partir dos mesmos tópicos.</summary>
    public static string ManualMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Manual do usuário — ERP Loja");
        sb.AppendLine();
        sb.AppendLine("> Gerado automaticamente a partir da ajuda do sistema (botão **?** / F1 em cada tela). Não edite à mão: altere `ErpLoja/UI/Ajuda.cs`.");
        sb.AppendLine();
        foreach (var t in Topicos)
        {
            sb.AppendLine($"## {t.Titulo}");
            sb.AppendLine();
            foreach (var linha in t.Texto.Split('\n').Select(l => l.TrimEnd('\r')))
                sb.AppendLine(linha.StartsWith("## ") ? "#" + linha : linha);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

/// <summary>Janela de ajuda: mostra somente a explicação da tela de onde foi chamada.</summary>
public class AjudaForm : Form
{
    readonly RichTextBox texto = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = Ui.Fonte };

    public AjudaForm(string chave)
    {
        var topico = Ajuda.Topicos.FirstOrDefault(t => t.Chave == chave)
                     ?? new TopicoAjuda(chave, "Ajuda", "Ainda não há explicação cadastrada para esta tela.");
        Text = "Ajuda — " + topico.Titulo;
        Font = Ui.Fonte;
        Icon = Ui.Icone;
        Size = new Size(720, 600);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        BackColor = Color.White;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        var conteudo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 16, 16), BackColor = Color.White };
        conteudo.Controls.Add(texto);
        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10), BackColor = Color.White };
        rodape.Controls.Add(Ui.Botao("Fechar", (_, _) => Close(), primario: true));
        Controls.Add(conteudo);
        Controls.Add(rodape);
        Renderizar(topico);
    }

    void Renderizar(TopicoAjuda t)
    {
        texto.Clear();
        void Escrever(string s, Font fonte, Color cor, bool marcador = false)
        {
            texto.SelectionStart = texto.TextLength;
            texto.SelectionFont = fonte;
            texto.SelectionColor = cor;
            texto.SelectionBullet = marcador;
            texto.SelectionIndent = marcador ? 8 : 0;
            texto.AppendText(s + "\n");
        }
        using var titulo = new Font("Segoe UI Semibold", 15F);
        using var subtitulo = new Font("Segoe UI Semibold", 11.5F);
        Escrever(t.Titulo, titulo, Color.Black);
        Escrever("", Ui.Fonte, Color.Black);
        foreach (var bruta in t.Texto.Split('\n'))
        {
            var linha = bruta.TrimEnd('\r');
            if (linha.StartsWith("## "))
            {
                Escrever("", Ui.Fonte, Color.Black);
                Escrever(linha[3..], subtitulo, Ui.Primaria);
            }
            else if (linha.StartsWith("- "))
                Escrever(linha[2..], Ui.Fonte, Color.FromArgb(31, 41, 55), marcador: true);
            else
                Escrever(linha, Ui.Fonte, Color.FromArgb(31, 41, 55));
        }
        texto.Select(texto.TextLength, 0);
        texto.SelectionBullet = false;
        texto.SelectionStart = 0;
        texto.ScrollToCaret();
    }
}
