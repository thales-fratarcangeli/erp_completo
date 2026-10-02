using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

static class VendasPedidosUtil
{
    public static DataGridView Lista(Form f) => Usuario.Achar<DataGridView>(f);

    /// <summary>Seleciona na lista a linha do documento (como clicar nela).</summary>
    public static bool Selecionar(Form f, long id)
    {
        var g = Lista(f);
        foreach (DataGridViewRow r in g.Rows)
            if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == id)
            {
                g.ClearSelection();
                g.CurrentCell = r.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                r.Selected = true;
                Usuario.Esperar(50);
                return true;
            }
        return false;
    }

    public static void Situacao(Usuario u, Form f, string texto)
    {
        var combo = Usuario.Achar<ComboBox>(f, c => c.Items.Contains(texto));
        combo.SelectedItem = texto;
        Usuario.Esperar();
    }

    /// <summary>Agenda a janela "Faturar pedido": escolhe a forma e confirma; se recusar, cancela. Informa se aceitou.</summary>
    public static void Faturar(Usuario u, string forma, Action<bool> aceitou)
    {
        u.QuandoAbrirJanela("Faturar pedido", d =>
        {
            var cb = Usuario.Achar<ComboBox>(d);
            cb.SelectedItem = forma;
            u.Clicar(d, "Confirmar");
            var ok = d.DialogResult == DialogResult.OK;
            if (!ok)
                d.DialogResult = DialogResult.Cancel;
            aceitou(ok);
        });
    }
}

