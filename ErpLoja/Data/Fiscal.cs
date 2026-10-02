using System.Security.Cryptography;
using System.Text;

namespace ErpLoja.Data;

public class NotaItem
{
    [Browsable(false)] public long? ProdutoId { get; set; }
    [DisplayName("Código")] public string Codigo { get; set; } = "";
    [DisplayName("Descrição")] public string Descricao { get; set; } = "";
    [DisplayName("NCM")] public string Ncm { get; set; } = "";
    [DisplayName("CFOP")] public string Cfop { get; set; } = "";
    [DisplayName("CST/CSOSN")] public string Cst { get; set; } = "";
    [DisplayName("Un.")] public string Unidade { get; set; } = "UN";
    [DisplayName("Qtd.")] public decimal Quantidade { get; set; }
    [DisplayName("Vlr. unit.")] public decimal ValorUnitario { get; set; }
    [DisplayName("Desconto")] public decimal Desconto { get; set; }
    [DisplayName("Total")] public decimal ValorTotal => Math.Round(Quantidade * ValorUnitario - Desconto, 2);
    [DisplayName("% ICMS")] public decimal AliqIcms { get; set; }
    [DisplayName("ICMS")] public decimal ValorIcms => Imposto(AliqIcms);
    [DisplayName("% IPI")] public decimal AliqIpi { get; set; }
    [DisplayName("IPI")] public decimal ValorIpi => Imposto(AliqIpi);
    [DisplayName("% PIS")] public decimal AliqPis { get; set; }
    [Browsable(false)] public decimal ValorPis => Imposto(AliqPis);
    [DisplayName("% COFINS")] public decimal AliqCofins { get; set; }
    [Browsable(false)] public decimal ValorCofins => Imposto(AliqCofins);

    decimal Imposto(decimal aliquota) => Math.Round(ValorTotal * aliquota / 100, 2);
}

public class NotaFiscal
{
    public long? Id { get; set; }
    public string Tipo { get; set; } = Fiscal.Saida;
    public string Modelo { get; set; } = "55";
    public int Serie { get; set; } = 1;
    public long? Numero { get; set; }
    public string? Chave { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Now;
    public string Natureza { get; set; } = "";
    public long? ClienteId { get; set; }
    public long? FornecedorId { get; set; }
    public long? VendaId { get; set; }
    public long? CompraId { get; set; }
    public string PartNome { get; set; } = "";
    public string PartDocumento { get; set; } = "";
    public string PartIe { get; set; } = "";
    public string PartEndereco { get; set; } = "";
    public string PartCidade { get; set; } = "";
    public string PartUf { get; set; } = "";
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public string Info { get; set; } = "";
    public string Status { get; set; } = "DIGITADA";
    public BindingList<NotaItem> Itens { get; } = new();

    public decimal ValorProdutos => Itens.Sum(i => Math.Round(i.Quantidade * i.ValorUnitario, 2));
    public decimal DescontoTotal => Itens.Sum(i => i.Desconto) + Desconto;
    public decimal ValorIcms => Itens.Sum(i => i.ValorIcms);
    public decimal ValorIpi => Itens.Sum(i => i.ValorIpi);
    public decimal ValorPis => Itens.Sum(i => i.ValorPis);
    public decimal ValorCofins => Itens.Sum(i => i.ValorCofins);
    public decimal ValorTotal => Math.Max(0, ValorProdutos - DescontoTotal + Frete + ValorIpi);
}

/// <summary>
/// Notas fiscais: montagem a partir de vendas/compras, cálculo de impostos, numeração, chave de acesso e DANFE.
/// A transmissão à SEFAZ (assinatura com certificado A1) não está implementada: a nota fica "EMITIDA" apenas no sistema.
/// </summary>
public static class Fiscal
{
    public const string Saida = "SAIDA", Entrada = "ENTRADA";

