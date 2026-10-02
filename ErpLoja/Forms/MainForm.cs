namespace ErpLoja.Forms;

/// <summary>
/// Janela principal: menu lateral em acordeão (montado de Navegacao.Telas, fonte única), busca de telas (Ctrl+K)
/// e o palco à direita, onde as telas abrem embutidas, cada uma com sua aba no topo. Sem tela aberta, mostra a
/// tela inicial limpa (logo, saudação e busca). Os indicadores ficam em Gestão > Painel gerencial.
/// </summary>
public class MainForm : Form
{
    static readonly Color FundoMenu = Color.FromArgb(17, 24, 39);
    static readonly Color FundoItem = Color.FromArgb(31, 41, 55);

    readonly Dictionary<string, Form> abertos = new();
    readonly List<(Form Tela, Aba Aba)> ordem = new();
    readonly Dictionary<Keys, Tela> atalhos = new();
    readonly Label titulo = new() { AutoSize = true, Dock = DockStyle.Right, Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(107, 114, 128), Padding = new Padding(0, 14, 56, 0) };
    readonly FlowLayoutPanel abas = new() { Dock = DockStyle.Fill, WrapContents = false, BackColor = Color.White, Padding = new Padding(8, 8, 0, 0) };
    readonly Panel palco = new() { Dock = DockStyle.Fill, BackColor = Ui.Fundo };
    readonly Control inicio;
    Form? ativa;
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

    /// <summary>Tela em primeiro plano no palco (null = tela inicial).</summary>
    public Form? TelaAtiva => ativa;

    /// <summary>Telas abertas no palco, na ordem das abas.</summary>
    public IReadOnlyList<Form> TelasAbertas => ordem.Select(o => o.Tela).ToList();

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

