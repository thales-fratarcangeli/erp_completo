using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Financeiro no fechamento do mês digita 2.000 no lugar de 200 ao baixar uma conta: o sistema pergunta antes de gravar;
/// respondendo "Não", a janela continua aberta para corrigir.
/// </summary>
public class FinanceiroBaixaValorDigitadoErrado : ICenario
{
    public string Area => "Financeiro";

    public void Executar(Usuario u)
    {
        Operacoes.LancarConta("PAGAR", "Internet (teste)", 200m, 1, DateTime.Today.AddDays(2), null, null);
        var id = FinanceiroUtil.UltimoId("contas");
        var f = u.AbrirTela("FinanceiroForm");
        u.Ok(FinanceiroUtil.SelecionarLinha(Usuario.Achar<DataGridView>(f), id), "conta na lista");
        u.QuandoAbrir<Dialogo>(d =>
        {
            var valor = Usuario.Achar<NumericUpDown>(d);
            valor.Value = 2000m;
            u.RespostasConfirmacao.Enqueue(false);
            u.Clicar(d, "Confirmar");
            u.Ok(d.DialogResult != DialogResult.OK && u.Viu("CONFIRMAR", "muito diferente"), "valor 10× maior pede confirmação; \"Não\" mantém a janela aberta");
            valor.Value = 200m;
            u.Clicar(d, "Confirmar");
        });
        u.Clicar(f, "Baixar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(Ui.Dec(Db.Scalar("SELECT valor_pago FROM contas WHERE id = @p0", id)) == 200m, "baixa gravada com o valor corrigido");
        f.Close();
    }
}
