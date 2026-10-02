using System.Globalization;

namespace ErpLoja.Data;

/// <summary>
/// Definição de um parâmetro configurável (tabela configuracoes).
/// Opcoes = lista fixa; OpcoesDinamicas = lista calculada ao abrir a tela (ex.: impressoras instaladas); Secreto = campo mascarado.
/// </summary>
public record Parametro(string Chave, string Padrao, string Grupo, string Rotulo, string[]? Opcoes = null, string? Dica = null,
    bool Secreto = false, Func<string[]>? OpcoesDinamicas = null);

/// <summary>
/// Parâmetros do sistema, editados na tela Configurações. Todos ficam na tabela configuracoes (chave/valor).
/// A lista Definicoes é a fonte única: a migração 004 grava os padrões e a tela de configurações monta os campos a partir dela.
/// </summary>
public static class Parametros
{
    public const string Empresa = "Empresa", Fiscal = "Fiscal", Reforma = "Reforma tributária", Comercial = "Vendas e compras",
        Producao = "Produção", Transporte = "Transporte e CIOT", Tef = "TEF", Impressoras = "Impressoras", Email = "E-mail", Banco = "Banco de dados";
    public const string Sim = "SIM", Nao = "NAO";
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

        new("reforma_ativa", Sim, Reforma, "Calcular IBS/CBS nas notas", [Sim, Nao],
            "Reforma tributária (EC 132/2023, LC 214/2025). Em 2026 os valores são destacados em caráter de teste e não somam ao total da nota."),
        new("aliq_cbs", "0,90", Reforma, "Alíquota CBS (%)", Dica: "Padrão 2026 (teste): 0,90%. Confirme com seu contador a cada ano da transição."),
        new("aliq_ibs_uf", "0,10", Reforma, "Alíquota IBS estadual (%)", Dica: "Padrão 2026 (teste): 0,10%."),
        new("aliq_ibs_mun", "0,00", Reforma, "Alíquota IBS municipal (%)"),
        new("cst_ibscbs_padrao", "000", Reforma, "CST IBS/CBS padrão de produto novo", Dica: "000 = tributação integral."),
        new("cclass_trib_padrao", "000001", Reforma, "Classificação tributária (cClassTrib) padrão"),

        new("venda_sem_estoque", Perguntar, Comercial, "Venda sem estoque suficiente", [Perguntar, Permitir, Bloquear]),
        new("desconto_maximo_pct", "10", Comercial, "Desconto máximo sem administrador (%)"),
        new("validade_orcamento_dias", "15", Comercial, "Validade padrão do orçamento (dias)"),
        new("prazo_vencimento_dias", "30", Comercial, "1º vencimento a prazo (dias)"),
        new("max_parcelas", "12", Comercial, "Número máximo de parcelas"),
        new("caixa_numero", "01", Comercial, "Número deste caixa (PDV)"),
        new("imprimir_cupom", Perguntar, Comercial, "Imprimir comprovante ao finalizar venda", [Perguntar, "SEMPRE", "NUNCA"]),

        new("producao_sem_estoque", Perguntar, Producao, "Concluir produção sem insumo suficiente", [Perguntar, Permitir, Bloquear]),

        new("rntrc_empresa", "", Transporte, "RNTRC da empresa (frota própria)"),
        new("ciot_administradora", "PAMCARD", Transporte, "Administradora do CIOT", ["PAMCARD", "SIMULADOR", "EFRETE", "REPOM", "TARGET"],
            "Empresa habilitada pela ANTT que gera o CIOT e paga o frete ao transportador autônomo."),
        new("ciot_ambiente", "HOMOLOGACAO", Transporte, "Ambiente do CIOT", ["HOMOLOGACAO", "PRODUCAO"]),
        new("pamcard_url", "", Transporte, "Pamcard: endereço do serviço", Dica: "Fornecido pela Pamcard no credenciamento (homologação e produção têm endereços diferentes)."),
        new("pamcard_usuario", "", Transporte, "Pamcard: usuário / código do contratante"),
        new("pamcard_senha", "", Transporte, "Pamcard: senha / token", Secreto: true),
        new("pamcard_cnpj_contratante", "", Transporte, "Pamcard: CNPJ do contratante"),

        new("tef_ativo", Sim, Tef, "Usar TEF no PDV para cartões e PIX", [Sim, Nao]),
        new("tef_provedor", "SIMULADOR", Tef, "Provedor de TEF", ["SIMULADOR", "SITEF", "PAYGO"],
            "SIMULADOR permite testar o PDV sem maquininha. Os demais exigem o cliente TEF do provedor instalado."),
        new("tef_imprimir_via_loja", Nao, Tef, "Imprimir também a via do estabelecimento", [Sim, Nao]),

