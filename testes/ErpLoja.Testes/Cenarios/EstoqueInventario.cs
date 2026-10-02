using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O estoquista abre a tela de Estoque de manhã, filtra o produto e faz a contagem. Enquanto conta, o caixa vende
/// 2 unidades (a tela não foi atualizada). Ao "Definir saldo" com o contado, o saldo final tem que ser o contado.
/// </summary>
public class EstoqueInventarioComTelaDesatualizada : ICenario
{
    public string Area => "Estoque";

    static void Ajustar(Usuario u, Form tela, int operacao, decimal quantidade, string motivo)
    {
        u.QuandoAbrirJanela("Ajustar estoque", d =>
        {
            ComprasUtil.CampoApos<ComboBox>(d, "Operação:").SelectedIndex = operacao;
            ComprasUtil.CampoApos<NumericUpDown>(d, "Quantidade:").Value = quantidade;
            ComprasUtil.CampoApos<TextBox>(d, "Motivo:").Text = motivo;
            u.Clicar(d, "Aplicar");
        });
        u.Clicar(tela, "Ajustar estoque");
        Usuario.Esperar(300);
        u.ConferirJanelas();
    }

    public void Executar(Usuario u)
    {
        var (produto, codigo) = ComprasUtil.Produtos(1)[0];
        Db.Exec("UPDATE produtos SET estoque = 30 WHERE id = @p0", produto);
        var nome = Ui.Txt(Db.Scalar("SELECT nome FROM produtos WHERE id = @p0", produto));

        var tela = u.AbrirTela("EstoqueForm");
        var grid = Usuario.Achar<DataGridView>(tela);
        u.Digitar(ComprasUtil.CampoApos<TextBox>(tela, "Pesquisar:"), nome);
        u.Ok(grid.Rows.Cast<DataGridViewRow>().Count(r => r.Visible) >= 1, "filtro pelo nome mostra o produto");
        var resumo = Usuario.Achar<Label>(tela, l => l.Text.Contains("produto(s)")).Text;
        u.Ok(resumo.StartsWith($"{grid.Rows.Count} produto(s)"), $"resumo acompanha a pesquisa ({resumo}; {grid.Rows.Count} na grade)");
        u.Ok(ComprasUtil.SelecionarLinha(grid, produto), "produto selecionado na grade");

        // venda no caixa enquanto a tela está aberta (sem atualizar)
        Operacoes.RegistrarVenda(null, [new ItemDoc(produto, 2, 10, 5)], 0, "Dinheiro", 1, DateTime.Today);
        u.Ok(ComprasUtil.Estoque(produto) == 28, "venda baixou 2 (saldo real 28)");

        Ajustar(u, tela, 2, 25, "Inventário mensal");
        u.Ok(ComprasUtil.Estoque(produto) == 25, $"definir saldo grava exatamente o contado (saldo {ComprasUtil.Estoque(produto)})");
        var mov = Db.Query("SELECT tipo, quantidade, saldo, origem FROM movimentacoes_estoque WHERE produto_id = @p0 ORDER BY id DESC LIMIT 1", produto).Rows[0];
        u.Ok(Ui.Txt(mov["tipo"]) == "AJUSTE" && Ui.Dec(mov["quantidade"]) == 3 && Ui.Dec(mov["saldo"]) == 25, "movimentação AJUSTE de 3 com saldo 25");
        u.Ok(Ui.Txt(mov["origem"]).Contains("Inventário mensal"), "motivo registrado na origem");
        u.Ok(Ui.Dec(Usuario.Achar<DataGridView>(tela, g => g != grid).Rows.Cast<DataGridViewRow>().Select(r => ((System.Data.DataRowView)r.DataBoundItem!)["Saldo após"]).FirstOrDefault()) == 25,
            "histórico do produto mostra o ajuste logo após aplicar");

        // contar de novo e achar o mesmo saldo: nada é gravado
        var movs = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM movimentacoes_estoque WHERE produto_id = @p0", produto));
        Ajustar(u, tela, 2, 25, "Recontagem");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM movimentacoes_estoque WHERE produto_id = @p0", produto)) == movs, "saldo igual ao atual não gera movimentação");
        u.Ok(u.Viu("INFO", "igual ao atual"), "avisa que nada foi alterado");

        // motivo em branco é recusado (o diálogo continua aberto; o estoquista desiste)
        u.QuandoAbrirJanela("Ajustar estoque", d =>
        {
            ComprasUtil.CampoApos<NumericUpDown>(d, "Quantidade:").Value = 1;
            u.Clicar(d, "Aplicar");
            d.DialogResult = DialogResult.Cancel;
        });
        u.Clicar(tela, "Ajustar estoque");
        Usuario.Esperar(300);
        u.Ok(u.Viu("ERRO", "motivo"), "ajuste sem motivo é recusado");
        u.Ok(ComprasUtil.Estoque(produto) == 25, "nada mudou sem motivo");

        // inventário zerado (produto não encontrado na prateleira)
        Ajustar(u, tela, 2, 0, "Não encontrado na contagem");
        u.Ok(ComprasUtil.Estoque(produto) == 0, "definir saldo 0 zera o estoque");
        tela.Close();
    }
}

