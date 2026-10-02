using System.Text;
using Microsoft.Data.Sqlite;

namespace ErpLoja.Data;

/// <summary>
/// Onde está o banco. Ordem de busca na inicialização:
/// 1) o caminho lembrado da última vez (arquivo %LOCALAPPDATA%\ErpLoja\banco.txt);
/// 2) loja.db na mesma pasta do executável.
/// Se nenhum existir, a tela de abertura pergunta se cria um banco novo ou se o usuário indica o arquivo.
/// </summary>
public static class LocalBanco
{
    public const string NomePadrao = "loja.db";

    /// <summary>Pasta das preferências locais da máquina (substituível nos testes).</summary>
    public static string PastaPreferencias { get; set; } =
        Environment.GetEnvironmentVariable("ERPLOJA_PREFERENCIAS") is { Length: > 0 } pasta
            ? pasta
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErpLoja");

    static string ArquivoLembrado => Path.Combine(PastaPreferencias, "banco.txt");

    public static string AoLadoDoExecutavel => Path.Combine(AppContext.BaseDirectory, NomePadrao);

    public static string? Lembrado()
    {
        try
        {
            return File.Exists(ArquivoLembrado) ? File.ReadAllText(ArquivoLembrado, Encoding.UTF8).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void Lembrar(string caminho)
    {
        Directory.CreateDirectory(PastaPreferencias);
        File.WriteAllText(ArquivoLembrado, Path.GetFullPath(caminho), Encoding.UTF8);
    }

    /// <summary>Caminho do banco encontrado automaticamente, ou null.</summary>
    public static string? Localizar()
    {
        if (Lembrado() is { Length: > 0 } lembrado && File.Exists(lembrado))
            return lembrado;
        return File.Exists(AoLadoDoExecutavel) ? AoLadoDoExecutavel : null;
    }

    /// <summary>Confere se o arquivo é um banco SQLite deste sistema. Devolve a mensagem de erro, ou null se ok.</summary>
    public static string? Validar(string caminho)
    {
        if (!File.Exists(caminho))
            return "O arquivo não existe.";
        try
        {
            var cabecalho = new byte[16];
            using (var f = File.OpenRead(caminho))
                if (f.Read(cabecalho, 0, 16) < 16 || Encoding.ASCII.GetString(cabecalho, 0, 15) != "SQLite format 3")
                    return "O arquivo não é um banco de dados SQLite.";

            using var c = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = caminho,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('usuarios', '__migracoes')";
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0 ? null : "O arquivo é um SQLite, mas não é um banco do ERP Loja.";
        }
        catch (Exception ex)
        {
            return "Não foi possível abrir o arquivo: " + ex.Message;
        }
    }

    /// <summary>Seleciona o banco e aplica as migrações pendentes.</summary>
    public static (List<Migracao> Aplicadas, string? Backup) Abrir(string caminho, bool criar = false)
    {
        Db.Caminho = Path.GetFullPath(caminho);
        var resultado = Migracoes.Atualizar(criar);
        Lembrar(Db.Caminho);
        return resultado;
    }

    /// <summary>Cria um banco novo, zerado (com o usuário admin), no caminho indicado.</summary>
    public static void CriarNovo(string caminho)
    {
        if (File.Exists(caminho))
            throw new InvalidOperationException("Já existe um arquivo nesse caminho.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho))!);
        Abrir(caminho, criar: true);
    }

    /// <summary>Testa se dá para gravar na pasta (ex.: Arquivos de Programas costuma ser protegida).</summary>
    public static bool PastaGravavel(string pasta)
    {
        try
        {
            var teste = Path.Combine(pasta, $".erp_teste_{Guid.NewGuid():N}");
            File.WriteAllText(teste, "");
            File.Delete(teste);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Cópias de segurança do banco, na pasta "backups" ao lado do arquivo .db.</summary>
public static class Backups
{
    public static string Pasta => Path.Combine(Path.GetDirectoryName(Db.Caminho)!, "backups");

    /// <summary>Cria uma cópia consistente do banco (API de backup do SQLite) e devolve o caminho.</summary>
    public static string Criar(string motivo)
    {
        Directory.CreateDirectory(Pasta);
        var nome = $"{Path.GetFileNameWithoutExtension(Db.Caminho)}_{DateTime.Now:yyyyMMdd_HHmmss}_{motivo}.db";
        var destino = Path.Combine(Pasta, nome);
        Db.Backup(destino);
        return destino;
    }

    /// <summary>Backup diário automático (um por dia) mantendo apenas os N mais recentes.</summary>
    public static string? Diario(int manter)
    {
        if (Directory.Exists(Pasta) && Directory.GetFiles(Pasta, $"*_{DateTime.Now:yyyyMMdd}_*_diario.db").Length > 0)
            return null;
        var criado = Criar("diario");
        foreach (var antigo in Directory.GetFiles(Pasta, "*_diario.db").OrderByDescending(f => f).Skip(Math.Max(1, manter)))
            File.Delete(antigo);
        return criado;
    }
}
