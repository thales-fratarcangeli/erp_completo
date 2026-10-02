namespace ErpLoja.Forms;

/// <summary>Janela principal: menu lateral (único ponto de navegação) e o painel gerencial ao centro.</summary>
public class MainForm : Form
{
    readonly Dictionary<string, Form> abertos = new();
    readonly Dictionary<Keys, Action> atalhos = new();
    readonly DashboardPainel painel = new() { Dock = DockStyle.Fill };
    readonly Label titulo = new() { AutoSize = true, Font = Ui.FonteTitulo, Margin = new Padding(16, 12, 0, 0) };

    public MainForm()
    {
        Ui.Estilizar(this, "ERP Loja", 1300, 820);
        WindowState = FormWindowState.Maximized;

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 236,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(10, 12, 10, 12),
        };
        var marca = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(4, 0, 0, 14) };
        marca.Controls.Add(new PictureBox { Image = Ui.Logo(36), Size = new Size(36, 36), Margin = new Padding(0, 0, 10, 0) });
        marca.Controls.Add(new Label { Text = "ERP Loja", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F), AutoSize = true, Margin = new Padding(0, 5, 0, 0) });
        menu.Controls.Add(marca);

        void Secao(string nome) => menu.Controls.Add(new Label
        {
            Text = nome.ToUpper(),
            ForeColor = Color.FromArgb(156, 163, 175),
            Font = new Font("Segoe UI Semibold", 8.5F),
            AutoSize = true,
            Margin = new Padding(8, 12, 0, 4),
        });
        Button Item(string texto, Action acao, Keys atalho = Keys.None)
        {
            var b = new Button
            {
                Text = "   " + texto + (atalho == Keys.None ? "" : $"  ({Ui.NomeAtalho(atalho)})"),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(229, 231, 235),
                BackColor = menu.BackColor,
                Size = new Size(206, 32),
                Margin = new Padding(0, 1, 0, 1),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(31, 41, 55);
            b.Click += (_, _) => acao();
            menu.Controls.Add(b);
            if (atalho != Keys.None)
                atalhos[atalho] = acao;
            return b;
        }

        Secao("Vendas");
        Item("PDV (balcão)", () => Abrir("pdv", () => new PdvForm()), Keys.F2);
        Item("Orçamentos", () => Abrir("orcamentos", () => new PedidosForm(Pedidos.Orcamento)), Keys.Control | Keys.O);
        Item("Pedidos de venda", () => Abrir("pedidos_venda", () => new PedidosForm(Pedidos.Venda)), Keys.Control | Keys.P);
        Item("Vendas realizadas", () => Abrir("vendas", () => new DocumentosForm(venda: true)));
        Secao("Compras");
        Item("Pedidos de compra", () => Abrir("pedidos_compra", () => new PedidosForm(Pedidos.Compra)), Keys.F4);
        Item("Importar XML de NF-e", () => { if (ImportarXmlForm.Executar(this)) painel.Atualizar(); });
        Item("Compras realizadas", () => Abrir("compras", () => new DocumentosForm(venda: false)));
        Secao("Gestão");
        Item("Estoque", () => Abrir("estoque", () => new EstoqueForm()), Keys.F6);
        Item("Financeiro", () => Abrir("financeiro", () => new FinanceiroForm()), Keys.F7);
        Item("Notas fiscais", () => Abrir("notas", () => new NotasForm()), Keys.Control | Keys.N);
        Item("Relatórios", () => Abrir("relatorios", () => new RelatoriosForm()), Keys.F8);
        Secao("Cadastros e sistema");
        var submenu = new ContextMenuStrip { Font = Ui.Fonte, ShowImageMargin = false };
        void Cadastro(string nome, string chave, Func<Form> criar) =>
            submenu.Items.Add(new ToolStripMenuItem(nome, null, (_, _) => Abrir(chave, criar)) { Padding = new Padding(4, 4, 24, 4) });
        Cadastro("Clientes", "clientes", Cadastros.Clientes);
        Cadastro("Fornecedores", "fornecedores", Cadastros.Fornecedores);
        Cadastro("Produtos", "produtos", Cadastros.Produtos);
        Cadastro("Categorias", "categorias", Cadastros.Categorias);
        if (Sessao.Admin)
            Cadastro("Usuários", "usuarios", Cadastros.Usuarios);
        Button? botaoCadastros = null;
        botaoCadastros = Item("Cadastros  ▸", () => submenu.Show(botaoCadastros!, new Point(botaoCadastros!.Width, 0)));
        Item("Configurações", () => Abrir("configuracoes", () => new ConfiguracoesForm()));
        Item("Sair", Close);

        var cabecalho = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, BackColor = Color.White, WrapContents = false, Padding = new Padding(0, 0, 52, 0) };
        cabecalho.Controls.Add(titulo);

        var status = new StatusStrip();
        status.Items.Add($"Usuário: {Sessao.Nome}{(Sessao.Admin ? " (administrador)" : "")}");
        status.Items.Add("   Banco: " + Db.Caminho);
        status.Items.Add($"   Versão {Application.ProductVersion.Split('+')[0]} · banco v{Migracoes.VersaoDoSistema}");

        Controls.Add(painel);
        Controls.Add(cabecalho);
        Controls.Add(menu);
        Controls.Add(status);

        Load += (_, _) => AtualizarCabecalho();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (atalhos.TryGetValue(keyData, out var acao))
        {
            acao();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void AtualizarCabecalho()
    {
        titulo.Text = Db.Config("nome_loja");
        Text = "ERP Loja - " + titulo.Text;
    }

    /// <summary>Abre a tela (ou traz para frente se já estiver aberta). Ao fechar, atualiza o painel.</summary>
    void Abrir(string chave, Func<Form> criar)
    {
        if (abertos.TryGetValue(chave, out var f) && !f.IsDisposed)
        {
            if (f.WindowState == FormWindowState.Minimized)
                f.WindowState = FormWindowState.Normal;
            f.Activate();
            return;
        }
        f = criar();
        f.FormClosed += (_, _) =>
        {
            abertos.Remove(chave);
            AtualizarCabecalho();
            painel.Atualizar();
        };
        abertos[chave] = f;
        f.Show(this);
    }
}
