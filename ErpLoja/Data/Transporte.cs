using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ErpLoja.Data;

/// <summary>Dados da operação de transporte enviados à administradora para gerar o CIOT.</summary>
public record OperacaoTransporte(long FreteId, string ContratanteCnpj, string ContratanteNome, string TransportadorDocumento,
    string TransportadorNome, string Rntrc, string MotoristaCpf, string MotoristaNome, string Placa, string OrigemCidade,
    string OrigemUf, string DestinoCidade, string DestinoUf, decimal ValorFrete, decimal ValorPedagio, decimal ValorAdiantamento,
    decimal PesoKg, string Mercadoria, DateTime Inicio, DateTime Fim);

public record RespostaCiot(bool Sucesso, string? Ciot, string? Protocolo, string Mensagem);

/// <summary>Administradora de meios de pagamento de frete habilitada pela ANTT (gera e encerra o CIOT).</summary>
public interface IAdministradoraCiot
{
    string Nome { get; }
    Task<RespostaCiot> Declarar(OperacaoTransporte operacao);
    Task<RespostaCiot> Encerrar(string ciot, decimal pesoEntregueKg);
    Task<RespostaCiot> Cancelar(string ciot, string motivo);
}

/// <summary>Simulador: gera CIOT fictício para treinar e testar o fluxo sem contrato com administradora.</summary>
public class CiotSimulador : IAdministradoraCiot
{
    public string Nome => "SIMULADOR";

    public Task<RespostaCiot> Declarar(OperacaoTransporte op)
    {
        var ciot = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString() + RandomNumberGenerator.GetInt32(100_000, 999_999) + "0001";
        return Task.FromResult(new RespostaCiot(true, ciot, "SIM" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            "CIOT SIMULADO gerado (sem validade legal)."));
    }

    public Task<RespostaCiot> Encerrar(string ciot, decimal peso) =>
        Task.FromResult(new RespostaCiot(true, ciot, "SIMENC" + DateTime.Now.ToString("HHmmss"), "Operação encerrada (simulada)."));

    public Task<RespostaCiot> Cancelar(string ciot, string motivo) =>
        Task.FromResult(new RespostaCiot(true, ciot, "SIMCAN" + DateTime.Now.ToString("HHmmss"), "Operação cancelada (simulada)."));
}

/// <summary>
/// Adaptador Pamcard. A troca de mensagens está isolada em Montar*/Interpretar: o layout exato (nomes de campos, envelope e
/// endereços de homologação/produção) é o do manual técnico entregue pela Pamcard no credenciamento e deve ser conferido
/// na homologação. Credenciais em Configurações > Transporte e CIOT.
/// </summary>
public class PamcardAdministradora : IAdministradoraCiot
{
    public static HttpMessageHandler? TransporteParaTestes { get; set; }

    public string Nome => "PAMCARD";