    static readonly Dictionary<string, string> CodigosUf = new()
    {
        ["RO"] = "11", ["AC"] = "12", ["AM"] = "13", ["RR"] = "14", ["PA"] = "15", ["AP"] = "16", ["TO"] = "17",
        ["MA"] = "21", ["PI"] = "22", ["CE"] = "23", ["RN"] = "24", ["PB"] = "25", ["PE"] = "26", ["AL"] = "27",
        ["SE"] = "28", ["BA"] = "29", ["MG"] = "31", ["ES"] = "32", ["RJ"] = "33", ["SP"] = "35", ["PR"] = "41",
        ["SC"] = "42", ["RS"] = "43", ["MS"] = "50", ["MT"] = "51", ["GO"] = "52", ["DF"] = "53",
    };

    public static string[] Ufs => CodigosUf.Keys.Order().ToArray();

    public static string SoDigitos(string? s) => new((s ?? "").Where(char.IsDigit).ToArray());

    /// <summary>5102/6102 (venda) ou 1102/2102 (compra) conforme o participante esteja na mesma UF da loja.</summary>
    public static string CfopPadrao(string tipo, string? ufParticipante)
    {
        var ufLoja = Db.Config("uf");
        var foraDoEstado = !string.IsNullOrWhiteSpace(ufParticipante) && !string.IsNullOrWhiteSpace(ufLoja) &&
                           !ufParticipante.Equals(ufLoja, StringComparison.OrdinalIgnoreCase);
        return tipo == Saida ? (foraDoEstado ? "6102" : "5102") : (foraDoEstado ? "2102" : "1102");
    }

    public static NotaFiscal Nova(string tipo)
    {
        var modelo = "55";
        return new NotaFiscal
        {
            Tipo = tipo,
            Modelo = modelo,
            Serie = int.TryParse(Db.Config("serie_" + modelo), out var serie) ? serie : 1,
            Natureza = tipo == Saida ? Parametros.NaturezaVenda : Parametros.NaturezaCompra,
        };
    }

    public static void PreencherParticipante(NotaFiscal n, long? id)
    {
        var cliente = n.Tipo == Saida;
        n.ClienteId = cliente ? id : null;
        n.FornecedorId = cliente ? null : id;
        if (id is null)
        {
            n.PartNome = cliente ? "Consumidor final" : "";
            n.PartDocumento = n.PartIe = n.PartEndereco = n.PartCidade = n.PartUf = "";
            return;
        }
        var r = Db.Query(cliente
            ? "SELECT nome, cpf_cnpj AS doc, ie, endereco, cidade, uf FROM clientes WHERE id = @p0"
            : "SELECT nome, cnpj AS doc, ie, endereco, cidade, uf FROM fornecedores WHERE id = @p0", id).Rows[0];
        n.PartNome = Ui.Txt(r["nome"]);
        n.PartDocumento = Ui.Txt(r["doc"]);
        n.PartIe = Ui.Txt(r["ie"]);
        n.PartEndereco = Ui.Txt(r["endereco"]);
        n.PartCidade = Ui.Txt(r["cidade"]);
        n.PartUf = Ui.Txt(r["uf"]).ToUpper();
    }

    public static NotaItem ItemDeProduto(long produtoId, decimal quantidade, decimal unitario, string cfop)
    {
        var p = Db.Query("SELECT * FROM produtos WHERE id = @p0", produtoId).Rows[0];
        return new NotaItem
        {
            ProdutoId = produtoId,
            Codigo = Ui.Txt(p["codigo"]),
            Descricao = Ui.Txt(p["nome"]),
            Ncm = Ui.Txt(p["ncm"]),
            Cfop = cfop,
            Cst = Ui.Txt(p["cst"]),
            Unidade = Ui.Txt(p["unidade"]),
            Quantidade = quantidade,
            ValorUnitario = unitario,
            AliqIcms = Ui.Dec(p["aliq_icms"]),
            AliqIpi = Ui.Dec(p["aliq_ipi"]),
            AliqPis = Ui.Dec(p["aliq_pis"]),
            AliqCofins = Ui.Dec(p["aliq_cofins"]),
        };
    }

