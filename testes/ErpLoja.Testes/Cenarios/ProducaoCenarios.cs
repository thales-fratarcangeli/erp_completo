using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários de produção e logística (localizar campos pelo rótulo, selecionar linhas).</summary>
static class ProducaoLogisticaUtil
{
    /// <summary>Controle que vem logo depois do rótulo (diálogos, barras e cadastros montam rótulo + campo em sequência).</summary>
    public static T CampoApos<T>(Control raiz, string rotulo) where T : Control
    {
        foreach (var container in Usuario.Todos(raiz).Prepend(raiz))
        {
            var lista = container.Controls.Cast<Control>().ToList();
            for (var i = 0; i < lista.Count - 1; i++)
                if (lista[i] is Label l && l.Text.StartsWith(rotulo, StringComparison.OrdinalIgnoreCase) && lista[i + 1] is T t)
                    return t;
        }
        throw new InvalidOperationException($"Campo '{rotulo}' não encontrado em '{raiz.Text}'.");
    }

    /// <summary>Seleciona na grade a linha do registro (coluna id), como um clique do usuário.</summary>
    public static bool SelecionarLinha(DataGridView g, long id)
    {
        foreach (DataGridViewRow r in g.Rows)
            if (r.DataBoundItem is System.Data.DataRowView v && Convert.ToInt64(v["id"]) == id)
            {
                // como um clique: muda a célula atual (a grade seleciona a linha inteira)
                g.CurrentCell = r.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                if (!r.Selected)
                {
                    g.ClearSelection();
                    r.Selected = true;
                }
                Usuario.Esperar(100);
                return true;
            }
        return false;
    }

    public static long Produto(string codigo, string nome, string unidade, decimal custo, decimal estoque, string tipo, decimal preco = 0) =>
        Convert.ToInt64(Db.Scalar("""
            INSERT INTO produtos(codigo, nome, unidade, preco_custo, estoque, tipo, preco_venda) VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6) RETURNING id
            """, codigo, nome, unidade, custo, estoque, tipo, preco));

    public static decimal Dec(string sql, params object?[] p) => Ui.Dec(Db.Scalar(sql, p));

    public static string Erros(Usuario u) => string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => " | " + m.Texto));
}

/// <summary>
/// Encarregado da produção monta a ficha técnica de um bolo pela tela: insumo por código (Enter), insumo repetido soma,
/// perda %, rendimento, custo adicional, custo/margem estimados, F3, produto como insumo de si mesmo e nova OP a partir da ficha.
/// </summary>
public class ProducaoEncarregadoMontaFicha : ICenario
{
    public string Area => "Produção";

