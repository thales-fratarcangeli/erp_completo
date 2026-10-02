using System.Globalization;

namespace ErpLoja.Forms;

/// <summary>Painel gerencial da tela inicial: indicadores com comparação ao período anterior e gráficos.</summary>
public class DashboardPainel : UserControl
{
    static readonly Color TextoBom = ColorTranslator.FromHtml("#006300");
    static readonly Color TextoRuim = ColorTranslator.FromHtml("#d03b3b");

    readonly ComboBox periodo = Ui.Combo(200);
    readonly Label legendaPeriodo = Ui.Rotulo("");
    readonly TableLayoutPanel indicadores = new() { Dock = DockStyle.Top, Height = 118, ColumnCount = 6, RowCount = 1, Padding = new Padding(8, 8, 8, 0) };
    readonly Dictionary<string, (Label Valor, Label Variacao)> cards = new();
    readonly Grafico faturamento = new() { Dock = DockStyle.Fill, Titulo = "Faturamento" };
    readonly Grafico produtos = new() { Dock = DockStyle.Fill, Titulo = "Top 10 produtos", Subtitulo = "Faturamento no período", Horizontal = true };
    readonly Grafico formas = new() { Dock = DockStyle.Fill, Titulo = "Formas de pagamento", Subtitulo = "Faturamento no período", Horizontal = true };
    readonly Grafico caixa = new() { Dock = DockStyle.Fill, Titulo = "Caixa realizado: entradas x saídas" };
    readonly Grafico categorias = new() { Dock = DockStyle.Fill, Titulo = "Vendas por categoria", Subtitulo = "Faturamento no período", Horizontal = true };
    readonly Grafico horas = new() { Dock = DockStyle.Fill, Titulo = "Movimento por hora do dia", Subtitulo = "Quantidade de vendas" };

    public DashboardPainel()
    {
        Font = Ui.Fonte;
        BackColor = Ui.Fundo;

        periodo.Items.AddRange(["Hoje", "Últimos 7 dias", "Últimos 30 dias", "Este mês", "Mês anterior",
            "Últimos 90 dias", "Este ano", "Últimos 12 meses"]);
        var barra = Ui.Barra();
        barra.Controls.AddRange([Ui.Rotulo("Período:"), periodo, Ui.Botao("Atualizar", (_, _) => Atualizar()), legendaPeriodo]);

        for (int i = 0; i < 6; i++)
            indicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
        Card("fat", "Faturamento");
        Card("lucro", "Lucro bruto");
        Card("margem", "Margem bruta");
        Card("vendas", "Vendas");
        Card("ticket", "Ticket médio");
        Card("itens", "Itens vendidos");

        var graficos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(8) };
        graficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        graficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        graficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        graficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        graficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        graficos.Controls.AddRange([faturamento, produtos, formas, caixa, categorias, horas]);
        Func<double, string> moeda = v => v.ToString("C0");
        faturamento.Formatar = produtos.Formatar = formas.Formatar = caixa.Formatar = categorias.Formatar = moeda;

        Controls.Add(graficos);
        Controls.Add(indicadores);
        Controls.Add(barra);

