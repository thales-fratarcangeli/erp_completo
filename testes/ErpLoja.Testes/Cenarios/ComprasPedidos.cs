using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O comprador monta um pedido de compra com três itens e frete, usa "Salvar e receber" a prazo em 2x:
/// estoque entra, o custo médio recebe o frete rateado e as contas a pagar somam exatamente o total do pedido.
/// </summary>
public class ComprasPedidoComFreteRateado : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var prods = ComprasUtil.Produtos(3);
        var fornecedor = ComprasUtil.Fornecedor();
        var estoqueAntes = prods.Select(p => ComprasUtil.Estoque(p.Id)).ToList();
        var custoAntes = prods.Select(p => ComprasUtil.Custo(p.Id)).ToList();

        var tela = u.AbrirTela("pedidos_COMPRA");
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
            foreach (var p in prods)
                ComprasUtil.LancarItem(u, ed, p.Codigo, 1, 10m);
            ComprasUtil.CampoApos<NumericUpDown>(ed, "Frete R$:").Value = 1m;
            ComprasUtil.ResponderRecebimento(u, Operacoes.APrazo, 2, "123");
            u.Clicar(ed, "Salvar e receber");
        });
        u.Clicar(tela, "Novo");
        Usuario.Esperar(500);
        u.ConferirJanelas();

        var compra = Db.Query("SELECT id, total, numero_nota FROM compras ORDER BY id DESC LIMIT 1").Rows[0];
        var id = (long)compra["id"];
        u.Ok(Ui.Dec(Db.Scalar("SELECT total FROM pedidos WHERE compra_id = @p0", id)) == 31m, "pedido recebido com total R$ 31,00 (3 × 10 + frete 1)");
        u.Ok(Ui.Dec(compra["total"]) == 31m, $"compra gravada com o total do pedido (gravou {Ui.Dec(compra["total"])})");
        var contas = Db.Query("SELECT valor FROM contas WHERE compra_id = @p0 AND tipo = 'PAGAR'", id).Rows.Cast<System.Data.DataRow>().Select(r => Ui.Dec(r["valor"])).ToList();
        u.Ok(contas.Count == 2 && contas.Sum() == 31m, $"2 parcelas a pagar somando R$ 31,00 (soma {contas.Sum()})");
        u.Ok(Ui.Txt(compra["numero_nota"]) == "123", "nº da nota fiscal gravado na compra");
        for (int i = 0; i < prods.Count; i++)
        {
            u.Ok(ComprasUtil.Estoque(prods[i].Id) == estoqueAntes[i] + 1, $"estoque do item {i + 1} aumentou 1");
            var esperado = estoqueAntes[i] > 0
                ? Math.Round((estoqueAntes[i] * custoAntes[i] + 31m / 3m) / (estoqueAntes[i] + 1), 4)
                : Math.Round(31m / 3m, 4);
            u.Ok(Math.Abs(ComprasUtil.Custo(prods[i].Id) - esperado) <= 0.0001m, $"custo médio do item {i + 1} com frete rateado ({ComprasUtil.Custo(prods[i].Id)} ≈ {esperado})");
        }
        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhum erro" + string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        tela.Close();
    }
}

/// <summary>
/// O dono erra a digitação e coloca desconto de R$ 15 num item de R$ 10 (o outro item cobre o total).
/// O sistema deve recusar ao salvar — senão o recebimento grava custo negativo no produto.
/// </summary>
public class ComprasDescontoMaiorQueItem : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var prods = ComprasUtil.Produtos(2);
        var fornecedor = ComprasUtil.Fornecedor();
        var pedidosAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pedidos"));

        var tela = u.AbrirTela("pedidos_COMPRA");
        u.QuandoAbrir<PedidoEditorForm>(ed =>
        {
            Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
            ComprasUtil.LancarItem(u, ed, prods[0].Codigo, 1, 10m, desconto: 15m);
            ComprasUtil.LancarItem(u, ed, prods[1].Codigo, 1, 100m);
            Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
            Usuario.Esperar();
            u.Clicar(ed, "Fechar");
        });
        u.Clicar(tela, "Novo");
        Usuario.Esperar(400);
        u.ConferirJanelas();

        u.Ok(u.Viu("ERRO", "desconto"), "salvar avisa que o desconto do item é maior que o valor do item");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pedidos")) == pedidosAntes, "pedido com desconto inválido não é gravado");

        // camada de dados também recusa (proteção para quem chamar sem a tela)
        var p = new Pedido { Tipo = Pedidos.Compra, FornecedorId = fornecedor };
        p.Itens.Add(new PedidoItem { ProdutoId = prods[0].Id, Quantidade = 1, Unitario = 10, Desconto = 15 });
        p.Itens.Add(new PedidoItem { ProdutoId = prods[1].Id, Quantidade = 1, Unitario = 100 });
        var recusou = false;
        try { Pedidos.Salvar(p); } catch (InvalidOperationException) { recusou = true; }
        u.Ok(recusou, "Pedidos.Salvar recusa item com desconto maior que o valor");
        tela.Close();
    }
}
