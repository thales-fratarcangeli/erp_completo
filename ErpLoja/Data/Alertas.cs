namespace ErpLoja.Data;

/// <summary>Aviso da tela inicial: o que precisa de atenção hoje, com a tela (e o filtro) que resolve.</summary>
public record Alerta(string Texto, string Tela, string Filtro, bool Grave);

/// <summary>Telas que sabem abrir já filtradas (usadas pelos alertas da tela inicial).</summary>
public interface IFiltroInicial
{
    void AplicarFiltro(string filtro);
}

public static class Alertas
{
    public const string OsAtrasadas = "atrasadas", OsHoje = "hoje", OsProntas = "prontas", OsAFaturar = "faturar",
        ContasVencidas = "vencidas", PromessasVencidas = "promessas", EstoqueBaixo = "baixo";

    /// <summary>Alertas atuais, dos mais graves para os informativos. Sem nada pendente, a lista vem vazia.</summary>
    public static List<Alerta> Listar()
    {
        var lista = new List<Alerta>();
        var os = OrdensServico.Resumo();
        if (os.Atrasadas > 0)
            lista.Add(new($"{os.Atrasadas} ordem(ns) de serviço atrasada(s)", nameof(Forms.OrdensServicoForm), OsAtrasadas, true));
        if (os.VencemHoje > 0)
            lista.Add(new($"{os.VencemHoje} OS com entrega para hoje", nameof(Forms.OrdensServicoForm), OsHoje, false));
        if (os.Prontas > 0)
            lista.Add(new($"{os.Prontas} OS pronta(s) aguardando entrega", nameof(Forms.OrdensServicoForm), OsProntas, false));
        if (os.AFaturar > 0)
            lista.Add(new($"{os.AFaturar} OS entregue(s) sem faturar", nameof(Forms.OrdensServicoForm), OsAFaturar, false));

        var c = Db.Query($"""
            SELECT COUNT(*) AS n, COALESCE(SUM(valor), 0) AS total FROM contas c
            WHERE c.tipo = 'RECEBER' AND c.status = 'ABERTA' AND c.vencimento < date('now', 'localtime')
            """).Rows[0];
        if (Convert.ToInt64(c["n"]) > 0)
            lista.Add(new($"{c["n"]} conta(s) a receber vencida(s): {Ui.Moeda(Ui.Dec(c["total"]))}", nameof(Forms.FinanceiroForm), ContasVencidas, true));
        var promessas = Convert.ToInt64(Db.Scalar($"""
            SELECT COUNT(*) FROM contas c WHERE c.tipo = 'RECEBER' AND c.status = 'ABERTA' AND {ContatosCobranca.SqlPromessaVencida}
            """));
        if (promessas > 0)
            lista.Add(new($"{promessas} promessa(s) de pagamento não cumprida(s)", nameof(Forms.FinanceiroForm), PromessasVencidas, true));
        var baixo = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM produtos WHERE ativo = 1 AND estoque_minimo > 0 AND estoque <= estoque_minimo"));
        if (baixo > 0)
            lista.Add(new($"{baixo} produto(s) com estoque baixo", nameof(Forms.EstoqueForm), EstoqueBaixo, false));
        return lista.OrderByDescending(a => a.Grave).ToList();
    }
}
