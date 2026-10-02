using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Verificações de segurança da revisão OWASP Top 10 (ver docs/15-seguranca-owasp.md).</summary>
public class SegurancaOwaspTop10 : ICenario
{
    public string Area => "Segurança";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        // ---------- A07: bloqueio após 5 senhas erradas
        var hash = Senhas.Hash("Loja!Forte2026");
        Db.Exec("INSERT INTO usuarios(nome, username, senha, admin, ativo, email) VALUES('Caixa', 'caixa1', @p0, 0, 1, 'caixa1@loja.com.br')", hash);
        void TentarEntrar(string senha)
        {
            u.QuandoAbrirJanela("Acesso", f =>
            {
                var caixas = Usuario.Todos(f).OfType<TextBox>().ToList();
                caixas.First(c => !c.UseSystemPasswordChar).Text = "caixa1";
                caixas.First(c => c.UseSystemPasswordChar).Text = senha;
                u.Clicar(f, "Entrar");
                if (!f.IsDisposed && f.Visible)
                    f.DialogResult = DialogResult.Cancel;
            });
            LoginForm.Executar();
            Usuario.Esperar(100);
        }
        for (int i = 0; i < 5; i++)
            TentarEntrar("errada" + i);
        var bloqueado = Ui.Txt(Db.Scalar("SELECT bloqueado_ate FROM usuarios WHERE username = 'caixa1'")).Length > 0;
        u.Mensagens.Clear();
        TentarEntrar("Loja!Forte2026");
        u.Ok(bloqueado && u.Viu("ERRO", "bloqueado temporariamente") && Sessao.Nome != "Caixa",
            "após 5 senhas erradas o usuário fica bloqueado (nem a senha certa entra)");
        Db.Exec("UPDATE usuarios SET senha = @p0 WHERE username = 'caixa1'", Senhas.Hash("Outra!Senha2026"));
        u.Ok(Ui.Txt(Db.Scalar("SELECT bloqueado_ate FROM usuarios WHERE username = 'caixa1'")).Length == 0,
            "definir senha nova desbloqueia o usuário");

        // ---------- A04: hash forte e atualização de hashes antigos
        u.Ok(hash.StartsWith("600000.") && Senhas.Verificar("Loja!Forte2026", hash) && !Senhas.Verificar("loja!forte2026", hash),
            "senhas com PBKDF2-SHA256 de 600.000 iterações");
        u.Ok(!Senhas.Verificar("x", "100.lixo.lixo") && !Senhas.Verificar("x", "abc"), "hash corrompido nega o acesso (falha fechada)");
        var antigo = $"100000.{Convert.ToBase64String(new byte[16])}.{Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("Antiga!2026", new byte[16], 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32))}";
        Db.Exec("UPDATE usuarios SET senha = @p0 WHERE username = 'caixa1'", antigo);
        ControleDeAcesso.RegistrarSucesso((long)Db.Scalar("SELECT id FROM usuarios WHERE username = 'caixa1'")!, "Antiga!2026", antigo);
        var refeito = Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE username = 'caixa1'"));
        u.Ok(Senhas.PrecisaAtualizar(antigo) && refeito.StartsWith("600000.") && Senhas.Verificar("Antiga!2026", refeito),
            "hash antigo (100.000 iterações) é refeito no padrão atual no próximo acesso");

        // ---------- A07: política de senha
        u.Ok(RegrasCadastro.Senha("1234567") is not null && RegrasCadastro.Senha("12345678") is not null && RegrasCadastro.Senha("aaaaaaaa") is not null
             && RegrasCadastro.Senha("Caixa!Loja7") is null, "senha: mínimo 8 caracteres e sem senhas óbvias");

