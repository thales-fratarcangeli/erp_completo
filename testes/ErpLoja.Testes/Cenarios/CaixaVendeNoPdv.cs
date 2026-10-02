using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Exemplo de cenário: a operadora de caixa vende dois itens no PDV e recebe em dinheiro com troco.</summary>
public class CaixaVendeNoPdv : ICenario
{
    public string Area => "PDV";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        var produto = Db.Query("SELECT codigo, preco_venda FROM produtos WHERE ativo = 1 AND tipo <> 'INSUMO' AND estoque > 10 ORDER BY id LIMIT 1").Rows[0];
        var codigo = Ui.Txt(produto["codigo"]);
        var preco = Ui.Dec(produto["preco_venda"]);
        var vendasAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM vendas"));

        var pdv = u.AbrirTela("PdvForm");
        var campo = Usuario.Achar<TextBox>(pdv, t => t.PlaceholderText.Contains("código"));
        u.Digitar(campo, "2*" + codigo, enter: true);
        u.Ok(Usuario.Todos(pdv).OfType<Label>().Any(l => l.Text == Ui.Moeda(preco * 2)), "lançar 2*código mostra o total da venda");

        u.QuandoAbrir<PagamentoForm>(pagamento =>
        {
            Usuario.Achar<NumericUpDown>(pagamento).Value = Math.Ceiling(preco * 2) + 10;
            u.Teclar(pagamento, Keys.Enter);
        });
        u.Teclar(pdv, Keys.F10);
        Usuario.Esperar(600);
        u.ConferirJanelas();

        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM vendas")) == vendasAntes + 1, "F10 + dinheiro grava a venda");
        u.Ok(Ui.Dec(Db.Scalar("SELECT total FROM vendas ORDER BY id DESC LIMIT 1")) == preco * 2, "total gravado = 2 × preço");
        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhuma mensagem de erro durante a venda" + string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        pdv.Close();
    }
}
