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
            O ERP Loja controla vendas, compras, estoque, financeiro e notas fiscais de uma loja.

            ## Menu lateral
            Cada função existe em um único lugar do menu:
            - Vendas: PDV (balcão), orçamentos, pedidos de venda e o histórico de vendas.
            - Compras: pedidos de compra, importação do XML da nota do fornecedor e o histórico de compras.
            - Estoque, Financeiro, Fiscal (notas fiscais), Cadastros e Relatórios.
            - Configurações: todos os parâmetros do sistema, banco de dados, e-mail e sua senha.

            ## Painel (dashboard)
            A tela inicial mostra os indicadores do período escolhido: faturamento, lucro bruto, margem, número de vendas, ticket médio e itens vendidos, cada um comparado com o período anterior de mesmo tamanho (▲ subiu, ▼ caiu). Os gráficos mostram faturamento por dia (ou por mês em períodos longos), produtos e categorias que mais venderam, formas de pagamento, entradas x saídas do caixa e movimento por hora. Passe o mouse sobre uma barra para ver o valor.

            ## Atalhos
            - F2 PDV · F4 Pedidos de compra · F6 Estoque · F7 Financeiro · F8 Relatórios
            - Ctrl+O Orçamentos · Ctrl+P Pedidos de venda · Ctrl+N Notas fiscais
            - F1 em qualquer tela abre esta ajuda.
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

            ## Esqueci minha senha
            Clique em "Esqueci minha senha": o sistema envia um código de 6 dígitos para o e-mail cadastrado no seu usuário. O código vale 15 minutos e aceita até 5 tentativas.
            Se o seu usuário não tiver e-mail, um administrador pode definir uma nova senha em Cadastros > Usuários.
            """),

        new("RecuperarSenhaForm", "Recuperar senha", """
            1. Digite seu username (ou e-mail) e clique em Enviar código.
            2. Abra seu e-mail, copie o código de 6 dígitos e informe aqui junto com a nova senha.

            - O código vale 15 minutos e aceita até 5 tentativas. Pedir um novo código invalida o anterior.
            - O envio usa o serviço Resend, configurado pelo administrador em Configurações > E-mail.
            """),

        new("PdvForm", "PDV - venda no balcão", """
            Venda rápida, com pagamento na hora (ou a prazo para cliente cadastrado).

            ## Como vender
            - Leia o código de barras ou digite o código e tecle Enter. Sem código, F3 abre a busca.
            - Para vender mais de uma unidade, informe a quantidade antes de adicionar. Quantidade e preço também podem ser editados na grade.
            - Escolha a forma de pagamento. Em dinheiro, informe o valor recebido para calcular o troco.
            - F10 finaliza. Del remove o item selecionado.

            ## Regras
            - A venda baixa o estoque e lança o recebimento no financeiro (à vista já entra como pago; a prazo gera as parcelas mensais em aberto).
            - Venda a prazo exige cliente identificado.
            - Estoque insuficiente: o comportamento (perguntar, permitir ou bloquear) é definido em Configurações > Vendas e compras.
            - Desconto acima do limite configurado só com usuário administrador.
            - Para venda com entrega, negociação ou prazo de aprovação, use Orçamentos / Pedidos de venda.
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
            - Venda a prazo exige cliente. A regra de estoque insuficiente vale também aqui.
            """),

        new("pedidos_COMPRA", "Pedidos de compra", """
            Compra de mercadoria do fornecedor. É também a forma de dar entrada manual de mercadoria.

            ## Fluxo
            - Novo → ABERTO. Para entrada imediata use "Salvar e receber" no próprio pedido.
            - Receber mercadoria: dá entrada no estoque, recalcula o custo médio e lança o contas a pagar; o pedido fica RECEBIDO.
            - Cancelar: só para pedido em aberto. Se a compra gerada for cancelada depois, o pedido volta para ABERTO.

            ## Custo
            Frete e descontos do pedido são distribuídos proporcionalmente no custo dos itens.
            Para notas com XML, prefira Compras > Importar XML: cadastra produtos e lança tudo automaticamente.
            """),

        new("PedidoEditorForm", "Editar orçamento / pedido", """
            - Adicione itens pelo código (Enter) ou pela busca (F3). Edite quantidade, preço e desconto do item direto na grade.
            - Desconto geral e frete ficam no rodapé. O total é recalculado na hora.
            - Pagamento e parcelas são a condição combinada (pode ser alterada ao faturar/receber).
            - Salvar e faturar / Salvar e receber: atalho para concluir o pedido sem voltar à lista.
            - Documentos que não estão em aberto abrem somente para consulta.
            """),

        new("vendas", "Vendas realizadas", """
            Histórico de vendas (PDV e pedidos faturados) por período, com os itens e o lucro bruto de cada item.

            - Comprovante: reimprime o comprovante não fiscal.
            - Cancelar venda (somente administrador): devolve os produtos ao estoque, cancela as contas a receber da venda e reabre o pedido de origem, se houver. A nota fiscal emitida para a venda deve ser cancelada em Fiscal > Notas fiscais.
            """),

        new("compras", "Compras realizadas", """
            Histórico de compras (pedidos recebidos e notas importadas) por período, com os itens.

            - Cancelar compra (somente administrador): retira os produtos do estoque, cancela as contas a pagar e reabre o pedido de origem, se houver. O custo médio não é recalculado retroativamente.
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
            - A mesma nota (chave de acesso) não pode ser importada duas vezes.
            - Produtos novos entram com preço de venda zero: ajuste em Cadastros > Produtos.
            """),

        new("EstoqueForm", "Estoque", """
            Posição do estoque, valor investido, sugestão de compra e histórico de movimentações de cada produto.

            ## Situação
            - SEM ESTOQUE (saldo ≤ 0), BAIXO (saldo ≤ mínimo), OK.
            - Sugestão de compra = mínimo − saldo, quando positivo.

            ## Ajustar estoque
            - Entrada ou Saída (perda, quebra, uso interno) somam/subtraem do saldo.
            - Definir saldo (inventário) grava a diferença entre o contado e o saldo atual.
            - Todo ajuste exige motivo e fica no histórico com data e usuário.
            """),

        new("FinanceiroForm", "Financeiro - contas a receber e a pagar", """
            Todas as contas geradas por vendas, compras e lançamentos manuais.

            - Filtros por tipo, situação e período de vencimento. "Vencidas" mostra as abertas com vencimento passado e os dias de atraso.
            - Nova conta: despesas e receitas avulsas (aluguel, energia, serviços), com parcelas mensais.
            - Baixar: registra o pagamento/recebimento (data, valor e forma). Juros ou descontos são lançados ajustando o valor.
            - Estornar baixa: volta a conta para aberta.
            - Cancelar conta: somente contas em aberto.
            - O rodapé mostra o total em aberto a receber e a pagar, o saldo previsto e o realizado no mês.
            """),

        new("NotasForm", "Notas fiscais", """
            Notas de saída (vendas) e de entrada (compras), próprias ou importadas do fornecedor.

            ## Emissão
            - Nova NF de saída: a partir de uma venda sem nota, ou avulsa.
            - Nova NF de entrada: a partir de uma compra sem nota, ou avulsa.
            - Emitir: valida a nota, atribui o próximo número da série, gera a chave de acesso de 44 dígitos e mostra o DANFE. Depois de emitida a nota não pode ser alterada.
            - Cancelar: notas emitidas são canceladas; rascunhos em digitação são excluídos. Cancelar a nota não desfaz a venda/compra.

            ## Importante
            A transmissão à SEFAZ (assinatura com certificado digital A1) ainda não está ativa: a numeração e o controle ficam no sistema e o DANFE sai marcado "sem valor fiscal".
            Notas de compra com XML são importadas em Compras > Importar XML e aparecem aqui como "Fornecedor (XML)".
            """),

        new("NotaEditorForm", "Editar nota fiscal", """
            - Destinatário/remetente: escolha do cadastro (preenche os dados) ou digite.
            - CFOP: 5102/6102 na saída e 1102/2102 na entrada, conforme a UF do participante ser igual ou diferente da UF da loja. Muda sozinho ao trocar a UF.
            - NCM (8 dígitos), CST/CSOSN e alíquotas vêm do cadastro do produto e podem ser editados na grade.
            - Impostos: ICMS, IPI, PIS e COFINS = valor do item × alíquota. O IPI soma no total da nota.
            - Total da nota = produtos − descontos + frete + IPI.
            - Salvar rascunho guarda sem numerar. Salvar e emitir valida e numera.
            """),

        new("RelatoriosForm", "Relatórios", """
            Relatórios em tabela, com totais e exportação para CSV (abre no Excel).

            - Vendas por dia, produtos mais vendidos (com custo e lucro), vendas por cliente e por vendedor.
            - Lucro bruto do período: faturamento líquido − custo das mercadorias vendidas (custo médio no momento da venda).
            - Fluxo de caixa realizado (contas pagas por dia) e previsto (contas em aberto por vencimento).
            - Produtos sem venda no período e compras por fornecedor.

            Visões que já existem em outras telas não se repetem aqui: gráficos no painel inicial, estoque baixo/inventário na tela Estoque, contas vencidas no Financeiro.
            """),

        new("ConfiguracoesForm", "Configurações", """
            Tela única de configuração. Administradores veem todas as abas; os demais usuários veem apenas Minha conta.

            - Empresa: dados que saem no comprovante, orçamentos e DANFE. CNPJ e UF são necessários para a chave de acesso das notas.
            - Fiscal: regime tributário, séries e próximos números da NF-e/NFC-e, naturezas de operação e CST padrão.
            - Vendas e compras: venda sem estoque (perguntar/permitir/bloquear), desconto máximo sem administrador, validade do orçamento, prazo do 1º vencimento e máximo de parcelas.
            - E-mail: chave da API do Resend e remetente, usados na recuperação de senha. Use "Enviar e-mail de teste" para conferir.
            - Banco de dados: arquivo em uso, versão e migrações aplicadas, backup agora, backup automático diário e troca de banco.
            - Minha conta: seu e-mail de recuperação e troca de senha.
            """),

        new("cadastro_clientes", "Cadastro de clientes", """
            Selecione um cliente na lista para editar, ou clique em Novo. Nome é obrigatório.
            UF e inscrição estadual são usados na nota fiscal (definem o CFOP dentro/fora do estado).
            Cliente com vendas não pode ser excluído: o sistema oferece inativá-lo (some das listas de seleção, mas o histórico continua).
            """),

        new("cadastro_fornecedores", "Cadastro de fornecedores", """
            Selecione um fornecedor para editar, ou clique em Novo. Fornecedores também são cadastrados automaticamente na importação de XML (pelo CNPJ).
            Fornecedor com compras não pode ser excluído: o sistema oferece inativá-lo.
            """),

        new("cadastro_produtos", "Cadastro de produtos", """
            Código e descrição são obrigatórios; o código e o código de barras são usados no PDV.

            - Preço de custo é o custo médio: atualizado automaticamente a cada compra recebida. A margem é calculada sobre o custo.
            - Estoque mínimo define a situação BAIXO e a sugestão de compra.
            - O saldo de estoque não é editado aqui: ele muda por vendas, compras e ajustes (tela Estoque).
            - Dados fiscais (NCM, CST/CSOSN, alíquotas) preenchem as notas automaticamente.
            - Produto com movimentação não pode ser excluído: o sistema oferece inativá-lo.
            """),

        new("cadastro_categorias", "Cadastro de categorias", """
            Agrupam os produtos nos relatórios e no painel. Categoria em uso por produtos não pode ser excluída.
            """),

        new("cadastro_usuarios", "Cadastro de usuários", """
            - Username: usado para entrar (3 a 30 caracteres: letras, números, ponto, hífen ou sublinhado).
            - Senha: obrigatória no cadastro; ao editar, deixe em branco para manter a atual (é assim que o administrador redefine a senha de alguém).
            - E-mail: obrigatório e único; é para onde vai o código de recuperação de senha.
            - Administrador: acessa usuários e configurações, cancela vendas/compras e concede descontos acima do limite.
            - Ativo: usuário inativo não consegue entrar. Você não pode excluir o próprio usuário.
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
            Registra o recebimento/pagamento. Informe a data efetiva, o valor (ajuste para juros ou desconto) e a forma.
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

