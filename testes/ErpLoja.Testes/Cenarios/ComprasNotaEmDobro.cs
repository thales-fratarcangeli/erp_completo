using System.Reflection;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O comprador recebe o pedido do atacadista digitando a NF 4567. Dias depois o dono recebe o XML da mesma NF e
/// vai importar: o sistema tem que perceber que a mercadoria já entrou e não lançar estoque/contas em dobro.
/// Depois o comprador tenta receber outro pedido com a mesma NF (digitada "004567"): o sistema pergunta.
/// </summary>
public class ComprasMesmaNotaEmDobro : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        Db.Exec("INSERT INTO pessoas(nome, cpf_cnpj, eh_fornecedor) VALUES('Atacadão de Alimentos', '12.345.678/0001-95', 1)");
        var fornecedor = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM pessoas"));
        var (produto, codigo) = ComprasUtil.Produtos(1)[0];

        void ReceberPedido(string nf)
        {
            var tela = u.AbrirTela("pedidos_COMPRA");
            u.QuandoAbrir<PedidoEditorForm>(ed =>
            {
                Ui.Selecionar(ComprasUtil.CampoApos<ComboBox>(ed, "Fornecedor:"), fornecedor);
                ComprasUtil.LancarItem(u, ed, codigo, 20, 13.10m);
                ComprasUtil.ResponderRecebimento(u, "Boleto", 1, nf);
                u.Clicar(ed, "Salvar e receber");
            });
            u.Clicar(tela, "Novo");
            Usuario.Esperar(400);
            u.ConferirJanelas();
            tela.Close();
        }

        var estoque0 = ComprasUtil.Estoque(produto);
        ReceberPedido("4567");
        u.Ok(ComprasUtil.Estoque(produto) == estoque0 + 20, "pedido recebido com a NF 4567");
        var compras = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compras"));

        // chega o XML da mesma NF
        var nota = ImportacaoXml.Ler(ComprasUtil.CaminhoNfeExemplo);
        ImportacaoXml.SugerirVinculos(nota);
        u.Ok(nota.FornecedorId == fornecedor, "fornecedor localizado pelo CNPJ com pontuação");
        var conf = (Form)Activator.CreateInstance(typeof(ImportarXmlForm), BindingFlags.NonPublic | BindingFlags.Instance, null, [nota], null)!;
        conf.Show();
        Usuario.Esperar();
        var lancar = Usuario.Achar<CheckBox>(conf, c => c.Text.StartsWith("Lançar compra"));
        u.Ok(!lancar.Checked, "\"Lançar compra\" vem desmarcado porque a NF já entrou pelo pedido");
        u.Ok(Usuario.Todos(conf).OfType<Label>().Any(l => l.Text.Contains("já entrou no estoque")), "a conferência explica o motivo");
        var grid = Usuario.Achar<DataGridView>(conf);
        grid.Rows[0].Cells["produto"].Value = produto;
        u.Clicar(conf, "Importar");
        u.Ok(u.Viu("INFO", "Nota importada"), "nota registrada");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compras")) == compras, "nenhuma compra nova (sem entrada em dobro)");
        u.Ok(ComprasUtil.Estoque(produto) == estoque0 + 20, "estoque não dobrou");

        // outro pedido com a mesma NF (com zeros à esquerda): pergunta; respondendo Não, o pedido fica em aberto
        u.RespostasConfirmacao.Enqueue(false);
        ReceberPedido("004567");
        u.Ok(u.Viu("CONFIRMAR", "Já existe a compra"), "receber com NF repetida pede confirmação");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compras")) == compras, "respondendo Não, nada é lançado");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos ORDER BY id DESC LIMIT 1")) == "ABERTO", "o pedido continua em aberto para receber depois");
        u.Ok(u.Viu("INFO", "salvo em aberto"), "o usuário é avisado de que o pedido ficou salvo em aberto");
    }
}
