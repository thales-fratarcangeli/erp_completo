using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários de Financeiro e Cobrança.</summary>
static class FinanceiroUtil
{
    /// <summary>Seleciona na grade a linha cujo "id" é o informado (como o usuário clicando nela). Devolve false se não estiver na lista.</summary>
    public static bool SelecionarLinha(DataGridView g, long id)
    {
        foreach (DataGridViewRow r in g.Rows)
            if (r.DataBoundItem is DataRowView v && v.Row.Table.Columns.Contains("id") && v["id"] is not DBNull && Convert.ToInt64(v["id"]) == id)
            {
                g.ClearSelection();
                var celula = r.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                g.CurrentCell = celula;
                r.Selected = true;
                return true;
            }
        return false;
    }

    public static long UltimoId(string tabela) => Convert.ToInt64(Db.Scalar($"SELECT MAX(id) FROM {tabela}"));

    public static long ContaReceber(string descricao, decimal valor, int diasVencimento, bool comCliente = true)
    {
        long? cliente = comCliente ? Convert.ToInt64(Db.Scalar("SELECT id FROM pessoas WHERE eh_cliente = 1 AND ativo = 1 AND cpf_cnpj IS NOT NULL ORDER BY id LIMIT 1")) : null;
        Operacoes.LancarConta("RECEBER", descricao, valor, 1, DateTime.Today.AddDays(diasVencimento), cliente, null);
        return UltimoId("contas");
    }

    public static long ContaBancariaDemo() => Convert.ToInt64(Db.Scalar("SELECT id FROM contas_bancarias ORDER BY id LIMIT 1"));

    public static string Status(string tabela, long id) => Ui.Txt(Db.Scalar($"SELECT status FROM {tabela} WHERE id = @p0", id));
}

/// <summary>Financeiro no fechamento do mês: lança uma receita avulsa parcelada pela tela "Nova conta" e confere as parcelas (RN-FIN-02).</summary>
public class FinanceiroLancaContaParcelada : ICenario
{
    public string Area => "Financeiro";

    public void Executar(Usuario u)
    {
        var f = u.AbrirTela("FinanceiroForm");
        var vencimento = DateTime.Today.AddDays(10);

        // 1ª tentativa sem descrição: a tela tem que avisar e continuar aberta; depois preenche e salva
        u.QuandoAbrir<Dialogo>(d =>
        {
            var combos = Usuario.Todos(d).OfType<ComboBox>().ToList();
            combos[0].SelectedIndex = 1; // A receber
            var numeros = Usuario.Todos(d).OfType<NumericUpDown>().ToList();
            numeros[0].Value = 100m;
            numeros[1].Value = 3;
            Usuario.Achar<DateTimePicker>(d).Value = vencimento;
            combos[1].SelectedIndex = 1; // primeiro cliente
            u.Clicar(d, "Salvar");
            u.Ok(!d.IsDisposed && d.DialogResult != DialogResult.OK && u.Viu("ERRO", "descrição"), "sem descrição a nova conta não é salva e o motivo é mostrado");
            u.Digitar(Usuario.Achar<TextBox>(d), "Consultoria avulsa");
            u.Clicar(d, "Salvar");
        });
        u.Clicar(f, "Nova conta");
        Usuario.Esperar(300);
        u.ConferirJanelas();

        var parcelas = Db.Query("SELECT descricao, valor, vencimento, tipo, status, cliente_id FROM contas WHERE descricao LIKE 'Consultoria avulsa%' ORDER BY id");
        u.Ok(parcelas.Rows.Count == 3, $"3 parcelas geradas ({parcelas.Rows.Count})");
        if (parcelas.Rows.Count != 3)
            return;
        u.Ok(parcelas.Rows.Cast<DataRow>().Select(r => Ui.Dec(r["valor"])).SequenceEqual([33.33m, 33.33m, 33.34m]), "valores 33,33 / 33,33 / 33,34 (centavos na última)");
        u.Ok(Enumerable.Range(0, 3).All(i => Db.LerData(parcelas.Rows[i]["vencimento"]).Date == vencimento.AddMonths(i)), "vencimentos mensais a partir do 1º");
        u.Ok(Ui.Txt(parcelas.Rows[1]["descricao"]) == "Consultoria avulsa - parcela 2/3", "descrição \"… parcela k/n\"");
        u.Ok(parcelas.Rows.Cast<DataRow>().All(r => Ui.Txt(r["tipo"]) == "RECEBER" && Ui.Txt(r["status"]) == "ABERTA" && r["cliente_id"] is long),
            "parcelas a receber, em aberto e com o cliente escolhido");
        var grade = Usuario.Achar<DataGridView>(f);
        u.Ok(grade.Rows.Cast<DataGridViewRow>().Count(r => Ui.Txt(((DataRowView)r.DataBoundItem!)["Descrição"]).StartsWith("Consultoria avulsa")) == 3,
            "a lista é atualizada com as parcelas novas");
        f.Close();
    }
}

