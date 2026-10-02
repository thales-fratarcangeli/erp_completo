using System.ComponentModel;
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
var resultados = new List<(bool Ok, string Texto)>();
void Ok(bool cond, string msg) { Console.WriteLine((cond ? "OK   " : "FALHA") + " " + msg); resultados.Add((cond, msg)); if (!cond) falhas++; }
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
        Ok(aplicadas.Select(m => m.Versao).SequenceEqual(Enumerable.Range(2, Migracoes.VersaoDoSistema - 1)), $"legado v1: registra a 1 e aplica as migrações 2 a {Migracoes.VersaoDoSistema}");
        Ok(backup != null && File.Exists(backup), "backup automático antes de migrar banco existente");
        Ok(Convert.ToInt64(Db.Scalar("PRAGMA user_version")) == Migracoes.VersaoDoSistema && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM __migracoes")) == Migracoes.VersaoDoSistema, "__migracoes com todas as versões");
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
        Ok(LocalBanco.Abrir(legado2).Aplicadas.Select(m => m.Versao).SequenceEqual(Enumerable.Range(3, Migracoes.VersaoDoSistema - 2)), "legado v2: aplica da migração 3 em diante");
        Db.Exec("INSERT INTO __migracoes VALUES(99, 'futuro', '2030-01-01 00:00:00')");
        try { Migracoes.Atualizar(); Ok(false, "banco mais novo deveria ser recusado"); }
        catch (InvalidOperationException) { Ok(true, "banco de versão mais nova que o executável é recusado"); }

        // ---------- migração 5: clientes + fornecedores -> pessoas, preservando os relacionamentos
        var legado4 = Path.Combine(Temp, "legado_v4.db");
        Db.Caminho = legado4;
        using (var s = new DbSessao(transacao: true, criar: true))
        {
            s.Exec("CREATE TABLE __migracoes (versao INTEGER PRIMARY KEY, nome TEXT NOT NULL, aplicada_em TEXT NOT NULL)");
            foreach (var m in Migracoes.Todas.Take(4))
            {
                m.Aplicar(s);
                s.Exec("INSERT INTO __migracoes VALUES(@p0, @p1, @p2)", m.Versao, m.Nome, DateTime.Now);
            }
            s.Exec("PRAGMA user_version = 4");
            s.Exec("""
                INSERT INTO clientes(id, nome, cpf_cnpj) VALUES (1, 'Cliente Um', '111.444.777-35'), (2, 'Empresa Dupla', '11.222.333/0001-81');
                INSERT INTO fornecedores(id, nome, cnpj, contato) VALUES (1, 'Fornecedor A', '04.252.011/0001-10', 'Carlos'), (2, 'Empresa Dupla Forn', '11222333000181', 'Ana');
                INSERT INTO produtos(id, codigo, nome, estoque) VALUES (1, 'P1', 'Produto 1', 10);
                INSERT INTO vendas(id, data, cliente_id, subtotal, total, forma_pagamento) VALUES (1, '2026-01-10 10:00:00', 2, 50, 50, 'Dinheiro');
                INSERT INTO venda_itens(venda_id, produto_id, quantidade, preco_unitario, total) VALUES (1, 1, 1, 50, 50);
                INSERT INTO compras(id, data, fornecedor_id, total, forma_pagamento) VALUES (1, '2026-01-05 09:00:00', 1, 100, 'PIX'), (2, '2026-01-06 09:00:00', 2, 30, 'PIX');
                INSERT INTO compra_itens(compra_id, produto_id, quantidade, custo_unitario, total) VALUES (1, 1, 10, 10, 100);
                INSERT INTO contas(tipo, descricao, valor, vencimento, status, fornecedor_id, compra_id) VALUES ('PAGAR', 'Compra 1', 100, '2026-02-05', 'ABERTA', 1, 1);
                INSERT INTO contas(tipo, descricao, valor, vencimento, status, cliente_id, venda_id) VALUES ('RECEBER', 'Venda 1', 50, '2026-01-10', 'PAGA', 2, 1);
                INSERT INTO produtos_fornecedor(fornecedor_id, codigo_fornecedor, produto_id, fator) VALUES (1, 'X1', 1, 12);
                """);
            s.Commit();
        }
        var aplicadas4 = Migracoes.Atualizar().Aplicadas.Select(m => m.Versao).ToList();
        Ok(aplicadas4.SequenceEqual(Enumerable.Range(5, Migracoes.VersaoDoSistema - 4)), "banco v4 com dados: aplica da migração 5 em diante");
        Ok(Db.Scalar("SELECT 1 FROM sqlite_master WHERE name IN ('clientes', 'fornecedores')") is null && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pessoas")) == 3,
            "clientes e fornecedores viraram pessoas (mesmo CNPJ unificado: 4 cadastros → 3 pessoas)");
        Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pessoas WHERE eh_cliente = 1 AND eh_fornecedor = 1")) == 1, "pessoa com os dois papéis após a unificação");
        Ok((string?)Db.Scalar("SELECT p.nome FROM compras c JOIN pessoas p ON p.id = c.fornecedor_id WHERE c.id = 1") == "Fornecedor A"
           && (string?)Db.Scalar("SELECT p.nome FROM compras c JOIN pessoas p ON p.id = c.fornecedor_id WHERE c.id = 2") == "Empresa Dupla",
            "compras apontam para a pessoa certa");
        Ok((string?)Db.Scalar("SELECT p.nome FROM contas c JOIN pessoas p ON p.id = c.fornecedor_id WHERE c.compra_id = 1") == "Fornecedor A"
           && (string?)Db.Scalar("SELECT p.nome FROM vendas v JOIN pessoas p ON p.id = v.cliente_id WHERE v.id = 1") == "Empresa Dupla",
            "contas e vendas apontam para a pessoa certa");
        Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM venda_itens")) == 1 && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM compra_itens")) == 1,
            "itens preservados na reconstrução das tabelas (sem exclusão em cascata)");
        Ok(Db.Query("PRAGMA foreign_key_check").Rows.Count == 0
           && !Db.Query("SELECT sql FROM sqlite_master WHERE type = 'table'").Rows.Cast<DataRow>().Any(r => Regex.IsMatch(Convert.ToString(r[0]) ?? "", @"REFERENCES\s+""?(clientes|fornecedores)\b")),
            "integridade referencial ok e nenhuma referência às tabelas antigas");
        Ok(Convert.ToDecimal(Db.Scalar("SELECT fator FROM produtos_fornecedor pf JOIN pessoas p ON p.id = pf.fornecedor_id WHERE p.nome = 'Fornecedor A'")) == 12,
            "vínculos produto × fornecedor preservados");
        try { Db.Exec("INSERT INTO vendas(data, cliente_id, total, forma_pagamento) VALUES('2026-02-01', 999, 1, 'PIX')"); Ok(false, "FK deveria barrar pessoa inexistente"); }
        catch (Microsoft.Data.Sqlite.SqliteException) { Ok(true, "chaves estrangeiras ativas apontando para pessoas"); }

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
        Ok(Migracoes.Aplicadas().Count == Migracoes.VersaoDoSistema, $"banco novo: {Migracoes.VersaoDoSistema} migrações aplicadas");
        try { LocalBanco.CriarNovo(novo); Ok(false, "não deveria sobrescrever"); }
        catch (InvalidOperationException) { Ok(true, "criar banco não sobrescreve arquivo existente"); }
        Ok(Senhas.Verificar("admin", (string)Db.Scalar("SELECT senha FROM usuarios WHERE username='ADMIN'")!), "login admin/admin (username sem diferenciar maiúsculas)");
        Ok(!Senhas.Verificar("x", (string)Db.Scalar("SELECT senha FROM usuarios WHERE username='admin'")!), "senha errada rejeitada");
        Sessao.UsuarioId = 1; Sessao.Nome = "Administrador"; Sessao.Admin = true;

        var cli = Convert.ToInt64(Db.Scalar("INSERT INTO pessoas(nome, eh_cliente) VALUES('João', 1) RETURNING id"));
        var forn = Convert.ToInt64(Db.Scalar("INSERT INTO pessoas(nome, eh_fornecedor) VALUES('Distribuidora X', 1) RETURNING id"));
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
        var cupom = Impressao.SemMarcas(CupomForm.Gerar(v1));
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
        avulsa.Itens.Add(new NotaItem { Descricao = "Item sem NCM", Quantidade = 1, ValorUnitario = 10, Cfop = "5102", Cst = "102", CstIbsCbs = "000", CClassTrib = "000001" });
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

        // ---------- CPF/CNPJ
        Ok(Fiscal.DocumentoValido("111.444.777-35") && !Fiscal.DocumentoValido("111.444.777-36") && Fiscal.DocumentoValido("11.222.333/0001-81")
           && !Fiscal.DocumentoValido("11.111.111/1111-11") && Fiscal.DocumentoValido(""), "validação de CPF/CNPJ pelos dígitos verificadores");

        // ---------- navegação e localizador de telas
        var semTopico = Navegacao.Telas().Where(t => !Ajuda.Existe(t.Chave)).Select(t => t.Chave).ToList();
        Ok(semTopico.Count == 0, "toda entrada do menu tem tópico de ajuda" + (semTopico.Count > 0 ? ": " + string.Join(", ", semTopico) : ""));
        Ok(Navegacao.Telas().GroupBy(t => t.Chave).All(g => g.Count() == 1) && Navegacao.Telas().GroupBy(t => t.Titulo).All(g => g.Count() == 1),
            "menu sem entradas repetidas (uma função, um lugar)");
        Ok(Navegacao.Buscar("boleto", true).FirstOrDefault().Tela?.Chave == "CobrancaForm", "buscar 'boleto' traz a cobrança bancária primeiro");
        Ok(Navegacao.Buscar("ciot", true).Any(r => r.Tela.Chave == "FretesForm") && Navegacao.Buscar("caminhao", true).Any(r => r.Tela.Chave == "cadastro_veiculos"),
            "busca sem acento e por palavra-chave (ciot, caminhão)");
        Ok(Navegacao.Buscar("usuario", false).All(r => !r.Tela.SoAdmin), "telas de administrador não aparecem para usuário comum");
        Ok(Navegacao.Buscar("xyzinexistente", true).Count == 0, "termo sem relação não retorna telas");

        // ---------- crédito do cliente
        Db.Exec("UPDATE pessoas SET limite_credito = 100 WHERE id = @p0", cli);
        var abertoCli = D(Db.Scalar("SELECT COALESCE(SUM(valor), 0) FROM contas WHERE tipo = 'RECEBER' AND status = 'ABERTA' AND cliente_id = @p0", cli));
        Ok(Operacoes.CreditoDisponivel(cli) == 100 - abertoCli, "crédito disponível = limite − contas em aberto");
        Db.Exec("UPDATE pessoas SET limite_credito = 0 WHERE id = @p0", cli);
        Ok(Operacoes.CreditoDisponivel(cli) is null, "limite zero = sem limite");

        // ---------- cobrança bancária (Febraban / CNAB 240)
        Ok(Cobranca.FatorVencimento(new DateTime(2000, 7, 3)) == 1000 && Cobranca.FatorVencimento(new DateTime(2025, 2, 21)) == 9999
           && Cobranca.FatorVencimento(new DateTime(2025, 2, 22)) == 1000, "fator de vencimento (inclusive a virada de 22/02/2025)");
        Ok(Cobranca.DvCodigoBarras("0019373700000001000500940144816060680935031") == 3
           && Cobranca.LinhaDigitavel("00193373700000001000500940144816060680935031") == "00190.50095 40144.816069 06809.350314 3 37370000000100",
            "DV do código de barras e linha digitável (exemplo de referência)");
        Ok(Cobranca.ValidarModeloCampoLivre(Cobranca.ModeloCampoLivrePadrao) is null && Cobranca.ValidarModeloCampoLivre("{agencia:4}{conta:5}") != null
           && Cobranca.ValidarModeloCampoLivre("{banana:25}") != null, "validação do modelo de campo livre");
        var contaBanco = Convert.ToInt64(Db.Scalar("""
            INSERT INTO contas_bancarias(nome, banco_codigo, banco_nome, agencia, conta, conta_dv, carteira, convenio, campo_livre_modelo, multa_pct, juros_mes_pct, ambiente)
            VALUES('Teste', '341', 'Banco Teste', '1234', '56789', '0', '09', '123456', @p0, 2, 1, 'HOMOLOGACAO') RETURNING id
            """, Cobranca.ModeloCampoLivrePadrao));
        var contaReceber = Convert.ToInt64(Db.Scalar("INSERT INTO contas(tipo, descricao, valor, vencimento, status, cliente_id) VALUES('RECEBER', 'Teste boleto', 123.45, @p0, 'ABERTA', @p1) RETURNING id",
            Db.Dia(DateTime.Today.AddDays(10)), cli));
        var boletoId = Cobranca.Emitir(contaBanco, contaReceber);
        var bol = Db.Query("SELECT * FROM boletos WHERE id = @p0", boletoId).Rows[0];
        var cb = (string)bol["codigo_barras"];
        Ok(cb.Length == 44 && cb.StartsWith("3419") && cb[4] - '0' == Cobranca.DvCodigoBarras(cb[..4] + cb[5..]) && cb[9..19] == "0000012345"
           && Fiscal.SoDigitos((string)bol["linha_digitavel"]).Length == 47, "boleto: código de barras de 44 posições, DV e valor; linha digitável de 47 dígitos");
        try { Cobranca.Emitir(contaBanco, contaReceber); Ok(false, "boleto duplicado"); } catch (InvalidOperationException) { Ok(true, "não emite dois boletos ativos para a mesma conta"); }
        var (remessaId, arquivoRem, conteudoRem, qtdRem) = Cobranca.GerarRemessa(contaBanco);
        var linhasRem = conteudoRem.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Ok(qtdRem == 1 && linhasRem.Length == 7 && linhasRem.All(l => l.Length == 240) && Cobranca.ValidarArquivo240(conteudoRem, remessa: true).Count == 0
           && arquivoRem.StartsWith("TESTE_"), "remessa CNAB 240: header, lote, segmentos P/Q/R e trailers com 240 posições, validada");
        Ok((string?)Db.Scalar("SELECT status FROM boletos WHERE id = @p0", boletoId) == "ENVIADO", "boleto marcado como enviado na remessa");
        Ok(Cobranca.ValidarArquivo240(conteudoRem.Replace(linhasRem[3], linhasRem[3][..239]), true).Count > 0, "validador acusa linha fora do padrão");
        var retornoSim = Cobranca.SimularRetorno(remessaId, liquidar: true);
        Ok(Cobranca.ValidarArquivo240(retornoSim, remessa: false).Count == 0, "retorno simulado no layout CNAB 240");
        var resRet = Cobranca.ProcessarRetorno(contaBanco, "teste.ret", retornoSim);
        Ok(resRet.Confirmados == 1 && resRet.Liquidados == 1 && resRet.ValorLiquidado == 123.45m, "retorno: entrada confirmada (02) e liquidação (06)");
        Ok((string?)Db.Scalar("SELECT status FROM boletos WHERE id = @p0", boletoId) == "PAGO" && (string?)Db.Scalar("SELECT status FROM contas WHERE id = @p0", contaReceber) == "PAGA",
            "liquidação baixa o boleto e a conta a receber");
        Ok(Cobranca.Texto(boletoId).Contains("HOMOLOGAÇÃO"), "boleto de homologação identificado");

        // ---------- produção
        var farinha = Convert.ToInt64(Db.Scalar("INSERT INTO produtos(codigo, nome, unidade, preco_custo, estoque, tipo) VALUES('INS1', 'Farinha', 'KG', 4, 100, 'INSUMO') RETURNING id"));
        var fermento = Convert.ToInt64(Db.Scalar("INSERT INTO produtos(codigo, nome, unidade, preco_custo, estoque, tipo) VALUES('INS2', 'Fermento', 'KG', 20, 5, 'INSUMO') RETURNING id"));
        var pao = Convert.ToInt64(Db.Scalar("INSERT INTO produtos(codigo, nome, unidade, preco_venda) VALUES('PAO', 'Pão', 'KG', 15) RETURNING id"));
        Producao.SalvarFicha(new Producao.Ficha(null, pao, 10, 5, "", new BindingList<InsumoFicha>
            { new() { InsumoId = farinha, Quantidade = 6 }, new() { InsumoId = fermento, Quantidade = 0.1m } }));
        Ok((string?)Db.Scalar("SELECT tipo FROM produtos WHERE id = @p0", pao) == "PRODUTO_ACABADO", "ficha técnica marca o produto como produzido");
        try { Producao.SalvarFicha(new Producao.Ficha(null, pao, 10, 0, "", new BindingList<InsumoFicha> { new() { InsumoId = pao, Quantidade = 1 } })); Ok(false, ""); }
        catch (InvalidOperationException) { Ok(true, "produto não pode ser insumo da própria ficha"); }
        var op = Producao.CriarOrdem(pao, 20, DateTime.Today, "teste");
        Ok(D(Db.Scalar("SELECT quantidade_prevista FROM op_itens WHERE op_id = @p0 AND insumo_id = @p1", op, farinha)) == 12,
            "OP calcula a necessidade pela ficha (20 kg = 2 receitas = 12 kg de farinha)");
        Producao.Iniciar(op);
        var custoOp = Producao.Concluir(op, 20);
        Ok(custoOp == 3.1m && D(Db.Scalar("SELECT estoque FROM produtos WHERE id = @p0", pao)) == 20 && D(Db.Scalar("SELECT estoque FROM produtos WHERE id = @p0", farinha)) == 88,
            "concluir OP: baixa insumos, entra o produto e apura o custo real (48 + 4 + 10 = 62 → R$ 3,10/kg)");
        Ok(D(Db.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", pao)) == 3.1m, "custo médio do produto acabado atualizado");
        try { Producao.Concluir(op, 1); Ok(false, ""); } catch (InvalidOperationException) { Ok(true, "OP concluída não é concluída de novo"); }
        Ok(Producao.Texto(op).Contains("ORDEM DE PRODUÇÃO"), "impressão da OP");

        // ---------- fretes e CIOT
        var tac = Convert.ToInt64(Db.Scalar("INSERT INTO pessoas(nome, cpf_cnpj, eh_transportador, eh_motorista, rntrc, cnh) VALUES('Autônomo', '111.444.777-35', 1, 1, '12345678', '0123') RETURNING id"));
        var vei = Convert.ToInt64(Db.Scalar("INSERT INTO veiculos(placa, tipo, frota_propria, proprietario_id) VALUES('ABC1D23', 'TRUCK', 0, @p0) RETURNING id", tac));
        Ok(Fretes.CiotObrigatorio("CONTRATADO", "111.444.777-35") && !Fretes.CiotObrigatorio("CONTRATADO", "11.222.333/0001-81") && !Fretes.CiotObrigatorio("PROPRIO", "111.444.777-35"),
            "CIOT obrigatório só para transportador autônomo contratado");
        long NovoFrete(long? motorista) => Convert.ToInt64(Db.Scalar("""
            INSERT INTO fretes(data, modalidade, transportador_id, motorista_id, veiculo_id, origem_cidade, origem_uf, destino_cidade, destino_uf,
                               valor_frete, valor_pedagio, valor_adiantamento, peso_kg, ciot_status)
            VALUES(@p0, 'CONTRATADO', @p1, @p2, @p3, 'Campinas', 'SP', 'Curitiba', 'PR', 2000, 100, 800, 5000, 'PENDENTE') RETURNING id
            """, DateTime.Today, tac, motorista, vei));
        var frete = NovoFrete(tac);
        Db.SalvarConfig("ciot_administradora", "PAMCARD");
        try { Fretes.DeclararCiot(frete).GetAwaiter().GetResult(); Ok(false, ""); }
        catch (InvalidOperationException ex) { Ok(ex.Message.Contains("não está configurada"), "Pamcard sem credenciais: orienta a configurar"); }
        var pam = new ResendFalso { Resposta = "<retorno><sucesso>true</sucesso><ciot>123456789012</ciot><protocolo>P1</protocolo><mensagem>OK</mensagem></retorno>" };
        PamcardAdministradora.TransporteParaTestes = pam;
        Db.SalvarConfig("pamcard_url", "https://homologacao.exemplo/pamcard");
        Db.SalvarConfig("pamcard_usuario", "usuario");
        Db.SalvarConfig("pamcard_senha", "senha");
        Db.SalvarConfig("pamcard_cnpj_contratante", "11.222.333/0001-81");
        var rPam = Fretes.DeclararCiot(frete).GetAwaiter().GetResult();
        Ok(rPam.Sucesso && (string?)Db.Scalar("SELECT ciot FROM fretes WHERE id = @p0", frete) == "123456789012" && pam.Url.EndsWith("/declarar")
           && pam.Corpo.Contains("<declararOperacaoTransporte>") && pam.Corpo.Contains("<rntrc>12345678</rntrc>") && pam.Autorizacao.StartsWith("Basic "),
            "Pamcard: envia a declaração (autenticada) e grava o CIOT devolvido");
        try { Fretes.MudarStatus(frete, "CANCELADO"); Ok(false, ""); } catch (InvalidOperationException) { Ok(true, "frete com CIOT declarado não é cancelado antes do CIOT"); }
        Ok(Fretes.GerarContas(frete) == 2 && D(Db.Scalar("SELECT SUM(valor) FROM contas WHERE frete_id = @p0", frete)) == 2100,
            "contas do frete: adiantamento (800) + saldo (1.200 + 100 de pedágio)");
        try { Fretes.GerarContas(frete); Ok(false, ""); } catch (InvalidOperationException) { Ok(true, "contas do frete geradas uma única vez"); }
        Fretes.MudarStatus(frete, "EM_TRANSITO");
        Fretes.MudarStatus(frete, "ENTREGUE");
        Ok((string?)Db.Scalar("SELECT status FROM fretes WHERE id = @p0", frete) == "ENTREGUE", "frete em trânsito → entregue");
        Db.SalvarConfig("ciot_administradora", "SIMULADOR");
        var frete2 = NovoFrete(tac);
        Ok(Fretes.DeclararCiot(frete2).GetAwaiter().GetResult().Sucesso && Ui.Txt(Db.Scalar("SELECT ciot FROM fretes WHERE id = @p0", frete2)).Length == 16,
            "CIOT pelo simulador");
        Ok(Fretes.CancelarCiot(frete2, "Viagem desmarcada pelo cliente").GetAwaiter().GetResult().Sucesso
           && (string?)Db.Scalar("SELECT ciot_status FROM fretes WHERE id = @p0", frete2) == "CANCELADO", "cancelamento do CIOT");
        var (_, errosFrete) = Fretes.MontarOperacao(NovoFrete(null));
        Ok(errosFrete.Any(e => e.Contains("motorista")), "declaração do CIOT exige motorista com CPF válido");

        // ---------- TEF e impressão
        Db.SalvarConfig("tef_provedor", "SITEF");
        Ok(!Tef.Provedor().Pagar(null!, 10, ModalidadeTef.Debito, 1, "1").Aprovada, "provedor TEF real ausente: recusa com explicação (não simula)");
        Db.SalvarConfig("tef_provedor", "SIMULADOR");
        Ok(Tef.Provedor() is TefSimulador && Tef.Comprovante("VIA DO CLIENTE", "VISA", ModalidadeTef.Credito, 3, 99.9m, "1", "2").Contains("99,90"), "TEF simulador e comprovante");
        var escpos = Impressao.EscPos("[C][N]Título\nAção", 48, 850, cortar: true, abrirGaveta: true);
        Ok(escpos[0] == 0x1B && escpos[1] == 0x40 && Contem(escpos, [0x1D, 0x56, 0x42]) && Contem(escpos, [0x1B, 0x70]) && Contem(escpos, [0x1B, 0x61, 0x01])
           && escpos.Contains((byte)0x87), "ESC/POS: inicializa, centraliza, corta, abre a gaveta e acentua em CP850 (ç = 0x87)");
        Ok(Impressao.SemMarcas("[C][N]Olá") == "Olá" && Parametros.Definicoes.First(d => d.Chave == "impressora_cupom").OpcoesDinamicas!()[0] == "",
            "marcações removidas na tela; lista de impressoras instaladas");

        // ---------- reforma tributária
        Db.Exec("UPDATE produtos SET red_cbs_pct = 60, red_ibs_pct = 60, cst_ibscbs = '200', cclass_trib = '200003' WHERE id = @p0", p2);
        var itemRef = Fiscal.ItemDeProduto(p2, 10, 10, "5102");
        Ok(itemRef.AliqCbs == 0.36m && itemRef.AliqIbsUf == 0.04m && itemRef.ValorCbs == 0.36m && itemRef.CstIbsCbs == "200",
            "IBS/CBS com redução de 60% (CBS 0,9% → 0,36%)");
        var notaRef = Fiscal.Nova(Fiscal.Saida);
        notaRef.PartNome = "Cliente reforma";
        notaRef.Itens.Add(itemRef);
        Ok(notaRef.ValorTotal == 100 && notaRef.ValorCbs == 0.36m, "IBS/CBS destacados sem somar ao total da nota (regra de 2026)");
        var idRef = Fiscal.Salvar(notaRef);
        Ok(D(Db.Scalar("SELECT valor_cbs FROM notas_fiscais WHERE id = @p0", idRef)) == 0.36m && D(Db.Scalar("SELECT aliq_cbs FROM nota_itens WHERE nota_id = @p0", idRef)) == 0.36m
           && Fiscal.Carregar(idRef).Itens[0].CClassTrib == "200003", "campos da reforma gravados e recarregados");
        Db.SalvarConfig("reforma_ativa", "NAO");
        Ok(Fiscal.ItemDeProduto(p2, 1, 10, "5102").AliqCbs == 0, "reforma desligada não calcula IBS/CBS");
        Db.SalvarConfig("reforma_ativa", "SIM");
        Fiscal.Emitir(idRef);
        Ok(Fiscal.Danfe(idRef).Contains("REFORMA TRIBUTÁRIA"), "DANFE com o quadro da reforma");

        // abre todas as telas (executa os SQL de carga)
        Application.EnableVisualStyles();
        Form Privado<T>(params object[] args) =>
            (Form)typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(args);
        var telas = new (string, Func<Form>)[]
        {
            ("Principal", () => new MainForm()),
            ("Clientes e fornecedores", Cadastros.Pessoas), ("Produtos", Cadastros.Produtos),
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
            ("Comprovante", () => Privado<CupomForm>("Comprovante", "teste", 480, 9F, false)),
            ("Configurações", () => new ConfiguracoesForm()),
            ("Banco não encontrado", () => new BancoForm()),
            ("Recuperar senha", () => new RecuperarSenhaForm("admin")),
            ("Ajuda", () => new AjudaForm("PdvForm")),
            ("Veículos", Cadastros.Veiculos), ("Contas bancárias", Cadastros.ContasBancarias),
            ("Fichas técnicas", () => new FichasTecnicasForm()), ("Ordens de produção", () => new OrdensProducaoForm()),
            ("Fretes", () => new FretesForm()), ("Cobrança", () => new CobrancaForm()), ("Boleto", () => Privado<BoletoForm>(boletoId)),
            ("Localizar tela", () => Privado<LocalizarTelaForm>()), ("Pagamento", () => new PagamentoForm(123.45m, true)),
            ("TEF simulador", () => new TefSimuladorForm(50m, ModalidadeTef.Credito, 3)), ("Painel gerencial", () => new DashboardForm()),
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
                                    && botaoAjuda.Right <= f.ClientSize.Width), $"tela {nome}: botão de ajuda visível" + (botaoAjuda is null ? " (sem botão)" : $" [visível={botaoAjuda.Visible}, índice={f.Controls.GetChildIndex(botaoAjuda)}, direita={botaoAjuda.Right}/{f.ClientSize.Width}]"));
                f.Dispose();
                Ok(true, $"tela {nome}");
            }
            catch (Exception ex) { Ok(false, $"tela {nome}: {ex}"); }
        }
        // ---------- ajuda: uma explicação por tela (sem lista com todos os tópicos)
        using (var ajudaPdv = new AjudaForm("PdvForm"))
        {
            static IEnumerable<Control> Todos(Control c) => c.Controls.Cast<Control>().SelectMany(x => Todos(x).Prepend(x));
            var textoAjuda = Todos(ajudaPdv).OfType<RichTextBox>().Single().Text;
            Ok(!Todos(ajudaPdv).OfType<ListBox>().Any() && textoAjuda.Contains("PDV") && !textoAjuda.Contains("Cobrança bancária") && ajudaPdv.Text.Contains("PDV"),
                "o ? de cada tela abre só a explicação daquela tela");
        }

        // ---------- ajuda em todas as telas
        var semAjuda = Ajuda.ChavesUsadas.Where(k => !Ajuda.Existe(k)).ToList();
        Ok(semAjuda.Count == 0, "toda tela aberta tem tópico de ajuda" + (semAjuda.Count > 0 ? ": " + string.Join(", ", semAjuda) : ""));
        var fontes = Directory.GetFiles(Path.Combine(Raiz, "ErpLoja"), "*.cs", SearchOption.AllDirectories);
        var chavesDialogo = fontes.SelectMany(f => Regex.Matches(File.ReadAllText(f), @"new Dialogo\([^,]+,\s*""([^""]+)""").Select(m => m.Groups[1].Value)).Distinct().ToList();
        Ok(chavesDialogo.Count >= 6 && chavesDialogo.All(Ajuda.Existe), $"todos os diálogos têm ajuda ({chavesDialogo.Count} diálogos)");
        var classesDeTela = typeof(MainForm).Assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Form)) && !x.IsAbstract && x != typeof(Dialogo) && x != typeof(Toast)).Select(x => x.Name).ToList();
        var naoTestadas = classesDeTela.Where(c => !telasAbertas.Contains(c)).ToList();
        Ok(naoTestadas.Count == 0, "todas as telas do sistema foram abertas no teste" + (naoTestadas.Count > 0 ? ": faltou " + string.Join(", ", naoTestadas) : ""));
        File.WriteAllText(Path.Combine(Raiz, "docs", "12-manual-do-usuario.md"), Ajuda.ManualMarkdown());
        Ok(true, "manual do usuário gerado em docs/12-manual-do-usuario.md a partir da ajuda");

        // ---------- dados de demonstração (banco separado)
        var demo = Path.Combine(Temp, "demo", "loja.db");
        LocalBanco.CriarNovo(demo);
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var resumoDemo = DadosDemo.Gerar();
        Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM vendas")) > 500 && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM ordens_producao WHERE status = 'CONCLUIDA'")) >= 70
           && Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM notas_fiscais WHERE status = 'EMITIDA'")) == 5,
            $"dados de demonstração gerados em {cronometro.Elapsed.TotalSeconds:N1} s — {resumoDemo}");
        Ok(Db.Query("PRAGMA foreign_key_check").Rows.Count == 0, "dados de demonstração íntegros (chaves estrangeiras)");
        Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM produtos WHERE tipo = 'INSUMO' AND estoque < 0")) == 0, "insumos não ficam negativos na demonstração");
        Ok(Db.Query("SELECT cpf_cnpj FROM pessoas WHERE cpf_cnpj IS NOT NULL").Rows.Cast<DataRow>().All(r => Fiscal.DocumentoValido(Convert.ToString(r[0]))),
            "CPF/CNPJ da demonstração são válidos");
        File.WriteAllText(Path.Combine(Raiz, "docs", "06-modelo-de-dados.md"), ModeloDeDados());
        Ok(true, "modelo de dados (dicionário + diagrama ER) gerado em docs/06-modelo-de-dados.md");
        foreach (var (nome, criar) in new (string, Func<Form>)[] { ("Principal (demo)", () => new MainForm()), ("Painel gerencial (demo)", () => new DashboardForm()), ("PDV (demo)", () => new PdvForm()),
                     ("Fretes (demo)", () => new FretesForm()), ("Ordens de produção (demo)", () => new OrdensProducaoForm()), ("Cobrança (demo)", () => new CobrancaForm()) })
        {
            var f = criar();
            f.WindowState = FormWindowState.Normal;
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(0, 0);
            f.Size = new Size(1400, 860);
            f.Show();
            Application.DoEvents();
            Thread.Sleep(700);
            Application.DoEvents();
            using (var bmp = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(Path.Combine(Telas, nome + ".png"));
            }
            f.Dispose();
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(Temp, recursive: true); } catch { }
    }
    catch (Exception ex) { Ok(false, "exceção: " + ex); }
});
t.SetApartmentState(ApartmentState.STA);
t.Start();
t.Join();
Console.WriteLine(falhas == 0 ? "\nTODOS OS TESTES PASSARAM" : $"\n{falhas} FALHA(S)");
RelatorioDeTestes();
return falhas;

