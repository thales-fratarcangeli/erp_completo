using Microsoft.Data.Sqlite;

namespace ErpLoja.Forms;

public enum TipoCampo { Texto, MultiLinha, Inteiro, Decimal, Booleano, Lookup, Senha }

/// <summary>Campo editável de um cadastro. Lookup = SQL "SELECT id, nome ..." para um combo.</summary>
public record Campo(string Coluna, string Rotulo, TipoCampo Tipo = TipoCampo.Texto, bool Obrigatorio = false,
    string? Lookup = null, object? Padrao = null, Func<string, string?>? Validacao = null);

/// <summary>Tela genérica de cadastro: lista com pesquisa à esquerda, edição à direita.</summary>
public class CrudForm : Form
{
    readonly string tabela, sqlLista;
    readonly Campo[] campos;
    readonly Dictionary<Campo, Control> editores = new();
    readonly DataGridView grid = Ui.Grid();
    readonly FlowLayoutPanel painel;
    readonly TextBox busca = new() { Width = 280, PlaceholderText = "Digite para filtrar...", Margin = new Padding(4, 7, 4, 4) };
    readonly Label contador = Ui.Rotulo("");
    readonly Label modo = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), Margin = new Padding(0, 0, 0, 10) };
    long? idAtual;
    bool carregando;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<long, string?>? ValidarExclusao { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Dica { get; init; }

    public CrudForm(string titulo, string tabela, string sqlLista, params Campo[] campos)
    {
        this.tabela = tabela;
        this.sqlLista = sqlLista;
        this.campos = campos;
        Ui.Estilizar(this, titulo, 1150, 680, "cadastro_" + tabela);

        painel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 400,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(14),
            BackColor = Color.White,
        };
        painel.Controls.Add(modo);

        foreach (var c in campos)
        {
            Control ed = c.Tipo switch
            {
                TipoCampo.MultiLinha => new TextBox { Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical, Width = 350 },
                TipoCampo.Senha => new TextBox { UseSystemPasswordChar = true, Width = 350 },
                TipoCampo.Inteiro => Ui.Numero(0),
                TipoCampo.Decimal => Ui.Numero(2),
                TipoCampo.Booleano => new CheckBox { Text = c.Rotulo, AutoSize = true },
                TipoCampo.Lookup => Ui.Combo(350),
                _ => new TextBox { Width = 350 },
            };
            if (ed is NumericUpDown)
                ed.Width = 160;
            ed.Margin = new Padding(0, 0, 0, 8);
            if (c.Tipo != TipoCampo.Booleano)
                painel.Controls.Add(new Label { Text = c.Rotulo + (c.Obrigatorio ? " *" : ""), AutoSize = true, Margin = new Padding(0, 4, 0, 2) });
            painel.Controls.Add(ed);
            editores[c] = ed;
        }

        var botoes = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 12, 0, 0) };
        botoes.Controls.AddRange([
            Ui.Botao("Novo", (_, _) => Novo()),
            Ui.Botao("Salvar", (_, _) => Salvar(), primario: true),
            Ui.Botao("Excluir", (_, _) => Excluir()),
        ]);
        painel.Controls.Add(botoes);
        if (campos.Any(c => c.Tipo == TipoCampo.Senha))
            AdicionarNota("Ao editar, deixe a senha em branco para mantê-la.");

        var barra = Ui.Barra();
        barra.Controls.AddRange([
            Ui.Rotulo("Pesquisar:"), busca,
            Ui.Botao("Atualizar", (_, _) => { CarregarLookups(); Recarregar(idAtual); }),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(grid, tabela)),
            contador,
        ]);

        Controls.Add(grid);
        Controls.Add(painel);
        Controls.Add(barra);

        busca.TextChanged += (_, _) => { Ui.Filtrar(grid, busca.Text); AtualizarContador(); };
        grid.SelectionChanged += (_, _) =>
        {
            if (!carregando && Ui.IdLinha(grid) is long id && id != idAtual)
                Carregar(id);
        };
        Load += (_, _) =>
        {
            if (Dica != null)
                AdicionarNota(Dica);
            CarregarLookups();
            Recarregar();
        };
        Shown += (_, _) => Novo();
    }

    void AdicionarNota(string texto) =>
        painel.Controls.Add(new Label { Text = texto, AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(350, 0), Margin = new Padding(0, 10, 0, 0) });

    void CarregarLookups()
    {
        foreach (var (c, ed) in editores)
            if (c.Tipo == TipoCampo.Lookup && ed is ComboBox cb)
            {
                var atual = cb.SelectedValue;
                Ui.Preencher(cb, Db.Query(c.Lookup!), "(nenhum)");
                Ui.Selecionar(cb, atual);
            }
    }

    void Recarregar(long? selecionar = null)
    {
        carregando = true;
        try
        {
            grid.DataSource = Db.Query(sqlLista);
            Ui.Filtrar(grid, busca.Text);
            grid.ClearSelection();
            if (selecionar != null)
                foreach (DataGridViewRow r in grid.Rows)
                    if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                    {
                        r.Selected = true;
                        var celula = r.Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
                        if (celula != null)
                            grid.CurrentCell = celula;
                        break;
                    }
        }
        finally
        {
            carregando = false;
        }
        AtualizarContador();
    }

    void AtualizarContador() => contador.Text = $"{grid.Rows.Count} registro(s)";

    void Carregar(long id)
    {
        var dt = Db.Query($"SELECT * FROM {tabela} WHERE id = @p0", id);
        if (dt.Rows.Count == 0)
            return;
        var r = dt.Rows[0];
        idAtual = id;
        modo.Text = $"Editando registro nº {id}";
        foreach (var (c, ed) in editores)
        {
            var v = r[c.Coluna];
            switch (ed)
            {
                case CheckBox cb: cb.Checked = Ui.Dec(v) == 1; break;
                case NumericUpDown n: n.Value = Math.Clamp(Ui.Dec(v), n.Minimum, n.Maximum); break;
                case ComboBox cb: Ui.Selecionar(cb, v); break;
                case TextBox tb: tb.Text = c.Tipo == TipoCampo.Senha ? "" : Ui.Txt(v); break;
            }
        }
    }

    void Novo()
    {
        carregando = true;
        grid.ClearSelection();
        carregando = false;
        idAtual = null;
        modo.Text = "Novo registro";
        foreach (var (c, ed) in editores)
        {
            switch (ed)
            {
                case CheckBox cb: cb.Checked = c.Padrao is true; break;
                case NumericUpDown n: n.Value = c.Padrao is null ? 0 : Convert.ToDecimal(c.Padrao); break;
                case ComboBox cb: if (cb.Items.Count > 0) cb.SelectedIndex = 0; break;
                case TextBox tb: tb.Text = c.Padrao as string ?? ""; break;
            }
        }
        editores.Values.FirstOrDefault()?.Focus();
    }

    void Salvar()
    {
        var colunas = new List<string>();
        var valores = new List<object?>();
        foreach (var (c, ed) in editores)
        {
            object? v = ed switch
            {
                CheckBox cb => cb.Checked ? 1L : 0L,
                NumericUpDown n => c.Tipo == TipoCampo.Inteiro ? (long)n.Value : n.Value,
                ComboBox cb => Ui.Id(cb),
                TextBox tb => string.IsNullOrWhiteSpace(tb.Text) ? null : tb.Text.Trim(),
                _ => null,
            };

            if (c.Tipo == TipoCampo.Senha)
            {
                if (v is null)
                {
                    if (idAtual is null && c.Obrigatorio)
                    {
                        Ui.Erro($"Informe o campo \"{c.Rotulo}\".");
                        ed.Focus();
                        return;
                    }
                    continue; // mantém a senha atual
                }
                v = Senhas.Hash((string)v);
            }
            else if (c.Obrigatorio && v is null)
            {
                Ui.Erro($"O campo \"{c.Rotulo}\" é obrigatório.");
                ed.Focus();
                return;
            }
            if (v is string texto && c.Validacao?.Invoke(texto) is string erro)
            {
                Ui.Erro(erro);
                ed.Focus();
                return;
            }
            colunas.Add(c.Coluna);
            valores.Add(v);
        }

        try
        {
            if (idAtual is null)
            {
                using var s = new DbSessao();
                idAtual = s.Inserir(
                    $"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", colunas.Select((_, i) => "@p" + i))})",
                    valores.ToArray());
            }
            else
            {
                valores.Add(idAtual);
                Db.Exec($"UPDATE {tabela} SET {string.Join(", ", colunas.Select((c, i) => $"{c} = @p{i}"))} WHERE id = @p{colunas.Count}",
                    valores.ToArray());
            }
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Ui.Erro("Não foi possível salvar: já existe um registro com este valor (ex.: código, username ou e-mail repetido).");
            return;
        }

        var id = idAtual.Value;
        Recarregar(id);
        Carregar(id);
        modo.Text = $"Registro nº {id} salvo";
    }

    void Excluir()
    {
        if (idAtual is not long id)
        {
            Ui.Erro("Selecione um registro na lista.");
            return;
        }
        if (ValidarExclusao?.Invoke(id) is string msg)
        {
            Ui.Erro(msg);
            return;
        }
        if (!Ui.Confirmar("Excluir o registro selecionado?"))
            return;

        try
        {
            Db.Exec($"DELETE FROM {tabela} WHERE id = @p0", id);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            if (!campos.Any(c => c.Coluna == "ativo"))
            {
                Ui.Erro("Este registro está em uso e não pode ser excluído.");
                return;
            }
            if (!Ui.Confirmar("Este registro está em uso (vendas, compras etc.) e não pode ser excluído.\nDeseja inativá-lo?"))
                return;
            Db.Exec($"UPDATE {tabela} SET ativo = 0 WHERE id = @p0", id);
        }
        Recarregar();
        Novo();
    }
}
