using System.Diagnostics;

namespace ErpLoja.Forms;

/// <summary>Tela única de configuração: parâmetros (por grupo), e-mail, banco de dados e minha conta.</summary>
public class ConfiguracoesForm : Form
{
    readonly TabControl abas = new() { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
    readonly Dictionary<string, Control> campos = new();

    /// <summary>Avisado depois de salvar parâmetros (a janela principal atualiza o nome da loja no cabeçalho).</summary>
    public static event Action? ParametrosSalvos;

    public ConfiguracoesForm()
    {
        Ui.Estilizar(this, "Configurações", 900, 680, nameof(ConfiguracoesForm));
        if (Sessao.Admin)
        {
            foreach (var grupo in Parametros.Definicoes.Select(d => d.Grupo).Distinct())
                abas.TabPages.Add(AbaParametros(grupo));
        }
        abas.TabPages.Add(AbaMinhaConta());

        // espaço para o "?" à direita das abas
        var topo = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.White };
        topo.Controls.Add(new Label
        {
            Text = Sessao.Admin ? "Parâmetros do sistema — altere e clique em Salvar em cada aba." : "Somente administradores alteram os parâmetros do sistema.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(12, 14),
        });
        Controls.Add(abas);
        Controls.Add(topo);
    }

    static TabPage NovaAba(string titulo, out PainelCampos painel)
    {
        var aba = new TabPage(titulo) { BackColor = Color.White, AutoScroll = true };
        painel = new PainelCampos { Location = new Point(16, 16) };
        aba.Controls.Add(painel);
        return aba;
    }

    TabPage AbaParametros(string grupo)
    {
        var aba = NovaAba(grupo, out var painel);
        var definicoes = Parametros.Definicoes.Where(d => d.Grupo == grupo).ToList();
        foreach (var d in definicoes)
        {
            var valor = Db.Config(d.Chave);
            Control controle;
            if ((d.Opcoes ?? d.OpcoesDinamicas?.Invoke()) is { } opcoes)
            {
                var cb = Ui.Combo(320);
                cb.Items.AddRange(opcoes.Contains(valor) || valor.Length == 0 ? opcoes : [.. opcoes, valor]);
                cb.SelectedItem = cb.Items.Contains(valor) ? valor : d.Padrao;
                controle = cb;
            }
            else
                controle = new TextBox { Text = valor, UseSystemPasswordChar = d.Secreto };
            painel.Adicionar(d.Rotulo + ":", controle, 320);
            if (d.Dica != null)
                painel.Nota(d.Dica, 600);
            campos[d.Chave] = controle;
        }

        var botoes = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        botoes.Controls.Add(Ui.Botao("Salvar", (_, _) => Salvar(definicoes), primario: true));
        if (grupo == Parametros.Email)
            botoes.Controls.Add(Ui.Botao("Enviar e-mail de teste", async (_, _) => await TestarEmail(definicoes)));
        if (grupo == Parametros.Impressoras)
            botoes.Controls.Add(Ui.Botao("Imprimir página de teste", (_, _) =>
            {
                if (!Salvar(definicoes, silencioso: true))
                    return;
                try { Impressao.ImprimirCupom(Impressao.PaginaDeTeste()); Ui.Feito("Página de teste enviada."); }
                catch (Exception ex) { Ui.Erro("Falha na impressão:\n" + ex.Message); }
            }));
        painel.Linha(botoes);

        if (grupo == Parametros.Banco)
            MontarBanco(painel);
        return aba;
    }

    bool Salvar(List<Parametro> definicoes, bool silencioso = false)
    {
        foreach (var d in definicoes)
        {
            if (Parametros.Validar(d, campos[d.Chave].Text) is { } erro)
            {
                Ui.Erro(erro);
                Focar(campos[d.Chave]);
                return false;
            }
        }
        foreach (var d in definicoes)
            Db.SalvarConfig(d.Chave, campos[d.Chave].Text.Trim());
        ParametrosSalvos?.Invoke();
        if (!silencioso)
            Ui.Feito("Configurações salvas.");
        return true;
    }

    /// <summary>Mostra a aba do campo e põe o cursor nele.</summary>
    void Focar(Control campo)
    {
        for (var p = campo.Parent; p != null; p = p.Parent)
            if (p is TabPage aba)
            {
                abas.SelectedTab = aba;
                break;
            }
        campo.Focus();
    }

