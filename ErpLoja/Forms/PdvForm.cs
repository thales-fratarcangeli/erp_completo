using System.Globalization;
using System.Text.RegularExpressions;

namespace ErpLoja.Forms;

/// <summary>Linha da grade de itens do PDV.</summary>
[System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
public class ItemLinha
{
    [DisplayName("#")] public int Seq { get; set; }
    [Browsable(false)] public long ProdutoId { get; set; }
    [DisplayName("Código")] public string Codigo { get; set; } = "";
    [DisplayName("Descrição")] public string Produto { get; set; } = "";
    [DisplayName("Qtd.")] public decimal Quantidade { get; set; }
    [DisplayName("Un.")] public string Unidade { get; set; } = "";
    [DisplayName("Preço")] public decimal Unitario { get; set; }
    [DisplayName("Total")] public decimal Total => Math.Round(Quantidade * Unitario, 2);
    [Browsable(false)] public decimal Custo { get; set; }
}

/// <summary>PDV: frente de caixa em tela cheia, operado pelo teclado.</summary>
public class PdvForm : Form
{
    // tema claro: fundo branco, cartões cinza-claro, texto escuro; verde e âmbar indicam a situação do caixa
    static readonly Color Fundo = Color.White;
    static readonly Color Barra = ColorTranslator.FromHtml("#f8fafc");
    static readonly Color Cartao = ColorTranslator.FromHtml("#f1f5f9");
    static readonly Color Linha = ColorTranslator.FromHtml("#e2e8f0");
    static readonly Color Suave = ColorTranslator.FromHtml("#64748b");
    static readonly Color Texto = ColorTranslator.FromHtml("#0f172a");
    static readonly Color Verde = ColorTranslator.FromHtml("#15803d");
    static readonly Color VerdeClaro = ColorTranslator.FromHtml("#dcfce7");
    static readonly Color Ambar = ColorTranslator.FromHtml("#fde68a");

    readonly BindingList<ItemLinha> itens = new();
    readonly DataGridView grid = new();
    readonly TextBox codigo = new();
    readonly Label lblUltimo = Rotulo("Caixa livre", 18F, Texto, negrito: true);
    readonly Label lblUltimoCalculo = Rotulo("Leia o código de barras ou digite o código do produto", 12F, Suave);
    readonly Label lblCliente = Rotulo("Consumidor final", 12F, Texto);
    readonly Label lblItens = Rotulo("0 itens", 12F, Suave);
    readonly Label lblSubtotal = Rotulo("R$ 0,00", 12F, Texto);
    readonly Label lblDesconto = Rotulo("R$ 0,00", 12F, Texto);
    readonly Label lblTotal = Rotulo("R$ 0,00", 40F, Verde, negrito: true);
    readonly Label lblSituacao = new() { AutoSize = false, Size = new Size(210, 30), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 10F), ForeColor = Color.Black };
    readonly Label lblRelogio = Rotulo("", 14F, Texto, negrito: true);
    readonly Label lblQuantidade = Rotulo("Qtd. 1", 11F, ColorTranslator.FromHtml("#b45309"), negrito: true);
    readonly Panel painelTotal = new() { Dock = DockStyle.Bottom, Height = 132, BackColor = VerdeClaro, Padding = new Padding(22, 14, 22, 10) };
    readonly System.Windows.Forms.Timer relogio = new() { Interval = 1000 };

    decimal proximaQuantidade = 1, desconto;
    long? clienteId;

    static Label Rotulo(string texto, float tamanho, Color cor, bool negrito = false) => new()
    {
        Text = texto,
        AutoSize = true,
        ForeColor = cor,
        Font = new Font(negrito ? "Segoe UI Semibold" : "Segoe UI", tamanho),
        BackColor = Color.Transparent,
    };

    public PdvForm()
    {
        Ui.Estilizar(this, "PDV - frente de caixa", 1280, 760, nameof(PdvForm));
        WindowState = FormWindowState.Maximized;
        BackColor = Fundo;
        ForeColor = Texto;

        // ---------------- topo
        var topo = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Barra, Padding = new Padding(16, 0, 64, 0) };
        var marca = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, BackColor = Barra, Padding = new Padding(0, 12, 0, 0) };
        marca.Controls.Add(new PictureBox { Image = Ui.Logo(40), Size = new Size(40, 40), Margin = new Padding(0, 0, 12, 0) });
        var identificacao = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Barra, Margin = new Padding(0) };
        identificacao.Controls.Add(Rotulo(Db.Config("nome_loja"), 13F, Texto, negrito: true));
        identificacao.Controls.Add(Rotulo($"CAIXA {Parametros.CaixaNumero}   ·   Operador: {Sessao.Nome}", 9.5F, Suave));
        marca.Controls.Add(identificacao);
        var direita = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = Barra, Padding = new Padding(0, 17, 0, 0) };
        lblSituacao.Margin = new Padding(0, 0, 18, 0);
        direita.Controls.Add(lblSituacao);
        direita.Controls.Add(lblRelogio);
        topo.Controls.Add(direita);
        topo.Controls.Add(marca);

        // ---------------- barra de teclas
        var teclas = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, BackColor = Barra, Padding = new Padding(12, 11, 12, 0), WrapContents = false };
        foreach (var (tecla, texto, acao) in new (string, string, Action)[]
                 {
                     ("F2", "Cliente", EscolherCliente), ("F3", "Buscar", () => Adicionar(buscar: true)), ("F4", "Quantidade", DefinirQuantidade),
                     ("F5", "Desconto", DarDesconto), ("F6", "Remover item", Remover), ("F7", "Cancelar venda", () => Limpar(perguntar: true)),
                     ("F10", "Pagamento", Pagar), ("Esc", "Sair", Close),
                 })
            teclas.Controls.Add(Tecla(tecla, texto, acao, tecla == "F10"));

        // ---------------- painel direito
        var lateral = new Panel { Dock = DockStyle.Right, Width = 430, BackColor = Fundo, Padding = new Padding(14, 14, 16, 14) };
        var entrada = new Panel { Dock = DockStyle.Top, Height = 98, BackColor = Cartao, Padding = new Padding(18, 10, 18, 10) };
        codigo.Dock = DockStyle.Bottom;
        codigo.Font = new Font("Segoe UI", 20F);
        codigo.BackColor = Color.White;
        codigo.ForeColor = Texto;
        codigo.BorderStyle = BorderStyle.FixedSingle;
        codigo.PlaceholderText = "código, código de barras ou 3*código";
        var cabecalhoEntrada = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Cartao };
        cabecalhoEntrada.Controls.Add(lblQuantidade);
        lblQuantidade.Dock = DockStyle.Right;
        var rotuloEntrada = Rotulo("PRODUTO", 9.5F, Suave, negrito: true);
        rotuloEntrada.Dock = DockStyle.Left;
        cabecalhoEntrada.Controls.Add(rotuloEntrada);
        entrada.Controls.Add(codigo);
        entrada.Controls.Add(cabecalhoEntrada);

        var ultimo = new Panel { Dock = DockStyle.Top, Height = 116, BackColor = Cartao, Padding = new Padding(18, 12, 18, 10), Margin = new Padding(0, 12, 0, 0) };
        var rotuloUltimo = Rotulo("ÚLTIMO ITEM", 9.5F, Suave, negrito: true);
        rotuloUltimo.Dock = DockStyle.Top;
        lblUltimo.Dock = DockStyle.Top;
        lblUltimo.AutoSize = false;
        lblUltimo.Height = 38;
        lblUltimo.AutoEllipsis = true;
        lblUltimoCalculo.Dock = DockStyle.Top;
        ultimo.Controls.Add(lblUltimoCalculo);
        ultimo.Controls.Add(lblUltimo);
        ultimo.Controls.Add(rotuloUltimo);

        var resumo = new TableLayoutPanel { Dock = DockStyle.Top, Height = 132, ColumnCount = 2, BackColor = Fundo, Padding = new Padding(4, 14, 4, 0) };
        resumo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        resumo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        void LinhaResumo(string nome, Label valor)
        {
            resumo.Controls.Add(Rotulo(nome, 12F, Suave));
            valor.Anchor = AnchorStyles.Right;
            resumo.Controls.Add(valor);
        }
        LinhaResumo("Cliente (F2)", lblCliente);
        LinhaResumo("Itens", lblItens);
        LinhaResumo("Subtotal", lblSubtotal);
        LinhaResumo("Desconto (F5)", lblDesconto);

        var rotuloTotal = Rotulo("TOTAL A PAGAR", 11F, Suave, negrito: true);
        rotuloTotal.Dock = DockStyle.Top;
        lblTotal.Dock = DockStyle.Fill;
        lblTotal.AutoSize = false;
        lblTotal.TextAlign = ContentAlignment.MiddleRight;
        painelTotal.Controls.Add(lblTotal);
        painelTotal.Controls.Add(rotuloTotal);

        var espaco1 = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Fundo };
        lateral.Controls.Add(painelTotal);
        lateral.Controls.Add(resumo);
        lateral.Controls.Add(ultimo);
        lateral.Controls.Add(espaco1);
        lateral.Controls.Add(entrada);

        // ---------------- grade
        var centro = new Panel { Dock = DockStyle.Fill, BackColor = Fundo, Padding = new Padding(16, 14, 0, 14) };
        EstilizarGrade();
        centro.Controls.Add(grid);

        Controls.Add(centro);
        Controls.Add(lateral);
        Controls.Add(teclas);
        Controls.Add(topo);

        itens.ListChanged += (_, _) => AtualizarTotais();
        codigo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Adicionar();
            }
        };
        relogio.Tick += (_, _) => lblRelogio.Text = DateTime.Now.ToString("HH:mm:ss");
        Load += (_, _) =>
        {
            relogio.Start();
            lblRelogio.Text = DateTime.Now.ToString("HH:mm:ss");
            AtualizarTotais();
        };
        Shown += (_, _) => codigo.Focus();
        FormClosing += (_, e) =>
        {
            if (itens.Count > 0 && !Ui.Confirmar("Há uma venda em andamento. Cancelar a venda e sair do PDV?"))
                e.Cancel = true;
        };
        FormClosed += (_, _) => relogio.Dispose();
    }

    Control Tecla(string tecla, string texto, Action acao, bool destaque)
    {
        var p = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Barra, Margin = new Padding(0, 0, 18, 0), Cursor = Cursors.Hand };
        var tampa = new Label
        {
            Text = tecla,
            AutoSize = false,
            Size = new Size(tecla.Length > 2 ? 46 : 38, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10F),
            BackColor = destaque ? ColorTranslator.FromHtml("#16a34a") : Color.White,
            ForeColor = destaque ? Color.White : Texto,
            BorderStyle = destaque ? BorderStyle.None : BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 6, 0),
        };
        var legenda = Rotulo(texto, 10.5F, Texto);
        legenda.Margin = new Padding(0, 5, 0, 0);
        p.Controls.Add(tampa);
        p.Controls.Add(legenda);
        foreach (Control c in new Control[] { p, tampa, legenda })
            c.Click += (_, _) => acao();
        Animacao.Hover(tampa, tampa.BackColor, Animacao.Clarear(tampa.BackColor, 0.25));
        return p;
    }

    void EstilizarGrade()
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Fundo;
        grid.GridColor = Linha;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Cartao;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Suave;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Cartao;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F);
        grid.DefaultCellStyle.BackColor = Fundo;
        grid.DefaultCellStyle.ForeColor = Texto;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 13F);
        grid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#dbeafe");
        grid.DefaultCellStyle.SelectionForeColor = Texto;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Barra;
        grid.RowTemplate.Height = 40;
        grid.DataSource = itens;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (c.ValueType == typeof(decimal))
                {
                    c.DefaultCellStyle.Format = c.DataPropertyName == nameof(ItemLinha.Quantidade) ? "N3" : "N2";
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                c.FillWeight = c.DataPropertyName switch
                {
                    nameof(ItemLinha.Produto) => 300,
                    nameof(ItemLinha.Seq) => 30,
                    nameof(ItemLinha.Unidade) => 40,
                    _ => 90,
                };
            }
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F2: EscolherCliente(); return true;
            case Keys.F3: Adicionar(buscar: true); return true;
            case Keys.F4: DefinirQuantidade(); return true;
            case Keys.F5: DarDesconto(); return true;
            case Keys.F6: Remover(); return true;
            case Keys.F7: Limpar(perguntar: true); return true;
            case Keys.F10: Pagar(); return true;
            case Keys.Escape: Close(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    decimal Subtotal => itens.Sum(i => i.Total);
    decimal Total => Math.Max(0, Subtotal - desconto);

    void AtualizarTotais()
    {
        var antes = lblTotal.Text;
        lblTotal.Text = Ui.Moeda(Total);
        lblSubtotal.Text = Ui.Moeda(Subtotal);
        lblDesconto.Text = desconto > 0 ? "- " + Ui.Moeda(desconto) : Ui.Moeda(0);
        lblItens.Text = $"{itens.Count} item(ns)";
        lblQuantidade.Text = $"Qtd. {proximaQuantidade:0.###}";
        var emVenda = itens.Count > 0;
        lblSituacao.Text = emVenda ? "VENDA EM ANDAMENTO" : "CAIXA LIVRE";
        lblSituacao.BackColor = emVenda ? Ambar : VerdeClaro;
        lblSituacao.ForeColor = emVenda ? ColorTranslator.FromHtml("#78350f") : Verde;
        if (antes != lblTotal.Text && emVenda)
            Animacao.Pulsar(painelTotal, ColorTranslator.FromHtml("#bbf7d0"), 450);
    }

    const decimal QuantidadeMaxima = 99_999;

    static readonly Regex Multiplicador = new(@"^\s*(\d+(?:[.,]\d+)?)\s*[*xX]\s*(.+)$");

    void Adicionar(bool buscar = false)
    {
        var texto = codigo.Text.Trim();
        var quantidade = proximaQuantidade;
        if (Multiplicador.Match(texto) is { Success: true } m)
        {
            // quantidade digitada errada (ex.: código lido duas vezes) não pode derrubar o caixa
            if (!decimal.TryParse(m.Groups[1].Value.Replace('.', ','), NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out quantidade) ||
                quantidade > QuantidadeMaxima)
            {
                Ui.Erro($"Quantidade \"{m.Groups[1].Value}\" inválida: use de 0,001 a {QuantidadeMaxima:N0} (ex.: 3*código).");
                codigo.SelectAll();
                return;
            }
            texto = m.Groups[2].Value.Trim();
        }
        if (!buscar && texto.Length == 0)
            return;
        var id = buscar ? BuscaProdutoForm.Escolher(this, texto) : BuscaProdutoForm.Localizar(this, texto);
        if (id is not long produto)
        {
            codigo.SelectAll();
            return;
        }
        if (quantidade <= 0)
        {
            Ui.Aviso("Quantidade inválida.");
            return;
        }
        var p = Db.Query("SELECT codigo, nome, unidade, preco_venda, preco_custo, estoque FROM produtos WHERE id = @p0", produto).Rows[0];
        var existente = itens.FirstOrDefault(i => i.ProdutoId == produto);
        var estoque = Ui.Dec(p["estoque"]);
        if ((existente?.Quantidade ?? 0) + quantidade > estoque &&
            !Parametros.PodeVenderSemEstoque($"Estoque insuficiente para \"{p["nome"]}\" (disponível: {estoque:N2})."))
            return;

        ItemLinha item;
        if (existente != null)
        {
            existente.Quantidade += quantidade;
            itens.ResetItem(itens.IndexOf(existente));
            item = existente;
        }
        else
        {
            item = new ItemLinha
            {
                Seq = itens.Count + 1,
                ProdutoId = produto,
                Codigo = Ui.Txt(p["codigo"]),
                Produto = Ui.Txt(p["nome"]),
                Unidade = Ui.Txt(p["unidade"]),
                Quantidade = quantidade,
                Unitario = Ui.Dec(p["preco_venda"]),
                Custo = Ui.Dec(p["preco_custo"]),
            };
            itens.Add(item);
        }
        Log.Registrar(Log.Acao, "PDV: item lançado", $"{quantidade:0.###} {item.Unidade} {item.Codigo} {item.Produto}");
        lblUltimo.Text = item.Produto;
        lblUltimoCalculo.Text = $"{quantidade:0.###} {item.Unidade} × {Ui.Moeda(item.Unitario)} = {Ui.Moeda(Math.Round(quantidade * item.Unitario, 2))}";
        var linha = itens.IndexOf(item);
        if (linha >= 0 && linha < grid.Rows.Count)
        {
            // a linha atual (CurrentCell) é a que o F6 remove: tem de ser a mesma que aparece selecionada
            grid.CurrentCell = grid.Rows[linha].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            grid.ClearSelection();
            grid.Rows[linha].Selected = true;
            grid.FirstDisplayedScrollingRowIndex = Math.Max(0, linha - 8);
        }
        proximaQuantidade = 1;
        codigo.Clear();
        codigo.Focus();
        AtualizarTotais();
    }

    void EscolherCliente()
    {
        using var d = new Dialogo("Cliente da venda", "PdvForm", "Selecionar");
        var combo = d.Adicionar("Cliente:", Ui.Combo(380));
        Ui.Preencher(combo, Db.Query("SELECT id, nome FROM pessoas WHERE eh_cliente = 1 AND ativo = 1 ORDER BY nome"), "Consumidor final");
        Ui.Selecionar(combo, clienteId);
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        d.Nota("Digite parte do nome para localizar. Venda a prazo exige cliente.");
        if (d.ShowDialog(this) != DialogResult.OK)
            return;
        clienteId = Ui.Id(combo);
        lblCliente.Text = clienteId is null ? "Consumidor final" : combo.Text;
        Log.Registrar(Log.Acao, "PDV: cliente definido", lblCliente.Text);
        codigo.Focus();
    }

    void DefinirQuantidade()
    {
        using var d = new Dialogo("Quantidade do próximo item", "PdvForm");
        var q = d.Adicionar("Quantidade:", Ui.Numero(3, QuantidadeMaxima, 0.001m));
        q.Value = proximaQuantidade;
        q.Select(0, q.Text.Length);
        if (d.ShowDialog(this) != DialogResult.OK)
            return;
        proximaQuantidade = q.Value;
        Log.Registrar(Log.Acao, "PDV: quantidade do próximo item", $"{q.Value:0.###}");
        AtualizarTotais();
        codigo.Focus();
    }

    void DarDesconto()
    {
        if (itens.Count == 0)
            return;
        using var d = new Dialogo("Desconto na venda", "PdvForm", "Aplicar");
        var valor = d.Adicionar("Desconto (R$):", Ui.Numero(2, Subtotal));
        var percentual = d.Adicionar("ou percentual (%):", Ui.Numero(2, 100));
        valor.Value = Math.Min(desconto, Subtotal);
        percentual.ValueChanged += (_, _) => { if (percentual.Value > 0) valor.Value = Math.Round(Subtotal * percentual.Value / 100, 2); };
        d.Validar = () => Parametros.ValidarDesconto(valor.Value, Subtotal) is not string erro || Falhar(erro);
        if (d.ShowDialog(this) != DialogResult.OK)
            return;
        desconto = valor.Value;
        Log.Registrar(Log.Acao, "PDV: desconto aplicado", Ui.Moeda(desconto));
        AtualizarTotais();
        codigo.Focus();
    }

    static bool Falhar(string mensagem)
    {
        Ui.Erro(mensagem);
        return false;
    }

    void Remover()
    {
        if (grid.CurrentRow?.DataBoundItem is not ItemLinha i)
            return;
        if (!Ui.Confirmar($"Remover \"{i.Produto}\" da venda?"))
            return;
        itens.Remove(i);
        Log.Registrar(Log.Acao, "PDV: item removido", $"{i.Codigo} {i.Produto}");
        for (int k = 0; k < itens.Count; k++)
            itens[k].Seq = k + 1;
        itens.ResetBindings();
        desconto = Math.Min(desconto, Subtotal);
        AtualizarTotais();
        codigo.Focus();
    }

    void Limpar(bool perguntar)
    {
        if (perguntar && itens.Count > 0 && !Ui.Confirmar("Cancelar a venda em andamento?"))
            return;
        if (perguntar && itens.Count > 0)
            Log.Registrar(Log.Acao, "PDV: venda em andamento cancelada", $"{itens.Count} item(ns), {Ui.Moeda(Total)}");
        itens.Clear();
        desconto = 0;
        proximaQuantidade = 1;
        clienteId = null;
        lblCliente.Text = "Consumidor final";
        lblUltimo.Text = "Caixa livre";
        lblUltimoCalculo.Text = "Leia o código de barras ou digite o código do produto";
        AtualizarTotais();
        codigo.Focus();
    }

    void Pagar()
    {
        if (itens.Count == 0)
        {
            Ui.Aviso("Adicione pelo menos um item antes do pagamento.");
            return;
        }
        // o desconto pode ter passado do limite depois de remover itens (F6): revalida antes de receber
        if (Parametros.ValidarDesconto(desconto, Subtotal) is string erroDesconto)
        {
            Ui.Erro(erroDesconto + "\n\nAjuste o desconto com F5 antes do pagamento.");
            codigo.Focus();
            return;
        }
        using var pagamento = new PagamentoForm(Total, clienteId != null);
        if (pagamento.ShowDialog(this) != DialogResult.OK)
        {
            codigo.Focus();
            return;
        }
        if (Operacoes.ValidarLimiteCredito(clienteId, Total, pagamento.Forma) is string erroCredito)
        {
            Ui.Erro(erroCredito);
            codigo.Focus();
            return;
        }

        ResultadoTef? tef = null;
        if (Parametros.TefAtivo && pagamento.Modalidade is ModalidadeTef modalidade)
        {
            tef = Tef.Pagar(this, Total, modalidade, pagamento.Parcelas);
            if (!tef.Aprovada)
            {
                Ui.Erro("Pagamento não aprovado: " + tef.Mensagem);
                return;
            }
        }

        long id;
        var total = Total;
        try
        {
            var lista = itens.Select(i => new ItemDoc(i.ProdutoId, i.Quantidade, i.Unitario, i.Custo)).ToList();
            id = Operacoes.RegistrarVenda(clienteId, lista, desconto, pagamento.Forma, pagamento.Parcelas, pagamento.Vencimento);
        }
        catch (Exception ex)
        {
            if (tef != null)
                Tef.Desfazer(tef.TransacaoId);
            Ui.Erro("Erro ao registrar a venda" + (tef != null ? " (a transação do cartão foi desfeita)" : "") + ":\n" + ex.Message);
            return;
        }
        if (tef != null)
            Tef.Confirmar(tef.TransacaoId, id);

        var imprimir = Parametros.ImprimirCupom switch
        {
            "SEMPRE" => true,
            "NUNCA" => false,
            _ => Ui.Confirmar($"Venda nº {id} registrada.\n\nImprimir o comprovante?"),
        };
        if (imprimir)
        {
            try
            {
                Impressao.ImprimirCupom(CupomForm.Gerar(id), abrirGaveta: pagamento.Forma == "Dinheiro");
                if (tef != null)
                {
                    Impressao.ImprimirCupom(tef.ComprovanteCliente);
                    if (Parametros.TefViaLoja)
                        Impressao.ImprimirCupom(tef.ComprovanteLoja);
                }
            }
            catch (Exception ex)
            {
                Ui.Aviso("Venda registrada, mas a impressão falhou: " + ex.Message);
            }
        }
        itens.Clear();
        Limpar(perguntar: false);
        var troco = pagamento.Recebido - total;
        lblUltimo.Text = troco > 0 ? $"TROCO: {Ui.Moeda(troco)}" : $"Venda nº {id} concluída";
        lblUltimoCalculo.Text = $"Pagamento: {pagamento.Forma}" + (tef?.Bandeira is string b ? $" · {b} · NSU {tef.Nsu}" : "");
        Ui.Feito($"Venda nº {id} registrada ({Ui.Moeda(total)}).");
    }
}
