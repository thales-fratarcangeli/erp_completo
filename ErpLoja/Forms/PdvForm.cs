namespace ErpLoja.Forms;

/// <summary>Linha da grade de itens do PDV.</summary>
public class ItemLinha
{
    [Browsable(false)] public long ProdutoId { get; set; }
    [DisplayName("Código")] public string Codigo { get; set; } = "";
    [DisplayName("Produto")] public string Produto { get; set; } = "";
    [DisplayName("Un.")] public string Unidade { get; set; } = "";
    [DisplayName("Qtd.")] public decimal Quantidade { get; set; }
    [DisplayName("Preço unit.")] public decimal Unitario { get; set; }
    [DisplayName("Total")] public decimal Total => Math.Round(Quantidade * Unitario, 2);
    [Browsable(false)] public decimal Custo { get; set; }
    [Browsable(false)] public decimal Estoque { get; set; }
}

/// <summary>PDV: venda de balcão.</summary>
public class PdvForm : Form
{
    readonly BindingList<ItemLinha> itens = new();
    readonly DataGridView grid = Ui.Grid(somenteLeitura: false);
    readonly ComboBox cliente = Ui.Combo(320), forma = Ui.Combo(170);
    readonly TextBox codigo = new() { Width = 220, Margin = new Padding(4, 7, 4, 4) };
    readonly NumericUpDown qtd = Ui.Numero(3), desconto = Ui.Numero(), recebido = Ui.Numero(), parcelas = Ui.Numero(0, 60, 1);
    readonly DateTimePicker vencimento = Ui.Data();
    readonly Label lblResumo = Ui.Rotulo(""), lblTroco = Ui.Rotulo("");
    readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), ForeColor = Ui.Primaria, Margin = new Padding(16, 4, 16, 0) };

    public PdvForm()
    {
        Ui.Estilizar(this, "PDV - venda no balcão", 1150, 720);
        qtd.Value = 1;
        parcelas.Width = 60;
        parcelas.Maximum = Parametros.MaxParcelas;

        var topo = Ui.Barra();
        topo.Controls.AddRange([Ui.Rotulo("Cliente:"), cliente]);

        var entrada = Ui.Barra();
        entrada.Controls.AddRange([
            Ui.Rotulo("Código ou cód. barras:"), codigo, Ui.Rotulo("Qtd.:"), qtd,
            Ui.Botao("Adicionar (Enter)", (_, _) => Adicionar(), primario: true),
            Ui.Botao("Buscar produto (F3)", (_, _) => Adicionar(buscar: true)),
            Ui.Botao("Remover item (Del)", (_, _) => Remover()),
        ]);

        var pagamento = Ui.Barra(DockStyle.Bottom);
        pagamento.Controls.AddRange([
            Ui.Rotulo("Pagamento:"), forma, Ui.Rotulo("Parcelas:"), parcelas, Ui.Rotulo("1º vencimento:"), vencimento,
            Ui.Rotulo("Desconto R$:"), desconto, Ui.Rotulo("Valor recebido R$:"), recebido,
        ]);

        var totais = Ui.Barra(DockStyle.Bottom);
        totais.Controls.AddRange([
            Ui.Botao("Finalizar (F10)", (_, _) => Finalizar(), primario: true),
            Ui.Botao("Limpar", (_, _) => Limpar(perguntar: true)),
            lblTotal, lblResumo, lblTroco,
        ]);

        Controls.Add(grid);
        Controls.Add(entrada);
        Controls.Add(topo);
        Controls.Add(pagamento);
        Controls.Add(totais);

        forma.Items.AddRange(["Dinheiro", "PIX", "Cartão de débito", "Cartão de crédito", Operacoes.APrazo]);

        grid.DataSource = itens;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in grid.Columns)
                c.ReadOnly = c.DataPropertyName is not (nameof(ItemLinha.Quantidade) or nameof(ItemLinha.Unitario));
        };
        grid.CellValidating += (_, e) =>
        {
            var nome = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (nome is not (nameof(ItemLinha.Quantidade) or nameof(ItemLinha.Unitario)))
                return;
            if (!decimal.TryParse(Convert.ToString(e.FormattedValue), out var v) || v < 0 || (nome == nameof(ItemLinha.Quantidade) && v == 0))
            {
                Ui.Erro("Informe um valor válido (maior que zero).");
                e.Cancel = true;
            }
        };
        grid.CellEndEdit += (_, e) => { grid.InvalidateRow(e.RowIndex); AtualizarTotais(); };

        itens.ListChanged += (_, _) => AtualizarTotais();
        desconto.ValueChanged += (_, _) => AtualizarTotais();
        recebido.ValueChanged += (_, _) => AtualizarTotais();
        forma.SelectedIndexChanged += (_, _) =>
        {
            parcelas.Enabled = vencimento.Enabled = forma.Text == Operacoes.APrazo;
            recebido.Enabled = EmDinheiro;
            AtualizarTotais();
        };
        codigo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Adicionar(); e.SuppressKeyPress = true; }
        };

        Load += (_, _) =>
        {
            Ui.Preencher(cliente, Db.Query("SELECT id, nome FROM clientes WHERE ativo = 1 ORDER BY nome"), "Consumidor final");
            Limpar(perguntar: false);
        };
        Shown += (_, _) => codigo.Focus();
        FormClosing += (_, e) =>
        {
            if (itens.Count > 0 && !Ui.Confirmar("Há itens lançados que não foram finalizados. Descartar e fechar?"))
                e.Cancel = true;
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F3: Adicionar(buscar: true); return true;
            case Keys.F10: Finalizar(); return true;
            case Keys.Delete when grid.Focused: Remover(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    decimal Subtotal => itens.Sum(i => i.Total);
    decimal Total => Math.Max(0, Subtotal - desconto.Value);
    bool EmDinheiro => forma.Text == "Dinheiro";

    void AtualizarTotais()
    {
        lblTotal.Text = "TOTAL " + Ui.Moeda(Total);
        lblResumo.Text = $"{itens.Count} item(ns) · Subtotal {Ui.Moeda(Subtotal)}"
            + (desconto.Value > 0 ? $" · Desconto {Ui.Moeda(desconto.Value)}" : "");
        lblTroco.Text = EmDinheiro && recebido.Value > 0 ? "Troco: " + Ui.Moeda(Math.Max(0, recebido.Value - Total)) : "";
    }

    void Adicionar(bool buscar = false)
    {
        var id = buscar ? BuscaProdutoForm.Escolher(this, codigo.Text.Trim()) : BuscaProdutoForm.Localizar(this, codigo.Text);
        if (id is not long produto)
            return;
        var quantidade = qtd.Value;
        if (quantidade <= 0)
        {
            Ui.Erro("Informe a quantidade.");
            return;
        }
        var p = Db.Query("SELECT codigo, nome, unidade, preco_venda, preco_custo, estoque FROM produtos WHERE id = @p0", produto).Rows[0];
        var item = itens.FirstOrDefault(i => i.ProdutoId == produto);
        var estoque = Ui.Dec(p["estoque"]);
        if ((item?.Quantidade ?? 0) + quantidade > estoque &&
            !Parametros.PodeVenderSemEstoque($"Estoque insuficiente para \"{p["nome"]}\" (disponível: {estoque:N2})."))
            return;

        if (item != null)
        {
            item.Quantidade += quantidade;
            itens.ResetItem(itens.IndexOf(item));
        }
        else
            itens.Add(new ItemLinha
            {
                ProdutoId = produto,
                Codigo = Ui.Txt(p["codigo"]),
                Produto = Ui.Txt(p["nome"]),
                Unidade = Ui.Txt(p["unidade"]),
                Quantidade = quantidade,
                Unitario = Ui.Dec(p["preco_venda"]),
                Custo = Ui.Dec(p["preco_custo"]),
                Estoque = estoque,
            });
        codigo.Clear();
        qtd.Value = 1;
        codigo.Focus();
    }

    void Remover()
    {
        if (grid.CurrentRow?.DataBoundItem is ItemLinha i)
            itens.Remove(i);
    }

    void Limpar(bool perguntar)
    {
        if (perguntar && itens.Count > 0 && !Ui.Confirmar("Descartar os itens lançados?"))
            return;
        itens.Clear();
        desconto.Value = recebido.Value = 0;
        parcelas.Value = 1;
        vencimento.Value = DateTime.Today.AddDays(Parametros.PrazoVencimentoDias);
        if (cliente.Items.Count > 0)
            cliente.SelectedIndex = 0;
        forma.SelectedIndex = 0;
        AtualizarTotais();
        codigo.Focus();
    }

    void Finalizar()
    {
        grid.EndEdit();
        if (itens.Count == 0)
        {
            Ui.Erro("Adicione pelo menos um item.");
            return;
        }
        var clienteId = Ui.Id(cliente);
        var aPrazo = forma.Text == Operacoes.APrazo;
        if (aPrazo && clienteId is null)
        {
            Ui.Erro("Para vender a prazo, selecione o cliente.");
            return;
        }
        if (desconto.Value > Subtotal)
        {
            Ui.Erro("O desconto não pode ser maior que o subtotal.");
            return;
        }
        if (Parametros.ValidarDesconto(desconto.Value, Subtotal) is string erroDesconto)
        {
            Ui.Erro(erroDesconto);
            return;
        }
        if (EmDinheiro && recebido.Value > 0 && recebido.Value < Total)
        {
            Ui.Erro("O valor recebido é menor que o total.");
            return;
        }
        var texto = $"Confirmar venda de {Ui.Moeda(Total)} em {forma.Text}"
            + (aPrazo ? $" ({parcelas.Value:N0}x, 1º vencimento {vencimento.Value:dd/MM/yyyy})" : "") + "?";
        if (!Ui.Confirmar(texto))
            return;

        try
        {
            var troco = EmDinheiro && recebido.Value > 0 ? recebido.Value - Total : 0;
            var lista = itens.Select(i => new ItemDoc(i.ProdutoId, i.Quantidade, i.Unitario, i.Custo)).ToList();
            var id = Operacoes.RegistrarVenda(clienteId, lista, desconto.Value, forma.Text, (int)parcelas.Value, vencimento.Value.Date);
            itens.Clear();
            var msg = $"Venda nº {id} registrada." + (troco > 0 ? $"\n\nTROCO: {Ui.Moeda(troco)}" : "")
                      + "\n\nDeseja ver/imprimir o comprovante?";
            if (Ui.Confirmar(msg))
                CupomForm.Mostrar(this, id);
            Limpar(perguntar: false);
        }
        catch (Exception ex)
        {
            Ui.Erro("Erro ao registrar: " + ex.Message);
        }
    }
}
