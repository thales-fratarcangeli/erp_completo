namespace ErpLoja.Forms;

public class BuscaProdutoForm : Form
{
    readonly TextBox busca = new() { Width = 350, Margin = new Padding(4, 7, 4, 4) };
    readonly DataGridView grid = Ui.Grid();
    long? selecionado;

    BuscaProdutoForm(string termo)
    {
        Ui.Estilizar(this, "Buscar produto", 900, 520, nameof(BuscaProdutoForm));
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        busca.Text = termo;

        var barra = Ui.Barra();
        barra.Controls.AddRange([
            Ui.Rotulo("Nome, código ou código de barras:"), busca,
            Ui.Botao("Selecionar (Enter)", (_, _) => Confirmar(), primario: true),
        ]);
        Controls.Add(grid);
        Controls.Add(barra);

        busca.TextChanged += (_, _) => Pesquisar();
        busca.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down) { grid.Focus(); e.Handled = true; }
        };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Confirmar(); };
        Shown += (_, _) =>
        {
            Pesquisar();
            busca.Focus();
            busca.SelectionStart = busca.Text.Length;
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter) { Confirmar(); return true; }
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void Pesquisar()
    {
        grid.DataSource = Db.Query("""
            SELECT p.id, p.codigo AS "Código", p.codigo_barras AS "Cód. barras", p.nome AS "Produto",
                   c.nome AS "Categoria", p.unidade AS "Un.", p.estoque AS "Estoque", p.preco_venda AS "Preço"
            FROM produtos p LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE p.ativo = 1 AND (p.nome LIKE @p0 OR p.codigo LIKE @p0 OR p.codigo_barras LIKE @p0)
            ORDER BY p.nome LIMIT 300
            """, "%" + busca.Text.Trim() + "%");
    }

    void Confirmar()
    {
        var linha = Ui.Linha(grid) ?? grid.CurrentRow?.DataBoundItem as DataRowView;
        if (linha is null)
            return;
        selecionado = Convert.ToInt64(linha["id"]);
        DialogResult = DialogResult.OK;
    }

    /// <summary>Código/código de barras exato; se não achar (ou vazio), abre a busca.</summary>
    public static long? Localizar(IWin32Window dono, string termo)
    {
        termo = termo.Trim();
        if (termo.Length > 0 &&
            Db.Scalar("SELECT id FROM produtos WHERE ativo = 1 AND (codigo = @p0 OR codigo_barras = @p0) LIMIT 1", termo) is long id)
            return id;
        return Escolher(dono, termo);
    }

    public static long? Escolher(IWin32Window dono, string termo)
    {
        using var f = new BuscaProdutoForm(termo);
        return f.ShowDialog(dono) == DialogResult.OK ? f.selecionado : null;
    }
}
