using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// O gerente arruma categorias (tenta excluir uma em uso, cria uma repetida, exclui uma vazia) e mexe no custo dos
/// produtos: custo inicial de produto novo é aceito; custo médio de produto com movimentação não é alterado à mão e
/// não perde casas decimais quando outro campo do produto é editado.
/// </summary>
public class CadastrosCategoriasECustoDoProduto : ICenario
{
    public string Area => "Cadastros";

    public void Executar(Usuario u)
    {
        // ---- categorias
        var cats = u.AbrirTela("cadastro_categorias");
        var emUso = Db.Query("SELECT c.id, c.nome FROM categorias c WHERE EXISTS (SELECT 1 FROM produtos p WHERE p.categoria_id = c.id) LIMIT 1").Rows[0];
        u.Ok(u.SelecionarRegistro(cats, (long)emUso["id"]), "categoria em uso aparece na lista");
        u.Clicar(cats, "Excluir");
        u.Ok(u.Viu("ERRO", "Troque a categoria deles") && CadastrosApoio.Contar("categorias", "id = @p0", emUso["id"]) == 1,
            "excluir categoria em uso explica o que fazer e não exclui");

        u.Clicar(cats, "Novo");
        u.Mensagens.Clear();
        u.Preencher(cats, "Nome", Ui.Txt(emUso["nome"]).ToUpperInvariant());
        u.Clicar(cats, "Salvar");
        u.Ok(u.Viu("ERRO", "já existe outro registro com este \"Nome\""), "categoria repetida (maiúsculas) é recusada" + CadastrosApoio.Erros(u));

        u.Mensagens.Clear();
        u.Preencher(cats, "Nome", "Categoria temporária");
        u.Clicar(cats, "Salvar");
        var nova = Convert.ToInt64(Db.Scalar("SELECT id FROM categorias WHERE nome = 'Categoria temporária'") ?? 0L);
        u.Ok(nova > 0 && !u.Viu("ERRO", ""), "categoria nova salva" + CadastrosApoio.Erros(u));
        u.Clicar(cats, "Excluir");
        u.Ok(CadastrosApoio.Contar("categorias", "id = @p0", nova) == 0, "categoria sem produtos é excluída");
        cats.Close();

        // ---- custo do produto
        var tela = u.AbrirTela("cadastro_produtos");
        var comMov = Convert.ToInt64(Db.Scalar("""
            SELECT p.id FROM produtos p WHERE p.ativo = 1 AND p.codigo_barras IS NOT NULL
              AND EXISTS (SELECT 1 FROM movimentacoes_estoque m WHERE m.produto_id = p.id) ORDER BY p.id LIMIT 1
            """));
        Db.Exec("UPDATE produtos SET preco_custo = 5.1234 WHERE id = @p0", comMov);
        u.Clicar(tela, "Atualizar");
        u.Mensagens.Clear();
        u.Ok(u.SelecionarRegistro(tela, comMov), "produto com movimentação aparece na lista");
        u.Preencher(tela, "Preço de venda (R$)", 9.9m);
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", "") && Ui.Dec(Db.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", comMov)) == 5.1234m,
            "editar o preço não arredonda o custo médio (4 casas)" + CadastrosApoio.Erros(u));

        u.Mensagens.Clear();
        u.Preencher(tela, "Preço de custo (R$, custo médio)", 1m);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "não pode ser alterado no cadastro") && Ui.Dec(Db.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", comMov)) == 5.1234m,
            "custo médio de produto com movimentação não é alterado à mão");

        u.Clicar(tela, "Novo");
        u.Mensagens.Clear();
        u.Preencher(tela, "Código", "CUSTO-NOVO");
        u.Preencher(tela, "Descrição", "Produto com custo inicial");
        u.Preencher(tela, "Preço de custo (R$, custo médio)", 7.5m);
        u.Clicar(tela, "Salvar");
        u.Mensagens.Clear();
        u.Preencher(tela, "Preço de custo (R$, custo médio)", 8m);
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", "") && Ui.Dec(Db.Scalar("SELECT preco_custo FROM produtos WHERE codigo = 'CUSTO-NOVO'")) == 8m,
            "custo inicial de produto sem movimentação pode ser informado e corrigido" + CadastrosApoio.Erros(u));
        tela.Close();
    }
}
