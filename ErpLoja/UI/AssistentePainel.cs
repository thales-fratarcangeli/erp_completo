namespace ErpLoja.UI;

/// <summary>
/// Painel lateral do assistente de dúvidas (abre ao lado da tela atual, sem substituí-la). Conversa curta em português,
/// com base apenas na ajuda do sistema (ver Data/Assistente).
/// </summary>
public sealed class AssistentePainel : Panel
{
    readonly RichTextBox conversa = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        Font = new Font("Segoe UI", 10F),
        DetectUrls = false,
    };
    readonly TextBox entrada = Validacao.Caixa(Formatos.TextoLongo(1000), "Pergunta");
    readonly Button enviar;
    readonly List<(bool DoUsuario, string Texto)> historico = new();
    CancellationTokenSource? emAndamento;

    /// <summary>Tela em que a pessoa está (vai junto da pergunta para contextualizar).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?>? TelaAtual { get; set; }

    public AssistentePainel()
    {
        Dock = DockStyle.Right;
        Width = 380;
        BackColor = Color.White;
        Padding = new Padding(12, 8, 12, 10);
        Tag = Alteracoes.MarcaFiltros; // conversar com o assistente não é alteração de dados da tela

        var titulo = new Panel { Dock = DockStyle.Top, Height = 34 };
        titulo.Controls.Add(new Label { Text = "✦  Assistente", Font = new Font("Segoe UI Semibold", 12F), AutoSize = true, Location = new Point(0, 6) });
        var fechar = new Button { Text = "×", Dock = DockStyle.Right, Width = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 13F), Cursor = Cursors.Hand };
        fechar.FlatAppearance.BorderSize = 0;
        fechar.Click += (_, _) => Visible = false;
        new ToolTip().SetToolTip(fechar, "Fechar o assistente (Ctrl+I)");
        titulo.Controls.Add(fechar);

        var aviso = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            ForeColor = Color.FromArgb(107, 114, 128),
            Font = new Font("Segoe UI", 8.5F),
            Text = "Tire dúvidas sobre como usar o sistema. Respostas com base na ajuda das telas. Não digite senhas nem dados de clientes.",
        };

        entrada.Height = 64;
        entrada.Dock = DockStyle.Fill;
        entrada.PlaceholderText = "Ex.: como emito um boleto? onde vejo o estoque baixo?";
        entrada.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                _ = Enviar();
            }
        };
        enviar = Ui.Botao("Perguntar", async (_, _) => await Enviar(), primario: true);
        enviar.Dock = DockStyle.Right;
        var limpar = Ui.Botao("Nova conversa", (_, _) => Limpar());
        limpar.Dock = DockStyle.Left;
        var acoes = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(0, 4, 0, 0) };
        acoes.Controls.Add(enviar);
        acoes.Controls.Add(limpar);
        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 112, Padding = new Padding(0, 6, 0, 0) };
        rodape.Controls.Add(entrada);
        rodape.Controls.Add(acoes);

        Controls.Add(conversa);
        Controls.Add(rodape);
        Controls.Add(aviso);
        Controls.Add(titulo);
        Paint += (_, e) =>
        {
            using var borda = new Pen(Tema.Fundo(Color.FromArgb(229, 231, 235)));
            e.Graphics.DrawLine(borda, 0, 0, 0, Height);
        };
        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                if (historico.Count == 0)
                    Apresentar();
                entrada.Focus();
            }
        };
    }

    void Apresentar()
    {
        conversa.Clear();
        Escrever("Assistente", Assistente.Ativo
            ? "Olá! Pergunte como fazer algo no sistema — por exemplo, \"como cancelo uma venda?\" ou \"onde configuro a impressora?\"."
            : "O assistente está desligado. Um administrador pode ligá-lo em Configurações > Assistente (IA).", Ui.Primaria);
    }

    void Limpar()
    {
        emAndamento?.Cancel();
        historico.Clear();
        Apresentar();
        entrada.Focus();
    }

    /// <summary>Envia a pergunta digitada (também usado pelos testes).</summary>
    public async Task Enviar()
    {
        var pergunta = entrada.Text.Trim();
        if (pergunta.Length == 0 || emAndamento != null)
            return;
        Escrever("Você", pergunta, Color.FromArgb(17, 24, 39));
        entrada.Clear();
        entrada.Enabled = enviar.Enabled = false;
        enviar.Text = "Pensando...";
        emAndamento = new CancellationTokenSource();
        try
        {
            var resposta = await Assistente.Perguntar(pergunta, historico, TelaAtual?.Invoke(), emAndamento.Token);
            if (IsDisposed)
                return;
            Escrever("Assistente", resposta.Texto, resposta.Ok ? Ui.Primaria : Ui.Perigo);
            if (resposta.Ok)
            {
                historico.Add((true, pergunta));
                historico.Add((false, resposta.Texto));
            }
        }
        finally
        {
            emAndamento?.Dispose();
            emAndamento = null;
            if (!IsDisposed)
            {
                entrada.Enabled = enviar.Enabled = true;
                enviar.Text = "Perguntar";
                entrada.Focus();
            }
        }
    }

    /// <summary>Texto da conversa (para testes).</summary>
    public string TextoDaConversa => conversa.Text;

    /// <summary>Campo da pergunta (para testes).</summary>
    public TextBox CampoPergunta => entrada;

    void Escrever(string quem, string texto, Color cor)
    {
        conversa.SelectionStart = conversa.TextLength;
        conversa.SelectionColor = Tema.Texto(cor);
        conversa.SelectionFont = new Font("Segoe UI Semibold", 9.5F);
        conversa.AppendText((conversa.TextLength > 0 ? "\n\n" : "") + quem + "\n");
        conversa.SelectionColor = Tema.Texto(Color.FromArgb(17, 24, 39));
        conversa.SelectionFont = new Font("Segoe UI", 10F);
        conversa.AppendText(texto);
        conversa.ScrollToCaret();
    }
}
