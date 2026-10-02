using System.Reflection;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O dono importa o XML da NF-e, mas esquece o fator de conversão (fardo com 10). Percebe o estoque errado,
/// cancela a compra em "Compras realizadas" e importa o mesmo XML de novo, agora com fator 10.
/// </summary>
public class ComprasXmlReimportarAposCancelar : ICenario
{
    public string Area => "Compras";

    static Form AbrirConferencia(Usuario u, NotaXml nota)
    {
        var f = (Form)Activator.CreateInstance(typeof(ImportarXmlForm), BindingFlags.NonPublic | BindingFlags.Instance, null, [nota], null)!;
        f.Show();
        Usuario.Esperar();
        return f;
    }

    public void Executar(Usuario u)
    {
        var produto = ComprasUtil.Produtos(1)[0].Id;
        var estoque0 = ComprasUtil.Estoque(produto);
        var produtosAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM produtos"));

        // 1ª importação: vincula o arroz ao produto, mas deixa fator 1 (erro de digitação)
        var nota = ImportacaoXml.Ler(ComprasUtil.CaminhoNfeExemplo);
        ImportacaoXml.SugerirVinculos(nota);
        var conf = AbrirConferencia(u, nota);
        var grid = Usuario.Achar<DataGridView>(conf);
        grid.Rows[0].Cells["produto"].Value = produto;
        grid.Rows[0].Cells["fator"].Value = 1m;
        u.Clicar(conf, "Importar");
        u.Ok(u.Viu("INFO", "Nota importada"), "1ª importação concluída");
        u.Ok(ComprasUtil.Estoque(produto) == estoque0 + 2, "com fator 1 entram só 2 unidades");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM produtos")) == produtosAntes + 1, "item sem vínculo vira produto novo");
        var compra = Convert.ToInt64(Db.Scalar("SELECT compra_id FROM notas_fiscais WHERE chave = @p0", nota.Chave));
        u.Ok(Ui.Dec(Db.Scalar("SELECT SUM(valor) FROM contas WHERE compra_id = @p0 AND status = 'ABERTA'", compra)) == 436m, "duplicatas (2 × 218) lançadas a pagar");

        // cancela a compra em "Compras realizadas"
        var docs = u.AbrirTela("compras");
        var gridDocs = Usuario.Achar<DataGridView>(docs);
        u.Ok(ComprasUtil.SelecionarLinha(gridDocs, compra), "compra importada aparece em Compras realizadas");
        u.Clicar(docs, "Cancelar compra");
        u.Ok(Ui.Txt(Db.Scalar("SELECT status FROM compras WHERE id = @p0", compra)) == "CANCELADA", "compra cancelada");
        u.Ok(ComprasUtil.Estoque(produto) == estoque0, "estoque volta ao saldo anterior");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM contas WHERE compra_id = @p0 AND status <> 'CANCELADA'", compra)) == 0, "contas a pagar da compra canceladas");
        docs.Close();

        // reimporta o mesmo XML, agora com fator 10
        u.Ok(!ImportacaoXml.JaImportada(nota.Chave), "nota cuja compra foi cancelada pode ser importada de novo");
        var nota2 = ImportacaoXml.Ler(ComprasUtil.CaminhoNfeExemplo);
        ImportacaoXml.SugerirVinculos(nota2);
        u.Ok(nota2.Itens[0].ProdutoId == produto && nota2.Itens.All(i => i.ProdutoId != null), "vínculos da 1ª importação foram memorizados");
        var mensagens = u.Mensagens.Count;
        try
        {
            var conf2 = AbrirConferencia(u, nota2);
            var grid2 = Usuario.Achar<DataGridView>(conf2);
            grid2.Rows[0].Cells["fator"].Value = 10m;
            u.Clicar(conf2, "Importar");
        }
        catch (Exception ex)
        {
            u.Ok(false, "abrir a conferência de novo: " + ex.Message);
        }
        u.Ok(u.Mensagens.Skip(mensagens).Any(m => m.Tipo == "INFO" && m.Texto.Contains("Nota importada")),
            "2ª importação concluída" + string.Concat(u.Mensagens.Skip(mensagens).Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        u.Ok(ComprasUtil.Estoque(produto) == estoque0 + 20, "com fator 10 entram 20 unidades");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM produtos")) == produtosAntes + 1, "reimportação não duplica o produto novo");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM notas_fiscais WHERE chave = @p0 AND status <> 'CANCELADA'", nota.Chave)) == 1, "só uma nota ativa com a chave");
        var novaCompra = Db.Scalar("SELECT compra_id FROM notas_fiscais WHERE chave = @p0", nota.Chave) as long?;
        u.Ok(novaCompra is long nc && nc != compra && Ui.Txt(Db.Scalar("SELECT status FROM compras WHERE id = @p0", nc)) == "FINALIZADA", "nota aponta para a nova compra");
        u.Ok(ImportacaoXml.JaImportada(nota.Chave), "depois de reimportada, a nota volta a constar como importada");
    }
}

/// <summary>O comprador escolhe por engano um arquivo que não é XML de NF-e (PDF renomeado, XML vazio, outro XML).</summary>
public class ComprasXmlArquivoInvalido : ICenario
{
    public string Area => "Compras";

    public void Executar(Usuario u)
    {
        var pasta = Path.Combine(Path.GetTempPath(), "erp_cenario_xml_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            var casos = new (string Nome, string Conteudo)[]
            {
                ("lixo.xml", "%PDF-1.4 isto não é xml"),
                ("vazio.xml", ""),
                ("outro.xml", "<?xml version=\"1.0\"?><pedido><item/></pedido>"),
                ("truncado.xml", File.ReadAllText(ComprasUtil.CaminhoNfeExemplo)[..900]),
            };
            foreach (var (nome, conteudo) in casos)
            {
                var caminho = Path.Combine(pasta, nome);
                File.WriteAllText(caminho, conteudo);
                try
                {
                    ImportacaoXml.Ler(caminho);
                    u.Ok(false, $"{nome}: deveria recusar o arquivo");
                }
                catch (InvalidDataException ex)
                {
                    u.Ok(ex.Message.Contains("NF-e") || ex.Message.Contains("XML"), $"{nome}: mensagem clara em português ({ex.Message})");
                }
                catch (Exception ex)
                {
                    u.Ok(false, $"{nome}: erro técnico mostrado ao usuário ({ex.GetType().Name}: {ex.Message})");
                }
            }
        }
        finally
        {
            try { Directory.Delete(pasta, true); } catch { }
        }
    }
}