    /// <summary>Id da nota (não cancelada) já gerada para a venda/compra, se houver.</summary>
    public static long? NotaExistente(string coluna, long id) =>
        Db.Scalar($"SELECT id FROM notas_fiscais WHERE {coluna} = @p0 AND status <> 'CANCELADA' LIMIT 1", id) as long?;

    public static NotaFiscal DeVenda(long vendaId)
    {
        var v = Db.Query("SELECT * FROM vendas WHERE id = @p0", vendaId).Rows[0];
        var n = Nova(Saida);
        n.VendaId = vendaId;
        PreencherParticipante(n, v["cliente_id"] as long?);
        n.Desconto = Ui.Dec(v["desconto"]);
        n.Frete = Ui.Dec(v["frete"]);
        n.Info = $"Referente à venda nº {vendaId}.";
        var cfop = CfopPadrao(Saida, n.PartUf);
        foreach (DataRow i in Db.Query("SELECT produto_id, quantidade, preco_unitario FROM venda_itens WHERE venda_id = @p0 ORDER BY id", vendaId).Rows)
            n.Itens.Add(ItemDeProduto((long)i["produto_id"], Ui.Dec(i["quantidade"]), Ui.Dec(i["preco_unitario"]), cfop));
        return n;
    }

    public static NotaFiscal DeCompra(long compraId)
    {
        var c = Db.Query("SELECT * FROM compras WHERE id = @p0", compraId).Rows[0];
        var n = Nova(Entrada);
        n.CompraId = compraId;
        PreencherParticipante(n, (long)c["fornecedor_id"]);
        n.Info = $"Referente à compra nº {compraId}" + (c["numero_nota"] is string nf ? $" (NF do fornecedor {nf})." : ".");
        var cfop = CfopPadrao(Entrada, n.PartUf);
        foreach (DataRow i in Db.Query("SELECT produto_id, quantidade, custo_unitario FROM compra_itens WHERE compra_id = @p0 ORDER BY id", compraId).Rows)
            n.Itens.Add(ItemDeProduto((long)i["produto_id"], Ui.Dec(i["quantidade"]), Math.Round(Ui.Dec(i["custo_unitario"]), 4), cfop));
        return n;
    }

    public static NotaFiscal Carregar(long id)
    {
        var r = Db.Query("SELECT * FROM notas_fiscais WHERE id = @p0", id).Rows[0];
        var n = new NotaFiscal
        {
            Id = id,
            Tipo = Ui.Txt(r["tipo"]),
            Modelo = Ui.Txt(r["modelo"]),
            Serie = (int)Ui.Dec(r["serie"]),
            Numero = r["numero"] as long?,
            Chave = r["chave"] as string,
            DataEmissao = Db.LerData(r["data_emissao"]),
            Natureza = Ui.Txt(r["natureza_operacao"]),
            ClienteId = r["cliente_id"] as long?,
            FornecedorId = r["fornecedor_id"] as long?,
            VendaId = r["venda_id"] as long?,
            CompraId = r["compra_id"] as long?,
            PartNome = Ui.Txt(r["part_nome"]),
            PartDocumento = Ui.Txt(r["part_documento"]),
            PartIe = Ui.Txt(r["part_ie"]),
            PartEndereco = Ui.Txt(r["part_endereco"]),
            PartCidade = Ui.Txt(r["part_cidade"]),
            PartUf = Ui.Txt(r["part_uf"]),
            Desconto = Ui.Dec(r["desconto"]),
            Frete = Ui.Dec(r["frete"]),
            Info = Ui.Txt(r["info_complementar"]),
            Status = Ui.Txt(r["status"]),
        };
        foreach (DataRow i in Db.Query("SELECT * FROM nota_itens WHERE nota_id = @p0 ORDER BY id", id).Rows)
            n.Itens.Add(new NotaItem
            {
                ProdutoId = i["produto_id"] as long?,
                Codigo = Ui.Txt(i["codigo"]),
                Descricao = Ui.Txt(i["descricao"]),
                Ncm = Ui.Txt(i["ncm"]),
                Cfop = Ui.Txt(i["cfop"]),
                Cst = Ui.Txt(i["cst"]),
                Unidade = Ui.Txt(i["unidade"]),
                Quantidade = Ui.Dec(i["quantidade"]),
                ValorUnitario = Ui.Dec(i["valor_unitario"]),
                Desconto = Ui.Dec(i["desconto"]),
                AliqIcms = Ui.Dec(i["aliq_icms"]),
                AliqIpi = Ui.Dec(i["aliq_ipi"]),
                AliqPis = Ui.Dec(i["aliq_pis"]),
                AliqCofins = Ui.Dec(i["aliq_cofins"]),
            });
        return n;
    }

