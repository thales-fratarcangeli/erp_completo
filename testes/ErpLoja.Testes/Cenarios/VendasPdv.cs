using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários da área Vendas (PDV, pagamento, orçamentos e pedidos).</summary>
static class VendasUtil
{
    /// <summary>Produto vendável com estoque folgado: (id, código, preço).</summary>
    public static (long Id, string Codigo, decimal Preco) Produto(int pular = 0, decimal estoqueMinimo = 10)
    {
        var r = Db.Query("""
            SELECT id, codigo, preco_venda FROM produtos
            WHERE ativo = 1 AND tipo <> 'INSUMO' AND estoque > @p0 AND preco_venda > 1 ORDER BY id LIMIT 1 OFFSET @p1
            """, estoqueMinimo, pular).Rows[0];
        return ((long)r["id"], Ui.Txt(r["codigo"]), Ui.Dec(r["preco_venda"]));
    }

    public static decimal Estoque(long produto) => Ui.Dec(Db.Scalar("SELECT estoque FROM produtos WHERE id = @p0", produto));

    public static long Contar(string sql, params object?[] p) => Convert.ToInt64(Db.Scalar(sql, p));

    /// <summary>Cliente novo com o limite de crédito informado (0 = sem limite).</summary>
    public static long Cliente(string nome, decimal limite) =>
        Convert.ToInt64(Db.Scalar("INSERT INTO pessoas(nome, eh_cliente, limite_credito) VALUES(@p0, 1, @p1) RETURNING id", nome, limite));

    public static TextBox CampoCodigo(Form pdv) => Usuario.Achar<TextBox>(pdv, t => t.PlaceholderText.Contains("código"));

    public static void Lancar(Usuario u, Form pdv, string texto) => u.Digitar(CampoCodigo(pdv), texto, enter: true);

    public static string Total(Form pdv) =>
        Usuario.Todos(pdv).OfType<Label>().First(l => l.Font.Size >= 39).Text;

    /// <summary>Escolhe o cliente da venda pela janela do F2.</summary>
    public static void EscolherCliente(Usuario u, Form pdv, long cliente)
    {
        u.QuandoAbrirJanela("Cliente da venda", d =>
        {
            Ui.Selecionar(Usuario.Achar<ComboBox>(d), cliente);
            u.Clicar(d, "Selecionar");
        });
        u.Teclar(pdv, Keys.F2);
        u.ConferirJanelas();
    }

    /// <summary>Tenta aplicar o desconto (R$) pelo F5; se a janela recusar, cancela. Devolve true se aplicou.</summary>
    public static bool Desconto(Usuario u, Form pdv, decimal valor = 0, decimal percentual = 0)
    {
        var aplicou = false;
        u.QuandoAbrirJanela("Desconto na venda", d =>
        {
            var campos = Usuario.Todos(d).OfType<NumericUpDown>().ToList();
            if (percentual > 0) campos[1].Value = percentual;
            else campos[0].Value = valor;
            u.Clicar(d, "Aplicar");
            aplicou = d.DialogResult == DialogResult.OK;
            if (!aplicou)
                d.DialogResult = DialogResult.Cancel;
        });
        u.Teclar(pdv, Keys.F5);
        u.ConferirJanelas();
        return aplicou;
    }

    /// <summary>F10, escolhe a forma pela tecla (1 a 5) como a operadora faria e confirma com Enter.</summary>
    public static void Pagar(Usuario u, Form pdv, Keys forma, Action<PagamentoForm>? ajustar = null)
    {
        u.QuandoAbrir<PagamentoForm>(pg =>
        {
            Usuario.Esperar(100);
            if (forma != Keys.D1)
                u.Teclar(pg, forma);
            ajustar?.Invoke(pg);
            if (!pg.IsDisposed && pg.DialogResult == DialogResult.None)
                u.Teclar(pg, Keys.Enter);
            if (!pg.IsDisposed && pg.DialogResult == DialogResult.None)
                pg.DialogResult = DialogResult.Cancel; // pagamento recusado: volta ao PDV
        });
        u.Teclar(pdv, Keys.F10);
        Usuario.Esperar(700);
        u.ConferirJanelas();
    }

    /// <summary>Agenda a resposta do simulador de TEF: "Aprovar", "Negar" ou "Cancelar".</summary>
    public static void Tef(Usuario u, string botao) =>
        u.QuandoAbrir<TefSimuladorForm>(f => u.Clicar(f, botao));

    public static string Erros(Usuario u) => string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => " | " + m.Texto));
}

