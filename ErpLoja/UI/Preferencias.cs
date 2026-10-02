namespace ErpLoja.UI;

/// <summary>Preferências de cada usuário guardadas no banco (hoje: tema claro ou escuro).</summary>
public static class Preferencias
{
    public const string Claro = "CLARO", Escuro = "ESCURO";

    /// <summary>Aplica o tema do usuário que acabou de entrar.</summary>
    public static void CarregarTema()
    {
        var tema = Sessao.UsuarioId is long id ? Convert.ToString(Db.Scalar("SELECT tema FROM usuarios WHERE id = @p0", id)) : null;
        Tema.Definir(tema == Escuro);
    }

    /// <summary>Troca o tema na hora (todas as janelas abertas) e guarda a escolha do usuário.</summary>
    public static void SalvarTema(bool escuro)
    {
        Tema.Definir(escuro);
        if (Sessao.UsuarioId is long id)
            Db.Exec("UPDATE usuarios SET tema = @p0 WHERE id = @p1", escuro ? Escuro : Claro, id);
        Log.Registrar(Log.Acao, "Tema alterado", escuro ? "modo escuro" : "modo claro");
    }
}
