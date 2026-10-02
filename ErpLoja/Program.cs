using System.Globalization;

namespace ErpLoja;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        var br = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = br;
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = br;
        ApplicationConfiguration.Initialize();

        // ErpLoja.exe --criar-banco [caminho]  → cria/atualiza o banco e sai (sem interface)
        var i = Array.FindIndex(args, a => a.Equals("--criar-banco", StringComparison.OrdinalIgnoreCase));
        if (i >= 0)
        {
            var caminho = Path.GetFullPath(i + 1 < args.Length ? args[i + 1] : LocalBanco.AoLadoDoExecutavel);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
                Db.Caminho = caminho;
                Migracoes.Atualizar(criar: true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Ui.Erro("Ocorreu um erro inesperado:\n" + e.Exception.Message);

        if (!AbrirBanco())
            return 1;
        if (!LoginForm.Executar())
            return 0;
        Application.Run(new MainForm());
        return 0;
    }

    /// <summary>Localiza o banco automaticamente; se não achar, pergunta (criar novo / indicar / sair). Aplica as migrações.</summary>
    static bool AbrirBanco()
    {
        var caminho = LocalBanco.Localizar();
        while (true)
        {
            if (caminho is null)
            {
                using var f = new BancoForm();
                if (f.ShowDialog() != DialogResult.OK)
                    return false;
                caminho = f.Caminho!;
                if (f.CriadoAgora)
                    return true; // já criado e migrado pela tela
            }
            try
            {
                var (aplicadas, backup) = LocalBanco.Abrir(caminho);
                if (aplicadas.Count > 0 && backup != null)
                    Ui.Info($"O banco de dados foi atualizado para a versão {Migracoes.VersaoDoSistema}:\n" +
                            string.Join("\n", aplicadas.Select(m => $"• {m.Versao} - {m.Nome}")) +
                            $"\n\nBackup feito antes da atualização:\n{backup}");
                if (Parametros.BackupAoIniciar)
                {
                    try { Backups.Diario(Parametros.BackupsManter); }
                    catch (Exception ex) { Ui.Erro("Não foi possível fazer o backup diário automático:\n" + ex.Message); }
                }
                return true;
            }
            catch (Exception ex)
            {
                Ui.Erro($"Não foi possível abrir o banco de dados:\n{caminho}\n\n{ex.Message}");
                caminho = null; // volta para a tela de escolha
            }
        }
    }
}
