using System.Globalization;

namespace ErpLoja.Data;

/// <summary>Definição de um parâmetro configurável (tabela configuracoes).</summary>
public record Parametro(string Chave, string Padrao, string Grupo, string Rotulo, string[]? Opcoes = null, string? Dica = null);

/// <summary>
/// Parâmetros do sistema, editados na tela Configurações. Todos ficam na tabela configuracoes (chave/valor).
/// A lista Definicoes é a fonte única: a migração 004 grava os padrões e a tela de configurações monta os campos a partir dela.
/// </summary>
public static class Parametros
{
    public const string Empresa = "Empresa", Fiscal = "Fiscal", Comercial = "Vendas e compras", Email = "E-mail", Banco = "Banco de dados";
    public const string Permitir = "PERMITIR", Perguntar = "PERGUNTAR", Bloquear = "BLOQUEAR";

    public static readonly Parametro[] Definicoes =
    [
        new("nome_loja", "Minha Loja", Empresa, "Nome / razão social"),
        new("cnpj", "", Empresa, "CNPJ", Dica: "Necessário para a chave de acesso das notas."),
        new("ie", "", Empresa, "Inscrição estadual"),
        new("endereco", "", Empresa, "Endereço"),
        new("cidade", "", Empresa, "Cidade"),
        new("uf", "", Empresa, "UF", ["", .. Data.Fiscal.Ufs], "Define CFOP dentro/fora do estado e a chave de acesso."),
        new("telefone", "", Empresa, "Telefone"),
        new("email_loja", "", Empresa, "E-mail da loja"),

        new("regime", "1 - Simples Nacional", Fiscal, "Regime tributário",
            ["1 - Simples Nacional", "2 - Simples Nacional (excesso de sublimite)", "3 - Regime normal"]),
        new("serie_55", "1", Fiscal, "Série da NF-e"),
        new("proximo_numero_55", "1", Fiscal, "Próximo número da NF-e"),
        new("serie_65", "1", Fiscal, "Série da NFC-e"),
        new("proximo_numero_65", "1", Fiscal, "Próximo número da NFC-e"),
        new("natureza_venda", "Venda de mercadoria", Fiscal, "Natureza da operação (saída)"),
        new("natureza_compra", "Compra para comercialização", Fiscal, "Natureza da operação (entrada)"),
        new("cst_padrao", "102", Fiscal, "CST/CSOSN padrão de produto novo", Dica: "102 = Simples Nacional sem crédito."),

        new("venda_sem_estoque", Perguntar, Comercial, "Venda sem estoque suficiente", [Perguntar, Permitir, Bloquear]),
        new("desconto_maximo_pct", "10", Comercial, "Desconto máximo sem administrador (%)"),
        new("validade_orcamento_dias", "15", Comercial, "Validade padrão do orçamento (dias)"),
        new("prazo_vencimento_dias", "30", Comercial, "1º vencimento a prazo (dias)"),
        new("max_parcelas", "12", Comercial, "Número máximo de parcelas"),

        new("resend_api_key", "", Email, "Chave da API do Resend", Dica: "Crie em resend.com > API Keys. Usada para enviar o código de recuperação de senha."),
        new("resend_remetente", "", Email, "Remetente (ex.: ERP Loja <nao-responda@sualoja.com.br>)", Dica: "O domínio precisa estar verificado no Resend."),

        new("backup_ao_iniciar", "SIM", Banco, "Backup automático diário ao abrir o sistema", ["SIM", "NAO"]),
        new("backups_manter", "15", Banco, "Quantidade de backups diários mantidos"),
    ];

    /// <summary>Chaves cujo valor precisa ser um número inteiro não negativo.</summary>
    public static readonly string[] Inteiros =
        ["serie_55", "proximo_numero_55", "serie_65", "proximo_numero_65", "validade_orcamento_dias", "prazo_vencimento_dias", "max_parcelas", "backups_manter"];

    public static readonly string[] Decimais = ["desconto_maximo_pct"];

    static string Txt(string chave) =>
        Db.Config(chave) is { Length: > 0 } v ? v : Definicoes.FirstOrDefault(d => d.Chave == chave)?.Padrao ?? "";

    static int Int(string chave) => int.TryParse(Txt(chave), out var v) ? v : int.Parse(Definicoes.First(d => d.Chave == chave).Padrao);

    static decimal Dec(string chave) =>
        decimal.TryParse(Txt(chave), NumberStyles.Number, CultureInfo.CurrentCulture, out var v) ? v : 0;

    public static string VendaSemEstoque => Txt("venda_sem_estoque");
    public static decimal DescontoMaximoPct => Dec("desconto_maximo_pct");
    public static int ValidadeOrcamentoDias => Int("validade_orcamento_dias");
    public static int PrazoVencimentoDias => Int("prazo_vencimento_dias");
    public static int MaxParcelas => Math.Clamp(Int("max_parcelas"), 1, 60);
    public static string CstPadrao => Txt("cst_padrao");
    public static string NaturezaVenda => Txt("natureza_venda");
    public static string NaturezaCompra => Txt("natureza_compra");
    public static string ResendApiKey => Db.Config("resend_api_key");
    public static string ResendRemetente => Db.Config("resend_remetente");
    public static bool BackupAoIniciar => Txt("backup_ao_iniciar") != "NAO";
    public static int BackupsManter => Math.Max(1, Int("backups_manter"));

    /// <summary>Regra de venda sem estoque. Devolve true se pode seguir.</summary>
    public static bool PodeVenderSemEstoque(string mensagem)
    {
        switch (VendaSemEstoque)
        {
            case Permitir:
                return true;
            case Bloquear:
                Ui.Erro(mensagem + "\n\nA venda sem estoque está bloqueada (Configurações > Vendas e compras).");
                return false;
            default:
                return Ui.Confirmar(mensagem + "\n\nDeseja continuar mesmo assim?");
        }
    }

    /// <summary>Regra de desconto máximo: acima do limite só administrador. Devolve a mensagem de erro, ou null.</summary>
    public static string? ValidarDesconto(decimal desconto, decimal valorBruto)
    {
        if (desconto <= 0 || valorBruto <= 0 || Sessao.Admin)
            return null;
        var pct = desconto / valorBruto * 100;
        return pct > DescontoMaximoPct
            ? $"Desconto de {pct:N1}% acima do limite de {DescontoMaximoPct:N1}% para usuários comuns. Peça a um administrador."
            : null;
    }
}
