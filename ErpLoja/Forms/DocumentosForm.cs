namespace ErpLoja.Forms;

/// <summary>Consulta de vendas ou compras realizadas, com itens e cancelamento.</summary>
public class DocumentosForm : Form
{
    readonly bool venda;
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-30)), ate = Ui.Data();
    readonly DataGridView docs = Ui.Grid(), itens = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly Label resumo = Ui.Rotulo("");

    public DocumentosForm(bool venda)
    {
        this.venda = venda;
        Ui.Estilizar(this, venda ? "Vendas realizadas" : "Compras realizadas", 1150, 720, venda ? "vendas" : "compras");

        var barra = Ui.Barra();
        barra.Controls.AddRange([Ui.Rotulo("De:"), de, Ui.Rotulo("Até:"), ate, Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true)]);
        if (venda)
            barra.Controls.Add(Ui.Botao("Comprovante", (_, _) => { if (Ui.IdLinha(docs) is long id) CupomForm.Mostrar(this, id); }));
        barra.Controls.AddRange([
            Ui.Botao(venda ? "Cancelar venda" : "Cancelar compra", (_, _) => Cancelar()),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(docs, venda ? "vendas" : "compras")),
            resumo,
        ]);

        split.Panel1.Controls.Add(docs);
        split.Panel2.Controls.Add(itens);
        split.Panel2.Controls.Add(new Label { Text = "Itens do documento selecionado", Dock = DockStyle.Top, Height = 28, Font = Ui.FonteNegrito, Padding = new Padding(6, 6, 0, 0) });
        itens.BringToFront();
        Ui.ColorirSituacao(docs);

        Controls.Add(split);
        Controls.Add(barra);

        docs.SelectionChanged += (_, _) => CarregarItens();
        Load += (_, _) =>
        {
            split.SplitterDistance = split.Height * 55 / 100;
            Carregar();
        };
    }

    void Carregar()
    {
        var sql = venda
            ? """
              SELECT v.id, v.id AS "Nº", strftime('%d/%m/%Y %H:%M', v.data) AS "Data", COALESCE(c.nome, 'Consumidor final') AS "Cliente",
                     u.nome AS "Usuário", v.forma_pagamento AS "Pagamento", v.subtotal AS "Subtotal", v.desconto AS "Desconto",
                     v.total AS "Total", v.status AS "Situação"
              FROM vendas v LEFT JOIN pessoas c ON c.id = v.cliente_id LEFT JOIN usuarios u ON u.id = v.usuario_id
              WHERE date(v.data) BETWEEN @p0 AND @p1 ORDER BY v.id DESC
              """
            : """
              SELECT o.id, o.id AS "Nº", strftime('%d/%m/%Y %H:%M', o.data) AS "Data", f.nome AS "Fornecedor",
                     o.numero_nota AS "Nota fiscal", u.nome AS "Usuário", o.forma_pagamento AS "Pagamento",
                     o.total AS "Total", o.status AS "Situação"
              FROM compras o JOIN pessoas f ON f.id = o.fornecedor_id LEFT JOIN usuarios u ON u.id = o.usuario_id
              WHERE date(o.data) BETWEEN @p0 AND @p1 ORDER BY o.id DESC
              """;
        var dt = Db.Query(sql, Db.Dia(de.Value), Db.Dia(ate.Value));
        docs.DataSource = dt;

        var validos = dt.Rows.Cast<DataRow>().Where(r => Ui.Txt(r["Situação"]) == "FINALIZADA").ToList();
        resumo.Text = $"{validos.Count} {(venda ? "venda(s)" : "compra(s)")} · Total {Ui.Moeda(validos.Sum(r => Ui.Dec(r["Total"])))}";
        CarregarItens();
    }

    void CarregarItens()
    {
        if (Ui.IdLinha(docs) is not long id)
        {
            itens.DataSource = null;
            return;
        }
        itens.DataSource = venda
            ? Db.Query("""
                SELECT i.id, p.codigo AS "Código", p.nome AS "Produto", i.quantidade AS "Qtd.", i.preco_unitario AS "Preço unit.",
                       i.total AS "Total", ROUND(i.total - i.quantidade * i.custo_unitario, 2) AS "Lucro bruto"
                FROM venda_itens i JOIN produtos p ON p.id = i.produto_id WHERE i.venda_id = @p0 ORDER BY i.id
                """, id)
            : Db.Query("""
                SELECT i.id, p.codigo AS "Código", p.nome AS "Produto", i.quantidade AS "Qtd.", i.custo_unitario AS "Custo unit.",
                       i.total AS "Total"
                FROM compra_itens i JOIN produtos p ON p.id = i.produto_id WHERE i.compra_id = @p0 ORDER BY i.id
                """, id);
    }

    void Cancelar()
    {
        if (Ui.IdLinha(docs) is not long id)
        {
            Ui.Erro("Selecione um documento.");
            return;
        }
        if (!Sessao.Admin)
        {
            Ui.Erro("Somente administradores podem cancelar.");
            return;
        }
        if (Ui.Txt(Ui.Linha(docs)!["Situação"]) == "CANCELADA")
        {
            Ui.Erro("Este documento já está cancelado.");
            return;
        }
        var aviso = venda
            ? $"Cancelar a venda nº {id}?\nOs produtos voltarão ao estoque e as contas a receber serão canceladas."
            : $"Cancelar a compra nº {id}?\nOs produtos sairão do estoque e as contas a pagar serão canceladas.";
        if (!Ui.Confirmar(aviso))
            return;
        try
        {
            if (venda)
            {
                Operacoes.CancelarVenda(id);
                var estornos = Tef.CancelarDaVenda(this, id);
                if (estornos.Count > 0)
                    Ui.Info("Estorno das transações de cartão/PIX:\n" + string.Join("\n", estornos));
            }
            else
                Operacoes.CancelarCompra(id);
            Carregar();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }
}
