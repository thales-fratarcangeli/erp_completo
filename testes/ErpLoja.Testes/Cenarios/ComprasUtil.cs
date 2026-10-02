using System.ComponentModel;
using System.Data;
using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários de Compras e Estoque.</summary>
static class ComprasUtil
{
    /// <summary>Controle que vem logo depois do rótulo (mesmo painel), como o usuário o enxerga na tela.</summary>
    public static T CampoApos<T>(Control raiz, string rotulo) where T : Control
    {
        var lbl = Usuario.Achar<Label>(raiz, l => l.Text.Trim() == rotulo);
        var irmaos = lbl.Parent!.Controls.Cast<Control>().ToList();
        return irmaos.Skip(irmaos.IndexOf(lbl) + 1).OfType<T>().First();
    }

    /// <summary>Produtos de revenda ativos (id, código), em ordem de id.</summary>
    public static List<(long Id, string Codigo)> Produtos(int quantos) =>
        Db.Query("SELECT id, codigo FROM produtos WHERE ativo = 1 AND tipo <> 'INSUMO' ORDER BY id LIMIT @p0", quantos)
            .Rows.Cast<DataRow>().Select(r => ((long)r["id"], Ui.Txt(r["codigo"]))).ToList();

    public static long Fornecedor() => Convert.ToInt64(Db.Scalar("SELECT id FROM pessoas WHERE eh_fornecedor = 1 AND ativo = 1 ORDER BY id LIMIT 1"));

    public static decimal Estoque(long produto) => Ui.Dec(Db.Scalar("SELECT estoque FROM produtos WHERE id = @p0", produto));

    public static decimal Custo(long produto) => Ui.Dec(Db.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", produto));

    /// <summary>Lança um item no editor de pedido digitando o código e teclando Enter.</summary>
    public static void LancarItem(Usuario u, Form editor, string codigo, decimal qtd, decimal unitario, decimal desconto = 0)
    {
        var campo = CampoApos<TextBox>(editor, "Código ou cód. barras:");
        u.Digitar(campo, codigo, enter: true);
        var itens = (BindingList<PedidoItem>)Usuario.Achar<DataGridView>(editor).DataSource!;
        var item = itens.Last();
        item.Quantidade = qtd;
        item.Unitario = unitario;
        item.Desconto = desconto;
        itens.ResetItem(itens.IndexOf(item));
    }

    /// <summary>Responde o diálogo "Receber mercadoria" (forma, parcelas e nº da NF) e confirma.</summary>
    public static void ResponderRecebimento(Usuario u, string forma, int parcelas = 1, string nota = "")
    {
        u.QuandoAbrirJanela("Receber mercadoria", d =>
        {
            CampoApos<ComboBox>(d, "Pagamento:").SelectedItem = forma;
            CampoApos<NumericUpDown>(d, "Parcelas:").Value = parcelas;
            CampoApos<TextBox>(d, "Nº da nota fiscal:").Text = nota;
            u.Clicar(d, "Confirmar");
        });
    }

    /// <summary>Clica na linha da grade cujo id é o informado (como o usuário selecionando o registro).</summary>
    public static bool SelecionarLinha(DataGridView g, long id)
    {
        foreach (DataGridViewRow r in g.Rows)
            if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == id)
            {
                g.ClearSelection();
                g.CurrentCell = r.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                r.Selected = true;
                Usuario.Esperar();
                return true;
            }
        return false;
    }

    public static string CaminhoNfeExemplo => Path.Combine(AppContext.BaseDirectory, "dados", "nfe_exemplo.xml");
}