/// <summary>Operadora de caixa apressada: usa só o teclado (N*código, F4, F6, F7) e recebe em PIX teclando "2".</summary>
public class VendasCaixaApressadaTeclado : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        var a = VendasUtil.Produto();
        var b = VendasUtil.Produto(pular: 1);
        var pdv = u.AbrirTela("PdvForm");

        // 3*código e depois o mesmo produto de novo: soma na mesma linha
        VendasUtil.Lancar(u, pdv, "3*" + a.Codigo);
        VendasUtil.Lancar(u, pdv, a.Codigo);
        var grade = Usuario.Achar<DataGridView>(pdv);
        u.Ok(grade.Rows.Count == 1 && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 4), "3*código + código somam 4 unidades na mesma linha");

        // F4 quantidade 2 para o próximo item
        u.QuandoAbrirJanela("Quantidade do próximo item", d =>
        {
            Usuario.Achar<NumericUpDown>(d).Value = 2;
            u.Clicar(d, "OK");
        });
        u.Teclar(pdv, Keys.F4);
        VendasUtil.Lancar(u, pdv, b.Codigo);
        u.Ok(grade.Rows.Count == 2 && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 4 + b.Preco * 2), "F4 = 2 lança o próximo item com 2 unidades");

        // F6 remove o item selecionado (último lançado), confirmando
        u.Teclar(pdv, Keys.F6);
        u.Ok(grade.Rows.Count == 1 && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 4), "F6 remove o item selecionado");

        // quantidade absurda digitada por engano não derruba o PDV
        var errosAntes = u.Mensagens.Count;
        try
        {
            VendasUtil.Lancar(u, pdv, "99999999999999999999999999999999*" + a.Codigo);
            u.Ok(grade.Rows.Count == 1 && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 4), "quantidade absurda é recusada sem alterar a venda");
        }
        catch (Exception ex)
        {
            u.Ok(false, "quantidade absurda derruba o PDV: " + ex.GetType().Name);
        }
        VendasUtil.CampoCodigo(pdv).Clear();

        // pagamento em PIX teclando 2 (TEF simulador aprova)
        var vendas = VendasUtil.Contar("SELECT COUNT(*) FROM vendas");
        VendasUtil.Tef(u, "Aprovar");
        VendasUtil.Pagar(u, pdv, Keys.D2);
        var ultima = Db.Query("SELECT id, forma_pagamento, total FROM vendas ORDER BY id DESC LIMIT 1").Rows[0];
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas + 1 && Ui.Txt(ultima["forma_pagamento"]) == "PIX",
            "F10 e tecla 2 escolhem PIX (a venda foi gravada como " + Ui.Txt(ultima["forma_pagamento"]) + ")" + VendasUtil.Erros(u));
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM transacoes_tef WHERE venda_id = @p0", ultima["id"])) == "CONFIRMADA", "transação TEF aprovada fica CONFIRMADA com a venda");
        u.Ok(grade.Rows.Count == 0, "PDV volta a ficar livre depois da venda");

        // F7 cancela venda em andamento (respondendo Não mantém; Sim limpa)
        VendasUtil.Lancar(u, pdv, a.Codigo);
        u.RespostasConfirmacao.Enqueue(false);
        u.Teclar(pdv, Keys.F7);
        u.Ok(grade.Rows.Count == 1, "F7 respondendo Não mantém a venda");
        u.Teclar(pdv, Keys.F7);
        u.Ok(grade.Rows.Count == 0 && VendasUtil.Total(pdv) == Ui.Moeda(0), "F7 respondendo Sim cancela a venda em andamento");
        pdv.Close();
    }
}

