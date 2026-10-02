using System.Globalization;
using System.Xml.Linq;

namespace ErpLoja.Data;

public class XmlItem
{
    public string CodigoFornecedor { get; set; } = "";
    public string Ean { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Ncm { get; set; } = "";
    public string Cfop { get; set; } = "";
    public string Cst { get; set; } = "";
    public string Unidade { get; set; } = "";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal Desconto { get; set; }
    public decimal AliqIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal AliqIpi { get; set; }
    public decimal ValorIpi { get; set; }
    /// <summary>Custo efetivo do item: produtos - desconto + frete + seguro + outras + IPI + ICMS-ST.</summary>
    public decimal CustoTotal { get; set; }

    // vínculo com o cadastro
    public long? ProdutoId { get; set; }
    public decimal Fator { get; set; } = 1;
}

public class NotaXml
{
    public string Chave { get; set; } = "";
    public long Numero { get; set; }
    public int Serie { get; set; }
    public string Modelo { get; set; } = "55";
    public DateTime DataEmissao { get; set; }
    public string Natureza { get; set; } = "";
    public string EmitCnpj { get; set; } = "";
    public string EmitNome { get; set; } = "";
    public string EmitIe { get; set; } = "";
    public string EmitEndereco { get; set; } = "";
    public string EmitCidade { get; set; } = "";
    public string EmitUf { get; set; } = "";
    public string EmitFone { get; set; } = "";
    public decimal ValorProdutos { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorTotal { get; set; }
    public List<XmlItem> Itens { get; } = new();
    public List<(DateTime Vencimento, decimal Valor)> Duplicatas { get; } = new();
    public string Xml { get; set; } = "";
    public long? FornecedorId { get; set; }
}

/// <summary>Lê o XML (procNFe/NFe) recebido do fornecedor e lança a nota de entrada + compra.</summary>
public static class ImportacaoXml
{
    static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    static string T(XElement? e, string nome) => e?.Element(Ns + nome)?.Value.Trim() ?? "";

