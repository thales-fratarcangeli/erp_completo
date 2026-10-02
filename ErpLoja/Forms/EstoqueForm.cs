namespace ErpLoja.Forms;

/// <summary>Posição de estoque, histórico de movimentações e ajuste manual (inventário).</summary>
public class EstoqueForm : Form
{
    readonly DataGridView produtos = Ui.Grid(), movimentos = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly TextBox busca = new() { Width = 240, PlaceholderText = "Digite para filtrar...", Margin = new Padding(4, 7, 4, 4) };
    readonly CheckBox somenteBaixo = new() { Text = "Somente estoque baixo", AutoSize = true, Margin = new Padding(10, 10, 4, 4) };
    readonly Label resumo = Ui.Rotulo("");

    public EstoqueForm()
    {
        Ui.Estilizar(this, "Estoque", 1150, 720);

        var barra = Ui.Barra();
        barra.Controls.AddRange([
            Ui.Rotulo("Pesquisar:"), busca, somenteBaixo,
            Ui.Botao("Atualizar", (_, _) => Carregar()),
            Ui.Botao("Ajustar estoque", (_, _) => Ajustar(), primario: true),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(produtos, "estoque")),
            resumo,
        ]);

        split.Panel1.Controls.Add(produtos);
        split.Panel2.Controls.Add(movimentos);
        split.Panel2.Controls.Add(new Label { Text = "Movimentações do produto selecionado", Dock = DockStyle.Top, Height = 28, Font = Ui.FonteNegrito, Padding = new Padding(6, 6, 0, 0) });
        movimentos.BringToFront();
        Ui.ColorirSituacao(produtos);

        Controls.Add(split);
        Controls.Add(barra);

        busca.TextChanged += (_, _) => Ui.Filtrar(produtos, busca.Text);
        somenteBaixo.CheckedChanged += (_, _) => Carregar();
        produtos.SelectionChanged += (_, _) => CarregarMovimentos();
        Load += (_, _) =>
        {
            split.SplitterDistance = split.Height * 60 / 100;
            Carregar();
        };
    }

    void Carregar()
    {
        var selecionado = Ui.IdLinha(produtos);
        var dt = Db.Query("""
            SELECT p.id, p.codigo AS "Código", p.nome AS "Produto", c.nome AS "Categoria", p.unidade AS "Un.",
                   p.estoque AS "Estoque", p.estoque_minimo AS "Mínimo", p.preco_custo AS "Custo médio",
                   p.preco_venda AS "Preço venda", ROUND(MAX(p.estoque, 0) * p.preco_custo, 2) AS "Valor em estoque",
                   MAX(p.estoque_minimo - p.estoque, 0) AS "Sugestão de compra",
                   CASE WHEN p.estoque <= 0 THEN 'SEM ESTOQUE' WHEN p.estoque <= p.estoque_minimo THEN 'BAIXO' ELSE 'OK' END AS "Situação"
            FROM produtos p LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE p.ativo = 1 AND (@p0 = 0 OR p.estoque <= p.estoque_minimo)
            ORDER BY p.nome
            """, somenteBaixo.Checked ? 1 : 0);
        produtos.DataSource = dt;
        Ui.Filtrar(produtos, busca.Text);

        var valor = dt.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["Valor em estoque"]));
        resumo.Text = $"{dt.Rows.Count} produto(s) · Valor do estoque (custo): {Ui.Moeda(valor)}";

        if (selecionado != null)
            foreach (DataGridViewRow r in produtos.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionado)
                {
                    produtos.ClearSelection();
                    r.Selected = true;
                    break;
                }
        CarregarMovimentos();
    }

    void CarregarMovimentos()
    {
        if (Ui.IdLinha(produtos) is not long id)
        {
            movimentos.DataSource = null;
            return;
        }
        movimentos.DataSource = Db.Query("""
            SELECT m.id, strftime('%d/%m/%Y %H:%M', m.data) AS "Data", m.tipo AS "Tipo", m.quantidade AS "Quantidade",
                   m.saldo AS "Saldo após", m.origem AS "Origem", u.nome AS "Usuário"
            FROM movimentacoes_estoque m LEFT JOIN usuarios u ON u.id = m.usuario_id
            WHERE m.produto_id = @p0 ORDER BY m.id DESC LIMIT 500
            """, id);
    }

    void Ajustar()
    {
        if (Ui.Linha(produtos) is not DataRowView linha)
        {
            Ui.Erro("Selecione um produto.");
            return;
        }
        var id = Convert.ToInt64(linha["id"]);
        var atual = Ui.Dec(linha["Estoque"]);

        using var d = new Dialogo("Ajustar estoque", "ajuste_estoque", "Aplicar");
        d.Nota($"{linha["Produto"]} — saldo atual: {atual:N3} {linha["Un."]}");
        var tipo = d.Adicionar("Operação:", Ui.Combo());
        tipo.Items.AddRange(["Entrada", "Saída (perda, quebra, uso)", "Definir saldo (inventário)"]);
        tipo.SelectedIndex = 0;
        var quantidade = d.Adicionar("Quantidade:", Ui.Numero(3));
        var motivo = d.Adicionar("Motivo:", new TextBox());
        d.Validar = () =>
        {
            if (tipo.SelectedIndex < 2 && quantidade.Value <= 0) { Ui.Erro("Informe a quantidade."); return false; }
            if (string.IsNullOrWhiteSpace(motivo.Text)) { Ui.Erro("Informe o motivo do ajuste."); return false; }
            return true;
        };
        if (d.ShowDialog(this) != DialogResult.OK)
            return;

        var (tipoMov, delta) = tipo.SelectedIndex switch
        {
            0 => ("ENTRADA", quantidade.Value),
            1 => ("SAIDA", -quantidade.Value),
            _ => ("AJUSTE", quantidade.Value - atual),
        };
        if (delta == 0)
        {
            Ui.Info("O saldo informado é igual ao atual. Nada foi alterado.");
            return;
        }
        Operacoes.AjustarEstoque(id, tipoMov, delta, motivo.Text.Trim());
        Carregar();
    }
}
