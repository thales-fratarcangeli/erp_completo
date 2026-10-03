namespace ErpLoja.Data;

/// <summary>
/// Ordens de serviço (OS): o trabalho feito sob encomenda para um cliente, do pedido à entrega e ao faturamento.
/// Toda OS tem etapa (nunca fica "sem situação") e prazo; cada mudança de etapa fica no histórico com quem e quando.
/// Faturar é independente da etapa: gera as contas a receber da OS (pode faturar antes ou depois de entregar).
/// </summary>
public static class OrdensServico
{
    public const string Aberta = "ABERTA", Aprovada = "APROVADA", EmExecucao = "EM_EXECUCAO", Pronta = "PRONTA",
        Entregue = "ENTREGUE", Cancelada = "CANCELADA";

    /// <summary>Etapas na ordem do fluxo (Cancelada fica de fora: só pelo cancelamento, com motivo).</summary>
    public static readonly string[] Fluxo = [Aberta, Aprovada, EmExecucao, Pronta, Entregue];

    public static string Nome(string etapa) => etapa switch
    {
        Aberta => "Aberta",
        Aprovada => "Aprovada",
        EmExecucao => "Em execução",
        Pronta => "Pronta",
        Entregue => "Entregue",
        Cancelada => "Cancelada",
        _ => etapa,
    };

    /// <summary>Expressão SQL da situação do prazo (alias da OS: o).</summary>
    public const string SqlSituacao = """
        CASE WHEN o.etapa = 'CANCELADA' THEN 'CANCELADA'
             WHEN o.etapa = 'ENTREGUE' AND date(o.entregue_em) > o.prazo THEN 'ENTREGUE COM ATRASO'
             WHEN o.etapa = 'ENTREGUE' THEN 'ENTREGUE'
             WHEN o.prazo < date('now', 'localtime') THEN 'ATRASADA'
             WHEN o.prazo = date('now', 'localtime') THEN 'VENCE HOJE'
             ELSE 'NO PRAZO' END
        """;

    /// <summary>OS em andamento (nem entregue nem cancelada).</summary>
    public const string SqlEmAndamento = "o.etapa NOT IN ('ENTREGUE', 'CANCELADA')";

    public record Dados(long ClienteId, string Descricao, DateTime Prazo, decimal Valor, long? ResponsavelId, string Observacoes);

    static void Conferir(Dados d, DateTime abertura)
    {
        if (d.ClienteId <= 0)
            throw new InvalidOperationException("Informe o cliente da OS.");
        if (string.IsNullOrWhiteSpace(d.Descricao))
            throw new InvalidOperationException("Descreva o serviço (o que será feito).");
        if (d.Valor < 0)
            throw new InvalidOperationException("O valor não pode ser negativo.");
        if (d.Prazo.Date < abertura.Date)
            throw new InvalidOperationException($"O prazo de entrega não pode ser antes da abertura da OS ({abertura:dd/MM/yyyy}).");
        if (!Datas.Plausivel(d.Prazo))
            throw new InvalidOperationException(Datas.MensagemImplausivel("prazo de entrega"));
    }

    public static long Criar(Dados d)
    {
        Conferir(d, DateTime.Today);
        using var s = new DbSessao(transacao: true);
        var id = s.Inserir("""
            INSERT INTO ordens_servico(cliente_id, descricao, prazo, valor, responsavel_id, observacoes, etapa, usuario_id)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, 'ABERTA', @p6)
            """, d.ClienteId, d.Descricao.Trim(), Db.Dia(d.Prazo), d.Valor, d.ResponsavelId, d.Observacoes.Trim(), Sessao.UsuarioId);
        Historico(s, id, Aberta, "OS aberta");
        s.Commit();
        return id;
    }

    public static void Alterar(long id, Dados d)
    {
        var o = Ler(id);
        if (Ui.Txt(o["etapa"]) == Cancelada)
            throw new InvalidOperationException("OS cancelada não pode ser alterada.");
        Conferir(d, Db.LerData(o["data"]));
        if (o["faturada_em"] is not DBNull && Ui.Dec(o["valor"]) != d.Valor)
            throw new InvalidOperationException("A OS já foi faturada: o valor não pode mudar (as contas a receber já foram geradas). Cancele as contas no Financeiro antes, se for o caso.");
        using var s = new DbSessao(transacao: true);
        s.Exec("""
            UPDATE ordens_servico SET cliente_id = @p0, descricao = @p1, prazo = @p2, valor = @p3, responsavel_id = @p4, observacoes = @p5
            WHERE id = @p6
            """, d.ClienteId, d.Descricao.Trim(), Db.Dia(d.Prazo), d.Valor, d.ResponsavelId, d.Observacoes.Trim(), id);
        if (Db.LerData(o["prazo"]).Date != d.Prazo.Date)
            Historico(s, id, Ui.Txt(o["etapa"]), $"Prazo alterado de {Db.LerData(o["prazo"]):dd/MM/yyyy} para {d.Prazo:dd/MM/yyyy}");
        s.Commit();
    }

