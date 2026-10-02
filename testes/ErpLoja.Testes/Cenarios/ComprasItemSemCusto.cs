using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O comprador lança um produto e esquece de digitar o custo (fica zero). Ao "Salvar e receber", o sistema pergunta
/// se é bonificação; respondendo Não, nada entra e o custo médio fica intacto.
/// </summary>
public class ComprasItemSemCusto : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var (produto, codigo) = ComprasUtil.Produtos(1)[0];
        Db.Exec("UPDATE produtos SET estoque = 10, preco_custo = 6 WHERE id = @p0", produto);
        var compras = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compras"));

        var tela = u.AbrirTela("pedidos_COMPRA");
        u.RespostasConfirmacao.Enqueue(false);
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), ComprasUtil.Fornecedor());
            ComprasUtil.LancarItem(u, ed, codigo, 10, 0m);
            u.Clicar(ed, "Salvar e receber");
        });
        u.Clicar(tela, "Novo");
        Usuario.Esperar(300);
        u.ConferirJanelas();

        u.Ok(u.Viu("CONFIRMAR", "custo unitário ZERO"), "pergunta se o item sem custo é bonificação");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compras")) == compras, "respondendo Não, nada é recebido");
        u.Ok(ComprasUtil.Custo(produto) == 6 && ComprasUtil.Estoque(produto) == 10, "custo médio e estoque intactos");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos ORDER BY id DESC LIMIT 1")) == "ABERTO", "pedido fica salvo em aberto para corrigir o custo");
        tela.Close();
    }
}
