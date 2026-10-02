using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>O administrador investiga o que aconteceu: o log registra telas abertas/fechadas e gravações, e os filtros funcionam.</summary>
public class SistemaAdminConsultaLog : ICenario
{
    public string Area => "Sistema";

    public void Executar(Usuario u)
    {
        var principal = SistemaUtil.AbrirPrincipal();
        u.Teclar(principal, Keys.F8);
        u.Teclar(principal, Keys.Control | Keys.W);
        Navegacao.Abridor!(SistemaUtil.Tela("ConfiguracoesForm"));
        var config = principal.TelaAtiva!;
        ConfigUtil.Parametro<TextBox>(config, "nome_loja").Text = "Mercadinho Novo";
        u.Clicar(ConfigUtil.Aba(config, Parametros.Empresa), "Salvar");
        u.Ok(principal.Text.Contains("Mercadinho Novo") && Usuario.Todos(principal).OfType<Label>().Any(l => l.Text == "Mercadinho Novo"),
            "salvar o nome da loja atualiza na hora o cabeçalho da janela principal: " + principal.Text);

        var tela = Log.Consultar(DateTime.Today, DateTime.Today, Log.Tela, null, null).Rows.Cast<DataRow>().Select(r => Ui.Txt(r["Ação"])).ToList();
        u.Ok(tela.Contains("Abriu Relatórios") && tela.Contains("Fechou Relatórios"), "o log registra abrir e fechar Relatórios");
        u.Ok(tela.Count(a => a == "Abriu Relatórios") == 1, "abrir uma tela registra uma linha só (não duplica): " + tela.Count(a => a == "Abriu Relatórios"));
        var dados = Log.Consultar(DateTime.Today, DateTime.Today, Log.Dados, null, "configuracoes").Rows.Cast<DataRow>().ToList();
        u.Ok(dados.Count > 0 && Ui.Txt(dados[0]["Tela"]) == "Configurações", "salvar configurações registra a gravação com a tela Configurações");

        Navegacao.Abridor!(SistemaUtil.Tela("LogForm"));
        var logForm = (LogForm)principal.TelaAtiva!;
        var grid = Usuario.Achar<DataGridView>(logForm);
        u.Ok(grid.Rows.Count > 0, "a tela de log abre listando registros");
        var combos = Usuario.Todos(logForm).OfType<ComboBox>().ToList();
        combos[0].SelectedItem = Log.Tela;
        u.Clicar(logForm, "Atualizar");
        var linhas = ((DataTable)grid.DataSource!).Rows.Cast<DataRow>().ToList();
        u.Ok(linhas.Count > 0 && linhas.All(r => Ui.Txt(r["Categoria"]) == Log.Tela), "filtro por categoria TELA mostra só TELA");
        u.Ok(combos[1].Items.Contains("Administrador"), "a lista de usuários traz quem aparece no log");

        var texto = Usuario.Todos(logForm).OfType<TextBox>().First(t => t.PlaceholderText.Contains("ação"));
        u.Digitar(texto, "relat", enter: true);
        linhas = ((DataTable)grid.DataSource!).Rows.Cast<DataRow>().ToList();
        u.Ok(linhas.Count > 0 && linhas.All(r => (Ui.Txt(r["Ação"]) + Ui.Txt(r["Tela"])).Contains("Relat", StringComparison.OrdinalIgnoreCase)), "busca por texto filtra (Enter)");
        u.Digitar(texto, "xyz-nada-aqui", enter: true);
        u.Ok(((DataTable)grid.DataSource!).Rows.Count == 0, "busca sem resultado deixa a grade vazia sem erro");

        var datas = Usuario.Todos(logForm).OfType<DateTimePicker>().ToList();
        datas[0].Value = DateTime.Today;
        datas[1].Value = DateTime.Today.AddDays(-3);
        u.Mensagens.Clear();
        u.Clicar(logForm, "Atualizar");
        u.Ok(u.Viu("ERRO", "anterior à inicial"), "datas invertidas no log avisam o usuário");
        SistemaUtil.Fechar(principal, u);
    }
}
