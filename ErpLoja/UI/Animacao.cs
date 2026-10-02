namespace ErpLoja.UI;

/// <summary>Animações leves (temporizador de 15 ms, curva de desaceleração) usadas para dar retorno visual ao usuário.</summary>
public static class Animacao
{
    /// <summary>Permite desligar as animações (ex.: testes automatizados ou máquinas lentas).</summary>
    public static bool Ativa { get; set; } = true;

    static double Suavizar(double p) => 1 - Math.Pow(1 - p, 3);

    /// <summary>Executa passo(0..1) durante ms milissegundos e chama fim ao terminar.</summary>
    public static void Executar(Control dono, int ms, Action<double> passo, Action? fim = null)
    {
        if (!Ativa || dono.IsDisposed)
        {
            passo(1);
            fim?.Invoke();
            return;
        }
        var inicio = Environment.TickCount64;
        var t = new System.Windows.Forms.Timer { Interval = 15 };
        t.Tick += (_, _) =>
        {
            if (dono.IsDisposed)
            {
                t.Stop();
                t.Dispose();
                return;
            }
            var p = Math.Min(1, (Environment.TickCount64 - inicio) / (double)ms);
            passo(Suavizar(p));
            if (p >= 1)
            {
                t.Stop();
                t.Dispose();
                fim?.Invoke();
            }
        };
        t.Start();
    }

    public static Color Misturar(Color a, Color b, double p) => Color.FromArgb(
        (int)(a.A + (b.A - a.A) * p), (int)(a.R + (b.R - a.R) * p), (int)(a.G + (b.G - a.G) * p), (int)(a.B + (b.B - a.B) * p));

    public static Color Clarear(Color c, double p) => Misturar(c, Color.White, p);
    public static Color Escurecer(Color c, double p) => Misturar(c, Color.Black, p);

    /// <summary>Janela surge suavemente.</summary>
    public static void Surgir(Form f)
    {
        // só janelas soltas: telas embutidas na janela principal (TopLevel = false) não usam transparência
        f.Load += (_, _) =>
        {
            if (Ativa && f.TopLevel)
                f.Opacity = 0;
        };
        f.Shown += (_, _) =>
        {
            if (f.TopLevel)
                Executar(f, 180, p => f.Opacity = p);
        };
    }

    /// <summary>Cor de fundo transita ao passar o mouse.</summary>
    public static void Hover(Control c, Color normal, Color foco)
    {
        double atual = 0;
        void Ir(double alvo)
        {
            var de = atual;
            Executar(c, 120, p =>
            {
                atual = de + (alvo - de) * p;
                c.BackColor = Misturar(Tema.Fundo(normal), Tema.Fundo(foco), atual); // cores do tema atual (claro/escuro)
            });
        }
        c.MouseEnter += (_, _) => Ir(1);
        c.MouseLeave += (_, _) => Ir(0);
    }

    /// <summary>Destaque rápido: a cor de fundo pisca na cor indicada e volta.</summary>
    public static void Pulsar(Control c, Color destaque, int ms = 600)
    {
        var original = c.BackColor;
        Executar(c, ms, p => c.BackColor = Misturar(destaque, original, p), () => c.BackColor = original);
    }

    /// <summary>Número que "conta" até o valor final (indicadores).</summary>
    public static void Contar(Label l, decimal de, decimal ate, Func<decimal, string> formatar, int ms = 500) =>
        Executar(l, ms, p => l.Text = formatar(de + (ate - de) * (decimal)p), () => l.Text = formatar(ate));
}

public enum TipoAviso { Sucesso, Atencao, Erro, Info }

/// <summary>Aviso discreto que desliza no canto da tela e some sozinho (não interrompe o trabalho).</summary>
public sealed class Toast : Form
{
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 | 0x00000080; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
            return cp;
        }
    }

    Toast(string texto, TipoAviso tipo)
    {
        var cor = tipo switch
        {
            TipoAviso.Sucesso => Color.FromArgb(22, 163, 74),
            TipoAviso.Atencao => Color.FromArgb(217, 119, 6),
            TipoAviso.Erro => Color.FromArgb(220, 38, 38),
            _ => Ui.Primaria,
        };
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        Size = new Size(380, 66);
        Font = Ui.Fonte;
        Controls.Add(new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 0, 10, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        });
        Controls.Add(new Label
        {
            Dock = DockStyle.Left,
            Width = 46,
            BackColor = cor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 16F),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = tipo switch { TipoAviso.Sucesso => "✓", TipoAviso.Erro => "✕", TipoAviso.Atencao => "!", _ => "i" },
        });
        Paint += (_, e) =>
        {
            using var borda = new Pen(Color.FromArgb(209, 213, 219));
            e.Graphics.DrawRectangle(borda, 0, 0, Width - 1, Height - 1);
        };
    }

    static readonly List<Toast> abertos = new();

    public static void Mostrar(string texto, TipoAviso tipo = TipoAviso.Sucesso)
    {
        var t = new Toast(texto, tipo);
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        var y = area.Bottom - 20 - (abertos.Count + 1) * (t.Height + 10);
        var x = area.Right - t.Width - 20;
        t.Location = new Point(x, y + 20);
        t.Opacity = 0;
        abertos.Add(t);
        t.FormClosed += (_, _) => abertos.Remove(t);
        t.Show();
        Animacao.Executar(t, 220, p =>
        {
            t.Opacity = p;
            t.Top = y + (int)(20 * (1 - p));
        });
        var espera = new System.Windows.Forms.Timer { Interval = tipo == TipoAviso.Erro ? 5000 : 3000 };
        espera.Tick += (_, _) =>
        {
            espera.Stop();
            espera.Dispose();
            Animacao.Executar(t, 300, p => t.Opacity = 1 - p, t.Close);
        };
        espera.Start();
    }
}
