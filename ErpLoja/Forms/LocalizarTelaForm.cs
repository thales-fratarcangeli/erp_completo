namespace ErpLoja.Forms;

/// <summary>Localizador de telas (Ctrl+K): digite uma palavra e veja todas as telas relacionadas.</summary>
public class LocalizarTelaForm : Form
{
    readonly TextBox busca = new()
    {
        Dock = DockStyle.Top,
        Font = new Font("Segoe UI", 15F),
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(30, 41, 59),
        ForeColor = Color.White,
        PlaceholderText = "O que você procura? Ex.: boleto, frete, nota, estoque, senha...",
    };
    readonly ListBox lista = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(15, 23, 42),
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 58,
        IntegralHeight = false,
    };
    readonly Label rodape = new()
    {
        Dock = DockStyle.Bottom,
        Height = 28,
        ForeColor = Color.FromArgb(148, 163, 184),
        BackColor = Color.FromArgb(15, 23, 42),
        Padding = new Padding(14, 6, 0, 0),
        Text = "↑ ↓ navegar   Enter abrir   Esc fechar   F1 ajuda",
    };
    List<(Tela Tela, int Pontos, string Trecho)> resultados = new();
    public Tela? Escolhida { get; private set; }

    LocalizarTelaForm()
    {
        Text = "Localizar tela";
        Icon = Ui.Icone;
        Font = Ui.Fonte;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(720, 520);
        BackColor = Color.FromArgb(15, 23, 42);
        ShowInTaskbar = false;
        KeyPreview = true;
        Ajuda.InstalarEmDialogo(this, nameof(LocalizarTelaForm));
        Animacao.Surgir(this);

        var caixa = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(16, 14, 16, 8), BackColor = Color.FromArgb(30, 41, 59) };
        caixa.Controls.Add(busca);
        Controls.Add(lista);
        Controls.Add(rodape);
        Controls.Add(caixa);

        lista.DrawItem += Desenhar;
        busca.TextChanged += (_, _) => Atualizar();
        lista.DoubleClick += (_, _) => Confirmar();
        KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.Down when lista.Items.Count > 0:
                    lista.SelectedIndex = Math.Min(lista.Items.Count - 1, lista.SelectedIndex + 1);
                    e.Handled = true;
                    break;
                case Keys.Up when lista.Items.Count > 0:
                    lista.SelectedIndex = Math.Max(0, lista.SelectedIndex - 1);
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    Confirmar();
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Escape:
                    Close();
                    break;
            }
        };
        Shown += (_, _) =>
        {
            Atualizar();
            busca.Focus();
        };
    }

    void Atualizar()
    {
        resultados = string.IsNullOrWhiteSpace(busca.Text)
            ? Navegacao.Telas().Where(t => Sessao.Admin || !t.SoAdmin).Select(t => (t, 0, "")).ToList()
            : Navegacao.Buscar(busca.Text, Sessao.Admin);
        // o próprio localizador não aparece na lista (escolhê-lo só reabriria esta janela)
        resultados.RemoveAll(r => r.Tela.Chave == nameof(LocalizarTelaForm));
        lista.BeginUpdate();
        lista.Items.Clear();
        foreach (var r in resultados)
            lista.Items.Add(r.Tela.Titulo);
        lista.EndUpdate();
        if (lista.Items.Count > 0)
            lista.SelectedIndex = 0;
        rodape.Text = (resultados.Count == 0 ? "Nenhuma tela encontrada. Tente outra palavra." : $"{resultados.Count} tela(s)") +
                      "      ↑ ↓ navegar   Enter abrir   Esc fechar";
    }

    void Desenhar(object? _, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= resultados.Count)
            return;
        var (tela, _, trecho) = resultados[e.Index];
        var selecionado = (e.State & DrawItemState.Selected) != 0;
        var g = e.Graphics;
        using (var fundo = new SolidBrush(selecionado ? Color.FromArgb(37, 99, 235) : Color.FromArgb(15, 23, 42)))
            g.FillRectangle(fundo, e.Bounds);
        var cor = Navegacao.ModuloDe(tela.Modulo).Cor;
        using (var marca = new SolidBrush(cor))
            g.FillRectangle(marca, e.Bounds.X + 12, e.Bounds.Y + 14, 6, e.Bounds.Height - 28);
        using var titulo = new Font("Segoe UI Semibold", 11.5F);
        TextRenderer.DrawText(g, tela.Titulo, titulo, new Point(e.Bounds.X + 30, e.Bounds.Y + 8), Color.White);
        var modulo = tela.Modulo + (tela.Atalho != Keys.None ? $"   ·   {Ui.NomeAtalho(tela.Atalho)}" : "");
        var larguraTitulo = TextRenderer.MeasureText(tela.Titulo, titulo).Width;
        TextRenderer.DrawText(g, modulo, Ui.Fonte, new Point(e.Bounds.X + 40 + larguraTitulo, e.Bounds.Y + 10),
            selecionado ? Color.FromArgb(219, 234, 254) : Color.FromArgb(148, 163, 184));
        TextRenderer.DrawText(g, trecho.Length > 0 ? trecho : tela.Palavras, new Font("Segoe UI", 9F),
            new Rectangle(e.Bounds.X + 30, e.Bounds.Y + 32, e.Bounds.Width - 44, 20),
            selecionado ? Color.FromArgb(219, 234, 254) : Color.FromArgb(100, 116, 139), TextFormatFlags.EndEllipsis);
    }

    void Confirmar()
    {
        if (lista.SelectedIndex < 0 || lista.SelectedIndex >= resultados.Count)
            return;
        Escolhida = resultados[lista.SelectedIndex].Tela;
        DialogResult = DialogResult.OK;
    }

    public static void Executar(IWin32Window dono)
    {
        Tela? escolhida;
        using (var f = new LocalizarTelaForm())
        {
            if (f.ShowDialog(dono) != DialogResult.OK)
                return;
            escolhida = f.Escolhida;
        }
        if (escolhida is not null)
            Navegacao.Abridor?.Invoke(escolhida);
    }
}
