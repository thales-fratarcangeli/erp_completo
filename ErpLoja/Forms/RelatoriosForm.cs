namespace ErpLoja.Forms;

/// <summary>Relatórios gerenciais. @p0/@p1 = período (yyyy-MM-dd).</summary>
public class RelatoriosForm : Form
{
    record Relatorio(string Nome, bool UsaPeriodo, string[] Somar, string Sql)
    {
        public override string ToString() => Nome;
    }

    static readonly Relatorio[] Relatorios =
    [
        new("Vendas por dia", true, ["Vendas", "Bruto", "Descontos", "Total"], """
            SELECT strftime('%d/%m/%Y', data) AS "Dia", COUNT(*) AS "Vendas", SUM(subtotal) AS "Bruto",
                   SUM(desconto) AS "Descontos", SUM(total) AS "Total", ROUND(SUM(total) / COUNT(*), 2) AS "Ticket médio"
            FROM vendas WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1
            GROUP BY date(data) ORDER BY date(data)
            """),
        new("Produtos mais vendidos", true, ["Quantidade", "Faturamento", "Custo", "Lucro bruto"], """
            SELECT p.codigo AS "Código", p.nome AS "Produto", SUM(i.quantidade) AS "Quantidade",
                   SUM(i.total) AS "Faturamento", SUM(i.quantidade * i.custo_unitario) AS "Custo",
                   SUM(i.total - i.quantidade * i.custo_unitario) AS "Lucro bruto"
            FROM venda_itens i JOIN vendas v ON v.id = i.venda_id JOIN produtos p ON p.id = i.produto_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            GROUP BY p.id ORDER BY 4 DESC
            """),
        new("Vendas por cliente", true, ["Vendas", "Total"], """
            SELECT COALESCE(c.nome, 'Consumidor final') AS "Cliente", COUNT(*) AS "Vendas", SUM(v.total) AS "Total",
                   strftime('%d/%m/%Y', MAX(v.data)) AS "Última compra"
            FROM vendas v LEFT JOIN pessoas c ON c.id = v.cliente_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            GROUP BY v.cliente_id ORDER BY 3 DESC
            """),
        new("Vendas por usuário (vendedor)", true, ["Vendas", "Total"], """
            SELECT COALESCE(u.nome, '-') AS "Usuário", COUNT(*) AS "Vendas", SUM(v.total) AS "Total"
            FROM vendas v LEFT JOIN usuarios u ON u.id = v.usuario_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            GROUP BY v.usuario_id ORDER BY 3 DESC
            """),
        new("Lucro bruto (resumo do período)", true, [], """
            WITH f AS (SELECT COUNT(*) AS n, COALESCE(SUM(total), 0) AS faturamento, COALESCE(SUM(desconto), 0) AS descontos
                       FROM vendas WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1),
                 c AS (SELECT COALESCE(SUM(i.quantidade * i.custo_unitario), 0) AS custo
                       FROM venda_itens i JOIN vendas v ON v.id = i.venda_id
                       WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1)
            SELECT f.n AS "Vendas", f.faturamento AS "Faturamento líquido", f.descontos AS "Descontos concedidos",
                   c.custo AS "Custo das mercadorias", f.faturamento - c.custo AS "Lucro bruto",
                   CASE WHEN f.faturamento > 0 THEN ROUND((f.faturamento - c.custo) * 100.0 / f.faturamento, 2) END AS "Margem %"
            FROM f, c
            """),
        new("Fluxo de caixa (realizado)", true, ["Entradas", "Saídas", "Saldo do dia"], """
            SELECT strftime('%d/%m/%Y', data_pagamento) AS "Dia",
                   SUM(CASE WHEN tipo = 'RECEBER' THEN valor_pago ELSE 0 END) AS "Entradas",
                   SUM(CASE WHEN tipo = 'PAGAR' THEN valor_pago ELSE 0 END) AS "Saídas",
                   SUM(CASE WHEN tipo = 'RECEBER' THEN valor_pago ELSE -valor_pago END) AS "Saldo do dia"
            FROM contas WHERE status = 'PAGA' AND data_pagamento BETWEEN @p0 AND @p1
            GROUP BY data_pagamento ORDER BY data_pagamento
            """),
        new("Fluxo de caixa (previsto - contas em aberto)", true, ["A receber", "A pagar", "Saldo"], """
            SELECT strftime('%d/%m/%Y', vencimento) AS "Vencimento",
                   SUM(CASE WHEN tipo = 'RECEBER' THEN valor ELSE 0 END) AS "A receber",
                   SUM(CASE WHEN tipo = 'PAGAR' THEN valor ELSE 0 END) AS "A pagar",
                   SUM(CASE WHEN tipo = 'RECEBER' THEN valor ELSE -valor END) AS "Saldo"
            FROM contas WHERE status = 'ABERTA' AND vencimento BETWEEN @p0 AND @p1
            GROUP BY vencimento ORDER BY vencimento
            """),
        new("Produtos sem venda no período", true, ["Estoque"], """
            SELECT p.codigo AS "Código", p.nome AS "Produto", p.estoque AS "Estoque", p.preco_venda AS "Preço venda"
            FROM produtos p
            WHERE p.ativo = 1 AND p.tipo <> 'INSUMO' AND NOT EXISTS (
                SELECT 1 FROM venda_itens i JOIN vendas v ON v.id = i.venda_id
                WHERE i.produto_id = p.id AND v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1)
            ORDER BY p.nome
            """),
        new("Compras por fornecedor", true, ["Compras", "Total"], """
            SELECT f.nome AS "Fornecedor", COUNT(*) AS "Compras", SUM(o.total) AS "Total"
            FROM compras o JOIN pessoas f ON f.id = o.fornecedor_id
            WHERE o.status = 'FINALIZADA' AND date(o.data) BETWEEN @p0 AND @p1
            GROUP BY f.id ORDER BY 3 DESC
            """),
    ];

