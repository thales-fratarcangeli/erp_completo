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

        // ErpLoja.exe --dados-demo <caminho>  → aplica as migrações e insere dados fictícios de demonstração
        var demo = Array.FindIndex(args, a => a.Equals("--dados-demo", StringComparison.OrdinalIgnoreCase));
        if (demo >= 0 && demo + 1 < args.Length)
        {
            try
            {
                Db.Caminho = Path.GetFullPath(args[demo + 1]);
                Migracoes.Atualizar(criar: true);
                Sessao.UsuarioId = 1;
                Sessao.Nome = "Administrador";
                Sessao.Admin = true;
                Backups.Criar("antes_dados_demo");
                Console.WriteLine(DadosDemo.Gerar());
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        // OWASP A10: falha inesperada vira mensagem amigável (sem detalhes internos) e o detalhe técnico vai para o log
        Application.ThreadException += (_, e) => TratarErroInesperado(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Registrar(Log.Erro, "Erro fatal: " + e.ExceptionObject.GetType().Name, e.ExceptionObject.ToString() ?? "");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Registrar(Log.Erro, "Erro em tarefa em segundo plano: " + e.Exception.GetType().Name, e.Exception.ToString());
            e.SetObserved();
        };

        if (!AbrirBanco())
            return 1;
        Log.Registrar(Log.Sistema, "Sistema iniciado", $"versão {Application.ProductVersion.Split('+')[0]} · banco {Db.Caminho}", tela: "");
        Log.Limpar(Parametros.LogDiasManter);
        AtalhoAreaDeTrabalho.CriarNaPrimeiraExecucao();
        if (!LoginForm.Executar())
        {
            Log.Registrar(Log.Sistema, "Sistema encerrado sem acesso", tela: "");
            return 0;
        }
        Application.Run(new MainForm());
        Log.Registrar(Log.Acesso, "Saiu do sistema", tela: "");
        return 0;
    }

    /// <summary>
    /// Erros de regra (mensagem escrita para o usuário) e de permissão aparecem como estão; os demais aparecem como
    /// mensagem genérica com a hora, para o suporte achar o detalhe técnico no log (sem expor SQL, caminhos ou pilha).
    /// </summary>
    static void TratarErroInesperado(Exception ex)
    {
        Log.Registrar(Log.Erro, "Erro inesperado: " + ex.GetType().Name, ex.ToString());
        if (ex is InvalidOperationException or UnauthorizedAccessException)
            Ui.Erro(ex.Message);
        else
            Ui.Erro($"Ocorreu um erro inesperado e a operação não foi concluída.\nO detalhe técnico foi registrado no Log de atividades ({DateTime.Now:dd/MM/yyyy HH:mm:ss}).");
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
                // OWASP A08: confere a integridade do arquivo do banco a cada abertura
                if (Convert.ToString(Db.Scalar("PRAGMA quick_check")) is var verificacao && verificacao != "ok")
                {
                    Log.Registrar(Log.Erro, "Banco de dados com problema de integridade", verificacao ?? "");
                    Ui.Erro("O arquivo do banco de dados apresentou problema de integridade (pode estar danificado).\n" +
                            "Feche o sistema e restaure o backup mais recente da pasta backups. Detalhe: " + verificacao);
                }
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
