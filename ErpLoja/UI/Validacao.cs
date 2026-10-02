using System.Runtime.CompilerServices;

namespace ErpLoja.UI;

/// <summary>
/// Liga os campos de texto aos formatos (Data/Formatos): bloqueia a digitação e a colagem de caracteres fora do formato
/// (avisando o que o campo aceita), aplica a máscara e marca o campo com erro ao sair dele, e confere todos os campos
/// de uma tela antes de gravar. Todo campo editável do sistema precisa de um formato (verificado pela bateria de testes).
/// </summary>
public static class Validacao
{
    sealed record Estado(Formato Formato, string Rotulo, bool Obrigatorio);

    static readonly ConditionalWeakTable<TextBox, Estado> registro = new();
    static readonly ErrorProvider erros = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    static readonly ToolTip dica = new() { ToolTipIcon = ToolTipIcon.Info, ToolTipTitle = "Formato do campo" };
    static readonly Color FundoErro = Color.FromArgb(254, 242, 242);
    static DateTime ultimoAviso;

    /// <summary>Caixa de texto nova já ligada ao formato.</summary>
    public static TextBox Caixa(Formato formato, string rotulo = "", bool obrigatorio = false, int largura = 0)
    {
        var tb = new TextBox { Multiline = formato.MultiLinha };
        if (formato.MultiLinha)
        {
            tb.Height = 70;
            tb.ScrollBars = ScrollBars.Vertical;
        }
        if (largura > 0)
            tb.Width = largura;
        return Aplicar(tb, formato, rotulo, obrigatorio);
    }

    /// <summary>Liga uma caixa existente ao formato. rotulo aparece nas mensagens de erro.</summary>
    public static TextBox Aplicar(TextBox tb, Formato formato, string rotulo = "", bool obrigatorio = false)
    {
        var novo = !registro.TryGetValue(tb, out _);
        registro.AddOrUpdate(tb, new Estado(formato, rotulo, obrigatorio));
        if (!novo)
            return tb;
        if (formato.Tamanho > 0)
            tb.MaxLength = formato.Tamanho;
        if (tb.UseSystemPasswordChar == false && !tb.Multiline)
            dica.SetToolTip(tb, Capitalizar(formato.Dica));

        tb.KeyPress += (_, e) =>
        {
            if (!registro.TryGetValue(tb, out var est) || char.IsControl(e.KeyChar))
                return;
            if (est.Formato.Permitido is { } permitido && !permitido(e.KeyChar))
            {
                e.Handled = true;
                Avisar(tb, est.Formato.Dica);
            }
            else if (est.Formato.Maiusculas)
                e.KeyChar = char.ToUpperInvariant(e.KeyChar);
        };
        // colar (Ctrl+V / menu) com caracteres fora do formato: o texto NÃO é alterado em silêncio (removendo a letra O de
        // "1O2" sobraria "12", outro valor válido); o campo fica marcado com a explicação e a gravação é recusada
        tb.TextChanged += (_, _) =>
        {
            if (!registro.TryGetValue(tb, out var est))
                return;
            if (Erro(tb) is string erro && est.Formato.Validar(tb.Text) == est.Formato.Dica)
            {
                erros.SetError(tb, erro);
                tb.BackColor = FundoErro;
                Avisar(tb, est.Formato.Dica);
            }
            else if (erros.GetError(tb).Length > 0 && Erro(tb) is null)
                Limpar(tb);
        };
        tb.Leave += (_, _) => Conferir(tb);
        return tb;
    }

    public static Formato? FormatoDe(TextBox tb) => registro.TryGetValue(tb, out var e) ? e.Formato : null;

    /// <summary>Mensagem de erro do campo (com o nome dele) ou null se estiver válido.</summary>
    public static string? Erro(TextBox tb)
    {
        if (!registro.TryGetValue(tb, out var est))
            return null;
        var nome = est.Rotulo.Length > 0 ? $"\"{est.Rotulo.TrimEnd(':', ' ', '*')}\"" : "O campo";
        if (string.IsNullOrWhiteSpace(tb.Text))
            return est.Obrigatorio ? $"{nome} é obrigatório." : null;
        return est.Formato.Validar(tb.Text) is string erro ? $"{nome} {erro}" : null;
    }

    /// <summary>Confere o campo: válido recebe a máscara; inválido fica marcado (ícone e fundo) com a explicação.</summary>
    public static bool Conferir(TextBox tb)
    {
        if (!registro.TryGetValue(tb, out var est) || tb.ReadOnly || !tb.Enabled)
            return true;
        if (Erro(tb) is string erro)
        {
            erros.SetError(tb, erro);
            tb.BackColor = FundoErro;
            return false;
        }
        var normal = est.Formato.Normalizar(tb.Text);
        if (normal != tb.Text.Trim() || tb.Text != tb.Text.Trim())
            tb.Text = normal;
        Limpar(tb);
        return true;
    }

    /// <summary>
    /// Confere todos os campos com formato dentro da tela/painel. No primeiro inválido mostra a mensagem, coloca o
    /// cursor nele e devolve false (a gravação não deve continuar).
    /// </summary>
    public static bool ConferirTodos(Control raiz, bool mostrarMensagem = true)
    {
        TextBox? primeiro = null;
        foreach (var tb in Todos(raiz).OfType<TextBox>())
            if (tb.Visible && !Conferir(tb))
                primeiro ??= tb;
        if (primeiro is null)
            return true;
        if (mostrarMensagem)
            Ui.Erro(Erro(primeiro)!);
        primeiro.Focus();
        return false;
    }

