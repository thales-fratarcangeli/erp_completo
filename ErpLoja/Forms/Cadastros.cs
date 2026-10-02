namespace ErpLoja.Forms;

/// <summary>Definição dos cadastros sobre o CrudForm.</summary>
public static class Cadastros
{
    const string SimNao = "CASE ativo WHEN 1 THEN 'Sim' ELSE 'Não' END";

    static string? ValidarDocumento(string d) =>
        Fiscal.DocumentoValido(d) ? null : "CPF/CNPJ inválido (confira os dígitos).";

    static string? ValidarEmail(string e) => Email.Valido(e) ? null : "Informe um e-mail válido.";

    static bool Marcado(IReadOnlyDictionary<string, object?> r, string coluna) => r.TryGetValue(coluna, out var v) && v is long l && l == 1;

    /// <summary>Clientes, fornecedores, transportadores e motoristas no mesmo cadastro (uma pessoa pode ter vários papéis).</summary>
    public static Form Pessoas() => new CrudForm("Clientes e fornecedores", "pessoas", $"""
        SELECT id, nome AS "Nome", cpf_cnpj AS "CPF/CNPJ",
               TRIM(CASE WHEN eh_cliente = 1 THEN 'Cliente ' ELSE '' END ||
                    CASE WHEN eh_fornecedor = 1 THEN 'Fornecedor ' ELSE '' END ||
                    CASE WHEN eh_transportador = 1 THEN 'Transportador ' ELSE '' END ||
                    CASE WHEN eh_motorista = 1 THEN 'Motorista' ELSE '' END) AS "Papéis",
               telefone AS "Telefone", email AS "E-mail", cidade || COALESCE('/' || uf, '') AS "Cidade", {SimNao} AS "Ativo"
        FROM pessoas WHERE {"{filtro}"} ORDER BY nome
        """,
        new Campo("nome", "Nome / razão social", Obrigatorio: true),
        new Campo("fantasia", "Nome fantasia"),
        new Campo("eh_cliente", "Cliente", TipoCampo.Booleano, Padrao: true),
        new Campo("eh_fornecedor", "Fornecedor", TipoCampo.Booleano),
        new Campo("eh_transportador", "Transportador", TipoCampo.Booleano),
        new Campo("eh_motorista", "Motorista", TipoCampo.Booleano),
        new Campo("cpf_cnpj", "CPF/CNPJ", Validacao: ValidarDocumento),
        new Campo("ie", "Inscrição estadual"),
        new Campo("telefone", "Telefone"),
        new Campo("email", "E-mail", Validacao: ValidarEmail),
        new Campo("contato", "Pessoa de contato"),
        new Campo("endereco", "Endereço"),
        new Campo("cidade", "Cidade"),
        new Campo("uf", "UF", TipoCampo.Opcoes, Opcoes: ["", .. Fiscal.Ufs]),
        new Campo("cep", "CEP"),
        new Campo("limite_credito", "Limite de crédito a prazo (R$, 0 = sem limite)", TipoCampo.Decimal),
        new Campo("rntrc", "RNTRC (transportador)"),
        new Campo("cnh", "CNH (motorista)"),
        new Campo("cnh_categoria", "Categoria da CNH", TipoCampo.Opcoes, Opcoes: ["", "A", "B", "C", "D", "E", "AB", "AC", "AD", "AE"]),
        new Campo("cnh_validade", "Validade da CNH (dd/mm/aaaa)"),
        new Campo("observacoes", "Observações", TipoCampo.MultiLinha),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true))
    {
        Filtros =
        [
            ("Todos ativos", "ativo = 1"), ("Clientes", "eh_cliente = 1 AND ativo = 1"), ("Fornecedores", "eh_fornecedor = 1 AND ativo = 1"),
            ("Transportadores", "eh_transportador = 1 AND ativo = 1"), ("Motoristas", "eh_motorista = 1 AND ativo = 1"), ("Inativos", "ativo = 0"),
        ],
        ValidarRegistro = r =>
        {
            if (!new[] { "eh_cliente", "eh_fornecedor", "eh_transportador", "eh_motorista" }.Any(c => Marcado(r, c)))
                return "Marque pelo menos um papel: cliente, fornecedor, transportador ou motorista.";
            if (Marcado(r, "eh_motorista") && (Fiscal.SoDigitos(r["cpf_cnpj"] as string).Length != 11 || r["cnh"] is null))
                return "Motorista precisa de CPF e CNH.";
            if (r["cnh_validade"] is string v && !DateTime.TryParseExact(v, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                return "Validade da CNH no formato dd/mm/aaaa.";
            return null;
        },
        Dica = "Uma mesma pessoa pode ser cliente e fornecedor ao mesmo tempo: marque os dois papéis em vez de cadastrar duas vezes.",
    };

    public static Form Produtos() => new CrudForm("Produtos", "produtos", $"""
        SELECT p.id, p.codigo AS "Código", p.codigo_barras AS "Cód. barras", p.nome AS "Produto",
               CASE p.tipo WHEN 'PRODUTO_ACABADO' THEN 'Produzido' WHEN 'INSUMO' THEN 'Insumo' ELSE 'Mercadoria' END AS "Tipo",
               c.nome AS "Categoria", p.unidade AS "Un.", p.preco_custo AS "Custo", p.preco_venda AS "Preço venda",
               CASE WHEN p.preco_custo > 0 THEN ROUND((p.preco_venda - p.preco_custo) * 100.0 / p.preco_custo, 1) END AS "Margem %",
               p.estoque AS "Estoque", p.estoque_minimo AS "Mínimo", p.ncm AS "NCM", CASE p.ativo WHEN 1 THEN 'Sim' ELSE 'Não' END AS "Ativo"
        FROM produtos p LEFT JOIN categorias c ON c.id = p.categoria_id
        WHERE {"{filtro}"}
        ORDER BY p.nome
        """,
        new Campo("codigo", "Código", Obrigatorio: true),
        new Campo("codigo_barras", "Código de barras (EAN)"),
        new Campo("nome", "Descrição", Obrigatorio: true),
        new Campo("tipo", "Tipo", TipoCampo.Opcoes, Obrigatorio: true, Opcoes: ["MERCADORIA", "PRODUTO_ACABADO", "INSUMO"], Padrao: "MERCADORIA"),
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
        new Campo("aliq_cofins", "Alíquota COFINS %", TipoCampo.Decimal),
        new Campo("cst_ibscbs", "Reforma: CST IBS/CBS", Padrao: Parametros.CstIbsCbsPadrao),
        new Campo("cclass_trib", "Reforma: classificação tributária (cClassTrib)", Padrao: Parametros.CClassTribPadrao),
        new Campo("red_ibs_pct", "Reforma: redução de alíquota IBS %", TipoCampo.Decimal),
        new Campo("red_cbs_pct", "Reforma: redução de alíquota CBS %", TipoCampo.Decimal),
        new Campo("aliq_is", "Reforma: Imposto Seletivo %", TipoCampo.Decimal))
    {
        Filtros =
        [
            ("Ativos", "p.ativo = 1"), ("Mercadorias", "p.tipo = 'MERCADORIA' AND p.ativo = 1"),
            ("Produzidos", "p.tipo = 'PRODUTO_ACABADO' AND p.ativo = 1"), ("Insumos", "p.tipo = 'INSUMO' AND p.ativo = 1"), ("Inativos", "p.ativo = 0"),
        ],
        Dica = "O saldo de estoque é alterado por compras, vendas, produção e ajustes (tela Estoque). " +
               "O custo é recalculado pelo custo médio a cada entrada. Produto acabado precisa de ficha técnica para ser produzido.",
    };

    public static Form Categorias() => new CrudForm("Categorias", "categorias", """
        SELECT c.id, c.nome AS "Categoria", (SELECT COUNT(*) FROM produtos p WHERE p.categoria_id = c.id) AS "Produtos"
        FROM categorias c ORDER BY c.nome
        """,
        new Campo("nome", "Nome", Obrigatorio: true));

    public static Form Veiculos() => new CrudForm("Veículos", "veiculos", $"""
        SELECT v.id, v.placa AS "Placa", v.tipo AS "Tipo", v.marca_modelo AS "Modelo", v.ano AS "Ano",
               v.capacidade_kg AS "Capacidade (kg)", CASE v.frota_propria WHEN 1 THEN 'Própria' ELSE 'Terceiro' END AS "Frota",
               p.nome AS "Proprietário", CASE v.ativo WHEN 1 THEN 'Sim' ELSE 'Não' END AS "Ativo"
        FROM veiculos v LEFT JOIN pessoas p ON p.id = v.proprietario_id
        ORDER BY v.placa
        """,
        new Campo("placa", "Placa (AAA0A00)", Obrigatorio: true,
            Validacao: p => System.Text.RegularExpressions.Regex.IsMatch(p.ToUpperInvariant().Replace("-", ""), "^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")
                ? null : "Placa inválida. Use o padrão Mercosul (ABC1D23) ou o antigo (ABC1234)."),
        new Campo("renavam", "RENAVAM"),
        new Campo("tipo", "Tipo", TipoCampo.Opcoes, Obrigatorio: true, Opcoes: ["CAMINHAO", "CARRETA", "TRUCK", "TOCO", "VAN", "UTILITARIO", "CARRO", "MOTO"], Padrao: "CAMINHAO"),
        new Campo("carroceria", "Carroceria", TipoCampo.Opcoes, Opcoes: ["", "BAU", "SIDER", "GRANELEIRA", "CACAMBA", "TANQUE", "FRIGORIFICA", "PRANCHA", "ABERTA"]),
        new Campo("marca_modelo", "Marca / modelo"),
        new Campo("ano", "Ano", TipoCampo.Inteiro),
        new Campo("uf", "UF de licenciamento", TipoCampo.Opcoes, Opcoes: ["", .. Fiscal.Ufs]),
        new Campo("capacidade_kg", "Capacidade (kg)", TipoCampo.Decimal),
        new Campo("capacidade_m3", "Capacidade (m³)", TipoCampo.Decimal),
        new Campo("tara_kg", "Tara (kg)", TipoCampo.Decimal),
        new Campo("frota_propria", "Frota própria", TipoCampo.Booleano, Padrao: true),
        new Campo("proprietario_id", "Proprietário (veículo de terceiro)", TipoCampo.Lookup,
            Lookup: "SELECT id, nome FROM pessoas WHERE eh_transportador = 1 ORDER BY nome"),
        new Campo("rntrc", "RNTRC do veículo"),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true))
    {
        ValidarRegistro = r => Marcado(r, "frota_propria") || r["proprietario_id"] is long
            ? null : "Veículo de terceiro precisa do proprietário (cadastre-o como transportador).",
    };

    public static Form ContasBancarias() => new CrudForm("Contas bancárias (cobrança)", "contas_bancarias", """
        SELECT id, nome AS "Conta", banco_codigo AS "Banco", agencia AS "Agência", conta AS "Conta corrente", carteira AS "Carteira",
               nosso_numero_atual AS "Último nosso nº", remessa_sequencia AS "Última remessa", ambiente AS "Ambiente",
               CASE ativo WHEN 1 THEN 'Sim' ELSE 'Não' END AS "Ativo"
        FROM contas_bancarias ORDER BY nome
        """,
        new Campo("nome", "Nome de identificação", Obrigatorio: true),
        new Campo("banco_codigo", "Código do banco (3 dígitos, ex.: 001, 237, 341, 756)", Obrigatorio: true,
            Validacao: b => Fiscal.SoDigitos(b).Length == 3 ? null : "Código do banco com 3 dígitos."),
        new Campo("banco_nome", "Nome do banco"),
        new Campo("agencia", "Agência", Obrigatorio: true),
        new Campo("agencia_dv", "Dígito da agência"),
        new Campo("conta", "Conta corrente", Obrigatorio: true),
        new Campo("conta_dv", "Dígito da conta"),
        new Campo("carteira", "Carteira"),
        new Campo("convenio", "Convênio / código do beneficiário"),
        new Campo("campo_livre_modelo", "Modelo do campo livre (25 posições)", Obrigatorio: true, Padrao: Cobranca.ModeloCampoLivrePadrao,
            Validacao: Cobranca.ValidarModeloCampoLivre),
        new Campo("nosso_numero_digitos", "Dígitos do nosso número", TipoCampo.Inteiro, Padrao: 11),
        new Campo("nosso_numero_atual", "Último nosso número usado", TipoCampo.Inteiro),
        new Campo("remessa_sequencia", "Última sequência de remessa", TipoCampo.Inteiro),
        new Campo("especie_titulo", "Espécie do título (02 = DM)", Padrao: "02"),
        new Campo("multa_pct", "Multa após vencimento (%)", TipoCampo.Decimal),
        new Campo("juros_mes_pct", "Juros ao mês (%)", TipoCampo.Decimal),
        new Campo("dias_protesto", "Dias para protesto (0 = não protestar)", TipoCampo.Inteiro),
        new Campo("instrucoes", "Instruções ao caixa", TipoCampo.MultiLinha),
        new Campo("ambiente", "Ambiente", TipoCampo.Opcoes, Obrigatorio: true, Opcoes: ["HOMOLOGACAO", "PRODUCAO"], Padrao: "HOMOLOGACAO"),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true))
    {
        ValidarRegistro = Cobranca.ValidarContaBancaria,
        Dica = "O campo livre é a parte do código de barras definida por cada banco. Monte-o com as marcações do manual do seu banco, " +
               "por exemplo {agencia:4}{carteira:2}{nossonumero:11}{conta:7}{zeros:1}. Veja a ajuda (F1) para a lista de marcações.",
    };

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
        new Campo("email", "E-mail (recuperação de senha)", Obrigatorio: true, Validacao: ValidarEmail),
        new Campo("admin", "Administrador (acessa usuários, configurações e cancelamentos)", TipoCampo.Booleano),
        new Campo("ativo", "Ativo", TipoCampo.Booleano, Padrao: true))
    {
        ValidarExclusao = id => id == Sessao.UsuarioId ? "Você não pode excluir o próprio usuário." : null,
    };
}
