using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ErpLoja.UI;

/// <summary>Estilo visual e utilitários comuns das telas.</summary>
public static class Ui
{
    public static readonly Font Fonte = new("Segoe UI", 10F);
    public static readonly Font FonteNegrito = new("Segoe UI Semibold", 10F);
    public static readonly Font FonteTitulo = new("Segoe UI Semibold", 16F);
    public static readonly Color Primaria = Color.FromArgb(37, 99, 235);
    public static readonly Color Fundo = Color.FromArgb(243, 244, 246);
    public static readonly Color Perigo = Color.FromArgb(220, 38, 38);
    public static readonly Color Alerta = Color.FromArgb(217, 119, 6);
    public static readonly Color Sucesso = Color.FromArgb(22, 163, 74);

    static Icon? icone;

    /// <summary>Ícone do sistema (Recursos/logo.ico embutido no executável).</summary>
    public static Icon Icone => icone ??= CarregarIcone();

    static Icon CarregarIcone()
    {
        using var s = typeof(Ui).Assembly.GetManifestResourceStream("ErpLoja.logo.ico");
        return s is null ? SystemIcons.Application : new Icon(s);
    }

    public static Image Logo(int tamanho)
    {
        using var s = typeof(Ui).Assembly.GetManifestResourceStream("ErpLoja.logo.png");
        if (s is null)
            return new Bitmap(tamanho, tamanho);
        using var original = Image.FromStream(s);
        return new Bitmap(original, tamanho, tamanho);
    }

