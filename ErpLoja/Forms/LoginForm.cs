namespace ErpLoja.Forms;

public static class LoginForm
{
    public static bool Executar()
    {
        using var d = new Dialogo("ERP Loja - Acesso", "login", "Entrar")
        {
            ConfirmarAoSair = false,
            ShowInTaskbar = true,
            StartPosition = FormStartPosition.CenterScreen,
        };
        var usuario = d.Adicionar("Username:", Validacao.Caixa(Formatos.Username, "Username"));
        var senha = d.Adicionar("Senha:", Validacao.Aplicar(new TextBox { UseSystemPasswordChar = true }, Formatos.Senha, "Senha"));

        var admin = Db.Query("SELECT senha FROM usuarios WHERE username = 'admin'");
        if (admin.Rows.Count > 0 && Senhas.Verificar("admin", Ui.Txt(admin.Rows[0]["senha"])))
            d.Nota("Primeiro acesso: usuário admin, senha admin. Ao entrar, o sistema pede uma senha nova.");
        d.Link("Esqueci minha senha", () =>
        {
            using var r = new RecuperarSenhaForm(usuario.Text.Trim());
            r.ShowDialog(d);
        });

        d.Validar = () =>
        {
            var dt = Db.Query("SELECT id, nome, senha, admin, ativo, email, falhas_login, bloqueado_ate FROM usuarios WHERE username = @p0", usuario.Text.Trim());
            if (dt.Rows.Count > 0 && ControleDeAcesso.Bloqueado(dt.Rows[0]) is TimeSpan falta)
            {
                Log.Registrar(Log.Acesso, "Acesso recusado (usuário bloqueado)", $"username informado: {usuario.Text.Trim()}", tela: "Acesso");
                Ui.Erro($"Usuário bloqueado temporariamente por excesso de senhas erradas. Tente de novo em {Math.Ceiling(falta.TotalMinutes):0} minuto(s) " +
                        "ou peça a um administrador para definir uma senha nova (ou use \"Esqueci minha senha\").");
                return false;
            }
            if (dt.Rows.Count == 0 || !Senhas.Verificar(senha.Text, Ui.Txt(dt.Rows[0]["senha"])))
            {
                Log.Registrar(Log.Acesso, "Acesso recusado", $"username informado: {usuario.Text.Trim()}", tela: "Acesso");
                if (dt.Rows.Count > 0 && ControleDeAcesso.RegistrarFalha((long)dt.Rows[0]["id"]))
                    Log.Registrar(Log.Acesso, "Usuário bloqueado por senhas erradas", $"username: {usuario.Text.Trim()}, bloqueio de {ControleDeAcesso.MinutosBloqueio} min", tela: "Acesso");
                Ui.Erro("Username ou senha inválidos.");
                senha.SelectAll();
                senha.Focus();
                return false;
            }
            var r = dt.Rows[0];
            if ((long)r["ativo"] != 1)
            {
                Log.Registrar(Log.Acesso, "Acesso recusado (usuário inativo)", usuario.Text.Trim(), tela: "Acesso");
                Ui.Erro("Este usuário está inativo.");
                return false;
            }
            Sessao.UsuarioId = (long)r["id"];
            Sessao.Nome = Ui.Txt(r["nome"]);
            Sessao.Admin = (long)r["admin"] == 1;
            ControleDeAcesso.RegistrarSucesso((long)r["id"], senha.Text, Ui.Txt(r["senha"]));
            Preferencias.CarregarTema();
            var alerta = Sessao.Admin ? ControleDeAcesso.AlertaDeTentativas((long)r["id"]) : null;
            Log.Registrar(Log.Acesso, "Entrou no sistema", Sessao.Admin ? "administrador" : "usuário comum", tela: "Acesso");
            if (alerta != null)
                Ui.Aviso(alerta);
            if (!Email.Valido(Ui.Txt(r["email"])))
                Ui.Aviso("Seu usuário não tem e-mail cadastrado. Cadastre em Configurações > Minha conta para poder recuperar a senha.");
            return true;
        };

        if (d.ShowDialog() != DialogResult.OK)
            return false;
        // senha padrão (admin) ou igual ao username: qualquer um adivinha — exige uma senha nova antes de entrar
        var senhaFraca = senha.Text == "admin" || string.Equals(senha.Text, usuario.Text.Trim(), StringComparison.OrdinalIgnoreCase);
        return !senhaFraca || TrocarSenhaFraca(senha.Text, usuario.Text.Trim());
    }

    /// <summary>Primeiro acesso com a senha padrão: a pessoa escolhe uma senha nova para continuar.</summary>
    static bool TrocarSenhaFraca(string atual, string username)
    {
        using var d = new Dialogo("Troque a senha padrão", "login", "Salvar e entrar") { ConfirmarAoSair = false, StartPosition = FormStartPosition.CenterScreen };
        d.Nota("Por segurança, escolha uma senha nova antes de começar. A senha atual é a padrão (ou igual ao username) e qualquer pessoa poderia adivinhá-la.");
        var nova = d.Adicionar("Nova senha:", Validacao.Aplicar(new TextBox { UseSystemPasswordChar = true }, Formatos.Senha, "Nova senha"));
        var confirmacao = d.Adicionar("Repita a nova senha:", Validacao.Aplicar(new TextBox { UseSystemPasswordChar = true }, Formatos.Senha, "Repita a nova senha"));
        d.Validar = () =>
        {
            string? erro = nova.Text == atual || nova.Text == "admin" || string.Equals(nova.Text, username, StringComparison.OrdinalIgnoreCase)
                    ? "Escolha uma senha diferente da padrão e do username."
                : RegrasCadastro.Senha(nova.Text) is string fraca ? fraca
                : nova.Text != confirmacao.Text ? "As duas senhas digitadas são diferentes."
                : null;
            if (erro is null)
                return true;
            Ui.Erro(erro);
            nova.Focus();
            return false;
        };
        if (d.ShowDialog() != DialogResult.OK)
        {
            Log.Registrar(Log.Acesso, "Entrada cancelada: senha padrão não trocada", tela: "Acesso");
            return false;
        }
        Db.Exec("UPDATE usuarios SET senha = @p0 WHERE id = @p1", Senhas.Hash(nova.Text), Sessao.UsuarioId);
        Log.Registrar(Log.Acesso, "Senha padrão trocada no primeiro acesso", tela: "Acesso");
        Ui.Feito("Senha alterada. Bem-vindo(a)!");
        return true;
    }
}
