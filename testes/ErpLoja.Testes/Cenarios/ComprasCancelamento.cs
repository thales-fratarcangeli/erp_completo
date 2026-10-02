using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Pedido de compra recebido a prazo (2x); parte da mercadoria já foi vendida e a 1ª parcela já foi paga.
/// O administrador cancela a compra: o sistema avisa do estoque negativo e da parcela paga; respondendo "Não",
/// nada muda; respondendo "Sim", estoque sai (pode ficar negativo), contas são canceladas e o pedido reabre.
/// </summary>
public class ComprasCancelarCompraJaVendida : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var (produto, codigo) = ComprasUtil.Produtos(1)[0];
        Db.Exec("UPDATE produtos SET estoque = 0 WHERE id = @p0", produto);
        var fornecedor = ComprasUtil.Fornecedor();

        var pedidos = u.AbrirTela("pedidos_COMPRA");
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
            ComprasUtil.LancarItem(u, ed, codigo, 5, 8m);
            ComprasUtil.ResponderRecebimento(u, Operacoes.APrazo, 2);
            u.Clicar(ed, "Salvar e receber");
        });
        u.Clicar(pedidos, "Novo");
        Usuario.Esperar(400);
        u.ConferirJanelas();
        pedidos.Close();

        var compra = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM compras"));
        var pedido = Convert.ToInt64(Db.Scalar("SELECT id FROM pedidos WHERE compra_id = @p0", compra));
        u.Ok(ComprasUtil.Estoque(produto) == 5, "recebimento deu entrada de 5");
        Operacoes.RegistrarVenda(null, [new ItemDoc(produto, 3, 15, 8)], 0, "Dinheiro", 1, DateTime.Today);
        var primeira = Convert.ToInt64(Db.Scalar("SELECT MIN(id) FROM contas WHERE compra_id = @p0", compra));
        Operacoes.BaixarConta(primeira, DateTime.Today, 20, "PIX");

        var docs = u.AbrirTela("compras");
        var grid = Usuario.Achar<DataGridView>(docs);
        ComprasUtil.SelecionarLinha(grid, compra);

        u.RespostasConfirmacao.Enqueue(false);
        u.Clicar(docs, "Cancelar compra");
        var pergunta = u.UltimaMensagem("CONFIRMAR") ?? "";
        u.Ok(pergunta.Contains("NEGATIVO") && pergunta.Contains("-3,000"), "confirmação avisa que o produto ficará com estoque negativo (2 − 5 = −3)");
        u.Ok(pergunta.Contains("já foram pagas") && pergunta.Contains(Ui.Moeda(20)), "confirmação avisa da parcela já paga");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM compras WHERE id = @p0", compra)) == "FINALIZADA", "respondendo Não, a compra continua");

        u.Clicar(docs, "Cancelar compra");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM compras WHERE id = @p0", compra)) == "CANCELADA", "respondendo Sim, a compra é cancelada");
        u.Ok(ComprasUtil.Estoque(produto) == -3, "estoque fica negativo (−3) porque 3 já foram vendidos (RN-EST-04)");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM contas WHERE compra_id = @p0 AND status <> 'CANCELADA'", compra)) == 0, "contas da compra canceladas");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", pedido)) == "ABERTO", "pedido de compra volta para ABERTO");
        var mov = Db.Query("SELECT tipo, quantidade, saldo FROM movimentacoes_estoque WHERE produto_id = @p0 ORDER BY id DESC LIMIT 1", produto).Rows[0];
        u.Ok(Ui.Txt(mov["tipo"]) == "SAIDA" && Ui.Dec(mov["quantidade"]) == 5 && Ui.Dec(mov["saldo"]) == -3, "movimentação SAIDA de 5 com saldo −3");

        u.Clicar(docs, "Cancelar compra");
        u.Ok(u.Viu("ERRO", "já está cancelad"), "cancelar de novo avisa que já está cancelada");
        docs.Close();
    }
}