void RelatorioDeTestes()
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# Relatório de testes");
    sb.AppendLine();
    sb.AppendLine("> Gerado automaticamente pela bateria `testes/ErpLoja.Testes` a cada execução. Não edite à mão.");
    sb.AppendLine();
    sb.AppendLine("| Item | Valor |");
    sb.AppendLine("|---|---|");
    sb.AppendLine($"| Data da execução | {DateTime.Now:dd/MM/yyyy HH:mm} |");
    sb.AppendLine($"| Versão do sistema | {typeof(MainForm).Assembly.GetName().Version} (banco v{Migracoes.VersaoDoSistema}) |");
    sb.AppendLine($"| Ambiente | {System.Runtime.InteropServices.RuntimeInformation.OSDescription} · {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} |");
    sb.AppendLine($"| Verificações | {resultados.Count} |");
    sb.AppendLine($"| Aprovadas | {resultados.Count(r => r.Ok)} |");
    sb.AppendLine($"| Reprovadas | {resultados.Count(r => !r.Ok)} |");
    sb.AppendLine($"| Resultado | {(falhas == 0 ? "**APROVADO**" : "**REPROVADO**")} |");
    sb.AppendLine();
    sb.AppendLine("## Verificações");
    sb.AppendLine();
    sb.AppendLine("| # | Resultado | Verificação |");
    sb.AppendLine("|---:|:---:|---|");
    for (int i = 0; i < resultados.Count; i++)
        sb.AppendLine($"| {i + 1} | {(resultados[i].Ok ? "✅" : "❌")} | {resultados[i].Texto.Replace("|", "\\|")} |");
    File.WriteAllText(Path.Combine(Raiz, "docs", "08-relatorio-de-testes.md"), sb.ToString());
}