/// <summary>
/// Vendedor novato faz um orçamento com desconto exagerado (recusado), corrige; o gerente aprova, fatura, consulta o
/// comprovante e cancela a venda (estoque, contas e pedido voltam); refaturar a prazo acima do limite é barrado.
/// </summary>
public class VendasOrcamentoAteFaturar : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("desconto_maximo_pct", "10");
        Db.SalvarConfig("validade_orcamento_dias", "15");
        var a = VendasUtil.Produto();
        var cliente = VendasUtil.Cliente("Cliente Orçamento Teste", limite: 1);
        var estoqueInicial = VendasUtil.Estoque(a.Id);
        var subtotal = a.Preco * 2;
        long orcamento = 0;

        // ---------- vendedor novato (usuário comum)
        Sessao.Admin = false;
        try
        {
            var orcs = u.AbrirTela("pedidos_ORCAMENTO");
            u.QuandoAbrir<PedidoEditorForm>(ed =>
            {
                Ui.Selecionar(Usuario.Achar<ComboBox>(ed), cliente);
                var cod = Usuario.Achar<TextBox>(ed);
                u.Digitar(cod, a.Codigo, enter: true);
                u.Digitar(cod, a.Codigo, enter: true);
                var desconto = Usuario.Todos(ed).OfType<NumericUpDown>().Where(n => n.DecimalPlaces == 2).ToList()[0];
                desconto.Value = Math.Round(subtotal * 0.5m, 2);
                Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
                u.Ok(ed.DialogResult != DialogResult.OK && u.Viu("ERRO", "acima do limite"), "orçamento com 50% de desconto é recusado para usuário comum");
                desconto.Value = Math.Round(subtotal * 0.05m, 2);
                Usuario.Achar<Button>(ed, b => b.Text == "Salvar").PerformClick();
                u.Ok(ed.DialogResult == DialogResult.OK, "orçamento com 5% de desconto é salvo" + VendasUtil.Erros(u));
                if (ed.DialogResult != DialogResult.OK)
                    ed.DialogResult = DialogResult.Cancel;
            });
            u.Clicar(orcs, "Novo");
            u.ConferirJanelas();
            orcamento = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM pedidos WHERE tipo = 'ORCAMENTO'"));
            var o = Pedidos.Carregar(orcamento);
            u.Ok(o.Itens.Count == 1 && o.Itens[0].Quantidade == 2, "código lançado duas vezes soma na mesma linha do orçamento");
            u.Ok(o.Validade?.Date == DateTime.Today.AddDays(15), "validade padrão do orçamento = hoje + validade_orcamento_dias");
            u.Ok(VendasUtil.Estoque(a.Id) == estoqueInicial, "orçamento não mexe no estoque");
            orcs.Close();
        }
        finally
        {
            Sessao.Admin = true;
        }

        // ---------- gerente aprova
        var tela = u.AbrirTela("pedidos_ORCAMENTO");
        u.Ok(VendasPedidosUtil.Selecionar(tela, orcamento), "orçamento aparece em aberto na lista");
        u.Clicar(tela, "Aprovar");
        var pedido = Convert.ToInt64(Db.Scalar("SELECT id FROM pedidos WHERE origem_id = @p0", orcamento) ?? 0L);
        u.Ok(pedido > 0 && Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", orcamento)) == "APROVADO", "aprovar gera o pedido de venda e marca o orçamento APROVADO");
        VendasPedidosUtil.Situacao(u, tela, "Todos");
        VendasPedidosUtil.Selecionar(tela, orcamento);
        var errosAntes = u.Mensagens.Count(m => m.Tipo == "ERRO");
        u.Clicar(tela, "Aprovar");
        u.Ok(u.Mensagens.Count(m => m.Tipo == "ERRO") == errosAntes + 1 && VendasUtil.Contar("SELECT COUNT(*) FROM pedidos WHERE origem_id = @p0", orcamento) == 1,
            "aprovar de novo um orçamento já aprovado é recusado (não gera pedido duplicado)");
        tela.Close();

        // ---------- faturar o pedido em dinheiro
        var pedidos = u.AbrirTela("pedidos_VENDA");
        u.Ok(VendasPedidosUtil.Selecionar(pedidos, pedido), "pedido gerado aparece em Pedidos de venda");
        var aceitou = false;
        VendasPedidosUtil.Faturar(u, "Dinheiro", ok => aceitou = ok);
        u.RespostasConfirmacao.Enqueue(false); // não emitir a nota agora
        u.Clicar(pedidos, "Faturar");
        u.ConferirJanelas();
        var venda = Convert.ToInt64(Db.Scalar("SELECT COALESCE(venda_id, 0) FROM pedidos WHERE id = @p0", pedido));
        var totalPedido = Ui.Dec(Db.Scalar("SELECT total FROM pedidos WHERE id = @p0", pedido));
        u.Ok(aceitou && venda > 0 && Ui.Dec(Db.Scalar("SELECT total FROM vendas WHERE id = @p0", venda)) == totalPedido,
            "faturar gera a venda com o total do pedido" + VendasUtil.Erros(u));
        u.Ok(VendasUtil.Estoque(a.Id) == estoqueInicial - 2, "faturar baixa o estoque");
        pedidos.Close();

        // ---------- comprovante e cancelamento em Vendas realizadas
        var vendas = u.AbrirTela("vendas");
        u.Ok(VendasPedidosUtil.Selecionar(vendas, venda), "venda faturada aparece em Vendas realizadas");
        var comprovante = "";
        u.QuandoAbrir<CupomForm>(c => { comprovante = Usuario.Achar<TextBox>(c).Text; c.Close(); });
        u.Clicar(vendas, "Comprovante");
        u.Ok(comprovante.Contains($"Venda nº {venda}") && comprovante.Contains("Cliente Orçamento Teste"), "comprovante mostra o nº da venda e o cliente");

        u.RespostaConfirmacao = true;
        u.Clicar(vendas, "Cancelar venda");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM vendas WHERE id = @p0", venda)) == "CANCELADA", "administrador cancela a venda");
        u.Ok(VendasUtil.Estoque(a.Id) == estoqueInicial, "cancelamento devolve o estoque");
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM contas WHERE venda_id = @p0 AND status <> 'CANCELADA'", venda) == 0, "cancelamento cancela as contas da venda");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", pedido)) == "ABERTO", "cancelamento reabre o pedido de origem");
        VendasPedidosUtil.Selecionar(vendas, venda);
        var errosAntes2 = u.Mensagens.Count(m => m.Tipo == "ERRO");
        u.Clicar(vendas, "Cancelar venda");
        u.Ok(u.Mensagens.Count(m => m.Tipo == "ERRO") == errosAntes2 + 1, "cancelar de novo avisa que já está cancelada");
        vendas.Close();

        // ---------- refaturar a prazo acima do limite (limite R$ 1,00) com usuário comum
        Sessao.Admin = false;
        try
        {
            var p2 = u.AbrirTela("pedidos_VENDA");
            VendasPedidosUtil.Selecionar(p2, pedido);
            var aceitou2 = true;
            VendasPedidosUtil.Faturar(u, Operacoes.APrazo, ok => aceitou2 = ok);
            u.RespostasConfirmacao.Clear();
            u.RespostaConfirmacao = false;
            u.Clicar(p2, "Faturar");
            u.ConferirJanelas();
            u.Ok(!aceitou2 && Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", pedido)) == "ABERTO" && u.Viu("ERRO", "Limite de crédito"),
                "usuário comum não fatura pedido a prazo acima do limite de crédito do cliente");
            p2.Close();
        }
        finally
        {
            Sessao.Admin = true;
            u.RespostaConfirmacao = true;
        }
    }
}

