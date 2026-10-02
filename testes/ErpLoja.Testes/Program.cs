using System.Data;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ErpLoja;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

int falhas = 0;
void Ok(bool cond, string msg) { Console.WriteLine((cond ? "OK   " : "FALHA") + " " + msg); if (!cond) falhas++; }
decimal D(object? v) => v is null or DBNull ? 0 : Convert.ToDecimal(v);

var Raiz = AcharRaiz();
var Dados = Path.Combine(AppContext.BaseDirectory, "dados");
var Telas = Path.Combine(AppContext.BaseDirectory, "telas");
var Temp = Path.Combine(Path.GetTempPath(), "erp_testes_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Telas);
Directory.CreateDirectory(Temp);
LocalBanco.PastaPreferencias = Path.Combine(Temp, "preferencias");
var telasAbertas = new HashSet<string>();

var t = new Thread(() =>
{
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");

        // ---------- migrations: banco legado legado1 (sem __migracoes, user_version 0)
        var legado1 = Path.Combine(Temp, "legado_v1.db");
        File.Copy(Path.Combine(Dados, "banco_v1.db"), legado1);
        Ok(LocalBanco.Validar(legado1) is null, "banco legado reconhecido como banco do sistema");
        var (aplicadas, backup) = LocalBanco.Abrir(legado1);
        Ok(aplicadas.Select(m => m.Versao).SequenceEqual([2, 3, 4]), "legado v1: registra a 1 e aplica as migrações 2, 3 e 4");
        Ok(backup != null && File.Exists(backup), "backup automático antes de migrar banco existente");
        Ok(Convert.ToInt64(Db.Scalar("PRAGMA user_version")) == 4 && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM __migracoes")) == 4, "__migracoes com as 4 versões");
        Ok(Db.Scalar("SELECT username FROM usuarios WHERE id = 1") as string == "admin", "coluna login renomeada para username");
        Ok(Db.Config("venda_sem_estoque") == "PERGUNTAR" && Db.Config("max_parcelas") == "12", "parâmetros padrão gravados pela migração 4");
        Ok(LocalBanco.Localizar() == Path.GetFullPath(legado1), "banco aberto fica lembrado e é localizado na próxima abertura");
        Ok(LocalBanco.Abrir(legado1).Aplicadas.Count == 0, "reabrir não reaplica migrações");

        // legado legado2 (user_version 2, ainda sem __migracoes)
        var legado2 = Path.Combine(Temp, "legado_v2.db");
        File.Copy(Path.Combine(Dados, "banco_v1.db"), legado2);
        Db.Caminho = legado2;
        using (var s = new DbSessao(transacao: true))
        {
            Migracoes.Todas[1].Aplicar(s);
            s.Exec("PRAGMA user_version = 2");
            s.Commit();
        }
        Ok(LocalBanco.Abrir(legado2).Aplicadas.Select(m => m.Versao).SequenceEqual([3, 4]), "legado v2: aplica só as migrações 3 e 4");
        Db.Exec("INSERT INTO __migracoes VALUES(99, 'futuro', '2030-01-01 00:00:00')");
        try { Migracoes.Atualizar(); Ok(false, "banco mais novo deveria ser recusado"); }
        catch (InvalidOperationException) { Ok(true, "banco de versão mais nova que o executável é recusado"); }

        // localização / validação
        var texto = Path.Combine(Temp, "texto.db");
        File.WriteAllText(texto, "não é banco");
        Ok(LocalBanco.Validar(texto) != null, "arquivo que não é SQLite é recusado");
        Ok(LocalBanco.Validar(Path.Combine(Temp, "nao_existe.db")) != null, "arquivo inexistente é recusado");
        LocalBanco.Lembrar(Path.Combine(Temp, "sumiu.db"));
        Ok(LocalBanco.Localizar() == (File.Exists(LocalBanco.AoLadoDoExecutavel) ? LocalBanco.AoLadoDoExecutavel : null),
            "banco lembrado que sumiu não é usado (cai no loja.db ao lado do exe ou pergunta)");

        // banco novo
        var novo = Path.Combine(Temp, "novo", "loja.db");
        LocalBanco.CriarNovo(novo);
        Ok(Migracoes.Aplicadas().Count == 4, "banco novo: 4 migrações aplicadas");
        try { LocalBanco.CriarNovo(novo); Ok(false, "não deveria sobrescrever"); }
        catch (InvalidOperationException) { Ok(true, "criar banco não sobrescreve arquivo existente"); }
        Ok(Senhas.Verificar("admin", (string)Db.Scalar("SELECT senha FROM usuarios WHERE username='ADMIN'")!), "login admin/admin (username sem diferenciar maiúsculas)");
        Ok(!Senhas.Verificar("x", (string)Db.Scalar("SELECT senha FROM usuarios WHERE username='admin'")!), "senha errada rejeitada");
        Sessao.UsuarioId = 1; Sessao.Nome = "Administrador"; Sessao.Admin = true;

        var cli = Convert.ToInt64(Db.Scalar("INSERT INTO clientes(nome) VALUES('João') RETURNING id"));
        var forn = Convert.ToInt64(Db.Scalar("INSERT INTO fornecedores(nome) VALUES('Distribuidora X') RETURNING id"));
        var p1 = Convert.ToInt64(Db.Scalar("INSERT INTO produtos(codigo, codigo_barras, nome, categoria_id, preco_venda, estoque_minimo) VALUES('001','78900011','Arroz 5kg',1,30,5) RETURNING id"));
        var p2 = Convert.ToInt64(Db.Scalar("INSERT INTO produtos(codigo, nome, preco_venda) VALUES('002','Feijão 1kg',9.9) RETURNING id"));

        Operacoes.RegistrarCompra(forn, "123", [new ItemDoc(p1, 10, 5, 0), new ItemDoc(p2, 20, 4, 0)], "PIX", 1, DateTime.Today);
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == 10, "compra soma estoque");
        Operacoes.RegistrarCompra(forn, null, [new ItemDoc(p1, 10, 7, 0)], Operacoes.APrazo, 2, DateTime.Today.AddDays(30));
        Ok(D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id=@p0", p1)) == 6, "custo médio ponderado (5 e 7 => 6)");
        Ok(D(Db.Scalar("SELECT COUNT(*) FROM contas WHERE tipo='PAGAR' AND status='ABERTA'")) == 2, "compra a prazo gera 2 parcelas a pagar");
        Ok(D(Db.Scalar("SELECT SUM(valor) FROM contas WHERE tipo='PAGAR' AND status='ABERTA'")) == 70, "parcelas somam o total da compra");

        var v1 = Operacoes.RegistrarVenda(null, [new ItemDoc(p1, 3, 30, 6), new ItemDoc(p2, 2, 9.9m, 4)], 5, "Dinheiro", 1, DateTime.Today);
        Ok(D(Db.Scalar("SELECT total FROM vendas WHERE id=@p0", v1)) == 104.8m, "total da venda com desconto (90+19,80-5)");
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == 17, "venda baixa estoque");
        Ok((string?)Db.Scalar("SELECT status FROM contas WHERE venda_id=@p0", v1) == "PAGA", "venda à vista gera recebimento pago");

        var v2 = Operacoes.RegistrarVenda(cli, [new ItemDoc(p1, 1, 100, 6)], 0, Operacoes.APrazo, 3, DateTime.Today.AddDays(30));
        var parc = Db.Query("SELECT valor FROM contas WHERE venda_id=@p0 ORDER BY id", v2);
        Ok(parc.Rows.Count == 3 && parc.Rows.Cast<DataRow>().Sum(r => D(r["valor"])) == 100 && D(parc.Rows[2]["valor"]) == 33.34m, "3 parcelas 33,33/33,33/33,34");

        Operacoes.CancelarVenda(v2);
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == 17, "cancelamento devolve estoque");
        Ok(D(Db.Scalar("SELECT COUNT(*) FROM contas WHERE venda_id=@p0 AND status='CANCELADA'", v2)) == 3, "cancelamento cancela contas");
        try { Operacoes.CancelarVenda(v2); Ok(false, "cancelar duas vezes deveria falhar"); } catch (InvalidOperationException) { Ok(true, "cancelar duas vezes bloqueado"); }

        var conta = Convert.ToInt64(Db.Scalar("SELECT id FROM contas WHERE tipo='PAGAR' AND status='ABERTA' LIMIT 1"));
        Operacoes.BaixarConta(conta, DateTime.Today, 35, "PIX");
        Ok((string?)Db.Scalar("SELECT status FROM contas WHERE id=@p0", conta) == "PAGA", "baixa de conta");
        Operacoes.EstornarConta(conta);
        Ok((string?)Db.Scalar("SELECT status FROM contas WHERE id=@p0", conta) == "ABERTA", "estorno de baixa");
        Operacoes.LancarConta("PAGAR", "Aluguel", 1500, 1, DateTime.Today.AddDays(-2), null, null);

        Operacoes.AjustarEstoque(p2, "AJUSTE", 5 - 18, "inventário");
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p2)) == 5, "ajuste de inventário");
        Ok(D(Db.Scalar("SELECT COUNT(*) FROM movimentacoes_estoque")) == 8, "histórico de movimentações registrado");

        var compra2 = Convert.ToInt64(Db.Scalar("SELECT MAX(id) FROM compras"));
        Operacoes.CancelarCompra(compra2);
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == 7, "cancelar compra retira estoque");

        // DataTable com tipos deduzidos
        var dt = Db.Query("SELECT id, nome, estoque, COUNT(*) AS n FROM produtos GROUP BY id");
        Ok(dt.Columns["id"]!.DataType == typeof(long) && dt.Columns["estoque"]!.DataType == typeof(double) && dt.Columns["nome"]!.DataType == typeof(string), "tipagem das colunas");

        // todos os relatórios
        var campo = typeof(RelatoriosForm).GetField("Relatorios", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var r in (Array)campo.GetValue(null)!)
        {
            var nome = (string)r.GetType().GetProperty("Nome")!.GetValue(r)!;
            var sql = (string)r.GetType().GetProperty("Sql")!.GetValue(r)!;
            try { var res = Db.Query(sql, Db.Dia(DateTime.Today.AddDays(-60)), Db.Dia(DateTime.Today.AddDays(90))); Ok(true, $"relatório \"{nome}\" ({res.Rows.Count} linhas)"); }
            catch (Exception ex) { Ok(false, $"relatório \"{nome}\": {ex.Message}"); }
        }

        // comprovante
        var gerar = typeof(CupomForm).GetMethod("Gerar", BindingFlags.NonPublic | BindingFlags.Static)!;
        var cupom = (string)gerar.Invoke(null, [v1])!;
        Ok(cupom.Contains("104,80") && cupom.Contains("Arroz 5kg"), "comprovante gerado");
        Console.WriteLine(cupom);

        Db.Backup(Db.Caminho + ".bak");
        Ok(new FileInfo(Db.Caminho + ".bak").Length > 0, "backup do banco");

        // ---------- orçamento -> pedido -> venda
        var orc = new Pedido { Tipo = Pedidos.Orcamento, ClienteId = cli, Validade = DateTime.Today.AddDays(10), Forma = Operacoes.APrazo, Parcelas = 2, Desconto = 2, Frete = 5 };
        orc.Itens.Add(new PedidoItem { ProdutoId = p1, Quantidade = 2, Unitario = 30, Desconto = 1 });
        var orcId = Pedidos.Salvar(orc);
        Ok(D(Db.Scalar("SELECT total FROM pedidos WHERE id=@p0", orcId)) == 62, "orçamento: 60 - 1 - 2 + 5 frete = 62");
        Ok(Pedidos.Carregar(Pedidos.Duplicar(orcId)).Itens.Count == 1, "duplicar orçamento");
        var ped = Pedidos.AprovarOrcamento(orcId);
        Ok((string?)Db.Scalar("SELECT status FROM pedidos WHERE id=@p0", orcId) == "APROVADO"
           && (string?)Db.Scalar("SELECT tipo FROM pedidos WHERE id=@p0", ped) == "VENDA"
           && Convert.ToInt64(Db.Scalar("SELECT origem_id FROM pedidos WHERE id=@p0", ped)) == orcId, "aprovar orçamento gera pedido de venda");
        try { Pedidos.AprovarOrcamento(orcId); Ok(false, "aprovar 2x deveria falhar"); } catch (InvalidOperationException) { Ok(true, "aprovar 2x bloqueado"); }
        var estoqueAntes = D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1));
        var vendaPed = Pedidos.Faturar(ped, Operacoes.APrazo, 2, DateTime.Today.AddDays(30));
        var vp = Db.Query("SELECT total, frete, desconto FROM vendas WHERE id=@p0", vendaPed).Rows[0];
        Ok(D(vp["total"]) == 62 && D(vp["frete"]) == 5 && D(vp["desconto"]) == 3, "faturar: venda com descontos e frete (62)");
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == estoqueAntes - 2, "faturar baixa estoque");
        Ok(D(Db.Scalar("SELECT SUM(valor) FROM contas WHERE venda_id=@p0", vendaPed)) == 62 && D(Db.Scalar("SELECT COUNT(*) FROM contas WHERE venda_id=@p0", vendaPed)) == 2, "faturar gera 2 parcelas = 62");
        Ok((string?)Db.Scalar("SELECT status FROM pedidos WHERE id=@p0", ped) == "FATURADO", "pedido FATURADO");
        Operacoes.CancelarVenda(vendaPed);
        Ok((string?)Db.Scalar("SELECT status FROM pedidos WHERE id=@p0", ped) == "ABERTO" && D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == estoqueAntes, "cancelar venda reabre o pedido e devolve estoque");

        // ---------- pedido de compra (frete entra no custo)
        var pc = new Pedido { Tipo = Pedidos.Compra, FornecedorId = forn, Frete = 10, Forma = "PIX" };
        pc.Itens.Add(new PedidoItem { ProdutoId = p2, Quantidade = 10, Unitario = 5 });
        var pcId = Pedidos.Salvar(pc);
        var custoAntes = D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id=@p0", p2));
        var estoqueP2 = D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p2));
        Pedidos.Receber(pcId, "PIX", 1, DateTime.Today, "999");
        var custoEsperado = Math.Round((estoqueP2 * custoAntes + 10 * 6) / (estoqueP2 + 10), 4);
        Ok(D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id=@p0", p2)) == custoEsperado, $"receber pedido de compra: custo médio com frete ({custoEsperado})");
        Ok((string?)Db.Scalar("SELECT status FROM pedidos WHERE id=@p0", pcId) == "RECEBIDO", "pedido de compra RECEBIDO");

        // ---------- nota fiscal própria
        Db.SalvarConfig("cnpj", "11.222.333/0001-81"); Db.SalvarConfig("uf", "SP"); Db.SalvarConfig("ie", "111222333444");
        Db.Exec("UPDATE produtos SET ncm='10063021' WHERE id=@p0", p1);
        Db.Exec("UPDATE produtos SET ncm='07133319' WHERE id=@p0", p2);
        Ok(Fiscal.DigitoMod11("5206043300991100250655012000000780026730161") == 5, "DV mod 11 (exemplo do manual da NF-e)");
        var nota = Fiscal.DeVenda(v1);
        Ok(nota.Itens.Count == 2 && nota.Itens[0].Cfop == "5102" && nota.ValorTotal == 104.8m, "nota montada a partir da venda (CFOP 5102, total 104,80)");
        nota.Itens[0].AliqIcms = 18;
        Ok(nota.ValorIcms == 16.2m, "ICMS 18% sobre 90,00 = 16,20");
        var notaId = Fiscal.Salvar(nota);
        Fiscal.Emitir(notaId);
        var nr = Db.Query("SELECT numero, chave, status FROM notas_fiscais WHERE id=@p0", notaId).Rows[0];
        var chave = (string)nr["chave"];
        Ok((long)nr["numero"] == 1 && (string)nr["status"] == "EMITIDA", "emissão: número 1, situação EMITIDA");
        Ok(chave.Length == 44 && chave.StartsWith("35") && chave.Substring(6, 14) == "11222333000181" && Fiscal.DigitoMod11(chave[..43]) == chave[43] - '0', "chave de acesso 44 dígitos com DV válido");
        Ok(Db.Config("proximo_numero_55") == "2", "numeração avançou");
        try { Fiscal.Emitir(notaId); Ok(false, "emitir 2x deveria falhar"); } catch (InvalidOperationException) { Ok(true, "emitir 2x bloqueado"); }
        Ok(Fiscal.NotaExistente("venda_id", v1) == notaId, "venda reconhece nota existente");
        var danfe = Fiscal.Danfe(notaId);
        Ok(danfe.Contains("SEM VALOR FISCAL") && danfe.Contains("104,80") && danfe.Contains("Arroz 5kg"), "DANFE gerado");
        File.WriteAllText(Path.Combine(Telas, "danfe.txt"), danfe);
        var avulsa = Fiscal.Nova(Fiscal.Saida);
        avulsa.PartNome = "Cliente X";
        avulsa.Itens.Add(new NotaItem { Descricao = "Item sem NCM", Quantidade = 1, ValorUnitario = 10, Cfop = "5102", Cst = "102" });
        Ok(Fiscal.Validar(avulsa).Count == 1, "validação acusa NCM faltando");
        var compraNota = Fiscal.DeCompra(Convert.ToInt64(Db.Scalar("SELECT MIN(id) FROM compras")));
        Ok(compraNota.Tipo == Fiscal.Entrada && compraNota.Itens.All(i => i.Cfop == "1102"), "nota de entrada a partir da compra (CFOP 1102)");

        // ---------- importação de XML do fornecedor
        var caminhoXml = Path.Combine(Dados, "nfe_exemplo.xml");
        var xml = ImportacaoXml.Ler(caminhoXml);
        Ok(xml.Itens.Count == 2 && xml.Numero == 4567 && xml.ValorTotal == 436 && xml.Duplicatas.Count == 2 && xml.Chave.Length == 44, "XML lido (2 itens, 2 duplicatas, total 436)");
        Ok(xml.Itens[0].CustoTotal == 262 && xml.Itens[1].CustoTotal == 174, "custo do item inclui frete/IPI e desconta desconto");
        ImportacaoXml.SugerirVinculos(xml);
        Ok(xml.FornecedorId is null && xml.Itens[0].ProdutoId == p1 && xml.Itens[1].ProdutoId is null, "vínculo pelo código de barras; item novo sem vínculo");
        xml.Itens[0].Fator = 10;
        var estoqueXml = D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1));
        var custoXml = D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id=@p0", p1));
        var (nfId, novos) = ImportacaoXml.Importar(xml, true, Operacoes.APrazo);
        Ok(novos == 1, "1 produto novo cadastrado");
        Ok(D(Db.Scalar("SELECT estoque FROM produtos WHERE id=@p0", p1)) == estoqueXml + 20, "fator 10: 2 fardos = 20 unidades no estoque");
        Ok(D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id=@p0", p1)) == Math.Round((estoqueXml * custoXml + 262) / (estoqueXml + 20), 4), "custo médio com o custo efetivo do XML");
        var compraXml = Convert.ToInt64(Db.Scalar("SELECT compra_id FROM notas_fiscais WHERE id=@p0", nfId));
        Ok(D(Db.Scalar("SELECT COUNT(*) FROM contas WHERE compra_id=@p0 AND status='ABERTA'", compraXml)) == 2
           && (string?)Db.Scalar("SELECT vencimento FROM contas WHERE compra_id=@p0 ORDER BY id LIMIT 1", compraXml) == "2026-10-28", "contas a pagar = duplicatas da nota");
        Ok(ImportacaoXml.JaImportada(xml.Chave), "nota marcada como importada");
        try { ImportacaoXml.Importar(xml, true, "PIX"); Ok(false, "importar 2x deveria falhar"); } catch (InvalidOperationException) { Ok(true, "importar 2x bloqueado"); }
        var xml2 = ImportacaoXml.Ler(caminhoXml);
        ImportacaoXml.SugerirVinculos(xml2);
        Ok(xml2.FornecedorId != null && xml2.Itens.All(i => i.ProdutoId != null) && xml2.Itens[0].Fator == 10, "2ª vez: fornecedor e vínculos (com fator) memorizados");
        Ok(Fiscal.Danfe(nfId).Contains("ATACADAO"), "DANFE da nota importada");

        // vendas espalhadas nos últimos dias para o dashboard ter o que mostrar
        var rnd = new Random(7);
        for (int d = 0; d < 25; d++)
        {
            var idv = Operacoes.RegistrarVenda(d % 3 == 0 ? cli : null, [new ItemDoc(p1, 1 + rnd.Next(3), 30, 11), new ItemDoc(p2, 1 + rnd.Next(4), 9.9m, 5)], 0,
                new[] { "Dinheiro", "PIX", "Cartão de crédito", "Cartão de débito" }[rnd.Next(4)], 1, DateTime.Today);
            Db.Exec("UPDATE vendas SET data = datetime('now', 'localtime', @p0, @p1) WHERE id = @p2", $"-{d} day", $"{rnd.Next(-6, 6)} hour", idv);
            Db.Exec("UPDATE contas SET data_pagamento = date('now', 'localtime', @p0) WHERE venda_id = @p1", $"-{d} day", idv);
        }

        // ---------- parâmetros
        Ok(Parametros.MaxParcelas == 12 && Parametros.ValidadeOrcamentoDias == 15 && Parametros.PrazoVencimentoDias == 30, "parâmetros padrão lidos");
        Sessao.Admin = false;
        Ok(Parametros.ValidarDesconto(15, 100) != null && Parametros.ValidarDesconto(10, 100) == null, "desconto acima de 10% bloqueado para usuário comum");
        Sessao.Admin = true;
        Ok(Parametros.ValidarDesconto(50, 100) == null, "administrador pode dar desconto acima do limite");
        Db.SalvarConfig("max_parcelas", "6");
        Ok(Parametros.MaxParcelas == 6, "parâmetro alterado é respeitado");
        Db.SalvarConfig("max_parcelas", "12");
        Ok(Fiscal.Nova(Fiscal.Saida).Natureza == "Venda de mercadoria", "natureza padrão da nota vem dos parâmetros");

        // ---------- recuperação de senha (Resend simulado)
        var resend = new ResendFalso();
        Email.TransporteParaTestes = resend;
        string Erro(Action a) { try { a(); return ""; } catch (Exception ex) { return (ex as AggregateException)?.InnerException?.Message ?? ex.Message; } }
        void Solicitar(string u) => Task.Run(() => RecuperacaoSenha.Solicitar(u)).GetAwaiter().GetResult();
        string CodigoEnviado() => Regex.Match(JsonDocument.Parse(resend.Corpo).RootElement.GetProperty("html").GetString()!, @">(\d{6})<").Groups[1].Value;
        Ok(Erro(() => Solicitar("admin")).Contains("não tem e-mail"), "usuário sem e-mail: recusa com orientação");
        Db.Exec("UPDATE usuarios SET email = 'admin@loja.com' WHERE username = 'admin'");
        Ok(Erro(() => Solicitar("admin")).Contains("não está configurado"), "sem chave do Resend: avisa que falta configurar");
        Db.SalvarConfig("resend_api_key", "re_teste");
        Db.SalvarConfig("resend_remetente", "ERP <nao-responda@loja.com>");
        var mascarado = Task.Run(() => RecuperacaoSenha.Solicitar("ADMIN@loja.com")).GetAwaiter().GetResult();
        Ok(mascarado == "a****@loja.com", "código enviado; e-mail exibido mascarado");
        Ok(resend.Url == "https://api.resend.com/emails" && resend.Autorizacao == "Bearer re_teste" && resend.Corpo.Contains("\"to\":[\"admin@loja.com\"]"),
            "chamada à API do Resend (URL, chave e destinatário)");
        var codigo = CodigoEnviado();
        Ok(codigo.Length == 6, "código de 6 dígitos no e-mail");
        Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM recuperacao_senha WHERE codigo_hash LIKE '%' || @p0 || '%'", codigo)) == 0, "código guardado só como hash");
        var errado = codigo == "000000" ? "111111" : "000000";
        Ok(Erro(() => RecuperacaoSenha.Redefinir("admin", errado, "nova123")).Contains("Restam 4"), "código errado conta tentativa");
        RecuperacaoSenha.Redefinir("admin", codigo, "nova123");
        Ok(Senhas.Verificar("nova123", (string)Db.Scalar("SELECT senha FROM usuarios WHERE username='admin'")!), "código certo redefine a senha");
        Ok(Erro(() => RecuperacaoSenha.Redefinir("admin", codigo, "outra")).Contains("Nenhum código pendente"), "código não pode ser reutilizado");
        Solicitar("admin");
        var codigoExpirado = CodigoEnviado();
        Db.Exec("UPDATE recuperacao_senha SET expira_em = '2000-01-01 00:00:00' WHERE usado = 0");
        Ok(Erro(() => RecuperacaoSenha.Redefinir("admin", codigoExpirado, "nova123")).Contains("expirou"), "código expirado é recusado");
        Solicitar("admin");
        var codigoBloqueado = CodigoEnviado();
        var ultimo = "";
        for (int k = 0; k < 5; k++) ultimo = Erro(() => RecuperacaoSenha.Redefinir("admin", codigoBloqueado == "000000" ? "111111" : "000000", "x1234"));
        Ok(ultimo.Contains("Limite") && Erro(() => RecuperacaoSenha.Redefinir("admin", codigoBloqueado, "x1234")).Contains("Nenhum código pendente"),
            "5 tentativas erradas invalidam o código");
        resend.Status = 403;
        resend.Resposta = "{\"statusCode\":403,\"message\":\"The domain is not verified\"}";
        Ok(Erro(() => Solicitar("admin")).Contains("domain is not verified"), "erro do Resend é mostrado ao usuário");
        resend.Status = 200;
        Db.Exec("UPDATE usuarios SET senha = @p0 WHERE username = 'admin'", Senhas.Hash("admin"));

        // abre todas as telas (executa os SQL de carga)
        Application.EnableVisualStyles();
        Form Privado<T>(params object[] args) =>
            (Form)typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(args);
        var telas = new (string, Func<Form>)[]
        {
            ("Principal", () => new MainForm()),
            ("Clientes", Cadastros.Clientes), ("Fornecedores", Cadastros.Fornecedores), ("Produtos", Cadastros.Produtos),
            ("Categorias", Cadastros.Categorias), ("Usuários", Cadastros.Usuarios),
            ("PDV", () => new PdvForm()),
            ("Vendas", () => new DocumentosForm(true)), ("Compras", () => new DocumentosForm(false)),
            ("Estoque", () => new EstoqueForm()), ("Financeiro", () => new FinanceiroForm()), ("Relatórios", () => new RelatoriosForm()),
            ("Orçamentos", () => new PedidosForm(Pedidos.Orcamento)), ("Pedidos de venda", () => new PedidosForm(Pedidos.Venda)),
            ("Pedidos de compra", () => new PedidosForm(Pedidos.Compra)), ("Notas fiscais", () => new NotasForm()),
            ("Editor de orçamento", () => new PedidoEditorForm(Pedidos.Orcamento, orcId)), ("Editor de pedido novo", () => new PedidoEditorForm(Pedidos.Compra, null)),
            ("Editor de nota", () => new NotaEditorForm(Fiscal.Carregar(notaId))), ("Editor de nota nova", () => new NotaEditorForm(Fiscal.DeCompra(compraXml))),
            ("Conferência de XML", () => { var x = ImportacaoXml.Ler(caminhoXml); ImportacaoXml.SugerirVinculos(x); return Privado<ImportarXmlForm>(x); }),
            ("Busca de produto", () => Privado<BuscaProdutoForm>("arroz")),
            ("Comprovante", () => Privado<CupomForm>("Comprovante", "teste", 480, 9F)),
            ("Configurações", () => new ConfiguracoesForm()),
            ("Banco não encontrado", () => new BancoForm()),
            ("Recuperar senha", () => new RecuperarSenhaForm("admin")),
            ("Ajuda", () => new AjudaForm("PdvForm")),
        };
        foreach (var (nome, criar) in telas)
        {
            try
            {
                var f = criar();
                telasAbertas.Add(f.GetType().Name);
                f.WindowState = FormWindowState.Normal;
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(0, 0);
                f.Size = new Size(1400, 860);
                f.Show();
                Application.DoEvents();
                if (f is MainForm) f.Activate();
                Application.DoEvents();
                // DrawToBitmap não captura outras janelas da área de trabalho (mas desenha os filhos em ordem invertida,
                // por isso o botão "?" é conferido pela estrutura logo abaixo, e não pela imagem)
                using (var bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(Path.Combine(Telas, nome + ".png"));
                }
                var botaoAjuda = f.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "?");
                Ok(f is AjudaForm || f.HelpButton || (botaoAjuda != null && botaoAjuda.Visible && f.Controls.GetChildIndex(botaoAjuda) == 0
                                    && botaoAjuda.Right <= f.ClientSize.Width), $"tela {nome}: botão de ajuda visível");
                f.Dispose();
                Ok(true, $"tela {nome}");
            }
            catch (Exception ex) { Ok(false, $"tela {nome}: {ex}"); }
        }
        // ---------- ajuda em todas as telas
        var semAjuda = Ajuda.ChavesUsadas.Where(k => !Ajuda.Existe(k)).ToList();
        Ok(semAjuda.Count == 0, "toda tela aberta tem tópico de ajuda" + (semAjuda.Count > 0 ? ": " + string.Join(", ", semAjuda) : ""));
        var fontes = Directory.GetFiles(Path.Combine(Raiz, "ErpLoja"), "*.cs", SearchOption.AllDirectories);
        var chavesDialogo = fontes.SelectMany(f => Regex.Matches(File.ReadAllText(f), @"new Dialogo\([^,]+,\s*""([^""]+)""").Select(m => m.Groups[1].Value)).Distinct().ToList();
        Ok(chavesDialogo.Count >= 6 && chavesDialogo.All(Ajuda.Existe), $"todos os diálogos têm ajuda ({chavesDialogo.Count} diálogos)");
        var classesDeTela = typeof(MainForm).Assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Form)) && !x.IsAbstract && x != typeof(Dialogo)).Select(x => x.Name).ToList();
        var naoTestadas = classesDeTela.Where(c => !telasAbertas.Contains(c)).ToList();
        Ok(naoTestadas.Count == 0, "todas as telas do sistema foram abertas no teste" + (naoTestadas.Count > 0 ? ": faltou " + string.Join(", ", naoTestadas) : ""));
        File.WriteAllText(Path.Combine(Raiz, "docs", "manual-do-usuario.md"), Ajuda.ManualMarkdown());
        Ok(true, "manual do usuário gerado em docs/manual-do-usuario.md a partir da ajuda");

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(Temp, recursive: true); } catch { }
    }
    catch (Exception ex) { Ok(false, "exceção: " + ex); }
});
t.SetApartmentState(ApartmentState.STA);
t.Start();
t.Join();
Console.WriteLine(falhas == 0 ? "\nTODOS OS TESTES PASSARAM" : $"\n{falhas} FALHA(S)");
return falhas;

static string AcharRaiz()
{
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    while (d != null && !File.Exists(Path.Combine(d.FullName, "ErpLoja.sln")))
        d = d.Parent;
    return d?.FullName ?? throw new DirectoryNotFoundException("ErpLoja.sln não encontrado acima da pasta do teste.");
}

/// <summary>Substitui a API do Resend: registra a chamada e devolve a resposta configurada.</summary>
class ResendFalso : HttpMessageHandler
{
    public string Url = "", Autorizacao = "", Corpo = "";
    public int Status = 200;
    public string Resposta = "{\"id\":\"teste\"}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
    {
        Url = r.RequestUri!.ToString();
        Autorizacao = r.Headers.Authorization!.ToString();
        Corpo = await r.Content!.ReadAsStringAsync(c);
        return new HttpResponseMessage((HttpStatusCode)Status) { Content = new StringContent(Resposta) };
    }
}
