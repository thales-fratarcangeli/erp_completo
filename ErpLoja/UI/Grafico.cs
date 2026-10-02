using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ErpLoja.UI;

public record Serie(string Nome, Color Cor, double[] Valores);

/// <summary>
/// Gráfico de colunas (ou barras horizontais) com uma ou mais séries.
/// Barras finas (até 24px) com ponta arredondada, grade discreta, legenda quando há 2+ séries
/// e dica (tooltip) ao passar o mouse.
/// </summary>
public class Grafico : Control
{
    // paleta categórica validada (ordem fixa) e tintas do gráfico
    public static readonly Color Cor1 = ColorTranslator.FromHtml("#2a78d6");
    public static readonly Color Cor2 = ColorTranslator.FromHtml("#eb6834");
    public static readonly Color Cor3 = ColorTranslator.FromHtml("#1baf7a");
    public static readonly Color Superficie = ColorTranslator.FromHtml("#fcfcfb");
    static readonly Color TintaPrimaria = ColorTranslator.FromHtml("#0b0b0b");
    static readonly Color TintaSecundaria = ColorTranslator.FromHtml("#52514e");
    static readonly Color TintaApagada = ColorTranslator.FromHtml("#898781");
    static readonly Color Grade = ColorTranslator.FromHtml("#e1e0d9");
    static readonly Color Eixo = ColorTranslator.FromHtml("#c3c2b7");

    const int BarraMax = 24, Raio = 4, Folga = 2;

