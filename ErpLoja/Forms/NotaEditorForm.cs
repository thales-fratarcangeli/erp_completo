namespace ErpLoja.Forms;

/// <summary>Digitação/conferência de uma nota fiscal própria (saída ou entrada).</summary>
public class NotaEditorForm : Form
{
    static readonly string[] CfopsPadrao = ["5102", "6102", "1102", "2102"];

    readonly NotaFiscal n;
    readonly bool editavel;
    readonly DataGridView grid = Ui.Grid(somenteLeitura: false);
    readonly ComboBox modelo = Ui.Combo(140), cadastro = Ui.Combo(260), uf = Ui.Combo(70);
    readonly NumericUpDown serie = Ui.Numero(0, 999, 1), frete = Ui.Numero(), desconto = Ui.Numero(), qtd = Ui.Numero(3);
    readonly TextBox natureza = Caixa(260), nome = Caixa(260), documento = Caixa(150), ie = Caixa(120),
        endereco = Caixa(360), cidade = Caixa(180), info = Caixa(600), codigo = Caixa(180);
    readonly Label lblTotais = Ui.Rotulo("");
    readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Ui.Primaria, Margin = new Padding(16, 4, 8, 0) };
    bool carregando = true;

    public long? IdSalvo => n.Id;

    static TextBox Caixa(int largura) => new() { Width = largura, Margin = new Padding(4, 7, 4, 4) };

    public NotaEditorForm(NotaFiscal nota)
    {
        n = nota;
        editavel = n.Status == "DIGITADA";
        var saida = n.Tipo == Fiscal.Saida;
        var titulo = $"Nota fiscal de {(saida ? "saída" : "entrada")}"
            + (n.Numero is long num ? $" nº {num}" : n.Id is long id ? $" (rascunho {id})" : " (nova)")
            + (editavel ? "" : $" — {n.Status}");
        Ui.Estilizar(this, titulo, 1250, 760, nameof(NotaEditorForm));
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        qtd.Value = 1;
        serie.Width = 60;

        var cabecalho = Ui.Barra();
        var referencia = n.VendaId is long v ? $"Venda nº {v}" : n.CompraId is long c ? $"Compra nº {c}" : "Avulsa";
        cabecalho.Controls.AddRange([
            Ui.Rotulo("Modelo:"), modelo, Ui.Rotulo("Série:"), serie, Ui.Rotulo("Natureza da operação:"), natureza,
            Ui.Rotulo("Origem: " + referencia),
        ]);

        var participante = Ui.Barra();
        participante.Controls.AddRange([
            Ui.Rotulo(saida ? "Destinatário:" : "Remetente:"), cadastro, Ui.Rotulo("Nome:"), nome,
            Ui.Rotulo("CPF/CNPJ:"), documento, Ui.Rotulo("IE:"), ie,
            Ui.Rotulo("Endereço:"), endereco, Ui.Rotulo("Cidade:"), cidade, Ui.Rotulo("UF:"), uf,
        ]);
        participante.SetFlowBreak(ie, true);

        var entrada = Ui.Barra();
        entrada.Controls.AddRange([
            Ui.Rotulo("Produto (código):"), codigo, Ui.Rotulo("Qtd.:"), qtd,
            Ui.Botao("Adicionar", (_, _) => Adicionar(), primario: true),
            Ui.Botao("Buscar (F3)", (_, _) => Adicionar(buscar: true)),
            Ui.Botao("Remover item", (_, _) => Remover()),
            Ui.Rotulo("Edite NCM, CFOP, CST e alíquotas direto na grade."),
        ]);
        entrada.Visible = editavel;

        var adicionais = Ui.Barra(DockStyle.Bottom);
        adicionais.Controls.AddRange([
            Ui.Rotulo("Frete R$:"), frete, Ui.Rotulo("Desconto adicional R$:"), desconto,
            Ui.Rotulo("Informações complementares:"), info,
        ]);

        var rodape = Ui.Barra(DockStyle.Bottom);
        var salvar = Ui.Botao("Salvar rascunho", (_, _) => Salvar(emitir: false));
        var emitir = Ui.Botao("Salvar e emitir", (_, _) => Salvar(emitir: true), primario: true);
        salvar.Visible = emitir.Visible = editavel;
        rodape.Controls.AddRange([emitir, salvar, Ui.Botao("Fechar", (_, _) => Close()), lblTotal, lblTotais]);

        Controls.Add(grid);
        Controls.Add(entrada);
        Controls.Add(participante);
        Controls.Add(cabecalho);
        Controls.Add(adicionais);
        Controls.Add(rodape);

        modelo.Items.AddRange(["55 - NF-e", "65 - NFC-e"]);
        uf.Items.Add("");
        uf.Items.AddRange(Fiscal.Ufs);
        grid.DataSource = n.Itens;
        grid.ReadOnly = !editavel;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                // colunas de valores calculados (Total, ICMS, IPI, IBS, CBS...) já vêm somente leitura do binding:
                // nunca forçar ReadOnly = false nelas (o WinForms lança exceção)
                col.MinimumWidth = col.DataPropertyName == nameof(NotaItem.Descricao) ? 200 : 60;
            }
        };
        grid.CellEndEdit += (_, e) => { grid.InvalidateRow(e.RowIndex); AtualizarTotais(); };
        n.Itens.ListChanged += (_, _) => AtualizarTotais();
        frete.ValueChanged += (_, _) => AtualizarTotais();
        desconto.ValueChanged += (_, _) => AtualizarTotais();
        cadastro.SelectedIndexChanged += (_, _) => TrocarParticipante();
        uf.SelectedIndexChanged += (_, _) => AjustarCfops();
        codigo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Adicionar(); e.SuppressKeyPress = true; }
        };

        Load += (_, _) =>
        {
            Ui.Preencher(cadastro, saida
                ? Db.Query("SELECT id, nome FROM pessoas WHERE (eh_cliente = 1 AND ativo = 1) OR id = @p0 ORDER BY nome", n.ClienteId)
                : Db.Query("SELECT id, nome FROM pessoas WHERE (eh_fornecedor = 1 AND ativo = 1) OR id = @p0 ORDER BY nome", n.FornecedorId),
                saida ? "(consumidor / digitar)" : "(digitar)");
            Ui.Selecionar(cadastro, saida ? n.ClienteId : n.FornecedorId);
            modelo.SelectedIndex = n.Modelo == "65" ? 1 : 0;
            serie.Value = Math.Clamp(n.Serie, 0, 999);
            natureza.Text = n.Natureza;
            MostrarParticipante();
            frete.Value = n.Frete;
            desconto.Value = n.Desconto;
            info.Text = n.Info;
            foreach (var ctl in new Control[] { modelo, serie, natureza, cadastro, nome, documento, ie, endereco, cidade, uf, frete, desconto, info })
                ctl.Enabled = editavel;
            carregando = false;
            AtualizarTotais();
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F3 && editavel)
        {
            Adicionar(buscar: true);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void MostrarParticipante()
    {
        nome.Text = n.PartNome;
        documento.Text = n.PartDocumento;
        ie.Text = n.PartIe;
        endereco.Text = n.PartEndereco;
        cidade.Text = n.PartCidade;
        uf.SelectedItem = uf.Items.Contains(n.PartUf) ? n.PartUf : "";
    }

    void TrocarParticipante()
    {
        if (carregando)
            return;
        Fiscal.PreencherParticipante(n, Ui.Id(cadastro));
        MostrarParticipante();
    }

    /// <summary>Ao mudar a UF, troca 5102↔6102 (1102↔2102) nos itens que estão com o CFOP padrão.</summary>
    void AjustarCfops()
    {
        if (carregando)
            return;
        var cfop = Fiscal.CfopPadrao(n.Tipo, uf.Text);
        foreach (var i in n.Itens.Where(i => CfopsPadrao.Contains(i.Cfop) || i.Cfop.Length == 0))
            i.Cfop = cfop;
        grid.Refresh();
    }

    void AtualizarTotais()
    {
        n.Frete = frete.Value;
        n.Desconto = desconto.Value;
        lblTotal.Text = "TOTAL DA NOTA " + Ui.Moeda(n.ValorTotal);
        lblTotais.Text = $"Produtos {Ui.Moeda(n.ValorProdutos)} · Desconto {Ui.Moeda(n.DescontoTotal)} · Frete {Ui.Moeda(n.Frete)} · " +
                         $"ICMS {Ui.Moeda(n.ValorIcms)} · IPI {Ui.Moeda(n.ValorIpi)} · PIS {Ui.Moeda(n.ValorPis)} · COFINS {Ui.Moeda(n.ValorCofins)}";
    }

    void Adicionar(bool buscar = false)
    {
        var id = buscar ? BuscaProdutoForm.Escolher(this, codigo.Text.Trim()) : BuscaProdutoForm.Localizar(this, codigo.Text);
        if (id is not long produto)
            return;
        var p = Db.Query("SELECT preco_venda, preco_custo FROM produtos WHERE id = @p0", produto).Rows[0];
        var preco = Ui.Dec(n.Tipo == Fiscal.Saida ? p["preco_venda"] : p["preco_custo"]);
        n.Itens.Add(Fiscal.ItemDeProduto(produto, qtd.Value, preco, Fiscal.CfopPadrao(n.Tipo, uf.Text)));
        codigo.Clear();
        qtd.Value = 1;
        codigo.Focus();
    }

    void Remover()
    {
        if (grid.CurrentRow?.DataBoundItem is NotaItem i)
            n.Itens.Remove(i);
    }

    void Salvar(bool emitir)
    {
        grid.EndEdit();
        n.Modelo = modelo.SelectedIndex == 1 ? "65" : "55";
        n.Serie = (int)serie.Value;
        n.Natureza = natureza.Text.Trim();
        n.PartNome = nome.Text.Trim();
        n.PartDocumento = documento.Text.Trim();
        n.PartIe = ie.Text.Trim();
        n.PartEndereco = endereco.Text.Trim();
        n.PartCidade = cidade.Text.Trim();
        n.PartUf = uf.Text;
        n.Info = info.Text.Trim();

        if (n.Itens.Count == 0)
        {
            Ui.Erro("Adicione pelo menos um item.");
            return;
        }
        if (emitir)
        {
            var erros = Fiscal.Validar(n);
            if (erros.Count > 0)
            {
                Ui.Erro("Corrija antes de emitir:\n\n" + string.Join("\n", erros.Take(12)) +
                        (erros.Count > 12 ? $"\n... e mais {erros.Count - 12}" : "") +
                        "\n\nDica: preencha NCM/CST no cadastro de produtos para não precisar digitar toda vez.");
                return;
            }
            if (!Ui.Confirmar("Emitir a nota? A numeração passa a ser definitiva e a nota não poderá mais ser alterada."))
                return;
        }
        try
        {
            var id = Fiscal.Salvar(n);
            if (emitir)
            {
                Fiscal.Emitir(id);
                CupomForm.Visualizar(this, "DANFE", Fiscal.Danfe(id), 100);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }
}
