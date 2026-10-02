using System.Net;
using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

static class LogisticaUtil
{
    public static long Tac() => Convert.ToInt64(Db.Scalar("SELECT id FROM pessoas WHERE nome LIKE '%(autônomo)%' LIMIT 1"));
    public static long Etc() => Convert.ToInt64(Db.Scalar("SELECT id FROM pessoas WHERE nome LIKE 'Rápido Paulista%' LIMIT 1"));
    public static long Veiculo(string placa) => Convert.ToInt64(Db.Scalar("SELECT id FROM veiculos WHERE placa = @p0", placa));

    /// <summary>Frete contratado gravado direto (como o editor grava), com a situação do CIOT pela regra.</summary>
    public static long Frete(long transportador, decimal valor, decimal adiantamento = 0)
    {
        var doc = Ui.Txt(Db.Scalar("SELECT cpf_cnpj FROM pessoas WHERE id = @p0", transportador));
        return Convert.ToInt64(Db.Scalar("""
            INSERT INTO fretes(data, modalidade, transportador_id, motorista_id, veiculo_id, origem_cidade, origem_uf, destino_cidade, destino_uf,
                               valor_frete, valor_pedagio, valor_adiantamento, peso_kg, ciot_status, previsao_entrega)
            VALUES(@p0, 'CONTRATADO', @p1, @p2, @p3, 'Campinas', 'SP', 'Curitiba', 'PR', @p4, 0, @p5, 3000, @p6, @p7) RETURNING id
            """, DateTime.Today, transportador, Tac(), Veiculo("JTA4B56"), valor, adiantamento,
            Fretes.CiotObrigatorio("CONTRATADO", doc) ? "PENDENTE" : "NAO_SE_APLICA", Db.Dia(DateTime.Today.AddDays(2))));
    }

    public static string Status(long frete) => Ui.Txt(Db.Scalar("SELECT status FROM fretes WHERE id = @p0", frete));
    public static string Ciot(long frete) => Ui.Txt(Db.Scalar("SELECT ciot_status FROM fretes WHERE id = @p0", frete));
}

/// <summary>Resposta HTTP falsa da Pamcard (nada sai para a internet).</summary>
sealed class PamcardFalsa : HttpMessageHandler
{
    public string Resposta { get; set; } = "";
    public int Chamadas { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Chamadas++;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Resposta) });
    }
}

/// <summary>
/// Logística monta a viagem com transportador autônomo pela tela: novo frete (CIOT obrigatório), tentar liberar a viagem
/// sem CIOT (regressão: era permitido), declarar pelo simulador, gerar contas uma única vez, valores travados no editor
/// depois das contas (regressão), em trânsito, entregue e encerramento do CIOT.
/// </summary>
public class LogisticaMontaViagemAutonomo : ICenario
{
    public string Area => "Logística";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("ciot_administradora", "SIMULADOR");
        var tac = LogisticaUtil.Tac();
        var vei = LogisticaUtil.Veiculo("JTA4B56");
        var f = u.AbrirTela("FretesForm");
        var grade = Usuario.Achar<DataGridView>(f);

        string? avisoCiot = null;
        u.QuandoAbrirJanela("Novo frete", d =>
        {
            ProducaoLogisticaUtil.CampoApos<ComboBox>(d, "Modalidade").SelectedItem = "CONTRATADO";
            Ui.Selecionar(ProducaoLogisticaUtil.CampoApos<ComboBox>(d, "Transportador"), tac);
            Ui.Selecionar(ProducaoLogisticaUtil.CampoApos<ComboBox>(d, "Motorista"), tac);
            Ui.Selecionar(ProducaoLogisticaUtil.CampoApos<ComboBox>(d, "Veículo"), vei);
            ProducaoLogisticaUtil.CampoApos<TextBox>(d, "Destino (cidade)").Text = "Curitiba";
            ProducaoLogisticaUtil.CampoApos<ComboBox>(d, "Destino (UF)").SelectedItem = "PR";
            ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Peso").Value = 5000;
            ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Valor do frete").Value = 2000;
            ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Pedágio").Value = 100;
            ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Adiantamento").Value = 800;
            avisoCiot = ProducaoLogisticaUtil.CampoApos<Label>(d, "CIOT").Text;
            u.Clicar(d, "Salvar");
        });
        u.Clicar(f, "Novo frete");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(avisoCiot?.Contains("OBRIGATÓRIO") == true, "o editor avisa que o CIOT é obrigatório para autônomo: " + avisoCiot);
        var frete = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM fretes"));
        u.Ok(LogisticaUtil.Ciot(frete) == "PENDENTE" && Ui.Dec(Db.Scalar("SELECT valor_frete FROM fretes WHERE id = @p0", frete)) == 2000,
            "frete salvo com CIOT pendente");