    async Task TestarEmail(List<Parametro> definicoes)
    {
        if (!Salvar(definicoes))
            return;
        if (string.IsNullOrWhiteSpace(Parametros.ResendApiKey) || string.IsNullOrWhiteSpace(Parametros.ResendRemetente))
        {
            Ui.Erro("O envio de e-mail não está configurado: preencha a chave da API do Resend e o remetente nesta aba e tente de novo.");
            return;
        }
        var destino = Ui.Txt(Db.Scalar("SELECT email FROM usuarios WHERE id = @p0", Sessao.UsuarioId));
        if (!Email.Valido(destino))
        {
            Ui.Erro("Cadastre o seu e-mail na aba Minha conta para receber o teste.");
            return;
        }
        try
        {
            Cursor = Cursors.WaitCursor;
            await Email.Enviar(destino, "Teste de e-mail - ERP Loja", "<p>Envio de e-mail do ERP Loja configurado corretamente.</p>");
            Ui.Info($"E-mail de teste enviado para {destino}.");
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    void MontarBanco(PainelCampos painel)
    {
        painel.Nota("", 10);
        painel.Adicionar("Arquivo em uso:", new TextBox { Text = Db.Caminho, ReadOnly = true }, 560);
        var aplicadas = Migracoes.Aplicadas();
        painel.Adicionar("Versão do banco:", new Label
        {
            AutoSize = true,
            Text = $"v{aplicadas.Select(a => a.Versao).DefaultIfEmpty(0).Max()} (sistema: v{Migracoes.VersaoDoSistema})",
        });
        var lista = new ListBox { Width = 560, Height = 100, IntegralHeight = false };
        foreach (var (versao, nome, quando) in aplicadas)
            lista.Items.Add($"{versao:000}  {nome}  —  {Db.LerData(quando):dd/MM/yyyy HH:mm}");
        painel.Adicionar("Migrações aplicadas:", lista);

        var acoes = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        acoes.Controls.AddRange([
            Ui.Botao("Fazer backup agora", (_, _) =>
            {
                try { Ui.Info("Backup salvo em:\n" + Backups.Criar("manual")); }
                catch (Exception ex) { Ui.Erro("Não foi possível fazer o backup:\n" + ex.Message); }
            }),
            Ui.Botao("Abrir pasta de backups", (_, _) =>
            {
                Directory.CreateDirectory(Backups.Pasta);
                Process.Start(new ProcessStartInfo("explorer.exe", Backups.Pasta) { UseShellExecute = true });
            }),
            Ui.Botao("Trocar de banco...", (_, _) => TrocarBanco()),
            Ui.Botao("Gerar dados de demonstração", (_, _) =>
            {
                if (!Ui.Confirmar("Inserir dados fictícios de demonstração (clientes, produtos, vendas de 6 meses, compras, produção, fretes, boletos...)?\n\n" +
                                  "Use apenas em banco de teste. Um backup é feito antes."))
                    return;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    var backup = Backups.Criar("antes_dados_demo");
                    var resumo = DadosDemo.Gerar();
                    Ui.Info(resumo + "\n\nBackup anterior: " + backup);
                }
                catch (Exception ex) { Ui.Erro(ex.Message); }
                finally { Cursor = Cursors.Default; }
            }),
        ]);
        painel.Linha(acoes);
        painel.Nota("Para restaurar um backup: feche o sistema, copie o arquivo de backup desejado por cima do banco (ou use \"Trocar de banco\" apontando para ele).", 600);
    }

    void TrocarBanco()
    {
        using var dlg = new OpenFileDialog { Title = "Selecione o banco do ERP", Filter = "Banco do ERP (*.db)|*.db" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        if (LocalBanco.Validar(dlg.FileName) is string erro)
        {
            Ui.Erro(erro);
            return;
        }
        if (!Ui.Confirmar($"Usar o banco\n{dlg.FileName}\n\nO sistema será reiniciado."))
            return;
        LocalBanco.Lembrar(dlg.FileName);
        Application.Restart();
        Environment.Exit(0);
    }

    TabPage AbaMinhaConta()
    {
        var aba = NovaAba("Minha conta", out var painel);
        var u = Db.Query("SELECT username, email FROM usuarios WHERE id = @p0", Sessao.UsuarioId).Rows[0];
        painel.Adicionar("Username:", new TextBox { Text = Ui.Txt(u["username"]), ReadOnly = true });
        var email = painel.Adicionar("E-mail:", new TextBox { Text = Ui.Txt(u["email"]) });
        painel.Nota("Usado para receber o código de recuperação de senha.");
        painel.Linha(Ui.Botao("Salvar e-mail", (_, _) =>
        {
            if (!Email.Valido(email.Text.Trim()))
            {
                Ui.Erro("Informe um e-mail válido.");
                return;
            }
            try
            {
                Db.Exec("UPDATE usuarios SET email = @p0 WHERE id = @p1", email.Text.Trim(), Sessao.UsuarioId);
                Ui.Info("E-mail salvo.");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Ui.Erro("Este e-mail já está em uso por outro usuário.");
            }
        }, primario: true));

        painel.Nota("", 10);
        var atual = painel.Adicionar("Senha atual:", new TextBox { UseSystemPasswordChar = true });
        var nova = painel.Adicionar("Nova senha:", new TextBox { UseSystemPasswordChar = true });
        var confirma = painel.Adicionar("Confirmar nova senha:", new TextBox { UseSystemPasswordChar = true });
        painel.Linha(Ui.Botao("Alterar senha", (_, _) =>
        {
            var hash = Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE id = @p0", Sessao.UsuarioId));
            if (!Senhas.Verificar(atual.Text, hash)) { Ui.Erro("Senha atual incorreta."); return; }
            if (nova.Text.Length < 4) { Ui.Erro("A nova senha deve ter pelo menos 4 caracteres."); return; }
            if (nova.Text != confirma.Text) { Ui.Erro("A confirmação não confere."); return; }
            Db.Exec("UPDATE usuarios SET senha = @p0 WHERE id = @p1", Senhas.Hash(nova.Text), Sessao.UsuarioId);
            Log.Registrar(Log.Acesso, "Senha alterada pelo próprio usuário", "Configurações > Minha conta");
            atual.Clear(); nova.Clear(); confirma.Clear();
            Ui.Info("Senha alterada.");
        }, primario: true));
        return aba;
    }
}
