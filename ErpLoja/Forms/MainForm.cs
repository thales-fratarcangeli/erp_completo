namespace ErpLoja.Forms;

/// <summary>
/// Janela principal: menu lateral em acordeão (montado de Navegacao.Telas, fonte única), busca de telas (Ctrl+K) e, à
/// direita, uma tela por vez (sem abas). O cabeçalho mostra "Área › Tela" e o botão de fechar. Ao trocar ou fechar a
/// tela com dados digitados e não salvos, o sistema pergunta se quer salvar, descartar ou continuar editando.
/// Sem tela aberta, mostra a tela inicial (logo, saudação, busca e "continuar de onde parou").
/// </summary>
public class MainForm : Form
{
    static readonly Color FundoMenu = Color.FromArgb(17, 24, 39);
    static readonly Color FundoItem = Color.FromArgb(31, 41, 55);

    readonly Dictionary<Keys, Tela> atalhos = new();
    readonly Label loja = new() { AutoSize = true, Dock = DockStyle.Right, Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(107, 114, 128), Padding = new Padding(0, 14, 12, 0) };
    readonly Label caminho = new() { AutoSize = true, Dock = DockStyle.Left, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Color.FromArgb(17, 24, 39), Padding = new Padding(30, 12, 0, 0) };
    readonly Button fechar = new()
    {
        Text = "×",
        Dock = DockStyle.Right,
        Width = 44,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 15F),
        ForeColor = Color.FromArgb(107, 114, 128),
        BackColor = Color.White,
        Cursor = Cursors.Hand,
        Visible = false,
        TabStop = false,
        UseVisualStyleBackColor = false,
    };
    readonly Panel cabecalho = new() { Dock = DockStyle.Top, Height = 46, BackColor = Color.White };
    readonly Panel palco = new() { Dock = DockStyle.Fill, BackColor = Ui.Fundo };
    readonly AssistentePainel assistente = new() { Visible = false };
    readonly Control inicio;
    Form? ativa;
    Tela? telaAtiva;
    // tela inicial: telas usadas por último ("continuar de onde parou")
    readonly Label tituloRecentes = new()
    {
        Text = "Continuar de onde parou",
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10F),
        ForeColor = Color.FromArgb(107, 114, 128),
        Anchor = AnchorStyles.None,
        Margin = new Padding(0, 28, 0, 6),
    };
    readonly FlowLayoutPanel recentes = new() { AutoSize = true, WrapContents = true, MaximumSize = new Size(780, 0), Anchor = AnchorStyles.None, BackColor = Color.White };
    // tela inicial: o que precisa de atenção hoje (OS atrasadas, contas vencidas...), cada um abre a tela já filtrada
    readonly FlowLayoutPanel alertas = new() { AutoSize = true, WrapContents = true, MaximumSize = new Size(780, 0), Anchor = AnchorStyles.None, BackColor = Color.White, Margin = new Padding(0, 22, 0, 0) };
    // menu lateral: item de cada tela, para destacar a que está aberta
    readonly Dictionary<string, Button> itensMenu = new();
    string? chaveAtiva;
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

    /// <summary>Tela aberta à direita (null = tela inicial).</summary>
    public Form? TelaAtiva => ativa;

    /// <summary>Telas abertas: no máximo uma (a janela principal mostra uma tela por vez).</summary>
    public IReadOnlyList<Form> TelasAbertas => ativa is null ? [] : [ativa];

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

        // cabeçalho: "Área › Tela" com o ponto na cor da área, nome da loja e o × para fechar a tela
        fechar.FlatAppearance.BorderSize = 0;
        fechar.Click += (_, _) => FecharTela();
        fechar.MouseEnter += (_, _) => fechar.ForeColor = Ui.Perigo;
        fechar.MouseLeave += (_, _) => fechar.ForeColor = Tema.Texto(Color.FromArgb(107, 114, 128));
        new ToolTip().SetToolTip(fechar, "Fechar esta tela (Ctrl+W)");
        var perguntar = new Button
        {
            Text = "✦ Assistente",
            Dock = DockStyle.Right,
            Width = 118,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Ui.Primaria,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false,
        };
        perguntar.FlatAppearance.BorderSize = 0;
        perguntar.Click += (_, _) => AlternarAssistente();
        new ToolTip().SetToolTip(perguntar, "Tire dúvidas sobre como usar o sistema (Ctrl+I)");
        assistente.TelaAtual = () => ativa?.Text;
        cabecalho.Controls.Add(caminho);
        cabecalho.Controls.Add(loja);
        cabecalho.Controls.Add(perguntar);
        cabecalho.Controls.Add(fechar);
        cabecalho.Paint += (_, e) =>
        {
            using var borda = new Pen(Tema.Fundo(Color.FromArgb(229, 231, 235)));
            e.Graphics.DrawLine(borda, 0, cabecalho.Height - 1, cabecalho.Width, cabecalho.Height - 1);
            if (telaAtiva is null)
                return;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var ponto = new SolidBrush(Navegacao.ModuloDe(telaAtiva.Modulo).Cor);
            e.Graphics.FillEllipse(ponto, 14, 19, 9, 9);
        };

        var status = new StatusStrip();
        status.Items.Add($"Usuário: {Sessao.Nome}{(Sessao.Admin ? " (administrador)" : "")}");
        status.Items.Add("   Banco: " + Db.Caminho);
        status.Items.Add($"   Versão {Application.ProductVersion.Split('+')[0]} · banco v{Migracoes.VersaoDoSistema}");
        var alternarTema = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight, IsLink = true, LinkBehavior = LinkBehavior.HoverUnderline };
        void TextoTema() => alternarTema.Text = Tema.Escuro ? "☀ Modo claro" : "☾ Modo escuro";
        TextoTema();
        alternarTema.Click += (_, _) => Preferencias.SalvarTema(!Tema.Escuro);
        Tema.Mudou += TextoTema;
        Disposed += (_, _) => Tema.Mudou -= TextoTema;
        status.Items.Add(alternarTema);

        inicio = Inicio();
        palco.Controls.Add(inicio);
        var direita = new Panel { Dock = DockStyle.Fill };
        direita.Controls.Add(palco);
        direita.Controls.Add(assistente);
        direita.Controls.Add(cabecalho);
        Controls.Add(direita);
        Controls.Add(menu);
        Controls.Add(status);

        Load += (_, _) =>
        {
            AtualizarCabecalho();
            AtualizarAjuda();
            AtualizarRecentes();
            AtualizarAlertas();
        };
        ConfiguracoesForm.ParametrosSalvos += AtualizarCabecalho;
        Tema.Mudou += Redesenhar;
        Disposed += (_, _) =>
        {
            ConfiguracoesForm.ParametrosSalvos -= AtualizarCabecalho;
            Tema.Mudou -= Redesenhar;
        };
        FormClosing += (_, e) =>
        {
            // a tela aberta pode ter dados não salvos (pergunta) ou recusar o fechamento (ex.: venda em andamento no PDV)
            if (ativa != null && !FecharTela("fechar o sistema"))
                e.Cancel = true;
        };
    }

    void Redesenhar()
    {
        cabecalho.Invalidate();
        menu.Invalidate(true);
    }

    /// <summary>Área central sem tela aberta: identidade da loja, busca e as telas usadas por último.</summary>
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
        centro.Controls.AddRange([logo, saudacao, dica, buscar, alertas, tituloRecentes, recentes]);
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
            var ativo = chaveAtiva != null && b.Tag as string == chaveAtiva;
            e.Graphics.FillRectangle(pincel, 10, ativo ? 5 : 9, ativo ? 6 : largura, b.Height - (ativo ? 10 : 18));
        };
        b.MouseEnter += (_, _) => Animacao.Executar(b, 150, p => { largura = 3 + 3 * (float)p; b.Invalidate(); });
        b.MouseLeave += (_, _) => Animacao.Executar(b, 150, p => { largura = 6 - 3 * (float)p; b.Invalidate(); });
        return b;
    }

    /// <summary>Seção do acordeão: o cabeçalho abre/fecha os itens com animação de altura.</summary>
    void Secao(Modulo modulo, List<Tela> telas, bool aberta)
    {
        var titulo = new Button
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
        titulo.FlatAppearance.BorderSize = 0;
        titulo.FlatAppearance.MouseOverBackColor = FundoItem;
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
            b.Tag = t.Chave;
            itensMenu[t.Chave] = b;
            b.Click += (_, _) => Abrir(t);
            itens.Controls.Add(b);
        }
        var alturaCheia = telas.Count * 34;
        itens.Height = aberta ? alturaCheia : 0;
        var expandida = aberta;
        titulo.Paint += (_, e) =>
        {
            using var ponto = new SolidBrush(modulo.Cor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(ponto, 2, 10, 8, 8);
            TextRenderer.DrawText(e.Graphics, expandida ? "▾" : "▸", titulo.Font, new Point(titulo.Width - 22, 6), Color.FromArgb(156, 163, 175));
        };
        titulo.Click += (_, _) =>
        {
            expandida = !expandida;
            titulo.Invalidate();
            var de = itens.Height;
            var para = expandida ? alturaCheia : 0;
            Animacao.Executar(itens, 180, p => itens.Height = (int)(de + (para - de) * p));
        };
        menu.Controls.Add(titulo);
        menu.Controls.Add(itens);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.I))
        {
            AlternarAssistente();
            return true;
        }
        if (keyData == (Keys.Control | Keys.W) && ativa != null)
        {
            FecharTela();
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
        if (IsDisposed)
            return;
        loja.Text = Db.Config("nome_loja");
        caminho.Text = telaAtiva is null ? "Início" : $"{telaAtiva.Modulo}  ›  {ativa?.Text ?? telaAtiva.Titulo}";
        fechar.Visible = ativa != null;
        Text = "ERP Loja - " + loja.Text + (ativa is null ? "" : " - " + ativa.Text);
        cabecalho.Invalidate();
    }

    /// <summary>O "?" da janela principal só aparece na tela inicial: cada tela tem o seu próprio "?".</summary>
    void AtualizarAjuda()
    {
        foreach (var b in Controls.OfType<Button>().Where(b => b.Text == "?"))
            b.Visible = ativa is null;
    }

    /// <summary>Abre a tela à direita, no lugar da atual (perguntando antes se a atual tem dados não salvos).</summary>
    void Abrir(Tela tela)
    {
        if (tela.SoAdmin && !Sessao.Admin)
        {
            Log.Registrar(Log.Acesso, "Acesso negado a " + tela.Titulo, "tela exclusiva de administrador", tela: tela.Titulo);
            Ui.Erro($"\"{tela.Titulo}\" é exclusiva de administradores. Peça a um administrador para fazer isso.");
            return;
        }
        if (tela.Acao is { } acao)
        {
            Log.Registrar(Log.Tela, "Abriu " + tela.Titulo, tela.Chave, tela: tela.Titulo);
            acao(this);
            return;
        }
        if (ativa != null && telaAtiva?.Chave == tela.Chave)
        {
            Focar(ativa);
            return;
        }
        if (ativa != null && !FecharTela($"abrir \"{tela.Titulo}\""))
            return;
        Embutir(tela.Criar!(), tela);
    }

    /// <summary>Fecha a tela aberta e volta ao início. Devolve false se a pessoa preferiu continuar nela.</summary>
    bool FecharTela(string acao = "fechar esta tela")
    {
        var f = ativa;
        if (f is null)
            return true;
        if (!Alteracoes.PodeSair(f, acao))
            return false;
        f.Close(); // a própria tela ainda pode recusar (ex.: PDV com venda em andamento)
        return f.IsDisposed;
    }

    /// <summary>Transforma a janela em conteúdo do palco, no modelo padrão (listagem ao meio, opções à direita).</summary>
    void Embutir(Form f, Tela tela)
    {
        f.TopLevel = false;
        f.FormBorderStyle = FormBorderStyle.None;
        f.WindowState = FormWindowState.Normal;
        f.MinimumSize = Size.Empty;
        f.ShowInTaskbar = false;
        f.Dock = DockStyle.Fill;
        f.Load += (_, _) => ModeloDeTela.Padronizar(f);
        f.TextChanged += (_, _) => AtualizarCabecalho();
        f.FormClosed += (_, _) =>
        {
            Log.Registrar(Log.Tela, "Fechou " + f.Text, tela: f.Text);
            palco.Controls.Remove(f);
            if (ativa == f)
            {
                ativa = null;
                telaAtiva = null;
                MostrarInicio();
            }
        };
        ativa = f;
        telaAtiva = tela;
        palco.Controls.Add(f);
        inicio.Visible = false;
        f.Show();
        f.BringToFront();
        Log.Registrar(Log.Tela, "Abriu " + f.Text, tela.Chave, tela: f.Text);
        Log.TelaAtual = f.Text;
        MarcarMenu(tela.Chave);
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
        MarcarMenu(null);
        AtualizarRecentes();
        AtualizarAlertas();
        AtualizarCabecalho();
        AtualizarAjuda();
    }

    /// <summary>O item do menu da tela aberta fica em negrito, branco e com a marca de cor cheia.</summary>
    void MarcarMenu(string? chave)
    {
        chaveAtiva = chave;
        foreach (var (k, b) in itensMenu)
        {
            var ativo = k == chave;
            b.Font = ativo ? Ui.FonteNegrito : Ui.Fonte;
            b.ForeColor = ativo ? Color.White : Color.FromArgb(229, 231, 235);
            b.Invalidate();
        }
    }

    /// <summary>Botões dos alertas mostrados na tela inicial (para testes).</summary>
    public IReadOnlyList<Button> BotoesAlerta => alertas.Controls.OfType<Button>().ToList();

    /// <summary>Alertas da tela inicial: cada um abre a tela que resolve, já filtrada.</summary>
    void AtualizarAlertas()
    {
        alertas.SuspendLayout();
        foreach (Control c in alertas.Controls.Cast<Control>().ToList())
            c.Dispose();
        List<Alerta> lista;
        try
        {
            lista = Alertas.Listar();
        }
        catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or InvalidOperationException)
        {
            lista = []; // alerta nunca impede de abrir o sistema
        }
        foreach (var a in lista)
        {
            var tela = Navegacao.Telas().FirstOrDefault(t => t.Chave == a.Tela);
            if (tela is null)
                continue;
            var cor = a.Grave ? Ui.Perigo : Ui.Alerta;
            var b = Ui.Botao("     " + a.Texto + "  →", (_, _) => AbrirAlerta(tela, a));
            b.Margin = new Padding(4);
            b.MinimumSize = new Size(250, 38);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Tag = a;
            b.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var ponto = new SolidBrush(Tema.Texto(cor));
                e.Graphics.FillEllipse(ponto, 12, b.Height / 2 - 5, 10, 10);
            };
            new ToolTip().SetToolTip(b, $"Abrir {tela.Titulo} já filtrada");
            alertas.Controls.Add(b);
        }
        alertas.Visible = alertas.Controls.Count > 0;
        alertas.ResumeLayout();
    }

    void AbrirAlerta(Tela tela, Alerta a)
    {
        Log.Registrar(Log.Tela, "Abriu pelo alerta da tela inicial", a.Texto, tela: tela.Titulo);
        Abrir(tela);
        if (telaAtiva?.Chave == tela.Chave && ativa is IFiltroInicial filtravel)
            filtravel.AplicarFiltro(a.Filtro);
    }

    /// <summary>Atalhos para as telas usadas por último (vêm do log de atividades), com a cor da área de cada uma.</summary>
    void AtualizarRecentes()
    {
        recentes.SuspendLayout();
        foreach (Control c in recentes.Controls.Cast<Control>().ToList())
            c.Dispose();
        foreach (var tela in Navegacao.Recentes())
        {
            var cor = Navegacao.ModuloDe(tela.Modulo).Cor;
            var b = Ui.Botao("     " + tela.Titulo, (_, _) => Abrir(tela));
            b.Margin = new Padding(4);
            b.MinimumSize = new Size(170, 38);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var ponto = new SolidBrush(cor);
                e.Graphics.FillEllipse(ponto, 12, b.Height / 2 - 4, 8, 8);
            };
            new ToolTip().SetToolTip(b, $"{tela.Modulo} · abrir {tela.Titulo}" + (tela.Atalho != Keys.None ? $" ({Ui.NomeAtalho(tela.Atalho)})" : ""));
            recentes.Controls.Add(b);
        }
        tituloRecentes.Visible = recentes.Visible = recentes.Controls.Count > 0;
        recentes.ResumeLayout();
    }

    /// <summary>Mostra ou esconde o painel do assistente de dúvidas ao lado da tela atual.</summary>
    public void AlternarAssistente()
    {
        assistente.Visible = !assistente.Visible;
        Log.Registrar(Log.Tela, assistente.Visible ? "Abriu o assistente" : "Fechou o assistente", tela: ativa?.Text ?? "Início");
    }

    /// <summary>Painel do assistente (para testes).</summary>
    public AssistentePainel PainelAssistente => assistente;

    /// <summary>Telas mostradas em "Continuar de onde parou" (para testes).</summary>
    public IReadOnlyList<string> AtalhosRecentes => recentes.Controls.OfType<Button>().Select(b => b.Text.Trim()).ToList();

    /// <summary>Chave da tela destacada no menu lateral (para testes).</summary>
    public string? TelaDestacadaNoMenu => chaveAtiva;

    /// <summary>Texto do cabeçalho "Área › Tela" (para testes).</summary>
    public string CaminhoNoCabecalho => caminho.Text;
}
