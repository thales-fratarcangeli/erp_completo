namespace ErpLoja.Forms;

/// <summary>Pagamento do PDV: forma (teclas 1 a 5), valor recebido e troco, parcelas e vencimento.</summary>
public class PagamentoForm : Form
{
    static readonly Color Fundo = ColorTranslator.FromHtml("#0b1220");
    static readonly Color Cartao = ColorTranslator.FromHtml("#1e293b");
    static readonly Color Selecionado = ColorTranslator.FromHtml("#2563eb");

    static readonly (string Forma, ModalidadeTef? Tef)[] Formas =
    [
        ("Dinheiro", null), ("PIX", ModalidadeTef.Pix), ("Cartão de débito", ModalidadeTef.Debito),
        ("Cartão de crédito", ModalidadeTef.Credito), (Operacoes.APrazo, null),
    ];

    readonly decimal total;
    readonly bool temCliente;
    readonly List<Button> botoes = new();
    readonly NumericUpDown recebido = Ui.Numero(2, 9_999_999), parcelas = Ui.Numero(0, 60, 1);
    readonly DateTimePicker vencimento = Ui.Data();
    readonly Label troco = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 20F), ForeColor = ColorTranslator.FromHtml("#fbbf24") };
    readonly Label aviso = new() { AutoSize = true, ForeColor = ColorTranslator.FromHtml("#94a3b8"), MaximumSize = new Size(560, 0) };
    readonly FlowLayoutPanel detalhes = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Fundo };
    int escolha;

    public string Forma => Formas[escolha].Forma;
    public ModalidadeTef? Modalidade => Formas[escolha].Tef;
    public int Parcelas => Forma is "Cartão de crédito" or Operacoes.APrazo ? (int)parcelas.Value : 1;
    public DateTime Vencimento => vencimento.Value.Date;
    public decimal Recebido => Forma == "Dinheiro" && recebido.Value > 0 ? recebido.Value : total;

    public PagamentoForm(decimal total, bool temCliente)
    {
        this.total = total;
        this.temCliente = temCliente;
        Text = "Pagamento";
        Icon = Ui.Icone;
        Font = Ui.Fonte;
        BackColor = Fundo;
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(22);
        KeyPreview = true;
        Ajuda.InstalarEmDialogo(this, nameof(PagamentoForm));
        Animacao.Surgir(this);

        var raiz = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Fundo };
        raiz.Controls.Add(new Label { Text = "TOTAL A PAGAR", AutoSize = true, ForeColor = ColorTranslator.FromHtml("#94a3b8"), Font = new Font("Segoe UI Semibold", 10F) });
        raiz.Controls.Add(new Label { Text = Ui.Moeda(total), AutoSize = true, ForeColor = ColorTranslator.FromHtml("#4ade80"), Font = new Font("Segoe UI Semibold", 34F), Margin = new Padding(0, 0, 0, 12) });

        var grade = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Fundo, Margin = new Padding(0, 0, 0, 14) };
        for (int i = 0; i < Formas.Length; i++)
        {
            var indice = i;
            var b = new Button
            {
                Text = $"{i + 1}\n{Formas[i].Forma}",
                Size = new Size(118, 74),
                FlatStyle = FlatStyle.Flat,
                BackColor = Cartao,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => Escolher(indice);
            botoes.Add(b);
            grade.Controls.Add(b);
        }
        raiz.Controls.Add(grade);
        raiz.Controls.Add(detalhes);
        raiz.Controls.Add(aviso);

        var confirmar = Ui.Botao("Confirmar (Enter)", (_, _) => Confirmar(), primario: true);
        var voltar = Ui.Botao("Voltar (Esc)", (_, _) => { DialogResult = DialogResult.Cancel; });
        var acoes = new FlowLayoutPanel { AutoSize = true, BackColor = Fundo, Margin = new Padding(0, 16, 0, 0) };
        acoes.Controls.AddRange([confirmar, voltar]);
        raiz.Controls.Add(acoes);
        Controls.Add(raiz);

        parcelas.Maximum = Parametros.MaxParcelas;
        vencimento.Value = DateTime.Today.AddDays(Parametros.PrazoVencimentoDias);
        recebido.Font = new Font("Segoe UI", 16F);
        recebido.Width = 180;
        recebido.ValueChanged += (_, _) => AtualizarTroco();
        recebido.KeyUp += (_, _) => AtualizarTroco();
        Shown += (_, _) => Escolher(0);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var digitando = ActiveControl is NumericUpDown or TextBox || ActiveControl?.Parent is NumericUpDown;
        if (!digitando && keyData is >= Keys.D1 and <= Keys.D5)
        {
            Escolher(keyData - Keys.D1);
            return true;
        }
        if (!digitando && keyData is >= Keys.NumPad1 and <= Keys.NumPad5)
        {
            Escolher(keyData - Keys.NumPad1);
            return true;
        }
        switch (keyData)
        {
            case Keys.Enter: Confirmar(); return true;
            case Keys.Escape: DialogResult = DialogResult.Cancel; return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void Escolher(int i)
    {
        escolha = i;
        for (int k = 0; k < botoes.Count; k++)
        {
            var alvo = k == i ? Selecionado : Cartao;
            var botao = botoes[k];
            var de = botao.BackColor;
            Animacao.Executar(botao, 140, p => botao.BackColor = Animacao.Misturar(de, alvo, p));
        }
        detalhes.Controls.Clear();
        void Campo(string rotulo, Control c)
        {
            var linha = new FlowLayoutPanel { AutoSize = true, BackColor = Fundo, Margin = new Padding(0, 4, 0, 4) };
            linha.Controls.Add(new Label { Text = rotulo, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 12F), Margin = new Padding(0, 6, 10, 0), MinimumSize = new Size(170, 0) });
            linha.Controls.Add(c);
            detalhes.Controls.Add(linha);
        }
        switch (Forma)
        {
            case "Dinheiro":
                recebido.Value = 0;
                Campo("Valor recebido R$:", recebido);
                Campo("Troco:", troco);
                AtualizarTroco();
                aviso.Text = "Informe o valor entregue pelo cliente para calcular o troco (opcional).";
                BeginInvoke(() => { recebido.Focus(); recebido.Select(0, recebido.Text.Length); });
                break;
            case "Cartão de crédito":
                parcelas.Value = 1;
                Campo("Parcelas:", parcelas);
                aviso.Text = Parametros.TefAtivo ? "O TEF será acionado ao confirmar." : "Receba na maquininha e confirme.";
                break;
            case Operacoes.APrazo:
                parcelas.Value = 1;
                Campo("Parcelas:", parcelas);
                Campo("1º vencimento:", vencimento);
                aviso.Text = temCliente ? "As parcelas serão lançadas no contas a receber do cliente." : "Venda a prazo exige cliente: feche e use F2.";
                break;
            default:
                aviso.Text = Parametros.TefAtivo ? "O TEF será acionado ao confirmar." : "Receba e confirme.";
                break;
        }
    }

    void AtualizarTroco()
    {
        var valor = recebido.Value;
        troco.Text = valor > 0 ? Ui.Moeda(Math.Max(0, valor - total)) : "—";
    }

    void Confirmar()
    {
        if (Forma == Operacoes.APrazo && !temCliente)
        {
            Ui.Erro("Para vender a prazo, identifique o cliente (F2 no PDV).");
            return;
        }
        if (Forma == "Dinheiro" && recebido.Value > 0 && recebido.Value < total)
        {
            Ui.Erro("O valor recebido é menor que o total.");
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