    static (string Url, string Usuario, string Senha, string Cnpj) Credenciais()
    {
        var url = Db.Config("pamcard_url");
        var usuario = Db.Config("pamcard_usuario");
        var senha = Db.Config("pamcard_senha");
        var cnpj = Fiscal.SoDigitos(Db.Config("pamcard_cnpj_contratante"));
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha) || cnpj.Length != 14)
            throw new InvalidOperationException(
                "A integração com a Pamcard não está configurada. Preencha endereço, usuário, senha e CNPJ do contratante em " +
                "Configurações > Transporte e CIOT (dados fornecidos pela Pamcard no credenciamento), ou use o SIMULADOR.");
        return (url, usuario, senha, cnpj);
    }

    static string D(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Mensagem de declaração da operação de transporte.</summary>
    public static XElement MontarDeclaracao(OperacaoTransporte op, string cnpjContratante) =>
        new("declararOperacaoTransporte",
            new XElement("contratante", new XElement("cnpj", cnpjContratante), new XElement("nome", op.ContratanteNome)),
            new XElement("contratado",
                new XElement("documento", Fiscal.SoDigitos(op.TransportadorDocumento)),
                new XElement("nome", op.TransportadorNome),
                new XElement("rntrc", Fiscal.SoDigitos(op.Rntrc))),
            new XElement("motorista", new XElement("cpf", Fiscal.SoDigitos(op.MotoristaCpf)), new XElement("nome", op.MotoristaNome)),
            new XElement("veiculo", new XElement("placa", op.Placa.ToUpperInvariant().Replace("-", ""))),
            new XElement("origem", new XElement("municipio", op.OrigemCidade), new XElement("uf", op.OrigemUf)),
            new XElement("destino", new XElement("municipio", op.DestinoCidade), new XElement("uf", op.DestinoUf)),
            new XElement("valores",
                new XElement("frete", D(op.ValorFrete)),
                new XElement("pedagio", D(op.ValorPedagio)),
                new XElement("adiantamento", D(op.ValorAdiantamento))),
            new XElement("carga", new XElement("pesoKg", D(op.PesoKg)), new XElement("natureza", op.Mercadoria)),
            new XElement("periodo", new XElement("inicio", op.Inicio.ToString("yyyy-MM-dd")), new XElement("fim", op.Fim.ToString("yyyy-MM-dd"))),
            new XElement("referencia", op.FreteId));

    static RespostaCiot Interpretar(string corpo)
    {
        try
        {
            var x = XDocument.Parse(corpo).Root!;
            string? Campo(string nome) => x.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(nome, StringComparison.OrdinalIgnoreCase))?.Value;
            var ciot = Campo("ciot");
            var mensagem = Campo("mensagem") ?? Campo("message") ?? "";
            var sucesso = (Campo("sucesso") ?? Campo("status") ?? "").Trim().ToLowerInvariant() is "true" or "ok" or "sucesso" or "1";
            return new RespostaCiot(sucesso && !string.IsNullOrEmpty(ciot ?? "x"), ciot, Campo("protocolo"), mensagem);
        }
        catch (Exception ex)
        {
            return new RespostaCiot(false, null, null, "Resposta da Pamcard não reconhecida: " + ex.Message);
        }
    }

    static async Task<RespostaCiot> Enviar(string operacao, XElement corpo)
    {
        var (url, usuario, senha, _) = Credenciais();
        using var http = TransporteParaTestes is null ? new HttpClient() : new HttpClient(TransporteParaTestes, disposeHandler: false);
        http.Timeout = TimeSpan.FromSeconds(40);
        using var req = new HttpRequestMessage(HttpMethod.Post, url.TrimEnd('/') + "/" + operacao)
        {
            Content = new StringContent(corpo.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "application/xml"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{senha}")));
        try
        {
            using var resp = await http.SendAsync(req);
            var texto = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new RespostaCiot(false, null, null, $"Pamcard respondeu {(int)resp.StatusCode}: {Interpretar(texto).Mensagem}");
            return Interpretar(texto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new RespostaCiot(false, null, null, "Não foi possível falar com a Pamcard: " + ex.Message);
        }
    }

    public Task<RespostaCiot> Declarar(OperacaoTransporte op) => Enviar("declarar", MontarDeclaracao(op, Credenciais().Cnpj));

    public Task<RespostaCiot> Encerrar(string ciot, decimal peso) =>
        Enviar("encerrar", new XElement("encerrarOperacaoTransporte", new XElement("ciot", ciot), new XElement("pesoKg", D(peso))));

    public Task<RespostaCiot> Cancelar(string ciot, string motivo) =>
        Enviar("cancelar", new XElement("cancelarOperacaoTransporte", new XElement("ciot", ciot), new XElement("motivo", motivo)));
}

/// <summary>Administradoras ainda não integradas: ficam selecionáveis e explicam o que falta.</summary>
public class AdministradoraNaoIntegrada(string nome) : IAdministradoraCiot
{
    public string Nome => nome;
    Task<RespostaCiot> Pendente() => Task.FromResult(new RespostaCiot(false, null, null,
        $"A integração com {nome} ainda não foi implementada. Use PAMCARD ou SIMULADOR em Configurações > Transporte e CIOT."));
    public Task<RespostaCiot> Declarar(OperacaoTransporte op) => Pendente();
    public Task<RespostaCiot> Encerrar(string ciot, decimal peso) => Pendente();
    public Task<RespostaCiot> Cancelar(string ciot, string motivo) => Pendente();
}

/// <summary>Fretes: regras do CIOT, contas a pagar ao transportador e situação da viagem.</summary>
public static class Fretes
{
    public static IAdministradoraCiot Administradora(string? nome = null) => (nome ?? Parametros.CiotAdministradora) switch
    {
        "SIMULADOR" => new CiotSimulador(),
        "PAMCARD" => new PamcardAdministradora(),
        var outra => new AdministradoraNaoIntegrada(outra),
    };

    /// <summary>Placa no padrão Mercosul (ABC1D23) ou antigo (ABC1234), sem hífen (evita a mesma placa gravada de dois jeitos).</summary>
    public static string? ValidarPlaca(string placa) =>
        placa.Contains('-') || placa.Contains(' ') ? "Digite a placa sem hífen ou espaço (ex.: ABC1D23)." :
        System.Text.RegularExpressions.Regex.IsMatch(placa.ToUpperInvariant(), "^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")
            ? null : "Placa inválida. Use o padrão Mercosul (ABC1D23) ou o antigo (ABC1234).";

    /// <summary>RENAVAM: só números, 9 a 11 dígitos (os antigos têm 9).</summary>
    public static string? ValidarRenavam(string renavam) =>
        renavam.All(char.IsDigit) && renavam.Length is >= 9 and <= 11 ? null : "RENAVAM inválido: informe só os números (9 a 11 dígitos), como no documento do veículo.";

    /// <summary>RNTRC (registro na ANTT): 8 ou 9 dígitos; aceita pontos, hífen e espaços.</summary>
    public static string? ValidarRntrc(string rntrc)
    {
        var digitos = Fiscal.SoDigitos(rntrc);
        return digitos.Length is 8 or 9 && rntrc.All(c => char.IsDigit(c) || c is '.' or '-' or ' ' or '/')
            ? null : "RNTRC inválido: são 8 ou 9 dígitos (consulte no registro da ANTT).";
    }

    /// <summary>
    /// CIOT obrigatório (regra simplificada da ANTT): frete contratado de transportador autônomo (pessoa física, TAC)
    /// ou equiparado — transportador com CPF. Frota própria e transportadora (ETC, CNPJ) não exigem.
    /// </summary>
    public static bool CiotObrigatorio(string modalidade, string? documentoTransportador) =>
        modalidade == "CONTRATADO" && Fiscal.SoDigitos(documentoTransportador).Length == 11;

    /// <summary>Monta a operação a partir do frete e devolve os problemas que impedem a declaração.</summary>
    public static (OperacaoTransporte? Operacao, List<string> Erros) MontarOperacao(long freteId)
    {
        var f = Db.Query("""
            SELECT f.*, t.nome AS t_nome, t.cpf_cnpj AS t_doc, t.rntrc AS t_rntrc, m.nome AS m_nome, m.cpf_cnpj AS m_doc,
                   v.placa, v.rntrc AS v_rntrc
            FROM fretes f
            LEFT JOIN pessoas t ON t.id = f.transportador_id
            LEFT JOIN pessoas m ON m.id = f.motorista_id
            LEFT JOIN veiculos v ON v.id = f.veiculo_id
            WHERE f.id = @p0
            """, freteId).Rows[0];
        var erros = new List<string>();
        string T(string c) => Ui.Txt(f[c]);
        var rntrc = T("t_rntrc") is { Length: > 0 } r ? r : T("v_rntrc");
        if (T("t_nome").Length == 0) erros.Add("Informe o transportador.");
        if (!Fiscal.DocumentoValido(T("t_doc")) || Fiscal.SoDigitos(T("t_doc")).Length == 0) erros.Add("CPF/CNPJ do transportador inválido.");
        if (Fiscal.SoDigitos(rntrc).Length < 8) erros.Add("RNTRC do transportador (ou do veículo) não informado.");
        if (Fiscal.SoDigitos(T("m_doc")).Length != 11 || !Fiscal.DocumentoValido(T("m_doc"))) erros.Add("CPF do motorista inválido.");
        if (T("placa").Length == 0) erros.Add("Informe o veículo.");
        if (T("origem_cidade").Length == 0 || T("origem_uf").Length == 0) erros.Add("Informe a origem (cidade e UF).");
        if (T("destino_cidade").Length == 0 || T("destino_uf").Length == 0) erros.Add("Informe o destino (cidade e UF).");
        if (Ui.Dec(f["valor_frete"]) <= 0) erros.Add("Informe o valor do frete.");
        if (Ui.Dec(f["valor_adiantamento"]) > Ui.Dec(f["valor_frete"])) erros.Add("O adiantamento não pode ser maior que o frete.");
        if (erros.Count > 0)
            return (null, erros);
        var inicio = Db.LerData(f["data"]);
        var fim = f["previsao_entrega"] is string p ? Db.LerData(p) : inicio.AddDays(7);
        return (new OperacaoTransporte(freteId, Fiscal.SoDigitos(Db.Config("cnpj")), Db.Config("nome_loja"), T("t_doc"), T("t_nome"),
            rntrc, T("m_doc"), T("m_nome"), T("placa"), T("origem_cidade"), T("origem_uf"), T("destino_cidade"), T("destino_uf"),
            Ui.Dec(f["valor_frete"]), Ui.Dec(f["valor_pedagio"]), Ui.Dec(f["valor_adiantamento"]), Ui.Dec(f["peso_kg"]),
            T("mercadoria"), inicio, fim), erros);
    }

    public static async Task<RespostaCiot> DeclararCiot(long freteId)
    {
        var atual = Db.Query("SELECT status, ciot_status FROM fretes WHERE id = @p0", freteId).Rows[0];
        if (Ui.Txt(atual["ciot_status"]) is "DECLARADO" or "ENCERRADO")
            throw new InvalidOperationException("Este frete já tem CIOT declarado.");
        if (Ui.Txt(atual["status"]) is "ENTREGUE" or "CANCELADO")
            throw new InvalidOperationException($"O frete está {Ui.Txt(atual["status"])}: não é possível declarar CIOT para ele.");
        var (op, erros) = MontarOperacao(freteId);
        if (op is null)
            throw new InvalidOperationException("Corrija antes de declarar o CIOT:\n" + string.Join("\n", erros));
        var adm = Administradora();
        var resp = await adm.Declarar(op);
        // sem o número do CIOT a declaração não serve para a viagem: trata como falha
        if (resp.Sucesso && string.IsNullOrWhiteSpace(resp.Ciot))
            resp = resp with { Sucesso = false, Mensagem = $"{adm.Nome} não devolveu o número do CIOT. Confira no portal da administradora antes de declarar de novo. {resp.Mensagem}".Trim() };
        Db.Exec("""
            UPDATE fretes SET ciot = COALESCE(@p0, ciot), ciot_protocolo = COALESCE(@p1, ciot_protocolo), ciot_mensagem = @p2,
                   ciot_administradora = @p3, ciot_status = CASE WHEN @p4 = 1 THEN 'DECLARADO' ELSE ciot_status END
            WHERE id = @p5
            """, resp.Ciot, resp.Protocolo, resp.Mensagem, adm.Nome, resp.Sucesso, freteId);
        return resp;
    }

    public static async Task<RespostaCiot> EncerrarCiot(long freteId)
    {
        var f = Db.Query("SELECT ciot, ciot_status, ciot_administradora, peso_kg FROM fretes WHERE id = @p0", freteId).Rows[0];
        if (Ui.Txt(f["ciot_status"]) != "DECLARADO")
            throw new InvalidOperationException("Só é possível encerrar um CIOT declarado.");
        var resp = await Administradora(Ui.Txt(f["ciot_administradora"])).Encerrar(Ui.Txt(f["ciot"]), Ui.Dec(f["peso_kg"]));
        Db.Exec("UPDATE fretes SET ciot_mensagem = @p0, ciot_status = CASE WHEN @p1 = 1 THEN 'ENCERRADO' ELSE ciot_status END WHERE id = @p2",
            resp.Mensagem, resp.Sucesso, freteId);
        return resp;
    }

    public static async Task<RespostaCiot> CancelarCiot(long freteId, string motivo)
    {
        var f = Db.Query("SELECT ciot, ciot_status, ciot_administradora FROM fretes WHERE id = @p0", freteId).Rows[0];
        if (Ui.Txt(f["ciot_status"]) != "DECLARADO")
            throw new InvalidOperationException("Só é possível cancelar um CIOT declarado (e ainda não encerrado).");
        if ((motivo ?? "").Trim().Length < 15)
            throw new InvalidOperationException("Descreva o motivo do cancelamento do CIOT (pelo menos 15 caracteres).");
        motivo = motivo!.Trim();
        var resp = await Administradora(Ui.Txt(f["ciot_administradora"])).Cancelar(Ui.Txt(f["ciot"]), motivo);
        Db.Exec("UPDATE fretes SET ciot_mensagem = @p0, ciot_status = CASE WHEN @p1 = 1 THEN 'CANCELADO' ELSE ciot_status END WHERE id = @p2",
            resp.Mensagem, resp.Sucesso, freteId);
        return resp;
    }

    /// <summary>
    /// Contas a pagar do frete contratado: adiantamento (vencimento na data do frete) e saldo
    /// (frete − adiantamento + pedágio + outras despesas, na previsão de entrega). Só uma vez por frete.
    /// </summary>
    public static int GerarContas(long freteId)
    {
        using var s = new DbSessao(transacao: true);
        var f = s.Query("SELECT * FROM fretes WHERE id = @p0", freteId).Rows[0];
        if (Ui.Dec(f["contas_geradas"]) == 1)
            throw new InvalidOperationException("As contas deste frete já foram geradas.");
        if (Ui.Txt(f["status"]) == "CANCELADO")
            throw new InvalidOperationException("O frete está cancelado: não gera contas a pagar.");
        if (Ui.Txt(f["modalidade"]) != "CONTRATADO" || f["transportador_id"] is not long transportador)
            throw new InvalidOperationException("Contas a pagar só para frete contratado com transportador informado.");
        var adiantamento = Ui.Dec(f["valor_adiantamento"]);
        var saldo = Ui.Dec(f["valor_frete"]) - adiantamento + Ui.Dec(f["valor_pedagio"]) + Ui.Dec(f["outras_despesas"]);
        if (adiantamento <= 0 && saldo <= 0)
            throw new InvalidOperationException("O frete não tem valor a pagar. Informe o valor do frete (Abrir / editar) antes de gerar as contas.");
        var data = Db.LerData(f["data"]);
        var entrega = f["previsao_entrega"] is string p ? Db.LerData(p) : data.AddDays(Parametros.PrazoVencimentoDias);
        int n = 0;
        foreach (var (descricao, valor, vencimento) in new[] { ($"Frete nº {freteId} - adiantamento", adiantamento, data), ($"Frete nº {freteId} - saldo", saldo, entrega) })
        {
            if (valor <= 0)
                continue;
            s.Exec("""
                INSERT INTO contas(tipo, descricao, valor, vencimento, status, fornecedor_id, frete_id)
                VALUES('PAGAR', @p0, @p1, @p2, 'ABERTA', @p3, @p4)
                """, descricao, valor, Db.Dia(vencimento), transportador, freteId);
            n++;
        }
        s.Exec("UPDATE fretes SET contas_geradas = 1 WHERE id = @p0", freteId);
        s.Commit();
        return n;
    }

    public static void MudarStatus(long freteId, string novo)
    {
        var atual = Convert.ToString(Db.Scalar("SELECT status FROM fretes WHERE id = @p0", freteId));
        var permitido = (atual, novo) switch
        {
            ("ABERTO", "EM_TRANSITO") or ("EM_TRANSITO", "ENTREGUE") or ("ABERTO", "ENTREGUE") => true,
            ("ABERTO", "CANCELADO") or ("EM_TRANSITO", "CANCELADO") => true,
            _ => false,
        };
        if (!permitido)
            throw new InvalidOperationException($"Não é possível passar de {atual} para {novo}.");
        var ciot = Convert.ToString(Db.Scalar("SELECT ciot_status FROM fretes WHERE id = @p0", freteId));
        if (novo == "CANCELADO" && ciot == "DECLARADO")
            throw new InvalidOperationException("Cancele o CIOT antes de cancelar o frete.");
        if (novo is "EM_TRANSITO" or "ENTREGUE" && ciot is "PENDENTE" or "CANCELADO")
            throw new InvalidOperationException(
                (ciot == "CANCELADO" ? "O CIOT deste frete foi cancelado. " : "Este frete exige CIOT (transportador autônomo contratado) e ele ainda não foi declarado. ") +
                "Use \"Declarar CIOT\" antes de liberar a viagem (ou cancele o frete).");
        Db.Exec("UPDATE fretes SET status = @p0, data_entrega = CASE WHEN @p0 = 'ENTREGUE' THEN @p1 ELSE data_entrega END WHERE id = @p2",
            novo, Db.Dia(DateTime.Today), freteId);
        if (novo == "CANCELADO")
            Db.Exec("UPDATE contas SET status = 'CANCELADA' WHERE frete_id = @p0 AND status = 'ABERTA'", freteId);
    }
}
