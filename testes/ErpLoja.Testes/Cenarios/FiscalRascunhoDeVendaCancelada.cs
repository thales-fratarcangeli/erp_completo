using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A auxiliar fiscal deixa o rascunho da nota de uma venda; antes de emitir, o administrador cancela a venda.
/// Emitir o rascunho depois disso é barrado com a orientação de excluí-lo; pela tela, o rascunho é excluído.
/// </summary>
public class FiscalRascunhoDeVendaCancelada : ICenario
{
    public string Area => "Fiscal";

    public void Executar(Usuario u)
    {
        var venda = Convert.ToInt64(Db.Scalar("""
            SELECT v.id FROM vendas v WHERE v.status = 'FINALIZADA'
              AND NOT EXISTS (SELECT 1 FROM notas_fiscais n WHERE n.venda_id = v.id) ORDER BY v.id LIMIT 1
            """));
        var n = Fiscal.DeVenda(venda);
        foreach (var i in n.Itens)
        {
            i.Ncm = "22021000";
            i.Cst = "102";
        }
        var id = Fiscal.Salvar(n);
        Operacoes.CancelarVenda(venda);

        try
        {
            Fiscal.Emitir(id);
            u.Ok(false, "emitir nota de venda cancelada deveria ser barrado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("foi cancelada"), "nota de venda cancelada não é emitida: " + ex.Message.Split('\n')[0]);
        }

        var tela = u.AbrirTela("NotasForm");
        Usuario.Todos(tela).OfType<ComboBox>().First(c => c.Items.Contains("Em digitação")).SelectedItem = "Em digitação";
        Usuario.Esperar();
        u.Ok(u.SelecionarRegistro(tela, id), "rascunho aparece em Em digitação");
        u.Clicar(tela, "Cancelar / excluir");
        u.Ok(u.Viu("CONFIRMAR", "Excluir este rascunho") && CadastrosApoio.Contar("notas_fiscais", "id = @p0", id) == 0 &&
             CadastrosApoio.Contar("nota_itens", "nota_id = @p0", id) == 0, "rascunho e itens excluídos");
        tela.Close();
    }
}
