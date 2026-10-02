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

/// <summary>
/// O dono navega pelo menu e pelos atalhos: uma tela por vez à direita (sem abas), cabeçalho "Área › Tela", opções da
/// tela numa lista à direita, × e Ctrl+W voltam ao início, e dados digitados não se perdem sem perguntar.
/// </summary>
public class SistemaDonoAbreMuitasTelas : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        var principal = SistemaUtil.AbrirPrincipal();
        u.Ok(principal.TelaAtiva is null && principal.TelasAbertas.Count == 0 && principal.CaminhoNoCabecalho == "Início",
            "a janela principal começa na tela inicial");

        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelaAtiva is DashboardForm, "F5 abre o Painel gerencial à direita");
        var painel = principal.TelaAtiva!;
        u.Teclar(principal, Keys.F8);
        u.Ok(principal.TelaAtiva is RelatoriosForm && painel.IsDisposed && principal.TelasAbertas.Count == 1,
            "F8 abre Relatórios no lugar do painel (uma tela por vez, sem abas)");
        u.Teclar(principal, Keys.F6);
        u.Ok(principal.TelaAtiva is EstoqueForm && principal.CaminhoNoCabecalho.StartsWith("Estoque") && principal.Text.Contains(principal.TelaAtiva!.Text),
            "F6 abre Estoque; o cabeçalho mostra a área e a tela: " + principal.CaminhoNoCabecalho);

        // modelo padrão: listagem ao meio e as opções da tela numa lista com rolagem à direita
        var opcoes = ModeloDeTela.PainelDeOpcoes(principal.TelaAtiva!);
        u.Ok(opcoes is { Dock: DockStyle.Right, AutoScroll: true } && opcoes.Controls.OfType<Button>().Count() >= 3
             && opcoes.Controls.OfType<Button>().All(b => b.Width == opcoes.Controls.OfType<Button>().First().Width),
            "as opções da tela ficam numa lista vertical com barra de rolagem à direita");
        u.Ok(Usuario.Todos(principal.TelaAtiva!).OfType<DataGridView>().Any(g => g.Dock == DockStyle.Fill || g.Parent is SplitterPanel),
            "a listagem fica no meio");

        // reabrir a mesma tela não cria outra
        var estoque = principal.TelaAtiva;
        u.Teclar(principal, Keys.F6);
        u.Ok(principal.TelaAtiva == estoque, "abrir de novo a tela que está aberta não cria outra");

        // Ctrl+W e × fecham a tela e voltam ao início
        u.Teclar(principal, Keys.Control | Keys.W);
        u.Ok(principal.TelaAtiva is null && estoque!.IsDisposed && principal.CaminhoNoCabecalho == "Início", "Ctrl+W fecha a tela e volta ao início");
        u.Teclar(principal, Keys.Control | Keys.W);
        u.Ok(principal.TelaAtiva is null, "Ctrl+W sem tela aberta não faz nada");
        Navegacao.Abridor!(SistemaUtil.Tela("FinanceiroForm"));
        var fechar = Usuario.Todos(principal).OfType<Button>().FirstOrDefault(b => b.Text == "×" && b.Visible);
        u.Ok(fechar is not null, "o cabeçalho tem o × para fechar a tela");
        fechar?.PerformClick();
        Usuario.Esperar();
        u.Ok(principal.TelaAtiva is null, "o × fecha a tela");

        // dados digitados e não salvos: perguntar antes de trocar de tela
        Navegacao.Abridor!(SistemaUtil.Tela("cadastro_categorias"));
        Usuario.Esperar(500); // a pessoa começa a digitar depois que a tela carregou
        var categorias = principal.TelaAtiva!;
        var nome = Usuario.Todos(categorias).OfType<TextBox>().First(c => Validacao.FormatoDe(c) != Formatos.Busca && !c.ReadOnly);
        nome.Focus();
        foreach (var ch in "Bebidas")
            typeof(Control).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(nome, [new KeyPressEventArgs(ch)]);
        nome.Text = "Bebidas";
        u.Ok(Alteracoes.Pendentes(categorias), "digitar num campo marca a tela com alterações não salvas");
        u.Mensagens.Clear();
        u.RespostasConfirmacao.Enqueue(false); // "Continuar editando"
        u.Teclar(principal, Keys.F6);
        u.Ok(principal.TelaAtiva == categorias && nome.Text == "Bebidas" && u.Viu("CONFIRMAR", "ainda não foram salvos"),
            "trocar de tela com dados não salvos pergunta; \"Continuar editando\" mantém tudo"
            + $" [ativa={principal.TelaAtiva?.GetType().Name}, texto={nome.Text}, mensagens={string.Join(" / ", u.Mensagens.Select(m => m.Tipo + ":" + m.Texto[..Math.Min(60, m.Texto.Length)]))}]");
        u.RespostasConfirmacao.Enqueue(true); // "Descartar e sair"
        u.Teclar(principal, Keys.F6);
        u.Ok(principal.TelaAtiva is EstoqueForm && categorias.IsDisposed, "\"Descartar e sair\" desfaz o que foi digitado e abre a outra tela");
        u.Ok(Log.Consultar(DateTime.Today, DateTime.Today, Log.Acao, null, "Alterações descartadas").Rows.Count > 0, "o descarte fica registrado no log");

        // pesquisar não é alteração
        var busca = Usuario.Todos(principal.TelaAtiva!).OfType<TextBox>().First(c => Validacao.FormatoDe(c) == Formatos.Busca);
        busca.Focus();
        typeof(Control).GetMethod("OnKeyPress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(busca, [new KeyPressEventArgs('a')]);
        u.Ok(!Alteracoes.Pendentes(principal.TelaAtiva), "digitar na pesquisa não conta como alteração");

        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhuma mensagem de erro" + SistemaUtil.Erros(u));
        SistemaUtil.Fechar(principal, u);
    }
}

