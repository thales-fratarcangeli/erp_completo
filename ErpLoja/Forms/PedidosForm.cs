namespace ErpLoja.Forms;

/// <summary>Janela de orçamentos, pedidos de venda ou pedidos de compra (um tipo por janela).</summary>
public class PedidosForm : Form
{
    public PedidosForm(string tipo)
    {
        Ui.Estilizar(this, tipo == Pedidos.Orcamento ? "Orçamentos" : tipo == Pedidos.Venda ? "Pedidos de venda" : "Pedidos de compra",
            1200, 720, "pedidos_" + tipo);
        Controls.Add(new PedidosPainel(tipo) { Dock = DockStyle.Fill });
    }
}

/// <summary>Lista de orçamentos / pedidos de um tipo, com as ações do fluxo.</summary>
public class PedidosPainel : UserControl
{
    readonly string tipo;
    readonly ComboBox situacao = Ui.Combo(160);
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-90)), ate = Ui.Data();
    readonly TextBox busca = new() { Width = 200, PlaceholderText = "Filtrar...", Margin = new Padding(4, 7, 4, 4) };
    readonly DataGridView docs = Ui.Grid(), itens = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly Label resumo = Ui.Rotulo("");

    public PedidosPainel(string tipo)
    {
        this.tipo = tipo;
        Font = Ui.Fonte;
        BackColor = Ui.Fundo;

        situacao.Items.AddRange(tipo switch
        {
            Pedidos.Orcamento => ["Em aberto", "Vencidos", "Aprovados", "Recusados", "Cancelados", "Todos"],
            Pedidos.Venda => ["Em aberto", "Faturados", "Cancelados", "Todos"],
            _ => ["Em aberto", "Recebidos", "Cancelados", "Todos"],
        });
        situacao.SelectedIndex = 0;

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Situação:"), situacao, Ui.Rotulo("Emitidos de:"), de, Ui.Rotulo("até:"), ate,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true), busca, resumo,
        ]);

        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Novo", (_, _) => Editar(null), primario: true),
            Ui.Botao("Abrir / editar", (_, _) => { if (Ui.IdLinha(docs) is long id) Editar(id); }),
            Ui.Botao("Duplicar", (_, _) => Duplicar()),
            Ui.Botao("Imprimir", (_, _) => Imprimir()),
        ]);
        switch (tipo)
        {
            case Pedidos.Orcamento:
                acoes.Controls.Add(Ui.Botao("Aprovar → gerar pedido", (_, _) => Aprovar(), primario: true));
                acoes.Controls.Add(Ui.Botao("Recusar", (_, _) => Mudar("RECUSADO", "Marcar o orçamento como recusado pelo cliente?")));
                break;
            case Pedidos.Venda:
                acoes.Controls.Add(Ui.Botao("Faturar (gerar venda)", (_, _) => Faturar(), primario: true));
                break;
            default:
                acoes.Controls.Add(Ui.Botao("Receber mercadoria", (_, _) => Receber(), primario: true));
                break;
        }
        acoes.Controls.AddRange([
            Ui.Botao("Cancelar", (_, _) => Mudar("CANCELADO", "Cancelar este documento?")),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(docs, tipo.ToLower())),
        ]);

        split.Panel1.Controls.Add(docs);
        split.Panel2.Controls.Add(itens);
        split.Panel2.Controls.Add(new Label { Text = "Itens", Dock = DockStyle.Top, Height = 26, Font = Ui.FonteNegrito, Padding = new Padding(6, 5, 0, 0) });
        itens.BringToFront();
        Ui.ColorirSituacao(docs);

        Controls.Add(split);
        Controls.Add(acoes);
        Controls.Add(filtros);

        situacao.SelectedIndexChanged += (_, _) => Carregar();
        busca.TextChanged += (_, _) => Ui.Filtrar(docs, busca.Text);
        docs.SelectionChanged += (_, _) => CarregarItens();
        docs.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && Ui.IdLinha(docs) is long id) Editar(id); };
        Load += (_, _) =>
        {
            split.SplitterDistance = Math.Max(100, split.Height * 60 / 100);
            Carregar();
        };
    }

    void Carregar(long? selecionar = null)
    {
        var compra = tipo == Pedidos.Compra;
        var hoje = "date('now', 'localtime')";
        var filtro = situacao.Text switch
        {
            "Em aberto" => tipo == Pedidos.Orcamento ? $"p.status = 'ABERTO' AND (p.validade IS NULL OR p.validade >= {hoje})" : "p.status = 'ABERTO'",
            "Vencidos" => $"p.status = 'ABERTO' AND p.validade < {hoje}",
            "Aprovados" => "p.status = 'APROVADO'",
            "Recusados" => "p.status = 'RECUSADO'",
            "Faturados" => "p.status = 'FATURADO'",
            "Recebidos" => "p.status = 'RECEBIDO'",
            "Cancelados" => "p.status = 'CANCELADO'",
            _ => "1 = 1",
        };
        // documentos em aberto aparecem sempre; os demais respeitam o período
        if (situacao.SelectedIndex != 0)
            filtro += " AND date(p.data) BETWEEN @p1 AND @p2";

        var dt = Db.Query($"""
            SELECT p.id, p.id AS "Nº", strftime('%d/%m/%Y', p.data) AS "Emissão",
                   {(compra ? "COALESCE(f.nome, '')" : "COALESCE(c.nome, 'Consumidor final')")} AS "{(compra ? "Fornecedor" : "Cliente")}",
                   strftime('%d/%m/%Y', p.validade) AS "{(tipo == Pedidos.Orcamento ? "Válido até" : "Previsão")}",
                   (SELECT COUNT(*) FROM pedido_itens i WHERE i.pedido_id = p.id) AS "Itens",
                   p.total AS "Total", p.forma_pagamento AS "Pagamento",
                   CASE WHEN p.status = 'ABERTO' AND p.tipo = 'ORCAMENTO' AND p.validade < {hoje} THEN 'VENCIDO' ELSE p.status END AS "Situação",
                   COALESCE('Venda nº ' || p.venda_id, 'Compra nº ' || p.compra_id,
                            (SELECT 'Pedido nº ' || x.id FROM pedidos x WHERE x.origem_id = p.id LIMIT 1),
                            CASE WHEN p.origem_id IS NOT NULL THEN 'Do orçamento nº ' || p.origem_id END, '') AS "Vínculo",
                   u.nome AS "Usuário"
            FROM pedidos p
            LEFT JOIN pessoas c ON c.id = p.cliente_id
            LEFT JOIN pessoas f ON f.id = p.fornecedor_id
            LEFT JOIN usuarios u ON u.id = p.usuario_id
            WHERE p.tipo = @p0 AND {filtro}
            ORDER BY p.id DESC
            """, tipo, Db.Dia(de.Value), Db.Dia(ate.Value));
        docs.DataSource = dt;
        Ui.Filtrar(docs, busca.Text);
        resumo.Text = $"{dt.Rows.Count} documento(s) · {Ui.Moeda(dt.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["Total"])))}";

        if (selecionar != null)
            foreach (DataGridViewRow r in docs.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                {
                    docs.ClearSelection();
                    r.Selected = true;
                    break;
                }
        CarregarItens();
    }

    void CarregarItens()
    {
        if (Ui.IdLinha(docs) is not long id)
        {
            itens.DataSource = null;
            return;
        }
        itens.DataSource = Db.Query("""
            SELECT i.id, p.codigo AS "Código", p.nome AS "Produto", p.unidade AS "Un.", i.quantidade AS "Qtd.",
                   i.preco_unitario AS "Preço unit.", i.desconto AS "Desconto", i.total AS "Total", p.estoque AS "Estoque atual"
            FROM pedido_itens i JOIN produtos p ON p.id = i.produto_id
            WHERE i.pedido_id = @p0 ORDER BY i.id
            """, id);
    }

    void Editar(long? id)
    {
        using var f = new PedidoEditorForm(tipo, id);
        f.ShowDialog(FindForm());
        Carregar(f.IdSalvo ?? id);
    }

    (long Id, string Situacao)? Selecionado()
    {
        if (Ui.Linha(docs) is DataRowView r)
            return (Convert.ToInt64(r["id"]), Ui.Txt(r["Situação"]));
        Ui.Erro("Selecione um documento.");
        return null;
    }

    bool EmAberto((long Id, string Situacao) sel)
    {
        if (sel.Situacao is "ABERTO" or "VENCIDO")
            return true;
        Ui.Erro($"Este documento está {sel.Situacao}. A operação só vale para documentos em aberto.");
        return false;
    }

    void Duplicar()
    {
        if (Selecionado() is not { } sel)
            return;
        var novo = Pedidos.Duplicar(sel.Id);
        Carregar(novo);
        Editar(novo);
    }

    void Imprimir()
    {
        if (Selecionado() is { } sel)
            CupomForm.Visualizar(FindForm()!, $"{Pedidos.Nome(tipo)} nº {sel.Id}", Pedidos.Texto(sel.Id), 80);
    }

    void Mudar(string para, string pergunta)
    {
        if (Selecionado() is not { } sel || !EmAberto(sel) || !Ui.Confirmar(pergunta))
            return;
        try
        {
            Pedidos.MudarStatus(sel.Id, "ABERTO", para);
            Carregar(sel.Id);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void Aprovar()
    {
        if (Selecionado() is not { } sel || !EmAberto(sel))
            return;
        if (sel.Situacao == "VENCIDO" && !Ui.Confirmar("Este orçamento está vencido. Aprovar mesmo assim (preços serão mantidos)?"))
            return;
        if (!Ui.Confirmar($"Aprovar o orçamento nº {sel.Id} e gerar um pedido de venda?"))
            return;
        try
        {
            var pedido = Pedidos.AprovarOrcamento(sel.Id);
            Ui.Info($"Pedido de venda nº {pedido} gerado. Ele está na tela Pedidos, aba Pedidos de venda.");
            Carregar(sel.Id);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void Faturar()
    {
        if (Selecionado() is { } sel && EmAberto(sel) && FluxoPedidos.Faturar(FindForm()!, sel.Id))
            Carregar(sel.Id);
    }

    void Receber()
    {
        if (Selecionado() is { } sel && EmAberto(sel) && FluxoPedidos.Receber(FindForm()!, sel.Id))
            Carregar(sel.Id);
    }
}

/// <summary>Conclusão de pedidos (faturar venda / receber compra), usada pela lista e pelo editor.</summary>
public static class FluxoPedidos
{
    /// <summary>Pergunta forma de pagamento / parcelas / vencimento (e nº da NF na compra).</summary>
    static bool PerguntarPagamento(IWin32Window dono, Pedido p, string titulo, out string forma, out int parcelas, out DateTime vencimento, out string nota)
    {
        using var d = new Dialogo(titulo, "pedido_pagamento", "Confirmar");
        d.Nota($"{Pedidos.Nome(p.Tipo)} nº {p.Id} — total {Ui.Moeda(p.Total)}");
        var cbForma = d.Adicionar("Pagamento:", Ui.Combo());
        cbForma.Items.AddRange(p.Tipo == Pedidos.Compra ? PedidoEditorForm.FormasCompra : PedidoEditorForm.FormasVenda);
        cbForma.SelectedItem = cbForma.Items.Contains(p.Forma) ? p.Forma : cbForma.Items[0];
        var nudParcelas = d.Adicionar("Parcelas:", Ui.Numero(0, Parametros.MaxParcelas, 1));
        nudParcelas.Value = Math.Clamp(p.Parcelas, 1, Parametros.MaxParcelas);
        var dtVenc = d.Adicionar("1º vencimento:", Ui.Data(DateTime.Today.AddDays(Parametros.PrazoVencimentoDias)));
        var tbNota = p.Tipo == Pedidos.Compra ? d.Adicionar("Nº da nota fiscal:", new TextBox()) : null;
        d.Nota("Parcelas e vencimento valem para pagamento a prazo.");
        d.Validar = () =>
        {
            if (p.Tipo == Pedidos.Venda && cbForma.Text == Operacoes.APrazo && p.ClienteId is null)
            {
                Ui.Erro("Para vender a prazo o pedido precisa ter um cliente.");
                return false;
            }
            return true;
        };
        var ok = d.ShowDialog(dono) == DialogResult.OK;
        forma = cbForma.Text;
        parcelas = (int)nudParcelas.Value;
        vencimento = dtVenc.Value.Date;
        nota = tbNota?.Text.Trim() ?? "";
        return ok;
    }

    public static bool Faturar(IWin32Window dono, long id)
    {
        var p = Pedidos.Carregar(id);
        var faltando = Db.Query("""
            SELECT p.nome, p.estoque, SUM(i.quantidade) AS q FROM pedido_itens i JOIN produtos p ON p.id = i.produto_id
            WHERE i.pedido_id = @p0 GROUP BY p.id HAVING SUM(i.quantidade) > p.estoque
            """, id);
        if (faltando.Rows.Count > 0 && !Parametros.PodeVenderSemEstoque("Estoque insuficiente para:\n" +
                string.Join("\n", faltando.Rows.Cast<DataRow>().Select(r => $"• {r["nome"]} (pedido {Ui.Dec(r["q"]):N2}, estoque {Ui.Dec(r["estoque"]):N2})"))))
            return false;
        if (!PerguntarPagamento(dono, p, "Faturar pedido", out var forma, out var parcelas, out var vencimento, out _))
            return false;
        try
        {
            var venda = Pedidos.Faturar(id, forma, parcelas, vencimento);
            if (Ui.Confirmar($"Pedido faturado: venda nº {venda} registrada (estoque baixado e contas geradas).\n\nDeseja emitir a nota fiscal agora?"))
                NotasForm.NotaDeVenda(dono, venda);
            return true;
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
            return false;
        }
    }

    public static bool Receber(IWin32Window dono, long id)
    {
        var p = Pedidos.Carregar(id);
        // custo zero puxa o custo médio para baixo: só faz sentido em bonificação
        var semCusto = p.Itens.Where(i => i.Unitario == 0).Select(i => "• " + i.Produto).ToList();
        if (semCusto.Count > 0 &&
            !Ui.Confirmar("Estes itens estão com custo unitário ZERO:\n" + string.Join("\n", semCusto) +
                          "\n\nEles entrarão no estoque sem custo e o custo médio desses produtos vai cair.\n" +
                          "É uma bonificação? Responda Não para voltar e informar o custo."))
            return false;
        if (!PerguntarPagamento(dono, p, "Receber mercadoria", out var forma, out var parcelas, out var vencimento, out var nota))
            return false;
        if (p.FornecedorId is long fornecedor && Operacoes.CompraComMesmaNota(fornecedor, nota) is long existente &&
            !Ui.Confirmar($"Já existe a compra nº {existente} deste fornecedor com a nota fiscal {nota}.\n" +
                          "Receber de novo dará entrada em dobro no estoque e nas contas a pagar.\n\nConfirma que é outra entrega?"))
            return false;
        try
        {
            var compra = Pedidos.Receber(id, forma, parcelas, vencimento, nota);
            Ui.Info($"Mercadoria recebida: compra nº {compra} registrada (estoque, custo médio e contas a pagar atualizados).");
            return true;
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
            return false;
        }
    }
}
