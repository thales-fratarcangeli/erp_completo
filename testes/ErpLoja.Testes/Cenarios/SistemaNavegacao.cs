using System.Reflection;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários de Sistema/Gestão: janela principal e cliques em rótulos.</summary>
static class SistemaUtil
{
    public static MainForm AbrirPrincipal()
    {
        var principal = new MainForm { WindowState = FormWindowState.Normal, StartPosition = FormStartPosition.Manual, Location = new Point(0, 0), Size = new Size(1400, 860) };
        principal.Show();
        Usuario.Esperar(300);
        return principal;
    }

    public static Tela Tela(string chave) => Navegacao.Telas().First(t => t.Chave == chave);

    /// <summary>Clique do mouse num controle qualquer (ex.: o "×" de uma aba, que é um Label).</summary>
    public static void ClicarControle(Control c)
    {
        typeof(Control).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(c, [EventArgs.Empty]);
        Usuario.Esperar();
    }

    public static void Fechar(MainForm principal, Usuario u)
    {
        u.RespostaConfirmacao = true;
        principal.Close();
        Usuario.Esperar();
        if (!principal.IsDisposed)
            principal.Dispose();
        Navegacao.Abridor = null;
    }

    public static string Erros(Usuario u) => string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => " | " + m.Texto));
}

/// <summary>O dono abre várias telas, alterna com Ctrl+Tab, fecha com Ctrl+W e pelo ×, reabre uma tela já aberta.</summary>
public class SistemaDonoAbreMuitasTelas : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        var principal = SistemaUtil.AbrirPrincipal();
        u.Ok(principal.TelaAtiva is null && principal.TelasAbertas.Count == 0, "a janela principal começa na tela inicial, sem abas");

        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelaAtiva is DashboardForm, "F5 abre o Painel gerencial embutido");
        u.Teclar(principal, Keys.F8);
        u.Ok(principal.TelaAtiva is RelatoriosForm, "F8 abre Relatórios");
        u.Teclar(principal, Keys.F6);
        u.Ok(principal.TelaAtiva is EstoqueForm, "F6 abre Estoque");
        Navegacao.Abridor!(SistemaUtil.Tela("ConfiguracoesForm"));
        Navegacao.Abridor!(SistemaUtil.Tela("LogForm"));
        Navegacao.Abridor!(SistemaUtil.Tela("cadastro_produtos"));
        Usuario.Esperar(300);
        u.Ok(principal.TelasAbertas.Count == 6, $"seis telas abertas ao mesmo tempo (abertas: {principal.TelasAbertas.Count})");
        u.Ok(principal.TelasAbertas.Count(t => t.Visible) == 1 && principal.TelaAtiva!.Visible, "só a tela ativa fica visível");
        u.Ok(principal.Text.Contains(principal.TelaAtiva!.Text), "o título da janela mostra a tela ativa");

        // reabrir uma tela que já está aberta: não duplica, só traz para frente
        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelasAbertas.Count == 6 && principal.TelaAtiva is DashboardForm, "F5 de novo traz o painel já aberto (sem abrir outro)");

        // Ctrl+Tab percorre as abas na ordem, Ctrl+Shift+Tab volta
        var ordem = principal.TelasAbertas;
        u.Teclar(principal, Keys.Control | Keys.Tab);
        u.Ok(principal.TelaAtiva == ordem[1], "Ctrl+Tab vai para a próxima aba");
        u.Teclar(principal, Keys.Control | Keys.Shift | Keys.Tab);
        u.Teclar(principal, Keys.Control | Keys.Shift | Keys.Tab);
        u.Ok(principal.TelaAtiva == ordem[^1], "Ctrl+Shift+Tab na primeira aba vai para a última");

        // Ctrl+W fecha a ativa e ativa outra
        var fechada = principal.TelaAtiva!;
        u.Teclar(principal, Keys.Control | Keys.W);
        u.Ok(fechada.IsDisposed && principal.TelasAbertas.Count == 5 && principal.TelaAtiva is not null && principal.TelaAtiva != fechada,
            "Ctrl+W fecha a tela ativa e mostra outra aba");

        // × da aba
        var abaAtiva = principal.TelaAtiva!;
        var fechar = Usuario.Todos(principal).OfType<Label>().Where(l => l.Text == "×" && l.Visible)
            .FirstOrDefault(l => l.Parent!.Controls.OfType<Label>().Any(t => t.Text != "×" && abaAtiva.Text.StartsWith(t.Text.TrimEnd('…'))));
        u.Ok(fechar is not null, "a aba da tela ativa tem o botão ×");
        if (fechar is not null)
        {
            SistemaUtil.ClicarControle(fechar);
            u.Ok(abaAtiva.IsDisposed && principal.TelasAbertas.Count == 4, "clicar no × fecha a tela da aba");
        }

        // fecha tudo com Ctrl+W: volta à tela inicial
        for (var i = 0; i < 10 && principal.TelaAtiva is not null; i++)
            u.Teclar(principal, Keys.Control | Keys.W);
        u.Ok(principal.TelasAbertas.Count == 0 && principal.TelaAtiva is null, "fechando todas as abas volta à tela inicial");
        u.Teclar(principal, Keys.Control | Keys.W);
        u.Ok(principal.TelasAbertas.Count == 0, "Ctrl+W sem tela aberta não faz nada");

        // reabrir depois de fechar cria uma tela nova (a antiga foi descartada)
        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelaAtiva is DashboardForm { IsDisposed: false }, "reabrir uma tela depois de fechá-la funciona");
        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhuma mensagem de erro" + SistemaUtil.Erros(u));
        SistemaUtil.Fechar(principal, u);
    }
}

