namespace ErpLoja.Forms;

public static class LoginForm
{
    public static bool Executar()
    {
        using var d = new Dialogo("ERP Loja - Acesso", "login", "Entrar")
        {
            ShowInTaskbar = true,
            StartPosition = FormStartPosition.CenterScreen,
        };
        var usuario = d.Adicionar("Username:", new TextBox());
        var senha = d.Adicionar("Senha:", new TextBox { UseSystemPasswordChar = true });

        var admin = Db.Query("SELECT senha FROM usuarios WHERE username = 'admin'");
        if (admin.Rows.Count > 0 && Senhas.Verificar("admin", Ui.Txt(admin.Rows[0]["senha"])))
            d.Nota("Primeiro acesso: usuário admin, senha admin (troque em Configurações > Minha conta).");
        d.Link("Esqueci minha senha", () =>
        {
            using var r = new RecuperarSenhaForm(usuario.Text.Trim());
            r.ShowDialog(d);
        });

        d.Validar = () =>
        {
            var dt = Db.Query("SELECT id, nome, senha, admin, ativo, email FROM usuarios WHERE username = @p0", usuario.Text.Trim());
            if (dt.Rows.Count == 0 || !Senhas.Verificar(senha.Text, Ui.Txt(dt.Rows[0]["senha"])))
            {
                Log.Registrar(Log.Acesso, "Acesso recusado", $"username informado: {usuario.Text.Trim()}", tela: "Acesso");
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
            Log.Registrar(Log.Acesso, "Entrou no sistema", Sessao.Admin ? "administrador" : "usuário comum", tela: "Acesso");
            if (!Email.Valido(Ui.Txt(r["email"])))
                Ui.Info("Seu usuário não tem e-mail cadastrado. Cadastre em Configurações > Minha conta para poder recuperar a senha se esquecer.");
            return true;
        };

        return d.ShowDialog() == DialogResult.OK;
    }
}