        // barra de abas das telas abertas
        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.White };
        cabecalho.Controls.Add(abas);
        cabecalho.Controls.Add(titulo);
        cabecalho.Paint += (_, e) =>
        {
            using var borda = new Pen(Color.FromArgb(229, 231, 235));
            e.Graphics.DrawLine(borda, 0, cabecalho.Height - 1, cabecalho.Width, cabecalho.Height - 1);
        };

        var status = new StatusStrip();
        status.Items.Add($"Usuário: {Sessao.Nome}{(Sessao.Admin ? " (administrador)" : "")}");
        status.Items.Add("   Banco: " + Db.Caminho);
        status.Items.Add($"   Versão {Application.ProductVersion.Split('+')[0]} · banco v{Migracoes.VersaoDoSistema}");

        inicio = Inicio();
        palco.Controls.Add(inicio);
        var direita = new Panel { Dock = DockStyle.Fill };
        direita.Controls.Add(palco);
        direita.Controls.Add(cabecalho);
        Controls.Add(direita);
        Controls.Add(menu);
        Controls.Add(status);

        Load += (_, _) =>
        {
            AtualizarCabecalho();
            AtualizarAjuda();
        };
        FormClosing += (_, e) =>
        {
            // fecha as telas embutidas uma a uma: cada uma pode pedir confirmação (ex.: venda em andamento no PDV)
            foreach (var (f, _) in ordem.ToList())
            {
                f.Close();
                if (!f.IsDisposed)
                {
                    Ativar(f);
                    e.Cancel = true;
                    return;
                }
            }
        };
    }

    /// <summary>Área central sem tela aberta: só a identidade da loja e o atalho para encontrar qualquer tela.</summary>
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
        if (keyData == (Keys.Control | Keys.W) && ativa != null)
        {
            ativa.Close();
            return true;
        }
        if ((keyData == (Keys.Control | Keys.Tab) || keyData == (Keys.Control | Keys.Shift | Keys.Tab)) && ordem.Count > 1)
        {
            var i = ordem.FindIndex(o => o.Tela == ativa);
            var passo = (keyData & Keys.Shift) != 0 ? -1 : 1;
            Ativar(ordem[(i + passo + ordem.Count) % ordem.Count].Tela);
            return true;
        }
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
        Text = "ERP Loja - " + titulo.Text + (ativa is null ? "" : " - " + ativa.Text);
    }

    /// <summary>O "?" da janela principal só aparece na tela inicial: cada tela aberta tem o seu próprio "?".</summary>
    void AtualizarAjuda()
    {
        foreach (var b in Controls.OfType<Button>().Where(b => b.Text == "?"))
            b.Visible = ativa is null;
    }

    /// <summary>Abre a tela no palco (ou traz para frente se já estiver aberta).</summary>
    void Abrir(Tela tela)
    {
        if (tela.Acao is { } acao)
        {
            Log.Registrar(Log.Tela, "Abriu " + tela.Titulo, tela: tela.Titulo);
            acao(this);
            return;
        }
        if (abertos.TryGetValue(tela.Chave, out var f) && !f.IsDisposed)
        {
            Ativar(f);
            return;
        }
        f = tela.Criar!();
        abertos[tela.Chave] = f;
        Embutir(f, tela);
    }

    /// <summary>Transforma a janela em conteúdo do palco, com uma aba no topo.</summary>
    void Embutir(Form f, Tela tela)
    {
        f.TopLevel = false;
        f.FormBorderStyle = FormBorderStyle.None;
        f.WindowState = FormWindowState.Normal;
        f.MinimumSize = Size.Empty;
        f.ShowInTaskbar = false;
        f.Dock = DockStyle.Fill;

        var aba = new Aba(f.Text, Navegacao.ModuloDe(tela.Modulo).Cor);
        aba.Selecionada += () => Ativar(f);
        aba.Fechada += () => f.Close();
        f.TextChanged += (_, _) => aba.Titulo = f.Text;
        f.FormClosed += (_, _) =>
        {
            Log.Registrar(Log.Tela, "Fechou " + f.Text, tela: f.Text);
            abertos.Remove(tela.Chave);
            ordem.RemoveAll(o => o.Tela == f);
            abas.Controls.Remove(aba);
            aba.Dispose();
            palco.Controls.Remove(f);
            if (ativa == f)
            {
                ativa = null;
                if (ordem.Count > 0)
                    Ativar(ordem[^1].Tela);
                else
                    MostrarInicio();
            }
            AtualizarCabecalho();
        };
        ordem.Add((f, aba));
        abas.Controls.Add(aba);
        palco.Controls.Add(f);
        f.Show();
        Log.Registrar(Log.Tela, "Abriu " + f.Text, tela: f.Text);
        Ativar(f);
    }

    void Ativar(Form f)
    {
        ativa = f;
        foreach (var (outra, aba) in ordem)
        {
            aba.Ativa = outra == f;
            if (outra != f)
                outra.Visible = false;
        }
        inicio.Visible = false;
        f.Visible = true;
        f.BringToFront();
        Log.TelaAtual = f.Text;
        AtualizarCabecalho();
        AtualizarAjuda();
        // foco no controle que a tela escolheu (ex.: código do produto no PDV) ou no primeiro da ordem de tabulação
        if (IsHandleCreated)
            BeginInvoke(() => Focar(f));
        else
            Focar(f);
    }

    static void Focar(Form f)
    {
        if (f.IsDisposed)
            return;
        if (f.ActiveControl is { } c)
            c.Focus();
        else
            f.SelectNextControl(f, true, true, true, true);
    }

    void MostrarInicio()
    {
        inicio.Visible = true;
        inicio.BringToFront();
        Log.TelaAtual = "";
        AtualizarCabecalho();
        AtualizarAjuda();
    }

    /// <summary>Aba de uma tela aberta: ponto na cor do módulo, título e botão de fechar; a aba ativa ganha sublinhado animado.</summary>
    sealed class Aba : Panel
    {
        static readonly Color Cinza = Color.FromArgb(107, 114, 128);
        readonly Label texto = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5F), Location = new Point(22, 9), Cursor = Cursors.Hand, BackColor = Color.Transparent, ForeColor = Color.FromArgb(75, 85, 99) };
        readonly Label fechar = new() { Text = "×", AutoSize = false, Size = new Size(22, 22), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11F), Cursor = Cursors.Hand, ForeColor = Cinza, BackColor = Color.Transparent };
        readonly Color cor;
        bool ativa;
        float sublinhado;

        public event Action? Selecionada, Fechada;

        public Aba(string titulo, Color cor)
        {
            this.cor = cor;
            Height = 38;
            Margin = new Padding(0, 0, 4, 0);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = Color.White;
            Controls.Add(texto);
            Controls.Add(fechar);
            Titulo = titulo;
            foreach (var c in new Control[] { this, texto })
            {
                c.Click += (_, _) => Selecionada?.Invoke();
                c.MouseUp += (_, e) =>
                {
                    if (e.Button == MouseButtons.Middle)
                        Fechada?.Invoke();
                };
            }
            fechar.Click += (_, _) => Fechada?.Invoke();
            fechar.MouseEnter += (_, _) => fechar.ForeColor = Ui.Perigo;
            fechar.MouseLeave += (_, _) => fechar.ForeColor = Cinza;
            Animacao.Hover(this, Color.White, Color.FromArgb(243, 244, 246));
            new ToolTip().SetToolTip(fechar, "Fechar (Ctrl+W)");
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Titulo
        {
            get => texto.Text;
            set
            {
                texto.Text = value.Length > 34 ? value[..33] + "…" : value;
                Width = 22 + TextRenderer.MeasureText(texto.Text, Ui.FonteNegrito).Width + 30;
                fechar.Location = new Point(Width - 26, 8);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Ativa
        {
            get => ativa;
            set
            {
                if (ativa == value)
                    return;
                ativa = value;
                texto.Font = new Font(value ? "Segoe UI Semibold" : "Segoe UI", 9.5F);
                texto.ForeColor = value ? Color.FromArgb(17, 24, 39) : Color.FromArgb(75, 85, 99);
                if (value)
                    Animacao.Executar(this, 200, p => { sublinhado = (float)p; Invalidate(); });
                else
                {
                    sublinhado = 0;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pincel = new SolidBrush(cor);
            e.Graphics.FillEllipse(pincel, 8, 15, 8, 8);
            if (sublinhado > 0)
            {
                var largura = Width * sublinhado;
                e.Graphics.FillRectangle(pincel, (Width - largura) / 2, Height - 3, largura, 3);
            }
        }
    }
}
