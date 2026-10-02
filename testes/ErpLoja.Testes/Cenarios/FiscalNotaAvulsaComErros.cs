using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A auxiliar fiscal digita uma nota de entrada avulsa com erros típicos: CFOP de saída, NFC-e numa entrada, CNPJ do
/// remetente errado, desconto adicional maior que os produtos, CFOP interestadual para fornecedor do mesmo estado.
/// Salvar rascunho é permitido; emitir é barrado com a lista do que corrigir. Depois corrige e emite.
/// </summary>
public class FiscalNotaAvulsaComErros : ICenario
{
    public string Area => "Fiscal";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("uf", "SP");
        Db.SalvarConfig("cnpj", "11.222.333/0001-81");
        var produto = Convert.ToInt64(Db.Scalar("SELECT id FROM produtos WHERE ativo = 1 ORDER BY id LIMIT 1"));

        var n = Fiscal.Nova(Fiscal.Entrada);
        n.Modelo = "65";
        n.PartNome = "Fornecedor digitado";
        n.PartDocumento = "11.222.333/0001-00";
        n.PartUf = "SP";
        var item = Fiscal.ItemDeProduto(produto, 2, 10, "5102");
        item.Ncm = "22021000";
        item.Cst = "102";
        n.Itens.Add(item);
        n.Desconto = 50;

        var erros = Fiscal.Validar(n);
        bool Tem(string trecho) => erros.Any(e => e.Contains(trecho, StringComparison.OrdinalIgnoreCase));
        u.Ok(Tem("CFOP 5102 é de saída"), "CFOP de saída numa nota de entrada é acusado");
        u.Ok(Tem("NFC-e (modelo 65) é só para venda"), "NFC-e numa nota de entrada é acusada");
        u.Ok(Tem("CPF/CNPJ do remetente inválido"), "CNPJ do remetente com dígito errado é acusado");
        u.Ok(Tem("desconto adicional"), "desconto adicional maior que os produtos é acusado");

        n.Modelo = "55";
        n.PartDocumento = "11.222.333/0001-81";
        n.Desconto = 0;
        item.Cfop = "2102";
        erros = Fiscal.Validar(n);
        u.Ok(Tem("use 1102"), "CFOP interestadual com fornecedor do mesmo estado é acusado");
        item.Cfop = "1102";
        u.Ok(Fiscal.Validar(n).Count == 0, "nota corrigida não tem pendências: " + string.Join(" / ", Fiscal.Validar(n)));

        // pela tela: rascunho com CFOP errado salva; emitir mostra o erro; corrigido emite
        item.Cfop = "5102";
        var id = Fiscal.Salvar(n);
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", id)) == "DIGITADA", "rascunho com pendências pode ser salvo");
        var tela = u.AbrirTela("NotasForm");
        var situacao = Usuario.Todos(tela).OfType<ComboBox>().First(c => c.Items.Contains("Em digitação"));
        situacao.SelectedItem = "Em digitação";
        Usuario.Esperar();
        u.Ok(u.SelecionarRegistro(tela, id), "rascunho aparece no filtro Em digitação");
        u.Mensagens.Clear();
        u.Clicar(tela, "Emitir");
        u.Ok(u.Viu("ERRO", "CFOP 5102 é de saída") && Ui.Txt(Db.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", id)) == "DIGITADA",
            "emitir rascunho com CFOP errado mostra o motivo e não emite");

        Db.Exec("UPDATE nota_itens SET cfop = '1102' WHERE nota_id = @p0", id);
        u.Mensagens.Clear();
        u.QuandoAbrir<CupomForm>(danfe => danfe.Close());
        u.Ok(u.SelecionarRegistro(tela, id), "rascunho selecionado");
        u.Clicar(tela, "Emitir");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", id)) == "EMITIDA", "nota de entrada corrigida é emitida" + CadastrosApoio.Erros(u));
        tela.Close();
    }
}