/// <summary>Modo escuro: todas as telas trocam de cor na hora e a escolha fica gravada para o usuário.</summary>
public class SistemaModoEscuro : ICenario
{
    /// <summary>Imagem da janela (só ela, sem a área de trabalho) para conferência visual: %TEMP%\erp_capturas.</summary>
    static void Capturar(Form f, string nome)
    {
        var pasta = Path.Combine(Path.GetTempPath(), "erp_capturas");
        Directory.CreateDirectory(pasta);
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
        bmp.Save(Path.Combine(pasta, nome + ".png"));
    }

    public string Area => "Sistema";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        var principal = SistemaUtil.AbrirPrincipal();
        Navegacao.Abridor!(SistemaUtil.Tela("EstoqueForm"));
        var tela = principal.TelaAtiva!;
        var grade = Usuario.Todos(tela).OfType<DataGridView>().First();
        var fundoClaro = grade.BackgroundColor;
        Capturar(principal, "modelo-claro");
        Preferencias.SalvarTema(true);
        Usuario.Esperar();
        Capturar(principal, "modelo-escuro");
        u.Ok(Tema.Escuro && grade.BackgroundColor.GetBrightness() < 0.3f && tela.BackColor.GetBrightness() < 0.3f,
            "modo escuro aplica cores escuras às telas abertas");
        u.Ok(Convert.ToString(Db.Scalar("SELECT tema FROM usuarios WHERE id = @p0", Sessao.UsuarioId)) == Preferencias.Escuro, "a escolha fica gravada para o usuário");
        Navegacao.Abridor!(SistemaUtil.Tela("FinanceiroForm"));
        Usuario.Esperar(300);
        var nova = principal.TelaAtiva!;
        u.Ok(nova.BackColor.GetBrightness() < 0.3f && Usuario.Todos(nova).OfType<DataGridView>().First().BackgroundColor.GetBrightness() < 0.3f,
            "telas abertas depois já vêm no modo escuro");
        Preferencias.SalvarTema(false);
        Usuario.Esperar();
        u.Ok(!Tema.Escuro && Usuario.Todos(nova).OfType<DataGridView>().First().BackgroundColor == fundoClaro,
            "voltar ao modo claro restaura as cores originais");
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

        // com venda em andamento, abrir outra tela (atalho fora do PDV) pergunta; "Não" mantém o PDV com a venda
        u.RespostaConfirmacao = false;
        u.Teclar(principal, Keys.F5);
        u.Ok(principal.TelaAtiva == pdv && !pdv.IsDisposed && u.Viu("CONFIRMAR", "venda em andamento"),
            "com venda em andamento, abrir outra tela pergunta antes; respondendo Não o PDV continua com a venda");

        principal.Close();
        Usuario.Esperar(300);
        u.Ok(!principal.IsDisposed && principal.Visible, "responder Não à venda em andamento mantém a janela principal aberta");
        u.Ok(principal.TelaAtiva == pdv && !pdv.IsDisposed, "o PDV continua na tela para o usuário concluir a venda");

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