    public void Executar(Usuario u)
    {
        var farinha = ProducaoLogisticaUtil.Produto("TINS1", "Farinha teste", "KG", 2, 100, "INSUMO");
        var cacau = ProducaoLogisticaUtil.Produto("TINS2", "Cacau teste", "KG", 10, 3, "INSUMO");
        var bolo = ProducaoLogisticaUtil.Produto("TBOLO", "Bolo teste", "UN", 0, 0, "MERCADORIA", 30);

        var f = u.AbrirTela("FichasTecnicasForm");
        var produtos = Usuario.Achar<DataGridView>(f, g => g.Columns.Contains("Ficha"));
        u.Ok(ProducaoLogisticaUtil.SelecionarLinha(produtos, bolo), "o produto novo aparece na lista das fichas técnicas");
        var codigo = ProducaoLogisticaUtil.CampoApos<TextBox>(f, "Insumo (código)");
        var qtd = ProducaoLogisticaUtil.CampoApos<NumericUpDown>(f, "Qtd.");
        qtd.Value = 2;
        u.Digitar(codigo, "TINS1", enter: true);
        qtd.Value = 1;
        u.Digitar(codigo, "TINS1", enter: true);
        qtd.Value = 0.5m;
        u.Digitar(codigo, "TINS2", enter: true);
        var insumos = Usuario.Achar<DataGridView>(f, g => !g.ReadOnly);
        var lista = (System.ComponentModel.BindingList<InsumoFicha>)insumos.DataSource!;
        u.Ok(lista.Count == 2 && lista.First(i => i.InsumoId == farinha).Quantidade == 3, "insumo repetido soma na mesma linha (2 + 1 = 3 kg)");
        lista.First(i => i.InsumoId == cacau).PerdaPct = 10;
        lista.ResetItem(1);
        ProducaoLogisticaUtil.CampoApos<NumericUpDown>(f, "Rendimento").Value = 2;
        ProducaoLogisticaUtil.CampoApos<NumericUpDown>(f, "Custo adicional").Value = 4;
        Usuario.Esperar();
        // insumos 3 × 2 + 0,5 × 1,1 × 10 = 11,50; lote 15,50; unitário 7,75; margem (30 − 7,75) ÷ 7,75 = 287,1%
        var resumo = Usuario.Todos(f).OfType<Label>().First(l => l.Text.Contains("custo unitário")).Text;
        u.Ok(resumo.Contains(Ui.Moeda(11.5m)) && resumo.Contains(Ui.Moeda(15.5m)) && resumo.Contains(Ui.Moeda(7.75m)) && resumo.Contains("287,1%"),
            "resumo mostra custo dos insumos (com perda), do lote, unitário e a margem: " + resumo);

        // produto como insumo de si mesmo
        u.Digitar(codigo, "TBOLO", enter: true);
        u.Ok(u.Viu("ERRO", "própria ficha") && lista.Count == 2, "o produto não entra como insumo da própria ficha");

        // F3 abre a busca de produtos (cancelada aqui)
        u.QuandoAbrirJanela("Buscar produto", j => j.DialogResult = DialogResult.Cancel);
        u.Teclar(f, Keys.F3);
        Usuario.Esperar(300);
        u.ConferirJanelas();

        u.Clicar(f, "Salvar ficha");
        u.Ok(Ui.Txt(Db.Scalar("SELECT tipo FROM produtos WHERE id = @p0", bolo)) == "PRODUTO_ACABADO"
             && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM ficha_itens fi JOIN fichas_tecnicas ft ON ft.id = fi.ficha_id WHERE ft.produto_id = @p0", bolo) == 2,
            "salvar a ficha grava os insumos e torna a mercadoria um produto acabado");

        // nova OP direto da ficha: 4 unidades = 2 receitas
        u.QuandoAbrirJanela("Nova ordem de produção", d =>
        {
            ProducaoLogisticaUtil.CampoApos<NumericUpDown>(d, "Quantidade a produzir").Value = 4;
            u.Clicar(d, "Criar");
        });
        u.Clicar(f, "Nova ordem de produção");
        Usuario.Esperar(400);
        u.ConferirJanelas();
        var op = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM ordens_producao WHERE produto_id = @p0", bolo) ?? 0L);
        u.Ok(op > 0 && ProducaoLogisticaUtil.Dec("SELECT quantidade_prevista FROM op_itens WHERE op_id = @p0 AND insumo_id = @p1", op, farinha) == 6
             && ProducaoLogisticaUtil.Dec("SELECT quantidade_prevista FROM op_itens WHERE op_id = @p0 AND insumo_id = @p1", op, cacau) == 1.1m,
            "OP criada pela ficha calcula a necessidade (6 kg de farinha; 0,5 × 2 × 1,1 = 1,1 kg de cacau)");
        f.Close();
    }
}

/// <summary>Regressão: ficha técnica circular (A usa B e B usa A) não pode ser salva.</summary>
public class ProducaoFichaCircular : ICenario
{
    public string Area => "Produção";