    /// <summary>Aparência padrão + ícone + botão de ajuda "?" (e F1). ajuda = chave do tópico (padrão: nome da classe).</summary>
    public static void Estilizar(Form f, string titulo, int largura, int altura, string? ajuda = null)
    {
        f.Text = titulo;
        f.Font = Fonte;
        f.BackColor = Fundo;
        f.Icon = Icone;
        f.StartPosition = FormStartPosition.CenterScreen;
        f.Size = new Size(largura, altura);
        f.MinimumSize = new Size(Math.Min(largura, 800), Math.Min(altura, 500));
        f.KeyPreview = true;
        var chave = ajuda ?? f.GetType().Name;
        Ajuda.Instalar(f, chave);

        // faixa superior na cor do módulo da tela + entrada suave
        var cor = Navegacao.CorDaTela(chave);
        f.Load += (_, _) =>
        {
            var faixa = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = cor };
            f.Controls.Add(faixa);
            faixa.SendToBack();
        };
        Animacao.Surgir(f);
        // alterações não salvas e tema (claro/escuro) em todas as telas
        f.Load += (_, _) => Alteracoes.Monitorar(f);
        Tema.Acompanhar(f);
        f.FormClosing += (_, e) =>
        {
            // janelas de edição soltas: perguntar antes de perder o que foi digitado (as telas do menu são tratadas pela janela principal)
            if (f.TopLevel && f is not MainForm && e.CloseReason == CloseReason.UserClosing && f.DialogResult != DialogResult.OK
                && !Alteracoes.PodeSair(f, "fechar esta janela", permitirSalvar: false))
                e.Cancel = true;
        };
    }

    public static DataGridView Grid(bool somenteLeitura = true)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = somenteLeitura,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        };
        g.RowTemplate.Height = 28;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(229, 231, 235);
        g.ColumnHeadersDefaultCellStyle.Font = FonteNegrito;
        // cabeçalho longo vira "Último nosso…" numa linha (em vez de quebrar e ficar cortado); o texto inteiro aparece na dica
        g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        g.ShowCellToolTips = true;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(191, 219, 254);
        g.DefaultCellStyle.SelectionForeColor = Color.Black;
        g.DataBindingComplete += (_, _) => FormatarColunas(g);
        g.DataError += (_, e) => { e.ThrowException = false; Erro("Valor inválido."); };
        Validacao.Grade(g); // células numéricas só aceitam números; colunas com formato filtram a digitação
        DestacarLinhaSobMouse(g);
        MostrarEstadoVazio(g);
        RolarNaHorizontal(g);
        Ordenacao(g);
        return g;
    }

    /// <summary>
    /// Colunas ocupam a largura da tela, mas nunca ficam espremidas: cada uma tem largura mínima (pelo título e pelo tipo
    /// de dado). Quando não cabem, aparece a barra de rolagem horizontal em vez de cortar o conteúdo.
    /// </summary>
    public static void RolarNaHorizontal(DataGridView g)
    {
        g.ScrollBars = ScrollBars.Both;
        g.AllowUserToResizeColumns = true;
        void Ajustar()
        {
            foreach (DataGridViewColumn c in g.Columns)
            {
                var titulo = TextRenderer.MeasureText(c.HeaderText ?? "", g.ColumnHeadersDefaultCellStyle.Font ?? FonteNegrito).Width + 30; // + seta de ordenação
                var tipo = c.ValueType;
                var piso = tipo == typeof(string) ? 110 : tipo == typeof(DateTime) ? 95 : c is DataGridViewCheckBoxColumn ? 50 : 80;
                var minimo = Math.Clamp(Math.Max(titulo, piso), 50, 240);
                if (c.MinimumWidth != minimo)
                    c.MinimumWidth = minimo;
            }
        }
        // durante ColumnAdded e no meio de uma ordenação a grade ainda está se montando (mudar largura ali quebra a
        // grade): ajusta logo depois, quando a fila de mensagens estiver livre
        void Depois()
        {
            // grade ainda não exibida (ex.: aba não aberta): ajusta em HandleCreated, quando for mostrada
            if (g.IsHandleCreated && !g.IsDisposed)
                g.BeginInvoke(() => { if (!g.IsDisposed) Ajustar(); });
        }
        g.ColumnAdded += (_, _) => Depois();
        g.DataBindingComplete += (_, _) => Depois();
        g.HandleCreated += (_, _) => Depois();
    }

    /// <summary>
    /// Clicar no título da coluna ordena (de novo inverte). Vale para listas de banco (DataTable) e para listas de itens
    /// (BindingList, ordenadas aqui mesmo). A ordem escolhida é mantida quando a tela recarrega a lista.
    /// Colunas marcadas como NotSortable (ex.: itens do PDV, cuja ordem é a da venda) não ordenam.
    /// </summary>
    static void Ordenacao(DataGridView g)
    {
        (string Coluna, ListSortDirection Direcao)? escolhida = null;
        g.Sorted += (_, _) =>
        {
            if (g.SortedColumn is { } c && !string.IsNullOrEmpty(c.DataPropertyName))
                escolhida = (c.DataPropertyName, g.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);
            foreach (DataGridViewColumn d in g.Columns)
                if (d.SortMode == DataGridViewColumnSortMode.Programmatic)
                    d.HeaderCell.SortGlyphDirection = SortOrder.None;
        };
        g.DataBindingComplete += (_, e) =>
        {
            if (e.ListChangedType != ListChangedType.Reset || g.DataSource is not DataTable dt)
                return;
            // colunas de data que o banco devolve como texto "dd/mm/aaaa": ordenação própria, pela data de verdade
            foreach (DataGridViewColumn c in g.Columns)
            {
                if (c.DataPropertyName.StartsWith(PrefixoOrdem))
                    c.Visible = false;
                else if (c.SortMode == DataGridViewColumnSortMode.Automatic && ColunaDeData(dt, c.DataPropertyName))
                    c.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (escolhida is not { } o || g.SortedColumn != null || dt.DefaultView.Sort.Length > 0)
                return;
            var col = g.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.DataPropertyName == o.Coluna);
            if (col is { SortMode: DataGridViewColumnSortMode.Automatic })
                g.Sort(col, o.Direcao);
            else if (col is { SortMode: DataGridViewColumnSortMode.Programmatic } && g.IsHandleCreated)
                g.BeginInvoke(() => { if (!g.IsDisposed && g.DataSource == dt) OrdenarPorData(g, dt, col, o.Direcao); });
        };
        g.ColumnHeaderMouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0)
                return;
            var col = g.Columns[e.ColumnIndex];
            if (col.SortMode == DataGridViewColumnSortMode.Programmatic && g.DataSource is DataTable dt && ColunaDeData(dt, col.DataPropertyName))
            {
                var direcao = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                OrdenarPorData(g, dt, col, direcao);
                escolhida = (col.DataPropertyName, direcao);
                Log.Registrar(Log.Acao, "Ordenou a lista", $"{col.HeaderText} ({(direcao == ListSortDirection.Descending ? "decrescente" : "crescente")})");
            }
            else
                OrdenarLista(g, col);
        };
    }

    const string PrefixoOrdem = "__ordem_";
    static readonly Regex DataBr = new(@"^\d{2}/\d{2}/\d{4}( \d{2}:\d{2}(:\d{2})?)?$");
    static readonly string[] FormatosDataBr = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"];

    /// <summary>Coluna de texto cujos valores preenchidos são todos datas "dd/mm/aaaa" (com hora opcional).</summary>
    static bool ColunaDeData(DataTable dt, string coluna)
    {
        if (string.IsNullOrEmpty(coluna) || dt.Columns[coluna] is not { } c || c.DataType != typeof(string))
            return false;
        var algum = false;
        var conferidas = 0;
        foreach (DataRow r in dt.Rows)
        {
            if (r[c] is not string s || s.Length == 0)
                continue;
            if (!DataBr.IsMatch(s))
                return false;
            algum = true;
            if (++conferidas >= 300) // amostra basta (listas grandes, como o log)
                break;
        }
        return algum;
    }

    /// <summary>
    /// Ordena pela data real: cria (uma vez) uma coluna oculta com o valor DateTime e ordena a lista por ela. Assim
    /// 15/03 vem antes de 01/12, o que a ordem do texto não faria.
    /// </summary>
    static void OrdenarPorData(DataGridView g, DataTable dt, DataGridViewColumn col, ListSortDirection direcao)
    {
        var auxiliar = PrefixoOrdem + col.DataPropertyName;
        if (!dt.Columns.Contains(auxiliar))
        {
            var nova = dt.Columns.Add(auxiliar, typeof(DateTime));
            foreach (DataRow r in dt.Rows)
                r[nova] = r[col.DataPropertyName] is string s
                          && DateTime.TryParseExact(s, FormatosDataBr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                    ? d : DBNull.Value;
            dt.AcceptChanges();
        }
        dt.DefaultView.Sort = $"[{auxiliar}] {(direcao == ListSortDirection.Descending ? "DESC" : "ASC")}";
        foreach (DataGridViewColumn c in g.Columns)
        {
            if (c.DataPropertyName.StartsWith(PrefixoOrdem))
                c.Visible = false;
            if (c.SortMode != DataGridViewColumnSortMode.NotSortable)
                c.HeaderCell.SortGlyphDirection = c.DataPropertyName == col.DataPropertyName ? (direcao == ListSortDirection.Descending ? SortOrder.Descending : SortOrder.Ascending) : SortOrder.None;
        }
    }

    /// <summary>Ordena uma BindingList (que não sabe se ordenar sozinha) pela coluna, mantendo o item selecionado.</summary>
    public static void OrdenarLista(DataGridView g, DataGridViewColumn col)
    {
        if (col.SortMode == DataGridViewColumnSortMode.NotSortable || string.IsNullOrEmpty(col.DataPropertyName)
            || g.DataSource is not IBindingList { SupportsSorting: false } lista || lista.Count < 2 || lista[0] is not { } primeiro
            || TypeDescriptor.GetProperties(primeiro)[col.DataPropertyName] is not { } prop)
            return;
        if (g.IsCurrentCellInEditMode && !g.EndEdit())
            return;
        var decrescente = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending;
        var itens = lista.Cast<object>().ToList();
        List<object> ordenados;
        try
        {
            ordenados = (decrescente ? itens.OrderByDescending(prop.GetValue, Comparer<object?>.Default) : itens.OrderBy(prop.GetValue, Comparer<object?>.Default)).ToList();
        }
        catch (InvalidOperationException)
        {
            return; // valores que não se comparam: deixa como está
        }
        var selecionado = g.CurrentRow?.DataBoundItem;
        lista.Clear();
        foreach (var i in ordenados)
            lista.Add(i);
        foreach (DataGridViewColumn c in g.Columns)
            if (c.SortMode != DataGridViewColumnSortMode.NotSortable)
                c.HeaderCell.SortGlyphDirection = c == col ? (decrescente ? SortOrder.Descending : SortOrder.Ascending) : SortOrder.None;
        if (selecionado != null && lista.IndexOf(selecionado) is var pos and >= 0 && pos < g.Rows.Count)
        {
            g.ClearSelection();
            g.CurrentCell = g.Rows[pos].Cells.Cast<DataGridViewCell>().FirstOrDefault(x => x.Visible);
        }
        Log.Registrar(Log.Acao, "Ordenou a lista", $"{col.HeaderText} ({(decrescente ? "decrescente" : "crescente")})");
    }

    /// <summary>Texto mostrado pela grade quando não há registros (Tag da grade pode trazer uma dica específica).</summary>
    public const string DicaGradeVazia = "Ajuste a pesquisa ou os filtros, ou use as opções da tela para incluir.";

    /// <summary>A linha sob o mouse ganha um fundo suave: a pessoa vê qual registro vai abrir ou selecionar.</summary>
    static void DestacarLinhaSobMouse(DataGridView g)
    {
        var destaque = Color.FromArgb(239, 246, 255);
        var linhaAtual = -1;
        void Limpar()
        {
            if (linhaAtual >= 0 && linhaAtual < g.Rows.Count)
                g.Rows[linhaAtual].DefaultCellStyle.BackColor = Color.Empty;
            linhaAtual = -1;
        }
        g.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex == linhaAtual)
                return;
            Limpar();
            if (e.RowIndex >= 0 && e.RowIndex < g.Rows.Count)
            {
                g.Rows[e.RowIndex].DefaultCellStyle.BackColor = destaque;
                linhaAtual = e.RowIndex;
            }
        };
        g.MouseLeave += (_, _) => Limpar();
        g.DataSourceChanged += (_, _) => linhaAtual = -1;
    }

    /// <summary>Grade sem registros mostra uma explicação em vez de um espaço em branco.</summary>
    static void MostrarEstadoVazio(DataGridView g)
    {
        g.Paint += (_, e) =>
        {
            if (g.Rows.Count > 0 || g.Columns.Count == 0)
                return;
            var area = new Rectangle(0, g.ColumnHeadersVisible ? g.ColumnHeadersHeight : 0, g.ClientSize.Width,
                Math.Max(60, g.ClientSize.Height - (g.ColumnHeadersVisible ? g.ColumnHeadersHeight : 0)));
            var titulo = new Rectangle(area.X, area.Y + Math.Min(60, area.Height / 3), area.Width, 28);
            TextRenderer.DrawText(e.Graphics, "Nenhum registro para mostrar", FonteNegrito, titulo, Color.FromArgb(107, 114, 128),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            var dica = new Rectangle(area.X + 20, titulo.Bottom, area.Width - 40, 40);
            TextRenderer.DrawText(e.Graphics, g.Tag as string ?? DicaGradeVazia, Fonte, dica, Color.FromArgb(156, 163, 175),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        };
        g.DataBindingComplete += (_, _) => g.Invalidate();
        g.RowsRemoved += (_, _) => { if (g.Rows.Count == 0) g.Invalidate(); };
    }

    static void FormatarColunas(DataGridView g)
    {
        foreach (DataGridViewColumn c in g.Columns)
        {
            if (c.DataPropertyName == "id")
                c.Visible = false;
            var t = c.ValueType;
            if (t == typeof(double) || t == typeof(decimal))
            {
                c.DefaultCellStyle.Format = "N2";
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (t == typeof(long) || t == typeof(int))
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }

    /// <summary>Pinta a coluna "Situação" conforme o valor (VENCIDA, PAGA, BAIXO...).</summary>
    public static void ColorirSituacao(DataGridView g)
    {
        g.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.CellStyle is null || g.Columns[e.ColumnIndex].DataPropertyName != "Situação")
                return;
            e.CellStyle.ForeColor = Txt(e.Value) switch
            {
                "VENCIDA" or "VENCIDO" or "ATRASADA" or "SEM ESTOQUE" or "RECUSADO" or "REJEITADO" or "PENDENTE" => Perigo,
                "BAIXO" or "VENCE HOJE" or "ENTREGUE COM ATRASO" or "DIGITADA" or "EM PRODUÇÃO" or "EM TRÂNSITO" or "ENVIADO" or "DECLARADO" => Alerta,
                "PAGA" or "PAGO" or "OK" or "APROVADO" or "FATURADO" or "RECEBIDO" or "EMITIDA" or "CONCLUÍDA" or "ENTREGUE"
                    or "REGISTRADO" or "ENCERRADO" => Sucesso,
                "CANCELADA" or "CANCELADO" or "BAIXADO" => Color.Gray,
                _ => e.CellStyle.ForeColor,
            };
        };
    }

    public static Button Botao(string texto, EventHandler? clique = null, bool primario = false)
    {
        var b = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(100, 34),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(4),
            BackColor = primario ? Primaria : Color.White,
            ForeColor = primario ? Color.White : Color.Black,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderColor = primario ? Primaria : Color.FromArgb(209, 213, 219);
        b.FlatAppearance.MouseOverBackColor = b.BackColor;
        b.FlatAppearance.MouseDownBackColor = primario ? Animacao.Escurecer(Primaria, 0.3) : Color.FromArgb(229, 231, 235);
        Animacao.Hover(b, b.BackColor, primario ? Animacao.Escurecer(Primaria, 0.18) : Color.FromArgb(239, 246, 255));
        // log de atividades: registrado antes da ação, com a tela onde o botão está
        b.Click += (_, _) => Log.Registrar(Log.Acao, "Botão " + b.Text.Trim(), tela: b.FindForm()?.Text);
        if (clique != null)
            b.Click += (s, e) =>
            {
                var form = b.FindForm();
                var errosAntes = ErrosExibidos;
                clique(s, e);
                // botão de gravar que terminou sem mensagem de erro: a tela não tem mais alterações pendentes
                if (Alteracoes.EhGravacao(b.Text) && ErrosExibidos == errosAntes)
                    Alteracoes.Limpar(form);
            };
        return b;
    }

    /// <summary>Quantos avisos de erro já foram mostrados (para saber se uma gravação deu certo).</summary>
    public static int ErrosExibidos { get; private set; }

    public static string NomeAtalho(Keys k) =>
        ((k & Keys.Control) != 0 ? "Ctrl+" : "") + (k & Keys.KeyCode);

    public static Label Rotulo(string texto) =>
        new() { Text = texto, AutoSize = true, Margin = new Padding(6, 11, 2, 0) };

    /// <summary>Barra de ferramentas. A barra do topo reserva espaço à direita para o botão "?".</summary>
    public static FlowLayoutPanel Barra(DockStyle dock = DockStyle.Top) => new()
    {
        Dock = dock,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = dock == DockStyle.Top ? new Padding(6, 6, 52, 6) : new Padding(6),
        WrapContents = true,
        BackColor = Color.White,
        // a barra superior é de pesquisa/filtros: o que se digita ali não é alteração de dados (ver Alteracoes)
        Tag = dock == DockStyle.Top ? Alteracoes.MarcaFiltros : null,
    };

    /// <summary>Campo numérico: não aceita letras (ver Validacao.SoNumeros).</summary>
    public static NumericUpDown Numero(int casas = 2, decimal maximo = 999_999_999m, decimal minimo = 0) => Validacao.SoNumeros(new()
    {
        DecimalPlaces = casas,
        Maximum = maximo,
        Minimum = minimo,
        ThousandsSeparator = true,
        Width = 110,
        TextAlign = HorizontalAlignment.Right,
        Margin = new Padding(4, 7, 4, 4),
    });

    /// <summary>
    /// Campo de data: só aceita datas plausíveis (Datas.Minima a Datas.Maxima), nada de 0201 ou 2099 por engano.
    /// Um registro antigo com data fora da faixa ainda abre (a faixa se estende só até ela).
    /// </summary>
    public static DateTimePicker Data(DateTime? valor = null)
    {
        var v = valor ?? DateTime.Today;
        return new()
        {
            Format = DateTimePickerFormat.Short,
            Width = 120,
            MinDate = v < Datas.Minima ? v : Datas.Minima,
            MaxDate = v > Datas.Maxima ? v : Datas.Maxima,
            Value = v,
            Margin = new Padding(4, 7, 4, 4),
        };
    }

    public static ComboBox Combo(int largura = 220) => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = largura,
        Margin = new Padding(4, 7, 4, 4),
    };

    /// <summary>Liga o combo a uma consulta "SELECT id, nome ...". itemVazio adiciona uma opção sem valor no topo.</summary>
    public static void Preencher(ComboBox cb, DataTable dt, string? itemVazio = null)
    {
        if (itemVazio != null)
        {
            var r = dt.NewRow();
            r[1] = itemVazio;
            dt.Rows.InsertAt(r, 0);
        }
        cb.DisplayMember = dt.Columns[1].ColumnName;
        cb.ValueMember = dt.Columns[0].ColumnName;
        cb.DataSource = dt;
    }

    public static long? Id(ComboBox cb) => cb.SelectedValue switch
    {
        long l => l,
        int i => i,
        _ => null,
    };

    public static void Selecionar(ComboBox cb, object? id)
    {
        if (id is null or DBNull)
        {
            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
        }
        else
            cb.SelectedValue = Convert.ToInt64(id);
    }

    public static DataRowView? Linha(DataGridView g) =>
        g.SelectedRows.Count > 0 ? g.SelectedRows[0].DataBoundItem as DataRowView : null;

    public static long? IdLinha(DataGridView g) =>
        Linha(g) is DataRowView r && r.Row.Table.Columns.Contains("id") && r["id"] is not DBNull
            ? Convert.ToInt64(r["id"])
            : null;

    /// <summary>Filtra as linhas da grade procurando o texto em todas as colunas de texto.</summary>
    public static void Filtrar(DataGridView g, string texto)
    {
        if (g.DataSource is not DataTable dt)
            return;
        var t = new string(texto.Where(ch => ch is not ('[' or ']' or '*' or '%')).ToArray()).Trim().Replace("'", "''");
        dt.DefaultView.RowFilter = t.Length == 0
            ? ""
            : string.Join(" OR ", dt.Columns.Cast<DataColumn>()
                .Where(c => c.DataType == typeof(string))
                .Select(c => $"[{c.ColumnName}] LIKE '%{t}%'"));
    }

    public static decimal Dec(object? v) => v is null or DBNull ? 0m : Convert.ToDecimal(v);
    public static string Txt(object? v) => v is null or DBNull ? "" : Convert.ToString(v) ?? "";
    public static string Moeda(decimal v) => v.ToString("C2");

    /// <summary>
    /// Para testes automatizados: quando definido, Erro/Info/Confirmar não abrem caixa de mensagem (que travaria o teste).
    /// Recebe (tipo: ERRO, INFO ou CONFIRMAR; texto) e devolve a resposta usada por Confirmar.
    /// </summary>
    public static Func<string, string, bool>? Interceptar { get; set; }

    public static void Erro(string m)
    {
        ErrosExibidos++;
        Log.Registrar(Log.Erro, "Aviso exibido ao usuário", m);
        if (Interceptar is { } i)
            i("ERRO", m);
        else
            MessageBox.Show(m, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void Info(string m)
    {
        Log.Registrar(Log.Acao, "Mensagem exibida", m);
        if (Interceptar is { } i)
            i("INFO", m);
        else
            MessageBox.Show(m, "ERP Loja", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Confirmação rápida e não bloqueante (aviso no canto da tela).</summary>
    public static void Feito(string m)
    {
        Log.Registrar(Log.Acao, "Concluído", m);
        Toast.Mostrar(m, TipoAviso.Sucesso);
    }

    public static void Aviso(string m)
    {
        Log.Registrar(Log.Acao, "Aviso", m);
        Toast.Mostrar(m, TipoAviso.Atencao);
    }

    public static bool Confirmar(string m)
    {
        var sim = Interceptar is { } i ? i("CONFIRMAR", m) : MessageBox.Show(m, "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        Log.Registrar(Log.Acao, sim ? "Confirmou" : "Não confirmou", m);
        return sim;
    }

    public static void ExportarCsv(DataGridView g, string nome)
    {
        if (g.DataSource is not DataTable dt || dt.DefaultView.Count == 0)
        {
            Erro("Não há dados para exportar.");
            return;
        }
        using var dlg = new SaveFileDialog
        {
            Filter = "Planilha CSV (*.csv)|*.csv",
            FileName = $"{nome}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
        };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        var colunas = dt.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id" && !c.ColumnName.StartsWith(PrefixoOrdem)).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", colunas.Select(c => Csv(c.ColumnName))));
        foreach (DataRowView r in dt.DefaultView)
            sb.AppendLine(string.Join(";", colunas.Select(c =>
                Csv(r[c.ColumnName] is double d ? d.ToString("0.00") : SemFormula(Txt(r[c.ColumnName]))))));
        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        Log.Registrar(Log.Acao, "Exportou CSV", $"{dt.DefaultView.Count} linha(s) → {dlg.FileName}");
        Info("Arquivo exportado:\n" + dlg.FileName);
    }

    /// <summary>
    /// OWASP A05 (CSV/formula injection): texto que começa com = + - @ ou tabulação seria executado como fórmula pelo
    /// Excel; recebe um apóstrofo na frente para ser exibido como texto.
    /// </summary>
    public static string SemFormula(string s) => s.Length > 0 && "=+-@\t\r".Contains(s[0]) ? "'" + s : s;

    static string Csv(string s) =>
        s.Contains(';') || s.Contains('"') || s.Contains('\n') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