/// <summary>
/// Financeiro baixando uma conta vencida: filtro "Vencidas" com dias de atraso, baixa com juros e forma de pagamento,
/// data futura recusada, estorno (limpa o pagamento) e cancelamento (RN-FIN-01, RN-FIN-03).
/// </summary>
public class FinanceiroBaixaEstornoCancelamento : ICenario
{
    public string Area => "Financeiro";

    public void Executar(Usuario u)
    {
        Operacoes.LancarConta("PAGAR", "Energia elétrica (teste)", 200m, 1, DateTime.Today.AddDays(-5), null, null);
        var id = FinanceiroUtil.UltimoId("contas");

        var f = u.AbrirTela("FinanceiroForm");
        var grade = Usuario.Achar<DataGridView>(f);
        var situacao = Usuario.Todos(f).OfType<ComboBox>().ElementAt(1);
        situacao.SelectedIndex = 1; // Vencidas
        Usuario.Esperar();
        var linhas = ((DataTable)grade.DataSource!).Rows.Cast<DataRow>().ToList();
        u.Ok(linhas.All(r => Ui.Txt(r["Situação"]) == "VENCIDA"), "filtro Vencidas só mostra contas vencidas");
        var minha = linhas.FirstOrDefault(r => Convert.ToInt64(r["id"]) == id);
        u.Ok(minha != null && Ui.Dec(minha["Dias de atraso"]) == 5, "conta vencida há 5 dias aparece com 5 dias de atraso");

        // baixa: data futura recusada; depois paga com juros, no PIX
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, id), "seleciona a conta vencida");
        u.QuandoAbrir<Dialogo>(d =>
        {
            var data = Usuario.Achar<DateTimePicker>(d);
            data.Value = DateTime.Today.AddDays(3);
            Usuario.Achar<NumericUpDown>(d).Value = 205.50m;
            var forma = Usuario.Achar<ComboBox>(d);
            forma.SelectedItem = "PIX";
            u.Clicar(d, "Confirmar");
            u.Ok(d.DialogResult != DialogResult.OK && u.Viu("ERRO", "futura"), "baixa com data futura é recusada com explicação");
            data.Value = DateTime.Today;
            u.Clicar(d, "Confirmar");
        });
        u.Clicar(f, "Baixar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        var c = Db.Query("SELECT status, valor_pago, forma_pagamento, data_pagamento FROM contas WHERE id = @p0", id).Rows[0];
        u.Ok(Ui.Txt(c["status"]) == "PAGA" && Ui.Dec(c["valor_pago"]) == 205.50m && Ui.Txt(c["forma_pagamento"]) == "PIX"
             && Db.LerData(c["data_pagamento"]).Date == DateTime.Today, "baixa grava data, valor com juros e forma");
        try
        {
            Operacoes.BaixarConta(id, DateTime.Today, 205.50m, "PIX");
            u.Ok(false, "baixar de novo uma conta já paga deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("não está mais em aberto"), "baixar duas vezes a mesma conta é recusado");
        }

        // estorno
        situacao.SelectedIndex = 2; // Pagas
        Usuario.Esperar();
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, id), "conta aparece em Pagas");
        u.Clicar(f, "Estornar");
        c = Db.Query("SELECT status, valor_pago, forma_pagamento, data_pagamento FROM contas WHERE id = @p0", id).Rows[0];
        u.Ok(Ui.Txt(c["status"]) == "ABERTA" && c["valor_pago"] is DBNull && c["data_pagamento"] is DBNull && c["forma_pagamento"] is DBNull,
            "estorno volta a conta para aberta e limpa o pagamento (data, valor e forma)");

        // cancelamento e tentativa de baixar conta cancelada
        situacao.SelectedIndex = 0; // Em aberto
        Usuario.Esperar();
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, id), "conta estornada volta a aparecer em aberto");
        u.Clicar(f, "Cancelar conta");
        u.Ok(FinanceiroUtil.Status("contas", id) == "CANCELADA", "cancelar conta aberta");
        situacao.SelectedIndex = 3; // Canceladas
        Usuario.Esperar();
        FinanceiroUtil.SelecionarLinha(grade, id);
        var erros = u.Mensagens.Count(m => m.Tipo == "ERRO");
        u.Clicar(f, "Baixar");
        u.Ok(u.Mensagens.Count(m => m.Tipo == "ERRO") == erros + 1 && u.Viu("ERRO", "Somente contas em aberto"), "conta cancelada não pode ser baixada");
        u.Ok(FinanceiroUtil.Status("contas", id) == "CANCELADA", "conta continua cancelada");
        f.Close();
    }
}
