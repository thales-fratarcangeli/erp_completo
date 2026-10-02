namespace ErpLoja.Forms;

/// <summary>Edição de orçamento, pedido de venda ou pedido de compra.</summary>
public class PedidoEditorForm : Form
{
    public static readonly string[] FormasVenda = ["Dinheiro", "PIX", "Cartão de débito", "Cartão de crédito", "Boleto", Operacoes.APrazo];
    public static readonly string[] FormasCompra = ["Dinheiro", "PIX", "Transferência", "Boleto", "Cartão", Operacoes.APrazo];

    readonly Pedido p;
    readonly bool editavel;
    readonly DataGridView grid = Ui.Grid(somenteLeitura: false);
    readonly ComboBox pessoa = Ui.Combo(320), forma = Ui.Combo(170);
    readonly DateTimePicker validade = Ui.Data();
    readonly NumericUpDown parcelas = Ui.Numero(0, 24, 1), desconto = Ui.Numero(), frete = Ui.Numero(), qtd = Ui.Numero(3);
    readonly TextBox codigo = new() { Width = 200, Margin = new Padding(4, 7, 4, 4) };
    readonly TextBox observacoes = new() { Width = 640, Margin = new Padding(4, 7, 4, 4) };
    readonly Label lblResumo = Ui.Rotulo("");
    readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Ui.Primaria, Margin = new Padding(16, 4, 8, 0) };

    public long? IdSalvo => p.Id;

    public PedidoEditorForm(string tipo, long? id) : this(id is long existente ? Pedidos.Carregar(existente) : Pedidos.Novo(tipo))
    {
    }

    /// <summary>Abre um documento ainda não gravado, já preenchido (ex.: pedido de compra gerado pela sugestão do estoque).</summary>
    public PedidoEditorForm(Pedido pedido)
    {
        p = pedido;
        editavel = p.Status == "ABERTO";
        var compra = p.Tipo == Pedidos.Compra;

        Ui.Estilizar(this, Pedidos.Nome(p.Tipo) + (p.Id is long n ? $" nº {n}" : " (novo)") + (editavel ? "" : $" — {p.Status}"), 1150, 720, nameof(PedidoEditorForm));
        parcelas.Maximum = Parametros.MaxParcelas;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        qtd.Value = 1;
        parcelas.Width = 60;

        var topo = Ui.Barra();
        topo.Controls.AddRange([
            Ui.Rotulo(compra ? "Fornecedor:" : "Cliente:"), pessoa,
            Ui.Rotulo(p.Tipo == Pedidos.Orcamento ? "Válido até:" : "Previsão de entrega:"), validade,
            Ui.Rotulo($"Emissão: {p.Data:dd/MM/yyyy}"),
        ]);

        var entrada = Ui.Barra();
        entrada.Controls.AddRange([
            Ui.Rotulo("Código ou cód. barras:"), codigo, Ui.Rotulo("Qtd.:"), qtd,
            Ui.Botao("Adicionar (Enter)", (_, _) => Adicionar(), primario: true),
            Ui.Botao("Buscar produto (F3)", (_, _) => Adicionar(buscar: true)),
            Ui.Botao("Remover item (Del)", (_, _) => Remover()),
        ]);
        entrada.Visible = editavel;

        var condicoes = Ui.Barra(DockStyle.Bottom);
        condicoes.Controls.AddRange([
            Ui.Rotulo("Pagamento:"), forma, Ui.Rotulo("Parcelas:"), parcelas,
            Ui.Rotulo("Desconto geral R$:"), desconto, Ui.Rotulo("Frete R$:"), frete,
            Ui.Rotulo("Observações:"), observacoes,
        ]);

        var rodape = Ui.Barra(DockStyle.Bottom);
        var salvar = Ui.Botao("Salvar", (_, _) => Salvar(fechar: true), primario: p.Tipo == Pedidos.Orcamento);
        var concluir = Ui.Botao(p.Tipo == Pedidos.Compra ? "Salvar e receber" : "Salvar e faturar", (_, _) => SalvarEConcluir(), primario: true);
        salvar.Visible = editavel;
        concluir.Visible = editavel && p.Tipo != Pedidos.Orcamento;
        rodape.Controls.AddRange([
            concluir, salvar,
            Ui.Botao("Imprimir", (_, _) => Imprimir()),
            Ui.Botao("Fechar", (_, _) => Close()),
            lblTotal, lblResumo,
        ]);

        Controls.Add(grid);
        Controls.Add(entrada);
        Controls.Add(topo);
        Controls.Add(condicoes);
        Controls.Add(rodape);

        forma.Items.AddRange(compra ? FormasCompra : FormasVenda);
        grid.DataSource = p.Itens;
        grid.ReadOnly = !editavel;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in grid.Columns)
                c.ReadOnly = c.DataPropertyName is not (nameof(PedidoItem.Quantidade) or nameof(PedidoItem.Unitario) or nameof(PedidoItem.Desconto));
            if (compra && grid.Columns[nameof(PedidoItem.Unitario)] is DataGridViewColumn col)
                col.HeaderText = "Custo unit.";
        };
        grid.CellValidating += (_, e) =>
        {
            var nome = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (grid.ReadOnly || nome is not (nameof(PedidoItem.Quantidade) or nameof(PedidoItem.Unitario) or nameof(PedidoItem.Desconto)))
                return;
            if (!decimal.TryParse(Convert.ToString(e.FormattedValue), out var v) || v < 0 || (nome == nameof(PedidoItem.Quantidade) && v == 0))
            {
                Ui.Erro("Informe um valor válido.");
                e.Cancel = true;
            }
        };
        grid.CellEndEdit += (_, e) => { grid.InvalidateRow(e.RowIndex); AtualizarTotais(); };
        p.Itens.ListChanged += (_, _) => AtualizarTotais();
        desconto.ValueChanged += (_, _) => AtualizarTotais();
        frete.ValueChanged += (_, _) => AtualizarTotais();
        forma.SelectedIndexChanged += (_, _) => parcelas.Enabled = editavel && forma.Text == Operacoes.APrazo;
        codigo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Adicionar(); e.SuppressKeyPress = true; }
        };

        Load += (_, _) =>
        {
            if (compra)
                Ui.Preencher(pessoa, Db.Query("SELECT id, nome FROM pessoas WHERE (eh_fornecedor = 1 AND ativo = 1) OR id = @p0 ORDER BY nome", p.FornecedorId), "(selecione o fornecedor)");
            else
                Ui.Preencher(pessoa, Db.Query("SELECT id, nome FROM pessoas WHERE (eh_cliente = 1 AND ativo = 1) OR id = @p0 ORDER BY nome", p.ClienteId), "Consumidor final");
            Ui.Selecionar(pessoa, compra ? p.FornecedorId : p.ClienteId);
            validade.Value = p.Validade ?? DateTime.Today;
            forma.SelectedItem = forma.Items.Contains(p.Forma) ? p.Forma : forma.Items[0];
            parcelas.Value = Math.Clamp(p.Parcelas, 1, Parametros.MaxParcelas);
            desconto.Value = p.Desconto;
            frete.Value = p.Frete;
            observacoes.Text = p.Observacoes;
            foreach (var c in new Control[] { pessoa, validade, forma, parcelas, desconto, frete, observacoes })
                c.Enabled = editavel;
            AtualizarTotais();
        };
        Shown += (_, _) => codigo.Focus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F3 when editavel: Adicionar(buscar: true); return true;
            case Keys.Delete when editavel && grid.Focused: Remover(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void AtualizarTotais()
    {
        p.Desconto = desconto.Value;
        p.Frete = frete.Value;
        lblTotal.Text = "TOTAL " + Ui.Moeda(p.Total);
        lblResumo.Text = $"{p.Itens.Count} item(ns) · Subtotal {Ui.Moeda(p.Subtotal)}"
            + (p.DescontoItens + p.Desconto > 0 ? $" · Descontos {Ui.Moeda(p.DescontoItens + p.Desconto)}" : "")
            + (p.Frete > 0 ? $" · Frete {Ui.Moeda(p.Frete)}" : "");
    }

    void Adicionar(bool buscar = false)
    {
        var id = buscar ? BuscaProdutoForm.Escolher(this, codigo.Text.Trim()) : BuscaProdutoForm.Localizar(this, codigo.Text);
        if (id is not long produto)
            return;
        var r = Db.Query("SELECT codigo, nome, unidade, preco_venda, preco_custo FROM produtos WHERE id = @p0", produto).Rows[0];
        var item = p.Itens.FirstOrDefault(i => i.ProdutoId == produto);
        if (item != null)
        {
            item.Quantidade += qtd.Value;
            p.Itens.ResetItem(p.Itens.IndexOf(item));
        }
        else
            p.Itens.Add(new PedidoItem
            {
                ProdutoId = produto,
                Codigo = Ui.Txt(r["codigo"]),
                Produto = Ui.Txt(r["nome"]),
                Unidade = Ui.Txt(r["unidade"]),
                Quantidade = qtd.Value,
                Unitario = Ui.Dec(p.Tipo == Pedidos.Compra ? r["preco_custo"] : r["preco_venda"]),
            });
        codigo.Clear();
        qtd.Value = 1;
        codigo.Focus();
    }

    void Remover()
    {
        if (grid.CurrentRow?.DataBoundItem is PedidoItem i)
            p.Itens.Remove(i);
    }

    bool Salvar(bool fechar)
    {
        grid.EndEdit();
        if (p.Itens.Count == 0)
        {
            Ui.Erro("Adicione pelo menos um item.");
            return false;
        }
        var pessoaId = Ui.Id(pessoa);
        if (p.Tipo == Pedidos.Compra && pessoaId is null)
        {
            Ui.Erro("Selecione o fornecedor.");
            return false;
        }
        if (p.Total <= 0 && p.Subtotal > 0)
        {
            Ui.Erro("Os descontos não podem ser maiores que o valor dos itens.");
            return false;
        }
        if (p.Tipo != Pedidos.Compra && Parametros.ValidarDesconto(p.DescontoItens + p.Desconto, p.Subtotal) is string erroDesconto)
        {
            Ui.Erro(erroDesconto);
            return false;
        }
        if (p.Tipo == Pedidos.Compra) p.FornecedorId = pessoaId;
        else p.ClienteId = pessoaId;
        p.Validade = validade.Value.Date;
        p.Forma = forma.Text;
        p.Parcelas = forma.Text == Operacoes.APrazo ? (int)parcelas.Value : 1;
        p.Observacoes = observacoes.Text.Trim();
        try
        {
            Pedidos.Salvar(p);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
            return false;
        }
        if (fechar)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        return true;
    }

    /// <summary>Salva e já fatura (venda) ou recebe (compra).</summary>
    void SalvarEConcluir()
    {
        if (!Salvar(fechar: false))
            return;
        var ok = p.Tipo == Pedidos.Compra ? FluxoPedidos.Receber(this, p.Id!.Value) : FluxoPedidos.Faturar(this, p.Id!.Value);
        DialogResult = DialogResult.OK;
        if (ok)
            Close();
        else
            Ui.Info("O pedido foi salvo em aberto. Você pode concluí-lo depois pela lista de pedidos.");
    }

    void Imprimir()
    {
        if (editavel && !Salvar(fechar: false))
            return;
        Text = Pedidos.Nome(p.Tipo) + $" nº {p.Id}";
        CupomForm.Visualizar(this, Text, Pedidos.Texto(p.Id!.Value), 80);
    }
}