/// <summary>TEF pelo simulador: negar e cancelar não gravam a venda e mantêm os itens; aprovar grava. Débito e crédito parcelado.</summary>
public class VendasTefSimulador : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        var a = VendasUtil.Produto();
        var pdv = u.AbrirTela("PdvForm");
        VendasUtil.Lancar(u, pdv, "2*" + a.Codigo);
        var vendas = VendasUtil.Contar("SELECT COUNT(*) FROM vendas");

        VendasUtil.Tef(u, "Negar");
        VendasUtil.Pagar(u, pdv, Keys.D3);
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas, "TEF negado não grava a venda");
        u.Ok(u.Viu("ERRO", "não aprovado"), "TEF negado avisa o operador");
        u.Ok(Usuario.Achar<DataGridView>(pdv).Rows.Count == 1, "TEF negado mantém os itens no PDV");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM transacoes_tef ORDER BY id DESC LIMIT 1")) == "NEGADA", "transação negada registrada como NEGADA");

        VendasUtil.Tef(u, "Cancelar");
        VendasUtil.Pagar(u, pdv, Keys.D3);
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas, "TEF cancelado pelo operador não grava a venda");

        VendasUtil.Tef(u, "Aprovar");
        VendasUtil.Pagar(u, pdv, Keys.D4, pg =>
        {
            var parcelas = Usuario.Todos(pg).OfType<NumericUpDown>().FirstOrDefault(n => n.Visible && n.DecimalPlaces == 0);
            if (parcelas != null) parcelas.Value = 3;
        });
        var venda = Db.Query("SELECT id, forma_pagamento FROM vendas ORDER BY id DESC LIMIT 1").Rows[0];
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas + 1 && Ui.Txt(venda["forma_pagamento"]) == "Cartão de crédito",
            "tecla 4 = crédito, aprovado no TEF, grava a venda" + VendasUtil.Erros(u));
        u.Ok(VendasUtil.Contar("SELECT parcelas FROM transacoes_tef WHERE venda_id = @p0", venda["id"]) == 3, "crédito parcelado em 3x chega ao TEF");
        u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM contas WHERE venda_id = @p0 AND status = 'PAGA'", venda["id"]) == 1,
            "crédito no TEF gera a conta a receber já paga (a operadora recebe da adquirente)");
        pdv.Close();
    }
}

/// <summary>Desconto: usuário comum não passa do limite (nem removendo itens depois); administrador pode.</summary>
public class VendasDescontoLimite : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        Db.SalvarConfig("desconto_maximo_pct", "10");
        var a = VendasUtil.Produto();
        var b = VendasUtil.Produto(pular: 1);
        Sessao.Admin = false;
        try
        {
            var pdv = u.AbrirTela("PdvForm");
            VendasUtil.Lancar(u, pdv, "4*" + a.Codigo);
            VendasUtil.Lancar(u, pdv, b.Codigo);
            var subtotal = a.Preco * 4 + b.Preco;

            u.Ok(!VendasUtil.Desconto(u, pdv, percentual: 30), "usuário comum: 30% de desconto é recusado");
            u.Ok(u.Viu("ERRO", "acima do limite"), "a recusa explica o limite");
            u.Ok(VendasUtil.Total(pdv) == Ui.Moeda(subtotal), "desconto recusado não altera o total");

            var permitido = Math.Round(subtotal * 0.10m, 2, MidpointRounding.ToZero);
            u.Ok(VendasUtil.Desconto(u, pdv, valor: permitido), "usuário comum: desconto dentro do limite é aceito");

            // o vendedor remove o item maior: o mesmo desconto em R$ agora passa do limite
            var grade = Usuario.Achar<DataGridView>(pdv);
            grade.ClearSelection();
            grade.CurrentCell = grade.Rows[0].Cells[1];
            u.Teclar(pdv, Keys.F6);
            var vendas = VendasUtil.Contar("SELECT COUNT(*) FROM vendas");
            // se a janela de pagamento abrir, a vigia do teste a cancela e reprova ("janela inesperada")
            u.Teclar(pdv, Keys.F10);
            var gravada = VendasUtil.Contar("SELECT COUNT(*) FROM vendas") > vendas;
            u.Ok(!gravada && u.Viu("ERRO", "Ajuste o desconto"),
                "removendo item, o desconto que passou do limite é barrado antes do pagamento (F5 para ajustar)");
            u.Teclar(pdv, Keys.F7);
            pdv.Close();
            Usuario.Esperar();

            Sessao.Admin = true;
            var pdv2 = u.AbrirTela("PdvForm");
            VendasUtil.Lancar(u, pdv2, "2*" + a.Codigo);
            u.Ok(VendasUtil.Desconto(u, pdv2, percentual: 50), "administrador pode dar 50% de desconto");
            VendasUtil.Pagar(u, pdv2, Keys.D1);
            u.Ok(Ui.Dec(Db.Scalar("SELECT total FROM vendas ORDER BY id DESC LIMIT 1")) == Math.Round(a.Preco * 2, 2) - Math.Round(a.Preco * 2 * 0.5m, 2),
                "venda com 50% de desconto do administrador gravada com o total certo");
            pdv2.Close();
        }
        finally
        {
            Sessao.Admin = true;
        }
    }
}