        ProducaoLogisticaUtil.SelecionarLinha(grade, frete);
        u.Clicar(f, "Em trânsito");
        u.Ok(u.Viu("ERRO", "Declarar CIOT") && LogisticaUtil.Status(frete) == "ABERTO", "sem CIOT declarado a viagem não é liberada (em trânsito)");
        u.Clicar(f, "Entregue");
        u.Ok(LogisticaUtil.Status(frete) == "ABERTO", "sem CIOT declarado o frete não vai direto para entregue");

        u.Clicar(f, "Declarar CIOT");
        Usuario.Esperar(300);
        u.Ok(LogisticaUtil.Ciot(frete) == "DECLARADO" && Ui.Txt(Db.Scalar("SELECT ciot FROM fretes WHERE id = @p0", frete)).Length == 16,
            "CIOT declarado pelo simulador");

        u.Clicar(f, "Gerar contas a pagar");
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM contas WHERE frete_id = @p0 AND tipo = 'PAGAR'", frete) == 2
             && ProducaoLogisticaUtil.Dec("SELECT SUM(valor) FROM contas WHERE frete_id = @p0", frete) == 2100,
            "contas a pagar: adiantamento 800 + saldo 1.300 (1.200 + 100 de pedágio)");
        u.Clicar(f, "Gerar contas a pagar");
        u.Ok(u.Viu("ERRO", "já foram geradas") && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM contas WHERE frete_id = @p0", frete) == 2,
            "as contas do frete são geradas uma única vez");

        bool? valorTravado = null, rotaTravada = null;
        u.QuandoAbrirJanela($"Frete nº {frete}", d =>
        {
            valorTravado = !ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Outras despesas").Enabled;
            rotaTravada = !ProducaoLogisticaUtil.CampoApos<TextBox>(d, "Destino (cidade)").Enabled;
            d.DialogResult = DialogResult.Cancel;
        });
        u.Clicar(f, "Abrir / editar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(valorTravado == true, "depois de gerar as contas, valores do frete ficam travados no editor");
        u.Ok(rotaTravada == true, "com CIOT declarado, a rota (origem/destino declarados à ANTT) fica travada");

        u.Clicar(f, "Em trânsito");
        u.Ok(LogisticaUtil.Status(frete) == "EM_TRANSITO", "com CIOT declarado a viagem sai (em trânsito)");
        u.Clicar(f, "Entregue");
        u.Ok(LogisticaUtil.Status(frete) == "ENTREGUE" && Db.Scalar("SELECT data_entrega FROM fretes WHERE id = @p0", frete) is string,
            "frete entregue grava a data da entrega");

        Usuario.Achar<ComboBox>(f).SelectedIndex = 3; // Todos
        Usuario.Esperar();
        ProducaoLogisticaUtil.SelecionarLinha(grade, frete);
        u.Clicar(f, "Encerrar CIOT");
        Usuario.Esperar(300);
        u.Ok(LogisticaUtil.Ciot(frete) == "ENCERRADO", "CIOT encerrado após a entrega");
        u.Clicar(f, "Cancelar frete");
        u.Ok(LogisticaUtil.Status(frete) == "ENTREGUE", "frete entregue não é cancelado");
        f.Close();
    }
}

/// <summary>
/// Cancelamentos: CIOT com motivo (tela e camada de dados), viagem com CIOT cancelado não sai, frete cancelado não gera
/// contas nem CIOT (regressão), frete sem valor não marca contas como geradas (regressão), transportadora (CNPJ) não exige CIOT.
/// </summary>
public class LogisticaCancelamentos : ICenario
{
    public string Area => "Logística";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("ciot_administradora", "SIMULADOR");
        var tac = LogisticaUtil.Tac();
        var frete = LogisticaUtil.Frete(tac, 1500, 500);
        Fretes.DeclararCiot(frete).GetAwaiter().GetResult();
        Fretes.GerarContas(frete);

        var f = u.AbrirTela("FretesForm");
        var grade = Usuario.Achar<DataGridView>(f);
        ProducaoLogisticaUtil.SelecionarLinha(grade, frete);
        u.Clicar(f, "Cancelar frete");
        u.Ok(u.Viu("ERRO", "Cancele o CIOT") && LogisticaUtil.Status(frete) == "ABERTO", "frete com CIOT declarado só cancela depois do CIOT");

