using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// A atendente cadastra um cliente às pressas: digita CPF errado, esquece o papel, tenta cadastrar de novo uma pessoa que
/// já existe (com o CPF formatado de outro jeito), corrige e salva; depois inativa um cliente com histórico de vendas.
/// </summary>
public class CadastrosAtendenteCadastraPessoa : ICenario
{
    public string Area => "Cadastros";

    public void Executar(Usuario u)
    {
        var tela = u.AbrirTela("cadastro_pessoas");
        var antes = CadastrosApoio.Contar("pessoas");

        // CPF com dígito errado
        u.Preencher(tela, "Nome / razão social", "Maria Apressada");
        u.Preencher(tela, "CPF/CNPJ", "123.456.789-00");
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "CPF/CNPJ inválido"), "CPF com dígito errado é recusado");

        // nenhum papel marcado
        var cpfNovo = CadastrosApoio.Cpf("529982247");
        u.Preencher(tela, "CPF/CNPJ", cpfNovo);
        u.Preencher(tela, "Cliente", false);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "pelo menos um papel"), "sem papel marcado é recusado");
        u.Preencher(tela, "Cliente", true);

        // pessoa que já existe, com o documento digitado sem pontuação
        var existente = Db.Query("SELECT id, nome, cpf_cnpj FROM pessoas WHERE length(cpf_cnpj) >= 11 ORDER BY id LIMIT 1").Rows[0];
        var docExistente = Fiscal.SoDigitos(Ui.Txt(existente["cpf_cnpj"]));
        u.Mensagens.Clear();
        u.Preencher(tela, "CPF/CNPJ", docExistente);
        u.Clicar(tela, "Salvar");
        u.Ok(u.Viu("ERRO", "Já existe um cadastro com este CPF/CNPJ") && u.Viu("ERRO", Ui.Txt(existente["nome"])),
            "CPF/CNPJ de pessoa já cadastrada é recusado e a mensagem diz quem é" + CadastrosApoio.Erros(u));
        u.Ok(CadastrosApoio.Contar("pessoas") == antes, "nenhuma pessoa duplicada gravada");

        // corrige e salva
        u.Mensagens.Clear();
        u.Preencher(tela, "CPF/CNPJ", cpfNovo);
        u.Preencher(tela, "E-mail", "maria@exemplo.com.br");
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", ""), "cadastro corrigido é salvo sem erro" + CadastrosApoio.Erros(u));
        u.Ok(CadastrosApoio.Contar("pessoas", "nome = 'Maria Apressada' AND cpf_cnpj = @p0", cpfNovo) == 1, "pessoa nova gravada uma única vez");

        // editar a própria pessoa (mesmo CPF) não acusa duplicidade
        u.Mensagens.Clear();
        var idExistente = (long)existente["id"];
        u.Ok(u.SelecionarRegistro(tela, idExistente), "pessoa existente aparece na lista");
        u.Preencher(tela, "Telefone", "(11) 99999-0000");
        u.Clicar(tela, "Salvar");
        u.Ok(!u.Viu("ERRO", ""), "salvar a própria pessoa não acusa CPF duplicado" + CadastrosApoio.Erros(u));
        u.Ok(Ui.Txt(Db.Scalar("SELECT telefone FROM pessoas WHERE id = @p0", idExistente)) == "(11) 99999-0000", "alteração gravada");

        // inativar cliente com vendas (histórico preservado)
        var comVenda = Convert.ToInt64(Db.Scalar("SELECT cliente_id FROM vendas WHERE cliente_id IS NOT NULL ORDER BY id LIMIT 1"));
        var vendas = CadastrosApoio.Contar("vendas", "cliente_id = @p0", comVenda);
        u.Ok(u.SelecionarRegistro(tela, comVenda), "cliente com vendas aparece na lista");
        u.Mensagens.Clear();
        u.RespostaConfirmacao = true;
        u.Clicar(tela, "Excluir");
        u.Ok(u.Viu("CONFIRMAR", "inativá-lo"), "excluir pessoa com histórico oferece inativar");
        u.Ok(CadastrosApoio.Contar("pessoas", "id = @p0 AND ativo = 0", comVenda) == 1 &&
             CadastrosApoio.Contar("vendas", "cliente_id = @p0", comVenda) == vendas, "pessoa inativada e vendas preservadas");
        var filtro = Usuario.Todos(tela).OfType<ComboBox>().First(c => c.Items.Contains("Inativos"));
        filtro.SelectedItem = "Inativos";
        Usuario.Esperar();
        u.Ok(u.SelecionarRegistro(tela, comVenda), "filtro Inativos mostra a pessoa inativada");
        tela.Close();
    }
}
