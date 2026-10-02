using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A auxiliar fiscal começa uma nota avulsa, troca para NFC-e (modelo 65): a série passa a ser a configurada para o
/// modelo 65; adiciona um item pelo código e salva o rascunho.
/// </summary>
public class FiscalEditorTrocaModelo : ICenario
{
    public string Area => "Fiscal";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("serie_55", "1");
        Db.SalvarConfig("serie_65", "7");
        var codigo = Ui.Txt(Db.Scalar("SELECT codigo FROM produtos WHERE ativo = 1 AND tipo <> 'INSUMO' ORDER BY id LIMIT 1"));
        var tela = u.AbrirTela("NotasForm");
        decimal serie65 = 0, serie55 = 0;
        int itens = 0;
        u.QuandoAbrir<NotaEditorForm>(ed =>
        {
            var modelo = Usuario.Todos(ed).OfType<ComboBox>().First(c => c.Items.Contains("65 - NFC-e"));
            var serie = Usuario.Todos(ed).OfType<NumericUpDown>().First(n => n.Maximum == 999);
            modelo.SelectedIndex = 1;
            Usuario.Esperar();
            serie65 = serie.Value;
            modelo.SelectedIndex = 0;
            Usuario.Esperar();
            serie55 = serie.Value;
            modelo.SelectedIndex = 1;
            u.Digitar(Usuario.Todos(ed).OfType<TextBox>().First(t => t.Width == 180), codigo, enter: true);
            itens = Usuario.Achar<DataGridView>(ed).Rows.Count;
            u.Clicar(ed, "Salvar rascunho");
        });
        using (var f = new NotaEditorForm(Fiscal.Nova(Fiscal.Saida)))
            f.ShowDialog(tela);
        Usuario.Esperar(200);
        u.ConferirJanelas();
        u.Ok(serie65 == 7 && serie55 == 1, $"trocar o modelo usa a série configurada do modelo (65 → {serie65}, 55 → {serie55})");
        u.Ok(itens == 1, "Enter no código adiciona o item");
        u.Ok(CadastrosApoio.Contar("notas_fiscais", "modelo = '65' AND serie = 7 AND status = 'DIGITADA'") == 1, "rascunho gravado como NFC-e série 7" + CadastrosApoio.Erros(u));
        tela.Close();
    }
}
