namespace ErpLoja.Data;

/// <summary>
/// Acompanhamento da cobrança: cada contato com o cliente sobre uma conta a receber (quando, como, o que foi dito e a
/// promessa de pagamento). Mostra quem já foi cobrado e quando (último contato na lista) e aponta promessas vencidas.
/// </summary>
public static class ContatosCobranca
{
    public static readonly string[] Meios = ["Telefone", "WhatsApp", "E-mail", "Pessoalmente", "Carta", "Outro"];

    public static long Registrar(long contaId, string meio, string resumo, DateTime? promessa)
    {
        var c = Db.Query("SELECT tipo, status FROM contas WHERE id = @p0", contaId);
        if (c.Rows.Count == 0)
            throw new InvalidOperationException("Conta não encontrada.");
        if (Ui.Txt(c.Rows[0]["tipo"]) != "RECEBER" || Ui.Txt(c.Rows[0]["status"]) != "ABERTA")
            throw new InvalidOperationException("Contatos de cobrança só valem para contas a receber em aberto.");
        if (!Meios.Contains(meio))
            throw new InvalidOperationException("Escolha como foi o contato.");
        if (string.IsNullOrWhiteSpace(resumo))
            throw new InvalidOperationException("Escreva o que foi conversado (ex.: \"cliente vai pagar na sexta\").");
        if (promessa is { } p && (p.Date < DateTime.Today || !Datas.Plausivel(p)))
            throw new InvalidOperationException("A promessa de pagamento deve ser de hoje em diante (e numa data plausível).");
        using var s = new DbSessao(transacao: true);
        var id = s.Inserir("""
            INSERT INTO cobranca_contatos(conta_id, meio, resumo, promessa, usuario_id) VALUES(@p0, @p1, @p2, @p3, @p4)
            """, contaId, meio, resumo.Trim(), promessa is { } d ? Db.Dia(d) : null, Sessao.UsuarioId);
        s.Commit();
        return id;
    }

    public static DataTable Listar(long contaId) => Db.Query("""
        SELECT strftime('%d/%m/%Y %H:%M', c.data) AS "Data", c.meio AS "Meio", c.resumo AS "Conversa",
               strftime('%d/%m/%Y', c.promessa) AS "Promessa", u.nome AS "Por"
        FROM cobranca_contatos c LEFT JOIN usuarios u ON u.id = c.usuario_id
        WHERE c.conta_id = @p0 ORDER BY c.data DESC, c.id DESC
        """, contaId);

    /// <summary>Contas a receber em aberto cuja última promessa de pagamento já passou.</summary>
    public const string SqlPromessaVencida = """
        (SELECT cc.promessa FROM cobranca_contatos cc WHERE cc.conta_id = c.id ORDER BY cc.data DESC, cc.id DESC LIMIT 1) < date('now', 'localtime')
        """;
}