/// <summary>
/// O dono registra uma perda maior que o saldo (erro de digitação: 100 em vez de 10). O sistema pergunta antes
/// de deixar o saldo negativo; respondendo "Não", nada é gravado. Depois, "Somente estoque baixo" lista o produto.
/// </summary>
public class EstoqueSaidaMaiorQueSaldo : ICenario
{
    public string Area => "Estoque";

    public void Executar(Usuario u)
    {
        var (produto, _) = ComprasUtil.Produtos(1)[0];
        Db.Exec("UPDATE produtos SET estoque = 12, estoque_minimo = 5 WHERE id = @p0", produto);

        var tela = u.AbrirTela("EstoqueForm");
        var grid = Usuario.Achar<DataGridView>(tela);
        ComprasUtil.SelecionarLinha(grid, produto);

        u.RespostasConfirmacao.Enqueue(false);
        u.QuandoAbrirJanela("Ajustar estoque", d =>
        {
            ComprasUtil.CampoApos<ComboBox>(d, "Operação:").SelectedIndex = 1;
            ComprasUtil.CampoApos<NumericUpDown>(d, "Quantidade:").Value = 100;
            ComprasUtil.CampoApos<TextBox>(d, "Motivo:").Text = "Quebra";
            u.Clicar(d, "Aplicar");
        });
        u.Clicar(tela, "Ajustar estoque");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(u.Viu("CONFIRMAR", "negativo"), "pergunta antes de deixar o saldo negativo");
        u.Ok(ComprasUtil.Estoque(produto) == 12, "respondendo Não, o saldo não muda");

        u.QuandoAbrirJanela("Ajustar estoque", d =>
        {
            ComprasUtil.CampoApos<ComboBox>(d, "Operação:").SelectedIndex = 1;
            ComprasUtil.CampoApos<NumericUpDown>(d, "Quantidade:").Value = 10;
            ComprasUtil.CampoApos<TextBox>(d, "Motivo:").Text = "Quebra";
            u.Clicar(d, "Aplicar");
        });
        u.Clicar(tela, "Ajustar estoque");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(ComprasUtil.Estoque(produto) == 2, "saída de 10 deixa saldo 2");

        Usuario.Achar<CheckBox>(tela, c => c.Text.StartsWith("Somente estoque baixo")).Checked = true;
        Usuario.Esperar();
        var linhas = grid.Rows.Cast<DataGridViewRow>().Select(r => (System.Data.DataRowView)r.DataBoundItem!).ToList();
        var minha = linhas.FirstOrDefault(v => Convert.ToInt64(v["id"]) == produto);
        u.Ok(minha != null && Ui.Txt(minha["Situação"]) == "BAIXO" && Ui.Dec(minha["Sugestão de compra"]) == 3, "somente baixo lista o produto: BAIXO, sugestão de compra 3");
        u.Ok(linhas.All(v => Ui.Txt(v["Situação"]) != "OK"), "somente baixo não lista produtos OK");
        tela.Close();
    }
}