    public static DataRow Ler(long id)
    {
        var dt = Db.Query("SELECT * FROM ordens_servico WHERE id = @p0", id);
        return dt.Rows.Count == 1 ? dt.Rows[0] : throw new InvalidOperationException($"OS nº {id} não encontrada.");
    }

    /// <summary>Próxima etapa do fluxo (null se já entregue ou cancelada).</summary>
    public static string? Proxima(string etapa)
    {
        var i = Array.IndexOf(Fluxo, etapa);
        return i >= 0 && i < Fluxo.Length - 1 ? Fluxo[i + 1] : null;
    }

    /// <summary>
    /// Muda a etapa. Avançar é livre; voltar exige o motivo (fica no histórico). Entregar grava a data da entrega;
    /// voltar de "Entregue" apaga essa data.
    /// </summary>
    public static void MudarEtapa(long id, string etapa, string observacao = "")
    {
        if (!Fluxo.Contains(etapa))
            throw new InvalidOperationException("Etapa inválida. Para cancelar, use \"Cancelar OS\".");
        var atual = Ui.Txt(Ler(id)["etapa"]);
        if (atual == Cancelada)
            throw new InvalidOperationException("OS cancelada não muda de etapa.");
        if (atual == etapa)
            return;
        var voltando = Array.IndexOf(Fluxo, etapa) < Array.IndexOf(Fluxo, atual);
        if (voltando && string.IsNullOrWhiteSpace(observacao))
            throw new InvalidOperationException($"Para voltar a OS de \"{Nome(atual)}\" para \"{Nome(etapa)}\", informe o motivo.");
        using var s = new DbSessao(transacao: true);
        s.Exec("""
            UPDATE ordens_servico SET etapa = @p0,
                   entregue_em = CASE WHEN @p0 = 'ENTREGUE' THEN datetime('now', 'localtime') ELSE NULL END
            WHERE id = @p1
            """, etapa, id);
        Historico(s, id, etapa, string.IsNullOrWhiteSpace(observacao) ? $"{Nome(atual)} → {Nome(etapa)}" : $"{Nome(atual)} → {Nome(etapa)}: {observacao.Trim()}");
        s.Commit();
    }

