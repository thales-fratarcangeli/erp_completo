using System.Text;

namespace ErpLoja.Data;

public class PedidoItem
{
    [Browsable(false)] public long ProdutoId { get; set; }
    [DisplayName("Código")] public string Codigo { get; set; } = "";
    [DisplayName("Produto")] public string Produto { get; set; } = "";
    [DisplayName("Un.")] public string Unidade { get; set; } = "";
    [DisplayName("Qtd.")] public decimal Quantidade { get; set; }
    [DisplayName("Preço unit.")] public decimal Unitario { get; set; }
    [DisplayName("Desconto R$")] public decimal Desconto { get; set; }
    [DisplayName("Total")] public decimal Total => Math.Round(Quantidade * Unitario - Desconto, 2);
}

public class Pedido
{
    public long? Id { get; set; }
    public string Tipo { get; set; } = Pedidos.Venda;
    public DateTime Data { get; set; } = DateTime.Now;
    public long? ClienteId { get; set; }
    public long? FornecedorId { get; set; }
    public DateTime? Validade { get; set; }
    public decimal Desconto { get; set; }
    public decimal Frete { get; set; }
    public string Forma { get; set; } = "";
    public int Parcelas { get; set; } = 1;
    public string Observacoes { get; set; } = "";
    public string Status { get; set; } = "ABERTO";
    public long? OrigemId { get; set; }
    public BindingList<PedidoItem> Itens { get; } = new();

    public decimal Subtotal => Itens.Sum(i => Math.Round(i.Quantidade * i.Unitario, 2));
    public decimal DescontoItens => Itens.Sum(i => i.Desconto);
    public decimal Total => Math.Max(0, Subtotal - DescontoItens - Desconto + Frete);
}

/// <summary>Orçamentos, pedidos de venda e pedidos de compra.</summary>
public static class Pedidos
{
    public const string Orcamento = "ORCAMENTO", Venda = "VENDA", Compra = "COMPRA";

    public static string Nome(string tipo) => tipo switch
    {
        Orcamento => "Orçamento",
        Venda => "Pedido de venda",
        _ => "Pedido de compra",
    };

    public static Pedido Carregar(long id)
    {
        var r = Db.Query("SELECT * FROM pedidos WHERE id = @p0", id).Rows[0];
        var p = new Pedido
        {
            Id = id,
            Tipo = Ui.Txt(r["tipo"]),
            Data = Db.LerData(r["data"]),
            ClienteId = r["cliente_id"] is long c ? c : null,
            FornecedorId = r["fornecedor_id"] is long f ? f : null,
            Validade = r["validade"] is DBNull ? null : Db.LerData(r["validade"]),
            Desconto = Ui.Dec(r["desconto"]),
            Frete = Ui.Dec(r["frete"]),
            Forma = Ui.Txt(r["forma_pagamento"]),
            Parcelas = (int)Ui.Dec(r["parcelas"]),
            Observacoes = Ui.Txt(r["observacoes"]),
            Status = Ui.Txt(r["status"]),
            OrigemId = r["origem_id"] is long o ? o : null,
        };
        foreach (DataRow i in Db.Query("""
                     SELECT i.*, p.codigo, p.nome, p.unidade FROM pedido_itens i JOIN produtos p ON p.id = i.produto_id
                     WHERE i.pedido_id = @p0 ORDER BY i.id
                     """, id).Rows)
            p.Itens.Add(new PedidoItem
            {
                ProdutoId = (long)i["produto_id"],
                Codigo = Ui.Txt(i["codigo"]),
                Produto = Ui.Txt(i["nome"]),
                Unidade = Ui.Txt(i["unidade"]),
                Quantidade = Ui.Dec(i["quantidade"]),
                Unitario = Ui.Dec(i["preco_unitario"]),
                Desconto = Ui.Dec(i["desconto"]),
            });
        return p;
    }

    public static long Salvar(Pedido p)
    {
        using var s = new DbSessao(transacao: true);
        var id = Salvar(s, p);
        s.Commit();
        return id;
    }