/// <summary>A prazo: sem cliente é recusado; com cliente gera parcelas; limite de crédito barra o usuário comum e libera o administrador.</summary>
public class VendasAPrazoLimiteCredito : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        Db.SalvarConfig("imprimir_cupom", "NUNCA");
        var a = VendasUtil.Produto();
        var cliente = VendasUtil.Cliente("Cliente Limite Teste", limite: Math.Round(a.Preco * 3, 2));
        var vendas = VendasUtil.Contar("SELECT COUNT(*) FROM vendas");
        Sessao.Admin = false;
        try
        {
            var pdv = u.AbrirTela("PdvForm");
            VendasUtil.Lancar(u, pdv, "2*" + a.Codigo);
            VendasUtil.Pagar(u, pdv, Keys.D5);
            u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas && u.Viu("ERRO", "cliente"), "a prazo sem cliente é recusado pedindo o cliente");

            VendasUtil.EscolherCliente(u, pdv, cliente);
            VendasUtil.Pagar(u, pdv, Keys.D5, pg =>
            {
                var parcelas = Usuario.Todos(pg).OfType<NumericUpDown>().First(n => n.Visible && n.DecimalPlaces == 0);
                parcelas.Value = 2;
            });
            var venda = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM vendas"));
            u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas + 1, "a prazo com cliente dentro do limite grava a venda" + VendasUtil.Erros(u));
            u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM contas WHERE venda_id = @p0 AND status = 'ABERTA' AND cliente_id = @p1", venda, cliente) == 2,
                "a prazo em 2x gera 2 parcelas em aberto no nome do cliente");
            u.Ok(Ui.Dec(Db.Scalar("SELECT SUM(valor) FROM contas WHERE venda_id = @p0", venda)) == a.Preco * 2, "as parcelas somam o total da venda");

            // segunda compra estoura o limite (já há 2× preço em aberto, limite 3× preço)
            VendasUtil.EscolherCliente(u, pdv, cliente);
            VendasUtil.Lancar(u, pdv, "2*" + a.Codigo);
            VendasUtil.Pagar(u, pdv, Keys.D5);
            u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas + 1 && u.Viu("ERRO", "Limite de crédito"), "usuário comum não vende acima do limite de crédito");
            u.Ok(Usuario.Achar<DataGridView>(pdv).Rows.Count == 1, "venda recusada por limite continua no PDV para outra forma de pagamento");

            Sessao.Admin = true;
            VendasUtil.Pagar(u, pdv, Keys.D5);
            u.Ok(VendasUtil.Contar("SELECT COUNT(*) FROM vendas") == vendas + 2, "administrador vende acima do limite de crédito" + VendasUtil.Erros(u));
            pdv.Close();
        }
        finally
        {
            Sessao.Admin = true;
        }
    }
}

/// <summary>Venda sem estoque: PERGUNTAR (Não/Sim), BLOQUEAR e PERMITIR.</summary>
public class VendasSemEstoque : ICenario
{
    public string Area => "Vendas";

    public void Executar(Usuario u)
    {
        var a = VendasUtil.Produto(estoqueMinimo: 0);
        Db.Exec("UPDATE produtos SET estoque = 2 WHERE id = @p0", a.Id);
        var pdv = u.AbrirTela("PdvForm");
        var grade = Usuario.Achar<DataGridView>(pdv);

        Db.SalvarConfig("venda_sem_estoque", Parametros.Perguntar);
        VendasUtil.Lancar(u, pdv, "2*" + a.Codigo);
        u.RespostasConfirmacao.Enqueue(false);
        VendasUtil.Lancar(u, pdv, a.Codigo);
        u.Ok(u.Viu("CONFIRMAR", "Estoque insuficiente") && grade.Rows.Count == 1 && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 2),
            "PERGUNTAR: 3ª unidade (estoque 2) pergunta; respondendo Não, não lança");
        VendasUtil.CampoCodigo(pdv).Clear();
        VendasUtil.Lancar(u, pdv, a.Codigo);
        u.Ok(VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 3), "PERGUNTAR: respondendo Sim, lança");

        Db.SalvarConfig("venda_sem_estoque", Parametros.Bloquear);
        VendasUtil.Lancar(u, pdv, a.Codigo);
        u.Ok(u.Viu("ERRO", "bloqueada") && VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 3), "BLOQUEAR: não lança e explica que está bloqueado");
        VendasUtil.CampoCodigo(pdv).Clear();

        Db.SalvarConfig("venda_sem_estoque", Parametros.Permitir);
        var confirmacoes = u.Mensagens.Count(m => m.Tipo == "CONFIRMAR");
        VendasUtil.Lancar(u, pdv, a.Codigo);
        u.Ok(VendasUtil.Total(pdv) == Ui.Moeda(a.Preco * 4) && u.Mensagens.Count(m => m.Tipo == "CONFIRMAR") == confirmacoes,
            "PERMITIR: lança sem perguntar");
        u.RespostaConfirmacao = true;
        pdv.Close();
    }
}