    public void Executar(Usuario u)
    {
        var acucar = ProducaoLogisticaUtil.Produto("TACU", "Açúcar teste", "KG", 3, 50, "INSUMO");
        var massa = ProducaoLogisticaUtil.Produto("TMASSA", "Massa base teste", "KG", 0, 0, "MERCADORIA");
        var torta = ProducaoLogisticaUtil.Produto("TTORTA", "Torta teste", "UN", 0, 0, "MERCADORIA", 50);
        Producao.SalvarFicha(new Producao.Ficha(null, massa, 1, 0, "", new() { new() { InsumoId = acucar, Quantidade = 1 } }));
        Producao.SalvarFicha(new Producao.Ficha(null, torta, 1, 0, "", new() { new() { InsumoId = massa, Quantidade = 1, Insumo = "Massa base teste" } }));
        try
        {
            Producao.SalvarFicha(new Producao.Ficha(null, massa, 1, 0, "", new()
                { new() { InsumoId = acucar, Quantidade = 1 }, new() { InsumoId = torta, Quantidade = 0.1m, Insumo = "Torta teste" } }));
            u.Ok(false, "ficha circular (massa usa torta, que usa massa) foi aceita");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("circular") && ex.Message.Contains("Torta teste"), "ficha circular é recusada com o nome do insumo: " + ex.Message);
        }
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM ficha_itens fi JOIN fichas_tecnicas ft ON ft.id = fi.ficha_id WHERE ft.produto_id = @p0", massa) == 1,
            "a ficha anterior continua intacta");

        // regressão: perda negativa (barateava o custo) ou absurda era aceita
        try
        {
            Producao.SalvarFicha(new Producao.Ficha(null, torta, 1, 0, "", new() { new() { InsumoId = acucar, Quantidade = 1, PerdaPct = -20, Insumo = "Açúcar teste" } }));
            u.Ok(false, "perda negativa foi aceita na ficha");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("Perda inválida") && ex.Message.Contains("Açúcar teste"), "perda negativa é recusada com o nome do insumo");
        }
    }
}

/// <summary>
/// Encarregado conduz a OP pela tela: imprimir, iniciar, tentar concluir mais que o planejado sem insumo (BLOQUEAR e
/// PERGUNTAR → não), concluir menos que o planejado (consumo proporcional e custo real), cancelar e custo médio.
/// Regressão: a falta de insumo era conferida com o planejado, então produzir a mais deixava o insumo negativo mesmo com BLOQUEAR.
/// </summary>
public class ProducaoEncarregadoConduzOrdem : ICenario
{
    public string Area => "Produção";

