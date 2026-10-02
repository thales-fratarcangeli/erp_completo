using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ErpLoja.UI;

/// <summary>
/// Modo claro / escuro. As telas são desenhadas com as cores claras; no modo escuro cada cor conhecida é trocada pela
/// equivalente escura (as originais ficam guardadas para voltar ao claro sem reabrir nada). A barra de título e as
/// barras de rolagem do Windows também ficam escuras. A preferência é de cada usuário (Configurações > Minha conta).
/// </summary>
public static class Tema
{
    public static bool Escuro { get; private set; }

    /// <summary>Avisado quando o tema muda (telas com desenho próprio se redesenham).</summary>
    public static event Action? Mudou;

    sealed class Original
    {
        public Color Fundo, Texto;
        public Color? FundoGrade, Linhas, Celula, TextoCelula, Alternada, Cabecalho, TextoCabecalho, Selecao, TextoSelecao, Borda;
    }

    static readonly ConditionalWeakTable<Control, Original> originais = new();

    // fundos claros -> escuros
    static readonly Dictionary<int, Color> fundos = new()
    {
        [Color.White.ToArgb()] = Cor("#1f2937"),
        [SystemColors.Window.ToArgb()] = Cor("#1f2937"),
        [SystemColors.Control.ToArgb()] = Cor("#111827"),
        [Color.FromArgb(243, 244, 246).ToArgb()] = Cor("#111827"),   // Ui.Fundo
        [Color.FromArgb(249, 250, 251).ToArgb()] = Cor("#243042"),   // linha alternada
        [Cor("#f8fafc").ToArgb()] = Cor("#18212f"),
        [Cor("#f1f5f9").ToArgb()] = Cor("#243042"),
        [Cor("#e2e8f0").ToArgb()] = Cor("#334155"),
        [Color.FromArgb(229, 231, 235).ToArgb()] = Cor("#374151"),   // cabeçalho de grade
        [Color.FromArgb(239, 246, 255).ToArgb()] = Cor("#1e3a5f"),   // destaque sob o mouse
        [Color.FromArgb(191, 219, 254).ToArgb()] = Cor("#1e40af"),   // seleção
        [Cor("#dbeafe").ToArgb()] = Cor("#1e40af"),
        [Cor("#dcfce7").ToArgb()] = Cor("#14532d"),                   // total do PDV
        [Cor("#fde68a").ToArgb()] = Cor("#78350f"),
        [Color.FromArgb(254, 242, 242).ToArgb()] = Cor("#4c1d1d"),   // campo com erro
    };

    // textos escuros -> claros
    static readonly Dictionary<int, Color> textos = new()
    {
        [Color.Black.ToArgb()] = Cor("#e5e7eb"),
        [SystemColors.WindowText.ToArgb()] = Cor("#e5e7eb"),
        [SystemColors.ControlText.ToArgb()] = Cor("#e5e7eb"),
        [Color.FromArgb(17, 24, 39).ToArgb()] = Cor("#f3f4f6"),
        [Cor("#0f172a").ToArgb()] = Cor("#f1f5f9"),
        [Color.FromArgb(75, 85, 99).ToArgb()] = Cor("#d1d5db"),
        [Color.FromArgb(107, 114, 128).ToArgb()] = Cor("#9ca3af"),
        [Cor("#64748b").ToArgb()] = Cor("#94a3b8"),
        [Color.DimGray.ToArgb()] = Cor("#9ca3af"),
        [Cor("#15803d").ToArgb()] = Cor("#4ade80"),
        [Cor("#b45309").ToArgb()] = Cor("#fbbf24"),
        [Cor("#78350f").ToArgb()] = Cor("#fde68a"),
    };

    static Color Cor(string html) => ColorTranslator.FromHtml(html);

    /// <summary>Cor de fundo clara convertida para o tema atual (para quem pinta à mão).</summary>
    public static Color Fundo(Color clara) => Converter(clara, fundos);

    /// <summary>Cor de texto escura convertida para o tema atual.</summary>
    public static Color Texto(Color escura) => Converter(escura, textos);

    /// <summary>Troca o tema e aplica em todas as janelas abertas.</summary>
    public static void Definir(bool escuro)
    {
        if (Escuro == escuro)
            return;
        Escuro = escuro;
        foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
            Aplicar(f);
        Mudou?.Invoke();
    }

    /// <summary>Aplica o tema atual na janela/controle e em todos os filhos (e nos que forem adicionados depois).</summary>
    public static void Aplicar(Control raiz)
    {
        AplicarEm(raiz);
        foreach (Control c in raiz.Controls)
            Aplicar(c);
        if (raiz is Form f && f.IsHandleCreated)
            BarraDeTitulo(f);
    }

    /// <summary>Liga o tema a uma janela: aplica ao carregar e em controles criados depois.</summary>
    public static void Acompanhar(Form f)
    {
        f.HandleCreated += (_, _) => BarraDeTitulo(f);
        f.Load += (_, _) => Aplicar(f);
        Seguir(f);
    }

    static void Seguir(Control c)
    {
        c.ControlAdded += (_, e) =>
        {
            if (e.Control is null)
                return;
            Aplicar(e.Control);
            Seguir(e.Control);
        };
        foreach (Control filho in c.Controls)
            Seguir(filho);
    }

