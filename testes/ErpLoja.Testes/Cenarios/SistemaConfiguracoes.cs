using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Resend falso: guarda os e-mails "enviados" e responde com o status configurado (nada sai para a internet).</summary>
sealed class ResendFalso : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public List<string> Corpos { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Corpos.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(Status)
        {
            Content = new StringContent(Status == HttpStatusCode.OK ? "{\"id\":\"teste\"}" : "{\"message\":\"falha simulada\"}"),
        };
    }

    /// <summary>Código de 6 dígitos do último e-mail de recuperação.</summary>
    public string UltimoCodigo() =>
        Regex.Match(Regex.Unescape(Corpos[^1]), @"letter-spacing:6px.>(\d{6})<").Groups[1].Value;
}

static class ConfigUtil
{
    /// <summary>Seleciona a aba (como o usuário clicando nela) e a devolve: botões de abas escondidas não respondem ao clique.</summary>
    public static TabPage Aba(Form f, string titulo)
    {
        var abas = Usuario.Achar<TabControl>(f);
        var aba = abas.TabPages.Cast<TabPage>().First(p => p.Text == titulo);
        abas.SelectedTab = aba;
        Usuario.Esperar(50);
        return aba;
    }

    /// <summary>Campo pelo rótulo (o PainelCampos põe o rótulo antes do controle).</summary>
    public static T Campo<T>(Control raiz, string rotulo) where T : Control
    {
        var todos = Usuario.Todos(raiz).ToList();
        var i = todos.FindIndex(c => c is Label l && l.Text.StartsWith(rotulo));
        if (i < 0)
            throw new InvalidOperationException($"rótulo '{rotulo}' não encontrado");
        var rot = todos[i];
        var tabela = (TableLayoutPanel)rot.Parent!;
        var linha = tabela.GetRow(rot);
        return tabela.Controls.OfType<T>().First(c => tabela.GetRow(c) == linha && tabela.GetColumn(c) == 1);
    }