    static long Salvar(DbSessao s, Pedido p)
    {
        object?[] valores =
        [
            p.Tipo, p.Data, p.ClienteId, p.FornecedorId, p.Validade is DateTime v ? Db.Dia(v) : null,
            p.Subtotal, p.Desconto, p.Frete, p.Total, p.Forma, p.Parcelas, p.Observacoes, p.OrigemId,
        ];
        if (p.Id is long id)
        {
            if (Convert.ToString(s.Scalar("SELECT status FROM pedidos WHERE id = @p0", id)) != "ABERTO")
                throw new InvalidOperationException("Somente documentos em aberto podem ser alterados.");
            s.Exec("""
                UPDATE pedidos SET tipo = @p0, data = @p1, cliente_id = @p2, fornecedor_id = @p3, validade = @p4,
                       subtotal = @p5, desconto = @p6, frete = @p7, total = @p8, forma_pagamento = @p9, parcelas = @p10,
                       observacoes = @p11, origem_id = @p12
                WHERE id = @p13
                """, [.. valores, id]);
            s.Exec("DELETE FROM pedido_itens WHERE pedido_id = @p0", id);
        }
        else
        {
            id = s.Inserir("""
                INSERT INTO pedidos(tipo, data, cliente_id, fornecedor_id, validade, subtotal, desconto, frete, total,
                                    forma_pagamento, parcelas, observacoes, origem_id, usuario_id)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13)
                """, [.. valores, Sessao.UsuarioId]);
            p.Id = id;
        }

        foreach (var i in p.Itens)
            s.Exec("""
                INSERT INTO pedido_itens(pedido_id, produto_id, quantidade, preco_unitario, desconto, total)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5)
                """, id, i.ProdutoId, i.Quantidade, i.Unitario, i.Desconto, i.Total);
        return id;
    }

    static void ExigirStatus(DbSessao s, long id, string esperado)
    {
        if (Convert.ToString(s.Scalar("SELECT status FROM pedidos WHERE id = @p0", id)) != esperado)
            throw new InvalidOperationException($"Operação permitida apenas para documentos com situação {esperado}.");
    }

    /// <summary>Orçamento aprovado vira pedido de venda. Devolve o id do pedido.</summary>
    public static long AprovarOrcamento(long id)
    {
        var orc = Carregar(id);
        using var s = new DbSessao(transacao: true);
        ExigirStatus(s, id, "ABERTO");
        orc.Id = null;
        orc.Tipo = Venda;
        orc.Data = DateTime.Now;
        orc.Validade = null;
        orc.OrigemId = id;
        var pedido = Salvar(s, orc);
        s.Exec("UPDATE pedidos SET status = 'APROVADO' WHERE id = @p0", id);
        s.Commit();
        return pedido;
    }

    public static long Duplicar(long id)
    {
        var p = Carregar(id);
        p.Id = null;
        p.Data = DateTime.Now;
        p.Status = "ABERTO";
        p.OrigemId = null;
        if (p.Tipo == Orcamento)
            p.Validade = DateTime.Today.AddDays(Parametros.ValidadeOrcamentoDias);
        return Salvar(p);
    }

