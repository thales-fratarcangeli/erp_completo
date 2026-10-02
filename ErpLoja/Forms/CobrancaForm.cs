using System.Drawing.Printing;
using System.Text;

namespace ErpLoja.Forms;

/// <summary>Cobrança bancária: boletos, remessa/retorno CNAB 240 e roteiro de homologação com o banco.</summary>
public class CobrancaForm : Form
{
    readonly ComboBox conta = Ui.Combo(300), situacao = Ui.Combo(140);
    readonly DataGridView boletos = Ui.Grid(), arquivos = Ui.Grid();
    readonly TextBox log = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9.5F), BackColor = Color.White };
    readonly Label resumo = Ui.Rotulo("");

    public CobrancaForm()
    {
        Ui.Estilizar(this, "Cobrança bancária", 1250, 740, nameof(CobrancaForm));
        situacao.Items.AddRange(["Ativos", "Pagos", "Todos"]);
        situacao.SelectedIndex = 0;

        var topo = Ui.Barra();
        topo.Controls.AddRange([Ui.Rotulo("Conta bancária:"), conta, resumo]);

        var abas = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 5) };

        // --- boletos
        var abaBoletos = new TabPage("Boletos") { BackColor = Ui.Fundo };
        var barraBoletos = Ui.Barra();
        barraBoletos.Controls.AddRange([
            Ui.Rotulo("Mostrar:"), situacao,
            Ui.Botao("Emitir boletos...", (_, _) => Emitir(), primario: true),
            Ui.Botao("Visualizar / imprimir", (_, _) => { if (Ui.IdLinha(boletos) is long id) BoletoForm.Mostrar(this, id); }),
            Ui.Botao("Cancelar / pedir baixa", (_, _) => CancelarBoleto()),
            Ui.Botao("Exportar CSV", (_, _) => Ui.ExportarCsv(boletos, "boletos")),
        ]);
        abaBoletos.Controls.Add(boletos);
        abaBoletos.Controls.Add(barraBoletos);
        boletos.BringToFront();
        Ui.ColorirSituacao(boletos);

        // --- remessa e retorno
        var abaArquivos = new TabPage("Remessa e retorno") { BackColor = Ui.Fundo };
        var barraArquivos = Ui.Barra();
        barraArquivos.Controls.AddRange([
            Ui.Botao("Gerar remessa", (_, _) => GerarRemessa(), primario: true),
            Ui.Botao("Importar retorno do banco...", (_, _) => ImportarRetorno()),
            Ui.Botao("Salvar arquivo selecionado", (_, _) => SalvarArquivo()),
        ]);
        abaArquivos.Controls.Add(arquivos);
        abaArquivos.Controls.Add(barraArquivos);
        arquivos.BringToFront();

        // --- homologação
        var abaHomologacao = new TabPage("Homologação") { BackColor = Ui.Fundo };
        var roteiro = new Label
        {
            Dock = DockStyle.Top,
            Height = 128,
            Padding = new Padding(12, 10, 12, 0),
            BackColor = Color.White,
            Text = "Roteiro de homologação com o banco (a conta bancária deve estar no ambiente HOMOLOGACAO):\n" +
                   "1. Gere boletos de teste e imprima alguns para enviar ao banco (validação visual e leitura do código de barras).\n" +
                   "2. Gere a remessa de teste (aba Remessa e retorno) e envie o arquivo ao gerente/canal de homologação do banco.\n" +
                   "3. Antes de enviar, confira o arquivo com o Validador CNAB 240. Ajuste a conta bancária conforme o manual do banco se houver apontamentos.\n" +
                   "4. Importe o retorno que o banco devolver. Sem retorno ainda? Use Simular retorno para testar o processamento.\n" +
                   "5. Com a aprovação do banco, mude o ambiente da conta para PRODUCAO.",
        };
        var barraHomologacao = Ui.Barra();
        barraHomologacao.Controls.AddRange([
            Ui.Botao("1. Gerar boletos de teste", (_, _) => BoletosDeTeste(), primario: true),
            Ui.Botao("3. Validador CNAB 240...", (_, _) => ValidarArquivo()),
            Ui.Botao("4. Simular retorno (confirmar)", (_, _) => Simular(liquidar: false)),
            Ui.Botao("4. Simular retorno (confirmar e pagar)", (_, _) => Simular(liquidar: true)),
        ]);
        abaHomologacao.Controls.Add(log);
        abaHomologacao.Controls.Add(barraHomologacao);
        abaHomologacao.Controls.Add(roteiro);
        log.BringToFront();

        abas.TabPages.AddRange([abaBoletos, abaArquivos, abaHomologacao]);
        Controls.Add(abas);
        Controls.Add(topo);

        conta.SelectedIndexChanged += (_, _) => Carregar();
        situacao.SelectedIndexChanged += (_, _) => Carregar();
        Load += (_, _) =>
        {
            Ui.Preencher(conta, Db.Query("SELECT id, nome || ' (' || banco_codigo || ' · ' || ambiente || ')' FROM contas_bancarias WHERE ativo = 1 ORDER BY nome"));
            if (conta.Items.Count == 0)
            {
                resumo.Text = "   Cadastre uma conta bancária em Financeiro > Contas bancárias.";
                resumo.ForeColor = Ui.Perigo;
            }
            Carregar();
        };
    }

    long? ContaId => Ui.Id(conta);

    /// <summary>Conta bancária selecionada no topo; sem nenhuma, explica o que fazer.</summary>
    long? ContaEscolhida()
    {
        if (ContaId is long c)
            return c;
        Ui.Erro("Nenhuma conta bancária selecionada. Cadastre uma em Financeiro > Contas bancárias e escolha-a no topo desta tela.");
        return null;
    }

    void Registrar(string texto) => log.AppendText($"[{DateTime.Now:HH:mm:ss}] {texto}\r\n");

    void Carregar()
    {
        if (ContaId is not long c)
            return;
        var filtro = situacao.SelectedIndex switch
        {
            0 => "b.status IN ('EMITIDO', 'ENVIADO', 'REGISTRADO', 'REJEITADO')",
            1 => "b.status = 'PAGO'",
            _ => "1 = 1",
        };
        var dt = Db.Query($"""
            SELECT b.id, b.nosso_numero AS "Nosso número", p.nome AS "Pagador", strftime('%d/%m/%Y', b.vencimento) AS "Vencimento",
                   b.valor AS "Valor", b.status AS "Situação", CASE b.baixa_solicitada WHEN 1 THEN 'Sim' ELSE '' END AS "Baixa pedida",
                   strftime('%d/%m/%Y', b.data_pagamento) AS "Pago em", b.valor_pago AS "Valor pago", b.ocorrencia AS "Última ocorrência",
                   b.linha_digitavel AS "Linha digitável"
            FROM boletos b LEFT JOIN pessoas p ON p.id = b.pagador_id
            WHERE b.conta_bancaria_id = @p0 AND {filtro} ORDER BY b.id DESC
            """, c);
        boletos.DataSource = dt;
        arquivos.DataSource = Db.Query("""
            SELECT 'R' || id AS id, 'Remessa' AS "Tipo", sequencia AS "Sequência", strftime('%d/%m/%Y %H:%M', data) AS "Data", arquivo AS "Arquivo",
                   quantidade AS "Títulos", valor_total AS "Valor", CASE homologacao WHEN 1 THEN 'Homologação' ELSE 'Produção' END AS "Ambiente"
            FROM remessas WHERE conta_bancaria_id = @p0
            UNION ALL
            SELECT 'T' || id, 'Retorno', NULL, strftime('%d/%m/%Y %H:%M', data), arquivo, registros, valor_liquidado, liquidados || ' liquidado(s)'
            FROM retornos WHERE conta_bancaria_id = @p0
            ORDER BY 4 DESC
            """, c);
        resumo.ForeColor = Color.Black;
        resumo.Text = $"   {dt.Rows.Count} boleto(s) · {Ui.Moeda(dt.Rows.Cast<DataRow>().Sum(r => Ui.Dec(r["Valor"])))}";
    }

    void Emitir()
    {
        if (ContaId is not long c)
        {
            Ui.Erro("Selecione a conta bancária.");
            return;
        }
        var abertas = Db.Query("""
            SELECT ct.id, ct.descricao AS "Descrição", p.nome AS "Cliente", strftime('%d/%m/%Y', ct.vencimento) AS "Vencimento", ct.valor AS "Valor"
            FROM contas ct LEFT JOIN pessoas p ON p.id = ct.cliente_id
            WHERE ct.tipo = 'RECEBER' AND ct.status = 'ABERTA' AND ct.cliente_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM boletos b WHERE b.conta_id = ct.id AND b.status NOT IN ('CANCELADO', 'BAIXADO', 'REJEITADO'))
            ORDER BY ct.vencimento
            """);
        if (abertas.Rows.Count == 0)
        {
            Ui.Info("Não há contas a receber em aberto (com cliente) sem boleto.");
            return;
        }
        abertas.Columns.Add("Emitir", typeof(bool));
        foreach (DataRow r in abertas.Rows)
            r["Emitir"] = true;
        using var f = new Form { Text = "Emitir boletos", Size = new Size(860, 520), StartPosition = FormStartPosition.CenterParent, Font = Ui.Fonte, Icon = Ui.Icone, ShowInTaskbar = false };
        var grade = Ui.Grid(somenteLeitura: false);
        grade.DataSource = abertas;
        grade.DataBindingComplete += (_, _) => { foreach (DataGridViewColumn col in grade.Columns) col.ReadOnly = col.DataPropertyName != "Emitir"; };
        var barra = Ui.Barra(DockStyle.Bottom);
        barra.Controls.Add(Ui.Botao("Emitir selecionados", (_, _) => { grade.EndEdit(); f.DialogResult = DialogResult.OK; }, primario: true));
        barra.Controls.Add(Ui.Botao("Cancelar", (_, _) => f.DialogResult = DialogResult.Cancel));
        f.Controls.Add(grade);
        f.Controls.Add(barra);
        if (f.ShowDialog(this) != DialogResult.OK)
            return;
        int emitidos = 0;
        foreach (DataRow r in abertas.Rows)
        {
            if (r["Emitir"] is not true)
                continue;
            try
            {
                Cobranca.Emitir(c, (long)r["id"]);
                emitidos++;
            }
            catch (Exception ex)
            {
                Ui.Erro($"Conta {r["Descrição"]}: {ex.Message}");
            }
        }
        Ui.Feito($"{emitidos} boleto(s) emitido(s).");
        Carregar();
    }

    void CancelarBoleto()
    {
        if (Ui.IdLinha(boletos) is not long id || !Ui.Confirmar("Cancelar o boleto (ou pedir a baixa ao banco, se já enviado)?"))
            return;
        try
        {
            Ui.Feito(Cobranca.CancelarOuBaixar(id));
            Carregar();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void GerarRemessa()
    {
        if (ContaEscolhida() is not long c)
            return;
        try
        {
            var (_, arquivo, conteudo, quantidade) = Cobranca.GerarRemessa(c);
            Registrar($"Remessa {arquivo} gerada com {quantidade} título(s).");
            using var dlg = new SaveFileDialog { FileName = arquivo, Filter = "Remessa CNAB (*.rem)|*.rem|Todos|*.*" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, conteudo, Encoding.ASCII);
                Ui.Feito("Remessa salva. Envie o arquivo ao banco.");
            }
            Carregar();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }

    void ImportarRetorno()
    {
        if (ContaEscolhida() is not long c)
            return;
        using var dlg = new OpenFileDialog { Filter = "Retorno CNAB (*.ret;*.txt)|*.ret;*.txt|Todos|*.*", Title = "Arquivo de retorno do banco" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        Processar(c, Path.GetFileName(dlg.FileName), File.ReadAllText(dlg.FileName, Encoding.Latin1));
    }

    void Processar(long c, string arquivo, string conteudo)
    {
        try
        {
            var r = Cobranca.ProcessarRetorno(c, arquivo, conteudo);
            var texto = $"Retorno {arquivo}: {r.Registros} registro(s), {r.Confirmados} entrada(s) confirmada(s), {r.Rejeitados} rejeitada(s), " +
                        $"{r.Liquidados} liquidado(s) ({Ui.Moeda(r.ValorLiquidado)}), {r.NaoEncontrados} não encontrado(s).";
            Registrar(texto);
            foreach (var m in r.Mensagens)
                Registrar("   " + m);
            Ui.Info(texto + (r.Liquidados > 0 ? "\n\nAs contas a receber liquidadas foram baixadas no financeiro." : ""));
            Carregar();
        }
        catch (Exception ex)
        {
            Registrar("ERRO: " + ex.Message);
            Ui.Erro(ex.Message);
        }
    }

    void SalvarArquivo()
    {
        if (Ui.Linha(arquivos) is not DataRowView r)
            return;
        var id = Ui.Txt(r["id"]);
        var tabela = id[0] == 'R' ? "remessas" : "retornos";
        var conteudo = Ui.Txt(Db.Scalar($"SELECT conteudo FROM {tabela} WHERE id = @p0", long.Parse(id[1..])));
        using var dlg = new SaveFileDialog { FileName = Ui.Txt(r["Arquivo"]) };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dlg.FileName, conteudo, Encoding.ASCII);
    }

    void BoletosDeTeste()
    {
        if (ContaEscolhida() is not long c)
            return;
        var banco = ContaBancaria.Carregar(c);
        if (!banco.Homologacao)
        {
            Ui.Erro("Os boletos de teste só podem ser gerados em conta no ambiente HOMOLOGACAO.");
            return;
        }
        if (Cobranca.ValidarModeloCampoLivre(banco.ModeloCampoLivre) is string erroModelo)
        {
            Ui.Erro("A conta bancária está com o modelo do campo livre inválido: " + erroModelo + "\nCorrija em Financeiro > Contas bancárias.");
            return;
        }
        using var s = new DbSessao(transacao: true);
        var pagador = s.Scalar("SELECT id FROM pessoas WHERE nome = 'PAGADOR DE TESTE - HOMOLOGACAO'") as long?
                      ?? s.Inserir("""
                          INSERT INTO pessoas(nome, cpf_cnpj, endereco, cidade, uf, cep, eh_cliente)
                          VALUES('PAGADOR DE TESTE - HOMOLOGACAO', '111.444.777-35', 'RUA DO TESTE, 100', @p0, @p1, '01001000', 1)
                          """, Db.Config("cidade") is { Length: > 0 } ci ? ci : "SAO PAULO", Db.Config("uf") is { Length: > 0 } u ? u : "SP");
        var contas = new List<long>();
        foreach (var valor in new[] { 1.00m, 2.50m, 10.00m })
            contas.Add(s.Inserir("""
                INSERT INTO contas(tipo, descricao, valor, vencimento, status, cliente_id)
                VALUES('RECEBER', 'HOMOLOGAÇÃO BANCÁRIA - boleto de teste', @p0, @p1, 'ABERTA', @p2)
                """, valor, Db.Dia(DateTime.Today.AddDays(10)), pagador));
        s.Commit();
        foreach (var conta in contas)
        {
            var id = Cobranca.Emitir(c, conta);
            Registrar($"Boleto de teste {id}: {Db.Scalar("SELECT linha_digitavel FROM boletos WHERE id = @p0", id)}");
        }
        Ui.Feito("3 boletos de teste emitidos. Gere a remessa na aba Remessa e retorno.");
        Carregar();
    }

    void ValidarArquivo()
    {
        using var dlg = new OpenFileDialog { Filter = "CNAB 240 (*.rem;*.ret;*.txt)|*.rem;*.ret;*.txt|Todos|*.*", Title = "Arquivo CNAB 240 para validar" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        var conteudo = File.ReadAllText(dlg.FileName, Encoding.Latin1);
        var linhas = conteudo.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToList();
        var remessa = linhas.Count > 0 && linhas[0].Length >= 143 && linhas[0][142] == '1';
        var erros = Cobranca.ValidarArquivo240(conteudo, remessa);
        Registrar($"Validação de {Path.GetFileName(dlg.FileName)} ({(remessa ? "remessa" : "retorno")}, {linhas.Count} linhas): " +
                  (erros.Count == 0 ? "estrutura CNAB 240 OK." : $"{erros.Count} problema(s)."));
        foreach (var e in erros)
            Registrar("   " + e);
    }

    void Simular(bool liquidar)
    {
        if (ContaEscolhida() is not long c)
            return;
        var remessa = Db.Scalar("SELECT id FROM remessas WHERE conta_bancaria_id = @p0 ORDER BY id DESC LIMIT 1", c) as long?;
        if (remessa is null)
        {
            Ui.Erro("Gere uma remessa primeiro.");
            return;
        }
        var conteudo = Cobranca.SimularRetorno(remessa.Value, liquidar);
        Registrar($"Retorno simulado para a remessa {remessa} ({(liquidar ? "entradas confirmadas e pagas" : "entradas confirmadas")}).");
        Processar(c, $"SIMULADO_REMESSA_{remessa}.ret", conteudo);
    }
}

/// <summary>Visualização e impressão do boleto (ficha + código de barras).</summary>
public class BoletoForm : Form
{
    readonly long id;
    readonly string texto, codigo;

    BoletoForm(long id)
    {
        this.id = id;
        texto = Cobranca.Texto(id);
        codigo = Ui.Txt(Db.Scalar("SELECT codigo_barras FROM boletos WHERE id = @p0", id));
        Ui.Estilizar(this, $"Boleto {id}", 1000, 560, "CobrancaForm");
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var tela = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.White };
        tela.Paint += (_, e) => Desenhar(e.Graphics, new RectangleF(20, 20, tela.Width - 40, tela.Height - 40));
        var barra = Ui.Barra(DockStyle.Bottom);
        barra.Controls.AddRange([Ui.Botao("Imprimir", (_, _) => Imprimir(), primario: true), Ui.Botao("Fechar", (_, _) => Close())]);
        Controls.Add(tela);
        Controls.Add(barra);
    }

    void Desenhar(Graphics g, RectangleF area)
    {
        using var fonte = new Font("Consolas", 9.5F);
        using var negrito = new Font("Consolas", 10.5F, FontStyle.Bold);
        float y = area.Top;
        foreach (var bruta in texto.Replace("\r\n", "\n").Split('\n'))
        {
            var forte = bruta.StartsWith("[N]") || bruta.StartsWith("[C][N]");
            var linha = Impressao.SemMarcas(bruta);
            g.DrawString(linha, forte ? negrito : fonte, Brushes.Black, area.Left, y);
            y += (forte ? negrito : fonte).GetHeight(g);
        }
        CodigoBarras.Desenhar(g, codigo, new RectangleF(area.Left, y + 12, Math.Min(area.Width, 520), 56));
    }

    void Imprimir()
    {
        try
        {
            using var doc = new PrintDocument { DocumentName = $"Boleto {id}" };
            if (Parametros.ImpressoraDocumentos is { Length: > 0 } nome)
                doc.PrinterSettings.PrinterName = nome;
            doc.PrintPage += (_, e) => Desenhar(e.Graphics!, e.MarginBounds);
            doc.Print();
            Ui.Feito("Boleto enviado para a impressora.");
        }
        catch (Exception ex)
        {
            Ui.Erro("Não foi possível imprimir: " + ex.Message);
        }
    }

    public static void Mostrar(IWin32Window dono, long boletoId)
    {
        using var f = new BoletoForm(boletoId);
        f.ShowDialog(dono);
    }
}