    /// <summary>Campo de um parâmetro na tela de Configurações (pela chave do parâmetro).</summary>
    public static T Parametro<T>(Form config, string chave) where T : Control =>
        (T)((Dictionary<string, Control>)typeof(ConfiguracoesForm).GetField("campos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(config)!)[chave];

    public static void Esperar(Func<bool> condicao, int ms = 3000)
    {
        var fim = DateTime.Now.AddMilliseconds(ms);
        while (!condicao() && DateTime.Now < fim)
            Usuario.Esperar(50);
    }
}

/// <summary>Administrador configurando a loja: salva cada aba, tenta valores inválidos, testa impressora/e-mail e faz backup.</summary>
public class SistemaAdminConfiguraLoja : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("impressora_cupom", "Impressora Que Nao Existe ERP");
        var f = u.AbrirTela("ConfiguracoesForm");
        var grupos = Parametros.Definicoes.Select(d => d.Grupo).Distinct().ToList();
        var abas = Usuario.Achar<TabControl>(f).TabPages.Cast<TabPage>().Select(p => p.Text).ToList();
        u.Ok(abas.SequenceEqual([.. grupos, "Minha conta"]), "Configurações do administrador tem uma aba por grupo + Minha conta");

        // salvar cada aba com os valores atuais (válidos) não dá erro
        foreach (var g in grupos)
        {
            var aba = ConfigUtil.Aba(f, g);
            u.Clicar(aba, "Salvar");
            u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), $"salvar a aba '{g}' sem mudar nada não dá erro" + SistemaUtil.Erros(u));
        }

        // valores inválidos são recusados e não são gravados
        var fiscal = ConfigUtil.Aba(f, Parametros.Fiscal);
        var numero = ConfigUtil.Parametro<TextBox>(f, "proximo_numero_55");
        var antes = Db.Config("proximo_numero_55");
        foreach (var invalido in new[] { "abc", "-1", "0", "1,5" })
        {
            u.Mensagens.Clear();
            numero.Text = invalido;
            u.Clicar(ConfigUtil.Aba(f, Parametros.Fiscal), "Salvar");
            u.Ok(u.Viu("ERRO", "Próximo número da NF-e") && Db.Config("proximo_numero_55") == antes, $"próximo número da NF-e '{invalido}' é recusado");
        }
        numero.Text = antes;

        var banco = ConfigUtil.Aba(f, Parametros.Banco);
        var diasLog = ConfigUtil.Parametro<TextBox>(f, "log_dias_manter");
        u.Mensagens.Clear();
        diasLog.Text = "um ano";
        u.Clicar(ConfigUtil.Aba(f, Parametros.Banco), "Salvar");
        u.Ok(u.Viu("ERRO", "Dias de log") && Db.Config("log_dias_manter") != "um ano", "dias de log não numérico é recusado");
        diasLog.Text = "0";
        u.Mensagens.Clear();
        u.Clicar(ConfigUtil.Aba(f, Parametros.Banco), "Salvar");
        u.Ok(!u.Viu("ERRO", "") && Db.Config("log_dias_manter") == "0", "dias de log = 0 (manter tudo) é aceito");

        var reforma = ConfigUtil.Aba(f, Parametros.Reforma);
        var cbs = ConfigUtil.Parametro<TextBox>(f, "aliq_cbs");
        u.Mensagens.Clear();
        cbs.Text = "150";
        u.Clicar(ConfigUtil.Aba(f, Parametros.Reforma), "Salvar");
        u.Ok(u.Viu("ERRO", "entre 0 e 100") && Db.Config("aliq_cbs") != "150", "alíquota de 150% é recusada");
        cbs.Text = "0,90";

        var empresa = ConfigUtil.Aba(f, Parametros.Empresa);
        var nome = ConfigUtil.Parametro<TextBox>(f, "nome_loja");
        var nomeAntes = Db.Config("nome_loja");
        u.Mensagens.Clear();
        nome.Text = "   ";
        u.Clicar(ConfigUtil.Aba(f, Parametros.Empresa), "Salvar");
        u.Ok(u.Viu("ERRO", "Nome") && Db.Config("nome_loja") == nomeAntes, "nome da loja em branco é recusado");
        nome.Text = "Loja do Teste";
        u.Mensagens.Clear();
        u.Clicar(ConfigUtil.Aba(f, Parametros.Empresa), "Salvar");
        u.Ok(Db.Config("nome_loja") == "Loja do Teste", "nome da loja válido é gravado");

        // campos secretos ficam mascarados
        foreach (var d in Parametros.Definicoes.Where(d => d.Secreto))
            u.Ok(ConfigUtil.Parametro<TextBox>(f, d.Chave).UseSystemPasswordChar, $"'{d.Rotulo}' fica mascarado");

        // página de teste numa impressora que não existe: mensagem clara, sem travar
        u.Mensagens.Clear();
        u.Clicar(ConfigUtil.Aba(f, Parametros.Impressoras), "Imprimir página de teste");
        u.Ok(u.Viu("ERRO", "não encontrada"), "página de teste em impressora inexistente avisa que ela não foi encontrada" + string.Concat(u.Mensagens.Select(m => " | " + m.Texto)));

        // e-mail de teste sem chave do Resend: explica o que falta configurar (mesmo sem e-mail do usuário)
        Db.Exec("UPDATE usuarios SET email = NULL WHERE id = @p0", Sessao.UsuarioId);
        u.Mensagens.Clear();
        var email = ConfigUtil.Aba(f, Parametros.Email);
        u.Clicar(ConfigUtil.Aba(f, Parametros.Email), "Enviar e-mail de teste");
        ConfigUtil.Esperar(() => u.Mensagens.Any(m => m.Tipo == "ERRO"));
        u.Ok(u.Viu("ERRO", "não está configurado"), "e-mail de teste sem chave diz que o envio não está configurado" + SistemaUtil.Erros(u));

        // com chave e Resend falso: envia
        var resend = new ResendFalso();
        Email.TransporteParaTestes = resend;
        try
        {
            Db.Exec("UPDATE usuarios SET email = 'dono@loja.com.br' WHERE id = @p0", Sessao.UsuarioId);
            ConfigUtil.Parametro<TextBox>(f, "resend_api_key").Text = "re_teste";
            ConfigUtil.Parametro<TextBox>(f, "resend_remetente").Text = "sem arroba";
            u.Mensagens.Clear();
            u.Clicar(ConfigUtil.Aba(f, Parametros.Email), "Enviar e-mail de teste");
            u.Ok(u.Viu("ERRO", "remetente") && resend.Corpos.Count == 0, "remetente sem e-mail é recusado antes de enviar");
            ConfigUtil.Parametro<TextBox>(f, "resend_remetente").Text = "ERP Loja <nao-responda@loja.com.br>";
            u.Mensagens.Clear();
            u.Clicar(ConfigUtil.Aba(f, Parametros.Email), "Enviar e-mail de teste");
            ConfigUtil.Esperar(() => u.Mensagens.Count > 0);
            u.Ok(u.Viu("INFO", "dono@loja.com.br") && resend.Corpos.Count == 1, "e-mail de teste enviado ao e-mail do usuário" + SistemaUtil.Erros(u));
        }
        finally
        {
            Email.TransporteParaTestes = null;
        }

        // backup agora
        u.Mensagens.Clear();
        u.Clicar(ConfigUtil.Aba(f, Parametros.Banco), "Fazer backup agora");
        var caminho = u.UltimaMensagem("INFO")?.Split('\n').LastOrDefault()?.Trim() ?? "";
        u.Ok(caminho.Length > 0 && File.Exists(caminho), "Fazer backup agora cria o arquivo de backup: " + caminho + string.Concat(u.Mensagens.Select(m => " | " + m.Texto)));
        f.Close();
    }
}

