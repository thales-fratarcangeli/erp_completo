namespace ErpLoja.Data;

/// <summary>
/// Regras dos cadastros que dependem do banco (duplicidade, administrador ativo). Cada método devolve a mensagem
/// para o usuário (o que aconteceu e o que fazer) ou null quando está tudo certo.
/// </summary>
public static class RegrasCadastro
{
    /// <summary>GTIN/EAN (8, 12, 13 ou 14 dígitos) com dígito verificador módulo 10 correto.</summary>
    public static bool GtinValido(string? codigo)
    {
        var d = codigo?.Trim() ?? "";
        if (d.Length is not (8 or 12 or 13 or 14) || !d.All(char.IsDigit))
            return false;
        var soma = 0;
        for (int i = d.Length - 2, peso = 3; i >= 0; i--, peso = peso == 3 ? 1 : 3)
            soma += (d[i] - '0') * peso;
        return (10 - soma % 10) % 10 == d[^1] - '0';
    }

    /// <summary>RN-CAD-10: a mesma pessoa (mesmo CPF/CNPJ) não é cadastrada duas vezes.</summary>
    public static string? PessoaDuplicada(string? documento, long? id)
    {
        var digitos = Fiscal.SoDigitos(documento);
        if (digitos.Length == 0)
            return null;
        foreach (DataRow r in Db.Query("SELECT id, nome, cpf_cnpj, ativo FROM pessoas WHERE cpf_cnpj IS NOT NULL AND id <> @p0", id ?? 0).Rows)
            if (Fiscal.SoDigitos(Ui.Txt(r["cpf_cnpj"])) == digitos)
                return $"Já existe um cadastro com este CPF/CNPJ: \"{r["nome"]}\" (registro nº {r["id"]}" +
                       (Ui.Dec(r["ativo"]) == 1 ? ")" : ", inativo)") +
                       ".\nEm vez de cadastrar de novo, edite esse registro e marque os papéis que faltam (cliente, fornecedor...).";
        return null;
    }

    /// <summary>RN-CAD-11: código de barras do produto: GTIN válido e não usado por outro produto (nem como código interno).</summary>
    public static string? CodigoBarrasProduto(string? codigoBarras, long? id)
    {
        var cb = codigoBarras?.Trim() ?? "";
        if (cb.Length == 0)
            return null;
        if (!GtinValido(cb))
            return "Código de barras inválido: use um EAN/GTIN de 8, 12, 13 ou 14 dígitos (confira o último dígito). " +
                   "Se o produto não tem código de barras, deixe o campo em branco.";
        if (Db.Query("SELECT codigo, nome FROM produtos WHERE (codigo_barras = @p0 OR codigo = @p0 COLLATE NOCASE) AND id <> @p1 LIMIT 1", cb, id ?? 0)
                .Rows.Cast<DataRow>().FirstOrDefault() is { } outro)
            return $"O código de barras {cb} já pertence ao produto \"{outro["nome"]}\" (código {outro["codigo"]}). " +
                   "No PDV a leitura ficaria ambígua: confira o código lido.";
        return null;
    }

    /// <summary>NCM opcional no cadastro, mas se informado deve ter 8 dígitos (a nota exige).</summary>
    public static string? Ncm(string? ncm) =>
        string.IsNullOrWhiteSpace(ncm) || System.Text.RegularExpressions.Regex.IsMatch(ncm.Trim(), @"^\d{4}\.?\d{2}\.?\d{2}$")
            ? null : "NCM deve ter 8 dígitos (ex.: 22021000). Deixe em branco se ainda não souber.";

    /// <summary>CST (2 dígitos, regime normal) ou CSOSN (3 dígitos, Simples Nacional).</summary>
    public static string? Cst(string? cst) =>
        System.Text.RegularExpressions.Regex.IsMatch(cst?.Trim() ?? "", @"^\d{2,3}$")
            ? null : "CST/CSOSN deve ter 2 dígitos (regime normal, ex.: 00) ou 3 dígitos (Simples Nacional, ex.: 102).";

    /// <summary>Senha nova: mesmo mínimo da recuperação de senha (RN-USU-06).</summary>
    public static string? Senha(string senha) =>
        senha.Length >= 4 ? null : "A senha deve ter pelo menos 4 caracteres.";

    /// <summary>
    /// RN-USU-07: o sistema nunca fica sem administrador ativo; ninguém inativa o próprio usuário nem tira o próprio
    /// acesso de administrador (outro administrador deve fazê-lo).
    /// </summary>
    public static string? AlteracaoUsuario(long? id, bool admin, bool ativo, long? usuarioLogado)
    {
        if (id is null)
            return null;
        if (id == usuarioLogado && !ativo)
            return "Você não pode inativar o próprio usuário (ficaria sem acesso). Peça a outro administrador.";
        if (id == usuarioLogado && !admin)
            return "Você não pode tirar o seu próprio acesso de administrador. Peça a outro administrador.";
        if ((!admin || !ativo) && !OutroAdminAtivo(id.Value))
            return "Este é o único administrador ativo: o sistema não pode ficar sem administrador.\n" +
                   "Marque outro usuário como administrador antes de inativar este ou tirar o acesso dele.";
        return null;
    }

    /// <summary>Excluir (ou inativar) um usuário não pode deixar o sistema sem administrador ativo.</summary>
    public static string? ExclusaoUsuario(long id, long? usuarioLogado)
    {
        if (id == usuarioLogado)
            return "Você não pode excluir o próprio usuário.";
        var alvoEhAdminAtivo = Db.Scalar("SELECT 1 FROM usuarios WHERE id = @p0 AND admin = 1 AND ativo = 1", id) != null;
        return alvoEhAdminAtivo && !OutroAdminAtivo(id)
            ? "Este é o único administrador ativo e não pode ser excluído. Marque outro usuário como administrador antes."
            : null;
    }

    static bool OutroAdminAtivo(long id) =>
        Db.Scalar("SELECT 1 FROM usuarios WHERE admin = 1 AND ativo = 1 AND id <> @p0 LIMIT 1", id) != null;
}
