namespace ErpLoja.Data;

public record ItemDoc(long ProdutoId, decimal Quantidade, decimal Unitario, decimal Custo)
{
    public decimal Total => Math.Round(Quantidade * Unitario, 2);
}

/// <summary>Regras de negócio que mexem em várias tabelas (sempre dentro de uma transação).</summary>
public static class Operacoes
{
    public const string APrazo = "A prazo";

    public static void MovimentarEstoque(DbSessao s, long produtoId, string tipo, decimal delta, string origem, long? referencia = null)
    {
        s.Exec("UPDATE produtos SET estoque = ROUND(estoque + @p0, 3) WHERE id = @p1", delta, produtoId);
        var saldo = s.Scalar("SELECT estoque FROM produtos WHERE id = @p0", produtoId);
        s.Exec("""
            INSERT INTO movimentacoes_estoque(data, produto_id, tipo, quantidade, saldo, origem, referencia_id, usuario_id)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)
            """, DateTime.Now, produtoId, tipo, Math.Abs(delta), saldo, origem, referencia, Sessao.UsuarioId);
    }

    /// <summary>À vista: gera a conta já paga. A prazo: gera as parcelas em aberto, uma por mês.</summary>
    static void GerarContas(DbSessao s, string tipo, string descricao, decimal total, string forma, bool aPrazo,
        int parcelas, DateTime primeiroVencimento, long? clienteId, long? fornecedorId, long? vendaId, long? compraId)
    {
        if (total <= 0)
            return;

        const string sql = """
            INSERT INTO contas(tipo, descricao, valor, vencimento, data_pagamento, valor_pago, forma_pagamento, status,
                               cliente_id, fornecedor_id, venda_id, compra_id)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)
            """;

        if (!aPrazo)
        {
            var hoje = Db.Dia(DateTime.Today);
            s.Exec(sql, tipo, descricao, total, hoje, hoje, total, forma, "PAGA", clienteId, fornecedorId, vendaId, compraId);
            return;
        }

        parcelas = Math.Max(1, parcelas);
        var valorParcela = Math.Round(total / parcelas, 2);
        for (int i = 0; i < parcelas; i++)
        {
            var valor = i == parcelas - 1 ? total - valorParcela * (parcelas - 1) : valorParcela;
            var desc = parcelas > 1 ? $"{descricao} - parcela {i + 1}/{parcelas}" : descricao;
            s.Exec(sql, tipo, desc, valor, Db.Dia(primeiroVencimento.AddMonths(i)), null, null, null, "ABERTA",
                clienteId, fornecedorId, vendaId, compraId);
        }
    }

    public static long RegistrarVenda(long? clienteId, IList<ItemDoc> itens, decimal desconto, string forma,
        int parcelas, DateTime vencimento)
    {
        using var s = new DbSessao(transacao: true);
        var id = RegistrarVenda(s, clienteId, itens, desconto, 0, forma, parcelas, vencimento, null);
        s.Commit();
        return id;
    }

    public static long RegistrarVenda(DbSessao s, long? clienteId, IList<ItemDoc> itens, decimal desconto, decimal frete,
        string forma, int parcelas, DateTime vencimento, string? observacoes)
    {
        var subtotal = itens.Sum(i => i.Total);
        var total = Math.Max(0, subtotal - desconto + frete);

        var id = s.Inserir("""
            INSERT INTO vendas(data, cliente_id, usuario_id, subtotal, desconto, frete, total, forma_pagamento, observacoes)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)
            """, DateTime.Now, clienteId, Sessao.UsuarioId, subtotal, desconto, frete, total, forma, observacoes);

        foreach (var i in itens)
        {
            s.Exec("""
                INSERT INTO venda_itens(venda_id, produto_id, quantidade, preco_unitario, custo_unitario, total)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5)
                """, id, i.ProdutoId, i.Quantidade, i.Unitario, i.Custo, i.Total);
            MovimentarEstoque(s, i.ProdutoId, "SAIDA", -i.Quantidade, $"Venda nº {id}", id);
        }

        GerarContas(s, "RECEBER", $"Venda nº {id}", total, forma, forma == APrazo, parcelas, vencimento,
            clienteId, null, id, null);
        return id;
    }

    public static long RegistrarCompra(long fornecedorId, string? nota, IList<ItemDoc> itens, string forma,
        int parcelas, DateTime vencimento)
    {
        using var s = new DbSessao(transacao: true);
        var id = RegistrarCompra(s, fornecedorId, nota, itens, forma, parcelas, vencimento);
        s.Commit();
        return id;
    }

