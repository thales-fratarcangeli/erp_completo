namespace ErpLoja.Forms;

/// <summary>Notas fiscais de saída e de entrada: emissão, DANFE, importação de XML e cancelamento.</summary>
public class NotasForm : Form
{
    readonly ComboBox tipo = Ui.Combo(120), situacao = Ui.Combo(130);
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-30)), ate = Ui.Data();
    readonly TextBox busca = new() { Width = 180, PlaceholderText = "Filtrar...", Margin = new Padding(4, 7, 4, 4) };
    readonly DataGridView notas = Ui.Grid(), itens = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly Label resumo = Ui.Rotulo("");

    public NotasForm()
    {
        Validacao.Aplicar(busca, Formatos.Busca, "Filtro");
        Ui.Estilizar(this, "Notas fiscais", 1250, 740, nameof(NotasForm));
        tipo.Items.AddRange(["Todas", "Saída", "Entrada"]);
        situacao.Items.AddRange(["Todas", "Em digitação", "Emitidas", "Canceladas"]);
        tipo.SelectedIndex = situacao.SelectedIndex = 0;

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Tipo:"), tipo, Ui.Rotulo("Situação:"), situacao, Ui.Rotulo("Emissão de:"), de, Ui.Rotulo("até:"), ate,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true), busca, resumo,
        ]);
        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Nova NF de saída (venda)", (_, _) => NovaSaida(), primario: true),
            Ui.Botao("Nova NF de entrada (compra)", (_, _) => NovaEntrada()),
            Ui.Botao("Abrir / editar", (_, _) => Abrir()),
            Ui.Botao("Emitir", (_, _) => Emitir()),
            Ui.Botao("DANFE", (_, _) => { if (Ui.IdLinha(notas) is long id) CupomForm.Visualizar(this, "DANFE", Fiscal.Danfe(id), 100); }),
            Ui.Botao("Salvar XML", (_, _) => SalvarXml()),
            Ui.Botao("Cancelar / excluir", (_, _) => Cancelar()),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(notas, "notas_fiscais")),
        ]);
        var aviso = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            ForeColor = Color.DimGray,
            Padding = new Padding(8, 5, 0, 0),
            Text = "Notas próprias são numeradas e controladas pelo sistema; a transmissão à SEFAZ (certificado A1) ainda não está ativa, " +
                   "por isso o DANFE sai marcado \"sem valor fiscal\".",
        };

        split.Panel1.Controls.Add(notas);
        split.Panel2.Controls.Add(itens);
        split.Panel2.Controls.Add(new Label { Text = "Itens da nota", Dock = DockStyle.Top, Height = 26, Font = Ui.FonteNegrito, Padding = new Padding(6, 5, 0, 0) });
        itens.BringToFront();
        Ui.ColorirSituacao(notas);

        Controls.Add(split);
        Controls.Add(acoes);
        Controls.Add(filtros);
        Controls.Add(aviso);

        tipo.SelectedIndexChanged += (_, _) => Carregar();
        situacao.SelectedIndexChanged += (_, _) => Carregar();
        busca.TextChanged += (_, _) => Ui.Filtrar(notas, busca.Text);
        notas.SelectionChanged += (_, _) => CarregarItens();
        notas.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Abrir(); };
        Load += (_, _) =>
        {
            split.SplitterDistance = Math.Max(100, split.Height * 60 / 100);
            Carregar();
        };
    }

    void Carregar(long? selecionar = null)
    {
        var condicoes = new List<string> { "date(n.data_emissao) BETWEEN @p0 AND @p1" };
        if (tipo.SelectedIndex == 1) condicoes.Add("n.tipo = 'SAIDA'");
        if (tipo.SelectedIndex == 2) condicoes.Add("n.tipo = 'ENTRADA'");
        if (situacao.SelectedIndex == 1) condicoes[0] = "n.status = 'DIGITADA'"; // rascunhos aparecem independente da data
        if (situacao.SelectedIndex == 2) condicoes.Add("n.status = 'EMITIDA'");
        if (situacao.SelectedIndex == 3) condicoes.Add("n.status = 'CANCELADA'");

        var dt = Db.Query($"""
            SELECT n.id, CASE n.tipo WHEN 'SAIDA' THEN 'Saída' ELSE 'Entrada' END AS "Tipo",
                   CASE n.modelo WHEN '65' THEN 'NFC-e' ELSE 'NF-e' END AS "Modelo",
                   n.numero AS "Número", n.serie AS "Série", strftime('%d/%m/%Y %H:%M', n.data_emissao) AS "Emissão",
                   n.part_nome AS "Destinatário / emitente", n.part_documento AS "CPF/CNPJ", n.natureza_operacao AS "Natureza",
                   n.valor_total AS "Total", n.valor_icms AS "ICMS", n.status AS "Situação",
                   CASE n.propria WHEN 1 THEN 'Própria' ELSE 'Fornecedor (XML)' END AS "Emissão por",
                   COALESCE('Venda nº ' || n.venda_id, 'Compra nº ' || n.compra_id, '') AS "Vínculo"
            FROM notas_fiscais n
            WHERE {string.Join(" AND ", condicoes)}
            ORDER BY n.id DESC
            """, Db.Dia(de.Value), Db.Dia(ate.Value));
        notas.DataSource = dt;
        Ui.Filtrar(notas, busca.Text);
        var validas = dt.Rows.Cast<DataRow>().Where(r => Ui.Txt(r["Situação"]) == "EMITIDA").ToList();
        resumo.Text = $"{dt.Rows.Count} nota(s) · emitidas: {Ui.Moeda(validas.Sum(r => Ui.Dec(r["Total"])))}";

        if (selecionar != null)
            foreach (DataGridViewRow r in notas.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                {
                    notas.ClearSelection();
                    r.Selected = true;
                    break;
                }
        CarregarItens();
    }

    void CarregarItens()
    {
        if (Ui.IdLinha(notas) is not long id)
        {
            itens.DataSource = null;
            return;
        }
        itens.DataSource = Db.Query("""
            SELECT id, codigo AS "Código", descricao AS "Descrição", ncm AS "NCM", cfop AS "CFOP", cst AS "CST/CSOSN",
                   unidade AS "Un.", quantidade AS "Qtd.", valor_unitario AS "Vlr. unit.", desconto AS "Desconto",
                   valor_total AS "Total", aliq_icms AS "% ICMS", valor_icms AS "ICMS", valor_ipi AS "IPI"
            FROM nota_itens WHERE nota_id = @p0 ORDER BY id
            """, id);
    }

    void AbrirEditor(NotaFiscal n)
    {
        using var f = new NotaEditorForm(n);
        f.ShowDialog(this);
        Carregar(f.IdSalvo);
    }

    /// <summary>Abre a nota da venda (a existente, ou uma nova montada a partir da venda).</summary>
    public static void NotaDeVenda(IWin32Window dono, long vendaId)
    {
        if (Fiscal.NotaExistente("venda_id", vendaId) is long existente)
        {
            Ui.Info($"A venda nº {vendaId} já possui nota (registro {existente}). Abrindo...");
            using var f1 = new NotaEditorForm(Fiscal.Carregar(existente));
            f1.ShowDialog(dono);
            return;
        }
        using var f = new NotaEditorForm(Fiscal.DeVenda(vendaId));
        f.ShowDialog(dono);
    }

    long? EscolherOrigem(string titulo, string sql, string vazio)
    {
        using var d = new Dialogo(titulo, "nota_origem", "Continuar");
        var combo = d.Adicionar("Origem:", Ui.Combo(520));
        Ui.Preencher(combo, Db.Query(sql), vazio);
        d.Nota("Somente documentos dos últimos 180 dias que ainda não têm nota aparecem na lista.");
        if (d.ShowDialog(this) != DialogResult.OK)
            return -1;
        return Ui.Id(combo);
    }

    void NovaSaida()
    {
        var venda = EscolherOrigem("Nova NF de saída", """
            SELECT v.id, 'Venda nº ' || v.id || '  ·  ' || strftime('%d/%m/%Y', v.data) || '  ·  ' ||
                   COALESCE(c.nome, 'Consumidor final') || '  ·  R$ ' || printf('%.2f', v.total) AS nome
            FROM vendas v LEFT JOIN pessoas c ON c.id = v.cliente_id
            WHERE v.status = 'FINALIZADA' AND date(v.data) >= date('now', 'localtime', '-180 day')
              AND NOT EXISTS (SELECT 1 FROM notas_fiscais n WHERE n.venda_id = v.id AND n.status <> 'CANCELADA')
            ORDER BY v.id DESC
            """, "(nota avulsa, sem venda)");
        if (venda == -1)
            return;
        AbrirEditor(venda is long id ? Fiscal.DeVenda(id) : Fiscal.Nova(Fiscal.Saida));
    }

    void NovaEntrada()
    {
        var compra = EscolherOrigem("Nova NF de entrada", """
            SELECT o.id, 'Compra nº ' || o.id || '  ·  ' || strftime('%d/%m/%Y', o.data) || '  ·  ' || f.nome ||
                   '  ·  R$ ' || printf('%.2f', o.total) AS nome
            FROM compras o JOIN pessoas f ON f.id = o.fornecedor_id
            WHERE o.status = 'FINALIZADA' AND date(o.data) >= date('now', 'localtime', '-180 day')
              AND NOT EXISTS (SELECT 1 FROM notas_fiscais n WHERE n.compra_id = o.id AND n.status <> 'CANCELADA')
            ORDER BY o.id DESC
            """, "(nota avulsa, sem compra)");
        if (compra == -1)
            return;
        AbrirEditor(compra is long id ? Fiscal.DeCompra(id) : Fiscal.Nova(Fiscal.Entrada));
    }

    (long Id, string Situacao, bool Propria)? Selecionada()
    {
        if (Ui.Linha(notas) is DataRowView r)
            return (Convert.ToInt64(r["id"]), Ui.Txt(r["Situação"]), Ui.Txt(r["Emissão por"]) == "Própria");
        Ui.Erro("Selecione uma nota.");
        return null;
    }

    void Abrir()
    {
        if (Selecionada() is not { } sel)
            return;
        if (!sel.Propria)
        {
            CupomForm.Visualizar(this, "DANFE", Fiscal.Danfe(sel.Id), 100);
            return;
        }
        AbrirEditor(Fiscal.Carregar(sel.Id));
    }

    void Emitir()
    {
        if (Selecionada() is not { } sel)
            return;
        if (sel.Situacao != "DIGITADA")
        {
            Ui.Erro("Somente notas em digitação podem ser emitidas.");
            return;
        }
        if (!Ui.Confirmar("Emitir a nota selecionada? A numeração passa a ser definitiva."))
            return;
        try
        {
            Fiscal.Emitir(sel.Id);
            Carregar(sel.Id);
            CupomForm.Visualizar(this, "DANFE", Fiscal.Danfe(sel.Id), 100);
        }
        catch (Exception ex)
        {
            Ui.Erro("Não foi possível emitir:\n\n" + ex.Message);
        }
    }

    void SalvarXml()
    {
        if (Selecionada() is not { } sel)
            return;
        var xml = Db.Scalar("SELECT xml FROM notas_fiscais WHERE id = @p0", sel.Id) as string;
        if (string.IsNullOrEmpty(xml))
        {
            Ui.Erro("Só há XML para notas importadas do fornecedor. Notas próprias terão XML quando a transmissão à SEFAZ for ativada.");
            return;
        }
        using var dlg = new SaveFileDialog { Filter = "XML (*.xml)|*.xml", FileName = $"NFe{Db.Scalar("SELECT chave FROM notas_fiscais WHERE id = @p0", sel.Id)}.xml" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dlg.FileName, xml);
    }

    void Cancelar()
    {
        if (Selecionada() is not { } sel)
            return;
        switch (sel.Situacao)
        {
            case "CANCELADA":
                Ui.Erro("A nota já está cancelada.");
                return;
            case "DIGITADA":
                if (Ui.Confirmar("Excluir este rascunho de nota?"))
                    Db.Exec("DELETE FROM notas_fiscais WHERE id = @p0 AND status = 'DIGITADA'", sel.Id);
                break;
            default:
                if (!Sessao.Admin)
                {
                    Ui.Erro("Somente um administrador pode cancelar nota fiscal emitida. Peça a um administrador.");
                    return;
                }
                if (!Ui.Confirmar("Cancelar a nota?" + (sel.Propria ? "" :
                        "\n\nAtenção: isso não desfaz a compra lançada. Para estornar estoque e contas, cancele a compra em Compras realizadas.")))
                    return;
                try
                {
                    Fiscal.Cancelar(sel.Id);
                }
                catch (InvalidOperationException ex)
                {
                    Ui.Erro(ex.Message);
                }
                break;
        }
        Carregar();
    }
}
