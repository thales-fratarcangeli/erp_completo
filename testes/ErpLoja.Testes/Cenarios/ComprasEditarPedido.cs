using System.ComponentModel;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O comprador salva um pedido com 2 itens, reabre, muda a quantidade, remove um item e salva. Depois cancela o
/// pedido: ele não pode mais ser recebido e abre só para consulta.
/// </summary>
public class ComprasEditarECancelarPedido : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var prods = ComprasUtil.Produtos(2);
        var fornecedor = ComprasUtil.Fornecedor();
        var tela = u.AbrirTela("pedidos_COMPRA");

        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
            ComprasUtil.LancarItem(u, ed, prods[0].Codigo, 2, 5m);
            ComprasUtil.LancarItem(u, ed, prods[1].Codigo, 3, 4m);
            Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
        });
        u.Clicar(tela, "Novo");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        var id = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM pedidos WHERE tipo = 'COMPRA'"));
        u.Ok(Ui.Dec(Db.Scalar("SELECT total FROM pedidos WHERE id = @p0", id)) == 22m, "pedido salvo com total 22,00");

        var grid = Usuario.Achar<DataGridView>(tela);
        u.Ok(ComprasUtil.SelecionarLinha(grid, id), "pedido aparece em aberto na lista");
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            var g = Usuario.Achar<DataGridView>(ed);
            var itens = (BindingList<PedidoItem>)g.DataSource!;
            itens[0].Quantidade = 7;
            itens.ResetItem(0);
            g.CurrentCell = g.Rows[1].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
            u.Clicar(ed, "Remover item");
            Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
        });
        u.Clicar(tela, "Abrir / editar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        var itensDb = Db.Query("SELECT produto_id, quantidade FROM pedido_itens WHERE pedido_id = @p0", id).Rows;
        u.Ok(itensDb.Count == 1 && (long)itensDb[0]["produto_id"] == prods[0].Id && Ui.Dec(itensDb[0]["quantidade"]) == 7, "edição grava quantidade 7 e remove o 2º item");
        u.Ok(Ui.Dec(Db.Scalar("SELECT total FROM pedidos WHERE id = @p0", id)) == 35m, "total recalculado 7 × 5,00 = 35,00");

        // cancela o pedido
        ComprasUtil.SelecionarLinha(grid, id);
        u.Clicar(tela, "Cancelar");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", id)) == "CANCELADO", "pedido cancelado");
        var estoque = ComprasUtil.Estoque(prods[0].Id);
        var recusou = false;
        try { Pedidos.Receber(id, "PIX", 1, DateTime.Today, null); } catch (InvalidOperationException) { recusou = true; }
        u.Ok(recusou && ComprasUtil.Estoque(prods[0].Id) == estoque, "pedido cancelado não pode ser recebido");

        // consulta somente leitura
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            u.Ok(!Usuario.Botao(ed, "Salvar").Visible && Usuario.Achar<DataGridView>(ed).ReadOnly, "pedido cancelado abre só para consulta");
            u.Clicar(ed, "Fechar");
        });
        var f = new PedidoEditorForm(Pedidos.Compra, id);
        f.ShowDialog();
        u.ConferirJanelas();
        tela.Close();
    }
}