        periodo.SelectedIndexChanged += (_, _) => Atualizar();
        Load += (_, _) => periodo.SelectedIndex = 2;
    }

    void Card(string chave, string rotulo)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Grafico.Superficie, Margin = new Padding(6), Padding = new Padding(14, 8, 10, 6) };
        var valor = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F), TextAlign = ContentAlignment.MiddleLeft };
        var variacao = new Label { Dock = DockStyle.Bottom, Height = 22, ForeColor = Color.DimGray, AutoEllipsis = true };
        p.Controls.Add(valor);
        p.Controls.Add(new Label { Dock = DockStyle.Top, Height = 22, Text = rotulo, ForeColor = ColorTranslator.FromHtml("#52514e") });
        p.Controls.Add(variacao);
        indicadores.Controls.Add(p);
        cards[chave] = (valor, variacao);
    }

    (DateTime De, DateTime Ate) Intervalo()
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        return periodo.SelectedIndex switch
        {
            0 => (hoje, hoje),
            1 => (hoje.AddDays(-6), hoje),
            2 => (hoje.AddDays(-29), hoje),
            3 => (inicioMes, hoje),
            4 => (inicioMes.AddMonths(-1), inicioMes.AddDays(-1)),
            5 => (hoje.AddDays(-89), hoje),
            6 => (new DateTime(hoje.Year, 1, 1), hoje),
            _ => (inicioMes.AddMonths(-11), hoje),
        };
    }

    record Resumo(decimal Faturamento, decimal Custo, long Vendas, decimal Itens)
    {
        public decimal Lucro => Faturamento - Custo;
        public decimal? Margem => Faturamento > 0 ? Lucro / Faturamento * 100 : null;
        public decimal Ticket => Vendas > 0 ? Faturamento / Vendas : 0;
    }

    static Resumo Calcular(DateTime de, DateTime ate)
    {
        var v = Db.Query("""
            SELECT COUNT(*) AS n, COALESCE(SUM(total), 0) AS fat FROM vendas
            WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1
            """, Db.Dia(de), Db.Dia(ate)).Rows[0];
        var i = Db.Query("""
            SELECT COALESCE(SUM(i.quantidade * i.custo_unitario), 0) AS custo, COALESCE(SUM(i.quantidade), 0) AS itens
            FROM venda_itens i JOIN vendas v ON v.id = i.venda_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            """, Db.Dia(de), Db.Dia(ate)).Rows[0];
        return new Resumo(Ui.Dec(v["fat"]), Ui.Dec(i["custo"]), Convert.ToInt64(v["n"]), Ui.Dec(i["itens"]));
    }

    public void Atualizar()
    {
        if (periodo.SelectedIndex < 0)
            return;
        try
        {
            Cursor = Cursors.WaitCursor;
            var (de, ate) = Intervalo();
            var dias = (ate - de).Days + 1;
            var (deAnt, ateAnt) = (de.AddDays(-dias), de.AddDays(-1));
            legendaPeriodo.Text = $"{de:dd/MM/yyyy} a {ate:dd/MM/yyyy}  ·  comparação com {deAnt:dd/MM/yyyy} a {ateAnt:dd/MM/yyyy}";

            var atual = Calcular(de, ate);
            var anterior = Calcular(deAnt, ateAnt);
            Mostrar("fat", Ui.Moeda(atual.Faturamento), atual.Faturamento, anterior.Faturamento);
            Mostrar("lucro", Ui.Moeda(atual.Lucro), atual.Lucro, anterior.Lucro);
            Mostrar("margem", atual.Margem is decimal m ? $"{m:N1}%" : "-", atual.Margem, anterior.Margem, pontos: true);
            Mostrar("vendas", atual.Vendas.ToString("N0"), atual.Vendas, anterior.Vendas);
            Mostrar("ticket", Ui.Moeda(atual.Ticket), atual.Ticket, anterior.Ticket);
            Mostrar("itens", atual.Itens.ToString("N0"), atual.Itens, anterior.Itens);

            AtualizarGraficos(de, ate, dias);
        }
        catch (Exception ex)
        {
            Ui.Erro("Erro ao montar o dashboard: " + ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    void Mostrar(string chave, string valor, decimal? atual, decimal? anterior, bool pontos = false)
    {
        var (lValor, lVariacao) = cards[chave];
        lValor.Text = valor;
        if (atual is null || anterior is null || (!pontos && anterior == 0))
        {
            lVariacao.Text = anterior == 0m && atual > 0 ? "▲ (antes: zero)" : "sem comparação";
            lVariacao.ForeColor = anterior == 0m && atual > 0 ? TextoBom : Color.DimGray;
            return;
        }
        var delta = pontos ? atual.Value - anterior.Value : (atual.Value - anterior.Value) / Math.Abs(anterior.Value) * 100;
        var sufixo = pontos ? " p.p." : "%";
        lVariacao.Text = delta == 0 ? "= igual ao período anterior"
            : $"{(delta > 0 ? "▲" : "▼")} {Math.Abs(delta):N1}{sufixo} vs anterior";
        lVariacao.ForeColor = delta > 0 ? TextoBom : delta < 0 ? TextoRuim : Color.DimGray;
    }

    void AtualizarGraficos(DateTime de, DateTime ate, int dias)
    {
        var porMes = dias > 62;
        var formato = porMes ? "%Y-%m" : "%Y-%m-%d";
        var chaves = new List<DateTime>();
        for (var d = porMes ? new DateTime(de.Year, de.Month, 1) : de; d <= ate; d = porMes ? d.AddMonths(1) : d.AddDays(1))
            chaves.Add(d);
        string Chave(DateTime d) => d.ToString(porMes ? "yyyy-MM" : "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var rotulos = chaves.Select(d => porMes ? d.ToString("MMM/yy") : d.ToString("dd/MM")).ToArray();
        var p = new object[] { Db.Dia(de), Db.Dia(ate), formato };

        Dictionary<string, double> Mapa(string sql) => Db.Query(sql, p).Rows.Cast<DataRow>()
            .ToDictionary(r => Ui.Txt(r[0]), r => Convert.ToDouble(r[1]));

        // faturamento por dia/mês
        var fat = Mapa("""
            SELECT strftime(@p2, data) AS k, SUM(total) FROM vendas
            WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1 GROUP BY k
            """);
        faturamento.Subtitulo = porMes ? "Por mês" : "Por dia";
        faturamento.Definir(rotulos, new Serie("Faturamento", Grafico.Cor1, chaves.Select(d => fat.GetValueOrDefault(Chave(d))).ToArray()));

        // caixa realizado
        var entradas = Mapa("""
            SELECT strftime(@p2, data_pagamento) AS k, SUM(valor_pago) FROM contas
            WHERE status = 'PAGA' AND tipo = 'RECEBER' AND data_pagamento BETWEEN @p0 AND @p1 GROUP BY k
            """);
        var saidas = Mapa("""
            SELECT strftime(@p2, data_pagamento) AS k, SUM(valor_pago) FROM contas
            WHERE status = 'PAGA' AND tipo = 'PAGAR' AND data_pagamento BETWEEN @p0 AND @p1 GROUP BY k
            """);
        caixa.Subtitulo = $"Entradas {Ui.Moeda((decimal)entradas.Values.Sum())} · Saídas {Ui.Moeda((decimal)saidas.Values.Sum())}";
        caixa.Definir(rotulos,
            new Serie("Entradas", Grafico.Cor1, chaves.Select(d => entradas.GetValueOrDefault(Chave(d))).ToArray()),
            new Serie("Saídas", Grafico.Cor2, chaves.Select(d => saidas.GetValueOrDefault(Chave(d))).ToArray()));

        Ranking(produtos, """
            SELECT p.nome, SUM(i.total) AS t FROM venda_itens i JOIN vendas v ON v.id = i.venda_id JOIN produtos p ON p.id = i.produto_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            GROUP BY p.id ORDER BY t DESC LIMIT 10
            """, p);
        Ranking(formas, """
            SELECT forma_pagamento, SUM(total) AS t FROM vendas
            WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1
            GROUP BY forma_pagamento ORDER BY t DESC
            """, p);
        Ranking(categorias, """
            SELECT COALESCE(c.nome, '(sem categoria)'), SUM(i.total) AS t
            FROM venda_itens i JOIN vendas v ON v.id = i.venda_id JOIN produtos p ON p.id = i.produto_id
            LEFT JOIN categorias c ON c.id = p.categoria_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) BETWEEN @p0 AND @p1
            GROUP BY c.id ORDER BY t DESC LIMIT 8
            """, p);

        var porHora = Db.Query("""
            SELECT CAST(strftime('%H', data) AS INTEGER) AS h, COUNT(*) FROM vendas
            WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1 GROUP BY h
            """, p).Rows.Cast<DataRow>().ToDictionary(r => Convert.ToInt32(r[0]), r => Convert.ToDouble(r[1]));
        int primeira = Math.Min(8, porHora.Keys.DefaultIfEmpty(8).Min()), ultima = Math.Max(20, porHora.Keys.DefaultIfEmpty(20).Max());
        var faixaHoras = Enumerable.Range(primeira, ultima - primeira + 1).ToArray();
        horas.Definir(faixaHoras.Select(h => $"{h}h").ToArray(),
            new Serie("Vendas", Grafico.Cor1, faixaHoras.Select(h => porHora.GetValueOrDefault(h)).ToArray()));
    }

    static void Ranking(Grafico grafico, string sql, object[] p)
    {
        var dt = Db.Query(sql, p);
        grafico.Definir(dt.Rows.Cast<DataRow>().Select(r => Ui.Txt(r[0])).ToArray(),
            new Serie(grafico.Titulo, Grafico.Cor1, dt.Rows.Cast<DataRow>().Select(r => Convert.ToDouble(r[1])).ToArray()));
    }
}
