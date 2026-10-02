namespace ErpLoja.Forms;

/// <summary>Conferência do XML da NF-e do fornecedor: vínculo dos itens com o cadastro e lançamento da compra.</summary>
public class ImportarXmlForm : Form
{
    const long Novo = -1;
    readonly NotaXml nota;
    readonly DataGridView grid = Ui.Grid(somenteLeitura: false);
    readonly CheckBox lancar = new() { Text = "Lançar compra (entrada no estoque, custo médio e contas a pagar)", AutoSize = true, Checked = true, Margin = new Padding(8, 10, 8, 4) };
    readonly ComboBox forma = Ui.Combo(170);

    ImportarXmlForm(NotaXml nota)
    {
        this.nota = nota;
        Ui.Estilizar(this, $"Importar NF-e nº {nota.Numero} — {nota.EmitNome}", 1250, 720, nameof(ImportarXmlForm));
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var cab = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 92,
            BackColor = Color.White,
            Padding = new Padding(12, 8, 56, 4),
            Text = $"Emitente: {nota.EmitNome}   CNPJ: {nota.EmitCnpj}   " +
                   (nota.FornecedorId is long f ? $"(fornecedor já cadastrado, nº {f})" : "(fornecedor será cadastrado automaticamente)") + "\n" +
                   $"NF-e nº {nota.Numero}  série {nota.Serie}  ·  emissão {nota.DataEmissao:dd/MM/yyyy}  ·  {nota.Natureza}\n" +
                   $"Chave: {Fiscal.FormatarChave(nota.Chave)}\n" +
                   $"Produtos {Ui.Moeda(nota.ValorProdutos)} · Frete {Ui.Moeda(nota.Frete)} · IPI {Ui.Moeda(nota.ValorIpi)} · " +
                   $"Desconto {Ui.Moeda(nota.Desconto)} · TOTAL {Ui.Moeda(nota.ValorTotal)}" +
                   (nota.Duplicatas.Count > 0 ? "   ·   Duplicatas: " + string.Join(", ", nota.Duplicatas.Select(d => $"{d.Vencimento:dd/MM} {Ui.Moeda(d.Valor)}")) : ""),
        };