    /// <summary>duplicatas: vencimentos/valores exatos (ex.: vindos do XML da NF-e); senão gera parcelas mensais.</summary>
    public static long RegistrarCompra(DbSessao s, long fornecedorId, string? nota, IList<ItemDoc> itens, string forma,
        int parcelas, DateTime vencimento, IList<(DateTime Vencimento, decimal Valor)>? duplicatas = null)
    {
        nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();
        var total = itens.Sum(i => i.Total);

        var id = s.Inserir("""
            INSERT INTO compras(data, fornecedor_id, usuario_id, numero_nota, total, forma_pagamento)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5)
            """, DateTime.Now, fornecedorId, Sessao.UsuarioId, nota, total, forma);

        foreach (var i in itens)
        {
            s.Exec("""
                INSERT INTO compra_itens(compra_id, produto_id, quantidade, custo_unitario, total)
                VALUES(@p0, @p1, @p2, @p3, @p4)
                """, id, i.ProdutoId, i.Quantidade, i.Unitario, i.Total);

            // custo médio ponderado
            s.Exec("""
                UPDATE produtos SET preco_custo =
                    CASE WHEN estoque > 0 THEN ROUND((estoque * preco_custo + @p0 * @p1) / (estoque + @p0), 4) ELSE @p1 END
                WHERE id = @p2
                """, i.Quantidade, i.Unitario, i.ProdutoId);

            MovimentarEstoque(s, i.ProdutoId, "ENTRADA", i.Quantidade, $"Compra nº {id}", id);
        }

        var descricao = $"Compra nº {id}" + (nota is null ? "" : $" (NF {nota})");
        if (forma == APrazo && duplicatas is { Count: > 0 })
        {
            int n = 1;
            foreach (var (venc, valor) in duplicatas)
                s.Exec("""
                    INSERT INTO contas(tipo, descricao, valor, vencimento, status, fornecedor_id, compra_id)
                    VALUES('PAGAR', @p0, @p1, @p2, 'ABERTA', @p3, @p4)
                    """, $"{descricao} - duplicata {n++}/{duplicatas.Count}", valor, Db.Dia(venc), fornecedorId, id);
        }
        else
            GerarContas(s, "PAGAR", descricao, total, forma, forma == APrazo, parcelas, vencimento,
                null, fornecedorId, null, id);
        return id;
    }

    public static void CancelarVenda(long id)
    {
        using var s = new DbSessao(transacao: true);
        if (Convert.ToString(s.Scalar("SELECT status FROM vendas WHERE id = @p0", id)) != "FINALIZADA")
            throw new InvalidOperationException("Esta venda já está cancelada.");

        var itens = s.Query("SELECT produto_id, quantidade FROM venda_itens WHERE venda_id = @p0", id);
        foreach (DataRow r in itens.Rows)
            MovimentarEstoque(s, (long)r["produto_id"], "ENTRADA", Convert.ToDecimal(r["quantidade"]), $"Cancelamento da venda nº {id}", id);

        s.Exec("UPDATE vendas SET status = 'CANCELADA' WHERE id = @p0", id);
        s.Exec("UPDATE contas SET status = 'CANCELADA' WHERE venda_id = @p0", id);
        s.Exec("UPDATE pedidos SET status = 'ABERTO', venda_id = NULL WHERE venda_id = @p0", id); // pedido volta a ficar pendente
        s.Commit();
    }

    public static void CancelarCompra(long id)
    {
        using var s = new DbSessao(transacao: true);
        if (Convert.ToString(s.Scalar("SELECT status FROM compras WHERE id = @p0", id)) != "FINALIZADA")
            throw new InvalidOperationException("Esta compra já está cancelada.");

        var itens = s.Query("SELECT produto_id, quantidade FROM compra_itens WHERE compra_id = @p0", id);
        foreach (DataRow r in itens.Rows)
            MovimentarEstoque(s, (long)r["produto_id"], "SAIDA", -Convert.ToDecimal(r["quantidade"]), $"Cancelamento da compra nº {id}", id);

        s.Exec("UPDATE compras SET status = 'CANCELADA' WHERE id = @p0", id);
        s.Exec("UPDATE contas SET status = 'CANCELADA' WHERE compra_id = @p0", id);
        s.Exec("UPDATE pedidos SET status = 'ABERTO', compra_id = NULL WHERE compra_id = @p0", id);
        s.Commit();
    }

    public static void AjustarEstoque(long produtoId, string tipo, decimal delta, string motivo)
    {
        using var s = new DbSessao(transacao: true);
        MovimentarEstoque(s, produtoId, tipo, delta, "Ajuste manual: " + motivo);
        s.Commit();
    }

    public static void LancarConta(string tipo, string descricao, decimal valor, int parcelas, DateTime vencimento,
        long? clienteId, long? fornecedorId)
    {
        using var s = new DbSessao(transacao: true);
        GerarContas(s, tipo, descricao, valor, "", aPrazo: true, parcelas, vencimento, clienteId, fornecedorId, null, null);
        s.Commit();
    }

    public static void BaixarConta(long id, DateTime data, decimal valorPago, string forma) =>
        Db.Exec("""
            UPDATE contas SET status = 'PAGA', data_pagamento = @p0, valor_pago = @p1, forma_pagamento = @p2
            WHERE id = @p3 AND status = 'ABERTA'
            """, Db.Dia(data), valorPago, forma, id);

    public static void EstornarConta(long id) =>
        Db.Exec("""
            UPDATE contas SET status = 'ABERTA', data_pagamento = NULL, valor_pago = NULL
            WHERE id = @p0 AND status = 'PAGA'
            """, id);

    public static void CancelarConta(long id) =>
        Db.Exec("UPDATE contas SET status = 'CANCELADA' WHERE id = @p0 AND status = 'ABERTA'", id);
}