/// <summary>Funcionário comum ajusta a própria conta: e-mail e senha.</summary>
public class SistemaFuncionarioMinhaConta : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo) VALUES('Joana Caixa', 'joana', @p0, 0, 1)", Senhas.Hash("1234"));
        var id = Convert.ToInt64(Db.Scalar("SELECT id FROM usuarios WHERE username = 'joana'"));
        Db.Exec("UPDATE usuarios SET email = 'dono@loja.com.br' WHERE id = 1");
        var (idAntes, nomeAntes) = (Sessao.UsuarioId, Sessao.Nome);
        Sessao.UsuarioId = id;
        Sessao.Nome = "Joana Caixa";
        Sessao.Admin = false;
        try
        {
            var f = u.AbrirTela("ConfiguracoesForm");
            var aba = ConfigUtil.Aba(f, "Minha conta");
            var email = ConfigUtil.Campo<TextBox>(aba, "E-mail");
            u.Digitar(email, "joana@");
            u.Clicar(aba, "Salvar e-mail");
            u.Ok(u.Viu("ERRO", "e-mail válido"), "e-mail incompleto é recusado");
            u.Digitar(email, "DONO@loja.com.br");
            u.Clicar(aba, "Salvar e-mail");
            u.Ok(u.Viu("ERRO", "já está em uso"), "e-mail de outro usuário (mesmo com maiúsculas) é recusado");
            u.Digitar(email, " joana@loja.com.br ");
            u.Clicar(aba, "Salvar e-mail");
            u.Ok(Ui.Txt(Db.Scalar("SELECT email FROM usuarios WHERE id = @p0", id)) == "joana@loja.com.br", "e-mail válido é gravado sem espaços");

            var atual = ConfigUtil.Campo<TextBox>(aba, "Senha atual");
            var nova = ConfigUtil.Campo<TextBox>(aba, "Nova senha");
            var confirma = ConfigUtil.Campo<TextBox>(aba, "Confirmar nova senha");
            u.Mensagens.Clear();
            atual.Text = "errada"; nova.Text = "abcd"; confirma.Text = "abcd";
            u.Clicar(aba, "Alterar senha");
            u.Ok(u.Viu("ERRO", "Senha atual incorreta"), "senha atual errada é recusada");
            atual.Text = "1234"; nova.Text = "Nova!Senha99"; confirma.Text = "Nova!Senha98";
            u.Clicar(aba, "Alterar senha");
            u.Ok(u.Viu("ERRO", "confirmação"), "confirmação diferente é recusada");
            atual.Text = "1234"; nova.Text = "Nova!Senha99"; confirma.Text = "Nova!Senha99";
            u.Clicar(aba, "Alterar senha");
            u.Ok(Senhas.Verificar("Nova!Senha99", Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE id = @p0", id))) && atual.Text == "",
                "senha alterada e campos limpos");
            u.Ok(Log.Consultar(DateTime.Today, DateTime.Today, Log.Acesso, "Joana Caixa", "Senha alterada").Rows.Count == 1,
                "a troca de senha fica registrada no log de acesso (categoria ACESSO)");
            f.Close();
        }
        finally
        {
            (Sessao.UsuarioId, Sessao.Nome, Sessao.Admin) = (idAntes, nomeAntes, true);
        }
    }
}

