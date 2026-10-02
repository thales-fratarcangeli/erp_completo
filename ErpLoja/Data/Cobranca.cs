using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ErpLoja.Data;

/// <summary>Conta bancária usada para emitir boletos (tabela contas_bancarias).</summary>
public record ContaBancaria(long Id, string Nome, string Banco, string BancoNome, string Agencia, string AgenciaDv, string Conta,
    string ContaDv, string Carteira, string Convenio, string ModeloCampoLivre, int DigitosNossoNumero, string Especie,
    decimal MultaPct, decimal JurosMesPct, int DiasProtesto, string Instrucoes, bool Homologacao)
{
    public static ContaBancaria Carregar(long id)
    {
        var r = Db.Query("SELECT * FROM contas_bancarias WHERE id = @p0", id).Rows[0];
        return new ContaBancaria(id, Ui.Txt(r["nome"]), Fiscal.SoDigitos(Ui.Txt(r["banco_codigo"])).PadLeft(3, '0'), Ui.Txt(r["banco_nome"]),
            Fiscal.SoDigitos(Ui.Txt(r["agencia"])), Ui.Txt(r["agencia_dv"]), Fiscal.SoDigitos(Ui.Txt(r["conta"])), Ui.Txt(r["conta_dv"]),
            Fiscal.SoDigitos(Ui.Txt(r["carteira"])), Fiscal.SoDigitos(Ui.Txt(r["convenio"])), Ui.Txt(r["campo_livre_modelo"]),
            (int)Ui.Dec(r["nosso_numero_digitos"]), Ui.Txt(r["especie_titulo"]), Ui.Dec(r["multa_pct"]), Ui.Dec(r["juros_mes_pct"]),
            (int)Ui.Dec(r["dias_protesto"]), Ui.Txt(r["instrucoes"]), Ui.Txt(r["ambiente"]) == "HOMOLOGACAO");
    }
}

public record ResultadoRetorno(int Registros, int Confirmados, int Rejeitados, int Liquidados, decimal ValorLiquidado, int NaoEncontrados, List<string> Mensagens);

/// <summary>
/// Cobrança bancária genérica (qualquer banco):
/// - boleto no padrão Febraban: código de barras de 44 posições, linha digitável, fator de vencimento (com a virada de 22/02/2025);
/// - campo livre (25 posições, específico de cada banco) montado por um modelo configurável na conta bancária;
/// - remessa e retorno no layout CNAB 240 Febraban (segmentos P, Q, R / T, U), com validador e simulador de retorno para homologação.
/// </summary>
public static class Cobranca
{
    /// <summary>Exemplo de modelo (agência 4 + carteira 2 + nosso número 11 + conta 7 + zero = 25). Ajuste conforme o manual do banco.</summary>
    public const string ModeloCampoLivrePadrao = "{agencia:4}{carteira:2}{nossonumero:11}{conta:7}{zeros:1}";

    static readonly string[] Marcacoes = ["agencia", "agenciadv", "conta", "contadv", "carteira", "convenio", "nossonumero", "zeros"];
    static readonly Regex Marca = new(@"\{(\w+):(\d+)\}");

    // ------------------------------------------------------------------ campo livre

    /// <summary>Confere o modelo: só marcações conhecidas e dígitos fixos, totalizando 25 posições.</summary>
    public static string? ValidarModeloCampoLivre(string modelo)
    {
        var total = 0;
        foreach (Match m in Marca.Matches(modelo))
        {
            if (!Marcacoes.Contains(m.Groups[1].Value))
                return $"Marcação desconhecida: {{{m.Groups[1].Value}}}. Use: {string.Join(", ", Marcacoes.Select(x => "{" + x + ":N}"))}.";
            total += int.Parse(m.Groups[2].Value);
        }
        var fixos = Marca.Replace(modelo, "");
        if (fixos.Any(c => !char.IsDigit(c)))
            return "Fora das marcações só são aceitos dígitos fixos.";
        total += fixos.Length;
        return total == 25 ? null : $"O campo livre precisa ter 25 posições (o modelo tem {total}).";
    }

