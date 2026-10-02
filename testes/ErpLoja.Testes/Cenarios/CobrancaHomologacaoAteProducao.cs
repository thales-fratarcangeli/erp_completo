using System.Data;
using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Operador homologando com o banco pela aba Homologação: gera boletos de teste, remessa, simula o retorno com pagamento
/// pela tela; depois passa a conta para PRODUCAO com boletos de teste esquecidos (a remessa de produção tem que recusar).
/// Por fim o financeiro estorna uma conta liquidada pelo banco (aviso) e consegue emitir boleto novo para ela.
/// </summary>
public class CobrancaHomologacaoAteProducao : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var f = u.AbrirTela("CobrancaForm");
        Usuario.Achar<TabControl>(f).SelectedIndex = 2; // Homologação
        Usuario.Esperar();

        var antes = FinanceiroUtil.UltimoId("boletos");
        u.Clicar(f, "1. Gerar boletos de teste");
        var teste = Db.Query("SELECT id, conta_id FROM boletos WHERE id > @p0 ORDER BY id", antes);
        u.Ok(teste.Rows.Count == 3, "3 boletos de teste emitidos");
        var (remessa, arquivo, _, qtd) = Cobranca.GerarRemessa(banco);
        u.Ok(arquivo.StartsWith("TESTE_") && qtd == 3, "remessa de teste com os 3 boletos");

        u.Clicar(f, "4. Simular retorno (confirmar e pagar)");
        u.Ok(u.Viu("INFO", "3 liquidado(s)"), "simulação pela tela informa os 3 liquidados" + string.Concat(u.Mensagens.Select(m => " | " + m.Texto)));
        u.Ok(teste.Rows.Cast<DataRow>().All(r => FinanceiroUtil.Status("contas", (long)r["conta_id"]) == "PAGA"), "contas de teste baixadas pelo retorno simulado");
        var log = Usuario.Todos(f).OfType<TextBox>().First(t => t.Multiline && t.ReadOnly);
        u.Ok(log.Text.Contains("Retorno simulado") && log.Text.Contains("liquidado"), "registro da homologação mostra o retorno processado");

        // esqueceu boletos de teste e mudou para produção
        u.Clicar(f, "1. Gerar boletos de teste");
        Db.Exec("UPDATE contas_bancarias SET ambiente = 'PRODUCAO' WHERE id = @p0", banco);
        try
        {
            Cobranca.GerarRemessa(banco);
            u.Ok(false, "remessa de produção com boletos de teste pendentes deveria ser recusada");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("boleto(s) de teste"), "remessa de produção recusa boletos de teste esquecidos");
        }
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM boletos WHERE status = 'EMITIDO'")) == 3, "boletos de teste continuam só emitidos (nada foi enviado)");
        var retornos = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM retornos"));
        u.Clicar(f, "4. Simular retorno (confirmar e pagar)");
        u.Ok(u.Viu("ERRO", "só existe no ambiente HOMOLOGACAO") && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM retornos")) == retornos,
            "em produção a simulação de retorno é recusada (não baixa contas sem dinheiro)");
        f.Close();

        // estorno de conta liquidada pelo banco: avisa, e depois permite novo boleto
        var paga = (long)teste.Rows[0]["conta_id"];
        var fin = u.AbrirTela("FinanceiroForm");
        var situacao = Usuario.Todos(fin).OfType<ComboBox>().ElementAt(1);
        situacao.SelectedIndex = 2; // Pagas
        Usuario.Esperar();
        u.Ok(FinanceiroUtil.SelecionarLinha(Usuario.Achar<DataGridView>(fin), paga), "conta liquidada aparece em Pagas");
        u.Clicar(fin, "Estornar");
        u.Ok(u.Viu("CONFIRMAR", "liquidada pelo banco"), "estorno de conta liquidada pelo banco pede confirmação com aviso");
        u.Ok(FinanceiroUtil.Status("contas", paga) == "ABERTA", "conta estornada volta para aberta");
        fin.Close();
        Db.Exec("UPDATE contas_bancarias SET ambiente = 'HOMOLOGACAO' WHERE id = @p0", banco);
        try
        {
            var novo = Cobranca.Emitir(banco, paga);
            u.Ok(FinanceiroUtil.Status("boletos", novo) == "EMITIDO", "conta estornada pode receber boleto novo (o pago não conta como ativo)");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(false, "conta estornada deveria aceitar boleto novo: " + ex.Message);
        }
    }
}