        var tentativas = 0;
        u.QuandoAbrirJanela("Cancelar CIOT", d =>
        {
            var motivo = ProducaoLogisticaUtil.CampoApos<TextBox>(d, "Motivo");
            motivo.Text = "desistiu";
            u.Clicar(d, "Cancelar CIOT");
            tentativas++;
            motivo.Text = "Cliente desistiu da carga antes da coleta";
            u.Clicar(d, "Cancelar CIOT");
            tentativas++;
        });
        u.Clicar(f, "Cancelar CIOT");
        Usuario.Esperar(400);
        u.ConferirJanelas();
        u.Ok(tentativas == 2 && u.Viu("ERRO", "15 caracteres") && LogisticaUtil.Ciot(frete) == "CANCELADO",
            "cancelar CIOT exige motivo com 15 caracteres; com motivo válido cancela");

        ProducaoLogisticaUtil.SelecionarLinha(grade, frete);
        u.Clicar(f, "Em trânsito");
        u.Ok(u.Viu("ERRO", "foi cancelado") && LogisticaUtil.Status(frete) == "ABERTO", "viagem com CIOT cancelado não é liberada");

        ProducaoLogisticaUtil.SelecionarLinha(grade, frete);
        u.Clicar(f, "Cancelar frete");
        u.Ok(LogisticaUtil.Status(frete) == "CANCELADO"
             && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM contas WHERE frete_id = @p0 AND status = 'ABERTA'", frete) == 0,
            "cancelar o frete cancela as contas abertas dele");
        f.Close();

        // frete cancelado: nem contas nem CIOT
        var cancelado = LogisticaUtil.Frete(tac, 900);
        Fretes.MudarStatus(cancelado, "CANCELADO");
        try { Fretes.GerarContas(cancelado); u.Ok(false, "frete cancelado gerou contas a pagar"); }
        catch (InvalidOperationException ex) { u.Ok(ex.Message.Contains("cancelado"), "frete cancelado não gera contas a pagar"); }
        try { Fretes.DeclararCiot(cancelado).GetAwaiter().GetResult(); u.Ok(false, "frete cancelado recebeu CIOT"); }
        catch (InvalidOperationException ex) { u.Ok(ex.Message.Contains("CANCELADO"), "frete cancelado não declara CIOT"); }

        // motivo curto direto na camada de dados
        var outro = LogisticaUtil.Frete(tac, 700);
        Fretes.DeclararCiot(outro).GetAwaiter().GetResult();
        try { Fretes.CancelarCiot(outro, "curto").GetAwaiter().GetResult(); u.Ok(false, "CIOT cancelado com motivo curto"); }
        catch (InvalidOperationException) { u.Ok(LogisticaUtil.Ciot(outro) == "DECLARADO", "camada de dados exige motivo de 15 caracteres"); }

        // transportadora (CNPJ): CIOT não se aplica; frete sem valor não marca contas como geradas
        var etc = LogisticaUtil.Frete(LogisticaUtil.Etc(), 0);
        u.Ok(LogisticaUtil.Ciot(etc) == "NAO_SE_APLICA", "transportadora (CNPJ) não exige CIOT");
        try { Fretes.GerarContas(etc); u.Ok(false, "frete sem valor 'gerou' contas"); }
        catch (InvalidOperationException ex) { u.Ok(ex.Message.Contains("valor"), "frete sem valor avisa que falta o valor"); }
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT contas_geradas FROM fretes WHERE id = @p0", etc) == 0, "frete sem valor continua podendo gerar contas depois");
        Fretes.MudarStatus(etc, "EM_TRANSITO");
        u.Ok(LogisticaUtil.Status(etc) == "EM_TRANSITO", "frete de transportadora sai sem CIOT");
    }
}

/// <summary>
/// Pamcard sem credenciais orienta a configurar; com resposta de sucesso sem número de CIOT o frete não fica
/// "declarado" sem CIOT (regressão); com resposta completa grava o CIOT. Transporte HTTP falso, nada vai à internet.
/// </summary>
public class LogisticaPamcard : ICenario
{
    public string Area => "Logística";