/// <summary>Fechar a janela principal com uma venda em andamento no PDV pergunta antes; "Não" mantém tudo aberto.</summary>
public class SistemaFecharComVendaNoPdv : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        var codigo = Ui.Txt(Db.Scalar("SELECT codigo FROM produtos WHERE ativo = 1 AND tipo <> 'INSUMO' AND estoque > 10 ORDER BY id LIMIT 1"));
        var principal = SistemaUtil.AbrirPrincipal();
        u.Teclar(principal, Keys.F8);
        u.Teclar(principal, Keys.F2);
        var pdv = (PdvForm)principal.TelaAtiva!;
        var campo = Usuario.Achar<TextBox>(pdv, t => t.PlaceholderText.Contains("código"));
        u.Digitar(campo, codigo, enter: true);

        // tecla F5 com o cursor no PDV embutido: é o desconto do PDV, não o atalho do painel
        u.QuandoAbrirJanela("Desconto na venda", d => d.DialogResult = DialogResult.Cancel);
        campo.Focus();
        var msg = Message.Create(campo.Handle, 0x0100, (IntPtr)(int)Keys.F5, IntPtr.Zero);
        campo.PreProcessMessage(ref msg);
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(principal.TelaAtiva == pdv && principal.TelasAbertas.All(t => t is not DashboardForm), "no PDV embutido F5 abre o desconto, não o painel");

        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelaAtiva is DashboardForm, "com o PDV aberto, F5 fora do PDV abre o painel");

        u.RespostaConfirmacao = false;
        principal.Close();
        Usuario.Esperar(300);
        u.Ok(!principal.IsDisposed && principal.Visible, "responder Não à venda em andamento mantém a janela principal aberta");
        u.Ok(u.Viu("CONFIRMAR", "venda em andamento"), "o sistema avisa que há uma venda em andamento");
        u.Ok(principal.TelaAtiva == pdv && !pdv.IsDisposed, "o PDV volta para frente para o usuário concluir a venda");

        // no PDV, F5 é desconto (não abre o painel)
        u.Ok(principal.TelasAbertas.Count >= 2, "as outras telas continuam abertas depois de desistir de fechar" + $" ({principal.TelasAbertas.Count})");

        u.RespostaConfirmacao = true;
        principal.Close();
        Usuario.Esperar(300);
        u.Ok(principal.IsDisposed || !principal.Visible, "respondendo Sim a janela principal fecha");
        Navegacao.Abridor = null;
    }
}

/// <summary>Funcionário (usuário comum) não vê telas de administrador no menu, no localizador nem nos atalhos.</summary>
public class SistemaUsuarioComumNaoVeTelasDeAdmin : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        Sessao.Admin = false;
        Sessao.Nome = "Funcionário Novo";
        try
        {
            var principal = SistemaUtil.AbrirPrincipal();
            var textos = Usuario.Todos(principal).OfType<Button>().Select(b => b.Text).ToList();
            foreach (var t in Navegacao.Telas().Where(t => t.SoAdmin))
                u.Ok(!textos.Any(x => x.Trim().StartsWith(t.Titulo)), $"menu do usuário comum não mostra '{t.Titulo}'");
            u.Ok(textos.Any(x => x.Trim().StartsWith("PDV")), "menu do usuário comum mostra o PDV");
            u.Ok(Navegacao.Buscar("log", admin: false).All(r => !r.Tela.SoAdmin) && Navegacao.Buscar("usuário senha", admin: false).All(r => !r.Tela.SoAdmin),
                "o localizador não oferece telas de administrador ao usuário comum");

            // mesmo que alguém chame a navegação direto, a tela de administrador não abre
            Navegacao.Abridor!(SistemaUtil.Tela("LogForm"));
            Usuario.Esperar();
            u.Ok(principal.TelasAbertas.All(t => t is not LogForm), "usuário comum não consegue abrir o Log de atividades");
            u.Ok(u.Viu("ERRO", "exclusiva de administradores"), "e recebe a explicação de que a tela é só de administradores");
            u.Mensagens.Clear();

            // Configurações: só Minha conta
            Navegacao.Abridor!(SistemaUtil.Tela("ConfiguracoesForm"));
            var config = principal.TelaAtiva!;
            var abas = Usuario.Achar<TabControl>(config).TabPages.Cast<TabPage>().Select(p => p.Text).ToList();
            u.Ok(abas.SequenceEqual(["Minha conta"]), "Configurações do usuário comum tem só a aba Minha conta: " + string.Join(", ", abas));
            SistemaUtil.Fechar(principal, u);
        }
        finally
        {
            Sessao.Admin = true;
            Sessao.Nome = "Administrador";
        }
    }
}