        var dica = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(12, 6, 12, 0),
            ForeColor = Color.DimGray,
            Text = "Confira o produto do sistema para cada item (\"(cadastrar novo)\" cria o produto). Use \"Fator\" quando a unidade do fornecedor " +
                   "for diferente da sua (ex.: CX com 12 → fator 12). O custo inclui frete, IPI e ICMS-ST. Os vínculos ficam memorizados para as próximas notas.",
        };

        var rodape = Ui.Barra(DockStyle.Bottom);
        rodape.Controls.AddRange([
            Ui.Botao("Importar", (_, _) => Importar(), primario: true), Ui.Botao("Cancelar", (_, _) => Close()),
            lancar, Ui.Rotulo("Pagamento:"), forma,
        ]);

        Controls.Add(grid);
        Controls.Add(dica);
        Controls.Add(cab);
        Controls.Add(rodape);

        forma.Items.AddRange(PedidoEditorForm.FormasCompra);
        forma.SelectedItem = nota.Duplicatas.Count > 0 ? Operacoes.APrazo : "Dinheiro";
        lancar.CheckedChanged += (_, _) => forma.Enabled = lancar.Checked;

        MontarGrade();
    }

    void MontarGrade()
    {
        grid.AutoGenerateColumns = false;
        void Texto(string nome, string titulo, int peso, bool editavel = false, bool numero = false)
        {
            var c = new DataGridViewTextBoxColumn { Name = nome, HeaderText = titulo, FillWeight = peso, ReadOnly = !editavel };
            if (numero)
            {
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                c.DefaultCellStyle.Format = "N2";
                c.ValueType = typeof(decimal);
            }
            grid.Columns.Add(c);
        }
        Texto("cod", "Cód. fornecedor", 70);
        Texto("desc", "Descrição na nota", 180);
        Texto("un", "Un.", 35);
        Texto("qtd", "Qtd.", 55, numero: true);
        Texto("custo", "Custo total", 70, numero: true);

        var produtos = Db.Query("SELECT id, codigo || ' - ' || nome AS nome FROM produtos WHERE ativo = 1 ORDER BY nome");
        var linhaNovo = produtos.NewRow();
        linhaNovo["id"] = Novo;
        linhaNovo["nome"] = "(cadastrar novo)";
        produtos.Rows.InsertAt(linhaNovo, 0);
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "produto",
            HeaderText = "Produto no sistema",
            FillWeight = 220,
            DataSource = produtos,
            ValueMember = "id",
            DisplayMember = "nome",
            ValueType = typeof(long),
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
        });
        Texto("fator", "Fator", 45, editavel: true, numero: true);
        Texto("estoque", "Entra no estoque", 70, numero: true);

        foreach (var i in nota.Itens)
            grid.Rows.Add(i.CodigoFornecedor, i.Descricao, i.Unidade, i.Quantidade, i.CustoTotal, i.ProdutoId ?? Novo, i.Fator, i.Quantidade * i.Fator);

        grid.CellValidating += (_, e) =>
        {
            if (grid.Columns[e.ColumnIndex].Name == "fator" &&
                (!decimal.TryParse(Convert.ToString(e.FormattedValue), out var f) || f <= 0))
            {
                Ui.Erro("O fator deve ser maior que zero.");
                e.Cancel = true;
            }
        };
        grid.CellEndEdit += (_, e) =>
        {
            if (grid.Columns[e.ColumnIndex].Name == "fator")
                grid.Rows[e.RowIndex].Cells["estoque"].Value = nota.Itens[e.RowIndex].Quantidade * Ui.Dec(grid.Rows[e.RowIndex].Cells["fator"].Value);
        };
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.CurrentCell is DataGridViewComboBoxCell)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
    }

    void Importar()
    {
        grid.EndEdit();
        for (int r = 0; r < nota.Itens.Count; r++)
        {
            var produto = Convert.ToInt64(grid.Rows[r].Cells["produto"].Value);
            nota.Itens[r].ProdutoId = produto == Novo ? null : produto;
            nota.Itens[r].Fator = Ui.Dec(grid.Rows[r].Cells["fator"].Value);
        }
        var novos = nota.Itens.Count(i => i.ProdutoId is null);
        if (!Ui.Confirmar($"Importar a NF-e nº {nota.Numero}?" +
                          (novos > 0 ? $"\n\n{novos} produto(s) novo(s) serão cadastrados." : "") +
                          (lancar.Checked ? $"\nEntrada no estoque e contas a pagar em \"{forma.Text}\"." : "\nSomente registro da nota (sem estoque/financeiro).")))
            return;
        try
        {
            var (notaId, criados) = ImportacaoXml.Importar(nota, lancar.Checked, forma.Text);
            Ui.Info($"Nota importada (registro nº {notaId})." +
                    (criados > 0 ? $"\n\n{criados} produto(s) cadastrado(s) com preço de venda zerado — ajuste os preços em Cadastros > Produtos." : ""));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    /// <summary>Pede o arquivo, lê e abre a conferência. Devolve true se importou.</summary>
    public static bool Executar(IWin32Window dono)
    {
        using var dlg = new OpenFileDialog { Filter = "XML da NF-e (*.xml)|*.xml", Title = "Selecione o XML da nota do fornecedor" };
        if (dlg.ShowDialog(dono) != DialogResult.OK)
            return false;
        NotaXml nota;
        try
        {
            nota = ImportacaoXml.Ler(dlg.FileName);
        }
        catch (Exception ex)
        {
            Ui.Erro("Não foi possível ler o XML:\n" + ex.Message);
            return false;
        }
        if (ImportacaoXml.JaImportada(nota.Chave))
        {
            Ui.Erro(ImportacaoXml.MensagemJaImportada);
            return false;
        }
        ImportacaoXml.SugerirVinculos(nota);
        using var f = new ImportarXmlForm(nota);
        return f.ShowDialog(dono) == DialogResult.OK;
    }
}
