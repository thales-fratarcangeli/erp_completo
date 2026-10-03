namespace ErpLoja.Forms;

/// <summary>
/// Ordens de serviço: abrir, acompanhar etapas e prazos (painel com atrasadas e do dia), entregar, faturar e imprimir.
/// </summary>
public class OrdensServicoForm : Form, IFiltroInicial
{
    static readonly string[] Filtros = ["Em andamento", "Atrasadas", "Entrega hoje", "Prontas para entregar", "Entregues sem faturar",
        "Entregues", "Canceladas", "Todas"];

    readonly ComboBox situacao = Ui.Combo(190);
    readonly TextBox busca = Validacao.Caixa(Formatos.Busca, "Pesquisa", largura: 220);
    readonly DataGridView ordens = Ui.Grid(), historico = Ui.Grid();
    readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    readonly Label resumo = Ui.Rotulo("");

    public OrdensServicoForm()
    {
        Ui.Estilizar(this, "Ordens de serviço", 1200, 720, nameof(OrdensServicoForm));
        situacao.Items.AddRange(Filtros);
        situacao.SelectedIndex = 0;
        busca.PlaceholderText = "Nº, cliente ou serviço";

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Mostrar:"), situacao, Ui.Rotulo("Pesquisar:"), busca,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true), resumo,
        ]);
        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Nova OS", (_, _) => { if (Editar(this, null) is long id) Carregar(id); }, primario: true),
            Ui.Botao("Editar", (_, _) => { if (Selecionada() is long id && Editar(this, id) is long) Carregar(id); }),
            Ui.Botao("Avançar etapa", (_, _) => Avancar(), primario: true),
            Ui.Botao("Mudar etapa", (_, _) => MudarEtapa()),
            Ui.Botao("Faturar", (_, _) => Faturar()),
            Ui.Botao("Cancelar OS", (_, _) => Cancelar()),
            Ui.Botao("Imprimir", (_, _) => { if (Selecionada() is long id) CupomForm.Visualizar(this, $"OS nº {id}", OrdensServico.Texto(id), 80); }),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(ordens, "ordens_servico")),
        ]);

        split.Panel1.Controls.Add(ordens);
        split.Panel2.Controls.Add(historico);
        split.Panel2.Controls.Add(new Label { Text = "Histórico da OS selecionada (etapas, prazos e faturamento)", Dock = DockStyle.Top, Height = 26, Font = Ui.FonteNegrito, Padding = new Padding(6, 5, 0, 0) });
        historico.BringToFront();
        ordens.Tag = "Nenhuma OS nesta lista. Use \"Nova OS\" ou mude o filtro \"Mostrar\".";
        Ui.ColorirSituacao(ordens);

        Controls.Add(split);
        Controls.Add(acoes);
        Controls.Add(filtros);

        situacao.SelectedIndexChanged += (_, _) => Carregar();
        busca.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Carregar(); } };
        ordens.SelectionChanged += (_, _) => CarregarHistorico();
        ordens.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && Selecionada() is long id && Editar(this, id) is long) Carregar(id); };
        Load += (_, _) =>
        {
            split.SplitterDistance = Math.Max(120, split.Height * 62 / 100);
            Carregar();
        };
    }

    /// <summary>Abre já filtrada (alertas da tela inicial).</summary>
    public void AplicarFiltro(string filtro)
    {
        situacao.SelectedIndex = filtro switch
        {
            Alertas.OsAtrasadas => 1,
            Alertas.OsHoje => 2,
            Alertas.OsProntas => 3,
            Alertas.OsAFaturar => 4,
            _ => 0,
        };
        Carregar();
    }

    /// <summary>Filtro escolhido (para testes).</summary>
    public string FiltroAtual => situacao.Text;

    void Carregar(long? selecionar = null)
    {
        var condicao = situacao.SelectedIndex switch
        {
            0 => OrdensServico.SqlEmAndamento,
            1 => $"{OrdensServico.SqlEmAndamento} AND o.prazo < date('now', 'localtime')",
            2 => $"{OrdensServico.SqlEmAndamento} AND o.prazo = date('now', 'localtime')",
            3 => "o.etapa = 'PRONTA'",
            4 => "o.etapa = 'ENTREGUE' AND o.faturada_em IS NULL AND o.valor > 0",
            5 => "o.etapa = 'ENTREGUE'",
            6 => "o.etapa = 'CANCELADA'",
            _ => "1 = 1",
        };
        var texto = busca.Text.Trim();
        if (texto.Length > 0)
            condicao += long.TryParse(texto, out _)
                ? " AND o.id = @p0"
                : $" AND (p.nome LIKE @p0 ESCAPE '\\' OR o.descricao LIKE @p0 ESCAPE '\\')";
        var dt = Db.Query($"""
            SELECT o.id, o.id AS "Nº", p.nome AS "Cliente", o.descricao AS "Serviço",
                   strftime('%d/%m/%Y', o.data) AS "Abertura", strftime('%d/%m/%Y', o.prazo) AS "Prazo",
                   CASE o.etapa WHEN 'ABERTA' THEN 'Aberta' WHEN 'APROVADA' THEN 'Aprovada' WHEN 'EM_EXECUCAO' THEN 'Em execução'
                                WHEN 'PRONTA' THEN 'Pronta' WHEN 'ENTREGUE' THEN 'Entregue' ELSE 'Cancelada' END AS "Etapa",
                   {OrdensServico.SqlSituacao} AS "Situação",
                   CASE WHEN {OrdensServico.SqlEmAndamento} AND o.prazo < date('now', 'localtime')
                        THEN CAST(julianday(date('now', 'localtime')) - julianday(o.prazo) AS INTEGER) END AS "Dias de atraso",
                   u.nome AS "Responsável", o.valor AS "Valor",
                   CASE WHEN o.faturada_em IS NOT NULL THEN 'Sim' ELSE 'Não' END AS "Faturada"
            FROM ordens_servico o JOIN pessoas p ON p.id = o.cliente_id LEFT JOIN usuarios u ON u.id = o.responsavel_id
            WHERE {condicao}
            ORDER BY CASE WHEN {OrdensServico.SqlEmAndamento} THEN 0 ELSE 1 END, o.prazo, o.id
            """, long.TryParse(texto, out var n) ? n : Db.Contem(texto));
        ordens.DataSource = dt;
        var r = OrdensServico.Resumo();
        resumo.Text = $"{dt.Rows.Count} OS na lista  ·  em andamento {r.EmAndamento}  ·  atrasadas {r.Atrasadas}  ·  entrega hoje {r.VencemHoje}"
                      + (r.PctNoPrazo is decimal pct ? $"  ·  entregues no prazo (90 dias): {pct:N1}%" : "");
        if (selecionar != null)
            foreach (DataGridViewRow linha in ordens.Rows)
                if (linha.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                {
                    ordens.ClearSelection();
                    linha.Selected = true;
                    ordens.CurrentCell = linha.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                }
        CarregarHistorico();
    }

    void CarregarHistorico()
    {
        if (Ui.IdLinha(ordens) is not long id)
        {
            historico.DataSource = null;
            return;
        }
        historico.DataSource = Db.Query("""
            SELECT h.id, strftime('%d/%m/%Y %H:%M', h.data) AS "Quando",
                   CASE h.etapa WHEN 'ABERTA' THEN 'Aberta' WHEN 'APROVADA' THEN 'Aprovada' WHEN 'EM_EXECUCAO' THEN 'Em execução'
                                WHEN 'PRONTA' THEN 'Pronta' WHEN 'ENTREGUE' THEN 'Entregue' ELSE 'Cancelada' END AS "Etapa",
                   h.observacao AS "O que aconteceu", u.nome AS "Por"
            FROM os_historico h LEFT JOIN usuarios u ON u.id = h.usuario_id
            WHERE h.os_id = @p0 ORDER BY h.data DESC, h.id DESC
            """, id);
    }

    long? Selecionada()
    {
        if (Ui.IdLinha(ordens) is long id)
            return id;
        Ui.Erro("Selecione uma ordem de serviço.");
        return null;
    }

    void Executar(Action<long> acao)
    {
        if (Selecionada() is not long id)
            return;
        try
        {
            acao(id);
        }
        catch (InvalidOperationException ex)
        {
            Ui.Erro(ex.Message);
        }
        Carregar(id);
    }

    void Avancar() => Executar(id =>
    {
        var etapa = Ui.Txt(OrdensServico.Ler(id)["etapa"]);
        if (OrdensServico.Proxima(etapa) is not { } proxima)
            throw new InvalidOperationException(etapa == OrdensServico.Cancelada ? "OS cancelada não muda de etapa." : "Esta OS já foi entregue (última etapa).");
        OrdensServico.MudarEtapa(id, proxima);
        Ui.Feito($"OS nº {id}: {OrdensServico.Nome(proxima)}.");
    });

    void MudarEtapa() => Executar(id =>
    {
        var atual = Ui.Txt(OrdensServico.Ler(id)["etapa"]);
        using var d = new Dialogo($"Mudar etapa da OS nº {id}", nameof(OrdensServicoForm), "Mudar");
        d.Nota($"Etapa atual: {OrdensServico.Nome(atual)}. Para voltar a uma etapa anterior, informe o motivo.");
        var etapa = d.Adicionar("Nova etapa:", Ui.Combo());
        etapa.Items.AddRange(OrdensServico.Fluxo.Select(OrdensServico.Nome).ToArray());
        etapa.SelectedIndex = Math.Max(0, Array.IndexOf(OrdensServico.Fluxo, atual));
        var motivo = d.Adicionar("Observação / motivo:", Validacao.Caixa(Formatos.Texto(300), "Observação"));
        d.Validar = () =>
        {
            try
            {
                OrdensServico.MudarEtapa(id, OrdensServico.Fluxo[etapa.SelectedIndex], motivo.Text);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Ui.Erro(ex.Message);
                return false;
            }
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            Ui.Feito($"OS nº {id}: {etapa.Text}.");
    });

    void Faturar() => Executar(id =>
    {
        var o = OrdensServico.Ler(id);
        if (o["faturada_em"] is not DBNull)
            throw new InvalidOperationException("Esta OS já foi faturada (veja as contas no Financeiro).");
        using var d = new Dialogo($"Faturar OS nº {id}", nameof(OrdensServicoForm), "Faturar");
        d.Nota($"Gera as contas a receber da OS no Financeiro. Valor: {Ui.Moeda(Ui.Dec(o["valor"]))}.");
        var parcelas = d.Adicionar("Parcelas:", Ui.Numero(0, Parametros.MaxParcelas, 1));
        var vencimento = d.Adicionar("1º vencimento:", Ui.Data(DateTime.Today.AddDays(Parametros.PrazoVencimentoDias)));
        d.Nota("As demais parcelas vencem de mês em mês. Para emitir boletos, use Financeiro > Cobrança bancária.");
        d.Validar = () =>
        {
            try
            {
                OrdensServico.Faturar(id, (int)parcelas.Value, vencimento.Value.Date);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Ui.Erro(ex.Message);
                return false;
            }
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            Ui.Feito($"OS nº {id} faturada.");
    });

    void Cancelar() => Executar(id =>
    {
        using var d = new Dialogo($"Cancelar OS nº {id}", nameof(OrdensServicoForm), "Cancelar OS");
        var motivo = d.Adicionar("Motivo:", Validacao.Caixa(Formatos.Texto(300), "Motivo", obrigatorio: true));
        d.Nota("A OS cancelada sai da lista de andamento, mas continua consultável (filtro \"Canceladas\").");
        d.Validar = () =>
        {
            try
            {
                OrdensServico.Cancelar(id, motivo.Text);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Ui.Erro(ex.Message);
                return false;
            }
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            Ui.Feito($"OS nº {id} cancelada.");
    });

    /// <summary>Diálogo de nova OS (id null) ou de edição. Devolve o id gravado (null se desistiu).</summary>
    public static long? Editar(IWin32Window dono, long? id)
    {
        var o = id is long existente ? OrdensServico.Ler(existente) : null;
        using var d = new Dialogo(o is null ? "Nova ordem de serviço" : $"OS nº {id}", nameof(OrdensServicoForm), "Salvar");
        var cliente = d.Adicionar("Cliente:", Ui.Combo(380));
        Ui.Preencher(cliente, Db.Query("SELECT id, nome FROM pessoas WHERE eh_cliente = 1 AND (ativo = 1 OR id = @p0) ORDER BY nome", o?["cliente_id"] ?? 0L));
        if (cliente.Items.Count == 0)
        {
            Ui.Erro("Nenhum cliente cadastrado. Cadastre em Cadastros > Clientes e fornecedores.");
            return null;
        }
        var descricao = d.Adicionar("Serviço (o que será feito):", Validacao.Caixa(Formatos.TextoLongo(2000), "Serviço", obrigatorio: true, largura: 380));
        descricao.AcceptsReturn = true;
        descricao.Height = 90;
        var prazo = d.Adicionar("Prazo de entrega:", Ui.Data(o is null ? DateTime.Today.AddDays(Parametros.PrazoOsDias) : Db.LerData(o["prazo"])));
        var valor = d.Adicionar("Valor (R$):", Ui.Numero());
        var responsavel = d.Adicionar("Responsável:", Ui.Combo());
        Ui.Preencher(responsavel, Db.Query("SELECT id, nome FROM usuarios WHERE ativo = 1 OR id = @p0 ORDER BY nome", o?["responsavel_id"] ?? 0L), "(ninguém)");
        var obs = d.Adicionar("Observações:", Validacao.Caixa(Formatos.Texto(500), "Observações", largura: 380));
        d.Nota("Toda OS tem prazo: as atrasadas aparecem em vermelho aqui e como alerta na tela inicial.");
        if (o != null)
        {
            Ui.Selecionar(cliente, o["cliente_id"]);
            descricao.Text = Ui.Txt(o["descricao"]);
            valor.Value = Ui.Dec(o["valor"]);
            Ui.Selecionar(responsavel, o["responsavel_id"] is DBNull ? null : o["responsavel_id"]);
            obs.Text = Ui.Txt(o["observacoes"]);
            Alteracoes.Limpar(d);
        }
        long? gravado = null;
        d.Validar = () =>
        {
            try
            {
                var dados = new OrdensServico.Dados(Ui.Id(cliente) ?? 0, descricao.Text, prazo.Value.Date, valor.Value, Ui.Id(responsavel), obs.Text);
                if (id is long existente)
                {
                    OrdensServico.Alterar(existente, dados);
                    gravado = existente;
                }
                else
                    gravado = OrdensServico.Criar(dados);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Ui.Erro(ex.Message);
                return false;
            }
        };
        if (d.ShowDialog(dono) != DialogResult.OK)
            return null;
        Ui.Feito(id is null ? $"OS nº {gravado} aberta." : $"OS nº {gravado} salva.");
        return gravado;
    }
}