/// <summary>Funcionário novo procura telas pelo localizador (Ctrl+K), com e sem acento, e uma palavra que não existe.</summary>
public class SistemaFuncionarioProcuraTela : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        u.Ok(Navegacao.Buscar("configuracoes", true).FirstOrDefault().Tela?.Chave == "ConfiguracoesForm", "busca sem acento ('configuracoes') acha Configurações");
        u.Ok(Navegacao.Buscar("RELATÓRIO", true).FirstOrDefault().Tela?.Chave == "RelatoriosForm", "busca com acento e maiúsculas acha Relatórios");
        u.Ok(Navegacao.Buscar("boleto", true).Any(r => r.Tela.Chave == "CobrancaForm"), "busca por 'boleto' acha a cobrança");
        u.Ok(Navegacao.Buscar("xyzqwk", true).Count == 0, "palavra inexistente não acha nada");
        u.Ok(Navegacao.Buscar("   ", true).Count == Navegacao.Telas().Count, "busca só com espaços lista todas as telas");
        foreach (var termo in new[] { "senha", "pix", "nota fiscal", "ç", "ã", "a e i", "()", "%", "'" })
        {
            try { Navegacao.Buscar(termo, true); u.Ok(true, $"buscar '{termo}' não dá erro"); }
            catch (Exception ex) { u.Ok(false, $"buscar '{termo}' deu erro: {ex.Message}"); }
        }

        var principal = SistemaUtil.AbrirPrincipal();
        u.QuandoAbrir<LocalizarTelaForm>(f =>
        {
            var busca = Usuario.Achar<TextBox>(f);
            var todas = Usuario.Achar<ListBox>(f).Items.Cast<string>().ToList();
            u.Ok(todas.Count > 0 && !todas.Contains("Localizar tela"), "o localizador lista as telas, sem listar a si mesmo");
            u.Digitar(busca, "estoque minimo");
            var lista = Usuario.Achar<ListBox>(f);
            u.Ok(lista.Items.Count > 0 && lista.SelectedIndex == 0, "o localizador lista resultados e seleciona o primeiro");
            u.Teclar(f, Keys.Enter);
        });
        u.Teclar(principal, Keys.Control | Keys.K);
        Usuario.Esperar(400);
        u.ConferirJanelas();
        u.Ok(principal.TelaAtiva is EstoqueForm, "Ctrl+K, 'estoque minimo' e Enter abre a tela de Estoque");

        // sem resultado: Enter não faz nada e o rodapé explica
        u.QuandoAbrir<LocalizarTelaForm>(f =>
        {
            u.Digitar(Usuario.Achar<TextBox>(f), "xyzqwk");
            u.Ok(Usuario.Todos(f).OfType<Label>().Any(l => l.Text.Contains("Nenhuma tela encontrada")), "sem resultado o localizador diz 'Nenhuma tela encontrada'");
            u.Teclar(f, Keys.Enter);
            u.Ok(f.Visible, "Enter sem resultado não fecha o localizador");
            u.Teclar(f, Keys.Escape);
        });
        u.Teclar(principal, Keys.Control | Keys.K);
        Usuario.Esperar(400);
        u.ConferirJanelas();
        u.Ok(principal.TelasAbertas.Count == 1, "Esc no localizador não abre nenhuma tela");
        SistemaUtil.Fechar(principal, u);
    }
}

/// <summary>Toda tela do menu tem ajuda; o "?" de uma tela embutida abre a ajuda dela.</summary>
public class SistemaAjudaEmTodasAsTelas : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        foreach (var t in Navegacao.Telas())
            u.Ok(Ajuda.Existe(t.Chave), $"a tela '{t.Titulo}' tem tópico de ajuda");

        var principal = SistemaUtil.AbrirPrincipal();
        u.Teclar(principal, Keys.F8);
        var relatorios = principal.TelaAtiva!;
        var interrogacoes = Usuario.Todos(principal).OfType<Button>().Where(b => b.Text == "?" && b.Visible).ToList();
        u.Ok(interrogacoes.Count == 1 && relatorios.Contains(interrogacoes[0]), $"com uma tela aberta só aparece o '?' dela (visíveis: {interrogacoes.Count})");
        u.Ok(interrogacoes.Count > 0 && interrogacoes[0].Right <= relatorios.ClientSize.Width && interrogacoes[0].Left > relatorios.ClientSize.Width / 2,
            "o '?' fica no canto direito da tela embutida");
        var abriu = false;
        u.QuandoAbrir<AjudaForm>(f =>
        {
            abriu = Usuario.Todos(f).Any(c => c.Text.Contains("Relatórios") || c.Text.Contains("relatório"));
            f.Close();
        });
        if (interrogacoes.Count > 0)
            interrogacoes[0].PerformClick();
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(abriu, "o '?' de Relatórios abre a ajuda de Relatórios");
        SistemaUtil.Fechar(principal, u);
    }
}
