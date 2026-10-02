namespace ErpLoja.Data;

/// <summary>Insumo da ficha técnica (linha da grade de edição).</summary>
[System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
public class InsumoFicha
{
    [Browsable(false)] public long InsumoId { get; set; }
    [DisplayName("Código")] public string Codigo { get; set; } = "";
    [DisplayName("Insumo")] public string Insumo { get; set; } = "";
    [DisplayName("Un.")] public string Unidade { get; set; } = "";
    [DisplayName("Quantidade")] public decimal Quantidade { get; set; }
    [DisplayName("Perda %")] public decimal PerdaPct { get; set; }
    [DisplayName("Custo unit.")] public decimal CustoUnitario { get; set; }
    [DisplayName("Custo")] public decimal Custo => Math.Round(Quantidade * (1 + PerdaPct / 100) * CustoUnitario, 2);
}

/// <summary>
/// Produção: ficha técnica (receita) e ordens de produção.
/// Concluir uma OP consome os insumos (saída de estoque), dá entrada no produto acabado e recalcula o custo médio dele
/// com o custo real: Σ(consumo × custo médio do insumo) + custo adicional da ficha proporcional à quantidade.
/// </summary>
public static class Producao
{
    public const string Planejada = "PLANEJADA", EmProducao = "EM_PRODUCAO", Concluida = "CONCLUIDA", Cancelada = "CANCELADA";

    public record Ficha(long? Id, long ProdutoId, decimal Rendimento, decimal CustoAdicional, string Observacoes, BindingList<InsumoFicha> Insumos)
    {
        public decimal CustoInsumos => Insumos.Sum(i => i.Custo);
        public decimal CustoLote => CustoInsumos + CustoAdicional;
        public decimal CustoUnitario => Rendimento > 0 ? Math.Round(CustoLote / Rendimento, 4) : 0;
    }

    public static Ficha CarregarFicha(long produtoId)
    {
        var f = Db.Query("SELECT * FROM fichas_tecnicas WHERE produto_id = @p0", produtoId).Rows.Cast<DataRow>().FirstOrDefault();
        var insumos = new BindingList<InsumoFicha>();
        if (f is null)
            return new Ficha(null, produtoId, 1, 0, "", insumos);
        foreach (DataRow i in Db.Query("""
                     SELECT fi.insumo_id, fi.quantidade, fi.perda_pct, p.codigo, p.nome, p.unidade, p.preco_custo
                     FROM ficha_itens fi JOIN produtos p ON p.id = fi.insumo_id
                     WHERE fi.ficha_id = @p0 ORDER BY fi.id
                     """, f["id"]).Rows)
            insumos.Add(new InsumoFicha
            {
                InsumoId = (long)i["insumo_id"],
                Codigo = Ui.Txt(i["codigo"]),
                Insumo = Ui.Txt(i["nome"]),
                Unidade = Ui.Txt(i["unidade"]),
                Quantidade = Ui.Dec(i["quantidade"]),
                PerdaPct = Ui.Dec(i["perda_pct"]),
                CustoUnitario = Ui.Dec(i["preco_custo"]),
            });
        return new Ficha((long)f["id"], produtoId, Ui.Dec(f["rendimento"]), Ui.Dec(f["custo_adicional"]), Ui.Txt(f["observacoes"]), insumos);
    }

    public static void SalvarFicha(Ficha f)
    {
        if (f.Rendimento <= 0)
            throw new InvalidOperationException("O rendimento precisa ser maior que zero.");
        if (f.Insumos.Count == 0)
            throw new InvalidOperationException("Inclua pelo menos um insumo.");
        if (f.Insumos.Any(i => i.InsumoId == f.ProdutoId))
            throw new InvalidOperationException("O produto não pode ser insumo da própria ficha.");
        if (f.Insumos.Any(i => i.Quantidade <= 0))
            throw new InvalidOperationException("Todos os insumos precisam de quantidade maior que zero.");
        if (f.Insumos.FirstOrDefault(i => i.PerdaPct is < 0 or > 100) is { } perda)
            throw new InvalidOperationException($"Perda inválida no insumo \"{perda.Insumo}\": informe de 0 a 100%.");
        if (f.Insumos.GroupBy(i => i.InsumoId).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Há insumo repetido na ficha.");
        foreach (var i in f.Insumos)
            if (UsaNaFabricacao(i.InsumoId, f.ProdutoId))
                throw new InvalidOperationException(
                    $"O insumo \"{i.Insumo}\" é fabricado (direta ou indiretamente) com este produto: a receita ficaria circular. " +
                    "Remova esse insumo ou corrija a ficha técnica dele.");

        using var s = new DbSessao(transacao: true);
        var id = s.Scalar("SELECT id FROM fichas_tecnicas WHERE produto_id = @p0", f.ProdutoId) as long?;
        if (id is long existente)
        {
            s.Exec("UPDATE fichas_tecnicas SET rendimento = @p0, custo_adicional = @p1, observacoes = @p2, atualizado_em = @p3 WHERE id = @p4",
                f.Rendimento, f.CustoAdicional, f.Observacoes, DateTime.Now, existente);
            s.Exec("DELETE FROM ficha_itens WHERE ficha_id = @p0", existente);
        }
        else
            id = s.Inserir("INSERT INTO fichas_tecnicas(produto_id, rendimento, custo_adicional, observacoes, atualizado_em) VALUES(@p0, @p1, @p2, @p3, @p4)",
                f.ProdutoId, f.Rendimento, f.CustoAdicional, f.Observacoes, DateTime.Now);
        foreach (var i in f.Insumos)
            s.Exec("INSERT INTO ficha_itens(ficha_id, insumo_id, quantidade, perda_pct) VALUES(@p0, @p1, @p2, @p3)", id, i.InsumoId, i.Quantidade, i.PerdaPct);
        s.Exec("UPDATE produtos SET tipo = 'PRODUTO_ACABADO' WHERE id = @p0 AND tipo = 'MERCADORIA'", f.ProdutoId);
        s.Commit();
    }

    /// <summary>true se a ficha técnica do produto (ou de algum insumo dela, em qualquer nível) usa o insumo procurado.</summary>
    static bool UsaNaFabricacao(long produtoId, long procurado) =>
        Db.Scalar("""
            WITH RECURSIVE usa(id) AS (
                SELECT @p0
                UNION
                SELECT fi.insumo_id FROM ficha_itens fi JOIN fichas_tecnicas ft ON ft.id = fi.ficha_id JOIN usa ON ft.produto_id = usa.id
            )
            SELECT 1 FROM usa WHERE id = @p1 LIMIT 1
            """, produtoId, procurado) is not null;

    /// <summary>Cria uma OP planejada, já com a necessidade de insumos calculada pela ficha (proporção + perdas).</summary>
    public static long CriarOrdem(long produtoId, decimal quantidade, DateTime previsao, string observacoes, long? pedidoId = null)
    {
        if (quantidade <= 0)
            throw new InvalidOperationException("Informe a quantidade a produzir.");
        var f = CarregarFicha(produtoId);
        if (f.Id is null || f.Insumos.Count == 0)
            throw new InvalidOperationException("O produto não tem ficha técnica. Cadastre em Produção > Fichas técnicas.");
        var fator = quantidade / f.Rendimento;
        using var s = new DbSessao(transacao: true);
        var id = s.Inserir("""
            INSERT INTO ordens_producao(data, produto_id, quantidade, status, previsao, observacoes, pedido_id, usuario_id)
            VALUES(@p0, @p1, @p2, 'PLANEJADA', @p3, @p4, @p5, @p6)
            """, DateTime.Now, produtoId, quantidade, Db.Dia(previsao), observacoes, pedidoId, Sessao.UsuarioId);
        foreach (var i in f.Insumos)
            s.Exec("INSERT INTO op_itens(op_id, insumo_id, quantidade_prevista) VALUES(@p0, @p1, @p2)",
                id, i.InsumoId, Math.Round(i.Quantidade * fator * (1 + i.PerdaPct / 100), 4));
        s.Commit();
        return id;
    }

    static string Status(DbSessao s, long op) => Convert.ToString(s.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) ?? "";

    public static void Iniciar(long op)
    {
        using var s = new DbSessao(transacao: true);
        if (Status(s, op) != Planejada)
            throw new InvalidOperationException("Só uma OP planejada pode ser iniciada.");
        s.Exec("UPDATE ordens_producao SET status = 'EM_PRODUCAO', inicio = @p0 WHERE id = @p1", DateTime.Now, op);
        s.Commit();
    }

    public static void Cancelar(long op)
    {
        using var s = new DbSessao(transacao: true);
        if (Status(s, op) is Concluida or Cancelada)
            throw new InvalidOperationException("OP concluída ou cancelada não pode ser cancelada.");
        s.Exec("UPDATE ordens_producao SET status = 'CANCELADA' WHERE id = @p0", op);
        s.Commit();
    }

    /// <summary>
    /// Insumos cujo saldo não cobre o consumo (nome, necessário, saldo). Sem quantidade produzida, usa o previsto da OP;
    /// com ela, o consumo real = previsto × produzido ÷ planejado (produzir mais que o planejado consome mais).
    /// </summary>
    public static List<(string Insumo, decimal Necessario, decimal Saldo)> Faltas(long op, decimal? quantidadeProduzida = null)
    {
        var planejado = Ui.Dec(Db.Scalar("SELECT quantidade FROM ordens_producao WHERE id = @p0", op));
        var proporcao = quantidadeProduzida is decimal q && planejado > 0 ? q / planejado : 1;
        return Db.Query("""
            SELECT p.nome, i.quantidade_prevista, p.estoque FROM op_itens i JOIN produtos p ON p.id = i.insumo_id
            WHERE i.op_id = @p0 ORDER BY p.nome
            """, op).Rows.Cast<DataRow>()
            .Select(r => (Insumo: Ui.Txt(r[0]), Necessario: Math.Round(Ui.Dec(r[1]) * proporcao, 4), Saldo: Ui.Dec(r[2])))
            .Where(x => x.Necessario > x.Saldo).ToList();
    }

    /// <summary>
    /// Conclui a OP: baixa os insumos (consumo real = previsto × produzido ÷ planejado), dá entrada no produto acabado
    /// e recalcula o custo médio com o custo real. Devolve o custo unitário apurado.
    /// </summary>
    public static decimal Concluir(long op, decimal quantidadeProduzida)
    {
        if (quantidadeProduzida <= 0)
            throw new InvalidOperationException("Informe a quantidade produzida.");
        using var s = new DbSessao(transacao: true);
        var o = s.Query("SELECT * FROM ordens_producao WHERE id = @p0", op).Rows[0];
        if (Ui.Txt(o["status"]) is not (Planejada or EmProducao))
            throw new InvalidOperationException("Só OP planejada ou em produção pode ser concluída.");
        if (Parametros.ProducaoSemEstoque == Parametros.Bloquear && Faltas(op, quantidadeProduzida) is { Count: > 0 } faltas)
            throw new InvalidOperationException($"Insumos insuficientes para produzir {quantidadeProduzida:N3}:\n" +
                string.Join("\n", faltas.Select(f => $"• {f.Insumo}: precisa {f.Necessario:N3}, saldo {f.Saldo:N3}")) +
                "\n\nA produção sem insumo está bloqueada em Configurações. Dê entrada nos insumos ou produza menos.");
        var produtoId = (long)o["produto_id"];
        var proporcao = quantidadeProduzida / Ui.Dec(o["quantidade"]);
        var ficha = s.Query("SELECT rendimento, custo_adicional FROM fichas_tecnicas WHERE produto_id = @p0", produtoId).Rows[0];

        decimal custo = 0;
        foreach (DataRow i in s.Query("""
                     SELECT i.id, i.insumo_id, i.quantidade_prevista, p.preco_custo
                     FROM op_itens i JOIN produtos p ON p.id = i.insumo_id WHERE i.op_id = @p0
                     """, op).Rows)
        {
            var consumo = Math.Round(Ui.Dec(i["quantidade_prevista"]) * proporcao, 4);
            var custoUnit = Ui.Dec(i["preco_custo"]);
            custo += Math.Round(consumo * custoUnit, 2);
            s.Exec("UPDATE op_itens SET quantidade_consumida = @p0, custo_unitario = @p1 WHERE id = @p2", consumo, custoUnit, i["id"]);
            Operacoes.MovimentarEstoque(s, (long)i["insumo_id"], "SAIDA", -consumo, $"Consumo na OP nº {op}", op);
        }
        custo += Math.Round(Ui.Dec(ficha["custo_adicional"]) * quantidadeProduzida / Ui.Dec(ficha["rendimento"]), 2);
        var custoUnitario = Math.Round(custo / quantidadeProduzida, 4);

        s.Exec("""
            UPDATE produtos SET preco_custo =
                CASE WHEN estoque > 0 THEN ROUND((estoque * preco_custo + @p0 * @p1) / (estoque + @p0), 4) ELSE @p1 END
            WHERE id = @p2
            """, quantidadeProduzida, custoUnitario, produtoId);
        Operacoes.MovimentarEstoque(s, produtoId, "ENTRADA", quantidadeProduzida, $"Produção da OP nº {op}", op);
        s.Exec("""
            UPDATE ordens_producao SET status = 'CONCLUIDA', quantidade_produzida = @p0, conclusao = @p1, custo_total = @p2,
                   custo_unitario = @p3, inicio = COALESCE(inicio, @p1)
            WHERE id = @p4
            """, quantidadeProduzida, DateTime.Now, custo, custoUnitario, op);
        s.Commit();
        return custoUnitario;
    }

    /// <summary>Texto da OP para imprimir e levar à produção.</summary>
    public static string Texto(long op)
    {
        var o = Db.Query("""
            SELECT o.*, p.codigo, p.nome, p.unidade FROM ordens_producao o JOIN produtos p ON p.id = o.produto_id WHERE o.id = @p0
            """, op).Rows[0];
        var itens = Db.Query("""
            SELECT p.codigo, p.nome, p.unidade, i.quantidade_prevista, i.quantidade_consumida
            FROM op_itens i JOIN produtos p ON p.id = i.insumo_id WHERE i.op_id = @p0 ORDER BY p.nome
            """, op);
        var sb = new System.Text.StringBuilder();
        var traco = new string('-', 80);
        sb.AppendLine($"[C][G]ORDEM DE PRODUÇÃO Nº {op}");
        sb.AppendLine($"[C]{Db.Config("nome_loja")}");
        sb.AppendLine(traco);
        sb.AppendLine($"[N]Produto: {o["codigo"]} - {o["nome"]}");
        sb.AppendLine($"Quantidade: {Ui.Dec(o["quantidade"]):N3} {o["unidade"]}    Emissão: {Db.LerData(o["data"]):dd/MM/yyyy}    " +
                      $"Previsão: {(o["previsao"] is string pr ? Db.LerData(pr).ToString("dd/MM/yyyy") : "-")}    Situação: {o["status"]}");
        sb.AppendLine(traco);
        sb.AppendLine("[N]INSUMOS".PadRight(50) + "Previsto".PadLeft(14) + "Consumido".PadLeft(16));
        foreach (DataRow i in itens.Rows)
            sb.AppendLine($"{i["codigo"]} {i["nome"]}".PadRight(48)[..48] + $" {i["unidade"]}".PadRight(5) +
                          Ui.Dec(i["quantidade_prevista"]).ToString("N3").PadLeft(12) +
                          (i["quantidade_consumida"] is DBNull ? "".PadLeft(16) : Ui.Dec(i["quantidade_consumida"]).ToString("N3").PadLeft(16)));
        sb.AppendLine(traco);
        if (Ui.Txt(o["observacoes"]) is { Length: > 0 } obs)
            sb.AppendLine("Observações: " + obs);
        sb.AppendLine();
        sb.AppendLine("Produzido por: ______________________   Conferido por: ______________________");
        return sb.ToString();
    }
}