    public void Executar(Usuario u)
    {
        var farinha = ProducaoLogisticaUtil.Produto("TINS1", "Farinha teste", "KG", 2, 100, "INSUMO");
        var cacau = ProducaoLogisticaUtil.Produto("TINS2", "Cacau teste", "KG", 10, 3, "INSUMO");
        var bolo = ProducaoLogisticaUtil.Produto("TBOLO", "Bolo teste", "UN", 0, 0, "MERCADORIA", 30);
        Producao.SalvarFicha(new Producao.Ficha(null, bolo, 2, 4, "", new()
            { new() { InsumoId = farinha, Quantidade = 3 }, new() { InsumoId = cacau, Quantidade = 0.5m, PerdaPct = 10 } }));
        var op = Producao.CriarOrdem(bolo, 4, DateTime.Today.AddDays(1), "encomenda");

        var f = u.AbrirTela("OrdensProducaoForm");
        var ordens = Usuario.Achar<DataGridView>(f, g => g.Columns.Contains("Planejado"));
        u.Ok(ProducaoLogisticaUtil.SelecionarLinha(ordens, op), "a OP planejada aparece em aberto");

        string? impresso = null;
        u.QuandoAbrirJanela($"OP nº {op}", j =>
        {
            impresso = string.Join("\n", Usuario.Todos(j).Select(c => c.Text));
            j.DialogResult = DialogResult.Cancel;
            j.Close();
        });
        u.Clicar(f, "Imprimir");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(impresso != null, "Imprimir abre a visualização da OP");
        u.Ok(Producao.Texto(op).Contains("Cacau teste") && Producao.Texto(op).Contains("1,100"), "impressão da OP lista os insumos e a quantidade prevista");

        u.Clicar(f, "Iniciar produção");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) == Producao.EmProducao, "iniciar produção muda a situação");

        void Concluir(decimal quantidade)
        {
            ProducaoLogisticaUtil.SelecionarLinha(ordens, op);
            u.QuandoAbrirJanela($"Concluir OP nº {op}", d =>
            {
                Usuario.Achar<NumericUpDown>(d).Value = quantidade;
                u.Clicar(d, "Concluir");
            });
            u.Clicar(f, "Concluir");
            Usuario.Esperar(300);
            u.ConferirJanelas();
        }

        // 12 unidades consomem 3,3 kg de cacau, mas só há 3: o planejado (4) não falta, o produzido falta
        Db.SalvarConfig("producao_sem_estoque", Parametros.Bloquear);
        Concluir(12);
        u.Ok(u.Viu("ERRO", "bloqueada") && Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) == Producao.EmProducao
             && ProducaoLogisticaUtil.Dec("SELECT estoque FROM produtos WHERE id = @p0", cacau) == 3,
            "BLOQUEAR: produzir mais que o planejado sem insumo é barrado (cacau continua 3)");
        try
        {
            Producao.Concluir(op, 12);
            u.Ok(false, "a camada de dados concluiu a OP sem insumo com BLOQUEAR");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("Cacau teste"), "BLOQUEAR também vale direto na camada de dados");
        }

        Db.SalvarConfig("producao_sem_estoque", Parametros.Perguntar);
        u.RespostasConfirmacao.Enqueue(false);
        Concluir(12);
        u.Ok(u.Viu("CONFIRMAR", "precisa 3,300") && Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) == Producao.EmProducao,
            "PERGUNTAR mostra a necessidade real (3,300) e, respondendo Não, nada é baixado");

        var confirmacoes = u.Mensagens.Count(m => m.Tipo == "CONFIRMAR");
        Concluir(2);
        u.Ok(u.Mensagens.Count(m => m.Tipo == "CONFIRMAR") == confirmacoes, "produzir menos que o planejado com insumo suficiente não pergunta nada");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) == Producao.Concluida
             && ProducaoLogisticaUtil.Dec("SELECT estoque FROM produtos WHERE id = @p0", farinha) == 97
             && ProducaoLogisticaUtil.Dec("SELECT estoque FROM produtos WHERE id = @p0", cacau) == 2.45m
             && ProducaoLogisticaUtil.Dec("SELECT estoque FROM produtos WHERE id = @p0", bolo) == 2,
            "concluir 2 de 4: consome metade (3 kg de farinha, 0,55 kg de cacau) e entra 2 bolos");
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT custo_unitario FROM ordens_producao WHERE id = @p0", op) == 7.75m
             && ProducaoLogisticaUtil.Dec("SELECT preco_custo FROM produtos WHERE id = @p0", bolo) == 7.75m,
            "custo real: (6 + 5,50 + 4 × 2 ÷ 2) ÷ 2 = R$ 7,75 e vira o custo do bolo");

        Usuario.Achar<ComboBox>(f).SelectedIndex = 3; // Todas (a concluída sai de "Em aberto")
        Usuario.Esperar();
        u.Ok(ProducaoLogisticaUtil.SelecionarLinha(ordens, op), "a OP concluída aparece no filtro Todas");
        u.Clicar(f, "Cancelar OP");
        u.Ok(u.Viu("ERRO", "não pode ser cancelada") && Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op)) == Producao.Concluida,
            "OP concluída não pode ser cancelada");

        var op2 = Producao.CriarOrdem(bolo, 2, DateTime.Today, "");
        Db.Exec("UPDATE produtos SET preco_custo = 4 WHERE id = @p0", farinha);
        Producao.Concluir(op2, 2);
        // (3 × 4 + 0,55 × 10 + 4) ÷ 2 = 10,75; médio (2 × 7,75 + 2 × 10,75) ÷ 4 = 9,25
        u.Ok(ProducaoLogisticaUtil.Dec("SELECT preco_custo FROM produtos WHERE id = @p0", bolo) == 9.25m, "segunda produção recalcula o custo médio ponderado (R$ 9,25)");

        var op3 = Producao.CriarOrdem(bolo, 1, DateTime.Today, "");
        f.Close();
        f = u.AbrirTela("OrdensProducaoForm");
        ordens = Usuario.Achar<DataGridView>(f, g => g.Columns.Contains("Planejado"));
        ProducaoLogisticaUtil.SelecionarLinha(ordens, op3);
        u.Clicar(f, "Cancelar OP");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM ordens_producao WHERE id = @p0", op3)) == Producao.Cancelada
             && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM movimentacoes_estoque WHERE referencia_id = @p0 AND origem LIKE '%OP nº%'", op3) == 0,
            "cancelar OP planejada não mexe no estoque");
        f.Close();
    }
}