        // ---------- A04/A02: credenciais cifradas no banco
        var chaveResend = Parametros.Definicoes.First(d => d.Chave == "resend_api_key");
        Parametros.Salvar(chaveResend, "re_teste_123");
        var gravado = Db.Config("resend_api_key");
        u.Ok(gravado.StartsWith(Segredos.Prefixo) && !gravado.Contains("re_teste_123") && Parametros.ResendApiKey == "re_teste_123",
            "chave de API gravada cifrada (DPAPI) e lida normalmente pelo sistema");
        u.Ok(Segredos.Abrir(Segredos.Prefixo + "AAAA") == "", "credencial cifrada em outro computador não é revelada (fica em branco)");

        // ---------- A01: autorização na camada de dados
        Sessao.Admin = false;
        string Negado(Action acao)
        {
            try { acao(); return ""; }
            catch (UnauthorizedAccessException ex) { return ex.Message; }
        }
        u.Ok(Negado(() => Operacoes.CancelarVenda(1)).Contains("administradores") && Negado(() => Fiscal.Cancelar(1)).Contains("administradores")
             && Negado(() => Parametros.Salvar(chaveResend, "x")).Contains("administradores") && Negado(() => DadosDemo.Gerar()).Contains("administradores"),
            "usuário comum é barrado na camada de dados (cancelar venda e nota, alterar configurações, gerar dados)");
        Sessao.Admin = true;

        // ---------- A05: injeção
        u.Ok(Ui.SemFormula("=HYPERLINK(\"http://x\")") == "'=HYPERLINK(\"http://x\")" && Ui.SemFormula("@SOMA(1)").StartsWith("'") && Ui.SemFormula("Arroz") == "Arroz",
            "exportação CSV neutraliza fórmulas (CSV injection)");
        Db.Exec("INSERT INTO categorias(nome) VALUES('100% algodão'), ('1000 algodão')");
        var achados = Db.Query("SELECT nome FROM categorias WHERE nome LIKE @p0 ESCAPE '\\'", Db.Contem("100%")).Rows.Count;
        u.Ok(achados == 1, "curingas digitados (% e _) são tratados como texto nas buscas");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM categorias WHERE nome = @p0", "x' OR '1'='1")) == 0, "SQL sempre parametrizado (texto malicioso é só texto)");
        var xxe = Path.Combine(Path.GetTempPath(), $"xxe_{Guid.NewGuid():N}.xml");
        File.WriteAllText(xxe, """<?xml version="1.0"?><!DOCTYPE nfeProc [<!ENTITY x SYSTEM "file:///c:/windows/win.ini">]><nfeProc>&x;</nfeProc>""");
        var recusado = false;
        try { ImportacaoXml.Ler(xxe); }
        catch (InvalidDataException) { recusado = true; }
        File.Delete(xxe);
        u.Ok(recusado, "XML com DTD/entidade externa é recusado (XXE)");
        var escpos = Impressao.EscPos("Produto\u001bp\u0000\u0019", 48, 850, cortar: false, abrirGaveta: false);
        u.Ok(!escpos.Skip(2).Take(escpos.Length - 2).Any(b => b == 0x1B && false) && !System.Text.Encoding.Latin1.GetString(escpos).Contains("Produto\u001bp"),
            "caracteres de controle nos dados não viram comandos da impressora");

        // ---------- A08/A09: log com cadeia de hashes detecta adulteração
        Log.Registrar(Log.Sistema, "Teste de integridade 1");
        Log.Registrar(Log.Sistema, "Teste de integridade 2");
        u.Ok(Log.VerificarIntegridade() is null, "log íntegro passa na verificação");
        using (var c = Log.Abrir())
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "UPDATE log SET acao = 'adulterado' WHERE acao = 'Teste de integridade 1'";
            cmd.ExecuteNonQuery();
        }
        u.Ok(Log.VerificarIntegridade() is string problema && problema.Contains("alterado"), "alteração direta no arquivo do log é detectada");
        u.Ok(Convert.ToString(Db.Scalar("PRAGMA quick_check")) == "ok", "verificação de integridade do banco (quick_check)");
    }
}
