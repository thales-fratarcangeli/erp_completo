using System.Text;

namespace ErpLoja.Forms;

/// <summary>Fretes: contratação de transporte, CIOT (Pamcard/administradoras), contas a pagar e situação da viagem.</summary>
public class FretesForm : Form
{
    readonly ComboBox situacao = Ui.Combo(150);
    readonly DateTimePicker de = Ui.Data(DateTime.Today.AddDays(-60)), ate = Ui.Data(DateTime.Today.AddDays(30));
    readonly DataGridView fretes = Ui.Grid();
    readonly Label resumo = Ui.Rotulo("");
    readonly Label administradora = Ui.Rotulo("");

    public FretesForm()
    {
        Ui.Estilizar(this, "Fretes e CIOT", 1300, 720, nameof(FretesForm));
        situacao.Items.AddRange(["Em aberto", "Entregues", "Cancelados", "Todos"]);
        situacao.SelectedIndex = 0;

        var filtros = Ui.Barra();
        filtros.Controls.AddRange([
            Ui.Rotulo("Situação:"), situacao, Ui.Rotulo("De:"), de, Ui.Rotulo("até:"), ate,
            Ui.Botao("Pesquisar", (_, _) => Carregar(), primario: true), resumo, administradora,
        ]);
        var acoes = Ui.Barra();
        acoes.Controls.AddRange([
            Ui.Botao("Novo frete", (_, _) => Editar(null), primario: true),
            Ui.Botao("Abrir / editar", (_, _) => { if (Ui.IdLinha(fretes) is long id) Editar(id); }),
            Ui.Botao("Declarar CIOT", async (_, _) => await Ciot(Fretes.DeclararCiot, "declarado")),
            Ui.Botao("Encerrar CIOT", async (_, _) => await Ciot(Fretes.EncerrarCiot, "encerrado")),
            Ui.Botao("Cancelar CIOT", async (_, _) => await CancelarCiot()),
            Ui.Botao("Gerar contas a pagar", (_, _) => Executar(id => Ui.Feito($"{Fretes.GerarContas(id)} conta(s) a pagar gerada(s).")) ),
            Ui.Botao("Em trânsito", (_, _) => Executar(id => Fretes.MudarStatus(id, "EM_TRANSITO"))),
            Ui.Botao("Entregue", (_, _) => Executar(id => Fretes.MudarStatus(id, "ENTREGUE"))),
            Ui.Botao("Cancelar frete", (_, _) => Executar(id => { if (Ui.Confirmar("Cancelar este frete?")) Fretes.MudarStatus(id, "CANCELADO"); })),
            Ui.Botao("Imprimir", (_, _) => { if (Ui.IdLinha(fretes) is long id) CupomForm.Visualizar(this, $"Frete nº {id}", Texto(id), 80); }),
        ]);
        Ui.ColorirSituacao(fretes);
        Controls.Add(fretes);
        Controls.Add(acoes);
        Controls.Add(filtros);

        situacao.SelectedIndexChanged += (_, _) => Carregar();
        fretes.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && Ui.IdLinha(fretes) is long id) Editar(id); };
        Load += (_, _) => Carregar();
    }

    void Carregar(long? selecionar = null)
    {
        administradora.Text = $"   Administradora do CIOT: {Parametros.CiotAdministradora}" + (Parametros.CiotProducao ? "" : " (homologação)");
        var filtro = situacao.SelectedIndex switch
        {
            0 => "f.status IN ('ABERTO', 'EM_TRANSITO')",
            1 => "f.status = 'ENTREGUE' AND date(f.data) BETWEEN @p0 AND @p1",
            2 => "f.status = 'CANCELADO' AND date(f.data) BETWEEN @p0 AND @p1",
            _ => "date(f.data) BETWEEN @p0 AND @p1",
        };
        var dt = Db.Query($"""
            SELECT f.id, f.id AS "Nº", strftime('%d/%m/%Y', f.data) AS "Data",
                   CASE f.modalidade WHEN 'PROPRIO' THEN 'Frota própria' ELSE 'Contratado' END AS "Modalidade",
                   t.nome AS "Transportador", m.nome AS "Motorista", v.placa AS "Placa",
                   COALESCE(f.origem_cidade || '/' || f.origem_uf, '') || ' → ' || COALESCE(f.destino_cidade || '/' || f.destino_uf, '') AS "Rota",
                   f.peso_kg AS "Peso (kg)", f.valor_frete AS "Frete",
                   CASE f.status WHEN 'EM_TRANSITO' THEN 'EM TRÂNSITO' ELSE f.status END AS "Situação",
                   f.ciot AS "CIOT", REPLACE(f.ciot_status, '_', ' ') AS "Situação CIOT",
                   CASE f.contas_geradas WHEN 1 THEN 'Sim' ELSE '' END AS "Contas"
            FROM fretes f
            LEFT JOIN pessoas t ON t.id = f.transportador_id
            LEFT JOIN pessoas m ON m.id = f.motorista_id
            LEFT JOIN veiculos v ON v.id = f.veiculo_id
            WHERE {filtro} ORDER BY f.id DESC
            """, Db.Dia(de.Value), Db.Dia(ate.Value));
        fretes.DataSource = dt;
        resumo.Text = $"{dt.Rows.Count} frete(s) · {Ui.Moeda(dt.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["Frete"])))}";
        if (selecionar != null)
            foreach (DataGridViewRow r in fretes.Rows)
                if (r.DataBoundItem is DataRowView v && Convert.ToInt64(v["id"]) == selecionar)
                {
                    fretes.ClearSelection();
                    r.Selected = true;
                }
    }

    void Executar(Action<long> acao)
    {
        if (Ui.IdLinha(fretes) is not long id)
        {
            Ui.Erro("Selecione um frete.");
            return;
        }
        try
        {
            acao(id);
            Carregar(id);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    async Task Ciot(Func<long, Task<RespostaCiot>> operacao, string verbo)
    {
        if (Ui.IdLinha(fretes) is not long id)
        {
            Ui.Erro("Selecione um frete.");
            return;
        }
        try
        {
            Cursor = Cursors.WaitCursor;
            var r = await operacao(id);
            if (r.Sucesso)
                Ui.Feito($"CIOT {verbo}: {r.Ciot}");
            else
                Ui.Erro("A administradora não concluiu a operação:\n" + r.Mensagem);
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
            Carregar(id);
        }
    }

    async Task CancelarCiot()
    {
        using var d = new Dialogo("Cancelar CIOT", "FretesForm", "Cancelar CIOT");
        var motivo = d.Adicionar("Motivo:", new TextBox());
        d.Validar = () => motivo.Text.Trim().Length >= 15 || Falhar("Descreva o motivo (pelo menos 15 caracteres).");
        if (Ui.IdLinha(fretes) is null || d.ShowDialog(this) != DialogResult.OK)
            return;
        await Ciot(id => Fretes.CancelarCiot(id, motivo.Text.Trim()), "cancelado");
    }

    static bool Falhar(string m)
    {
        Ui.Erro(m);
        return false;
    }

    void Editar(long? id)
    {
        if (FreteEditor.Executar(this, id) is long salvo)
            Carregar(salvo);
    }

    static string Texto(long id)
    {
        var f = Db.Query("""
            SELECT f.*, t.nome AS t_nome, t.cpf_cnpj AS t_doc, t.rntrc, m.nome AS m_nome, m.cpf_cnpj AS m_doc, m.cnh, v.placa, v.tipo AS v_tipo
            FROM fretes f LEFT JOIN pessoas t ON t.id = f.transportador_id LEFT JOIN pessoas m ON m.id = f.motorista_id
            LEFT JOIN veiculos v ON v.id = f.veiculo_id WHERE f.id = @p0
            """, id).Rows[0];
        var sb = new StringBuilder();
        var traco = new string('-', 80);
        sb.AppendLine($"[C][G]ORDEM DE FRETE Nº {id}");
        sb.AppendLine($"[C]{Db.Config("nome_loja")} - CNPJ {Db.Config("cnpj")}");
        sb.AppendLine(traco);
        sb.AppendLine($"Data: {Db.LerData(f["data"]):dd/MM/yyyy}   Modalidade: {f["modalidade"]}   Situação: {f["status"]}");
        sb.AppendLine($"Transportador: {f["t_nome"]}  CPF/CNPJ: {f["t_doc"]}  RNTRC: {f["rntrc"]}");
        sb.AppendLine($"Motorista: {f["m_nome"]}  CPF: {f["m_doc"]}  CNH: {f["cnh"]}");
        sb.AppendLine($"Veículo: {f["placa"]} ({f["v_tipo"]})");
        sb.AppendLine($"Origem: {f["origem_cidade"]}/{f["origem_uf"]}   Destino: {f["destino_cidade"]}/{f["destino_uf"]}   Distância: {Ui.Dec(f["distancia_km"]):N0} km");
        sb.AppendLine($"Carga: {f["mercadoria"]}   Peso: {Ui.Dec(f["peso_kg"]):N0} kg");
        sb.AppendLine(traco);
        sb.AppendLine($"[N]Frete: R$ {Ui.Dec(f["valor_frete"]):N2}   Pedágio: R$ {Ui.Dec(f["valor_pedagio"]):N2}   Adiantamento: R$ {Ui.Dec(f["valor_adiantamento"]):N2}");
        if (Ui.Txt(f["ciot"]) is { Length: > 0 } ciot)
            sb.AppendLine($"[N]CIOT: {ciot}   ({f["ciot_status"]} - {f["ciot_administradora"]})");
        sb.AppendLine(traco);
        sb.AppendLine();
        sb.AppendLine("Contratante: ____________________________     Transportador: ____________________________");
        return sb.ToString();
    }
}

/// <summary>Edição de um frete (diálogo).</summary>
public static class FreteEditor
{
    public static long? Executar(IWin32Window dono, long? id)
    {
        var f = id is long existente ? Db.Query("SELECT * FROM fretes WHERE id = @p0", existente).Rows[0] : null;
        if (f != null && Ui.Txt(f["status"]) is "ENTREGUE" or "CANCELADO")
        {
            Ui.Info("Frete encerrado: aberto só para consulta. Use Imprimir para ver os dados.");
            return null;
        }
        using var d = new Dialogo(id is null ? "Novo frete" : $"Frete nº {id}", "FretesForm", "Salvar");
        var data = d.Adicionar("Data:", Ui.Data(f is null ? DateTime.Today : Db.LerData(f["data"])));
        var modalidade = d.Adicionar("Modalidade:", Ui.Combo());
        modalidade.Items.AddRange(["CONTRATADO", "PROPRIO"]);
        modalidade.SelectedItem = f is null ? "CONTRATADO" : Ui.Txt(f["modalidade"]);
        var transportador = d.Adicionar("Transportador:", Ui.Combo());
        Ui.Preencher(transportador, Db.Query("SELECT id, nome || COALESCE(' (' || cpf_cnpj || ')', '') FROM pessoas WHERE eh_transportador = 1 AND ativo = 1 ORDER BY nome"), "(nenhum)");
        var motorista = d.Adicionar("Motorista:", Ui.Combo());
        Ui.Preencher(motorista, Db.Query("SELECT id, nome FROM pessoas WHERE eh_motorista = 1 AND ativo = 1 ORDER BY nome"), "(nenhum)");
        var veiculo = d.Adicionar("Veículo:", Ui.Combo());
        Ui.Preencher(veiculo, Db.Query("SELECT id, placa || ' - ' || tipo || COALESCE(' ' || marca_modelo, '') FROM veiculos WHERE ativo = 1 ORDER BY placa"), "(nenhum)");
        var origem = d.Adicionar("Origem (cidade):", new TextBox { Text = f is null ? Db.Config("cidade") : Ui.Txt(f["origem_cidade"]) });
        var origemUf = d.Adicionar("Origem (UF):", Ui.Combo(80));
        var destino = d.Adicionar("Destino (cidade):", new TextBox { Text = f is null ? "" : Ui.Txt(f["destino_cidade"]) });
        var destinoUf = d.Adicionar("Destino (UF):", Ui.Combo(80));
        foreach (var uf in new[] { origemUf, destinoUf })
            uf.Items.AddRange(["", .. Fiscal.Ufs]);
        origemUf.SelectedItem = f is null ? Db.Config("uf") : Ui.Txt(f["origem_uf"]);
        destinoUf.SelectedItem = f is null ? "" : Ui.Txt(f["destino_uf"]);
        var distancia = d.Adicionar("Distância (km):", Ui.Numero(0, 99_999));
        var peso = d.Adicionar("Peso (kg):", Ui.Numero(2, 999_999));
        var mercadoria = d.Adicionar("Mercadoria:", new TextBox { Text = f is null ? "" : Ui.Txt(f["mercadoria"]) });
        var valor = d.Adicionar("Valor do frete (R$):", Ui.Numero());
        var pedagio = d.Adicionar("Pedágio (R$):", Ui.Numero());
        var adiantamento = d.Adicionar("Adiantamento (R$):", Ui.Numero());
        var outras = d.Adicionar("Outras despesas (R$):", Ui.Numero());
        var previsao = d.Adicionar("Previsão de entrega:", Ui.Data(f?["previsao_entrega"] is string pe ? Db.LerData(pe) : DateTime.Today.AddDays(3)));
        var obs = d.Adicionar("Observações:", new TextBox { Text = f is null ? "" : Ui.Txt(f["observacoes"]) });
        var ciotInfo = new Label { AutoSize = true, Font = Ui.FonteNegrito, Margin = new Padding(3, 8, 3, 3) };
        d.Adicionar("CIOT:", ciotInfo);
        if (f != null)
        {
            Ui.Selecionar(transportador, f["transportador_id"]);
            Ui.Selecionar(motorista, f["motorista_id"]);
            Ui.Selecionar(veiculo, f["veiculo_id"]);
            distancia.Value = Ui.Dec(f["distancia_km"]);
            peso.Value = Ui.Dec(f["peso_kg"]);
            valor.Value = Ui.Dec(f["valor_frete"]);
            pedagio.Value = Ui.Dec(f["valor_pedagio"]);
            adiantamento.Value = Ui.Dec(f["valor_adiantamento"]);
            outras.Value = Ui.Dec(f["outras_despesas"]);
        }
        var ciotDeclarado = f != null && Ui.Txt(f["ciot_status"]) is "DECLARADO" or "ENCERRADO";
        void AtualizarCiot()
        {
            var doc = Ui.Id(transportador) is long t ? Ui.Txt(Db.Scalar("SELECT cpf_cnpj FROM pessoas WHERE id = @p0", t)) : "";
            var obrigatorio = Fretes.CiotObrigatorio(modalidade.Text, doc);
            ciotInfo.Text = ciotDeclarado ? $"já declarado ({f!["ciot"]}) — valores travados" :
                obrigatorio ? "OBRIGATÓRIO (transportador autônomo): declare após salvar" : "não obrigatório";
            ciotInfo.ForeColor = obrigatorio && !ciotDeclarado ? Ui.Alerta : Color.DimGray;
        }
        modalidade.SelectedIndexChanged += (_, _) => AtualizarCiot();
        transportador.SelectedIndexChanged += (_, _) => AtualizarCiot();
        AtualizarCiot();
        if (ciotDeclarado)
            foreach (var c in new Control[] { modalidade, transportador, motorista, veiculo, valor, pedagio, adiantamento })
                c.Enabled = false;

        d.Validar = () =>
        {
            if (modalidade.Text == "CONTRATADO" && Ui.Id(transportador) is null) { Ui.Erro("Frete contratado precisa do transportador."); return false; }
            if (adiantamento.Value > valor.Value) { Ui.Erro("O adiantamento não pode ser maior que o frete."); return false; }
            return true;
        };
        if (d.ShowDialog(dono) != DialogResult.OK)
            return null;

        var doc = Ui.Id(transportador) is long tr ? Ui.Txt(Db.Scalar("SELECT cpf_cnpj FROM pessoas WHERE id = @p0", tr)) : "";
        var statusCiot = ciotDeclarado ? Ui.Txt(f!["ciot_status"]) : Fretes.CiotObrigatorio(modalidade.Text, doc) ? "PENDENTE" : "NAO_SE_APLICA";
        object?[] v =
        [
            data.Value.Date, modalidade.Text, Ui.Id(transportador), Ui.Id(motorista), Ui.Id(veiculo), origem.Text.Trim(), origemUf.Text,
            destino.Text.Trim(), destinoUf.Text, distancia.Value, peso.Value, mercadoria.Text.Trim(), valor.Value, pedagio.Value,
            adiantamento.Value, outras.Value, Db.Dia(previsao.Value), obs.Text.Trim(), statusCiot,
        ];
        if (id is long atual)
        {
            Db.Exec("""
                UPDATE fretes SET data = @p0, modalidade = @p1, transportador_id = @p2, motorista_id = @p3, veiculo_id = @p4,
                    origem_cidade = @p5, origem_uf = @p6, destino_cidade = @p7, destino_uf = @p8, distancia_km = @p9, peso_kg = @p10,
                    mercadoria = @p11, valor_frete = @p12, valor_pedagio = @p13, valor_adiantamento = @p14, outras_despesas = @p15,
                    previsao_entrega = @p16, observacoes = @p17, ciot_status = @p18
                WHERE id = @p19
                """, [.. v, atual]);
            return atual;
        }
        return Convert.ToInt64(Db.Scalar("""
            INSERT INTO fretes(data, modalidade, transportador_id, motorista_id, veiculo_id, origem_cidade, origem_uf, destino_cidade,
                destino_uf, distancia_km, peso_kg, mercadoria, valor_frete, valor_pedagio, valor_adiantamento, outras_despesas,
                previsao_entrega, observacoes, ciot_status, usuario_id)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19)
            RETURNING id
            """, [.. v, Sessao.UsuarioId]));
    }
}
