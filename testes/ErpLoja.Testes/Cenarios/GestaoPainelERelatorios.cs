using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

static class GestaoUtil
{
    /// <summary>Valor mostrado no cartão do painel (rótulo → valor grande).</summary>
    public static string Cartao(Form painel, string rotulo)
    {
        var titulo = Usuario.Todos(painel).OfType<Label>().First(l => l.Text == rotulo);
        return titulo.Parent!.Controls.OfType<Label>().First(l => l.Dock == DockStyle.Fill).Text;
    }

    public static void PercorrerPeriodos(Usuario u, Form painel, Action<int, string>? conferir = null)
    {
        var periodo = Usuario.Achar<ComboBox>(painel);
        for (var i = 0; i < periodo.Items.Count; i++)
        {
            periodo.SelectedIndex = i;
            Usuario.Esperar(80);
            conferir?.Invoke(i, Ui.Txt(periodo.Items[i]));
        }
    }
}

/// <summary>O dono confere o painel gerencial em todos os períodos e compara com o banco.</summary>
public class GestaoDonoConfereOPainel : ICenario
{
    public string Area => "Gestão";

    public void Executar(Usuario u)
    {
        var painel = u.AbrirTela("DashboardForm");
        var de = DateTime.Today.AddDays(-29);
        var fat = Ui.Dec(Db.Scalar("SELECT COALESCE(SUM(total), 0) FROM vendas WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1", Db.Dia(de), Db.Dia(DateTime.Today)));
        var n = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM vendas WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1", Db.Dia(de), Db.Dia(DateTime.Today)));
        u.Ok(GestaoUtil.Cartao(painel, "Faturamento") == Ui.Moeda(fat), $"faturamento dos últimos 30 dias confere com o banco ({Ui.Moeda(fat)})");
        u.Ok(GestaoUtil.Cartao(painel, "Vendas") == n.ToString("N0"), "quantidade de vendas dos últimos 30 dias confere");
        if (n > 0)
            u.Ok(GestaoUtil.Cartao(painel, "Ticket médio") == Ui.Moeda(fat / n), "ticket médio = faturamento ÷ vendas");

        GestaoUtil.PercorrerPeriodos(u, painel, (_, nome) =>
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), $"período '{nome}' abre sem erro" + SistemaUtil.Erros(u)));
        painel.Close();
    }
}

/// <summary>Loja nova, sem nenhuma venda: o painel e os relatórios não podem dar erro nem mostrar margem inventada.</summary>
public class GestaoLojaNovaSemDados : ICenario
{
    public string Area => "Gestão";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        var painel = u.AbrirTela("DashboardForm");
        GestaoUtil.PercorrerPeriodos(u, painel, (_, nome) =>
        {
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), $"sem dados, período '{nome}' abre sem erro" + SistemaUtil.Erros(u));
            u.Ok(GestaoUtil.Cartao(painel, "Faturamento") == Ui.Moeda(0) && GestaoUtil.Cartao(painel, "Margem bruta") == "-",
                $"sem dados, '{nome}' mostra faturamento zero e margem '-'");
        });
        painel.Close();

        var rel = u.AbrirTela("RelatoriosForm");
        var lista = Usuario.Achar<ListBox>(rel);
        for (var i = 0; i < lista.Items.Count; i++)
        {
            lista.SelectedIndex = i;
            Usuario.Esperar(50);
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), $"sem dados, relatório '{lista.Items[i]}' gera sem erro" + SistemaUtil.Erros(u));
        }
        rel.Close();
    }
}

/// <summary>Gerente tira todos os relatórios: período normal, período sem movimento e datas invertidas.</summary>
public class GestaoGerenteTiraRelatorios : ICenario
{
    public string Area => "Gestão";

    public void Executar(Usuario u)
    {
        var rel = u.AbrirTela("RelatoriosForm");
        var lista = Usuario.Achar<ListBox>(rel);
        var datas = Usuario.Todos(rel).OfType<DateTimePicker>().ToList();
        var (de, ate) = (datas[0], datas[1]);
        var grid = Usuario.Achar<DataGridView>(rel);
        de.Value = DateTime.Today.AddMonths(-6);
        ate.Value = DateTime.Today;

        for (var i = 0; i < lista.Items.Count; i++)
        {
            lista.SelectedIndex = i;
            u.Clicar(rel, "Gerar");
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO") && grid.DataSource is DataTable, $"relatório '{lista.Items[i]}' em 6 meses gera sem erro" + SistemaUtil.Erros(u));
        }

        // vendas por dia: soma do relatório = soma do banco
        lista.SelectedIndex = 0;
        u.Clicar(rel, "Gerar");
        var total = ((DataTable)grid.DataSource!).Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["Total"]));
        var banco = Ui.Dec(Db.Scalar("SELECT COALESCE(SUM(total), 0) FROM vendas WHERE status = 'FINALIZADA' AND date(data) BETWEEN @p0 AND @p1", Db.Dia(de.Value), Db.Dia(ate.Value)));
        u.Ok(total == banco && total > 0, $"'Vendas por dia' soma {total:N2} = banco {banco:N2}");

        // período sem movimento
        de.Value = new DateTime(2001, 1, 1);
        ate.Value = new DateTime(2001, 1, 31);
        for (var i = 0; i < lista.Items.Count; i++)
        {
            lista.SelectedIndex = i;
            u.Clicar(rel, "Gerar");
            var linhas = grid.DataSource is DataTable dt ? dt.Rows.Count : -1;
            var esperado = lista.Items[i]!.ToString()!.StartsWith("Lucro bruto") ? 1 : lista.Items[i]!.ToString()!.StartsWith("Produtos sem venda") ? linhas : 0;
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO") && linhas == esperado, $"'{lista.Items[i]}' num período sem movimento: {linhas} linha(s), sem erro");
        }

        // datas invertidas: avisa em vez de mostrar relatório vazio
        de.Value = DateTime.Today;
        ate.Value = DateTime.Today.AddDays(-10);
        u.Mensagens.Clear();
        u.Clicar(rel, "Gerar");
        u.Ok(u.Viu("ERRO", "anterior à inicial"), "datas invertidas avisam que a data final é anterior à inicial");
        rel.Close();
    }
}
