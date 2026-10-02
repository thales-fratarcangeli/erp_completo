using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Revisão de interface: primeiro acesso exige trocar a senha padrão; a tela inicial e o localizador mostram as telas
/// usadas por último; o menu lateral destaca a tela em primeiro plano.
/// </summary>
public class InterfaceRevisaoUsabilidade : ICenario
{
    public string Area => "Interface";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        // ---------- primeiro acesso com a senha padrão
        static void Preencher(Form f, bool senha, params string[] valores)
        {
            var caixas = Usuario.Todos(f).OfType<TextBox>().Where(c => c.UseSystemPasswordChar == senha || !senha).ToList();
            for (int i = 0; i < valores.Length; i++)
                caixas[i].Text = valores[i];
        }
        u.QuandoAbrirJanela("Acesso", f =>
        {
            var caixas = Usuario.Todos(f).OfType<TextBox>().ToList();
            caixas.First(c => !c.UseSystemPasswordChar).Text = "admin";
            caixas.First(c => c.UseSystemPasswordChar).Text = "admin";
            u.Clicar(f, "Entrar");
        });
        u.QuandoAbrirJanela("Troque a senha padrão", f =>
        {
            Preencher(f, true, "admin", "admin");
            u.Clicar(f, "Salvar e entrar");
            Preencher(f, true, "nova1", "nova2");
            u.Clicar(f, "Salvar e entrar");
            Preencher(f, true, "abc", "abc");
            u.Clicar(f, "Salvar e entrar");
            Preencher(f, true, "segura9", "segura9");
            u.Clicar(f, "Salvar e entrar");
        });
        var entrou = LoginForm.Executar();
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(entrou && u.Viu("ERRO", "diferente da padrão") && u.Viu("ERRO", "diferentes") && u.Viu("ERRO", "pelo menos 4"),
            "primeiro acesso com a senha padrão exige senha nova (recusa a padrão, senhas diferentes e curtas)");
        var hash = Ui.Txt(Db.Scalar("SELECT senha FROM usuarios WHERE username = 'admin'"));
        u.Ok(Senhas.Verificar("segura9", hash) && !Senhas.Verificar("admin", hash), "senha nova gravada (como hash) e a padrão deixa de valer");

        // segundo acesso com a senha nova: entra direto, sem pedir troca
        u.QuandoAbrirJanela("Acesso", f =>
        {
            var caixas = Usuario.Todos(f).OfType<TextBox>().ToList();
            caixas.First(c => !c.UseSystemPasswordChar).Text = "admin";
            caixas.First(c => c.UseSystemPasswordChar).Text = "segura9";
            u.Clicar(f, "Entrar");
        });
        u.Ok(LoginForm.Executar() && u.JanelasPendentes == 0, "com a senha nova entra direto");

        // ---------- telas recentes e destaque no menu
        using var principal = new MainForm { WindowState = FormWindowState.Normal, Size = new Size(1400, 860) };
        principal.Show();
        Usuario.Esperar();
        Tela TelaDe(string chave) => Navegacao.Telas().First(x => x.Chave == chave);
        Navegacao.Abridor!(TelaDe("EstoqueForm"));
        Usuario.Esperar();
        u.Ok(principal.TelaDestacadaNoMenu == "EstoqueForm", "o menu lateral destaca a tela em primeiro plano");
        Navegacao.Abridor!(TelaDe("FinanceiroForm"));
        Usuario.Esperar();
        u.Ok(principal.TelaDestacadaNoMenu == "FinanceiroForm", "o destaque do menu acompanha a troca de tela");
        principal.TelaAtiva!.Close();
        Usuario.Esperar();
        principal.TelaAtiva!.Close();
        Usuario.Esperar();
        var atalhos = principal.AtalhosRecentes;
        u.Ok(principal.TelaAtiva is null && principal.TelaDestacadaNoMenu is null
             && atalhos.Count >= 2 && atalhos[0] == TelaDe("FinanceiroForm").Titulo && atalhos[1] == TelaDe("EstoqueForm").Titulo,
            "a tela inicial mostra \"Continuar de onde parou\" com as telas usadas por último: " + string.Join(", ", atalhos));

        using var localizador = (Form)Activator.CreateInstance(typeof(LocalizarTelaForm), nonPublic: true)!;
        localizador.Show();
        Usuario.Esperar(300);
        var lista = Usuario.Achar<ListBox>(localizador);
        u.Ok(lista.Items.Count > 2 && (string)lista.Items[0] == TelaDe("FinanceiroForm").Titulo && (string)lista.Items[1] == TelaDe("EstoqueForm").Titulo,
            "o localizador (Ctrl+K) vazio mostra as telas recentes primeiro");
        localizador.Close();
        principal.Close();
    }
}