    public static long Salvar(NotaFiscal n)
    {
        using var s = new DbSessao(transacao: true);
        object?[] cab =
        [
            n.Tipo, n.Modelo, n.Serie, n.DataEmissao, n.Natureza, n.ClienteId, n.FornecedorId, n.VendaId, n.CompraId,
            n.PartNome, n.PartDocumento, n.PartIe, n.PartEndereco, n.PartCidade, n.PartUf,
            n.ValorProdutos, n.DescontoTotal, n.Frete, n.ValorIcms, n.ValorIpi, n.ValorPis, n.ValorCofins, n.ValorTotal, n.Info,
        ];
        if (n.Id is long id)
        {
            if (Convert.ToString(s.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", id)) != "DIGITADA")
                throw new InvalidOperationException("Somente notas em digitação podem ser alteradas.");
            s.Exec("""
                UPDATE notas_fiscais SET tipo = @p0, modelo = @p1, serie = @p2, data_emissao = @p3, natureza_operacao = @p4,
                    cliente_id = @p5, fornecedor_id = @p6, venda_id = @p7, compra_id = @p8, part_nome = @p9,
                    part_documento = @p10, part_ie = @p11, part_endereco = @p12, part_cidade = @p13, part_uf = @p14,
                    valor_produtos = @p15, desconto = @p16, frete = @p17, valor_icms = @p18, valor_ipi = @p19,
                    valor_pis = @p20, valor_cofins = @p21, valor_total = @p22, info_complementar = @p23
                WHERE id = @p24
                """, [.. cab, id]);
            s.Exec("DELETE FROM nota_itens WHERE nota_id = @p0", id);
        }
        else
        {
            id = s.Inserir("""
                INSERT INTO notas_fiscais(tipo, modelo, serie, data_emissao, natureza_operacao, cliente_id, fornecedor_id,
                    venda_id, compra_id, part_nome, part_documento, part_ie, part_endereco, part_cidade, part_uf,
                    valor_produtos, desconto, frete, valor_icms, valor_ipi, valor_pis, valor_cofins, valor_total,
                    info_complementar, usuario_id)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16,
                       @p17, @p18, @p19, @p20, @p21, @p22, @p23, @p24)
                """, [.. cab, Sessao.UsuarioId]);
            n.Id = id;
        }
        InserirItens(s, id, n.Itens);
        s.Commit();
        return id;
    }

    internal static void InserirItens(DbSessao s, long notaId, IEnumerable<NotaItem> itens)
    {
        foreach (var i in itens)
            s.Exec("""
                INSERT INTO nota_itens(nota_id, produto_id, codigo, descricao, ncm, cfop, cst, unidade, quantidade,
                    valor_unitario, desconto, valor_total, aliq_icms, valor_icms, aliq_ipi, valor_ipi, aliq_pis, valor_pis,
                    aliq_cofins, valor_cofins)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19)
                """, notaId, i.ProdutoId, i.Codigo, i.Descricao, i.Ncm, i.Cfop, i.Cst, i.Unidade, i.Quantidade,
                i.ValorUnitario, i.Desconto, i.ValorTotal, i.AliqIcms, i.ValorIcms, i.AliqIpi, i.ValorIpi, i.AliqPis,
                i.ValorPis, i.AliqCofins, i.ValorCofins);
    }

    /// <summary>Lista do que falta para a nota poder ser emitida (vazia = ok).</summary>
    public static List<string> Validar(NotaFiscal n)
    {
        var erros = new List<string>();
        if (n.Itens.Count == 0) erros.Add("A nota não tem itens.");
        if (string.IsNullOrWhiteSpace(n.Natureza)) erros.Add("Informe a natureza da operação.");
        if (n.Modelo == "55" && string.IsNullOrWhiteSpace(n.PartNome)) erros.Add("Informe o destinatário/remetente.");
        foreach (var i in n.Itens)
        {
            if (SoDigitos(i.Ncm).Length != 8) erros.Add($"Item \"{i.Descricao}\": NCM deve ter 8 dígitos.");
            if (SoDigitos(i.Cfop).Length != 4) erros.Add($"Item \"{i.Descricao}\": CFOP deve ter 4 dígitos.");
            if (string.IsNullOrWhiteSpace(i.Cst)) erros.Add($"Item \"{i.Descricao}\": informe o CST/CSOSN.");
            if (i.Quantidade <= 0) erros.Add($"Item \"{i.Descricao}\": quantidade inválida.");
        }
        return erros;
    }

    /// <summary>Atribui número e chave de acesso e muda para EMITIDA.</summary>
    public static void Emitir(long id)
    {
        var n = Carregar(id);
        var erros = Validar(n);
        if (erros.Count > 0)
            throw new InvalidOperationException(string.Join("\n", erros.Take(10)));

        using var s = new DbSessao(transacao: true);
        if (Convert.ToString(s.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", id)) != "DIGITADA")
            throw new InvalidOperationException("Esta nota já foi emitida ou cancelada.");

        var chaveNumero = "proximo_numero_" + n.Modelo;
        long numero = long.TryParse(Convert.ToString(s.Scalar("SELECT valor FROM configuracoes WHERE chave = @p0", chaveNumero)), out var proximo) ? proximo : 1;
        while (s.Scalar("SELECT 1 FROM notas_fiscais WHERE propria = 1 AND modelo = @p0 AND serie = @p1 AND numero = @p2", n.Modelo, n.Serie, numero) != null)
            numero++;

        var agora = DateTime.Now;
        var chave = ChaveAcesso(Db.Config("uf"), agora, Db.Config("cnpj"), n.Modelo, n.Serie, numero);
        s.Exec("UPDATE notas_fiscais SET numero = @p0, chave = @p1, data_emissao = @p2, status = 'EMITIDA' WHERE id = @p3",
            numero, chave, agora, id);
        s.Exec("INSERT INTO configuracoes(chave, valor) VALUES(@p0, @p1) ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor",
            chaveNumero, (numero + 1).ToString());
        s.Commit();
    }

    public static void Cancelar(long id) =>
        Db.Exec("UPDATE notas_fiscais SET status = 'CANCELADA' WHERE id = @p0", id);

    /// <summary>Chave de 44 dígitos (layout NF-e). Null se a UF/CNPJ da empresa não estiverem configurados.</summary>
    public static string? ChaveAcesso(string uf, DateTime data, string cnpj, string modelo, int serie, long numero)
    {
        var cnpjDigitos = SoDigitos(cnpj);
        if (!CodigosUf.TryGetValue(uf.ToUpper(), out var cUf) || cnpjDigitos.Length != 14)
            return null;
        var codigoNumerico = RandomNumberGenerator.GetInt32(10_000_000, 99_999_999).ToString();
        var semDv = $"{cUf}{data:yyMM}{cnpjDigitos}{modelo.PadLeft(2, '0')}{serie:000}{numero:000000000}1{codigoNumerico}";
        return semDv + DigitoMod11(semDv);
    }

    public static int DigitoMod11(string numero)
    {
        int soma = 0, peso = 2;
        for (int i = numero.Length - 1; i >= 0; i--)
        {
            soma += (numero[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public static string FormatarChave(string? chave) =>
        string.IsNullOrEmpty(chave) ? "" : string.Join(" ", chave.Chunk(4).Select(c => new string(c)));

    /// <summary>DANFE em texto (para visualizar/imprimir).</summary>
    public static string Danfe(long id)
    {
        const int L = 100;
        var r = Db.Query("SELECT * FROM notas_fiscais WHERE id = @p0", id).Rows[0];
        var itens = Db.Query("SELECT * FROM nota_itens WHERE nota_id = @p0 ORDER BY id", id);
        var propria = Ui.Dec(r["propria"]) == 1;
        var entrada = Ui.Txt(r["tipo"]) == Entrada;
        var status = Ui.Txt(r["status"]);

        var sb = new StringBuilder();
        var duplo = new string('=', L);
        var traco = new string('-', L);
        void Centro(string t) => sb.AppendLine(t.Length >= L ? t : new string(' ', (L - t.Length) / 2) + t);
        void Secao(string t) => sb.AppendLine($"-- {t} " + new string('-', Math.Max(0, L - t.Length - 4)));
        static string Col(string t, int n, bool direita = false) =>
            t.Length > n ? t[..n] : direita ? t.PadLeft(n) : t.PadRight(n);
        string N2(string coluna) => Ui.Dec(r[coluna]).ToString("N2");

        sb.AppendLine(duplo);
        Centro(Ui.Txt(r["modelo"]) == "65" ? "DANFE NFC-e - DOCUMENTO AUXILIAR DA NOTA FISCAL DE CONSUMIDOR ELETRÔNICA"
                                           : "DANFE - DOCUMENTO AUXILIAR DA NOTA FISCAL ELETRÔNICA");
        sb.AppendLine(duplo);

        // junta só as partes preenchidas
        static string Juntar(string separador, params string?[] partes) =>
            string.Join(separador, partes.Where(x => !string.IsNullOrWhiteSpace(x)));
        static string Rotulado(string rotulo, string? valor) => string.IsNullOrWhiteSpace(valor) ? "" : rotulo + valor;
        void Linha(string texto)
        {
            if (texto.Length > 0) sb.AppendLine(texto);
        }

        if (propria)
        {
            sb.AppendLine("EMITENTE: " + Db.Config("nome_loja"));
            Linha(Juntar("   ", Rotulado("CNPJ: ", Db.Config("cnpj")), Rotulado("IE: ", Db.Config("ie")), Db.Config("regime")));
            Linha(Juntar("   ", Juntar(" - ", Db.Config("endereco"), Juntar("/", Db.Config("cidade"), Db.Config("uf"))),
                Rotulado("Fone: ", Db.Config("telefone"))));
        }
        else
        {
            sb.AppendLine("EMITENTE: " + Ui.Txt(r["part_nome"]));
            Linha(Juntar("   ", Rotulado("CNPJ: ", Ui.Txt(r["part_documento"])), Rotulado("IE: ", Ui.Txt(r["part_ie"]))));
            Linha(Juntar(" - ", Ui.Txt(r["part_endereco"]), Juntar("/", Ui.Txt(r["part_cidade"]), Ui.Txt(r["part_uf"]))));
        }
        sb.AppendLine(traco);
        var numero = r["numero"] is long num ? num.ToString("000\\.000\\.000") : "(não numerada)";
        sb.AppendLine($"{(entrada ? "0 - ENTRADA" : "1 - SAÍDA")}    Nº {numero}    SÉRIE {r["serie"]}    " +
                      $"EMISSÃO {Db.LerData(r["data_emissao"]):dd/MM/yyyy HH:mm}    SITUAÇÃO: {status}");
        sb.AppendLine("NATUREZA DA OPERAÇÃO: " + Ui.Txt(r["natureza_operacao"]));
        if (r["chave"] is string chave)
            sb.AppendLine("CHAVE DE ACESSO: " + FormatarChave(chave));
        if (propria)
            Centro("*** SEM VALOR FISCAL - NOTA NÃO TRANSMITIDA/AUTORIZADA PELA SEFAZ ***");
        if (status == "CANCELADA")
            Centro("*** NOTA CANCELADA ***");

        if (propria)
        {
            Secao(entrada ? "REMETENTE" : "DESTINATÁRIO");
            sb.AppendLine($"Nome: {r["part_nome"]}");
            Linha(Juntar("   ", Rotulado("CPF/CNPJ: ", Ui.Txt(r["part_documento"])), Rotulado("IE: ", Ui.Txt(r["part_ie"]))));
            Linha(Rotulado("Endereço: ", Juntar(" - ", Ui.Txt(r["part_endereco"]), Juntar("/", Ui.Txt(r["part_cidade"]), Ui.Txt(r["part_uf"])))));
        }
        else
        {
            Secao("DESTINATÁRIO");
            sb.AppendLine(Juntar("   ", Db.Config("nome_loja"), Rotulado("CNPJ: ", Db.Config("cnpj"))));
        }

        Secao("CÁLCULO DO IMPOSTO");
        sb.AppendLine(Col("Valor ICMS", 14) + Col("Valor IPI", 14) + Col("PIS", 12) + Col("COFINS", 12) + Col("Frete", 12) +
                      Col("Desconto", 12) + Col("Produtos", 12) + Col("VALOR TOTAL", 12, true));
        sb.AppendLine(Col(N2("valor_icms"), 14) + Col(N2("valor_ipi"), 14) + Col(N2("valor_pis"), 12) + Col(N2("valor_cofins"), 12) +
                      Col(N2("frete"), 12) + Col(N2("desconto"), 12) + Col(N2("valor_produtos"), 12) + Col(N2("valor_total"), 12, true));

        Secao("DADOS DOS PRODUTOS / SERVIÇOS");
        sb.AppendLine(Col("Código", 10) + Col("Descrição", 27) + Col("NCM", 9) + Col("CST", 5) + Col("CFOP", 5) + Col("Un", 4) +
                      Col("Qtd.", 9, true) + Col("V.Unit", 10, true) + Col("V.Total", 11, true) + Col("ICMS", 10, true));
        foreach (DataRow i in itens.Rows)
            sb.AppendLine(Col(Ui.Txt(i["codigo"]), 10) + Col(Ui.Txt(i["descricao"]), 27) + Col(Ui.Txt(i["ncm"]), 9) +
                          Col(Ui.Txt(i["cst"]), 5) + Col(Ui.Txt(i["cfop"]), 5) + Col(Ui.Txt(i["unidade"]), 4) +
                          Col(Ui.Dec(i["quantidade"]).ToString("N2"), 9, true) + Col(Ui.Dec(i["valor_unitario"]).ToString("N2"), 10, true) +
                          Col(Ui.Dec(i["valor_total"]).ToString("N2"), 11, true) + Col(Ui.Dec(i["valor_icms"]).ToString("N2"), 10, true));

        Secao("DADOS ADICIONAIS");
        var info = Ui.Txt(r["info_complementar"]);
        if (propria && Db.Config("regime").StartsWith('1'))
            info = ("Documento emitido por ME ou EPP optante pelo Simples Nacional. " + info).Trim();
        foreach (var linha in Quebrar(info, L))
            sb.AppendLine(linha);
        sb.AppendLine(duplo);
        return sb.ToString();
    }

    static IEnumerable<string> Quebrar(string texto, int largura)
    {
        var linha = new StringBuilder();
        foreach (var palavra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (linha.Length + palavra.Length + 1 > largura)
            {
                yield return linha.ToString();
                linha.Clear();
            }
            if (linha.Length > 0) linha.Append(' ');
            linha.Append(palavra);
        }
        if (linha.Length > 0)
            yield return linha.ToString();
    }
}
