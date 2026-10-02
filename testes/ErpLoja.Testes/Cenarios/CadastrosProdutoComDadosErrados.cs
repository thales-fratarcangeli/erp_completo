using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O atendente cadastra um produto com código de barras lido errado, código de barras de outro produto, NCM incompleto e
/// CST com letras; corrige tudo e salva. Depois edita um produto existente sem mexer no código de barras.
/// </summary>
public class CadastrosProdutoComDadosErrados : ICenario
{
    public string Area => "Cadastros";

    public void Executar(Usuario u)
    {
        var tela = u.AbrirTela("cadastro_produtos");
        var antes = CadastrosApoio.Contar("produtos");
        var eanBom = CadastrosApoio.Ean13("789000011122");
        var eanRuim = eanBom[..12] + (char)('0' + (eanBom[12] - '0' + 1) % 10);

        u.Preencher(tela, "Código", "TST-001");
        u.Preencher(tela, "Descrição", "Produto de teste");
        u.Preencher(tela, "Código de barras (EAN)", eanRuim);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "não é um código de barras válido"), "EAN com dígito verificador errado é recusado");

        var outro = Db.Query("SELECT id, codigo_barras, nome FROM produtos WHERE codigo_barras IS NOT NULL ORDER BY id LIMIT 1").Rows[0];
        u.Mensagens.Clear();
        u.Preencher(tela, "Código de barras (EAN)", Ui.Txt(outro["codigo_barras"]));
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "já pertence ao produto") && u.Viu("ERRO", Ui.Txt(outro["nome"])), "código de barras de outro produto é recusado" + CadastrosApoio.Erros(u));

        u.Mensagens.Clear();
        u.Preencher(tela, "Código de barras (EAN)", eanBom);
        u.Preencher(tela, "NCM (8 dígitos) — fiscal", "2202");
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "deve ter 8 dígitos"), "NCM incompleto é recusado");

        u.Mensagens.Clear();
        u.Preencher(tela, "NCM (8 dígitos) — fiscal", "2202.10.00");
        u.Preencher(tela, "CST / CSOSN (ex.: 102 no Simples)", "1O2");
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "aceita só números"), "CST com letra (O no lugar de zero) é recusado");

        u.Mensagens.Clear();
        u.Preencher(tela, "CST / CSOSN (ex.: 102 no Simples)", "102");
        u.Preencher(tela, "Reforma: redução de alíquota IBS %", 150m);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "Redução de alíquota"), "redução de IBS acima de 100% é recusada");

        u.Mensagens.Clear();
        u.Preencher(tela, "Reforma: redução de alíquota IBS %", 60m);
        var codigoExistente = Ui.Txt(Db.Scalar("SELECT codigo FROM produtos ORDER BY id LIMIT 1"));
        u.Preencher(tela, "Código", codigoExistente.ToLowerInvariant());
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "já existe outro registro com este \"Código\""), "código repetido (minúsculas) é recusado dizendo qual campo" + CadastrosApoio.Erros(u));

        u.Mensagens.Clear();
        u.Preencher(tela, "Código", "TST-001");
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", ""), "produto corrigido é salvo" + CadastrosApoio.Erros(u));
        u.Ok(CadastrosApoio.Contar("produtos") == antes + 1 &&
             CadastrosApoio.Contar("produtos", "codigo = 'TST-001' AND codigo_barras = @p0", eanBom) == 1, "produto gravado com o EAN correto");

        // editar produto existente mantendo o próprio código de barras
        u.Mensagens.Clear();
        u.Ok(u.SelecionarRegistro(tela, (long)outro["id"]), "produto existente aparece na lista");
        u.Preencher(tela, "Estoque mínimo", 3m);
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", ""), "editar produto existente não acusa o próprio código de barras" + CadastrosApoio.Erros(u));

        // busca e filtro da lista
        var busca = Usuario.Achar<TextBox>(tela, t => t.PlaceholderText.Contains("filtrar"));
        u.Digitar(busca, "TST-001");
        var grid = Usuario.Achar<DataGridView>(tela);
        u.Ok(grid.Rows.Cast<DataGridViewRow>().Count(r => r.Visible) == 1, "pesquisa pelo código encontra só o produto novo");
        tela.Close();
    }
}
