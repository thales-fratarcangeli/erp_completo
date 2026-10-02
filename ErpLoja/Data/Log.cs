using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace ErpLoja.Data;

/// <summary>
/// Log de atividades: cada ação (acesso, tela aberta, botão, gravação de dados, erro) vira uma linha num banco SQLite
/// separado, ao lado do banco da loja ("loja.db" → "loja_log.db"). O log nunca interrompe a operação: falhas ao gravar
/// o log são ignoradas. Valores gravados (senhas, preços...) não são registrados; só o que foi feito, onde e por quem.
/// </summary>
public static class Log
{
    public const string Acesso = "ACESSO", Tela = "TELA", Acao = "AÇÃO", Dados = "DADOS", Erro = "ERRO", Sistema = "SISTEMA";
    public static readonly string[] Categorias = [Acesso, Tela, Acao, Dados, Erro, Sistema];

    static readonly object trava = new();
    static string? estruturaCriadaEm;
    [ThreadStatic] static int suspenso;

    /// <summary>Tela em primeiro plano (preenchida pela janela principal) para contextualizar as gravações.</summary>
    public static string TelaAtual { get; set; } = "";

    /// <summary>Arquivo do log para o banco atual.</summary>
    public static string Caminho => string.IsNullOrWhiteSpace(Db.Caminho) ? "" :
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Db.Caminho))!, Path.GetFileNameWithoutExtension(Db.Caminho) + "_log.db");

    /// <summary>Suspende o registro de gravações nesta thread (migrações e geração em massa registram um resumo próprio).</summary>
    public static IDisposable Suspender()
    {
        suspenso++;
        return new Retomar();
    }

    sealed class Retomar : IDisposable
    {
        bool feito;
        public void Dispose()
        {
            if (!feito)
                suspenso--;
            feito = true;
        }
    }

    public static bool Suspenso => suspenso > 0;

    public static SqliteConnection Abrir()
    {
        var caminho = Caminho;
        if (caminho.Length == 0)
            throw new InvalidOperationException("Nenhum banco de dados selecionado.");
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = caminho, Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 5 }.ToString());
        c.Open();
        if (estruturaCriadaEm != caminho)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                PRAGMA journal_mode = DELETE; -- sem arquivos auxiliares (-wal/-shm): só o loja_log.db
                CREATE TABLE IF NOT EXISTS log (
                    id          INTEGER PRIMARY KEY,
                    data_hora   TEXT NOT NULL,
                    usuario_id  INTEGER,
                    usuario     TEXT NOT NULL DEFAULT '',
                    maquina     TEXT NOT NULL DEFAULT '',
                    tela        TEXT NOT NULL DEFAULT '',
                    categoria   TEXT NOT NULL,
                    acao        TEXT NOT NULL,
                    detalhe     TEXT NOT NULL DEFAULT '',
                    tabela      TEXT,
                    registro_id INTEGER
                );
                CREATE INDEX IF NOT EXISTS ix_log_data ON log(data_hora);
                CREATE INDEX IF NOT EXISTS ix_log_categoria ON log(categoria, data_hora);
                CREATE INDEX IF NOT EXISTS ix_log_usuario ON log(usuario, data_hora);
                """;
            cmd.ExecuteNonQuery();
            // cadeia de hashes (OWASP A08/A09): bancos de log anteriores ganham as colunas na primeira abertura
            using (var colunas = c.CreateCommand())
            {
                colunas.CommandText = "SELECT COUNT(*) FROM pragma_table_info('log') WHERE name = 'hash'";
                if (Convert.ToInt64(colunas.ExecuteScalar()) == 0)
                {
                    colunas.CommandText = "ALTER TABLE log ADD COLUMN anterior TEXT; ALTER TABLE log ADD COLUMN hash TEXT;";
                    colunas.ExecuteNonQuery();
                }
            }
            estruturaCriadaEm = caminho;
        }
        return c;
    }

    /// <summary>Registra uma ação. Nunca lança exceção.</summary>
    public static void Registrar(string categoria, string acao, string detalhe = "", string? tela = null, string? tabela = null, long? registroId = null)
    {
        if (string.IsNullOrWhiteSpace(Db.Caminho))
            return;
        try
        {
            lock (trava)
            {
                using var c = Abrir();
                using var tx = c.BeginTransaction();
                using var ultimo = c.CreateCommand();
                ultimo.Transaction = tx;
                ultimo.CommandText = "SELECT hash FROM log ORDER BY id DESC LIMIT 1";
                var anterior = ultimo.ExecuteScalar() as string ?? "";
                var registro = new Registro(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), Sessao.UsuarioId,
                    Sessao.Nome ?? "", Environment.MachineName, Limitar(tela ?? TelaAtual, 120), categoria, Limitar(acao, 300),
                    Limitar(detalhe, 4000), tabela, registroId);
                using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO log(data_hora, usuario_id, usuario, maquina, tela, categoria, acao, detalhe, tabela, registro_id, anterior, hash)
                    VALUES(@d, @uid, @u, @m, @t, @c, @a, @det, @tab, @reg, @ant, @hash)
                    """;
                cmd.Parameters.AddWithValue("@d", registro.DataHora);
                cmd.Parameters.AddWithValue("@uid", (object?)registro.UsuarioId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@u", registro.Usuario);
                cmd.Parameters.AddWithValue("@m", registro.Maquina);
                cmd.Parameters.AddWithValue("@t", registro.Tela);
                cmd.Parameters.AddWithValue("@c", registro.Categoria);
                cmd.Parameters.AddWithValue("@a", registro.Acao);
                cmd.Parameters.AddWithValue("@det", registro.Detalhe);
                cmd.Parameters.AddWithValue("@tab", (object?)registro.Tabela ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@reg", (object?)registro.RegistroId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ant", anterior);
                cmd.Parameters.AddWithValue("@hash", Hash(anterior, registro));
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
        }
        catch
        {
            // o log é auxiliar: nunca derruba a operação do usuário
        }
    }

    record Registro(string DataHora, long? UsuarioId, string Usuario, string Maquina, string Tela, string Categoria, string Acao,
        string Detalhe, string? Tabela, long? RegistroId);

    /// <summary>SHA-256 do hash anterior + todos os campos do registro (cadeia: alterar ou apagar uma linha quebra as seguintes).</summary>
    static string Hash(string anterior, Registro r)
    {
        var texto = string.Join("\u001f", anterior, r.DataHora, r.UsuarioId?.ToString(CultureInfo.InvariantCulture) ?? "", r.Usuario, r.Maquina,
            r.Tela, r.Categoria, r.Acao, r.Detalhe, r.Tabela ?? "", r.RegistroId?.ToString(CultureInfo.InvariantCulture) ?? "");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(texto)));
    }

    /// <summary>
    /// Confere a cadeia de hashes do log (OWASP A08/A09). Devolve null se íntegra ou a descrição do primeiro registro
    /// alterado/removido. Registros anteriores à cadeia (sem hash) e o início após a limpeza por retenção são aceitos.
    /// </summary>
    public static string? VerificarIntegridade()
    {
        if (!File.Exists(Caminho))
            return null;
        lock (trava)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT id, data_hora, usuario_id, usuario, maquina, tela, categoria, acao, detalhe, tabela, registro_id, anterior, hash
                FROM log WHERE hash IS NOT NULL ORDER BY id
                """;
            using var r = cmd.ExecuteReader();
            string? hashAnterior = null;
            long? idAnterior = null;
            while (r.Read())
            {
                var id = r.GetInt64(0);
                var registro = new Registro(r.GetString(1), r.IsDBNull(2) ? null : r.GetInt64(2), r.GetString(3), r.GetString(4), r.GetString(5),
                    r.GetString(6), r.GetString(7), r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9), r.IsDBNull(10) ? null : r.GetInt64(10));
                var anterior = r.IsDBNull(11) ? "" : r.GetString(11);
                if (hashAnterior != null && anterior != hashAnterior)
                    return $"A sequência do log foi quebrada entre os registros {idAnterior} e {id} (registro apagado ou alterado fora do sistema).";
                if (Hash(anterior, registro) != r.GetString(12))
                    return $"O registro {id} ({registro.DataHora}) foi alterado fora do sistema.";
                hashAnterior = r.GetString(12);
                idAnterior = id;
            }
        }
        return null;
    }

    static string Limitar(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    static readonly Regex Escrita = new(@"^\s*(?:(INSERT)(?:\s+OR\s+\w+)?\s+INTO|(REPLACE)\s+INTO|(UPDATE)(?:\s+OR\s+\w+)?|(DELETE)\s+FROM)\s+[""\[`]?(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Identifica comandos que alteram dados: (operação, tabela) ou null para consultas e comandos de estrutura.</summary>
    public static (string Operacao, string Tabela)? ClassificarSql(string sql)
    {
        var m = Escrita.Match(sql);
        if (!m.Success)
            return null;
        var op = (m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value + m.Groups[4].Value).ToUpperInvariant();
        if (op == "REPLACE")
            op = "INSERT";
        return (op, m.Groups[5].Value.ToLowerInvariant());
    }

    /// <summary>Apaga registros mais antigos que N dias (0 = manter tudo). Devolve quantos foram apagados.</summary>
    public static int Limpar(int dias)
    {
        if (dias <= 0 || !File.Exists(Caminho))
            return 0;
        try
        {
            lock (trava)
            {
                using var c = Abrir();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "DELETE FROM log WHERE data_hora < @d";
                cmd.Parameters.AddWithValue("@d", DateTime.Now.AddDays(-dias).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                return cmd.ExecuteNonQuery();
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Consulta para a tela de log (filtros opcionais).</summary>
    public static DataTable Consultar(DateTime de, DateTime ate, string? categoria, string? usuario, string? texto, int limite = 5000)
    {
        var dt = new DataTable();
        if (!File.Exists(Caminho))
            return dt;
        lock (trava)
        {
            using var c = Abrir();
            using var cmd = c.CreateCommand();
            var filtros = new List<string> { "data_hora >= @de", "data_hora < @ate" };
            cmd.Parameters.AddWithValue("@de", de.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@ate", ate.Date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(categoria))
            {
                filtros.Add("categoria = @c");
                cmd.Parameters.AddWithValue("@c", categoria);
            }
            if (!string.IsNullOrEmpty(usuario))
            {
                filtros.Add("usuario = @u");
                cmd.Parameters.AddWithValue("@u", usuario);
            }
            if (!string.IsNullOrWhiteSpace(texto))
            {
                filtros.Add(@"(acao LIKE @x ESCAPE '\' OR detalhe LIKE @x ESCAPE '\' OR tela LIKE @x ESCAPE '\' OR tabela LIKE @x ESCAPE '\')");
                cmd.Parameters.AddWithValue("@x", Db.Contem(texto));
            }
            cmd.CommandText = $"""
                SELECT id, substr(data_hora, 9, 2) || '/' || substr(data_hora, 6, 2) || '/' || substr(data_hora, 1, 4) || ' ' || substr(data_hora, 12, 8) AS "Data/hora",
                       usuario AS "Usuário", categoria AS "Categoria", tela AS "Tela", acao AS "Ação", detalhe AS "Detalhe",
                       COALESCE(tabela, '') AS "Tabela", registro_id AS "Registro", maquina AS "Máquina"
                FROM log WHERE {string.Join(" AND ", filtros)}
                ORDER BY id DESC LIMIT {limite}
                """;
            using var r = cmd.ExecuteReader();
            dt.Load(r);
        }
        foreach (DataColumn col in dt.Columns)
            col.ReadOnly = false;
        return dt;
    }

    /// <summary>Chaves das telas abertas mais recentemente pelo usuário (sem repetir), da mais nova para a mais antiga.</summary>
    public static List<string> TelasRecentes(long? usuarioId, int quantidade)
    {
        var lista = new List<string>();
        if (usuarioId is null || !File.Exists(Caminho))
            return lista;
        try
        {
            lock (trava)
            {
                using var c = Abrir();
                using var cmd = c.CreateCommand();
                cmd.CommandText = """
                    SELECT detalhe FROM log
                    WHERE categoria = @c AND usuario_id = @u AND acao LIKE 'Abriu %' AND detalhe <> ''
                    GROUP BY detalhe ORDER BY MAX(id) DESC LIMIT @n
                    """;
                cmd.Parameters.AddWithValue("@c", Tela);
                cmd.Parameters.AddWithValue("@u", usuarioId);
                cmd.Parameters.AddWithValue("@n", quantidade);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(r.GetString(0));
            }
        }
        catch
        {
            // sem recentes: a tela inicial e o localizador funcionam normalmente
        }
        return lista;
    }

    public static string[] Usuarios()
    {
        if (!File.Exists(Caminho))
            return [];
        try
        {
            lock (trava)
            {
                using var c = Abrir();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT usuario FROM log WHERE usuario <> '' ORDER BY usuario";
                using var r = cmd.ExecuteReader();
                var lista = new List<string>();
                while (r.Read())
                    lista.Add(r.GetString(0));
                return [.. lista];
            }
        }
        catch
        {
            return [];
        }
    }
}

/// <summary>Acumula as gravações de uma DbSessao e registra um resumo quando elas se confirmam.</summary>
sealed class RegistroDeGravacoes
{
    readonly Dictionary<(string Op, string Tabela), (int Comandos, int Linhas, long? Id)> itens = new();
    readonly List<(string Op, string Tabela)> ordem = new();

    public void Anotar(string operacao, string tabela, int linhas, long? id)
    {
        var chave = (operacao, tabela);
        if (itens.TryGetValue(chave, out var v))
            itens[chave] = (v.Comandos + 1, v.Linhas + linhas, v.Id);
        else
        {
            itens[chave] = (1, linhas, id);
            ordem.Add(chave);
        }
    }

    public bool Vazio => itens.Count == 0;

    public void Descartar()
    {
        itens.Clear();
        ordem.Clear();
    }

    /// <summary>Uma linha de log por sessão: a primeira tabela gravada dá o nome da ação; o detalhe lista todas.</summary>
    public void Registrar()
    {
        if (Vazio)
            return;
        var principal = ordem[0];
        var (_, _, id) = itens[principal];
        var verbo = principal.Op switch { "INSERT" => "Incluiu", "UPDATE" => "Alterou", _ => "Excluiu" };
        var detalhe = string.Join("; ", ordem.Select(k =>
        {
            var v = itens[k];
            return $"{k.Op} {k.Tabela}: {v.Linhas} linha(s)";
        }));
        Log.Registrar(Log.Dados, $"{verbo} {principal.Tabela}", detalhe, tabela: principal.Tabela, registroId: principal.Op == "INSERT" ? id : null);
        Descartar();
    }
}