/// <summary>Login: senha errada, usuário inativo, acesso certo; tudo registrado no log.</summary>
public class SistemaLoginERecuperacao : ICenario
{
    public string Area => "Sistema";

    static void Entrar(Usuario u, string usuario, string senha, bool cancelarDepois)
    {
        u.QuandoAbrirJanela("Acesso", d =>
        {
            var caixas = Usuario.Todos(d).OfType<TextBox>().ToList();
            caixas[0].Text = usuario;
            caixas[1].Text = senha;
            Usuario.Botao(d, "Entrar").PerformClick();
            Usuario.Esperar();
            if (cancelarDepois && d.DialogResult != DialogResult.OK && d.Visible)
                d.DialogResult = DialogResult.Cancel;
        });
    }

    public void Executar(Usuario u)
    {
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo, email) VALUES('Pedro Antigo', 'pedro', @p0, 0, 0, 'pedro@loja.com.br')", Senhas.Hash("1234"));
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo, email) VALUES('Maria Vendas', 'maria', @p0, 0, 1, 'maria@loja.com.br')", Senhas.Hash("1234"));
        try
        {
            Entrar(u, "maria", "errada", cancelarDepois: true);
            u.Ok(!LoginForm.Executar(), "senha errada não entra");
            u.Ok(u.Viu("ERRO", "Username ou senha inválidos"), "senha errada mostra mensagem");
            Entrar(u, "pedro", "1234", cancelarDepois: true);
            u.Ok(!LoginForm.Executar() && u.Viu("ERRO", "inativo"), "usuário inativo não entra");
            Entrar(u, "  MARIA ", "1234", cancelarDepois: true);
            u.Ok(LoginForm.Executar() && Sessao.Nome == "Maria Vendas" && !Sessao.Admin, "username com maiúsculas e espaços entra como usuário comum");
            u.ConferirJanelas();
            var log = Log.Consultar(DateTime.Today, DateTime.Today, Log.Acesso, null, null);
            var acoes = log.Rows.Cast<DataRow>().Select(r => Ui.Txt(r["Ação"])).ToList();
            u.Ok(acoes.Count(a => a.StartsWith("Acesso recusado")) == 2 && acoes.Contains("Entrou no sistema"), "o log registra acessos recusados e a entrada: " + string.Join(", ", acoes));
            u.Ok(log.Rows.Cast<DataRow>().All(r => !Ui.Txt(r["Detalhe"]).Contains("errada") && !Ui.Txt(r["Detalhe"]).Contains("1234")), "o log não guarda senhas digitadas");
        }
        finally
        {
            (Sessao.UsuarioId, Sessao.Nome, Sessao.Admin) = (1, "Administrador", true);
        }
    }
}