        new("impressora_cupom", "", Impressoras, "Impressora de cupom", OpcoesDinamicas: Impressao.ImpressorasInstaladas,
            Dica: "Em branco = impressora padrão do Windows."),
        new("impressora_cupom_tipo", "WINDOWS", Impressoras, "Tipo da impressora de cupom", ["WINDOWS", "ESCPOS"],
            "ESCPOS = térmica não fiscal (Elgin, Bematech, Epson, Daruma...) recebendo comandos diretos: corta papel e abre gaveta."),
        new("impressora_colunas", "48", Impressoras, "Colunas do cupom", ["48", "42", "32"], "80 mm costuma ter 48 colunas; 58 mm, 32."),
        new("impressora_codepage", "850", Impressoras, "Página de código (acentos)", ["850", "860"]),
        new("impressora_cortar", Sim, Impressoras, "Cortar o papel ao final", [Sim, Nao]),
        new("impressora_gaveta", Nao, Impressoras, "Abrir a gaveta ao imprimir venda em dinheiro", [Sim, Nao]),
        new("impressora_documentos", "", Impressoras, "Impressora de documentos A4 (DANFE, boletos, pedidos)",
            OpcoesDinamicas: Impressao.ImpressorasInstaladas, Dica: "Em branco = impressora padrão do Windows."),

        new("resend_api_key", "", Email, "Chave da API do Resend", Dica: "Crie em resend.com > API Keys. Usada para enviar o código de recuperação de senha.", Secreto: true),
        new("resend_remetente", "", Email, "Remetente (ex.: ERP Loja <nao-responda@sualoja.com.br>)", Dica: "O domínio precisa estar verificado no Resend."),

        new("backup_ao_iniciar", "SIM", Banco, "Backup automático diário ao abrir o sistema", ["SIM", "NAO"]),
        new("backups_manter", "15", Banco, "Quantidade de backups diários mantidos"),
        new("log_dias_manter", "365", Banco, "Dias de log de atividades mantidos", Dica: "O log fica em um banco separado (ex.: loja_log.db). 0 = manter tudo."),
    ];

    /// <summary>Chaves cujo valor precisa ser um número inteiro não negativo.</summary>
    public static readonly string[] Inteiros =
        ["serie_55", "proximo_numero_55", "serie_65", "proximo_numero_65", "validade_orcamento_dias", "prazo_vencimento_dias", "max_parcelas", "backups_manter"];

    /// <summary>Chaves com valor percentual (0 a 100).</summary>
    public static readonly string[] Decimais = ["desconto_maximo_pct", "aliq_cbs", "aliq_ibs_uf", "aliq_ibs_mun"];

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
    public static bool ReformaAtiva => Txt("reforma_ativa") == Sim;
    public static decimal AliqCbs => Dec("aliq_cbs");
    public static decimal AliqIbsUf => Dec("aliq_ibs_uf");
    public static decimal AliqIbsMun => Dec("aliq_ibs_mun");
    public static string CstIbsCbsPadrao => Txt("cst_ibscbs_padrao");
    public static string CClassTribPadrao => Txt("cclass_trib_padrao");
    public static string ProducaoSemEstoque => Txt("producao_sem_estoque");
    public static string CaixaNumero => Txt("caixa_numero");
    public static string ImprimirCupom => Txt("imprimir_cupom");
    public static string CiotAdministradora => Txt("ciot_administradora");
    public static bool CiotProducao => Txt("ciot_ambiente") == "PRODUCAO";
    public static bool TefAtivo => Txt("tef_ativo") == Sim;
    public static string TefProvedor => Txt("tef_provedor");
    public static bool TefViaLoja => Txt("tef_imprimir_via_loja") == Sim;
    public static string ImpressoraCupom => Db.Config("impressora_cupom");
    public static bool ImpressoraEscPos => Txt("impressora_cupom_tipo") == "ESCPOS";
    public static int ImpressoraColunas => Int("impressora_colunas");
    public static int ImpressoraCodepage => Int("impressora_codepage");
    public static bool ImpressoraCortar => Txt("impressora_cortar") == Sim;
    public static bool ImpressoraGaveta => Txt("impressora_gaveta") == Sim;
    public static string ImpressoraDocumentos => Db.Config("impressora_documentos");
    public static string ResendApiKey => Db.Config("resend_api_key");
    public static string ResendRemetente => Db.Config("resend_remetente");
    public static bool BackupAoIniciar => Txt("backup_ao_iniciar") != Nao;
    public static int BackupsManter => Math.Max(1, Int("backups_manter"));
    public static int LogDiasManter => Math.Max(0, Int("log_dias_manter"));

    /// <summary>Regra de venda sem estoque. Devolve true se pode seguir.</summary>
    public static bool PodeVenderSemEstoque(string mensagem) => AplicarRegraEstoque(VendaSemEstoque, mensagem, "A venda sem estoque");

    /// <summary>Regra de produção com insumo insuficiente. Devolve true se pode seguir.</summary>
    public static bool PodeProduzirSemEstoque(string mensagem) => AplicarRegraEstoque(ProducaoSemEstoque, mensagem, "A produção sem insumo");

    static bool AplicarRegraEstoque(string regra, string mensagem, string oQue)
    {
        switch (regra)
        {
            case Permitir:
                return true;
            case Bloquear:
                Ui.Erro(mensagem + $"\n\n{oQue} está bloqueada em Configurações.");
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
