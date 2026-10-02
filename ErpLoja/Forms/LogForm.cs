namespace ErpLoja.Forms;

/// <summary>Consulta do log de atividades (banco separado *_log.db): quem fez o quê, onde e quando.</summary>
public class LogForm : Form
{
    readonly DataGridView grid = Ui.Grid();
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-7)), ate = Ui.Data();
    readonly ComboBox categoria = Ui.Combo(130), usuario = Ui.Combo(170);
    readonly TextBox texto = new() { Width = 220, PlaceholderText = "ação, tela, tabela, detalhe...", Margin = new Padding(4, 7, 4, 4) };
    readonly TextBox detalhe = new() { Dock = DockStyle.Bottom, Height = 90, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, BorderStyle = BorderStyle.None };
    readonly Label resumo = Ui.Rotulo("");

    public LogForm()
    {
        Ui.Estilizar(this, "Log de atividades", 1250, 720, nameof(LogForm));

        categoria.Items.Add("(todas)");
        categoria.Items.AddRange(Log.Categorias);
        categoria.SelectedIndex = 0;

        var barra = Ui.Barra();
        barra.Controls.AddRange([
            Ui.Rotulo("De:"), de, Ui.Rotulo("até:"), ate,
            Ui.Rotulo("Categoria:"), categoria, Ui.Rotulo("Usuário:"), usuario,
            Ui.Rotulo("Contém:"), texto,
            Ui.Botao("Atualizar", (_, _) => Carregar(), primario: true),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(grid, "log")),
            resumo,
        ]);

        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.CellStyle is null || grid.Columns[e.ColumnIndex].DataPropertyName != "Categoria")
                return;
            e.CellStyle.ForeColor = Ui.Txt(e.Value) switch
            {
                Log.Erro => Ui.Perigo,
                Log.Acesso => Ui.Primaria,
                Log.Dados => Ui.Sucesso,
                Log.Sistema => Color.Gray,
                _ => e.CellStyle.ForeColor,
            };
        };
        grid.SelectionChanged += (_, _) =>
            detalhe.Text = Ui.Linha(grid) is { } r ? $"{r["Data/hora"]} · {r["Usuário"]} · {r["Tela"]}\r\n{r["Ação"]}\r\n{r["Detalhe"]}" : "";

        Controls.Add(grid);
        Controls.Add(detalhe);
        Controls.Add(barra);

        texto.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Carregar();
            }
        };
        Load += (_, _) =>
        {
            usuario.Items.Add("(todos)");
            usuario.Items.AddRange(Log.Usuarios());
            usuario.SelectedIndex = 0;
            Carregar();
        };
    }

    void Carregar()
    {
        if (ate.Value.Date < de.Value.Date)
        {
            Ui.Erro($"A data final ({ate.Value:dd/MM/yyyy}) é anterior à inicial ({de.Value:dd/MM/yyyy}). Corrija o período e clique em Atualizar.");
            return;
        }
        var dt = Log.Consultar(de.Value, ate.Value,
            categoria.SelectedIndex > 0 ? categoria.Text : null,
            usuario.SelectedIndex > 0 ? usuario.Text : null,
            texto.Text);
        grid.DataSource = dt;
        if (grid.Columns["Detalhe"] is { } col)
            col.FillWeight = 260;
        resumo.Text = dt.Rows.Count >= 5000 ? "5.000 registros mais recentes (refine o filtro)" : $"{dt.Rows.Count} registro(s)";
    }
}
