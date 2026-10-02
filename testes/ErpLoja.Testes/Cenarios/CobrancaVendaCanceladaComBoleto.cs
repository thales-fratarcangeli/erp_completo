using ErpLoja.Data;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Venda a prazo cobrada por boleto e depois cancelada: o boleto não enviado é cancelado e o já registrado no banco
/// recebe pedido de baixa — o cliente não pode continuar sendo cobrado por uma venda desfeita.
/// </summary>
public class CobrancaVendaCanceladaComBoleto : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var cliente = Convert.ToInt64(Db.Scalar("SELECT id FROM pessoas WHERE eh_cliente = 1 AND ativo = 1 AND cpf_cnpj IS NOT NULL ORDER BY id LIMIT 1"));
        var produto = Db.Query("SELECT id, preco_venda, preco_custo FROM produtos WHERE ativo = 1 AND estoque > 5 ORDER BY id LIMIT 1").Rows[0];
        var venda = Operacoes.RegistrarVenda(cliente, [new ItemDoc((long)produto["id"], 2, 50m, 10m)], 0, Operacoes.APrazo, 2, DateTime.Today.AddDays(30));
        var contas = Db.Query("SELECT id FROM contas WHERE venda_id = @p0 ORDER BY id", venda).Rows.Cast<System.Data.DataRow>().Select(r => (long)r["id"]).ToList();
        u.Ok(contas.Count == 2, "venda a prazo em 2 parcelas");

        var registrado = Cobranca.Emitir(banco, contas[0]);
        var (remessa, _, _, _) = Cobranca.GerarRemessa(banco);
        Cobranca.ProcessarRetorno(banco, "CONF.ret", Cobranca.SimularRetorno(remessa, liquidar: false));
        var emitido = Cobranca.Emitir(banco, contas[1]);

        Operacoes.CancelarVenda(venda);
        u.Ok(FinanceiroUtil.Status("boletos", emitido) == "CANCELADO", "boleto não enviado da venda cancelada é cancelado");
        u.Ok(FinanceiroUtil.Status("boletos", registrado) == "REGISTRADO"
             && Convert.ToInt64(Db.Scalar("SELECT baixa_solicitada FROM boletos WHERE id = @p0", registrado)) == 1,
            "boleto registrado da venda cancelada fica com pedido de baixa para a próxima remessa");
    }
}