    public static string CampoLivre(ContaBancaria c, string nossoNumero)
    {
        string Valor(string nome) => nome switch
        {
            "agencia" => c.Agencia,
            "agenciadv" => Fiscal.SoDigitos(c.AgenciaDv),
            "conta" => c.Conta,
            "contadv" => Fiscal.SoDigitos(c.ContaDv),
            "carteira" => c.Carteira,
            "convenio" => c.Convenio,
            "nossonumero" => nossoNumero,
            _ => "",
        };
        var r = Marca.Replace(c.ModeloCampoLivre, m =>
        {
            var n = int.Parse(m.Groups[2].Value);
            var v = Valor(m.Groups[1].Value);
            return v.Length >= n ? v[^n..] : v.PadLeft(n, '0');
        });
        if (r.Length != 25 || !r.All(char.IsDigit))
            throw new InvalidOperationException("Modelo do campo livre inválido na conta bancária.");
        return r;
    }

    // ------------------------------------------------------------------ código de barras e linha digitável

    /// <summary>Fator de vencimento Febraban. A partir de 22/02/2025 a contagem reinicia em 1000.</summary>
    public static int FatorVencimento(DateTime vencimento)
    {
        var d = vencimento.Date;
        var virada = new DateTime(2025, 2, 22);
        if (d >= virada)
            return (d - virada).Days % 9000 + 1000;
        var fator = (d - new DateTime(1997, 10, 7)).Days;
        if (fator < 1000)
            throw new ArgumentOutOfRangeException(nameof(vencimento), "Vencimento anterior ao suportado pelo fator de vencimento.");
        return fator;
    }

    public static string CodigoBarras(string banco, DateTime vencimento, decimal valor, string campoLivre)
    {
        var centavos = (long)Math.Round(valor * 100, MidpointRounding.AwayFromZero);
        if (centavos < 0 || centavos > 9_999_999_999)
            throw new ArgumentOutOfRangeException(nameof(valor));
        var semDv = banco.PadLeft(3, '0') + "9" + FatorVencimento(vencimento).ToString("0000") + centavos.ToString("0000000000") + campoLivre;
        return semDv[..4] + DvCodigoBarras(semDv) + semDv[4..];
    }

