using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ErpLoja.Data;

/// <summary>Acesso ao banco SQLite. O arquivo é localizado na inicialização (ver LocalBanco).</summary>
public static class Db
{
    /// <summary>Caminho do arquivo .db em uso. Definido por LocalBanco na inicialização.</summary>
    public static string Caminho { get; set; } = "";

    /// <summary>Abre uma conexão. Só cria o arquivo quando criar = true (banco novo); nos demais casos ele precisa existir.</summary>
    public static SqliteConnection NovaConexao(bool criar = false)
    {
        if (string.IsNullOrWhiteSpace(Caminho))
            throw new InvalidOperationException("Nenhum banco de dados selecionado.");
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = Caminho,
            Mode = criar ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
            ForeignKeys = true,
            DefaultTimeout = 10,
        }.ToString();
        var conexao = new SqliteConnection(cs);
        conexao.Open();
        return conexao;
    }

    public static DataTable Query(string sql, params object?[] args)
    {
        using var s = new DbSessao();
        return s.Query(sql, args);
    }

    public static int Exec(string sql, params object?[] args)
    {
        using var s = new DbSessao();
        return s.Exec(sql, args);
    }

    public static object? Scalar(string sql, params object?[] args)
    {
        using var s = new DbSessao();
        return s.Scalar(sql, args);
    }

    /// <summary>Data no formato gravado no banco (yyyy-MM-dd).</summary>
    public static string Dia(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateTime LerData(object? v) =>
        DateTime.Parse(Convert.ToString(v, CultureInfo.InvariantCulture) ?? "", CultureInfo.InvariantCulture);

    internal static object Converter(object? v) => v switch
    {
        null => DBNull.Value,
        decimal d => (double)d,
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        bool b => b ? 1L : 0L,
        _ => v,
    };

    public static string Config(string chave) =>
        Convert.ToString(Scalar("SELECT valor FROM configuracoes WHERE chave = @p0", chave)) ?? "";

    public static void SalvarConfig(string chave, string valor) =>
        Exec("INSERT INTO configuracoes(chave, valor) VALUES(@p0, @p1) ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor",
            chave, valor);

    public static void Backup(string destino)
    {
        if (File.Exists(destino))
            File.Delete(destino);
        using var origem = NovaConexao();
        using var copia = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destino, Pooling = false }.ToString());
        copia.Open();
        origem.BackupDatabase(copia);
    }
}

/// <summary>Uma conexão (opcionalmente com transação). Parâmetros são posicionais: @p0, @p1, ...</summary>
public sealed class DbSessao : IDisposable
{
    readonly SqliteConnection conexao;
    SqliteTransaction? tx;
    readonly RegistroDeGravacoes gravacoes = new();

    /// <param name="semChavesEstrangeiras">Desliga a verificação de FKs nesta conexão (só migrações que reconstroem tabelas).</param>
    public DbSessao(bool transacao = false, bool criar = false, bool semChavesEstrangeiras = false)
    {
        conexao = Db.NovaConexao(criar);
        if (semChavesEstrangeiras)
        {
            using var cmd = conexao.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = OFF";
            cmd.ExecuteNonQuery();
        }
        if (transacao)
            tx = conexao.BeginTransaction();
    }

    SqliteCommand Comando(string sql, object?[] args)
    {
        var cmd = conexao.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        for (int i = 0; i < args.Length; i++)
            cmd.Parameters.AddWithValue("@p" + i, Db.Converter(args[i]));
        return cmd;
    }

    public int Exec(string sql, params object?[] args)
    {
        using var cmd = Comando(sql, args);
        var linhas = cmd.ExecuteNonQuery();
        if (!Log.Suspenso && Log.ClassificarSql(sql) is { } escrita && linhas > 0)
        {
            long? id = null;
            if (escrita.Operacao == "INSERT")
            {
                using var ultimo = Comando("SELECT last_insert_rowid()", []);
                id = ultimo.ExecuteScalar() as long?;
            }
            gravacoes.Anotar(escrita.Operacao, escrita.Tabela, linhas, id);
            if (tx is null)
                gravacoes.Registrar(); // sem transação: a gravação já está confirmada
        }
        return linhas;
    }

    public object? Scalar(string sql, params object?[] args)
    {
        using var cmd = Comando(sql, args);
        var v = cmd.ExecuteScalar();
        return v is DBNull ? null : v;
    }

    /// <summary>Executa um INSERT e devolve o id gerado.</summary>
    public long Inserir(string sql, params object?[] args)
    {
        Exec(sql, args);
        return (long)Scalar("SELECT last_insert_rowid()")!;
    }

    /// <summary>
    /// Carrega o resultado num DataTable. O SQLite tem tipagem dinâmica, então o tipo de cada coluna
    /// é deduzido dos valores (inteiro, decimal ou texto) para que a grade formate e ordene corretamente.
    /// </summary>
    public DataTable Query(string sql, params object?[] args)
    {
        using var cmd = Comando(sql, args);
        using var r = cmd.ExecuteReader();

        int n = r.FieldCount;
        var nomes = new string[n];
        var declarados = new string[n];
        for (int i = 0; i < n; i++)
        {
            nomes[i] = r.GetName(i);
            try { declarados[i] = r.GetDataTypeName(i).ToUpperInvariant(); }
            catch { declarados[i] = ""; }
        }

        var linhas = new List<object?[]>();
        while (r.Read())
        {
            var valores = new object?[n];
            for (int i = 0; i < n; i++)
                valores[i] = r.IsDBNull(i) ? null : r.GetValue(i);
            linhas.Add(valores);
        }

        var dt = new DataTable();
        for (int i = 0; i < n; i++)
        {
            var naoNulos = linhas.Select(l => l[i]).Where(v => v != null).ToList();
            Type tipo;
            if (naoNulos.Count == 0)
                tipo = declarados[i] switch { "INTEGER" => typeof(long), "REAL" => typeof(double), _ => typeof(string) };
            else if (naoNulos.All(v => v is long))
                tipo = typeof(long);
            else if (naoNulos.All(v => v is long or double))
                tipo = typeof(double);
            else
                tipo = typeof(string);

            var nome = nomes[i];
            for (int k = 2; dt.Columns.Contains(nome); k++)
                nome = $"{nomes[i]}{k}";
            dt.Columns.Add(nome, tipo);
        }

        dt.BeginLoadData();
        foreach (var l in linhas)
        {
            var valores = new object[n];
            for (int i = 0; i < n; i++)
            {
                var tipo = dt.Columns[i].DataType;
                valores[i] = l[i] is null ? DBNull.Value
                    : tipo == typeof(double) ? Convert.ToDouble(l[i])
                    : tipo == typeof(string) ? Convert.ToString(l[i], CultureInfo.InvariantCulture)!
                    : l[i]!;
            }
            dt.Rows.Add(valores);
        }
        dt.EndLoadData();
        return dt;
    }

    public void Commit()
    {
        tx?.Commit();
        tx?.Dispose();
        tx = null;
        gravacoes.Registrar();
    }

    public void Dispose()
    {
        tx?.Dispose(); // sem Commit = rollback (as gravações anotadas não aconteceram)
        gravacoes.Descartar();
        conexao.Dispose();
    }
}
