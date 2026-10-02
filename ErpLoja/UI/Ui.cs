using System.Text;

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
        DestacarLinhaSobMouse(g);
        MostrarEstadoVazio(g);
        return g;
    }

    /// <summary>Texto mostrado pela grade quando não há registros (Tag da grade pode trazer uma dica específica).</summary>
    public const string DicaGradeVazia = "Ajuste a pesquisa ou os filtros, ou use os botões acima para incluir.";

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
                "VENCIDA" or "VENCIDO" or "SEM ESTOQUE" or "RECUSADO" or "REJEITADO" or "PENDENTE" => Perigo,
                "BAIXO" or "DIGITADA" or "EM PRODUÇÃO" or "EM TRÂNSITO" or "ENVIADO" or "DECLARADO" => Alerta,
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
            b.Click += clique;
        return b;
    }

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
    };

    public static NumericUpDown Numero(int casas = 2, decimal maximo = 999_999_999m, decimal minimo = 0) => new()
    {
        DecimalPlaces = casas,
        Maximum = maximo,
        Minimum = minimo,
        ThousandsSeparator = true,
        Width = 110,
        TextAlign = HorizontalAlignment.Right,
        Margin = new Padding(4, 7, 4, 4),
    };

    public static DateTimePicker Data(DateTime? valor = null) => new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 120,
        Value = valor ?? DateTime.Today,
        Margin = new Padding(4, 7, 4, 4),
    };

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

        var colunas = dt.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "id").ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", colunas.Select(c => Csv(c.ColumnName))));
        foreach (DataRowView r in dt.DefaultView)
            sb.AppendLine(string.Join(";", colunas.Select(c =>
                Csv(r[c.ColumnName] is double d ? d.ToString("0.00") : Txt(r[c.ColumnName])))));
        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        Log.Registrar(Log.Acao, "Exportou CSV", $"{dt.DefaultView.Count} linha(s) → {dlg.FileName}");
        Info("Arquivo exportado:\n" + dlg.FileName);
    }

    static string Csv(string s) =>
        s.Contains(';') || s.Contains('"') || s.Contains('\n') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