    readonly ListBox lista = new() { Dock = DockStyle.Left, Width = 300, IntegralHeight = false, BorderStyle = BorderStyle.None };
    readonly DateTimePicker de = Ui.Data(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)), ate = Ui.Data();
    readonly DataGridView grid = Ui.Grid();
    readonly Label totais = Ui.Rotulo("");

    public RelatoriosForm()
    {
        Ui.Estilizar(this, "Relatórios", 1200, 720, nameof(RelatoriosForm));
        lista.Items.AddRange(Relatorios);
        lista.ItemHeight = 26;

        var barra = Ui.Barra();
        barra.Controls.AddRange([
            Ui.Rotulo("Período de:"), de, Ui.Rotulo("até:"), ate,
            Ui.Botao("Gerar", (_, _) => Gerar(), primario: true),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(grid, "relatorio")),
        ]);
        var rodape = Ui.Barra(DockStyle.Bottom);
        totais.Font = Ui.FonteNegrito;
        rodape.Controls.Add(totais);

        Controls.Add(grid);
        Controls.Add(lista);
        Controls.Add(barra);
        Controls.Add(rodape);

        lista.SelectedIndexChanged += (_, _) => Gerar();
        // trocar o período já atualiza o relatório (datas invertidas só avisam no rodapé, sem caixa de mensagem a cada clique)
        de.ValueChanged += (_, _) => PeriodoMudou();
        ate.ValueChanged += (_, _) => PeriodoMudou();
        Load += (_, _) => lista.SelectedIndex = 0;
    }

    void PeriodoMudou()
    {
        if (ate.Value.Date < de.Value.Date)
        {
            grid.DataSource = null;
            totais.Text = "A data final é anterior à inicial: corrija o período.";
            return;
        }
        Gerar();
    }

    void Gerar()
    {
        if (lista.SelectedItem is not Relatorio r)
            return;
        de.Enabled = ate.Enabled = r.UsaPeriodo;
        if (r.UsaPeriodo && ate.Value.Date < de.Value.Date)
        {
            grid.DataSource = null;
            totais.Text = "";
            Ui.Erro($"A data final ({ate.Value:dd/MM/yyyy}) é anterior à inicial ({de.Value:dd/MM/yyyy}). Corrija o período e clique em Gerar.");
            return;
        }
        var dt = Db.Query(r.Sql, Db.Dia(de.Value), Db.Dia(ate.Value));
        grid.DataSource = dt;

        var partes = new List<string> { $"{r.Nome}: {dt.Rows.Count} linha(s)" };
        foreach (var coluna in r.Somar.Where(dt.Columns.Contains))
        {
            var soma = dt.Rows.Cast<DataRow>().Sum(x => Ui.Dec(x[coluna]));
            partes.Add($"{coluna}: {(dt.Columns[coluna]!.DataType == typeof(long) ? soma.ToString("N0") : soma.ToString("N2"))}");
        }
        totais.Text = string.Join("    ", partes);
    }
}
