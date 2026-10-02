using System.Security.Cryptography;

namespace ErpLoja;

/// <summary>Usuário logado.</summary>
public static class Sessao
{
    public static long? UsuarioId { get; set; }
    public static string Nome { get; set; } = "";
    public static bool Admin { get; set; }
}

/// <summary>
/// Hash de senhas com PBKDF2-HMAC-SHA256 (formato: iteracoes.salt.hash). 600.000 iterações, como recomenda a OWASP
/// (Password Storage Cheat Sheet); hashes antigos com menos iterações continuam válidos e são refeitos no próximo login.
/// </summary>
public static class Senhas
{
    public const int Iteracoes = 600_000;

    /// <summary>O hash foi gerado com menos iterações que o padrão atual (deve ser refeito com a senha digitada).</summary>
    public static bool PrecisaAtualizar(string armazenado) =>
        !int.TryParse(armazenado.Split('.')[0], out var iteracoes) || iteracoes < Iteracoes;

    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, 32);
        return $"{Iteracoes}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string armazenado)
    {
        var partes = armazenado.Split('.');
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteracoes) || iteracoes is < 1000 or > 10_000_000)
            return false;

        byte[] salt, esperado;
        try
        {
            salt = Convert.FromBase64String(partes[1]);
            esperado = Convert.FromBase64String(partes[2]);
        }
        catch (FormatException)
        {
            return false; // hash corrompido: nega o acesso (falha fechada)
        }
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(hash, esperado);
    }
}

/// <summary>
/// Controle de tentativas de acesso (OWASP A07): após 5 senhas erradas seguidas o usuário fica bloqueado por 15 minutos;
/// o acesso certo zera o contador. Trocar a senha (administrador ou recuperação) desbloqueia (gatilho da migração 13).
/// </summary>
public static class ControleDeAcesso
{
    public const int TentativasAntesDoBloqueio = 5;
    public const int MinutosBloqueio = 15;

    /// <summary>Tempo que falta para o desbloqueio, ou null se o usuário não está bloqueado.</summary>
    public static TimeSpan? Bloqueado(System.Data.DataRow usuario)
    {
        if (usuario["bloqueado_ate"] is not string ate || !DateTime.TryParse(ate, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var limite))
            return null;
        var falta = limite - DateTime.Now;
        return falta > TimeSpan.Zero ? falta : null;
    }

    /// <summary>Conta uma senha errada; devolve true se o usuário acabou de ser bloqueado.</summary>
    public static bool RegistrarFalha(long usuarioId)
    {
        using var s = new DbSessao(transacao: true);
        var falhas = Convert.ToInt64(s.Scalar("SELECT falhas_login FROM usuarios WHERE id = @p0", usuarioId)) + 1;
        var bloquear = falhas >= TentativasAntesDoBloqueio;
        s.Exec("UPDATE usuarios SET falhas_login = @p0, bloqueado_ate = @p1 WHERE id = @p2",
            bloquear ? 0 : falhas, bloquear ? DateTime.Now.AddMinutes(MinutosBloqueio) : null, usuarioId);
        s.Commit();
        return bloquear;
    }

    /// <summary>Acesso certo: zera o contador e, se o hash da senha usa o padrão antigo, refaz com o padrão atual.</summary>
    public static void RegistrarSucesso(long usuarioId, string senhaDigitada, string hashAtual)
    {
        if (Senhas.PrecisaAtualizar(hashAtual))
            Db.Exec("UPDATE usuarios SET senha = @p0 WHERE id = @p1", Senhas.Hash(senhaDigitada), usuarioId);
        Db.Exec("UPDATE usuarios SET falhas_login = 0, bloqueado_ate = NULL WHERE id = @p0 AND (falhas_login <> 0 OR bloqueado_ate IS NOT NULL)", usuarioId);
    }

    /// <summary>
    /// Alerta para o administrador (OWASP A09): tentativas de acesso recusadas desde a última vez que ele entrou.
    /// Chamar antes de registrar a entrada atual no log.
    /// </summary>
    public static string? AlertaDeTentativas(long usuarioId)
    {
        try
        {
            if (!File.Exists(Log.Caminho))
                return null;
            using var c = Log.Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*) FROM log
                WHERE categoria = 'ACESSO' AND (acao LIKE 'Acesso recusado%' OR acao LIKE 'Usuário bloqueado%')
                  AND id > COALESCE((SELECT MAX(id) FROM log WHERE categoria = 'ACESSO' AND acao = 'Entrou no sistema' AND usuario_id = @u), 0)
                """;
            cmd.Parameters.AddWithValue("@u", usuarioId);
            using var r = cmd.ExecuteReader();
            if (!r.Read() || r.GetInt64(0) == 0)
                return null;
            return $"Atenção: {r.GetInt64(0)} tentativa(s) de acesso recusada(s) desde a sua última entrada. Confira em Sistema > Log de atividades (categoria ACESSO).";
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Autorização na camada de dados (OWASP A01): operações sensíveis conferem o perfil mesmo que a tela já tenha
/// conferido, para que nenhum caminho alternativo (outra tela, atalho, automação) passe sem permissão.
/// </summary>
public static class Permissoes
{
    public static void ExigirAdmin(string acao)
    {
        if (Sessao.Admin)
            return;
        Log.Registrar(Log.Acesso, "Acesso negado: " + acao, "operação exclusiva de administrador");
        throw new UnauthorizedAccessException($"Somente administradores podem {acao}.");
    }
}