/// <summary>Orçamento vencido: aparece como VENCIDO, duplicar renova a validade, aprovar pede confirmação.</summary>
public class VendasOrcamentoVencido : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        var a = VendasUtil.Produto();
        var p = new Pedido { Tipo = Pedidos.Orcamento, Data = DateTime.Now.AddDays(-20), Validade = DateTime.Today.AddDays(-5), Forma = "Dinheiro" };
        p.Itens.Add(new PedidoItem { ProdutoId = a.Id, Codigo = a.Codigo, Produto = "x", Quantidade = 1, Unitario = a.Preco });
        var id = Pedidos.Salvar(p);

        var tela = u.AbrirTela("pedidos_ORCAMENTO");
        u.Ok(!VendasPedidosUtil.Selecionar(tela, id), "orçamento vencido não aparece entre os em aberto");
        VendasPedidosUtil.Situacao(u, tela, "Vencidos");
        u.Ok(VendasPedidosUtil.Selecionar(tela, id) && Ui.Txt(Ui.Linha(VendasPedidosUtil.Lista(tela))!["Situação"]) == "VENCIDO", "filtro Vencidos mostra o orçamento como VENCIDO");

        u.QuandoAbrir<PedidoEditorForm>(ed => u.Clicar(ed, "Fechar"));
        u.Clicar(tela, "Duplicar");
        u.ConferirJanelas();
        var copia = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM pedidos WHERE tipo = 'ORCAMENTO'"));
        var c = Pedidos.Carregar(copia);
        u.Ok(copia != id && c.Status == "ABERTO" && c.Validade?.Date == DateTime.Today.AddDays(Parametros.ValidadeOrcamentoDias) && c.Itens.Count == 1,
            "duplicar o orçamento vencido cria um novo em aberto com validade renovada");

        VendasPedidosUtil.Situacao(u, tela, "Vencidos");
        VendasPedidosUtil.Selecionar(tela, id);
        u.RespostasConfirmacao.Enqueue(false);
        u.Clicar(tela, "Aprovar");
        u.Ok(u.Viu("CONFIRMAR", "vencido") && Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", id)) == "ABERTO",
            "aprovar orçamento vencido pede confirmação; respondendo Não, nada muda");
        VendasPedidosUtil.Selecionar(tela, id);
        u.Clicar(tela, "Aprovar");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM pedidos WHERE id = @p0", id)) == "APROVADO", "confirmando, o orçamento vencido é aprovado");
        tela.Close();
    }
}

/// <summary>Gerente fatura um pedido com frete e confere o comprovante: subtotal − desconto + frete tem de fechar com o total.</summary>
public class VendasComprovanteComFrete : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        var a = VendasUtil.Produto();
        var p = new Pedido { Tipo = Pedidos.Venda, Forma = "Dinheiro", Desconto = 1, Frete = 15 };
        p.Itens.Add(new PedidoItem { ProdutoId = a.Id, Codigo = a.Codigo, Produto = "x", Quantidade = 2, Unitario = a.Preco });
        var pedido = Pedidos.Salvar(p);
        var venda = Pedidos.Faturar(pedido, "Dinheiro", 1, DateTime.Today);

        var tela = u.AbrirTela("vendas");
        VendasPedidosUtil.Selecionar(tela, venda);
        var texto = "";
        u.QuandoAbrir<CupomForm>(c => { texto = Usuario.Achar<TextBox>(c).Text; c.Close(); });
        u.Clicar(tela, "Comprovante");
        u.ConferirJanelas();
        u.Ok(texto.Contains("Frete") && texto.Contains("15,00"), "comprovante de venda com frete mostra a linha do frete");
        u.Ok(texto.Contains((a.Preco * 2 - 1 + 15).ToString("N2")), "comprovante mostra o total com frete");
        tela.Close();
    }
}