    public void Executar(Usuario u)
    {
        var frete = LogisticaUtil.Frete(LogisticaUtil.Tac(), 2500, 1000);
        Db.SalvarConfig("ciot_administradora", "PAMCARD");
        var f = u.AbrirTela("FretesForm");
        ProducaoLogisticaUtil.SelecionarLinha(Usuario.Achar<DataGridView>(f), frete);
        u.Clicar(f, "Declarar CIOT");
        Usuario.Esperar(300);
        u.Ok(u.Viu("ERRO", "não está configurada") && LogisticaUtil.Ciot(frete) == "PENDENTE", "Pamcard sem credenciais: orienta a configurar e nada muda");
        f.Close();

        var falsa = new PamcardFalsa { Resposta = "<retorno><sucesso>true</sucesso><mensagem>Recebido</mensagem></retorno>" };
        PamcardAdministradora.TransporteParaTestes = falsa;
        try
        {
            Db.SalvarConfig("pamcard_url", "https://homologacao.exemplo/pamcard");
            Db.SalvarConfig("pamcard_usuario", "usuario");
            Db.SalvarConfig("pamcard_senha", "senha");
            Db.SalvarConfig("pamcard_cnpj_contratante", "11.222.333/0001-81");
            var r = Fretes.DeclararCiot(frete).GetAwaiter().GetResult();
            u.Ok(!r.Sucesso && r.Mensagem.Contains("número do CIOT") && LogisticaUtil.Ciot(frete) == "PENDENTE",
                "resposta 'sucesso' sem número de CIOT não marca o frete como declarado: " + r.Mensagem);

            falsa.Resposta = "<retorno><sucesso>true</sucesso><ciot>987654321098</ciot><protocolo>P9</protocolo><mensagem>OK</mensagem></retorno>";
            r = Fretes.DeclararCiot(frete).GetAwaiter().GetResult();
            u.Ok(r.Sucesso && LogisticaUtil.Ciot(frete) == "DECLARADO" && Ui.Txt(Db.Scalar("SELECT ciot FROM fretes WHERE id = @p0", frete)) == "987654321098"
                 && Ui.Txt(Db.Scalar("SELECT ciot_administradora FROM fretes WHERE id = @p0", frete)) == "PAMCARD",
                "Pamcard com resposta completa grava o CIOT e a administradora");
            falsa.Resposta = "<retorno><sucesso>true</sucesso><mensagem>Cancelado</mensagem></retorno>";
            r = Fretes.CancelarCiot(frete, "Carga remarcada para a semana que vem").GetAwaiter().GetResult();
            u.Ok(r.Sucesso && LogisticaUtil.Ciot(frete) == "CANCELADO" && falsa.Chamadas == 3, "cancelamento do CIOT pela Pamcard");
        }
        finally
        {
            PamcardAdministradora.TransporteParaTestes = null;
        }
    }
}

/// <summary>
/// Cadastro de veículo pela tela: placa com hífen (regressão: a mesma placa entrava de dois jeitos), RENAVAM e RNTRC
/// inválidos (regressão: aceitava qualquer texto), placa repetida em minúsculas.
/// </summary>
public class LogisticaCadastraVeiculo : ICenario
{
    public string Area => "Logística";

    public void Executar(Usuario u)
    {
        var f = u.AbrirTela("cadastro_veiculos");
        var antes = ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM veiculos");
        u.Clicar(f, "Novo");
        var placa = ProducaoLogisticaUtil.CampoApos<TextBox>(f, "Placa");
        var renavam = ProducaoLogisticaUtil.CampoApos<TextBox>(f, "RENAVAM");
        var rntrc = ProducaoLogisticaUtil.CampoApos<TextBox>(f, "RNTRC do veículo");

        placa.Text = "QWE-1R23";
        u.Clicar(f, "Salvar");
        u.Ok(u.Viu("ERRO", "sem hífen"), "placa com hífen é recusada com orientação");
        placa.Text = "QWE1R23";
        renavam.Text = "12AB";
        u.Clicar(f, "Salvar");
        u.Ok(u.Viu("ERRO", "RENAVAM inválido"), "RENAVAM com letras é recusado");
        renavam.Text = "12345678901";
        rntrc.Text = "123";
        u.Clicar(f, "Salvar");
        u.Ok(u.Viu("ERRO", "RNTRC inválido"), "RNTRC curto é recusado");
        rntrc.Text = "12345678";
        u.Clicar(f, "Salvar");
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM veiculos") == antes + 1
             && Ui.Txt(Db.Scalar("SELECT renavam FROM veiculos WHERE placa = 'QWE1R23'")) == "12345678901",
            "veículo válido é salvo");

        u.Clicar(f, "Novo");
        ProducaoLogisticaUtil.CampoApos<TextBox>(f, "Placa").Text = "qwe1r23";
        var erros = u.Mensagens.Count(m => m.Tipo == "ERRO");
        u.Clicar(f, "Salvar");
        u.Ok(u.Mensagens.Count(m => m.Tipo == "ERRO") == erros + 1 && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM veiculos") == antes + 1,
            "a mesma placa em minúsculas não é cadastrada de novo");
        f.Close();
    }
}
