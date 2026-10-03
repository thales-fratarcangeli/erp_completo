using System.ComponentModel;
using System.Reflection;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Tabelas de todas as telas: rolam na horizontal quando as colunas não cabem (em vez de espremer) e ordenam ao clicar
/// no título da coluna (de novo inverte), inclusive listas de itens; a ordem escolhida sobrevive à recarga da lista.
/// </summary>
public class InterfaceTabelasRolamEOrdenam : ICenario
{
    public string Area => "Tabelas";
    public bool ComDadosDemo => true;

    /// <summary>
    /// Clique no título da coluna pelo tratador da própria grade (o mesmo caminho do clique do mouse). Não usa mensagens
    /// de mouse do Windows porque elas só valem com a janela em primeiro plano, o que a bateria não garante.
    /// </summary>
    static void ClicarTitulo(DataGridView g, DataGridViewColumn c)
    {
        g.FirstDisplayedScrollingColumnIndex = c.Index;
        Usuario.Esperar(50);
        var r = g.GetCellDisplayRectangle(c.Index, -1, true);
        var mouse = new MouseEventArgs(MouseButtons.Left, 1, r.X + 10, r.Y + r.Height / 2, 0);
        typeof(DataGridView).GetMethod("OnColumnHeaderMouseClick", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(g, [new DataGridViewCellMouseEventArgs(c.Index, -1, 10, r.Height / 2, mouse)]);
        Usuario.Esperar();
    }

    static List<string> Valores(DataGridView g, DataGridViewColumn c) =>
        g.Rows.Cast<DataGridViewRow>().Select(r => Ui.Txt(r.Cells[c.Index].Value)).ToList();

    static bool Crescente(List<string> v) => v.SequenceEqual(v.Order(StringComparer.CurrentCultureIgnoreCase), StringComparer.Ordinal);
    static bool Decrescente(List<string> v) => v.SequenceEqual(v.OrderDescending(StringComparer.CurrentCultureIgnoreCase), StringComparer.Ordinal);

    sealed class Linha
    {
        public string Descricao { get; set; } = "";
        public decimal Quantidade { get; set; }
    }

    public void Executar(Usuario u)
    {
        // ---------- todas as telas: tabelas com rolagem horizontal e colunas com largura mínima
        var semRolagem = new List<string>();
        var semOrdem = new List<string>();
        foreach (var tela in Navegacao.Telas().Where(t => t.Criar != null && t.Chave is not ("ConfiguracoesForm" or "LocalizarTelaForm")))
        {
            var f = u.AbrirTela(tela.Chave);
            var grades = Usuario.Todos(f).OfType<DataGridView>().ToList();
            foreach (var g in grades)
                _ = g.Handle; // grades em abas ainda não abertas: como se a pessoa abrisse a aba
            Usuario.Esperar();
            foreach (var g in grades)
            {
                if (g.ScrollBars != ScrollBars.Both || g.Columns.Cast<DataGridViewColumn>().Any(c => c.Visible && c.MinimumWidth < 50))
                    semRolagem.Add($"{tela.Titulo}/{g.Name}");
                if (f is not PdvForm && g.Columns.Cast<DataGridViewColumn>().Any(c => c.Visible && c.SortMode == DataGridViewColumnSortMode.NotSortable
                        && c is DataGridViewTextBoxColumn))
                    semOrdem.Add($"{tela.Titulo}/{g.Name}");
            }
            u.RespostaConfirmacao = true;
            f.Close();
            f.Dispose();
            Usuario.Esperar(50);
        }
        u.Ok(semRolagem.Count == 0, "todas as tabelas têm rolagem horizontal e colunas com largura mínima" + string.Concat(semRolagem.Select(s => " | " + s)));
        u.Ok(semOrdem.Count == 0, "todas as colunas de texto das tabelas podem ser ordenadas (exceto itens do PDV)" + string.Concat(semOrdem.Select(s => " | " + s)));

        // ---------- produtos: tela estreita rola; clicar no título ordena e inverte; recarregar mantém a ordem
        var produtos = u.AbrirTela("cadastro_produtos");
        var grade = Usuario.Achar<DataGridView>(produtos, g => g.Rows.Count > 3);
        produtos.Size = new Size(760, 600);
        Usuario.Esperar(200);
        var visiveis = grade.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
        u.Ok(visiveis.Sum(c => c.Width) > grade.ClientSize.Width && grade.Controls.OfType<HScrollBar>().Any(h => h.Visible)
             && visiveis.All(c => c.Width >= c.MinimumWidth),
            "tela estreita: as colunas mantêm a largura mínima e aparece a barra de rolagem horizontal");
        produtos.Size = new Size(1400, 860);
        Usuario.Esperar(200);

        var nome = visiveis.First(c => c.HeaderText == "Produto");
        ClicarTitulo(grade, nome);
        var asc = Valores(grade, nome);
        ClicarTitulo(grade, nome);
        var desc = Valores(grade, nome);
        u.Ok(Crescente(asc) && Decrescente(desc) && nome.HeaderCell.SortGlyphDirection == SortOrder.Descending,
            "clicar no título ordena de A a Z; clicar de novo inverte (com a seta no título)");
        u.Digitar(Usuario.Achar<TextBox>(produtos, t => Validacao.FormatoDe(t) == Formatos.Busca), "a", enter: true);
        u.Digitar(Usuario.Achar<TextBox>(produtos, t => Validacao.FormatoDe(t) == Formatos.Busca), "", enter: true);
        u.Ok(Decrescente(Valores(grade, nome)) && grade.Rows.Count == desc.Count, "a ordem escolhida continua depois de pesquisar e recarregar a lista");
        var preco = visiveis.FirstOrDefault(c => c.ValueType == typeof(double) || c.ValueType == typeof(decimal));
        if (preco != null)
        {
            ClicarTitulo(grade, preco);
            var numeros = grade.Rows.Cast<DataGridViewRow>().Select(r => Ui.Dec(r.Cells[preco.Index].Value)).ToList();
            u.Ok(numeros.SequenceEqual(numeros.Order()), "colunas de valor ordenam como número (2 antes de 10), não como texto");
        }
        u.RespostaConfirmacao = true;
        produtos.Close();
        produtos.Dispose();

        // ---------- datas que o banco devolve como texto "dd/mm/aaaa": ordenam pela data, não pelo dia
        var financeiro = u.AbrirTela("FinanceiroForm");
        var contas = Usuario.Achar<DataGridView>(financeiro);
        Usuario.Achar<ComboBox>(financeiro, c => c.Items.Contains("Todas") && c.Items.Contains("Vencidas")).SelectedItem = "Todas";
        Usuario.Esperar();
        DataGridViewColumn Vencimento() => contas.Columns.Cast<DataGridViewColumn>().First(c => c.HeaderText == "Vencimento"); // recriada ao recarregar
        List<DateTime> Datas() => contas.Rows.Cast<DataGridViewRow>()
            .Select(r => DateTime.ParseExact(Ui.Txt(r.Cells[Vencimento().Index].Value), "dd/MM/yyyy", null)).ToList();
        ClicarTitulo(contas, Vencimento());
        var crescente = Datas();
        ClicarTitulo(contas, Vencimento());
        var decrescente = Datas();
        u.Ok(crescente.Count > 3 && crescente.Select(d => d.Month).Distinct().Count() > 1
             && crescente.SequenceEqual(crescente.Order()) && decrescente.SequenceEqual(decrescente.OrderDescending())
             && Vencimento().HeaderCell.SortGlyphDirection == SortOrder.Descending,
            "coluna de data ordena pela data de verdade (15/03 antes de 01/12), e de novo inverte");
        u.Clicar(financeiro, "Pesquisar");
        Usuario.Esperar(300);
        var depoisDeRecarregar = Datas();
        u.Ok(depoisDeRecarregar.SequenceEqual(depoisDeRecarregar.OrderDescending()) && contas.Columns.Cast<DataGridViewColumn>().All(c => !c.Visible || !c.DataPropertyName.StartsWith("__")),
            "a ordem por data continua depois de recarregar, sem coluna auxiliar visível");
        u.RespostaConfirmacao = true;
        financeiro.Close();
        financeiro.Dispose();

        // ---------- lista de itens (BindingList, que não se ordena sozinha)
        var lista = new BindingList<Linha> { new() { Descricao = "Caneta", Quantidade = 10 }, new() { Descricao = "Agenda", Quantidade = 2 }, new() { Descricao = "Borracha", Quantidade = 5 } };
        using var janela = new Form { Size = new Size(700, 400), StartPosition = FormStartPosition.Manual, Location = new Point(0, 0) };
        var itens = Ui.Grid(somenteLeitura: false);
        janela.Controls.Add(itens);
        itens.DataSource = lista;
        janela.Show();
        Usuario.Esperar();
        itens.CurrentCell = itens.Rows[0].Cells[0]; // Caneta selecionada
        ClicarTitulo(itens, itens.Columns[nameof(Linha.Descricao)]!);
        var depois = lista.Select(l => l.Descricao).ToList();
        ClicarTitulo(itens, itens.Columns[nameof(Linha.Quantidade)]!);
        u.Ok(depois.SequenceEqual(["Agenda", "Borracha", "Caneta"]) && lista.Select(l => l.Quantidade).SequenceEqual([2m, 5m, 10m]),
            "lista de itens também ordena pelo título (texto e número)");
        u.Ok(itens.CurrentRow?.DataBoundItem is Linha { Descricao: "Caneta" }, "o item selecionado continua selecionado depois de ordenar");
        janela.Close();

        // ---------- PDV: itens ficam na ordem em que foram passados
        var pdv = u.AbrirTela("PdvForm");
        var gradePdv = Usuario.Achar<DataGridView>(pdv);
        u.Ok(gradePdv.Columns.Cast<DataGridViewColumn>().All(c => c.SortMode == DataGridViewColumnSortMode.NotSortable),
            "no PDV a lista de itens não reordena (fica na ordem da venda)");
        u.RespostaConfirmacao = true;
        pdv.Close();
        pdv.Dispose();
    }
}
