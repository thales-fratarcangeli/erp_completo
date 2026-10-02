using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A auxiliar fiscal emite a NF-e de uma venda para cliente de outro estado: troca a UF (CFOP vira 6102), digita por
/// engano 5102 num item e um desconto maior que o item, corrige e emite; reabre a venda (nota já existe, só leitura);
/// um usuário comum tenta cancelar e é barrado; o administrador cancela.
/// </summary>
public class FiscalAuxiliarEmiteNotaDeVenda : ICenario
{
    public string Area => "Fiscal";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("uf", "SP");
        Db.SalvarConfig("cnpj", "11.222.333/0001-81");
        var venda = Convert.ToInt64(Db.Scalar("""
            SELECT v.id FROM vendas v JOIN pessoas p ON p.id = v.cliente_id
            WHERE v.status = 'FINALIZADA' AND NOT EXISTS (SELECT 1 FROM notas_fiscais n WHERE n.venda_id = v.id)
            ORDER BY v.id DESC LIMIT 1
            """));
        var proximo = long.Parse(Db.Config("proximo_numero_55"));
        var tela = u.AbrirTela("NotasForm");

        List<string> cfopsAposUf = [];
        bool erroCfop = false, erroDesconto = false;
        u.QuandoAbrir<NotaEditorForm>(ed =>
        {
            var itens = (System.ComponentModel.BindingList<NotaItem>)Usuario.Achar<DataGridView>(ed).DataSource!;
            var uf = Usuario.Todos(ed).OfType<ComboBox>().First(c => c.Items.Contains("RJ"));
            uf.SelectedItem = "MG";
            uf.SelectedItem = "RJ";
            Usuario.Esperar();
            cfopsAposUf = itens.Select(i => i.Cfop).ToList();
            foreach (var i in itens)
            {
                i.Ncm = "22021000";
                i.Cst = "102";
            }
            itens[0].Cfop = "5102"; // digitado por engano na grade
            u.Clicar(ed, "Salvar e emitir");
            erroCfop = u.Viu("ERRO", "use 6102");
            itens[0].Cfop = "6102";
            itens[0].Desconto = itens[0].Quantidade * itens[0].ValorUnitario + 1;
            u.Mensagens.Clear();
            u.Clicar(ed, "Salvar e emitir");
            erroDesconto = u.Viu("ERRO", "desconto deve ficar entre zero");
            itens[0].Desconto = 0;
            u.Mensagens.Clear();
            u.QuandoAbrir<CupomForm>(danfe => danfe.Close());
            u.Clicar(ed, "Salvar e emitir");
        });
        NotasForm.NotaDeVenda(tela, venda);
        Usuario.Esperar(300);
        u.ConferirJanelas();

        u.Ok(cfopsAposUf.Count > 0 && cfopsAposUf.All(c => c == "6102"), "trocar a UF do destinatário para RJ muda o CFOP para 6102");
        u.Ok(erroCfop, "CFOP de dentro do estado para cliente de outra UF é barrado na emissão");
        u.Ok(erroDesconto, "desconto maior que o valor do item é barrado na emissão");
        var nota = Db.Query("SELECT id, numero, chave, status, part_uf FROM notas_fiscais WHERE venda_id = @p0", venda).Rows.Cast<System.Data.DataRow>().FirstOrDefault();
        u.Ok(nota != null && Ui.Txt(nota["status"]) == "EMITIDA" && Ui.Txt(nota["part_uf"]) == "RJ", "nota emitida para a venda" + CadastrosApoio.Erros(u));
        if (nota == null)
            return;
        var notaId = (long)nota["id"];
        var chave = Ui.Txt(nota["chave"]);
        u.Ok((long)nota["numero"] >= proximo && chave.Length == 44 && Fiscal.DigitoMod11(chave[..43]) == chave[43] - '0',
            "numeração definitiva e chave de 44 dígitos com DV");
        u.Ok(CadastrosApoio.Contar("nota_itens", "nota_id = @p0 AND cfop <> '6102'", notaId) == 0, "itens gravados com CFOP 6102");

        // nota já existente: abre a mesma, só leitura
        bool somenteLeitura = false;
        u.Mensagens.Clear();
        u.QuandoAbrir<NotaEditorForm>(ed =>
        {
            somenteLeitura = !Usuario.Botao(ed, "Salvar e emitir").Visible;
            ed.Close();
        });
        NotasForm.NotaDeVenda(tela, venda);
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(u.Viu("INFO", "já possui nota") && somenteLeitura, "venda com nota abre a nota existente sem permitir alterar");
        try
        {
            Fiscal.Salvar(Fiscal.DeVenda(venda));
            u.Ok(false, "segunda nota para a mesma venda deveria ser recusada");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("já tem nota fiscal"), "camada de dados recusa segunda nota para a mesma venda (RN-FIS-01)");
        }

        // cancelar: usuário comum não pode; administrador pode
        Usuario.Achar<Button>(tela, b => b.Text == "Pesquisar").PerformClick();
        Usuario.Esperar();
        u.Ok(u.SelecionarRegistro(tela, notaId), "nota emitida aparece na lista");
        u.Mensagens.Clear();
        Sessao.Admin = false;
        try { u.Clicar(tela, "Cancelar / excluir"); }
        finally { Sessao.Admin = true; }
        u.Ok(u.Viu("ERRO", "Somente um administrador") && Ui.Txt(Db.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", notaId)) == "EMITIDA",
            "usuário comum não cancela nota emitida");
        u.Mensagens.Clear();
        u.Ok(u.SelecionarRegistro(tela, notaId), "nota selecionada de novo");
        u.Clicar(tela, "Cancelar / excluir");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM notas_fiscais WHERE id = @p0", notaId)) == "CANCELADA", "administrador cancela a nota");
        u.Ok(Fiscal.NotaExistente("venda_id", venda) is null, "venda volta a aceitar nota depois do cancelamento");
        try
        {
            Fiscal.Cancelar(notaId);
            u.Ok(false, "cancelar nota já cancelada deveria falhar");
        }
        catch (InvalidOperationException)
        {
            u.Ok(true, "cancelar duas vezes é recusado na camada de dados");
        }
        tela.Close();
    }
}