    public static void Cancelar(long id, string motivo)
    {
        var o = Ler(id);
        if (Ui.Txt(o["etapa"]) == Cancelada)
            throw new InvalidOperationException("Esta OS já está cancelada.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Informe o motivo do cancelamento.");
        if (o["faturada_em"] is not DBNull
            && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM contas WHERE os_id = @p0 AND status <> 'CANCELADA'", id)) > 0)
            throw new InvalidOperationException("A OS já foi faturada. Cancele (ou estorne) as contas a receber dela no Financeiro antes de cancelar a OS.");
        using var s = new DbSessao(transacao: true);
        s.Exec("UPDATE ordens_servico SET etapa = 'CANCELADA' WHERE id = @p0", id);
        Historico(s, id, Cancelada, "Cancelada: " + motivo.Trim());
        s.Commit();
    }

    /// <summary>Gera as contas a receber da OS (parcelas mensais a partir do 1º vencimento). Pode faturar uma única vez.</summary>
    public static void Faturar(long id, int parcelas, DateTime primeiroVencimento)
    {
        var o = Ler(id);
        if (Ui.Txt(o["etapa"]) == Cancelada)
            throw new InvalidOperationException("OS cancelada não pode ser faturada.");
        if (o["faturada_em"] is not DBNull)
            throw new InvalidOperationException("Esta OS já foi faturada (veja as contas no Financeiro).");
        var valor = Ui.Dec(o["valor"]);
        if (valor <= 0)
            throw new InvalidOperationException("Informe o valor da OS (Editar) antes de faturar.");
        if (parcelas < 1 || parcelas > Parametros.MaxParcelas)
            throw new InvalidOperationException($"Parcelas: de 1 a {Parametros.MaxParcelas}.");
        if (primeiroVencimento.Date < DateTime.Today || !Datas.Plausivel(primeiroVencimento))
            throw new InvalidOperationException("O 1º vencimento deve ser hoje ou depois (e numa data plausível).");
        using var s = new DbSessao(transacao: true);
        var valorParcela = Math.Round(valor / parcelas, 2);
        for (int i = 0; i < parcelas; i++)
        {
            var v = i == parcelas - 1 ? valor - valorParcela * (parcelas - 1) : valorParcela;
            s.Exec("""
                INSERT INTO contas(tipo, descricao, valor, vencimento, status, cliente_id, os_id)
                VALUES('RECEBER', @p0, @p1, @p2, 'ABERTA', @p3, @p4)
                """, parcelas > 1 ? $"OS nº {id} - parcela {i + 1}/{parcelas}" : $"OS nº {id}", v,
                Db.Dia(primeiroVencimento.AddMonths(i)), o["cliente_id"], id);
        }
        s.Exec("UPDATE ordens_servico SET faturada_em = datetime('now', 'localtime') WHERE id = @p0", id);
        Historico(s, id, Ui.Txt(o["etapa"]), $"Faturada: {Ui.Moeda(valor)} em {parcelas} parcela(s), 1º vencimento {primeiroVencimento:dd/MM/yyyy}");
        s.Commit();
    }

    static void Historico(DbSessao s, long id, string etapa, string observacao) =>
        s.Exec("INSERT INTO os_historico(os_id, etapa, observacao, usuario_id) VALUES(@p0, @p1, @p2, @p3)",
            id, etapa, observacao, Sessao.UsuarioId);

    public record Indicadores(int EmAndamento, int Atrasadas, int VencemHoje, int Prontas, int AFaturar, decimal? PctNoPrazo);

    /// <summary>Números do painel de prazos. % no prazo = entregas dos últimos 90 dias feitas até o prazo.</summary>
    public static Indicadores Resumo()
    {
        var r = Db.Query($"""
            SELECT
              COALESCE(SUM(CASE WHEN {SqlEmAndamento} THEN 1 END), 0) AS andamento,
              COALESCE(SUM(CASE WHEN {SqlEmAndamento} AND o.prazo < date('now', 'localtime') THEN 1 END), 0) AS atrasadas,
              COALESCE(SUM(CASE WHEN {SqlEmAndamento} AND o.prazo = date('now', 'localtime') THEN 1 END), 0) AS hoje,
              COALESCE(SUM(CASE WHEN o.etapa = 'PRONTA' THEN 1 END), 0) AS prontas,
              COALESCE(SUM(CASE WHEN o.etapa <> 'CANCELADA' AND o.faturada_em IS NULL AND o.valor > 0 AND o.etapa = 'ENTREGUE' THEN 1 END), 0) AS faturar,
              SUM(CASE WHEN o.etapa = 'ENTREGUE' AND o.entregue_em >= date('now', 'localtime', '-90 days') THEN 1 END) AS entregues,
              SUM(CASE WHEN o.etapa = 'ENTREGUE' AND o.entregue_em >= date('now', 'localtime', '-90 days') AND date(o.entregue_em) <= o.prazo THEN 1 END) AS no_prazo
            FROM ordens_servico o
            """).Rows[0];
        decimal? pct = r["entregues"] is DBNull || Convert.ToInt64(r["entregues"]) == 0 ? null
            : Math.Round(Convert.ToDecimal(r["no_prazo"] is DBNull ? 0 : r["no_prazo"]) * 100 / Convert.ToDecimal(r["entregues"]), 1);
        return new Indicadores(Convert.ToInt32(r["andamento"]), Convert.ToInt32(r["atrasadas"]), Convert.ToInt32(r["hoje"]),
            Convert.ToInt32(r["prontas"]), Convert.ToInt32(r["faturar"]), pct);
    }

    /// <summary>Ficha da OS para imprimir (marcações de Impressao: [C] centro, [G] grande, [N] negrito).</summary>
    public static string Texto(long id)
    {
        var o = Db.Query("""
            SELECT o.*, p.nome AS cliente, p.telefone, u.nome AS responsavel
            FROM ordens_servico o JOIN pessoas p ON p.id = o.cliente_id LEFT JOIN usuarios u ON u.id = o.responsavel_id
            WHERE o.id = @p0
            """, id).Rows[0];
        var sb = new System.Text.StringBuilder();
        var traco = new string('-', 80);
        sb.AppendLine($"[C][G]ORDEM DE SERVIÇO Nº {id}");
        sb.AppendLine($"[C]{Db.Config("nome_loja")}");
        sb.AppendLine(traco);
        sb.AppendLine($"[N]Cliente: {o["cliente"]}" + (Ui.Txt(o["telefone"]) is { Length: > 0 } tel ? $"    Telefone: {tel}" : ""));
        sb.AppendLine($"Abertura: {Db.LerData(o["data"]):dd/MM/yyyy}    Prazo: {Db.LerData(o["prazo"]):dd/MM/yyyy}    Etapa: {Nome(Ui.Txt(o["etapa"]))}");
        if (Ui.Txt(o["responsavel"]) is { Length: > 0 } resp)
            sb.AppendLine("Responsável: " + resp);
        sb.AppendLine(traco);
        sb.AppendLine("[N]SERVIÇO");
        sb.AppendLine(Ui.Txt(o["descricao"]));
        sb.AppendLine(traco);
        sb.AppendLine($"[N]Valor: {Ui.Moeda(Ui.Dec(o["valor"]))}" + (o["faturada_em"] is DBNull ? "" : $"    Faturada em {Db.LerData(o["faturada_em"]):dd/MM/yyyy}"));
        if (Ui.Txt(o["observacoes"]) is { Length: > 0 } obs)
            sb.AppendLine("Observações: " + obs);
        sb.AppendLine();
        sb.AppendLine("Recebido por: ______________________________   Data: ____/____/______");
        return sb.ToString();
    }
}