    /// <summary>Pedido de venda vira venda: baixa estoque e gera contas a receber.</summary>
    public static long Faturar(long id, string forma, int parcelas, DateTime vencimento)
    {
        var p = Carregar(id);
        using var s = new DbSessao(transacao: true);
        ExigirStatus(s, id, "ABERTO");
        var itens = p.Itens.Select(i => new ItemDoc(i.ProdutoId, i.Quantidade, i.Unitario,
            Convert.ToDecimal(s.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", i.ProdutoId) ?? 0))).ToList();
        var venda = Operacoes.RegistrarVenda(s, p.ClienteId, itens, p.DescontoItens + p.Desconto, p.Frete,
            forma, parcelas, vencimento, $"Pedido nº {id}");
        s.Exec("UPDATE pedidos SET status = 'FATURADO', venda_id = @p0, forma_pagamento = @p1, parcelas = @p2 WHERE id = @p3",
            venda, forma, parcelas, id);
        s.Commit();
        return venda;
    }

    /// <summary>Pedido de compra recebido: dá entrada no estoque e gera contas a pagar. Frete e descontos entram no custo.</summary>
    public static long Receber(long id, string forma, int parcelas, DateTime vencimento, string? notaFiscal)
    {
        var p = Carregar(id);
        if (p.FornecedorId is not long fornecedor)
            throw new InvalidOperationException("Pedido sem fornecedor.");
        var somaItens = p.Itens.Sum(i => i.Total);
        var fator = somaItens > 0 ? p.Total / somaItens : 1;

        using var s = new DbSessao(transacao: true);
        ExigirStatus(s, id, "ABERTO");
        var itens = p.Itens.Select(i => new ItemDoc(i.ProdutoId, i.Quantidade, i.Total * fator / i.Quantidade, 0)).ToList();
        var compra = Operacoes.RegistrarCompra(s, fornecedor, notaFiscal, itens, forma, parcelas, vencimento);
        s.Exec("UPDATE pedidos SET status = 'RECEBIDO', compra_id = @p0, forma_pagamento = @p1, parcelas = @p2 WHERE id = @p3",
            compra, forma, parcelas, id);
        s.Commit();
        return compra;
    }

    public static void MudarStatus(long id, string de, string para)
    {
        using var s = new DbSessao(transacao: true);
        ExigirStatus(s, id, de);
        s.Exec("UPDATE pedidos SET status = @p0 WHERE id = @p1", para, id);
        s.Commit();
    }

    /// <summary>Texto para impressão (orçamento / pedido).</summary>
    public static string Texto(long id)
    {
        const int L = 80;
        var p = Carregar(id);
        var pessoa = p.Tipo == Compra
            ? Db.Query("SELECT nome, cnpj AS doc, telefone, email, endereco, cidade FROM fornecedores WHERE id = @p0", p.FornecedorId)
            : Db.Query("SELECT nome, cpf_cnpj AS doc, telefone, email, endereco, cidade FROM clientes WHERE id = @p0", p.ClienteId);

        var sb = new StringBuilder();
        var traco = new string('-', L);
        void Centro(string t) => sb.AppendLine(t.Length >= L ? t : new string(' ', (L - t.Length) / 2) + t);
        void Lados(string a, string b) => sb.AppendLine(a + new string(' ', Math.Max(1, L - a.Length - b.Length)) + b);
        static string Col(string t, int n, bool direita = false) =>
            t.Length > n ? t[..n] : direita ? t.PadLeft(n) : t.PadRight(n);

        Centro(Db.Config("nome_loja").ToUpper());
        var linhaEmpresa = string.Join(" · ", new[] { "cnpj", "telefone", "endereco" }.Select(Db.Config).Where(x => x.Length > 0));
        if (linhaEmpresa.Length > 0)
            Centro(linhaEmpresa);
        sb.AppendLine(traco);
        Lados($"{Nome(p.Tipo).ToUpper()} Nº {id}", $"Data: {p.Data:dd/MM/yyyy}");
        if (p.Tipo == Orcamento && p.Validade is DateTime v)
            sb.AppendLine($"Válido até: {v:dd/MM/yyyy}");
        if (p.Status != "ABERTO")
            sb.AppendLine($"Situação: {p.Status}");
        sb.AppendLine(traco);
        if (pessoa.Rows.Count > 0)
        {
            var r = pessoa.Rows[0];
            sb.AppendLine((p.Tipo == Compra ? "Fornecedor: " : "Cliente: ") + r["nome"]);
            var contato = string.Join(" · ", new[] { "doc", "telefone", "email" }.Select(c => Ui.Txt(r[c])).Where(x => x.Length > 0));
            if (contato.Length > 0) sb.AppendLine(contato);
            var endereco = string.Join(" - ", new[] { "endereco", "cidade" }.Select(c => Ui.Txt(r[c])).Where(x => x.Length > 0));
            if (endereco.Length > 0) sb.AppendLine(endereco);
        }
        else
            sb.AppendLine("Cliente: Consumidor final");
        sb.AppendLine(traco);
        sb.AppendLine(Col("Código", 10) + Col("Produto", 30) + Col("Qtd.", 9, true) + Col("Preço", 10, true) + Col("Desc.", 9, true) + Col("Total", 12, true));
        foreach (var i in p.Itens)
            sb.AppendLine(Col(i.Codigo, 10) + Col(i.Produto, 30) + Col(i.Quantidade.ToString("N2"), 9, true) +
                          Col(i.Unitario.ToString("N2"), 10, true) + Col(i.Desconto.ToString("N2"), 9, true) + Col(i.Total.ToString("N2"), 12, true));
        sb.AppendLine(traco);
        Lados("Subtotal", p.Subtotal.ToString("N2"));
        if (p.DescontoItens + p.Desconto > 0) Lados("Descontos", "-" + (p.DescontoItens + p.Desconto).ToString("N2"));
        if (p.Frete > 0) Lados("Frete", p.Frete.ToString("N2"));
        Lados("TOTAL R$", p.Total.ToString("N2"));
        if (p.Forma.Length > 0)
            Lados("Condição de pagamento", p.Forma + (p.Forma == Operacoes.APrazo ? $" em {p.Parcelas}x" : ""));
        if (p.Observacoes.Length > 0)
        {
            sb.AppendLine(traco);
            sb.AppendLine("Observações: " + p.Observacoes);
        }
        sb.AppendLine(traco);
        if (p.Tipo == Orcamento)
            Centro("Orçamento sujeito à disponibilidade de estoque.");
        sb.AppendLine();
        sb.AppendLine();
        Centro("______________________________________");
        Centro("Assinatura");
        return sb.ToString();
    }
}
