using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Atendente abre uma OS pela tela, acompanha as etapas até a entrega e o gerente fatura: toda OS tem etapa e prazo,
/// a atrasada aparece em vermelho e como alerta na tela inicial (RN-OS-01 a RN-OS-06).
/// </summary>
public class ServicosOrdemDeServicoDoPedidoAoFaturamento : ICenario
{
    public string Area => "Ordens de serviço";
    public bool ComDadosDemo => true;

    static DataRowView? Linha(DataGridView g, long id) =>
        g.Rows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem as DataRowView).FirstOrDefault(v => v != null && Convert.ToInt64(v["id"]) == id);

    public void Executar(Usuario u)
    {
        var f = (OrdensServicoForm)u.AbrirTela("OrdensServicoForm");
        var grade = Usuario.Achar<DataGridView>(f);
        var antes = FinanceiroUtil.UltimoId("ordens_servico");

        // ---------- nova OS pela tela: sem descrição não grava; prazo vem do parâmetro
        u.QuandoAbrirJanela("Nova ordem de serviço", d =>
        {
            var prazo = Usuario.Achar<DateTimePicker>(d);
            u.Ok(prazo.Value.Date == DateTime.Today.AddDays(Parametros.PrazoOsDias) && prazo.MinDate == Datas.Minima,
                "prazo sugerido pelo parâmetro (dias) e campo de data só aceita datas plausíveis");
            u.Clicar(d, "Salvar");
            u.Ok(!d.IsDisposed && u.Viu("ERRO", "Serviço"), "sem descrever o serviço a OS não é aberta");
            Usuario.Todos(d).OfType<TextBox>().First(t => t.Multiline).Text = "Cesta de frutas para reunião\r\n20 pessoas";
            Usuario.Achar<NumericUpDown>(d).Value = 480m;
            prazo.Value = DateTime.Today.AddDays(2);
            u.Clicar(d, "Salvar");
        });
        u.Clicar(f, "Nova OS");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        var id = FinanceiroUtil.UltimoId("ordens_servico");
        var os = OrdensServico.Ler(id);
        u.Ok(id > antes && Ui.Txt(os["etapa"]) == OrdensServico.Aberta && Ui.Dec(os["valor"]) == 480m && Ui.Txt(os["descricao"]).Contains("20 pessoas"),
            "OS aberta pela tela: etapa Aberta, valor e serviço (com várias linhas) gravados");
        u.Ok(Linha(grade, id) is { } l && Ui.Txt(l["Situação"]) == "NO PRAZO", "a OS nova aparece na lista, no prazo");

        // ---------- etapas: avançar pela tela; voltar exige motivo; tudo no histórico
        FinanceiroUtil.SelecionarLinha(grade, id);
        u.Clicar(f, "Avançar etapa");
        FinanceiroUtil.SelecionarLinha(grade, id);
        u.Clicar(f, "Avançar etapa");
        u.Ok(Ui.Txt(OrdensServico.Ler(id)["etapa"]) == OrdensServico.EmExecucao, "\"Avançar etapa\" passa para a próxima (Aberta → Aprovada → Em execução)");
        try
        {
            OrdensServico.MudarEtapa(id, OrdensServico.Aberta);
            u.Ok(false, "voltar etapa sem motivo deveria ser recusado");
        }
        catch (InvalidOperationException ex) { u.Ok(ex.Message.Contains("motivo"), "voltar para uma etapa anterior exige o motivo"); }
        OrdensServico.MudarEtapa(id, OrdensServico.Aprovada, "cliente pediu para trocar as frutas");
        var historico = Db.Query("SELECT etapa, observacao FROM os_historico WHERE os_id = @p0 ORDER BY id", id);
        u.Ok(historico.Rows.Count == 4 && Ui.Txt(historico.Rows[3]["observacao"]).Contains("trocar as frutas"),
            "histórico com cada mudança de etapa, inclusive o motivo da volta");
        try
        {
            OrdensServico.Criar(new(1, "x", DateTime.Today.AddDays(-1), 10, null, ""));
            u.Ok(false, "prazo antes da abertura deveria ser recusado");
        }
        catch (InvalidOperationException ex) { u.Ok(ex.Message.Contains("prazo"), "prazo de entrega antes da abertura é recusado"); }
        u.Ok(!Datas.Plausivel(new DateTime(201, 1, 1)) && !Datas.Plausivel(DateTime.Today.AddYears(30)) && Datas.Plausivel(DateTime.Today),
            "datas implausíveis (ano 201, daqui a 30 anos) são recusadas");

        // ---------- atraso: situação, filtro, resumo e alerta da tela inicial
        Db.Exec("UPDATE ordens_servico SET data = date('now', 'localtime', '-6 days'), prazo = date('now', 'localtime', '-2 days') WHERE id = @p0", id);
        f.AplicarFiltro(Alertas.OsAtrasadas);
        u.Ok(f.FiltroAtual == "Atrasadas" && Linha(grade, id) is { } atrasada && Ui.Txt(atrasada["Situação"]) == "ATRASADA"
             && Convert.ToInt64(atrasada["Dias de atraso"]) == 2, "OS atrasada: filtro \"Atrasadas\", situação ATRASADA e dias de atraso");
        u.Ok(grade.Rows.Cast<DataGridViewRow>().All(r => Ui.Txt(((DataRowView)r.DataBoundItem!)["Situação"]) == "ATRASADA"), "o filtro mostra só as atrasadas");
        var alertas = Alertas.Listar();
        u.Ok(alertas.Any(a => a.Tela == "OrdensServicoForm" && a.Filtro == Alertas.OsAtrasadas && a.Grave), "OS atrasada vira alerta grave da tela inicial");
        u.RespostaConfirmacao = true;
        f.Close();
        f.Dispose();

        var principal = SistemaUtil.AbrirPrincipal();
        var botao = principal.BotoesAlerta.FirstOrDefault(b => b.Tag is Alerta { Filtro: Alertas.OsAtrasadas });
        u.Ok(botao != null && botao.Text.Contains("atrasada"), "a tela inicial mostra o alerta das OS atrasadas");
        botao?.PerformClick();
        Usuario.Esperar(300);
        u.Ok(principal.TelaAtiva is OrdensServicoForm aberta && aberta.FiltroAtual == "Atrasadas", "clicar no alerta abre as OS já filtradas nas atrasadas");
        var contasVencidas = principal.BotoesAlerta.Count; // a tela inicial está escondida, mas os alertas continuam lá
        SistemaUtil.Fechar(principal, u);

        // ---------- entrega, faturamento (uma vez só) e travas
        OrdensServico.MudarEtapa(id, OrdensServico.Entregue);
        os = OrdensServico.Ler(id);
        u.Ok(os["entregue_em"] is string && Ui.Txt(Db.Scalar($"SELECT {OrdensServico.SqlSituacao} FROM ordens_servico o WHERE id = @p0", id)) == "ENTREGUE COM ATRASO",
            "entregar grava a data da entrega; entregue depois do prazo fica marcada \"com atraso\"");
        OrdensServico.Faturar(id, 3, DateTime.Today.AddDays(10));
        var contas = Db.Query("SELECT valor, vencimento, cliente_id FROM contas WHERE os_id = @p0 ORDER BY id", id);
        u.Ok(contas.Rows.Count == 3 && contas.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["valor"])) == 480m
             && Convert.ToInt64(contas.Rows[0]["cliente_id"]) == Convert.ToInt64(os["cliente_id"]),
            "faturar gera as contas a receber da OS (3 parcelas somando o valor, para o cliente da OS)");
        foreach (var (acao, descricao) in new (Action, string)[]
                 {
                     (() => OrdensServico.Faturar(id, 1, DateTime.Today), "faturar de novo"),
                     (() => OrdensServico.Alterar(id, new(Convert.ToInt64(os["cliente_id"]), "x", DateTime.Today.AddDays(1), 999, null, "")), "mudar o valor da OS faturada"),
                     (() => OrdensServico.Cancelar(id, "desistiu"), "cancelar OS faturada com contas abertas"),
                 })
        {
            try
            {
                acao();
                u.Ok(false, descricao + " deveria ser recusado");
            }
            catch (InvalidOperationException) { u.Ok(true, descricao + ": recusado com explicação"); }
        }
        u.Ok(OrdensServico.Texto(id).Contains($"ORDEM DE SERVIÇO Nº {id}") && OrdensServico.Texto(id).Contains("20 pessoas"), "ficha da OS para imprimir");

        // ---------- cancelamento exige motivo; indicador de entregas no prazo
        var outra = OrdensServico.Criar(new(Convert.ToInt64(os["cliente_id"]), "Arranjo de mesa", DateTime.Today.AddDays(5), 0, null, ""));
        try
        {
            OrdensServico.Cancelar(outra, " ");
            u.Ok(false, "cancelar sem motivo deveria ser recusado");
        }
        catch (InvalidOperationException) { u.Ok(true, "cancelar OS exige o motivo"); }
        OrdensServico.Cancelar(outra, "cliente desistiu");
        u.Ok(Ui.Txt(OrdensServico.Ler(outra)["etapa"]) == OrdensServico.Cancelada, "OS cancelada com motivo");
        var r = OrdensServico.Resumo();
        u.Ok(r.PctNoPrazo is decimal pct && pct > 0 && pct < 100, $"indicador de entregas no prazo nos últimos 90 dias ({r.PctNoPrazo:N1}%)");
        u.Ok(contasVencidas >= 2, "com dados de demonstração a tela inicial mostra vários alertas (OS, contas vencidas, estoque)");
    }
}