    static decimal D(XElement? e, string nome) =>
        decimal.TryParse(T(e, nome), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0;

    public static bool EanValido(string ean) =>
        ean.Length is 8 or 12 or 13 or 14 && ean.All(char.IsDigit);

    public static NotaXml Ler(string caminho)
    {
        var texto = File.ReadAllText(caminho);
        XDocument doc;
        try
        {
            doc = XDocument.Parse(texto);
        }
        catch (System.Xml.XmlException)
        {
            throw new InvalidDataException("O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). " +
                                           "Baixe novamente o XML enviado pelo fornecedor e tente de novo.");
        }
        var inf = doc.Descendants(Ns + "infNFe").FirstOrDefault()
                  ?? throw new InvalidDataException("O arquivo não é um XML de NF-e (infNFe não encontrado). Confira se escolheu o XML da nota, e não outro arquivo.");
        var ide = inf.Element(Ns + "ide");
        var emit = inf.Element(Ns + "emit");
        var ender = emit?.Element(Ns + "enderEmit");
        var tot = inf.Element(Ns + "total")?.Element(Ns + "ICMSTot");

        var n = new NotaXml
        {
            Xml = texto,
            Chave = ((string?)inf.Attribute("Id") ?? "").Replace("NFe", ""),
            Numero = long.TryParse(T(ide, "nNF"), out var num) ? num : 0,
            Serie = int.TryParse(T(ide, "serie"), out var serie) ? serie : 0,
            Modelo = T(ide, "mod") is { Length: > 0 } mod ? mod : "55",
            Natureza = T(ide, "natOp"),
            EmitCnpj = T(emit, "CNPJ") is { Length: > 0 } cnpj ? cnpj : T(emit, "CPF"),
            EmitNome = T(emit, "xNome"),
            EmitIe = T(emit, "IE"),
            EmitEndereco = $"{T(ender, "xLgr")}, {T(ender, "nro")} - {T(ender, "xBairro")}".Trim(' ', ',', '-'),
            EmitCidade = T(ender, "xMun"),
            EmitUf = T(ender, "UF"),
            EmitFone = T(ender, "fone"),
            ValorProdutos = D(tot, "vProd"),
            Desconto = D(tot, "vDesc"),
            Frete = D(tot, "vFrete"),
            ValorIcms = D(tot, "vICMS"),
            ValorIpi = D(tot, "vIPI"),
            ValorPis = D(tot, "vPIS"),
            ValorCofins = D(tot, "vCOFINS"),
            ValorTotal = D(tot, "vNF"),
        };
        var dataTexto = T(ide, "dhEmi") is { Length: > 0 } dh ? dh : T(ide, "dEmi");
        n.DataEmissao = DateTimeOffset.TryParse(dataTexto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data.DateTime : DateTime.Now;
        if (n.Chave.Length != 44)
            n.Chave = doc.Descendants(Ns + "chNFe").FirstOrDefault()?.Value ?? n.Chave;

        foreach (var det in inf.Elements(Ns + "det"))
        {
            var prod = det.Element(Ns + "prod");
            var imposto = det.Element(Ns + "imposto");
            var icms = imposto?.Element(Ns + "ICMS")?.Elements().FirstOrDefault();
            var ipi = imposto?.Element(Ns + "IPI")?.Element(Ns + "IPITrib");
            var item = new XmlItem
            {
                CodigoFornecedor = T(prod, "cProd"),
                Ean = T(prod, "cEAN") is var ean && EanValido(ean) ? ean : (T(prod, "cEANTrib") is var eanTrib && EanValido(eanTrib) ? eanTrib : ""),
                Descricao = T(prod, "xProd"),
                Ncm = T(prod, "NCM"),
                Cfop = T(prod, "CFOP"),
                Unidade = T(prod, "uCom"),
                Quantidade = D(prod, "qCom"),
                ValorUnitario = D(prod, "vUnCom"),
                ValorProdutos = D(prod, "vProd"),
                Desconto = D(prod, "vDesc"),
                Cst = T(icms, "CSOSN") is { Length: > 0 } csosn ? csosn : T(icms, "orig") + T(icms, "CST"),
                AliqIcms = D(icms, "pICMS"),
                ValorIcms = D(icms, "vICMS"),
                AliqIpi = D(ipi, "pIPI"),
                ValorIpi = D(ipi, "vIPI"),
            };
            item.CustoTotal = item.ValorProdutos - item.Desconto + D(prod, "vFrete") + D(prod, "vSeg") + D(prod, "vOutro")
                              + item.ValorIpi + D(icms, "vICMSST");
            n.Itens.Add(item);
        }

        foreach (var dup in inf.Element(Ns + "cobr")?.Elements(Ns + "dup") ?? Enumerable.Empty<XElement>())
            if (DateTime.TryParse(T(dup, "dVenc"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var venc))
                n.Duplicatas.Add((venc, D(dup, "vDup")));

        if (n.Itens.Count == 0)
            throw new InvalidDataException("A nota não possui itens.");
        return n;
    }

    /// <summary>Localiza o fornecedor pelo CNPJ e sugere o produto de cada item (vínculo anterior ou código de barras).</summary>
    public static void SugerirVinculos(NotaXml n)
    {
        n.FornecedorId = Db.Scalar("""
            SELECT id FROM pessoas
            WHERE replace(replace(replace(replace(COALESCE(cpf_cnpj, ''), '.', ''), '/', ''), '-', ''), ' ', '') = @p0
            LIMIT 1
            """, n.EmitCnpj) as long?;

        foreach (var item in n.Itens)
        {
            if (n.FornecedorId is long forn &&
                Db.Query("""
                    SELECT v.produto_id, v.fator FROM produtos_fornecedor v JOIN produtos p ON p.id = v.produto_id
                    WHERE v.fornecedor_id = @p0 AND v.codigo_fornecedor = @p1 AND p.ativo = 1
                    """, forn, item.CodigoFornecedor).Rows is { Count: > 0 } vinculo)
            {
                item.ProdutoId = (long)vinculo[0]["produto_id"];
                item.Fator = Ui.Dec(vinculo[0]["fator"]);
                continue;
            }
            if (item.Ean.Length > 0)
                // só produtos ativos: a conferência só lista ativos (inativo sugerido quebrava a grade)
                item.ProdutoId = Db.Scalar("SELECT id FROM produtos WHERE ativo = 1 AND (codigo_barras = @p0 OR codigo = @p0) LIMIT 1", item.Ean) as long?;
        }
    }

    /// <summary>
    /// true se a chave já foi importada. Nota cuja compra foi cancelada (ex.: importada com vínculo/fator errado)
    /// não conta: pode ser importada de novo.
    /// </summary>
    public static bool JaImportada(string chave) =>
        chave.Length > 0 && Db.Scalar("""
            SELECT 1 FROM notas_fiscais n LEFT JOIN compras c ON c.id = n.compra_id
            WHERE n.chave = @p0 AND COALESCE(c.status, '') <> 'CANCELADA'
            """, chave) != null;

    public const string MensagemJaImportada =
        "Esta nota já foi importada. Para importá-la de novo (ex.: para corrigir vínculo ou fator), " +
        "cancele antes a compra gerada por ela em Compras > Compras realizadas.";

    /// <summary>
    /// Grava a nota de entrada. Cria fornecedor e produtos que não existirem, memoriza os vínculos
    /// e, se lancarCompra, dá entrada no estoque e gera as contas a pagar (usando as duplicatas da nota).
    /// Devolve (id da nota, quantidade de produtos cadastrados).
    /// </summary>
    public static (long NotaId, int ProdutosNovos) Importar(NotaXml n, bool lancarCompra, string forma)
    {
        if (JaImportada(n.Chave))
            throw new InvalidOperationException(MensagemJaImportada);

        using var s = new DbSessao(transacao: true);
        // importação anterior cuja compra foi cancelada: fica no histórico como cancelada e libera a chave
        if (n.Chave.Length > 0)
            s.Exec("""
                UPDATE notas_fiscais SET chave = NULL, status = 'CANCELADA',
                       info_complementar = TRIM(COALESCE(info_complementar, '') || ' Compra cancelada; nota reimportada (chave ' || @p0 || ').')
                WHERE chave = @p0
                """, n.Chave);
        var fornecedor = n.FornecedorId ?? s.Inserir("""
            INSERT INTO pessoas(nome, cpf_cnpj, ie, telefone, endereco, cidade, uf, eh_fornecedor)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, 1)
            """, n.EmitNome, n.EmitCnpj, n.EmitIe, n.EmitFone, n.EmitEndereco, n.EmitCidade, n.EmitUf);
        s.Exec("UPDATE pessoas SET eh_fornecedor = 1 WHERE id = @p0", fornecedor);

        int novos = 0;
        foreach (var item in n.Itens)
        {
            if (item.Fator <= 0)
                item.Fator = 1;
            if (item.ProdutoId is null)
            {
                var codigo = item.Ean.Length > 0 ? item.Ean : $"F{fornecedor}-{item.CodigoFornecedor}";
                for (int k = 2; s.Scalar("SELECT 1 FROM produtos WHERE codigo = @p0", codigo) != null; k++)
                    codigo = $"F{fornecedor}-{item.CodigoFornecedor}-{k}";
                item.ProdutoId = s.Inserir("""
                    INSERT INTO produtos(codigo, codigo_barras, nome, unidade, ncm, cst, aliq_icms, aliq_ipi)
                    VALUES(@p0, @p1, @p2, @p3, @p4, '102', 0, 0)
                    """, codigo, item.Ean.Length > 0 ? item.Ean : null, item.Descricao,
                    item.Fator == 1 && item.Unidade.Length > 0 ? item.Unidade.ToUpper() : "UN", item.Ncm);
                novos++;
            }
            s.Exec("""
                INSERT INTO produtos_fornecedor(fornecedor_id, codigo_fornecedor, produto_id, fator) VALUES(@p0, @p1, @p2, @p3)
                ON CONFLICT(fornecedor_id, codigo_fornecedor) DO UPDATE SET produto_id = excluded.produto_id, fator = excluded.fator
                """, fornecedor, item.CodigoFornecedor, item.ProdutoId, item.Fator);
        }

        long? compra = null;
        if (lancarCompra)
        {
            var itens = n.Itens.Select(i =>
            {
                var quantidade = i.Quantidade * i.Fator;
                return new ItemDoc(i.ProdutoId!.Value, quantidade, quantidade > 0 ? i.CustoTotal / quantidade : 0, 0);
            }).ToList();
            compra = Operacoes.RegistrarCompra(s, fornecedor, n.Numero.ToString(), itens, forma, 1,
                DateTime.Today.AddDays(Parametros.PrazoVencimentoDias), n.Duplicatas);
        }

        var notaId = s.Inserir("""
            INSERT INTO notas_fiscais(tipo, modelo, serie, numero, chave, data_emissao, natureza_operacao, propria,
                fornecedor_id, compra_id, part_nome, part_documento, part_ie, part_endereco, part_cidade, part_uf,
                valor_produtos, desconto, frete, valor_icms, valor_ipi, valor_pis, valor_cofins, valor_total,
                status, xml, usuario_id, info_complementar)
            VALUES('ENTRADA', @p0, @p1, @p2, @p3, @p4, @p5, 0, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13,
                   @p14, @p15, @p16, @p17, @p18, @p19, @p20, @p21, 'EMITIDA', @p22, @p23, @p24)
            """, n.Modelo, n.Serie, n.Numero, n.Chave.Length > 0 ? n.Chave : null, n.DataEmissao, n.Natureza, fornecedor, compra,
            n.EmitNome, n.EmitCnpj, n.EmitIe, n.EmitEndereco, n.EmitCidade, n.EmitUf,
            n.ValorProdutos, n.Desconto, n.Frete, n.ValorIcms, n.ValorIpi, n.ValorPis, n.ValorCofins, n.ValorTotal,
            n.Xml, Sessao.UsuarioId, compra is long c ? $"Importada do XML. Compra nº {c}." : "Importada do XML (sem lançamento de estoque).");

        foreach (var i in n.Itens)
            s.Exec("""
                INSERT INTO nota_itens(nota_id, produto_id, codigo, descricao, ncm, cfop, cst, unidade, quantidade,
                    valor_unitario, desconto, valor_total, aliq_icms, valor_icms, aliq_ipi, valor_ipi)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15)
                """, notaId, i.ProdutoId, i.CodigoFornecedor, i.Descricao, i.Ncm, i.Cfop, i.Cst, i.Unidade, i.Quantidade,
                i.ValorUnitario, i.Desconto, i.ValorProdutos - i.Desconto, i.AliqIcms, i.ValorIcms, i.AliqIpi, i.ValorIpi);

        s.Commit();
        return (notaId, novos);
    }
}
