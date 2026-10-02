namespace ErpLoja.Forms;

/// <summary>
/// Janela principal: menu lateral em acordeão (montado de Navegacao.Telas, fonte única), busca de telas (Ctrl+K)
/// e uma tela inicial limpa (logo, saudação e busca). Os indicadores ficam em Gestão > Painel gerencial.
/// </summary>
public class MainForm : Form
{
    static readonly Color FundoMenu = Color.FromArgb(17, 24, 39);
    static readonly Color FundoItem = Color.FromArgb(31, 41, 55);

    readonly Dictionary<string, Form> abertos = new();
    readonly Dictionary<Keys, Tela> atalhos = new();
    readonly Label titulo = new() { AutoSize = true, Font = Ui.FonteTitulo, Margin = new Padding(16, 12, 0, 0) };
    readonly FlowLayoutPanel menu = new()
    {
        Dock = DockStyle.Left,
        Width = 244,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = FundoMenu,
        Padding = new Padding(10, 12, 6, 12),
    };

    public MainForm()
    {
        Ui.Estilizar(this, "ERP Loja", 1300, 820, nameof(MainForm));
        WindowState = FormWindowState.Maximized;
        Navegacao.Abridor = Abrir;

        var marca = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(4, 0, 0, 12) };
        marca.Controls.Add(new PictureBox { Image = Ui.Logo(38), Size = new Size(38, 38), Margin = new Padding(0, 0, 10, 0) });
        marca.Controls.Add(new Label { Text = "ERP Loja", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14F), AutoSize = true, Margin = new Padding(0, 6, 0, 0) });
        menu.Controls.Add(marca);

        var localizar = new Button
        {
            Text = "  ⌕  Buscar tela...          Ctrl+K",
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(203, 213, 225),
            BackColor = FundoItem,
            Size = new Size(212, 34),
            Margin = new Padding(0, 0, 0, 10),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        localizar.FlatAppearance.BorderColor = Color.FromArgb(55, 65, 81);
        localizar.Click += (_, _) => LocalizarTelaForm.Executar(this);
        Animacao.Hover(localizar, FundoItem, Color.FromArgb(55, 65, 81));
        menu.Controls.Add(localizar);

        var telas = Navegacao.Telas().Where(t => Sessao.Admin || !t.SoAdmin).ToList();
        foreach (var t in telas.Where(t => t.Atalho != Keys.None))
            atalhos[t.Atalho] = t;
        foreach (var modulo in Navegacao.Modulos)
        {
            var doModulo = telas.Where(t => t.Modulo == modulo.Nome && t.Chave != "LocalizarTelaForm").ToList();
            if (doModulo.Count > 0)
                Secao(modulo, doModulo, aberta: modulo.Nome == "Vendas");
        }
        var sair = ItemMenu("Sair", Navegacao.Modulos[^1].Cor, Keys.None);
        sair.Click += (_, _) => Close();
        menu.Controls.Add(sair);

        var cabecalho = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, BackColor = Color.White, WrapContents = false, Padding = new Padding(0, 0, 52, 0) };
        cabecalho.Controls.Add(titulo);

        var status = new StatusStrip();
        status.Items.Add($"Usuário: {Sessao.Nome}{(Sessao.Admin ? " (administrador)" : "")}");
        status.Items.Add("   Banco: " + Db.Caminho);
        status.Items.Add($"   Versão {Application.ProductVersion.Split('+')[0]} · banco v{Migracoes.VersaoDoSistema}");

        Controls.Add(Inicio());
        Controls.Add(cabecalho);
        Controls.Add(menu);
        Controls.Add(status);

        Load += (_, _) => AtualizarCabecalho();
    }

    /// <summary>Área central: só a identidade da loja e o atalho para encontrar qualquer tela.</summary>
    Control Inicio()
    {
        var area = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 3 };
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        area.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var centro = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Anchor = AnchorStyles.None, BackColor = Color.White };
        var logo = new PictureBox { Image = Ui.Logo(140), Size = new Size(140, 140), Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, 18) };
        var saudacao = new Label
        {
            Text = (DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite") + ", " + Sessao.Nome.Split(' ')[0],
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F),
            ForeColor = Color.FromArgb(17, 24, 39),
            Anchor = AnchorStyles.None,
        };
        var dica = new Label
        {
            Text = "Escolha uma área no menu ao lado ou procure a tela pelo nome.",
            AutoSize = true,
            ForeColor = Color.FromArgb(107, 114, 128),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 6, 0, 18),
        };
        var buscar = Ui.Botao("⌕   O que você quer fazer?        Ctrl+K", (_, _) => LocalizarTelaForm.Executar(this));
        buscar.MinimumSize = new Size(420, 46);
        buscar.Font = new Font("Segoe UI", 12F);
        buscar.ForeColor = Color.FromArgb(107, 114, 128);
        buscar.Anchor = AnchorStyles.None;
        centro.Controls.AddRange([logo, saudacao, dica, buscar]);
        area.Controls.Add(centro, 0, 1);
        return area;
    }

    Button ItemMenu(string texto, Color cor, Keys atalho)
    {
        var b = new Button
        {
            Text = "      " + texto + (atalho == Keys.None ? "" : $"   {Ui.NomeAtalho(atalho)}"),
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(229, 231, 235),
            BackColor = FundoMenu,
            Size = new Size(212, 32),
            Margin = new Padding(0, 1, 0, 1),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = FundoMenu;
        b.FlatAppearance.MouseDownBackColor = FundoItem;
        Animacao.Hover(b, FundoMenu, FundoItem);
        // marcador colorido do módulo, que cresce ao passar o mouse
        var largura = 3f;
        b.Paint += (_, e) =>
        {
            using var pincel = new SolidBrush(cor);
            e.Graphics.FillRectangle(pincel, 10, 9, largura, b.Height - 18);
        };
        b.MouseEnter += (_, _) => Animacao.Executar(b, 150, p => { largura = 3 + 3 * (float)p; b.Invalidate(); });
        b.MouseLeave += (_, _) => Animacao.Executar(b, 150, p => { largura = 6 - 3 * (float)p; b.Invalidate(); });
        return b;
    }

    /// <summary>Seção do acordeão: o cabeçalho abre/fecha os itens com animação de altura.</summary>
    void Secao(Modulo modulo, List<Tela> telas, bool aberta)
    {
        var cabecalho = new Button
        {
            Text = $"   {modulo.Nome.ToUpper()}",
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(156, 163, 175),
            BackColor = FundoMenu,
            Font = new Font("Segoe UI Semibold", 8.75F),
            Size = new Size(212, 28),
            Margin = new Padding(0, 6, 0, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        cabecalho.FlatAppearance.BorderSize = 0;
        cabecalho.FlatAppearance.MouseOverBackColor = FundoItem;
        var itens = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = false,
            Width = 212,
            Margin = new Padding(0),
            BackColor = FundoMenu,
        };
        foreach (var t in telas)
        {
            var b = ItemMenu(t.Titulo, modulo.Cor, t.Atalho);
            b.Click += (_, _) => Abrir(t);
            itens.Controls.Add(b);
        }
        var alturaCheia = telas.Count * 34;
        itens.Height = aberta ? alturaCheia : 0;
        var expandida = aberta;
        cabecalho.Paint += (_, e) =>
        {
            using var ponto = new SolidBrush(modulo.Cor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(ponto, 2, 10, 8, 8);
            TextRenderer.DrawText(e.Graphics, expandida ? "▾" : "▸", cabecalho.Font, new Point(cabecalho.Width - 22, 6), Color.FromArgb(156, 163, 175));
        };
        cabecalho.Click += (_, _) =>
        {
            expandida = !expandida;
            cabecalho.Invalidate();
            var de = itens.Height;
            var para = expandida ? alturaCheia : 0;
            Animacao.Executar(itens, 180, p => itens.Height = (int)(de + (para - de) * p));
        };
        menu.Controls.Add(cabecalho);
        menu.Controls.Add(itens);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (atalhos.TryGetValue(keyData, out var tela))
        {
            Abrir(tela);
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
    void Abrir(Tela tela)
    {
        if (tela.Acao is { } acao)
        {
            acao(this);
            return;
        }
        if (abertos.TryGetValue(tela.Chave, out var f) && !f.IsDisposed)
        {
            if (f.WindowState == FormWindowState.Minimized)
                f.WindowState = FormWindowState.Normal;
            f.Activate();
            return;
        }
        f = tela.Criar!();
        f.FormClosed += (_, _) =>
        {
            abertos.Remove(tela.Chave);
            AtualizarCabecalho();
        };
        abertos[tela.Chave] = f;
        f.Show(this);
    }
}
