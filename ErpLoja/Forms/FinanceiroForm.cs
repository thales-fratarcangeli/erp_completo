namespace ErpLoja.Forms;

/// <summary>Contas a receber e a pagar: lançamento, baixa, estorno e cancelamento.</summary>
public class FinanceiroForm : Form
{
    static readonly string[] Formas = ["Dinheiro", "PIX", "Cartão de débito", "Cartão de crédito", "Transferência", "Boleto"];

    readonly ComboBox tipo = Ui.Combo(140), situacao = Ui.Combo(150);
    readonly CheckBox usarPeriodo = new() { Text = "Vencimento de", AutoSize = true, Margin = new Padding(10, 10, 0, 4) };
    readonly DateTimePicker de, ate;
    readonly DataGridView grid = Ui.Grid();
    readonly Label filtrado = Ui.Rotulo(""), resumo = Ui.Rotulo("");

    public FinanceiroForm()
    {
        Ui.Estilizar(this, "Financeiro - contas a receber e a pagar", 1200, 720);
        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        de = Ui.Data(inicioMes);
        ate = Ui.Data(inicioMes.AddMonths(1).AddDays(-1));

        tipo.Items.AddRange(["Todas", "A receber", "A pagar"]);
        situacao.Items.AddRange(["Em aberto", "Vencidas", "Pagas", "Canceladas", "Todas"]);
        tipo.SelectedIndex = situacao.SelectedIndex = 0;

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Tipo:"), tipo, Ui.Rotulo("Situação:"), situacao, usarPeriodo, de, Ui.Rotulo("até"), ate,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true),
        ]);
        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Nova conta", (_, _) => NovaConta()),
            Ui.Botao("Baixar (receber/pagar)", (_, _) => Baixar(), primario: true),
            Ui.Botao("Estornar baixa", (_, _) => Estornar()),
            Ui.Botao("Cancelar conta", (_, _) => CancelarConta()),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(grid, "contas")),
            filtrado,
        ]);
        var rodape = Ui.Barra(DockStyle.Bottom);
        resumo.Font = Ui.FonteNegrito;
        rodape.Controls.Add(resumo);
        Ui.ColorirSituacao(grid);

        Controls.Add(grid);
        Controls.Add(acoes);
        Controls.Add(filtros);
        Controls.Add(rodape);

        tipo.SelectedIndexChanged += (_, _) => Carregar();
        situacao.SelectedIndexChanged += (_, _) => Carregar();
        usarPeriodo.CheckedChanged += (_, _) => Carregar();
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Baixar(); };
        Load += (_, _) => Carregar();
    }

    void Carregar()
    {
        var condicoes = new List<string> { "1 = 1" };
        if (tipo.SelectedIndex == 1) condicoes.Add("c.tipo = 'RECEBER'");
        if (tipo.SelectedIndex == 2) condicoes.Add("c.tipo = 'PAGAR'");
        condicoes.Add(situacao.SelectedIndex switch
        {
            0 => "c.status = 'ABERTA'",
            1 => "c.status = 'ABERTA' AND c.vencimento < date('now', 'localtime')",
            2 => "c.status = 'PAGA'",
            3 => "c.status = 'CANCELADA'",
            _ => "1 = 1",
        });
        if (usarPeriodo.Checked)
            condicoes.Add("c.vencimento BETWEEN @p0 AND @p1");

        var dt = Db.Query($"""
            SELECT c.id, c.id AS "Nº", CASE c.tipo WHEN 'RECEBER' THEN 'A receber' ELSE 'A pagar' END AS "Tipo",
                   c.descricao AS "Descrição", COALESCE(cl.nome, f.nome, '') AS "Cliente/Fornecedor",
                   strftime('%d/%m/%Y', c.vencimento) AS "Vencimento", c.valor AS "Valor",
                   CASE WHEN c.status = 'ABERTA' AND c.vencimento < date('now', 'localtime') THEN 'VENCIDA' ELSE c.status END AS "Situação",
                   CASE WHEN c.status = 'ABERTA' AND c.vencimento < date('now', 'localtime')
                        THEN CAST(julianday(date('now', 'localtime')) - julianday(c.vencimento) AS INTEGER) END AS "Dias de atraso",
                   strftime('%d/%m/%Y', c.data_pagamento) AS "Pago em", c.valor_pago AS "Valor pago", c.forma_pagamento AS "Forma"
            FROM contas c
            LEFT JOIN clientes cl ON cl.id = c.cliente_id
            LEFT JOIN fornecedores f ON f.id = c.fornecedor_id
            WHERE {string.Join(" AND ", condicoes)}
            ORDER BY c.vencimento, c.id
            """, Db.Dia(de.Value), Db.Dia(ate.Value));
        grid.DataSource = dt;

        decimal receber = 0, pagar = 0;
        foreach (DataRow r in dt.Rows)
            if (Ui.Txt(r["Tipo"]) == "A receber") receber += Ui.Dec(r["Valor"]);
            else pagar += Ui.Dec(r["Valor"]);
        filtrado.Text = $"{dt.Rows.Count} conta(s) · A receber {Ui.Moeda(receber)} · A pagar {Ui.Moeda(pagar)}";

        var t = Db.Query("""
            SELECT
              COALESCE(SUM(CASE WHEN tipo = 'RECEBER' AND status = 'ABERTA' THEN valor END), 0) AS receber,
              COALESCE(SUM(CASE WHEN tipo = 'PAGAR' AND status = 'ABERTA' THEN valor END), 0) AS pagar,
              COALESCE(SUM(CASE WHEN tipo = 'RECEBER' AND status = 'PAGA' AND data_pagamento >= @p0 THEN valor_pago END), 0) AS recebido,
              COALESCE(SUM(CASE WHEN tipo = 'PAGAR' AND status = 'PAGA' AND data_pagamento >= @p0 THEN valor_pago END), 0) AS pago
            FROM contas
            """, Db.Dia(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))).Rows[0];
        var (aReceber, aPagar) = (Ui.Dec(t["receber"]), Ui.Dec(t["pagar"]));
        resumo.Text = $"Em aberto — a receber: {Ui.Moeda(aReceber)}   a pagar: {Ui.Moeda(aPagar)}   saldo previsto: {Ui.Moeda(aReceber - aPagar)}"
                    + $"        No mês — recebido: {Ui.Moeda(Ui.Dec(t["recebido"]))}   pago: {Ui.Moeda(Ui.Dec(t["pago"]))}";
    }

    (long Id, DataRowView Linha)? Selecionada()
    {
        if (Ui.Linha(grid) is DataRowView r)
            return (Convert.ToInt64(r["id"]), r);
        Ui.Erro("Selecione uma conta.");
        return null;
    }

    void NovaConta()
    {
        using var d = new Dialogo("Nova conta", "conta_nova", "Salvar");
        var tipoConta = d.Adicionar("Tipo:", Ui.Combo());
        tipoConta.Items.AddRange(["A pagar", "A receber"]);
        var descricao = d.Adicionar("Descrição:", new TextBox());
        var valor = d.Adicionar("Valor total (R$):", Ui.Numero());
        var parcelas = d.Adicionar("Parcelas:", Ui.Numero(0, Parametros.MaxParcelas, 1));
        var vencimento = d.Adicionar("1º vencimento:", Ui.Data(DateTime.Today.AddDays(Parametros.PrazoVencimentoDias)));
        var cliente = d.Adicionar("Cliente:", Ui.Combo());
        var fornecedor = d.Adicionar("Fornecedor:", Ui.Combo());
        d.Nota("Ex.: aluguel, energia, salários, serviços, venda avulsa.");
        Ui.Preencher(cliente, Db.Query("SELECT id, nome FROM clientes WHERE ativo = 1 ORDER BY nome"), "(nenhum)");
        Ui.Preencher(fornecedor, Db.Query("SELECT id, nome FROM fornecedores WHERE ativo = 1 ORDER BY nome"), "(nenhum)");
        tipoConta.SelectedIndexChanged += (_, _) =>
        {
            cliente.Enabled = tipoConta.SelectedIndex == 1;
            fornecedor.Enabled = tipoConta.SelectedIndex == 0;
        };
        tipoConta.SelectedIndex = 0;
        d.Validar = () =>
        {
            if (string.IsNullOrWhiteSpace(descricao.Text)) { Ui.Erro("Informe a descrição."); return false; }
            if (valor.Value <= 0) { Ui.Erro("Informe o valor."); return false; }
            return true;
        };
        if (d.ShowDialog(this) != DialogResult.OK)
            return;

        var receber = tipoConta.SelectedIndex == 1;
        Operacoes.LancarConta(receber ? "RECEBER" : "PAGAR", descricao.Text.Trim(), valor.Value, (int)parcelas.Value,
            vencimento.Value.Date, receber ? Ui.Id(cliente) : null, receber ? null : Ui.Id(fornecedor));
        Carregar();
    }

    void Baixar()
    {
        if (Selecionada() is not { } sel)
            return;
        var (id, linha) = sel;
        if (Ui.Txt(linha["Situação"]) is not ("ABERTA" or "VENCIDA"))
        {
            Ui.Erro("Somente contas em aberto podem ser baixadas.");
            return;
        }
        var receber = Ui.Txt(linha["Tipo"]) == "A receber";
        using var d = new Dialogo(receber ? "Receber conta" : "Pagar conta", "conta_baixa", "Confirmar");
        d.Nota($"{linha["Descrição"]} — {Ui.Moeda(Ui.Dec(linha["Valor"]))}");
        var data = d.Adicionar(receber ? "Recebido em:" : "Pago em:", Ui.Data());
        var valor = d.Adicionar("Valor (R$):", Ui.Numero());
        valor.Value = Ui.Dec(linha["Valor"]);
        var forma = d.Adicionar("Forma:", Ui.Combo());
        forma.Items.AddRange(Formas);
        forma.SelectedIndex = 0;
        d.Nota("Juros ou descontos podem ser lançados ajustando o valor.");
        d.Validar = () => valor.Value > 0 || Fail("Informe o valor.");
        if (d.ShowDialog(this) != DialogResult.OK)
            return;
        Operacoes.BaixarConta(id, data.Value.Date, valor.Value, forma.Text);
        Carregar();
    }

    void Estornar()
    {
        if (Selecionada() is not { } sel)
            return;
        var (id, linha) = sel;
        if (Ui.Txt(linha["Situação"]) != "PAGA")
        {
            Ui.Erro("Somente contas pagas podem ser estornadas.");
            return;
        }
        if (Ui.Confirmar("Estornar a baixa? A conta voltará a ficar em aberto."))
        {
            Operacoes.EstornarConta(id);
            Carregar();
        }
    }

    void CancelarConta()
    {
        if (Selecionada() is not { } sel)
            return;
        var (id, linha) = sel;
        if (Ui.Txt(linha["Situação"]) is not ("ABERTA" or "VENCIDA"))
        {
            Ui.Erro("Somente contas em aberto podem ser canceladas.");
            return;
        }
        if (Ui.Confirmar($"Cancelar a conta \"{linha["Descrição"]}\"?"))
        {
            Operacoes.CancelarConta(id);
            Carregar();
        }
    }

    static bool Fail(string msg)
    {
        Ui.Erro(msg);
        return false;
    }
}
