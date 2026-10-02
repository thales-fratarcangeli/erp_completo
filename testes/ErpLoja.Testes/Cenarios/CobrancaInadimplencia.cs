using System.Data;
using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Dono conferindo a inadimplência: o rodapé do Financeiro mostra o total vencido a receber, o filtro por período
/// atualiza ao mudar as datas, e as contas vencidas são cobradas por boleto com novo vencimento (o banco recusa vencimento passado).
/// </summary>
public class CobrancaDonoCobraInadimplentes : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var atrasada = FinanceiroUtil.ContaReceber("Cliente em atraso", 275.40m, -12);

        var f = u.AbrirTela("FinanceiroForm");
        var vencido = Ui.Dec(Db.Scalar("SELECT COALESCE(SUM(valor), 0) FROM contas WHERE tipo = 'RECEBER' AND status = 'ABERTA' AND vencimento < date('now', 'localtime')"));
        var rodape = Usuario.Todos(f).OfType<Label>().First(l => l.Text.StartsWith("Em aberto"));
        u.Ok(rodape.Text.Contains($"vencido: {Ui.Moeda(vencido)}"), $"rodapé mostra o total vencido a receber ({Ui.Moeda(vencido)})");

        var grade = Usuario.Achar<DataGridView>(f);
        var periodo = Usuario.Achar<CheckBox>(f);
        var datas = Usuario.Todos(f).OfType<DateTimePicker>().ToList();
        periodo.Checked = true;
        Usuario.Esperar();
        datas[0].Value = DateTime.Today.AddDays(-13);
        datas[1].Value = DateTime.Today.AddDays(-11);
        Usuario.Esperar();
        var ids = ((DataTable)grade.DataSource!).Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["id"])).ToList();
        u.Ok(ids.Contains(atrasada), "mudar as datas do período já atualiza a lista (sem clicar em Pesquisar)");
        f.Close();

        try
        {
            Cobranca.Emitir(banco, atrasada);
            u.Ok(false, "boleto de conta vencida sem novo vencimento deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("vencimento passado"), "boleto com vencimento passado é recusado com explicação");
        }

        var c = u.AbrirTela("CobrancaForm");
        var novo = DateTime.Today.AddDays(5);
        u.QuandoAbrirJanela("Emitir boletos", janela =>
        {
            var dt = (DataTable)Usuario.Achar<DataGridView>(janela).DataSource!;
            foreach (DataRow r in dt.Rows)
                r["Emitir"] = Convert.ToInt64(r["id"]) == atrasada;
            var data = Usuario.Todos(janela).OfType<DateTimePicker>().FirstOrDefault();
            u.Ok(data != null && Usuario.Todos(janela).OfType<Label>().Any(l => l.Text.Contains("vencida")), "janela avisa das contas vencidas e pede o novo vencimento");
            if (data != null)
                data.Value = novo;
            u.Clicar(janela, "Emitir selecionados");
        });
        u.Clicar(c, "Emitir boletos");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        var b = Db.Query("SELECT vencimento, codigo_barras FROM boletos WHERE conta_id = @p0", atrasada).Rows.Cast<DataRow>().FirstOrDefault();
        u.Ok(b != null && Db.LerData(b["vencimento"]).Date == novo, "boleto da conta vencida sai com o novo vencimento");
        if (b != null)
            u.Ok(Ui.Txt(b["codigo_barras"]).Substring(5, 4) == Cobranca.FatorVencimento(novo).ToString("0000"), "fator de vencimento do código de barras usa o novo vencimento");
        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhum erro ao emitir" + string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        c.Close();
    }
}