    readonly ToolTip dica = new() { InitialDelay = 0, ReshowDelay = 0 };
    readonly List<(RectangleF Area, int Categoria, int Serie)> alvos = new();
    int focoCategoria = -1;
    string textoDica = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Titulo { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Subtitulo { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Horizontal { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<double, string> Formatar { get; set; } = v => v.ToString("N0");
    string[] categorias = [];
    List<Serie> series = new();

    public Grafico()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Superficie;
        Font = new Font("Segoe UI", 9F);
        Margin = new Padding(8);
    }

    public void Definir(string[] categorias, params Serie[] series)
    {
        this.categorias = categorias;
        this.series = series.ToList();
        focoCategoria = -1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        alvos.Clear();

        using var fonteTitulo = new Font("Segoe UI Semibold", 10.5F);
        using var tintaPrimaria = new SolidBrush(TintaPrimaria);
        using var tintaSecundaria = new SolidBrush(TintaSecundaria);
        using var tintaApagada = new SolidBrush(TintaApagada);

        float y = 12;
        g.DrawString(Titulo, fonteTitulo, tintaPrimaria, 14, y);
        y += fonteTitulo.GetHeight(g) + 2;
        if (Subtitulo.Length > 0)
        {
            g.DrawString(Subtitulo, Font, tintaSecundaria, 14, y);
            y += Font.GetHeight(g) + 2;
        }

        // legenda (somente com 2+ séries)
        if (series.Count >= 2)
        {
            float x = 14;
            foreach (var s in series)
            {
                using var pincel = new SolidBrush(s.Cor);
                FillRounded(g, pincel, new RectangleF(x, y + 4, 10, 10), 2, 2);
                g.DrawString(s.Nome, Font, tintaSecundaria, x + 14, y + 1);
                x += 14 + g.MeasureString(s.Nome, Font).Width + 14;
            }
            y += Font.GetHeight(g) + 6;
        }
        y += 8;

        var area = new RectangleF(14, y, Width - 28, Height - y - 12);
        var maximo = series.SelectMany(s => s.Valores).DefaultIfEmpty(0).Max();
        if (categorias.Length == 0 || area.Width < 60 || area.Height < 40 || maximo <= 0)
        {
            var vazio = "Sem dados no período";
            var tam = g.MeasureString(vazio, Font);
            g.DrawString(vazio, Font, tintaApagada, area.X + (area.Width - tam.Width) / 2, area.Y + (area.Height - tam.Height) / 2);
            return;
        }

        if (Horizontal) DesenharBarras(g, area, maximo, tintaPrimaria, tintaSecundaria);
        else DesenharColunas(g, area, maximo, tintaPrimaria, tintaApagada);
    }

    void DesenharColunas(Graphics g, RectangleF area, double maximo, Brush tintaPrimaria, Brush tintaApagada)
    {
        var (topo, passo) = EscalaRedonda(maximo, 4);
        var rotulosY = Enumerable.Range(0, (int)Math.Round(topo / passo) + 1).Select(i => Formatar(i * passo)).ToList();
        var larguraEixo = rotulosY.Max(r => g.MeasureString(r, Font).Width) + 6;
        var alturaTexto = Font.GetHeight(g);

        var plot = new RectangleF(area.X + larguraEixo, area.Y + alturaTexto / 2, area.Width - larguraEixo, area.Height - alturaTexto * 2);
        using var caneta = new Pen(Grade, 1);
        using var canetaEixo = new Pen(Eixo, 1);
        for (int i = 0; i < rotulosY.Count; i++)
        {
            var yy = plot.Bottom - (float)(i * passo / topo) * plot.Height;
            g.DrawLine(i == 0 ? canetaEixo : caneta, plot.Left, yy, plot.Right, yy);
            var tam = g.MeasureString(rotulosY[i], Font);
            g.DrawString(rotulosY[i], Font, tintaApagada, plot.Left - tam.Width - 4, yy - tam.Height / 2);
        }

        int n = categorias.Length, ns = series.Count;
        var faixa = plot.Width / n;
        var larguraBarra = Math.Max(2f, Math.Min(BarraMax, (faixa * 0.7f - Folga * (ns - 1)) / ns));
        var larguraGrupo = larguraBarra * ns + Folga * (ns - 1);

        // rótulos do eixo X: pula alguns se não couberem
        var maiorRotulo = categorias.Max(c => g.MeasureString(c, Font).Width) + 8;
        var saltoRotulo = Math.Max(1, (int)Math.Ceiling(maiorRotulo / faixa));

        // destaca apenas o maior valor da primeira série
        var indiceMaior = Array.IndexOf(series[0].Valores, series[0].Valores.Max());

        for (int c = 0; c < n; c++)
        {
            var xFaixa = plot.Left + c * faixa;
            if (c == focoCategoria)
                using (var lavagem = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
                    g.FillRectangle(lavagem, xFaixa, plot.Top, faixa, plot.Height);

            var x = xFaixa + (faixa - larguraGrupo) / 2;
            for (int s = 0; s < ns; s++)
            {
                var v = c < series[s].Valores.Length ? series[s].Valores[c] : 0;
                var h = (float)(v / topo) * plot.Height;
                if (h > 0)
                {
                    using var pincel = new SolidBrush(series[s].Cor);
                    FillRounded(g, pincel, new RectangleF(x, plot.Bottom - h, larguraBarra, h), Math.Min(Raio, Math.Min(h, larguraBarra / 2)), 0);
                }
                if (s == 0 && c == indiceMaior && ns == 1)
                {
                    var texto = Formatar(v);
                    var tam = g.MeasureString(texto, Font);
                    g.DrawString(texto, Font, tintaPrimaria, x + larguraBarra / 2 - tam.Width / 2, plot.Bottom - h - tam.Height - 1);
                }
                x += larguraBarra + Folga;
            }
            alvos.Add((new RectangleF(xFaixa, plot.Top, faixa, plot.Height + alturaTexto), c, -1));

            if (c % saltoRotulo == 0)
            {
                var tam = g.MeasureString(categorias[c], Font);
                g.DrawString(categorias[c], Font, tintaApagada, xFaixa + faixa / 2 - tam.Width / 2, plot.Bottom + 3);
            }
        }
    }

    void DesenharBarras(Graphics g, RectangleF area, double maximo, Brush tintaPrimaria, Brush tintaSecundaria)
    {
        int n = categorias.Length;
        var larguraRotulo = Math.Min(area.Width * 0.38f, categorias.Max(c => g.MeasureString(c, Font).Width) + 8);
        var larguraValor = series[0].Valores.Max(v => g.MeasureString(Formatar(v), Font).Width) + 6;
        var plot = new RectangleF(area.X + larguraRotulo, area.Y, area.Width - larguraRotulo - larguraValor, area.Height);
        var faixa = plot.Height / n;
        var espessura = Math.Max(2f, Math.Min(BarraMax, faixa * 0.6f));
        using var canetaEixo = new Pen(Eixo, 1);
        g.DrawLine(canetaEixo, plot.Left, plot.Top, plot.Left, plot.Bottom);

        for (int c = 0; c < n; c++)
        {
            var yFaixa = plot.Top + c * faixa;
            if (c == focoCategoria)
                using (var lavagem = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
                    g.FillRectangle(lavagem, area.X, yFaixa, area.Width, faixa);

            var v = series[0].Valores[c];
            var w = (float)(v / maximo) * plot.Width;
            var yy = yFaixa + (faixa - espessura) / 2;
            if (w > 0)
            {
                using var pincel = new SolidBrush(series[0].Cor);
                FillRounded(g, pincel, new RectangleF(plot.Left + 1, yy, w, espessura), 0, Math.Min(Raio, Math.Min(w, espessura / 2)));
            }

            var rotulo = Cortar(g, categorias[c], larguraRotulo - 8);
            var tr = g.MeasureString(rotulo, Font);
            g.DrawString(rotulo, Font, tintaSecundaria, plot.Left - tr.Width - 6, yFaixa + (faixa - tr.Height) / 2);
            var valor = Formatar(v);
            var tv = g.MeasureString(valor, Font);
            g.DrawString(valor, Font, tintaPrimaria, plot.Left + w + 4, yFaixa + (faixa - tv.Height) / 2);

            alvos.Add((new RectangleF(area.X, yFaixa, area.Width, faixa), c, -1));
        }
    }

    string Cortar(Graphics g, string texto, float largura)
    {
        if (g.MeasureString(texto, Font).Width <= largura)
            return texto;
        while (texto.Length > 1 && g.MeasureString(texto + "…", Font).Width > largura)
            texto = texto[..^1];
        return texto + "…";
    }

    /// <summary>Retângulo com cantos arredondados só na ponta de dados (topo da coluna ou fim da barra).</summary>
    static void FillRounded(Graphics g, Brush pincel, RectangleF r, float raioTopo, float raioDireita)
    {
        var raio = Math.Max(raioTopo, raioDireita);
        if (raio < 1)
        {
            g.FillRectangle(pincel, r);
            return;
        }
        var d = raio * 2;
        using var path = new GraphicsPath();
        if (raioDireita > 0 && raioTopo > 0) // quadrado pequeno (legenda): todos os cantos
        {
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        }
        else if (raioTopo > 0) // coluna: arredonda em cima, reta na base
        {
            path.AddLine(r.Left, r.Bottom, r.Left, r.Top + raio);
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddLine(r.Right, r.Top + raio, r.Right, r.Bottom);
        }
        else // barra horizontal: arredonda à direita
        {
            path.AddLine(r.Left, r.Top, r.Right - raio, r.Top);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddLine(r.Right - raio, r.Bottom, r.Left, r.Bottom);
        }
        path.CloseFigure();
        g.FillPath(pincel, path);
    }

    /// <summary>Topo e passo "redondos" do eixo (0, 250, 500, 750...).</summary>
    static (double Topo, double Passo) EscalaRedonda(double maximo, int divisoes)
    {
        var bruto = maximo / divisoes;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
        var passo = new[] { 1, 2, 2.5, 5, 10 }.Select(m => m * magnitude).First(p => p >= bruto);
        return (Math.Ceiling(maximo / passo) * passo, passo);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var alvo = alvos.FirstOrDefault(a => a.Area.Contains(e.Location));
        var categoria = alvo.Area.IsEmpty ? -1 : alvo.Categoria;
        if (categoria == focoCategoria)
            return;
        focoCategoria = categoria;
        Invalidate();
        if (categoria < 0)
        {
            dica.Hide(this);
            return;
        }
        textoDica = categorias[categoria] + "\n" + string.Join("\n", series.Select(s =>
            (series.Count > 1 ? s.Nome + ": " : "") + Formatar(categoria < s.Valores.Length ? s.Valores[categoria] : 0)));
        dica.Show(textoDica, this, e.X + 14, e.Y + 14);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        focoCategoria = -1;
        dica.Hide(this);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            dica.Dispose();
        base.Dispose(disposing);
    }
}
