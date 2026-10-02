using System.ComponentModel;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O comprador abre o Estoque, marca "Somente estoque baixo" e clica "Gerar pedido de compra": o editor abre com a
/// sugestão (mínimo − saldo) pelo custo médio; ele escolhe o fornecedor e salva. Pedido fica ABERTO, sem mexer no estoque.
/// </summary>
public class EstoqueGerarPedidoDaSugestao : ICenario
{
    public string Area => "Estoque";

    public void Executar(Usuario u)
    {
        var prods = ComprasUtil.Produtos(3);
        // só os dois primeiros abaixo do mínimo
        Db.Exec("UPDATE produtos SET estoque_minimo = 0 WHERE ativo = 1");
        Db.Exec("UPDATE produtos SET estoque = 2, estoque_minimo = 10, preco_custo = 4.5 WHERE id = @p0", prods[0].Id);
        Db.Exec("UPDATE produtos SET estoque = -1, estoque_minimo = 3, preco_custo = 7 WHERE id = @p0", prods[1].Id);
        Db.Exec("UPDATE produtos SET estoque = 50, estoque_minimo = 10 WHERE id = @p0", prods[2].Id);
        var fornecedor = ComprasUtil.Fornecedor();
        var pedidosAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pedidos"));
        var estoque0 = ComprasUtil.Estoque(prods[0].Id);

        var tela = u.AbrirTela("EstoqueForm");
        Usuario.Achar<CheckBox>(tela, c => c.Text.StartsWith("Somente estoque baixo")).Checked = true;
        Usuario.Esperar();

        List<PedidoItem> itensNoEditor = [];
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            itensNoEditor = ((BindingList<PedidoItem>)Usuario.Achar<DataGridView>(ed).DataSource!).ToList();
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
            Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
        });
        u.Clicar(tela, "Gerar pedido de compra");
        Usuario.Esperar(400);
        u.ConferirJanelas();

        u.Ok(itensNoEditor.Count == 2, $"editor abre com os 2 produtos abaixo do mínimo ({itensNoEditor.Count})");
        u.Ok(itensNoEditor.Any(i => i.ProdutoId == prods[0].Id && i.Quantidade == 8 && i.Unitario == 4.5m), "sugestão 10 − 2 = 8 pelo custo médio 4,50");
        u.Ok(itensNoEditor.Any(i => i.ProdutoId == prods[1].Id && i.Quantidade == 4 && i.Unitario == 7m), "saldo negativo: sugestão 3 − (−1) = 4");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pedidos")) == pedidosAntes + 1, "pedido de compra gravado");
        var ped = Db.Query("SELECT id, tipo, status, fornecedor_id, total FROM pedidos ORDER BY id DESC LIMIT 1").Rows[0];
        u.Ok(Ui.Txt(ped["tipo"]) == Pedidos.Compra && Ui.Txt(ped["status"]) == "ABERTO" && (long)ped["fornecedor_id"] == fornecedor, "pedido de compra ABERTO do fornecedor escolhido");
        u.Ok(Ui.Dec(ped["total"]) == 8 * 4.5m + 4 * 7m, "total do pedido = 8 × 4,50 + 4 × 7,00");
        u.Ok(u.Viu("INFO", $"nº {ped["id"]}"), "usuário é informado do número do pedido");
        u.Ok(ComprasUtil.Estoque(prods[0].Id) == estoque0, "gerar pedido não mexe no estoque (RN-PED-05)");

        // nada a sugerir: mensagem orienta em vez de abrir pedido vazio
        Db.Exec("UPDATE produtos SET estoque_minimo = 0, estoque = MAX(estoque, 0)");
        u.Clicar(tela, "Atualizar");
        u.Clicar(tela, "Gerar pedido de compra");
        u.Ok(u.Viu("INFO", "Não há o que sugerir"), "sem produtos abaixo do mínimo, avisa e não abre pedido");
        tela.Close();
    }
}
