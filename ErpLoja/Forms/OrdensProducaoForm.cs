namespace ErpLoja.Forms;

/// <summary>Ordens de produção: planejar, iniciar, concluir (baixa insumos e dá entrada no produto) e imprimir.</summary>
public class OrdensProducaoForm : Form
{
    readonly ComboBox situacao = Ui.Combo(150);
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-60)), ate = Ui.Data(DateTime.Today.AddDays(60));
    readonly DataGridView ordens = Ui.Grid(), insumos = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly Label resumo = Ui.Rotulo("");

    public OrdensProducaoForm()
    {
        Ui.Estilizar(this, "Ordens de produção", 1200, 720, nameof(OrdensProducaoForm));
        situacao.Items.AddRange(["Em aberto", "Concluídas", "Canceladas", "Todas"]);
        situacao.SelectedIndex = 0;

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Situação:"), situacao, Ui.Rotulo("Emitidas de:"), de, Ui.Rotulo("até:"), ate,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true), resumo,
        ]);
        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Nova OP", (_, _) => { if (Nova(this, null) is long id) Carregar(id); }, primario: true),
            Ui.Botao("Iniciar produção", (_, _) => Executar(id => { Producao.Iniciar(id); Ui.Feito($"OP nº {id} em produção."); })),
            Ui.Botao("Concluir", (_, _) => Concluir(), primario: true),
            Ui.Botao("Cancelar OP", (_, _) => Executar(id => { if (Ui.Confirmar($"Cancelar a OP nº {id}?")) Producao.Cancelar(id); })),
            Ui.Botao("Imprimir", (_, _) => { if (Selecionada() is long id) CupomForm.Visualizar(this, $"OP nº {id}", Producao.Texto(id), 80); }),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(ordens, "ordens_producao")),
        ]);

        split.Panel1.Controls.Add(ordens);
        split.Panel2.Controls.Add(insumos);
        split.Panel2.Controls.Add(new Label { Text = "Insumos da OP (previsto, consumido e saldo atual)", Dock = DockStyle.Top, Height = 26, Font = Ui.FonteNegrito, Padding = new Padding(6, 5, 0, 0) });
        insumos.BringToFront();
        Ui.ColorirSituacao(ordens);
        Ui.ColorirSituacao(insumos);

        Controls.Add(split);
        Controls.Add(acoes);
        Controls.Add(filtros);

        situacao.SelectedIndexChanged += (_, _) => Carregar();
        ordens.SelectionChanged += (_, _) => CarregarInsumos();
        Load += (_, _) =>
        {
            split.SplitterDistance = Math.Max(120, split.Height * 55 / 100);
            Carregar();
        };
    }

    void Carregar(long? selecionar = null)
    {
        var filtro = situacao.SelectedIndex switch
        {
            0 => "o.status IN ('PLANEJADA', 'EM_PRODUCAO')",
            1 => "o.status = 'CONCLUIDA' AND date(o.data) BETWEEN @p0 AND @p1",
            2 => "o.status = 'CANCELADA' AND date(o.data) BETWEEN @p0 AND @p1",
            _ => "date(o.data) BETWEEN @p0 AND @p1",
        };
        var dt = Db.Query($"""
            SELECT o.id, o.id AS "Nº", strftime('%d/%m/%Y', o.data) AS "Emissão", p.codigo AS "Código", p.nome AS "Produto",
                   o.quantidade AS "Planejado", o.quantidade_produzida AS "Produzido", p.unidade AS "Un.",
                   CASE o.status WHEN 'PLANEJADA' THEN 'PLANEJADA' WHEN 'EM_PRODUCAO' THEN 'EM PRODUÇÃO' WHEN 'CONCLUIDA' THEN 'CONCLUÍDA' ELSE 'CANCELADA' END AS "Situação",
                   strftime('%d/%m/%Y', o.previsao) AS "Previsão", o.custo_unitario AS "Custo unit.", o.custo_total AS "Custo total"
            FROM ordens_producao o JOIN produtos p ON p.id = o.produto_id
            WHERE {filtro} ORDER BY o.id DESC
            """, Db.Dia(de.Value), Db.Dia(ate.Value));
        ordens.DataSource = dt;
        resumo.Text = $"{dt.Rows.Count} ordem(ns)";
        if (selecionar != null)
            foreach (DataGridViewRow r in ordens.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                {
                    ordens.ClearSelection();
                    r.Selected = true;
                }
        CarregarInsumos();
    }

    void CarregarInsumos()
    {
        if (Ui.IdLinha(ordens) is not long id)
        {
            insumos.DataSource = null;
            return;
        }
        insumos.DataSource = Db.Query("""
            SELECT i.id, p.codigo AS "Código", p.nome AS "Insumo", p.unidade AS "Un.", i.quantidade_prevista AS "Previsto",
                   i.quantidade_consumida AS "Consumido", p.estoque AS "Saldo atual",
                   CASE WHEN i.quantidade_consumida IS NULL AND p.estoque < i.quantidade_prevista THEN 'BAIXO' ELSE 'OK' END AS "Situação"
            FROM op_itens i JOIN produtos p ON p.id = i.insumo_id WHERE i.op_id = @p0 ORDER BY p.nome
            """, id);
    }

    /// <summary>OP selecionada; sem seleção, avisa em vez de não fazer nada.</summary>
    long? Selecionada()
    {
        if (Ui.IdLinha(ordens) is long id)
            return id;
        Ui.Erro("Selecione uma ordem de produção.");
        return null;
    }

    void Executar(Action<long> acao)
    {
        if (Ui.IdLinha(ordens) is not long id)
        {
            Ui.Erro("Selecione uma ordem de produção.");
            return;
        }
        try
        {
            acao(id);
            Carregar(id);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void Concluir() => Executar(id =>
    {
        var o = Db.Query("SELECT quantidade, status FROM ordens_producao WHERE id = @p0", id).Rows[0];
        if (Ui.Txt(o["status"]) is not ("PLANEJADA" or "EM_PRODUCAO"))
            throw new InvalidOperationException("Só OP planejada ou em produção pode ser concluída.");
        using var d = new Dialogo($"Concluir OP nº {id}", "OrdensProducaoForm", "Concluir");
        var produzido = d.Adicionar("Quantidade produzida:", Ui.Numero(3, 9_999_999, 0.001m));
        produzido.Value = Ui.Dec(o["quantidade"]);
        d.Nota("Os insumos são baixados na proporção do que foi produzido. O custo real recalcula o custo médio do produto.");
        if (d.ShowDialog(this) != DialogResult.OK)
            return;
        // a falta é conferida com o que foi produzido de fato (produzir mais que o planejado consome mais insumo)
        var faltas = Producao.Faltas(id, produzido.Value);
        if (faltas.Count > 0 && !Parametros.PodeProduzirSemEstoque($"Insumos insuficientes para produzir {produzido.Value:N3}:\n" +
                string.Join("\n", faltas.Select(f => $"• {f.Insumo}: precisa {f.Necessario:N3}, saldo {f.Saldo:N3}"))))
            return;
        var custo = Producao.Concluir(id, produzido.Value);
        Ui.Feito($"OP nº {id} concluída. Custo unitário real: {Ui.Moeda(custo)}.");
    });

    /// <summary>Diálogo de nova OP. produtoId pré-seleciona o produto (vindo da ficha técnica).</summary>
    public static long? Nova(IWin32Window dono, long? produtoId)
    {
        using var d = new Dialogo("Nova ordem de produção", "OrdensProducaoForm", "Criar");
        var produto = d.Adicionar("Produto:", Ui.Combo(380));
        Ui.Preencher(produto, Db.Query("""
            SELECT p.id, p.codigo || ' - ' || p.nome AS nome FROM produtos p JOIN fichas_tecnicas f ON f.produto_id = p.id
            WHERE p.ativo = 1 ORDER BY p.nome
            """));
        if (produto.Items.Count == 0)
        {
            Ui.Erro("Nenhum produto tem ficha técnica. Cadastre em Produção > Fichas técnicas.");
            return null;
        }
        Ui.Selecionar(produto, produtoId);
        var quantidade = d.Adicionar("Quantidade a produzir:", Ui.Numero(3, 9_999_999, 0.001m));
        quantidade.Value = 1;
        var previsao = d.Adicionar("Previsão de conclusão:", Ui.Data(DateTime.Today.AddDays(1)));
        var obs = d.Adicionar("Observações:", Validacao.Caixa(Formatos.Texto(500), "Observações"));
        d.Nota("A necessidade de cada insumo é calculada pela ficha técnica (proporção do rendimento + perdas).");
        if (d.ShowDialog(dono) != DialogResult.OK || Ui.Id(produto) is not long id)
            return null;
        try
        {
            return Producao.CriarOrdem(id, quantidade.Value, previsao.Value, obs.Text.Trim());
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
            return null;
        }
    }
}
