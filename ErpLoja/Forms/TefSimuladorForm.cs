using System.Drawing.Drawing2D;
using System.Security.Cryptography;

namespace ErpLoja.Forms;

/// <summary>Provedor de TEF simulado: permite treinar o caixa e testar aprovação/negação sem maquininha.</summary>
public class TefSimulador : IProvedorTef
{
    public string Nome => "SIMULADOR";

    public ResultadoTef Pagar(IWin32Window dono, decimal valor, ModalidadeTef modalidade, int parcelas, string documento)
    {
        using var f = new TefSimuladorForm(valor, modalidade, parcelas);
        f.ShowDialog(dono);
        return f.Resultado ?? new ResultadoTef(false, "Operação cancelada pelo operador.");
    }

    public ResultadoTef Cancelar(IWin32Window dono, string nsu, decimal valor, DateTime data) =>
        new(true, $"Estorno simulado aprovado (NSU original {nsu}).", Tef.NovoNsu(), Tef.NovaAutorizacao());
}

/// <summary>Tela do simulador: animação do cartão/QR do PIX, aprovação automática em 5 s, ou Negar/Cancelar.</summary>
public class TefSimuladorForm : Form
{
    static readonly string[] Bandeiras = ["VISA", "MASTERCARD", "ELO", "HIPERCARD", "AMEX"];