/// <summary>
/// Financeiro do dia: baixa várias contas de uma vez (tudo ou nada) e registra a cobrança feita por telefone, com a
/// promessa de pagamento; promessa não cumprida vira alerta (RN-FIN-05, RN-FIN-06).
/// </summary>
public class FinanceiroBaixaEmLoteEAcompanhamentoDeCobranca : ICenario
{
    public string Area => "Financeiro";
    public bool ComDadosDemo => true;

    public void Executar(Usuario u)
    {
        var a = FinanceiroUtil.ContaReceber("Lote A", 100m, -5);
        var b = FinanceiroUtil.ContaReceber("Lote B", 250m, -2);
        var c = FinanceiroUtil.ContaReceber("Lote C", 80m, 3);
        var f = (FinanceiroForm)u.AbrirTela("FinanceiroForm");
        var grade = Usuario.Achar<DataGridView>(f);
        u.Clicar(f, "Pesquisar");

        // ---------- baixa em lote pela tela
        void Selecionar(params long[] ids)
        {
            grade.ClearSelection();
            foreach (DataGridViewRow r in grade.Rows)
                if (r.DataBoundItem is DataRowView v && ids.Contains(Convert.ToInt64(v["id"])))
                    r.Selected = true;
        }
        Selecionar(a);
        u.Clicar(f, "Baixar selecionadas");
        u.Ok(u.Viu("ERRO", "duas ou mais"), "baixa em lote com uma conta só orienta a usar \"Baixar\"");
        u.QuandoAbrirJanela("Receber contas em lote", d =>
        {
            u.Ok(Usuario.Todos(d).OfType<Label>().Any(l => l.Text.Contains("2 conta(s)") && l.Text.Contains("350,00")), "a janela mostra quantas contas e o total");
            Usuario.Achar<ComboBox>(d).SelectedItem = "PIX";
            u.Clicar(d, "Confirmar");
        });
        Selecionar(a, b);
        u.Clicar(f, "Baixar selecionadas");
        Usuario.Esperar(200);
        u.ConferirJanelas();
        var pagas = Db.Query("SELECT status, valor_pago, forma_pagamento FROM contas WHERE id IN (@p0, @p1)", a, b);
        u.Ok(pagas.Rows.Cast<DataRow>().All(r => Ui.Txt(r["status"]) == "PAGA" && Ui.Txt(r["forma_pagamento"]) == "PIX")
             && pagas.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["valor_pago"])) == 350m, "as duas contas baixadas, cada uma pelo próprio valor, com a forma escolhida");
        try
        {
            Operacoes.BaixarLote([c, a], DateTime.Today, "Dinheiro");
            u.Ok(false, "lote com conta já paga deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains($"nº {a}") && FinanceiroUtil.Status("contas", c) == "ABERTA",
                "lote com uma conta já paga: nada é baixado (tudo ou nada) e a mensagem diz qual conta");
        }

        // ---------- contato de cobrança pela tela, com promessa
        var vencida = FinanceiroUtil.ContaReceber("Cobrança Z", 300m, -15);
        u.Clicar(f, "Pesquisar");
        FinanceiroUtil.SelecionarLinha(grade, vencida);
        u.QuandoAbrirJanela("Registrar contato de cobrança", d =>
        {
            Usuario.Achar<ComboBox>(d).SelectedItem = "WhatsApp";
            Usuario.Achar<TextBox>(d).Text = "Cliente vai pagar segunda-feira";
            Usuario.Achar<CheckBox>(d).Checked = true;
            Usuario.Todos(d).OfType<DateTimePicker>().First().Value = DateTime.Today.AddDays(2);
            u.Clicar(d, "Salvar");
        });
        u.Clicar(f, "Registrar contato");
        Usuario.Esperar(200);
        u.ConferirJanelas();
        var contato = Db.Query("SELECT meio, resumo, promessa FROM cobranca_contatos WHERE conta_id = @p0", vencida);
        u.Ok(contato.Rows.Count == 1 && Ui.Txt(contato.Rows[0]["meio"]) == "WhatsApp" && Db.LerData(contato.Rows[0]["promessa"]).Date == DateTime.Today.AddDays(2),
            "contato registrado pela tela: meio, conversa e data da promessa");
        FinanceiroUtil.SelecionarLinha(grade, vencida);
        var linha = Ui.Linha(grade)!;
        u.Ok(Ui.Txt(linha["Último contato"]).Contains("WhatsApp") && Ui.Txt(linha["Promessa"]) == DateTime.Today.AddDays(2).ToString("dd/MM/yyyy"),
            "a lista mostra o último contato e a promessa");
        foreach (var (acao, descricao) in new (Action, string)[]
                 {
                     (() => ContatosCobranca.Registrar(vencida, "Telefone", "x", DateTime.Today.AddDays(-1)), "promessa com data passada"),
                     (() => ContatosCobranca.Registrar(a, "Telefone", "x", null), "contato numa conta já paga"),
                     (() => ContatosCobranca.Registrar(vencida, "Telefone", " ", null), "contato sem dizer o que foi conversado"),
                 })
        {
            try
            {
                acao();
                u.Ok(false, descricao + " deveria ser recusado");
            }
            catch (InvalidOperationException) { u.Ok(true, descricao + ": recusado"); }
        }

        // ---------- promessa não cumprida: filtro e alerta
        Db.Exec("UPDATE cobranca_contatos SET promessa = date('now', 'localtime', '-1 days') WHERE conta_id = @p0", vencida);
        f.AplicarFiltro(Alertas.PromessasVencidas);
        u.Ok(f.FiltroAtual == "A receber / Promessa vencida" && grade.Rows.Cast<DataGridViewRow>().Any(r => Convert.ToInt64(((DataRowView)r.DataBoundItem!)["id"]) == vencida),
            "filtro \"Promessa vencida\" mostra quem prometeu e não pagou");
        u.Ok(Alertas.Listar().Any(x => x.Filtro == Alertas.PromessasVencidas && x.Grave), "promessa não cumprida vira alerta na tela inicial");
        f.AplicarFiltro(Alertas.ContasVencidas);
        u.Ok(f.FiltroAtual == "A receber / Vencidas", "alerta de contas vencidas abre o Financeiro filtrado nas vencidas a receber");
        u.RespostaConfirmacao = true;
        f.Close();
        f.Dispose();
    }
}
