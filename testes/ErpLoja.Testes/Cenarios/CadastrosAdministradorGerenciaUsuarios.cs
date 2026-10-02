using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O administrador gerencia usuários: tenta se inativar e tirar o próprio acesso, cadastra um usuário com senha curta,
/// e-mail e username repetidos (com maiúsculas diferentes), corrige; confere que o usuário comum não vê telas de
/// administrador e que o sistema nunca fica sem administrador ativo.
/// </summary>
public class CadastrosAdministradorGerenciaUsuarios : ICenario
{
    public string Area => "Cadastros";

    public void Executar(Usuario u)
    {
        var tela = u.AbrirTela("cadastro_usuarios");
        var eu = Sessao.UsuarioId!.Value;

        u.Ok(u.SelecionarRegistro(tela, eu), "o próprio usuário aparece na lista");
        if (Db.Scalar("SELECT email FROM usuarios WHERE id = @p0", eu) is not string)
            u.Preencher(tela, "E-mail (recuperação de senha)", "admin@loja.com.br"); // banco novo: admin sem e-mail
        u.Preencher(tela, "Ativo", false);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "não pode inativar o próprio usuário"), "administrador não consegue se inativar");
        u.Ok(CadastrosApoio.Contar("usuarios", "id = @p0 AND ativo = 1 AND admin = 1", eu) == 1, "continua ativo");
        u.Preencher(tela, "Ativo", true);

        u.Mensagens.Clear();
        u.Preencher(tela, "Administrador (acessa usuários, configurações e cancelamentos)", false);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "seu próprio acesso de administrador"), "administrador não tira o próprio acesso");
        u.Ok(CadastrosApoio.Contar("usuarios", "id = @p0 AND admin = 1", eu) == 1, "continua administrador");

        // novo usuário com dados errados
        var emailAdmin = Ui.Txt(Db.Scalar("SELECT email FROM usuarios WHERE id = @p0", eu));
        var usernameAdmin = Ui.Txt(Db.Scalar("SELECT username FROM usuarios WHERE id = @p0", eu));
        u.Clicar(tela, "Novo");
        u.Mensagens.Clear();
        u.Preencher(tela, "Nome", "João Caixa");
        u.Preencher(tela, "Username", "joao.caixa");
        u.Preencher(tela, "Senha (mínimo 8 caracteres)", "12");
        u.Preencher(tela, "E-mail (recuperação de senha)", "joao@loja.com.br");
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "pelo menos 8 caracteres"), "senha curta é recusada");

        if (emailAdmin.Length > 0)
        {
            u.Mensagens.Clear();
            u.Preencher(tela, "Senha (mínimo 8 caracteres)", "Joao!Senha2026");
            u.Preencher(tela, "E-mail (recuperação de senha)", emailAdmin.ToUpperInvariant());
            u.Clicar(tela, "Salvar");
            u.Ok(u.Viu("ERRO", "já existe outro registro com este \"E-mail"), "e-mail de outro usuário (maiúsculas diferentes) é recusado" + CadastrosApoio.Erros(u));
        }

        u.Mensagens.Clear();
        u.Preencher(tela, "Senha (mínimo 8 caracteres)", "Joao!Senha2026");
        u.Preencher(tela, "E-mail (recuperação de senha)", "joao@loja.com.br");
        u.Preencher(tela, "Username", usernameAdmin.ToUpperInvariant());
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "já existe outro registro com este \"Username\""), "username repetido (maiúsculas diferentes) é recusado" + CadastrosApoio.Erros(u));

        u.Mensagens.Clear();
        u.Preencher(tela, "Username", "joao.caixa");
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", ""), "usuário comum cadastrado" + CadastrosApoio.Erros(u));
        var joao = Convert.ToInt64(Db.Scalar("SELECT id FROM usuarios WHERE username = 'joao.caixa'") ?? 0L);
        u.Ok(joao > 0 && Senhas.Verificar("Joao!Senha2026", Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE id = @p0", joao))),
            "senha gravada como hash e conferível");

        // editar sem digitar senha mantém a senha
        u.Mensagens.Clear();
        u.Preencher(tela, "Nome", "João da Caixa");
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", "") && Senhas.Verificar("Joao!Senha2026", Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE id = @p0", joao))),
            "editar com senha em branco mantém a senha" + CadastrosApoio.Erros(u));

        // usuário comum não vê telas de administrador
        u.Ok(!Navegacao.Buscar("usuário", admin: false).Any(r => r.Tela.SoAdmin) &&
             Navegacao.Buscar("usuário", admin: true).Any(r => r.Tela.Chave == "cadastro_usuarios"),
            "localizador de telas esconde as telas de administrador do usuário comum");

        // nunca sem administrador ativo (outra pessoa logada tentando mexer no único administrador)
        u.Ok(RegrasCadastro.AlteracaoUsuario(eu, admin: false, ativo: true, usuarioLogado: joao)?.Contains("único administrador") == true &&
             RegrasCadastro.AlteracaoUsuario(eu, admin: true, ativo: false, usuarioLogado: joao)?.Contains("único administrador") == true &&
             RegrasCadastro.ExclusaoUsuario(eu, joao)?.Contains("único administrador") == true,
            "o único administrador ativo não pode ser inativado, rebaixado nem excluído");
        u.Ok(RegrasCadastro.AlteracaoUsuario(joao, admin: false, ativo: false, usuarioLogado: eu) is null,
            "inativar usuário comum é permitido");

        // com um segundo administrador, o primeiro pode ser rebaixado por ele
        Db.Exec("UPDATE usuarios SET admin = 1 WHERE id = @p0", joao);
        u.Ok(RegrasCadastro.AlteracaoUsuario(eu, admin: false, ativo: true, usuarioLogado: joao) is null,
            "havendo outro administrador ativo, o primeiro pode ser rebaixado");

        // excluir o próprio usuário continua bloqueado
        u.Mensagens.Clear();
        u.Ok(u.SelecionarRegistro(tela, eu), "seleciona o próprio usuário");
        u.Clicar(tela, "Excluir");
        u.Ok(u.Viu("ERRO", "próprio usuário") && CadastrosApoio.Contar("usuarios", "id = @p0", eu) == 1, "não exclui o próprio usuário");
        tela.Close();
    }
}
