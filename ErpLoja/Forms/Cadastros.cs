namespace ErpLoja.Forms;

/// <summary>Definição dos cadastros básicos sobre o CrudForm.</summary>
public static class Cadastros
{
    const string SimNao = "CASE ativo WHEN 1 THEN 'Sim' ELSE 'Não' END";

    public static Form Clientes() => new CrudForm("Clientes", "clientes", $"""
        SELECT id, nome AS "Nome", cpf_cnpj AS "CPF/CNPJ", telefone AS "Telefone", email AS "E-mail",
               cidade AS "Cidade", {SimNao} AS "Ativo"
        FROM clientes ORDER BY nome
        """,
        new Campo("nome", "Nome", Obrigatorio: true),
        new Campo("cpf_cnpj", "CPF/CNPJ"),
        new Campo("telefone", "Telefone"),
        new Campo("email", "E-mail"),
        new Campo("endereco", "Endereço"),
        new Campo("cidade", "Cidade"),
        new Campo("uf", "UF"),
        new Campo("cep", "CEP"),
        new Campo("ie", "Inscrição estadual"),
        new Campo("observacoes", "Observações", TipoCampo.MultiLinha),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true));

    public static Form Fornecedores() => new CrudForm("Fornecedores", "fornecedores", $"""
        SELECT id, nome AS "Nome", cnpj AS "CNPJ", telefone AS "Telefone", email AS "E-mail",
               contato AS "Contato", cidade AS "Cidade", {SimNao} AS "Ativo"
        FROM fornecedores ORDER BY nome
        """,
        new Campo("nome", "Razão social / nome", Obrigatorio: true),
        new Campo("cnpj", "CNPJ"),
        new Campo("telefone", "Telefone"),
        new Campo("email", "E-mail"),
        new Campo("contato", "Pessoa de contato"),
        new Campo("endereco", "Endereço"),
        new Campo("cidade", "Cidade"),
        new Campo("uf", "UF"),
        new Campo("cep", "CEP"),
        new Campo("ie", "Inscrição estadual"),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true));

    public static Form Produtos() => new CrudForm("Produtos", "produtos", $"""
        SELECT p.id, p.codigo AS "Código", p.codigo_barras AS "Cód. barras", p.nome AS "Produto",
               c.nome AS "Categoria", p.unidade AS "Un.", p.preco_custo AS "Custo", p.preco_venda AS "Preço venda",
               CASE WHEN p.preco_custo > 0 THEN ROUND((p.preco_venda - p.preco_custo) * 100.0 / p.preco_custo, 1) END AS "Margem %",
               p.estoque AS "Estoque", p.estoque_minimo AS "Mínimo", p.ncm AS "NCM", CASE p.ativo WHEN 1 THEN 'Sim' ELSE 'Não' END AS "Ativo"
        FROM produtos p LEFT JOIN categorias c ON c.id = p.categoria_id
        ORDER BY p.nome
        """,
        new Campo("codigo", "Código", Obrigatorio: true),
        new Campo("codigo_barras", "Código de barras (EAN)"),
        new Campo("nome", "Descrição", Obrigatorio: true),
        new Campo("categoria_id", "Categoria", TipoCampo.Lookup, Lookup: "SELECT id, nome FROM categorias ORDER BY nome"),
        new Campo("unidade", "Unidade (UN, KG, CX...)", Obrigatorio: true, Padrao: "UN"),
        new Campo("preco_custo", "Preço de custo (R$)", TipoCampo.Decimal),
        new Campo("preco_venda", "Preço de venda (R$)", TipoCampo.Decimal),
        new Campo("estoque_minimo", "Estoque mínimo", TipoCampo.Decimal),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true),
        new Campo("ncm", "NCM (8 dígitos) — fiscal"),
        new Campo("cest", "CEST"),
        new Campo("origem", "Origem (0 = nacional, 1/2 = estrangeira)", TipoCampo.Inteiro),
        new Campo("cst", "CST / CSOSN (ex.: 102 no Simples)", Obrigatorio: true, Padrao: Parametros.CstPadrao),
        new Campo("aliq_icms", "Alíquota ICMS %", TipoCampo.Decimal),
        new Campo("aliq_ipi", "Alíquota IPI %", TipoCampo.Decimal),
        new Campo("aliq_pis", "Alíquota PIS %", TipoCampo.Decimal),
        new Campo("aliq_cofins", "Alíquota COFINS %", TipoCampo.Decimal))
    {
        Dica = "O saldo de estoque é alterado por compras, vendas e ajustes (tela Estoque). " +
               "O custo é recalculado pelo custo médio a cada compra.",
    };

    public static Form Categorias() => new CrudForm("Categorias", "categorias", """
        SELECT c.id, c.nome AS "Categoria", (SELECT COUNT(*) FROM produtos p WHERE p.categoria_id = c.id) AS "Produtos"
        FROM categorias c ORDER BY c.nome
        """,
        new Campo("nome", "Nome", Obrigatorio: true));

    public static Form Usuarios() => new CrudForm("Usuários", "usuarios", $"""
        SELECT id, nome AS "Nome", username AS "Username", email AS "E-mail", CASE admin WHEN 1 THEN 'Sim' ELSE 'Não' END AS "Administrador",
               {SimNao} AS "Ativo"
        FROM usuarios ORDER BY nome
        """,
        new Campo("nome", "Nome", Obrigatorio: true),
        new Campo("username", "Username", Obrigatorio: true,
            Validacao: u => System.Text.RegularExpressions.Regex.IsMatch(u, @"^[A-Za-z0-9._-]{3,30}$")
                ? null : "Username deve ter de 3 a 30 caracteres: letras, números, ponto, hífen ou sublinhado (sem espaços)."),
        new Campo("senha", "Senha", TipoCampo.Senha, Obrigatorio: true),
        new Campo("email", "E-mail (recuperação de senha)", Obrigatorio: true,
            Validacao: e => Email.Valido(e) ? null : "Informe um e-mail válido."),
        new Campo("admin", "Administrador (acessa usuários e cancelamentos)", TipoCampo.Booleano),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true))
    {
        ValidarExclusao = id => id == Sessao.UsuarioId ? "Você não pode excluir o próprio usuário." : null,
    };
}