    /// <summary>Campos editáveis sem formato (usado pelos testes para garantir que nenhum campo fique sem validação).</summary>
    public static List<TextBox> CamposSemFormato(Control raiz) =>
        Todos(raiz).OfType<TextBox>().Where(tb => !tb.ReadOnly && tb.Parent is not UpDownBase && !registro.TryGetValue(tb, out _)).ToList();

    static IEnumerable<Control> Todos(Control c) => c.Controls.Cast<Control>().SelectMany(x => Todos(x).Prepend(x));

    static void Limpar(TextBox tb)
    {
        erros.SetError(tb, "");
        if (tb.BackColor == FundoErro)
            tb.BackColor = SystemColors.Window;
    }

    static void Avisar(TextBox tb, string texto)
    {
        if ((DateTime.Now - ultimoAviso).TotalMilliseconds < 600 || !tb.IsHandleCreated || !tb.Visible)
            return;
        ultimoAviso = DateTime.Now;
        System.Media.SystemSounds.Beep.Play();
        try { dica.Show("Este campo " + texto, tb, 0, tb.Height + 2, 2500); } catch { }
    }

    static string Capitalizar(string s) => s.Length == 0 ? s : "Este campo " + s;

    // ------------------------------------------------------------------ números (NumericUpDown) e grades

    /// <summary>NumericUpDown não aceita letras: só algarismos, a vírgula (se tiver casas) e o sinal (se aceitar negativo).</summary>
    public static NumericUpDown SoNumeros(NumericUpDown n)
    {
        n.KeyPress += (_, e) =>
        {
            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                return;
            var separador = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            if (n.DecimalPlaces > 0 && (e.KeyChar == separador || e.KeyChar is ',' or '.'))
            {
                e.KeyChar = separador;
                return;
            }
            if (n.Minimum < 0 && e.KeyChar == '-')
                return;
            e.Handled = true;
            System.Media.SystemSounds.Beep.Play();
        };
        return n;
    }

    sealed class FormatosDaGrade
    {
        public readonly Dictionary<string, Formato> Colunas = new();
        public KeyPressEventHandler? Filtro;
        public TextBox? Editor;
    }

    static readonly ConditionalWeakTable<DataGridView, FormatosDaGrade> grades = new();

    /// <summary>
    /// Grades editáveis: colunas numéricas só aceitam números; colunas com formato (ex.: NCM, CFOP) filtram a digitação
    /// e não deixam sair da célula com valor inválido.
    /// </summary>
    public static void Grade(DataGridView g)
    {
        if (grades.TryGetValue(g, out _))
            return;
        var info = new FormatosDaGrade();
        grades.Add(g, info);
        g.EditingControlShowing += (_, e) =>
        {
            if (info.Editor != null && info.Filtro != null)
                info.Editor.KeyPress -= info.Filtro;
            info.Editor = null;
            info.Filtro = null;
            if (e.Control is not TextBox editor || g.CurrentCell is null)
                return;
            var coluna = g.Columns[g.CurrentCell.ColumnIndex];
            Func<char, bool>? permitido = null;
            var maiusculas = false;
            if (info.Colunas.TryGetValue(coluna.DataPropertyName, out var f))
            {
                permitido = f.Permitido;
                maiusculas = f.Maiusculas;
                editor.MaxLength = f.Tamanho > 0 ? f.Tamanho : 32767;
            }
            else if (Numerica(coluna.ValueType))
            {
                var inteiro = coluna.ValueType == typeof(int) || coluna.ValueType == typeof(long);
                permitido = c => char.IsDigit(c) || (!inteiro && c is ',' or '.') || c == '-';
                editor.MaxLength = 18;
            }
            else
                editor.MaxLength = 500;
            if (permitido is null && !maiusculas)
                return;
            info.Filtro = (_, k) =>
            {
                if (char.IsControl(k.KeyChar))
                    return;
                if (permitido != null && !permitido(k.KeyChar))
                {
                    k.Handled = true;
                    System.Media.SystemSounds.Beep.Play();
                }
                else if (maiusculas)
                    k.KeyChar = char.ToUpperInvariant(k.KeyChar);
            };
            info.Editor = editor;
            editor.KeyPress += info.Filtro;
        };
        g.CellValidating += (_, e) =>
        {
            if (e.RowIndex < 0 || !g.IsCurrentCellInEditMode)
                return;
            var coluna = g.Columns[e.ColumnIndex];
            if (!info.Colunas.TryGetValue(coluna.DataPropertyName, out var f))
                return;
            if (f.Validar(Convert.ToString(e.FormattedValue)) is string erro)
            {
                g.Rows[e.RowIndex].ErrorText = $"{coluna.HeaderText} {erro}";
                Ui.Aviso($"{coluna.HeaderText} {erro}");
                e.Cancel = true;
            }
            else
                g.Rows[e.RowIndex].ErrorText = "";
        };
    }

    /// <summary>Define o formato de uma coluna (pelo nome da propriedade/coluna de dados) de uma grade editável.</summary>
    public static void Coluna(DataGridView g, string propriedade, Formato formato)
    {
        Grade(g);
        grades.TryGetValue(g, out var info);
        info!.Colunas[propriedade] = formato;
    }

    /// <summary>Formato de uma coluna da grade (para testes).</summary>
    public static Formato? FormatoDaColuna(DataGridView g, string propriedade) =>
        grades.TryGetValue(g, out var info) && info.Colunas.TryGetValue(propriedade, out var f) ? f : null;

    static bool Numerica(Type? t) => t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(long);
}