    static void AplicarEm(Control c)
    {
        var o = originais.GetValue(c, _ => Guardar(c));
        c.BackColor = Converter(o.Fundo, fundos);
        c.ForeColor = Converter(o.Texto, textos);
        switch (c)
        {
            case DataGridView g:
                g.BackgroundColor = Converter(o.FundoGrade!.Value, fundos);
                g.GridColor = Escuro ? Cor("#374151") : o.Linhas!.Value;
                g.DefaultCellStyle.BackColor = Converter(o.Celula!.Value, fundos);
                g.DefaultCellStyle.ForeColor = Converter(o.TextoCelula!.Value, textos);
                g.AlternatingRowsDefaultCellStyle.BackColor = Converter(o.Alternada!.Value, fundos);
                g.ColumnHeadersDefaultCellStyle.BackColor = Converter(o.Cabecalho!.Value, fundos);
                g.ColumnHeadersDefaultCellStyle.ForeColor = Converter(o.TextoCabecalho!.Value, textos);
                g.DefaultCellStyle.SelectionBackColor = Converter(o.Selecao!.Value, fundos);
                g.DefaultCellStyle.SelectionForeColor = Escuro ? Color.White : o.TextoSelecao!.Value;
                g.EnableHeadersVisualStyles = false;
                RolagemEscura(g);
                break;
            case Button b when b.FlatStyle == FlatStyle.Flat:
                var bordaClara = Color.FromArgb(209, 213, 219).ToArgb();
                b.FlatAppearance.BorderColor = Escuro && o.Borda?.ToArgb() == bordaClara ? Cor("#4b5563") : o.Borda ?? b.FlatAppearance.BorderColor;
                b.FlatAppearance.MouseOverBackColor = b.BackColor;
                break;
            case ComboBox cb:
                cb.FlatStyle = Escuro ? FlatStyle.Flat : FlatStyle.Standard;
                break;
            case ToolStrip ts:
                // o desenho padrão da barra de status ignora a cor de fundo: no escuro usa o desenho do sistema
                ts.RenderMode = Escuro ? ToolStripRenderMode.System : ToolStripRenderMode.ManagerRenderMode;
                foreach (ToolStripItem item in ts.Items)
                    if (!(item is ToolStripStatusLabel { IsLink: true }))
                        item.ForeColor = ts.ForeColor;
                break;
            case TextBoxBase or ListBox or TreeView or Panel or FlowLayoutPanel or TableLayoutPanel:
                RolagemEscura(c);
                break;
        }
    }

    static Original Guardar(Control c)
    {
        var o = new Original { Fundo = c.BackColor, Texto = c.ForeColor };
        if (c is DataGridView g)
        {
            o.FundoGrade = g.BackgroundColor;
            o.Linhas = g.GridColor;
            o.Celula = g.DefaultCellStyle.BackColor.IsEmpty ? Color.White : g.DefaultCellStyle.BackColor;
            o.TextoCelula = g.DefaultCellStyle.ForeColor.IsEmpty ? Color.Black : g.DefaultCellStyle.ForeColor;
            o.Alternada = g.AlternatingRowsDefaultCellStyle.BackColor.IsEmpty ? o.Celula : g.AlternatingRowsDefaultCellStyle.BackColor;
            o.Cabecalho = g.ColumnHeadersDefaultCellStyle.BackColor.IsEmpty ? Color.FromArgb(229, 231, 235) : g.ColumnHeadersDefaultCellStyle.BackColor;
            o.TextoCabecalho = g.ColumnHeadersDefaultCellStyle.ForeColor.IsEmpty ? Color.Black : g.ColumnHeadersDefaultCellStyle.ForeColor;
            o.Selecao = g.DefaultCellStyle.SelectionBackColor.IsEmpty ? Color.FromArgb(191, 219, 254) : g.DefaultCellStyle.SelectionBackColor;
            o.TextoSelecao = g.DefaultCellStyle.SelectionForeColor.IsEmpty ? Color.Black : g.DefaultCellStyle.SelectionForeColor;
        }
        else if (c is Button b)
            o.Borda = b.FlatAppearance.BorderColor;
        return o;
    }

    static Color Converter(Color original, Dictionary<int, Color> mapa) =>
        Escuro && mapa.TryGetValue(original.ToArgb(), out var escura) ? escura : original;

    // ------------------------------------------------------------------ Windows: barra de título e rolagem

    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    static class Nativo
    {
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tamanho);

        [DllImport("uxtheme.dll", EntryPoint = "SetWindowTheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string? app, string? lista);
    }

    static void BarraDeTitulo(Form f)
    {
        if (!f.TopLevel)
            return;
        try
        {
            var valor = Escuro ? 1 : 0;
            Nativo.DwmSetWindowAttribute(f.Handle, 20, ref valor, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
        }
        catch
        {
            // versões antigas do Windows: barra de título continua clara
        }
    }

    static void RolagemEscura(Control c)
    {
        if (!c.IsHandleCreated)
        {
            c.HandleCreated += (_, _) => RolagemEscura(c);
            return;
        }
        try
        {
            Nativo.SetWindowTheme(c.Handle, Escuro ? "DarkMode_Explorer" : "Explorer", null);
            if (c is DataGridView)
                foreach (Control barra in c.Controls)
                    if (barra is ScrollBar && barra.IsHandleCreated)
                        Nativo.SetWindowTheme(barra.Handle, Escuro ? "DarkMode_Explorer" : "Explorer", null);
        }
        catch
        {
            // sem suporte: barras de rolagem claras
        }
    }
}