/// <summary>Funcionário esqueceu a senha: recebe código (Resend falso), erra, acerta; envio que falha não invalida o código já recebido.</summary>
public class SistemaRecuperacaoDeSenha : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo, email) VALUES('Maria Vendas', 'maria', @p0, 0, 1, 'maria@loja.com.br')", Senhas.Hash("1234"));
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo) VALUES('Sem Email', 'semail', @p0, 0, 1)", Senhas.Hash("1234"));
        var resend = new ResendFalso();
        Email.TransporteParaTestes = resend;
        try
        {
            var f = new RecuperarSenhaForm("");
            f.Show();
            Usuario.Esperar();

            // sem configuração do Resend
            var usuario = Usuario.Todos(f).OfType<TextBox>().First();
            usuario.Text = "maria";
            u.Clicar(f, "Enviar código");
            ConfigUtil.Esperar(() => u.Mensagens.Count > 0);
            u.Ok(u.Viu("ERRO", "não está configurado"), "sem chave do Resend o sistema explica que o e-mail não está configurado");

            Db.SalvarConfig("resend_api_key", "re_teste");
            Db.SalvarConfig("resend_remetente", "ERP <nao-responda@loja.com.br>");
            u.Mensagens.Clear();
            usuario.Text = "semail";
            u.Clicar(f, "Enviar código");
            ConfigUtil.Esperar(() => u.Mensagens.Count > 0);
            u.Ok(u.Viu("ERRO", "não tem e-mail"), "usuário sem e-mail recebe orientação para falar com o administrador");

            u.Mensagens.Clear();
            usuario.Text = "MARIA@loja.com.br";
            u.Clicar(f, "Enviar código");
            ConfigUtil.Esperar(() => resend.Corpos.Count > 0 && Usuario.Botao(f, "Redefinir").Enabled);
            var codigo = resend.Corpos.Count > 0 ? resend.UltimoCodigo() : "";
            u.Ok(codigo.Length == 6, "e-mail (com maiúsculas) recebe o código de 6 dígitos");

            // novo pedido que falha no envio: o código já recebido continua valendo
            resend.Status = HttpStatusCode.InternalServerError;
            u.Mensagens.Clear();
            u.Clicar(f, "Enviar código");
            ConfigUtil.Esperar(() => u.Mensagens.Count > 0);
            u.Ok(u.Viu("ERRO", "Resend"), "falha no envio mostra a mensagem do Resend");

            var caixas = Usuario.Todos(f).OfType<TextBox>().ToList();
            var (campoCodigo, nova, confirma) = (caixas[1], caixas[2], caixas[3]);
            u.Mensagens.Clear();
            campoCodigo.Text = codigo == "000000" ? "111111" : "000000";
            nova.Text = confirma.Text = "Nova!Senha123";
            u.Clicar(f, "Redefinir senha");
            u.Ok(u.Viu("ERRO", "Restam 4"), "código errado avisa quantas tentativas restam");
            campoCodigo.Text = codigo;
            u.Mensagens.Clear();
            u.Clicar(f, "Redefinir senha");
            Usuario.Esperar();
            u.Ok(u.Viu("INFO", "Senha redefinida"), "o código recebido redefine a senha mesmo depois de um reenvio que falhou" + SistemaUtil.Erros(u));
            u.Ok(Senhas.Verificar("Nova!Senha123", Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE username = 'maria'"))), "a nova senha passa a valer");
            if (!f.IsDisposed)
                f.Close();
        }
        finally
        {
            Email.TransporteParaTestes = null;
        }
    }
}
