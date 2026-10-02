using System.Runtime.CompilerServices;

namespace ErpLoja.UI;

/// <summary>
/// Detecta alterações não salvas numa tela: qualquer coisa que a pessoa digite, escolha, marque ou edite numa grade.
/// Pesquisas e filtros (barra superior, campos de busca) não contam. Ao sair da tela (trocar de tela, fechar o sistema
/// ou fechar uma janela de edição) com alterações pendentes, o sistema pergunta se quer salvar, descartar ou continuar.
/// A tela volta a ficar "limpa" quando um botão de gravar (Salvar, Emitir, Confirmar...) termina sem mensagem de erro.
/// </summary>
public static class Alteracoes
{
    sealed class Estado
    {
        public bool Pendente;
        public bool Ignorar;
    }

    static readonly ConditionalWeakTable<Form, Estado> estados = new();

    /// <summary>Marca a barra/painel de filtros: o que se digita ali não é alteração de dados.</summary>
    public static readonly object MarcaFiltros = new();

    /// <summary>Palavras que iniciam botões de gravação (quando terminam sem erro, a tela fica limpa).</summary>
    static readonly string[] Gravacoes = ["Salvar", "Gravar", "Emitir", "Confirmar", "Aplicar", "Importar", "Faturar", "Receber", "Concluir", "Entrar"];

    static Estado De(Form f) => estados.GetValue(f, _ => new Estado());

    /// <summary>A tela cuida sozinha de avisar o que está em andamento (ex.: PDV com venda aberta).</summary>
    public static void Ignorar(Form f) => De(f).Ignorar = true;

    public static bool Pendentes(Form? f) => f is not null && !De(f).Ignorar && De(f).Pendente;

    public static void Limpar(Form? f)
    {
        if (f is not null)
            De(f).Pendente = false;
    }

    public static void Marcar(Control c)
    {
        if (c.FindForm() is { } f && !EhFiltro(c))
            De(f).Pendente = true;
    }

    public static bool EhGravacao(string textoBotao) =>
        Gravacoes.Any(g => textoBotao.TrimStart().StartsWith(g, StringComparison.OrdinalIgnoreCase));

    /// <summary>Começa a observar a tela (chamado por Ui.Estilizar e pelos diálogos). Controles adicionados depois também são observados.</summary>
    public static void Monitorar(Control raiz)
    {
        Observar(raiz);
        foreach (Control c in raiz.Controls)
            Monitorar(c);
        raiz.ControlAdded += (_, e) =>
        {
            if (e.Control != null)
                Monitorar(e.Control);
        };
    }

    static void Observar(Control c)
    {
        switch (c)
        {
            case TextBox tb:
                tb.KeyPress += (_, e) => { if (!tb.ReadOnly && (!char.IsControl(e.KeyChar) || e.KeyChar == '\b')) Marcar(tb); };
                tb.KeyDown += (_, e) => { if (!tb.ReadOnly && (e.KeyCode == Keys.Delete || (e.Control && e.KeyCode is Keys.V or Keys.X))) Marcar(tb); };
                break;
            case NumericUpDown n:
                n.ValueChanged += (_, _) => { if (n.ContainsFocus) Marcar(n); };
                break;
            case ComboBox cb:
                cb.SelectionChangeCommitted += (_, _) => Marcar(cb);
                cb.KeyPress += (_, e) => { if (cb.DropDownStyle != ComboBoxStyle.DropDownList && !char.IsControl(e.KeyChar)) Marcar(cb); };
                break;
            case CheckBox ck:
                ck.Click += (_, _) => Marcar(ck);
                break;
            case DateTimePicker dt:
                dt.ValueChanged += (_, _) => { if (dt.Focused) Marcar(dt); };
                break;
            case DataGridView g:
                g.CellEndEdit += (_, _) => { if (!g.ReadOnly) Marcar(g); };
                g.UserDeletedRow += (_, _) => Marcar(g);
                break;
        }
    }

    /// <summary>Filtros: dentro da barra superior de filtros ou campo de pesquisa/lançamento rápido.</summary>
    static bool EhFiltro(Control c)
    {
        if (c is TextBox tb && Validacao.FormatoDe(tb) is { } f
            && (f == Formatos.Busca || f == Formatos.CodigoOuNome || f == Formatos.EntradaPdv))
            return true;
        for (var p = c.Parent; p != null && p is not Form; p = p.Parent)
            if (p.Tag == MarcaFiltros)
                return true;
        return false;
    }

    public enum Escolha { SalvarESair, DescartarESair, Continuar }

    /// <summary>
    /// Pergunta o que fazer com as alterações pendentes antes de sair da tela. Devolve true se pode sair.
    /// "Salvar e sair" clica no botão de gravar da tela e só sai se a gravação deu certo.
    /// </summary>
    public static bool PodeSair(Form f, string acao = "sair desta tela", bool permitirSalvar = true)
    {
        if (!Pendentes(f))
            return true;
        var salvar = !permitirSalvar ? null
            : Todos(f).OfType<Button>().FirstOrDefault(b => b.Visible && b.Enabled && EhGravacao(b.Text) && !b.Text.TrimStart().StartsWith("Entrar"));
        var escolha = Perguntar(f.Text, acao, salvar != null);
        switch (escolha)
        {
            case Escolha.SalvarESair:
                salvar!.PerformClick();
                if (Pendentes(f))
                    return false; // a gravação não aconteceu (a tela mostrou o motivo)
                return true;
            case Escolha.DescartarESair:
                Log.Registrar(Log.Acao, "Alterações descartadas ao sair", f.Text, tela: f.Text);
                Limpar(f);
                return true;
            default:
                return false;
        }
    }

    static Escolha Perguntar(string tela, string acao, bool podeSalvar)
    {
        var texto = $"Você preencheu ou alterou dados em \"{tela}\" que ainda não foram salvos.\n\nO que deseja fazer antes de {acao}?";
        // testes automatizados: sem janela (Ui.Interceptar) — "Sim" descarta e sai, "Não" continua editando
        if (Ui.Interceptar is { } interceptar)
            return interceptar("CONFIRMAR", texto) ? Escolha.DescartarESair : Escolha.Continuar;

        using var d = new Form
        {
            Text = "Alterações não salvas",
            Font = Ui.Fonte,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18),
            Icon = Ui.Icone,
            BackColor = Tema.Fundo(Color.White),
            ForeColor = Tema.Texto(Color.Black),
        };
        var escolha = Escolha.Continuar;
        var corpo = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        corpo.Controls.Add(new Label { Text = "⚠  " + texto, AutoSize = true, MaximumSize = new Size(460, 0), Margin = new Padding(0, 0, 0, 14) });
        var botoes = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        if (podeSalvar)
            botoes.Controls.Add(Ui.Botao("Salvar e sair", (_, _) => { escolha = Escolha.SalvarESair; d.Close(); }, primario: true));
        var descartar = Ui.Botao("Descartar e sair", (_, _) => { escolha = Escolha.DescartarESair; d.Close(); });
        descartar.ForeColor = Ui.Perigo;
        botoes.Controls.Add(descartar);
        var continuar = Ui.Botao("Continuar editando", (_, _) => { escolha = Escolha.Continuar; d.Close(); });
        botoes.Controls.Add(continuar);
        corpo.Controls.Add(botoes);
        d.Controls.Add(corpo);
        d.CancelButton = continuar;
        d.Shown += (_, _) => continuar.Focus();
        d.ShowDialog();
        return escolha;
    }

    static IEnumerable<Control> Todos(Control c) => c.Controls.Cast<Control>().SelectMany(x => Todos(x).Prepend(x));
}