    readonly decimal valor;
    readonly ModalidadeTef modalidade;
    readonly int parcelas;
    readonly Label mensagem = new() { Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 14F), ForeColor = ColorTranslator.FromHtml("#0f172a") };
    readonly Label contagem = new() { Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleCenter, ForeColor = ColorTranslator.FromHtml("#64748b") };
    readonly PictureBox desenho = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    readonly System.Windows.Forms.Timer tempo = new() { Interval = 40 };
    readonly bool[,] qr = new bool[25, 25];
    double fase;
    int restante = 125;
    public ResultadoTef? Resultado { get; private set; }

    public TefSimuladorForm(decimal valor, ModalidadeTef modalidade, int parcelas)
    {
        this.valor = valor;
        this.modalidade = modalidade;
        this.parcelas = parcelas;
        Text = "TEF (simulador)";
        Icon = Ui.Icone;
        Font = Ui.Fonte;
        BackColor = Color.White;
        Size = new Size(460, 470);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        KeyPreview = true;
        Ajuda.InstalarEmDialogo(this, "PagamentoForm");

        var valorRotulo = new Label
        {
            Dock = DockStyle.Top,
            Height = 54,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 22F),
            ForeColor = ColorTranslator.FromHtml("#15803d"),
            Text = Ui.Moeda(valor) + (modalidade == ModalidadeTef.Credito && parcelas > 1 ? $"  em {parcelas}x" : ""),
        };
        mensagem.Text = modalidade == ModalidadeTef.Pix ? "Leia o QR Code com o app do banco" : "Insira, aproxime ou passe o cartão";
        var botoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10), BackColor = ColorTranslator.FromHtml("#f8fafc") };
        botoes.Controls.AddRange([
            Ui.Botao("Cancelar (Esc)", (_, _) => Finalizar(false, "Operação cancelada pelo operador.")),
            Ui.Botao("Negar", (_, _) => Finalizar(false, "Transação negada pelo emissor (simulação).")),
            Ui.Botao("Aprovar (Enter)", (_, _) => Finalizar(true, "Transação aprovada."), primario: true),
        ]);
        Controls.Add(desenho);
        Controls.Add(contagem);
        Controls.Add(mensagem);
        Controls.Add(valorRotulo);
        Controls.Add(botoes);

        var bytes = RandomNumberGenerator.GetBytes(25 * 25);
        for (int i = 0; i < 25; i++)
            for (int j = 0; j < 25; j++)
                qr[i, j] = bytes[i * 25 + j] % 2 == 0;
        desenho.Paint += Desenhar;
        tempo.Tick += (_, _) =>
        {
            fase += 0.12;
            restante--;
            contagem.Text = restante > 0 ? $"Aprovação automática em {restante / 25 + 1} s (simulador)" : "";
            desenho.Invalidate();
            if (restante <= 0)
                Finalizar(true, "Transação aprovada.");
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) Finalizar(true, "Transação aprovada.");
            if (e.KeyCode == Keys.Escape) Finalizar(false, "Operação cancelada pelo operador.");
        };
        Shown += (_, _) => tempo.Start();
        FormClosed += (_, _) => tempo.Dispose();
    }

    void Desenhar(object? _, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var centro = new PointF(desenho.Width / 2f, desenho.Height / 2f);
        if (modalidade == ModalidadeTef.Pix)
        {
            const int modulo = 7;
            var tamanho = 25 * modulo;
            float x0 = centro.X - tamanho / 2f, y0 = centro.Y - tamanho / 2f;
            g.FillRectangle(Brushes.White, x0 - 10, y0 - 10, tamanho + 20, tamanho + 20);
            using (var moldura = new Pen(ColorTranslator.FromHtml("#e2e8f0"), 2))
                g.DrawRectangle(moldura, x0 - 10, y0 - 10, tamanho + 20, tamanho + 20);
            for (int i = 0; i < 25; i++)
                for (int j = 0; j < 25; j++)
                {
                    var finder = (i < 7 && j < 7) || (i < 7 && j > 17) || (i > 17 && j < 7);
                    var cheio = finder ? (i % 6 == 0 || j % 6 == 0 || i is 2 or 3 or 4 && j is 2 or 3 or 4 || i is 20 or 21 or 22 || j is 20 or 21 or 22 || i == 18 || j == 18 || i == 24 || j == 24) : qr[i, j];
                    if (cheio)
                        g.FillRectangle(Brushes.Black, x0 + j * modulo, y0 + i * modulo, modulo, modulo);
                }
            return;
        }
        // cartão "pulsando" com ondas de aproximação
        for (int k = 0; k < 3; k++)
        {
            var r = (float)(40 + ((fase * 25 + k * 30) % 90));
            var alfa = (int)Math.Max(0, 160 - r * 1.6);
            using var onda = new Pen(Color.FromArgb(alfa, 22, 163, 74), 3);
            g.DrawArc(onda, centro.X - r + 40, centro.Y - r, r * 2, r * 2, -50, 100);
        }
        var desloc = (float)(Math.Sin(fase) * 8);
        var cartao = new RectangleF(centro.X - 110 + desloc, centro.Y - 65, 170, 108);
        using var caminho = new GraphicsPath();
        caminho.AddArc(cartao.X, cartao.Y, 18, 18, 180, 90);
        caminho.AddArc(cartao.Right - 18, cartao.Y, 18, 18, 270, 90);
        caminho.AddArc(cartao.Right - 18, cartao.Bottom - 18, 18, 18, 0, 90);
        caminho.AddArc(cartao.X, cartao.Bottom - 18, 18, 18, 90, 90);
        caminho.CloseFigure();
        using (var fundo = new LinearGradientBrush(cartao, ColorTranslator.FromHtml("#3b82f6"), ColorTranslator.FromHtml("#1e3a8a"), 30))
            g.FillPath(fundo, caminho);
        using (var chip = new SolidBrush(ColorTranslator.FromHtml("#fbbf24")))
            g.FillRectangle(chip, cartao.X + 18, cartao.Y + 34, 30, 22);
        TextRenderer.DrawText(g, modalidade == ModalidadeTef.Debito ? "DÉBITO" : "CRÉDITO", Ui.Fonte, new Point((int)cartao.X + 18, (int)cartao.Bottom - 30), Color.White);
    }

    void Finalizar(bool aprovar, string texto)
    {
        if (Resultado != null)
            return;
        tempo.Stop();
        if (!aprovar)
        {
            Resultado = new ResultadoTef(false, texto);
            DialogResult = DialogResult.Cancel;
            return;
        }
        var bandeira = modalidade == ModalidadeTef.Pix ? "PIX" : Bandeiras[RandomNumberGenerator.GetInt32(Bandeiras.Length)];
        var nsu = Tef.NovoNsu();
        var aut = Tef.NovaAutorizacao();
        Resultado = new ResultadoTef(true, texto, nsu, aut, bandeira,
            Tef.Comprovante("VIA DO CLIENTE", bandeira, modalidade, parcelas, valor, nsu, aut),
            Tef.Comprovante("VIA DO ESTABELECIMENTO", bandeira, modalidade, parcelas, valor, nsu, aut));
        mensagem.Text = "✓ " + texto;
        mensagem.ForeColor = ColorTranslator.FromHtml("#4ade80");
        Animacao.Executar(this, 450, _ => { }, () => DialogResult = DialogResult.OK);
    }
}
