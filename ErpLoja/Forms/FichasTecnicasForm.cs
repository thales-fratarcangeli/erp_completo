namespace ErpLoja.Forms;

/// <summary>Fichas técnicas: a receita de cada produto fabricado (insumos, perdas, rendimento e custo).</summary>
public class FichasTecnicasForm : Form
{
    readonly DataGridView produtos = Ui.Grid(), insumos = Ui.Grid(somenteLeitura: false);
    readonly TextBox busca = new() { Width = 220, PlaceholderText = "Filtrar produtos...", Margin = new Padding(4, 7, 4, 4) };
    readonly TextBox codigo = new() { Width = 160, Margin = new Padding(4, 7, 4, 4) };
    readonly NumericUpDown rendimento = Ui.Numero(3, 999_999, 0.001m), custoAdicional = Ui.Numero(), quantidade = Ui.Numero(3, 999_999, 0.001m);
    readonly TextBox observacoes = new() { Width = 420, Margin = new Padding(4, 7, 4, 4) };
    readonly Label titulo = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), Margin = new Padding(6, 8, 6, 4) };
    readonly Label resumo = new() { AutoSize = true, Font = Ui.FonteNegrito, Margin = new Padding(6, 10, 6, 4) };
    readonly Panel editor = new() { Dock = DockStyle.Fill, Visible = false };
    Producao.Ficha? ficha;
    decimal precoVenda;

    public FichasTecnicasForm()
    {
        Ui.Estilizar(this, "Fichas técnicas", 1250, 740, nameof(FichasTecnicasForm));
        quantidade.Value = 1;

        var esquerda = new Panel { Dock = DockStyle.Left, Width = 420, Padding = new Padding(0, 0, 6, 0) };
        var barraEsq = Ui.Barra();
        barraEsq.Padding = new Padding(6);
        barraEsq.Controls.Add(busca);
        esquerda.Controls.Add(produtos);
        esquerda.Controls.Add(barraEsq);
        produtos.BringToFront();

        var cabecalho = Ui.Barra();
        cabecalho.Controls.Add(titulo);
        var parametros = Ui.Barra();
        parametros.Controls.AddRange([
            Ui.Rotulo("Rendimento (quantidade que a receita produz):"), rendimento,
            Ui.Rotulo("Custo adicional por lote R$ (mão de obra, energia):"), custoAdicional,
            Ui.Rotulo("Observações / modo de preparo:"), observacoes,
        ]);
        var entrada = Ui.Barra();
        entrada.Controls.AddRange([
            Ui.Rotulo("Insumo (código):"), codigo, Ui.Rotulo("Qtd.:"), quantidade,
            Ui.Botao("Adicionar", (_, _) => Adicionar(), primario: true),
            Ui.Botao("Buscar (F3)", (_, _) => Adicionar(buscar: true)),
            Ui.Botao("Remover insumo", (_, _) => { if (insumos.CurrentRow?.DataBoundItem is InsumoFicha i) ficha!.Insumos.Remove(i); }),
        ]);
        var rodape = Ui.Barra(DockStyle.Bottom);
        rodape.Controls.AddRange([
            Ui.Botao("Salvar ficha", (_, _) => Salvar(), primario: true),
            Ui.Botao("Nova ordem de produção", (_, _) => NovaOrdem()),
            resumo,
        ]);
        editor.Controls.Add(insumos);
        editor.Controls.Add(entrada);
        editor.Controls.Add(parametros);
        editor.Controls.Add(cabecalho);
        editor.Controls.Add(rodape);
        insumos.BringToFront();

        var vazio = new Label { Dock = DockStyle.Fill, Text = "Selecione um produto à esquerda para montar a ficha técnica.", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DimGray };
        var direita = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        direita.Controls.Add(editor);
        direita.Controls.Add(vazio);

        Controls.Add(direita);
        Controls.Add(esquerda);

        busca.TextChanged += (_, _) => Ui.Filtrar(produtos, busca.Text);
        produtos.SelectionChanged += (_, _) => { if (Ui.IdLinha(produtos) is long id) Abrir(id); };
        insumos.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in insumos.Columns)
                c.ReadOnly = c.DataPropertyName is not (nameof(InsumoFicha.Quantidade) or nameof(InsumoFicha.PerdaPct));
        };
        insumos.CellEndEdit += (_, e) => { insumos.InvalidateRow(e.RowIndex); Resumo(); };
        rendimento.ValueChanged += (_, _) => Resumo();
        custoAdicional.ValueChanged += (_, _) => Resumo();
        codigo.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { Adicionar(); e.SuppressKeyPress = true; } };
        Load += (_, _) => Carregar();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F3 && ficha != null)
        {
            Adicionar(buscar: true);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void Carregar()
    {
        produtos.DataSource = Db.Query("""
            SELECT p.id, p.codigo AS "Código", p.nome AS "Produto",
                   CASE WHEN f.id IS NULL THEN '' ELSE 'Sim' END AS "Ficha",
                   CASE p.tipo WHEN 'PRODUTO_ACABADO' THEN 'Produzido' WHEN 'INSUMO' THEN 'Insumo' ELSE 'Mercadoria' END AS "Tipo"
            FROM produtos p LEFT JOIN fichas_tecnicas f ON f.produto_id = p.id
            WHERE p.ativo = 1 AND p.tipo <> 'INSUMO'
            ORDER BY CASE p.tipo WHEN 'PRODUTO_ACABADO' THEN 0 ELSE 1 END, p.nome
            """);
        Ui.Filtrar(produtos, busca.Text);
    }

    void Abrir(long produtoId)
    {
        var p = Db.Query("SELECT codigo, nome, unidade, preco_venda FROM produtos WHERE id = @p0", produtoId).Rows[0];
        precoVenda = Ui.Dec(p["preco_venda"]);
        ficha = Producao.CarregarFicha(produtoId);
        titulo.Text = $"{p["codigo"]} - {p["nome"]}  ({p["unidade"]})";
        rendimento.Value = Math.Clamp(ficha.Rendimento, rendimento.Minimum, rendimento.Maximum);
        custoAdicional.Value = ficha.CustoAdicional;
        observacoes.Text = ficha.Observacoes;
        insumos.DataSource = ficha.Insumos;
        ficha.Insumos.ListChanged += (_, _) => Resumo();
        editor.Visible = true;
        editor.BringToFront();
        Resumo();
    }

    Producao.Ficha Atual() => ficha! with { Rendimento = rendimento.Value, CustoAdicional = custoAdicional.Value, Observacoes = observacoes.Text.Trim() };

    void Resumo()
    {
        if (ficha is null)
            return;
        var f = Atual();
        var margem = f.CustoUnitario > 0 && precoVenda > 0 ? (precoVenda - f.CustoUnitario) / f.CustoUnitario * 100 : (decimal?)null;
        resumo.Text = $"Insumos {Ui.Moeda(f.CustoInsumos)} + adicional {Ui.Moeda(f.CustoAdicional)} = lote {Ui.Moeda(f.CustoLote)}   ·   " +
                      $"custo unitário {Ui.Moeda(f.CustoUnitario)}   ·   preço {Ui.Moeda(precoVenda)}" + (margem is decimal m ? $"   ·   margem {m:N1}%" : "");
        resumo.ForeColor = margem is < 0 ? Ui.Perigo : Color.Black;
    }

    void Adicionar(bool buscar = false)
    {
        if (ficha is null)
            return;
        var id = buscar ? BuscaProdutoForm.Escolher(this, codigo.Text.Trim()) : BuscaProdutoForm.Localizar(this, codigo.Text);
        if (id is not long insumo)
            return;
        if (insumo == ficha.ProdutoId)
        {
            Ui.Erro("O produto não pode ser insumo da própria ficha.");
            return;
        }
        var p = Db.Query("SELECT codigo, nome, unidade, preco_custo FROM produtos WHERE id = @p0", insumo).Rows[0];
        if (ficha.Insumos.FirstOrDefault(i => i.InsumoId == insumo) is { } existente)
        {
            existente.Quantidade += quantidade.Value;
            ficha.Insumos.ResetItem(ficha.Insumos.IndexOf(existente));
        }
        else
            ficha.Insumos.Add(new InsumoFicha
            {
                InsumoId = insumo,
                Codigo = Ui.Txt(p["codigo"]),
                Insumo = Ui.Txt(p["nome"]),
                Unidade = Ui.Txt(p["unidade"]),
                Quantidade = quantidade.Value,
                CustoUnitario = Ui.Dec(p["preco_custo"]),
            });
        codigo.Clear();
        quantidade.Value = 1;
        codigo.Focus();
    }

    void Salvar()
    {
        if (ficha is null)
            return;
        insumos.EndEdit();
        try
        {
            Producao.SalvarFicha(Atual());
            Ui.Feito("Ficha técnica salva.");
            var id = ficha.ProdutoId;
            Carregar();
            foreach (DataGridViewRow r in produtos.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == id)
                {
                    produtos.ClearSelection();
                    r.Selected = true;
                }
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void NovaOrdem()
    {
        if (ficha?.Id is null)
        {
            Ui.Erro("Salve a ficha técnica antes de criar a ordem de produção.");
            return;
        }
        if (OrdensProducaoForm.Nova(this, ficha.ProdutoId) is long op)
            Ui.Feito($"Ordem de produção nº {op} criada (Produção > Ordens de produção).");
    }
}