    /// <summary>DV geral do código de barras: módulo 11 (pesos 2 a 9); resultados 0, 10 e 11 viram 1.</summary>
    public static int DvCodigoBarras(string numero43)
    {
        int soma = 0, peso = 2;
        for (int i = numero43.Length - 1; i >= 0; i--)
        {
            soma += (numero43[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var dv = 11 - soma % 11;
        return dv is 0 or 10 or 11 ? 1 : dv;
    }

    /// <summary>Módulo 10 (pesos 2 e 1 da direita para a esquerda) usado nos campos da linha digitável.</summary>
    public static int Modulo10(string numero)
    {
        int soma = 0, peso = 2;
        for (int i = numero.Length - 1; i >= 0; i--)
        {
            var p = (numero[i] - '0') * peso;
            soma += p > 9 ? p / 10 + p % 10 : p;
            peso = peso == 2 ? 1 : 2;
        }
        return (10 - soma % 10) % 10;
    }

    public static string LinhaDigitavel(string codigoBarras)
    {
        var livre = codigoBarras[19..44];
        var c1 = codigoBarras[..4] + livre[..5];
        c1 += Modulo10(c1);
        var c2 = livre[5..15];
        c2 += Modulo10(c2);
        var c3 = livre[15..25];
        c3 += Modulo10(c3);
        return $"{c1[..5]}.{c1[5..]} {c2[..5]}.{c2[5..]} {c3[..5]}.{c3[5..]} {codigoBarras[4]} {codigoBarras[5..19]}";
    }

    // ------------------------------------------------------------------ emissão

    /// <summary>Emite o boleto de uma conta a receber em aberto. Devolve o id do boleto.</summary>
    public static long Emitir(long contaBancariaId, long contaReceberId)
    {
        var banco = ContaBancaria.Carregar(contaBancariaId);
        using var s = new DbSessao(transacao: true);
        var conta = s.Query("SELECT tipo, status, valor, vencimento, cliente_id, descricao FROM contas WHERE id = @p0", contaReceberId)
            .Rows.Cast<DataRow>().FirstOrDefault() ?? throw new InvalidOperationException("Conta não encontrada.");
        if (Ui.Txt(conta["tipo"]) != "RECEBER" || Ui.Txt(conta["status"]) != "ABERTA")
            throw new InvalidOperationException("Só é possível emitir boleto de conta a receber em aberto.");
        if (s.Scalar("SELECT 1 FROM boletos WHERE conta_id = @p0 AND status NOT IN ('CANCELADO', 'BAIXADO', 'REJEITADO')", contaReceberId) != null)
            throw new InvalidOperationException("Esta conta já tem boleto ativo.");

        var sequencial = Convert.ToInt64(s.Scalar("SELECT nosso_numero_atual FROM contas_bancarias WHERE id = @p0", contaBancariaId)) + 1;
        var nossoNumero = sequencial.ToString().PadLeft(banco.DigitosNossoNumero, '0');
        var vencimento = Db.LerData(conta["vencimento"]);
        var valor = Ui.Dec(conta["valor"]);
        var codigo = CodigoBarras(banco.Banco, vencimento, valor, CampoLivre(banco, nossoNumero));
        s.Exec("UPDATE contas_bancarias SET nosso_numero_atual = @p0 WHERE id = @p1", sequencial, contaBancariaId);
        var id = s.Inserir("""
            INSERT INTO boletos(conta_bancaria_id, conta_id, pagador_id, nosso_numero, numero_documento, emissao, vencimento, valor,
                                codigo_barras, linha_digitavel)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)
            """, contaBancariaId, contaReceberId, conta["cliente_id"], nossoNumero, contaReceberId.ToString(), Db.Dia(DateTime.Today),
            Db.Dia(vencimento), valor, codigo, LinhaDigitavel(codigo));
        s.Commit();
        return id;
    }

    /// <summary>Boleto ainda não enviado: cancela. Já enviado/registrado: marca para a próxima remessa pedir a baixa ao banco.</summary>
    public static string CancelarOuBaixar(long boletoId)
    {
        var status = Convert.ToString(Db.Scalar("SELECT status FROM boletos WHERE id = @p0", boletoId));
        switch (status)
        {
            case "EMITIDO":
                Db.Exec("UPDATE boletos SET status = 'CANCELADO' WHERE id = @p0", boletoId);
                return "Boleto cancelado (ainda não tinha sido enviado ao banco).";
            case "ENVIADO" or "REGISTRADO":
                Db.Exec("UPDATE boletos SET baixa_solicitada = 1 WHERE id = @p0", boletoId);
                return "Pedido de baixa incluído na próxima remessa.";
            default:
                throw new InvalidOperationException($"Boleto {status}: nada a cancelar.");
        }
    }

    /// <summary>Texto da ficha de compensação (o código de barras é desenhado à parte).</summary>
    public static string Texto(long boletoId)
    {
        var b = Db.Query("""
            SELECT b.*, p.nome AS pagador, p.cpf_cnpj AS doc, p.endereco, p.cidade, p.uf
            FROM boletos b LEFT JOIN pessoas p ON p.id = b.pagador_id WHERE b.id = @p0
            """, boletoId).Rows[0];
        var c = ContaBancaria.Carregar((long)b["conta_bancaria_id"]);
        var sb = new StringBuilder();
        var traco = new string('-', 96);
        sb.AppendLine($"[N]{c.BancoNome} | {c.Banco} | {b["linha_digitavel"]}");
        sb.AppendLine(traco);
        sb.AppendLine($"Local de pagamento: pagável em qualquer banco até o vencimento        Vencimento: {Db.LerData(b["vencimento"]):dd/MM/yyyy}");
        sb.AppendLine($"Beneficiário: {Db.Config("nome_loja")} - CNPJ {Db.Config("cnpj")}");
        sb.AppendLine($"Agência/código do beneficiário: {c.Agencia}{(c.AgenciaDv.Length > 0 ? "-" + c.AgenciaDv : "")} / {c.Conta}{(c.ContaDv.Length > 0 ? "-" + c.ContaDv : "")}");
        sb.AppendLine($"Data do documento: {Db.LerData(b["emissao"]):dd/MM/yyyy}   Nº documento: {b["numero_documento"]}   Espécie: {c.Especie}   " +
                      $"Carteira: {c.Carteira}   Nosso número: {b["nosso_numero"]}");
        sb.AppendLine($"[N]Valor do documento: R$ {Ui.Dec(b["valor"]):N2}");
        sb.AppendLine(traco);
        var instrucoes = new List<string>();
        if (c.MultaPct > 0) instrucoes.Add($"Após o vencimento cobrar multa de {c.MultaPct:N2}%.");
        if (c.JurosMesPct > 0) instrucoes.Add($"Juros de {c.JurosMesPct:N2}% ao mês.");
        if (c.DiasProtesto > 0) instrucoes.Add($"Protestar após {c.DiasProtesto} dias do vencimento.");
        if (c.Instrucoes.Length > 0) instrucoes.Add(c.Instrucoes);
        sb.AppendLine("Instruções: " + (instrucoes.Count > 0 ? string.Join(" ", instrucoes) : "não receber após 60 dias do vencimento."));
        sb.AppendLine(traco);
        sb.AppendLine($"Pagador: {b["pagador"]}  CPF/CNPJ: {b["doc"]}");
        sb.AppendLine($"         {b["endereco"]} - {b["cidade"]}/{b["uf"]}");
        if (c.Homologacao)
            sb.AppendLine("[C][N]*** BOLETO DE HOMOLOGAÇÃO - SEM VALOR ***");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ CNAB 240: utilitários

    static string SemAcento(string s)
    {
        var n = s.Normalize(NormalizationForm.FormD);
        return new string(n.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC);
    }

    /// <summary>Campo numérico: só dígitos, zeros à esquerda, mantém os dígitos da direita se exceder.</summary>
    static string N(object? valor, int tamanho)
    {
        var d = Fiscal.SoDigitos(Convert.ToString(valor, CultureInfo.InvariantCulture));
        return d.Length >= tamanho ? d[^tamanho..] : d.PadLeft(tamanho, '0');
    }

    /// <summary>Valor monetário com 2 casas implícitas.</summary>
    static string V(decimal valor, int tamanho) => N((long)Math.Round(valor * 100, MidpointRounding.AwayFromZero), tamanho);

    /// <summary>Campo alfanumérico: maiúsculo, sem acento, espaços à direita.</summary>
    static string A(object? valor, int tamanho)
    {
        var t = SemAcento(Convert.ToString(valor) ?? "").ToUpperInvariant();
        return t.Length >= tamanho ? t[..tamanho] : t.PadRight(tamanho);
    }

    static string B(int tamanho) => new(' ', tamanho);

    static string Data(DateTime d) => d.ToString("ddMMyyyy");

    static string Pos(string linha, int inicio, int fim) => linha.Substring(inicio - 1, fim - inicio + 1);

    static (string Tipo, string Numero) InscricaoEmpresa()
    {
        var d = Fiscal.SoDigitos(Db.Config("cnpj"));
        return (d.Length == 11 ? "1" : "2", d);
    }

    // ------------------------------------------------------------------ CNAB 240: remessa

    /// <summary>Gera o arquivo de remessa com os boletos emitidos e os pedidos de baixa pendentes da conta.</summary>
    public static (long RemessaId, string Arquivo, string Conteudo, int Quantidade) GerarRemessa(long contaBancariaId)
    {
        var c = ContaBancaria.Carregar(contaBancariaId);
        using var s = new DbSessao(transacao: true);
        var boletos = s.Query("""
            SELECT b.*, p.nome AS pagador, p.cpf_cnpj AS doc, p.endereco, p.cidade, p.uf, p.cep
            FROM boletos b LEFT JOIN pessoas p ON p.id = b.pagador_id
            WHERE b.conta_bancaria_id = @p0 AND (b.status = 'EMITIDO' OR (b.baixa_solicitada = 1 AND b.status IN ('ENVIADO', 'REGISTRADO')))
            ORDER BY b.id
            """, contaBancariaId);
        if (boletos.Rows.Count == 0)
            throw new InvalidOperationException("Não há boletos novos nem pedidos de baixa para enviar.");

        var sequencia = Convert.ToInt32(s.Scalar("SELECT remessa_sequencia FROM contas_bancarias WHERE id = @p0", contaBancariaId)) + 1;
        var agora = DateTime.Now;
        var (tipoInscricao, inscricao) = InscricaoEmpresa();
        var empresa = Db.Config("nome_loja");
        var linhas = new List<string>();

        linhas.Add(c.Banco + "0000" + "0" + B(9) + tipoInscricao + N(inscricao, 14) + A(c.Convenio, 20) + N(c.Agencia, 5) + A(c.AgenciaDv, 1) +
                   N(c.Conta, 12) + A(c.ContaDv, 1) + " " + A(empresa, 30) + A(c.BancoNome, 30) + B(10) + "1" + Data(agora) +
                   agora.ToString("HHmmss") + N(sequencia, 6) + "103" + "00000" + B(20) + A(c.Homologacao ? "TESTE" : "", 20) + B(29));
        linhas.Add(c.Banco + "0001" + "1" + "R" + "01" + B(2) + "060" + " " + tipoInscricao + N(inscricao, 15) + A(c.Convenio, 20) +
                   N(c.Agencia, 5) + A(c.AgenciaDv, 1) + N(c.Conta, 12) + A(c.ContaDv, 1) + " " + A(empresa, 30) + B(40) + B(40) +
                   N(sequencia, 8) + Data(agora) + N(0, 8) + B(33));

        int seq = 0;
        decimal total = 0;
        foreach (DataRow b in boletos.Rows)
        {
            var baixa = Ui.Txt(b["status"]) != "EMITIDO";
            var movimento = baixa ? "02" : "01";
            var vencimento = Db.LerData(b["vencimento"]);
            var valor = Ui.Dec(b["valor"]);
            total += valor;
            var juros = c.JurosMesPct > 0;
            linhas.Add(c.Banco + "0001" + "3" + N(++seq, 5) + "P" + " " + movimento + N(c.Agencia, 5) + A(c.AgenciaDv, 1) + N(c.Conta, 12) +
                       A(c.ContaDv, 1) + " " + A(b["nosso_numero"], 20) + "1" + "1" + "1" + "2" + "2" + A(b["numero_documento"], 15) +
                       Data(vencimento) + V(valor, 15) + N(0, 5) + "0" + N(c.Especie, 2) + "N" + Data(Db.LerData(b["emissao"])) +
                       (juros ? "2" : "3") + (juros ? Data(vencimento.AddDays(1)) : N(0, 8)) + V(juros ? c.JurosMesPct : 0, 15) +
                       "0" + N(0, 8) + N(0, 15) + N(0, 15) + N(0, 15) + A(b["id"], 25) + (c.DiasProtesto > 0 ? "1" : "3") +
                       N(c.DiasProtesto, 2) + "1" + "060" + "09" + N(0, 10) + " ");
            var doc = Fiscal.SoDigitos(Ui.Txt(b["doc"]));
            var cep = Fiscal.SoDigitos(Ui.Txt(b["cep"])).PadLeft(8, '0');
            linhas.Add(c.Banco + "0001" + "3" + N(++seq, 5) + "Q" + " " + movimento + (doc.Length == 14 ? "2" : "1") + N(doc, 15) +
                       A(b["pagador"], 40) + A(b["endereco"], 40) + A("", 15) + cep[..5] + cep[5..] + A(b["cidade"], 15) + A(b["uf"], 2) +
                       "0" + N(0, 15) + B(40) + "000" + B(20) + B(8));
            linhas.Add(c.Banco + "0001" + "3" + N(++seq, 5) + "R" + " " + movimento + "0" + N(0, 8) + N(0, 15) + "0" + N(0, 8) + N(0, 15) +
                       (c.MultaPct > 0 ? "2" : "0") + (c.MultaPct > 0 ? Data(vencimento) : N(0, 8)) + V(c.MultaPct, 15) + B(10) +
                       A(c.Instrucoes, 40) + B(40) + B(20) + N(0, 8) + N(0, 3) + N(0, 5) + " " + N(0, 12) + " " + " " + "0" + B(9));
        }
        linhas.Add(c.Banco + "0001" + "5" + B(9) + N(seq + 2, 6) + N(boletos.Rows.Count, 6) + V(total, 17) + N(0, 6) + N(0, 17) +
                   N(0, 6) + N(0, 17) + N(0, 6) + N(0, 17) + B(8) + B(117));
        linhas.Add(c.Banco + "9999" + "9" + B(9) + N(1, 6) + N(linhas.Count + 1, 6) + N(0, 6) + B(205));

        var erros = ValidarArquivo240(string.Join("\r\n", linhas), remessa: true);
        if (erros.Count > 0)
            throw new InvalidOperationException("Remessa gerada com erro de layout:\n" + string.Join("\n", erros.Take(5)));

        var conteudo = string.Join("\r\n", linhas) + "\r\n";
        var arquivo = $"{(c.Homologacao ? "TESTE_" : "")}REM{c.Banco}_{agora:yyyyMMdd}_{sequencia:000000}.rem";
        var remessaId = s.Inserir("""
            INSERT INTO remessas(conta_bancaria_id, sequencia, data, arquivo, conteudo, quantidade, valor_total, homologacao)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)
            """, contaBancariaId, sequencia, agora, arquivo, conteudo, boletos.Rows.Count, total, c.Homologacao);
        s.Exec("UPDATE contas_bancarias SET remessa_sequencia = @p0 WHERE id = @p1", sequencia, contaBancariaId);
        foreach (DataRow b in boletos.Rows)
            s.Exec(Ui.Txt(b["status"]) == "EMITIDO"
                ? "UPDATE boletos SET status = 'ENVIADO', remessa_id = @p0 WHERE id = @p1"
                : "UPDATE boletos SET baixa_solicitada = 0, remessa_id = @p0 WHERE id = @p1", remessaId, b["id"]);
        s.Commit();
        return (remessaId, arquivo, conteudo, boletos.Rows.Count);
    }

    /// <summary>
    /// Validação estrutural de um arquivo CNAB 240 (remessa ou retorno): tamanho das linhas, ordem dos registros,
    /// lotes, quantidades e somatório informados nos trailers. Devolve a lista de problemas (vazia = ok).
    /// </summary>
    public static List<string> ValidarArquivo240(string conteudo, bool remessa)
    {
        var erros = new List<string>();
        var linhas = conteudo.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToList();
        if (linhas.Count < 4)
            return ["Arquivo muito curto: são necessários header de arquivo, header de lote, trailer de lote e trailer de arquivo."];
        for (int i = 0; i < linhas.Count; i++)
            if (linhas[i].Length != 240)
                erros.Add($"Linha {i + 1}: tem {linhas[i].Length} posições (o padrão é 240).");
        if (erros.Count > 0)
            return erros;

        string Tipo(string l) => Pos(l, 8, 8);
        if (Tipo(linhas[0]) != "0") erros.Add("A primeira linha deve ser o header de arquivo (tipo 0).");
        if (Tipo(linhas[^1]) != "9") erros.Add("A última linha deve ser o trailer de arquivo (tipo 9).");
        if (Pos(linhas[0], 143, 143) != (remessa ? "1" : "2")) erros.Add($"Header de arquivo: código {(remessa ? "1 (remessa)" : "2 (retorno)")} esperado na posição 143.");
        var banco = Pos(linhas[0], 1, 3);
        if (linhas.Any(l => Pos(l, 1, 3) != banco)) erros.Add("Há linhas com código de banco diferente do header.");

        int lotes = 0, i2 = 1;
        while (i2 < linhas.Count - 1)
        {
            if (Tipo(linhas[i2]) != "1")
            {
                erros.Add($"Linha {i2 + 1}: esperado header de lote (tipo 1).");
                break;
            }
            lotes++;
            var lote = Pos(linhas[i2], 4, 7);
            var inicio = i2;
            int titulos = 0;
            decimal soma = 0;
            i2++;
            while (i2 < linhas.Count - 1 && Tipo(linhas[i2]) == "3")
            {
                if (Pos(linhas[i2], 4, 7) != lote) erros.Add($"Linha {i2 + 1}: número de lote diferente do header do lote.");
                var segmento = Pos(linhas[i2], 14, 14);
                if (Pos(linhas[i2], 9, 13) != (i2 - inicio).ToString("00000")) erros.Add($"Linha {i2 + 1}: sequencial do registro no lote incorreto.");
                if (segmento is "P" or "T")
                {
                    titulos++;
                    soma += decimal.Parse(Pos(linhas[i2], segmento == "P" ? 86 : 82, segmento == "P" ? 100 : 96)) / 100;
                }
                i2++;
            }
            if (i2 >= linhas.Count - 1 || Tipo(linhas[i2]) != "5")
            {
                erros.Add($"Lote {lote}: trailer de lote (tipo 5) não encontrado.");
                break;
            }
            var qtdRegistros = int.Parse(Pos(linhas[i2], 18, 23));
            if (qtdRegistros != i2 - inicio + 1) erros.Add($"Lote {lote}: trailer informa {qtdRegistros} registros, o lote tem {i2 - inicio + 1}.");
            if (remessa)
            {
                if (int.Parse(Pos(linhas[i2], 24, 29)) != titulos) erros.Add($"Lote {lote}: quantidade de títulos do trailer não confere ({titulos}).");
                if (decimal.Parse(Pos(linhas[i2], 30, 46)) / 100 != soma) erros.Add($"Lote {lote}: valor total do trailer não confere ({soma:N2}).");
            }
            i2++;
        }
        if (int.Parse(Pos(linhas[^1], 18, 23)) != lotes) erros.Add($"Trailer de arquivo: quantidade de lotes não confere ({lotes}).");
        if (int.Parse(Pos(linhas[^1], 24, 29)) != linhas.Count) erros.Add($"Trailer de arquivo: quantidade de registros não confere ({linhas.Count}).");
        return erros;
    }

    // ------------------------------------------------------------------ CNAB 240: retorno

    static readonly Dictionary<string, string> Movimentos = new()
    {
        ["02"] = "Entrada confirmada",
        ["03"] = "Entrada rejeitada",
        ["06"] = "Liquidação",
        ["09"] = "Baixa",
        ["17"] = "Liquidação após baixa",
    };

    /// <summary>Lê o retorno CNAB 240: confirma entradas, registra rejeições, baixa títulos e liquida as contas a receber.</summary>
    public static ResultadoRetorno ProcessarRetorno(long contaBancariaId, string arquivo, string conteudo)
    {
        var erros = ValidarArquivo240(conteudo, remessa: false);
        if (erros.Count > 0)
            throw new InvalidOperationException("Arquivo de retorno inválido:\n" + string.Join("\n", erros.Take(5)));
        var c = ContaBancaria.Carregar(contaBancariaId);
        var linhas = conteudo.Replace("\r\n", "\n").Split('\n').Where(l => l.Length == 240).ToList();
        if (Pos(linhas[0], 1, 3) != c.Banco)
            throw new InvalidOperationException($"O retorno é do banco {Pos(linhas[0], 1, 3)}, e a conta selecionada é do banco {c.Banco}.");

        int registros = 0, confirmados = 0, rejeitados = 0, liquidados = 0, naoEncontrados = 0;
        decimal valorLiquidado = 0;
        var mensagens = new List<string>();
        using var s = new DbSessao(transacao: true);
        for (int i = 0; i < linhas.Count; i++)
        {
            var t = linhas[i];
            if (Pos(t, 8, 8) != "3" || Pos(t, 14, 14) != "T")
                continue;
            var u = i + 1 < linhas.Count && Pos(linhas[i + 1], 14, 14) == "U" ? linhas[i + 1] : null;
            registros++;
            var movimento = Pos(t, 16, 17);
            var nossoNumero = Pos(t, 38, 57).Trim();
            var usoEmpresa = Pos(t, 106, 130).Trim();
            var boleto = (long.TryParse(usoEmpresa, out var idInformado)
                             ? s.Query("SELECT id, conta_id, status FROM boletos WHERE id = @p0 AND conta_bancaria_id = @p1", idInformado, contaBancariaId)
                             : null)?.Rows.Cast<DataRow>().FirstOrDefault()
                         ?? s.Query("SELECT id, conta_id, status FROM boletos WHERE nosso_numero = @p0 AND conta_bancaria_id = @p1", nossoNumero, contaBancariaId)
                             .Rows.Cast<DataRow>().FirstOrDefault();
            var descricao = Movimentos.GetValueOrDefault(movimento, "Movimento " + movimento);
            if (boleto is null)
            {
                naoEncontrados++;
                mensagens.Add($"Nosso número {nossoNumero}: não encontrado ({descricao}).");
                continue;
            }
            var id = (long)boleto["id"];
            switch (movimento)
            {
                case "02":
                    s.Exec("UPDATE boletos SET status = 'REGISTRADO', ocorrencia = @p0 WHERE id = @p1 AND status = 'ENVIADO'", descricao, id);
                    confirmados++;
                    break;
                case "03":
                    s.Exec("UPDATE boletos SET status = 'REJEITADO', ocorrencia = @p0 WHERE id = @p1", $"{descricao}: motivos {Pos(t, 214, 223).Trim()}", id);
                    rejeitados++;
                    mensagens.Add($"Nosso número {nossoNumero}: entrada rejeitada (motivos {Pos(t, 214, 223).Trim()}).");
                    break;
                case "06" or "17":
                    var pago = u is null ? decimal.Parse(Pos(t, 82, 96)) / 100 : decimal.Parse(Pos(u, 78, 92)) / 100;
                    var data = u is null ? DateTime.Today : DateTime.ParseExact(Pos(u, 138, 145), "ddMMyyyy", CultureInfo.InvariantCulture);
                    s.Exec("UPDATE boletos SET status = 'PAGO', data_pagamento = @p0, valor_pago = @p1, ocorrencia = @p2 WHERE id = @p3",
                        Db.Dia(data), pago, descricao, id);
                    if (boleto["conta_id"] is long contaId)
                        s.Exec("""
                            UPDATE contas SET status = 'PAGA', data_pagamento = @p0, valor_pago = @p1, forma_pagamento = 'Boleto'
                            WHERE id = @p2 AND status = 'ABERTA'
                            """, Db.Dia(data), pago, contaId);
                    liquidados++;
                    valorLiquidado += pago;
                    break;
                case "09":
                    s.Exec("UPDATE boletos SET status = 'BAIXADO', ocorrencia = @p0 WHERE id = @p1 AND status <> 'PAGO'", descricao, id);
                    break;
                default:
                    s.Exec("UPDATE boletos SET ocorrencia = @p0 WHERE id = @p1", descricao, id);
                    break;
            }
        }
        s.Exec("""
            INSERT INTO retornos(conta_bancaria_id, data, arquivo, conteudo, registros, liquidados, valor_liquidado)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6)
            """, contaBancariaId, DateTime.Now, arquivo, conteudo, registros, liquidados, valorLiquidado);
        s.Commit();
        return new ResultadoRetorno(registros, confirmados, rejeitados, liquidados, valorLiquidado, naoEncontrados, mensagens);
    }

    /// <summary>
    /// Homologação: gera um retorno CNAB 240 como o banco devolveria para a remessa informada.
    /// Todos os títulos de entrada são confirmados (02) e, se liquidar = true, também pagos (06) na data de hoje.
    /// </summary>
    public static string SimularRetorno(long remessaId, bool liquidar)
    {
        var rem = Db.Query("SELECT * FROM remessas WHERE id = @p0", remessaId).Rows[0];
        var c = ContaBancaria.Carregar((long)rem["conta_bancaria_id"]);
        var boletos = Db.Query("SELECT * FROM boletos WHERE remessa_id = @p0 ORDER BY id", remessaId);
        var (tipoInscricao, inscricao) = InscricaoEmpresa();
        var hoje = DateTime.Today;
        var linhas = new List<string>
        {
            c.Banco + "0000" + "0" + B(9) + tipoInscricao + N(inscricao, 14) + A(c.Convenio, 20) + N(c.Agencia, 5) + A(c.AgenciaDv, 1) +
            N(c.Conta, 12) + A(c.ContaDv, 1) + " " + A(Db.Config("nome_loja"), 30) + A(c.BancoNome, 30) + B(10) + "2" + Data(DateTime.Now) +
            DateTime.Now.ToString("HHmmss") + N(rem["sequencia"], 6) + "103" + "00000" + B(20) + A("SIMULADO", 20) + B(29),
            c.Banco + "0001" + "1" + "T" + "01" + B(2) + "060" + " " + tipoInscricao + N(inscricao, 15) + A(c.Convenio, 20) + N(c.Agencia, 5) +
            A(c.AgenciaDv, 1) + N(c.Conta, 12) + A(c.ContaDv, 1) + " " + A(Db.Config("nome_loja"), 30) + B(40) + B(40) +
            N(rem["sequencia"], 8) + Data(DateTime.Now) + N(0, 8) + B(33),
        };
        int seq = 0;
        void Titulo(DataRow b, string movimento, decimal pago)
        {
            var valor = Ui.Dec(b["valor"]);
            linhas.Add(c.Banco + "0001" + "3" + N(++seq, 5) + "T" + " " + movimento + N(c.Agencia, 5) + A(c.AgenciaDv, 1) + N(c.Conta, 12) +
                       A(c.ContaDv, 1) + " " + A(b["nosso_numero"], 20) + "1" + A(b["numero_documento"], 15) + Data(Db.LerData(b["vencimento"])) +
                       V(valor, 15) + c.Banco + N(c.Agencia, 5) + " " + A(b["id"], 25) + "09" + "0" + N(0, 15) + B(40) + N(0, 10) +
                       N(0, 15) + B(10) + B(17));
            linhas.Add(c.Banco + "0001" + "3" + N(++seq, 5) + "U" + " " + movimento + N(0, 15) + N(0, 15) + N(0, 15) + N(0, 15) +
                       V(pago, 15) + V(pago, 15) + N(0, 15) + N(0, 15) + Data(hoje) + Data(hoje) + B(4) + N(0, 8) + N(0, 15) + B(30) +
                       "000" + B(20) + B(7));
        }
        foreach (DataRow b in boletos.Rows)
        {
            Titulo(b, "02", 0);
            if (liquidar)
                Titulo(b, "06", Ui.Dec(b["valor"]));
        }
        linhas.Add(c.Banco + "0001" + "5" + B(9) + N(seq + 2, 6) + N(0, 6) + N(0, 17) + N(0, 6) + N(0, 17) + N(0, 6) + N(0, 17) +
                   N(0, 6) + N(0, 17) + B(8) + B(117));
        linhas.Add(c.Banco + "9999" + "9" + B(9) + N(1, 6) + N(linhas.Count + 1, 6) + N(0, 6) + B(205));
        return string.Join("\r\n", linhas) + "\r\n";
    }
}