/// <summary>
/// Regressão: trocar de produto (ou fechar a tela) descartava sem aviso a ficha técnica em edição.
/// O encarregado adiciona um insumo, clica em outro produto, responde Não (continua na ficha) e depois Sim (descarta).
/// </summary>
public class ProducaoFichaNaoSalvaAvisa : ICenario
{
    public string Area => "Produção";

    public void Executar(Usuario u)
    {
        ProducaoLogisticaUtil.Produto("TINS1", "Farinha teste", "KG", 2, 100, "INSUMO");
        var bolo = ProducaoLogisticaUtil.Produto("TBOLO", "Bolo teste", "UN", 0, 0, "MERCADORIA", 30);
        var torta = ProducaoLogisticaUtil.Produto("TTORTA", "Torta teste", "UN", 0, 0, "MERCADORIA", 40);

        var f = u.AbrirTela("FichasTecnicasForm");
        var produtos = Usuario.Achar<DataGridView>(f, g => g.Columns.Contains("Ficha"));
        ProducaoLogisticaUtil.SelecionarLinha(produtos, bolo);
        var titulo = Usuario.Todos(f).OfType<Label>().First(l => l.Text.StartsWith("TBOLO"));
        ProducaoLogisticaUtil.SelecionarLinha(produtos, torta);
        u.Ok(!u.Viu("CONFIRMAR", "não salvas") && titulo.Text.StartsWith("TTORTA"), "sem alteração, trocar de produto não pergunta nada");

        u.Digitar(ProducaoLogisticaUtil.CampoApos<TextBox>(f, "Insumo (código)"), "TINS1", enter: true);
        u.RespostasConfirmacao.Enqueue(false);
        ProducaoLogisticaUtil.SelecionarLinha(produtos, bolo);
        var insumos = Usuario.Achar<DataGridView>(f, g => !g.ReadOnly);
        u.Ok(u.Viu("CONFIRMAR", "não salvas") && titulo.Text.StartsWith("TTORTA") && insumos.Rows.Count == 1 && Ui.IdLinha(produtos) == torta,
            "com insumo não salvo, trocar de produto pergunta; respondendo Não, a ficha em edição continua aberta");

        u.RespostasConfirmacao.Enqueue(false);
        f.Close();
        Usuario.Esperar();
        u.Ok(!f.IsDisposed && f.Visible, "fechar a tela com ficha não salva pergunta; respondendo Não, a tela continua aberta");

        u.RespostasConfirmacao.Enqueue(true);
        ProducaoLogisticaUtil.SelecionarLinha(produtos, bolo);
        u.Ok(titulo.Text.StartsWith("TBOLO") && insumos.Rows.Count == 0
             && ProducaoLogisticaUtil.Dec("SELECT COUNT(*) FROM fichas_tecnicas WHERE produto_id = @p0", torta) == 0,
            "respondendo Sim, descarta e abre o outro produto (nada gravado)");
        f.Close();
    }
}
