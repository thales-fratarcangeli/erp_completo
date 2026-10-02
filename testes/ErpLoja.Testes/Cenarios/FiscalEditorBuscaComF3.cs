using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A auxiliar fiscal lança uma nota de entrada avulsa buscando o produto com F3 pelo nome: o item entra pelo preço de
/// custo e CFOP de entrada; um produto inativo não aparece na busca.
/// </summary>
public class FiscalEditorBuscaComF3 : ICenario
{
    public string Area => "Fiscal";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("uf", "SP");
        var p = Db.Query("SELECT id, codigo, nome, preco_custo FROM produtos WHERE ativo = 1 ORDER BY id LIMIT 2");
        var alvo = p.Rows[0];
        var inativo = p.Rows[1];
        Db.Exec("UPDATE produtos SET ativo = 0 WHERE id = @p0", inativo["id"]);
        var tela = u.AbrirTela("NotasForm");

        int linhasInativo = -1;
        NotaItem? item = null;
        u.QuandoAbrir<NotaEditorForm>(ed =>
        {
            u.QuandoAbrir<BuscaProdutoForm>(busca =>
            {
                var campo = Usuario.Todos(busca).OfType<TextBox>().First();
                u.Digitar(campo, Ui.Txt(inativo["codigo"]));
                linhasInativo = Usuario.Achar<DataGridView>(busca).Rows.Count;
                u.Digitar(campo, Ui.Txt(alvo["nome"]));
                u.Teclar(busca, Keys.Enter);
            });
            u.Teclar(ed, Keys.F3);
            Usuario.Esperar(300);
            item = ((System.ComponentModel.BindingList<NotaItem>)Usuario.Achar<DataGridView>(ed).DataSource!).FirstOrDefault();
            ed.Close();
        });
        using (var f = new NotaEditorForm(Fiscal.Nova(Fiscal.Entrada)))
            f.ShowDialog(tela);
        Usuario.Esperar(200);
        u.ConferirJanelas();
        u.Ok(linhasInativo == 0, "produto inativo não aparece na busca (F3)");
        u.Ok(item != null && item.ProdutoId == (long)alvo["id"] && item.Cfop == "1102" && item.ValorUnitario == Ui.Dec(alvo["preco_custo"]),
            "F3 + nome + Enter adiciona o produto pelo custo com CFOP 1102");
        tela.Close();
    }
}