/// <summary>Janela de ajuda: lista de tópicos à esquerda, conteúdo à direita.</summary>
public class AjudaForm : Form
{
    readonly ListBox topicos = new() { Dock = DockStyle.Left, Width = 260, BorderStyle = BorderStyle.None, IntegralHeight = false, ItemHeight = 24 };
    readonly RichTextBox texto = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = Ui.Fonte };

    public AjudaForm(string chave)
    {
        Text = "Ajuda — ERP Loja";
        Font = Ui.Fonte;
        Icon = Ui.Icone;
        Size = new Size(940, 640);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        BackColor = Color.White;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        topicos.DrawMode = DrawMode.OwnerDrawFixed;
        topicos.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            var selecionado = (e.State & DrawItemState.Selected) != 0;
            using var fundo = new SolidBrush(selecionado ? Color.FromArgb(219, 234, 254) : Color.FromArgb(249, 250, 251));
            e.Graphics.FillRectangle(fundo, e.Bounds);
            TextRenderer.DrawText(e.Graphics, ((TopicoAjuda)topicos.Items[e.Index]).Titulo, Ui.Fonte,
                new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        topicos.BackColor = Color.FromArgb(249, 250, 251);
        foreach (var t in Ajuda.Topicos)
            topicos.Items.Add(t);

        var conteudo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 14, 14), BackColor = Color.White };
        conteudo.Controls.Add(texto);
        Controls.Add(conteudo);
        Controls.Add(topicos);

        topicos.SelectedIndexChanged += (_, _) => { if (topicos.SelectedItem is TopicoAjuda t) Renderizar(t); };
        var inicial = Array.FindIndex(Ajuda.Topicos, t => t.Chave == chave);
        topicos.SelectedIndex = Math.Max(0, inicial);
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