static string ModeloDeDados()
{
    var descricoes = new Dictionary<string, string>
    {
        ["__migracoes"] = "Controle de versão do banco: migrações aplicadas.",
        ["boletos"] = "Boletos emitidos (nosso número, código de barras, linha digitável) e sua situação na cobrança.",
        ["categorias"] = "Agrupamento de produtos para relatórios e painel.",
        ["compra_itens"] = "Itens de cada compra, com o custo unitário de entrada.",
        ["compras"] = "Entradas de mercadoria (pedido de compra recebido ou XML importado).",
        ["configuracoes"] = "Parâmetros do sistema (chave/valor), editados na tela Configurações.",
        ["contas"] = "Contas a receber e a pagar (parcelas), geradas por vendas, compras, fretes ou lançamento manual.",
        ["contas_bancarias"] = "Contas de cobrança: dados do banco, modelo do campo livre, sequências de nosso número e de remessa.",
        ["ficha_itens"] = "Insumos de cada ficha técnica, com quantidade e perda.",
        ["fichas_tecnicas"] = "Receita de cada produto fabricado: rendimento e custo adicional por lote.",
        ["fretes"] = "Fretes (frota própria ou contratados), valores, situação da viagem e dados do CIOT.",
        ["movimentacoes_estoque"] = "Histórico de toda alteração de saldo de estoque, com o saldo resultante.",
        ["nota_itens"] = "Itens das notas fiscais com tributos (ICMS, IPI, PIS, COFINS, IBS, CBS, IS).",
        ["notas_fiscais"] = "Notas fiscais próprias (saída/entrada) e importadas de fornecedores (XML).",
        ["op_itens"] = "Insumos de cada ordem de produção: previsto e consumido.",
        ["ordens_producao"] = "Ordens de produção e seu custo real apurado.",
        ["pedido_itens"] = "Itens de orçamentos e pedidos.",
        ["pedidos"] = "Orçamentos, pedidos de venda e pedidos de compra.",
        ["pessoas"] = "Cadastro único de clientes, fornecedores, transportadores e motoristas.",
        ["produtos"] = "Produtos (mercadorias, produtos acabados e insumos), preços, estoque e dados fiscais.",
        ["produtos_fornecedor"] = "Vínculo entre o código do produto no fornecedor e o produto do sistema (importação de XML).",
        ["recuperacao_senha"] = "Códigos de recuperação de senha (somente o hash), validade e tentativas.",
        ["remessas"] = "Arquivos de remessa CNAB 240 gerados.",
        ["retornos"] = "Arquivos de retorno CNAB 240 processados.",
        ["transacoes_tef"] = "Transações de cartão/PIX pelo TEF (duas fases: pendente → confirmada/desfeita).",
        ["usuarios"] = "Usuários do sistema (username, senha em hash, e-mail, perfil).",
        ["veiculos"] = "Veículos da frota própria e de terceiros.",
        ["venda_itens"] = "Itens de cada venda, com o custo médio do momento (lucro bruto).",
        ["vendas"] = "Vendas (PDV e pedidos faturados).",
    };
    var tabelas = Db.Query("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
        .Rows.Cast<DataRow>().Select(r => (string)r[0]).ToList();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# Modelo de dados");
    sb.AppendLine();
    sb.AppendLine("> Gerado automaticamente a partir do esquema real do banco (versão " + Migracoes.VersaoDoSistema + ") pela bateria de testes. Não edite à mão.");
    sb.AppendLine("> Para alterar o banco, crie uma migração (ver [arquitetura](05-arquitetura.md#migrations)).");
    sb.AppendLine();
    sb.AppendLine("Convenções: datas com hora em texto `yyyy-MM-dd HH:mm:ss`; datas sem hora `yyyy-MM-dd`; valores monetários em REAL com 2 casas;");
    sb.AppendLine("booleanos em INTEGER (0/1); chaves estrangeiras verificadas (`PRAGMA foreign_keys = ON`).");
    sb.AppendLine();
    sb.AppendLine("## Diagrama entidade-relacionamento");
    sb.AppendLine();
    sb.AppendLine("```mermaid");
    sb.AppendLine("erDiagram");
    foreach (var t in tabelas.Where(t => !t.StartsWith("__") && t != "configuracoes"))
    {
        sb.AppendLine($"    {t} {{");
        var fks = Db.Query($"PRAGMA foreign_key_list({t})").Rows.Cast<DataRow>().Select(r => (string)r["from"]).ToHashSet();
        foreach (DataRow c in Db.Query($"PRAGMA table_info({t})").Rows)
        {
            var tipo = Convert.ToString(c["type"]) is { Length: > 0 } tp ? tp.Split(' ')[0] : "TEXT";
            var marca = Convert.ToInt64(c["pk"]) > 0 ? " PK" : fks.Contains((string)c["name"]) ? " FK" : "";
            sb.AppendLine($"        {tipo} {c["name"]}{marca}");
        }
        sb.AppendLine("    }");
    }
    foreach (var t in tabelas)
        foreach (DataRow fk in Db.Query($"PRAGMA foreign_key_list({t})").Rows)
            if (fk["table"] is string pai && pai != t)
                sb.AppendLine($"    {pai} ||--o{{ {t} : \"{fk["from"]}\"");
    sb.AppendLine("```");
    sb.AppendLine();
    sb.AppendLine("## Dicionário de dados");
    foreach (var t in tabelas)
    {
        sb.AppendLine();
        sb.AppendLine($"### {t}");
        sb.AppendLine();
        if (descricoes.TryGetValue(t, out var d))
            sb.AppendLine(d + Environment.NewLine);
        var fks = Db.Query($"PRAGMA foreign_key_list({t})").Rows.Cast<DataRow>()
            .ToDictionary(r => (string)r["from"], r => $"{r["table"]}.{r["to"]}" + (Convert.ToString(r["on_delete"]) is "CASCADE" ? " (cascata)" : ""));
        sb.AppendLine("| Coluna | Tipo | Obrigatória | Padrão | Chave |");
        sb.AppendLine("|---|---|:---:|---|---|");
        foreach (DataRow c in Db.Query($"PRAGMA table_info({t})").Rows)
        {
            var nome = (string)c["name"];
            var chave = Convert.ToInt64(c["pk"]) > 0 ? "PK" : fks.TryGetValue(nome, out var refe) ? "FK → " + refe : "";
            sb.AppendLine($"| `{nome}` | {c["type"]} | {(Convert.ToInt64(c["notnull"]) == 1 || Convert.ToInt64(c["pk"]) > 0 ? "sim" : "")} | {Convert.ToString(c["dflt_value"])?.Replace("|", "\\|")} | {chave} |");
        }
        var indices = Db.Query("SELECT name, sql FROM sqlite_master WHERE type = 'index' AND tbl_name = @p0 AND sql IS NOT NULL", t).Rows.Cast<DataRow>().ToList();
        if (indices.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Índices: " + string.Join("; ", indices.Select(i => $"`{i["name"]}`")));
        }
        var linhas = Convert.ToInt64(Db.Scalar($"SELECT COUNT(*) FROM \"{t}\""));
        sb.AppendLine();
        sb.AppendLine($"Registros no banco de demonstração: {linhas:N0}.");
    }
    return sb.ToString();
}

static bool Contem(byte[] dados, byte[] sequencia)
{
    for (int i = 0; i + sequencia.Length <= dados.Length; i++)
        if (dados.AsSpan(i, sequencia.Length).SequenceEqual(sequencia))
            return true;
    return false;
}

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
