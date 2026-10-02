using System.Globalization;
using System.Text.RegularExpressions;

namespace ErpLoja.Data;

/// <summary>
/// Formato de um campo de entrada: quais caracteres podem ser digitados, tamanho máximo, conferência do valor e
/// máscara aplicada ao sair do campo. É a fonte única de validação de entrada: a interface usa para filtrar a
/// digitação e marcar erros (UI/Validacao) e a camada de dados usa para conferir de novo antes de gravar.
/// </summary>
/// <param name="Dica">O que o campo aceita (mostrado quando a pessoa digita algo fora do formato).</param>
/// <param name="Permitido">Caracteres aceitos na digitação (null = qualquer caractere visível).</param>
/// <param name="Tamanho">Máximo de caracteres digitáveis (0 = sem limite).</param>
/// <param name="Conferir">Confere o valor completo (já sem espaços nas pontas); devolve o erro ou null.</param>
/// <param name="Mascarar">Forma padrão de gravar/mostrar o valor válido (ex.: CNPJ com pontuação).</param>
public sealed record Formato(string Nome, string Dica, Func<char, bool>? Permitido, int Tamanho, Func<string, string?>? Conferir = null,
    Func<string, string>? Mascarar = null, bool Maiusculas = false, bool MultiLinha = false)
{
    /// <summary>Confere o valor. Vazio é válido aqui (obrigatoriedade é verificada por quem usa o campo).</summary>
    public string? Validar(string? valor)
    {
        var v = (valor ?? "").Trim();
        if (v.Length == 0)
            return null;
        if (v.Any(c => char.IsControl(c) && !(MultiLinha && c is '\r' or '\n' or '\t')))
            return "contém caracteres de controle (invisíveis). Digite o valor de novo.";
        if (Permitido != null && v.Any(c => !Permitido(c)))
            return Dica;
        if (Tamanho > 0 && v.Length > Tamanho)
            return $"aceita no máximo {Tamanho} caracteres.";
        return Conferir?.Invoke(Maiusculas ? v.ToUpperInvariant() : v);
    }

    /// <summary>Valor no formato de gravação (máscara, maiúsculas). Só aplicar a valores válidos.</summary>
    public string Normalizar(string? valor)
    {
        var v = (valor ?? "").Trim();
        if (Maiusculas)
            v = v.ToUpperInvariant();
        return v.Length > 0 && Mascarar != null ? Mascarar(v) : v;
    }
}

/// <summary>Formatos de todos os campos do sistema.</summary>
public static class Formatos
{
    static bool Digito(char c) => c is >= '0' and <= '9';
    static bool Letra(char c) => char.IsLetter(c);
    static string SoDigitos(string v) => new(v.Where(Digito).ToArray());

    static Func<char, bool> Digitos(string extras = "") => c => Digito(c) || extras.Contains(c);

    // ------------------------------------------------------------------ texto livre
    /// <summary>Texto livre de até N caracteres (sem caracteres de controle).</summary>
    public static Formato Texto(int maximo) => new($"Texto (até {maximo})", $"aceita texto de até {maximo} caracteres.", c => !char.IsControl(c), maximo);

    /// <summary>Texto de várias linhas (observações).</summary>
    public static Formato TextoLongo(int maximo) => new($"Texto longo (até {maximo})", $"aceita texto de até {maximo} caracteres.", c => !char.IsControl(c) || c is '\r' or '\n' or '\t', maximo, MultiLinha: true);

    public static readonly Formato Nome = Texto(120);
    public static readonly Formato Busca = Texto(100);
    public static readonly Formato Observacao = TextoLongo(2000);

    /// <summary>Senha: qualquer caractere visível, até 128 (regras de força em RegrasCadastro.Senha).</summary>
    public static readonly Formato Senha = new("Senha", "aceita até 128 caracteres visíveis.", c => !char.IsControl(c), 128);

    // ------------------------------------------------------------------ números
    /// <summary>Número inteiro digitado em texto (só algarismos).</summary>
    public static Formato Inteiro(int minimo = 0, int maximo = int.MaxValue) => new("Inteiro", "aceita só números inteiros (sem letras, pontos ou vírgulas).", Digito, 10,
        v => long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= minimo && n <= maximo
            ? null : $"deve ser um número inteiro de {minimo} a {(maximo == int.MaxValue ? "999999999" : maximo)}.");

    /// <summary>Número decimal digitado em texto (vírgula como separador).</summary>
    public static Formato Decimal(decimal minimo, decimal maximo) => new("Decimal", "aceita só números (use vírgula para os centavos).", Digitos(",."), 18,
        v => decimal.TryParse(v, NumberStyles.Number, new CultureInfo("pt-BR"), out var n) && n >= minimo && n <= maximo
            ? null : $"deve ser um número de {minimo.ToString("0.##", new CultureInfo("pt-BR"))} a {maximo.ToString("0.##", new CultureInfo("pt-BR"))} (ex.: 0,90).");

    public static readonly Formato Percentual = Decimal(0, 100);

    /// <summary>Exatamente uma das quantidades de algarismos indicadas (pontos/traços digitados são aceitos e removidos).</summary>
    static Formato DigitosFixos(string nome, string separadores, string descricao, params int[] tamanhos) =>
        new(nome, $"aceita só números ({descricao}).", Digitos(separadores), tamanhos.Max() + 6,
            v => tamanhos.Contains(SoDigitos(v).Length) ? null : $"deve ter {descricao}.", SoDigitos);

    static Formato DigitosEntre(string nome, int minimo, int maximo, string descricao) =>
        new(nome, $"aceita só números ({descricao}).", Digito, maximo,
            v => v.Length >= minimo && v.Length <= maximo ? null : $"deve ter {descricao}.");

    // ------------------------------------------------------------------ documentos e contato
    static string MascaraCpf(string d) => $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    static string MascaraCnpj(string d) => $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";

    public static readonly Formato Cpf = new("CPF", "aceita só números (11 dígitos do CPF).", Digitos(".-"), 14,
        v => SoDigitos(v).Length == 11 && Fiscal.DocumentoValido(v) ? null : "não é um CPF válido (11 dígitos com dígitos verificadores corretos).",
        v => MascaraCpf(SoDigitos(v)));

    public static readonly Formato Cnpj = new("CNPJ", "aceita só números (14 dígitos do CNPJ).", Digitos("./-"), 18,
        v => SoDigitos(v).Length == 14 && Fiscal.DocumentoValido(v) ? null : "não é um CNPJ válido (14 dígitos com dígitos verificadores corretos).",
        v => MascaraCnpj(SoDigitos(v)));

    public static readonly Formato CpfCnpj = new("CPF/CNPJ", "aceita só números (CPF com 11 ou CNPJ com 14 dígitos).", Digitos("./-"), 18,
        v => SoDigitos(v).Length is 11 or 14 && Fiscal.DocumentoValido(v) ? null : "não é um CPF (11 dígitos) nem um CNPJ (14 dígitos) válido.",
        v => SoDigitos(v) is var d && d.Length == 11 ? MascaraCpf(d) : MascaraCnpj(d));

    public static readonly Formato InscricaoEstadual = new("Inscrição estadual", "aceita números ou a palavra ISENTO.", c => Digito(c) || c is '.' or '-' or '/' || "ISENTOisento".Contains(c), 20,
        v => v.Equals("ISENTO", StringComparison.OrdinalIgnoreCase) || (v.All(c => Digito(c) || c is '.' or '-' or '/') && SoDigitos(v).Length is >= 2 and <= 14)
            ? null : "deve ter de 2 a 14 números ou ser ISENTO.", v => v.Equals("isento", StringComparison.OrdinalIgnoreCase) ? "ISENTO" : v, Maiusculas: true);

    public static readonly Formato Cep = new("CEP", "aceita só números (8 dígitos do CEP).", Digitos("-."), 10,
        v => SoDigitos(v).Length == 8 ? null : "deve ter 8 dígitos (ex.: 01310-100).", v => SoDigitos(v) is var d ? $"{d[..5]}-{d[5..]}" : v);

    public static readonly Formato Telefone = new("Telefone", "aceita só números, espaço, parênteses, + e traço.", c => Digito(c) || "()-+ ".Contains(c), 20,
        v => SoDigitos(v).Length is >= 10 and <= 13 ? null : "deve ter DDD + número (10 ou 11 dígitos), ex.: (11) 98765-4321.",
        v => SoDigitos(v) is var d && d.Length == 11 ? $"({d[..2]}) {d[2..7]}-{d[7..]}" : d.Length == 10 ? $"({d[..2]}) {d[2..6]}-{d[6..]}" : v);

    static readonly Regex RegexEmail = new(@"^[^@\s<>()""',;:]+@[^@\s<>()""',;:]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);

    public static bool EmailValido(string? email) => email is { Length: <= 254 } && RegexEmail.IsMatch(email.Trim());

    public static readonly Formato Email = new("E-mail", "não aceita espaços nem caracteres especiais de e-mail inválidos.", c => !char.IsControl(c) && !char.IsWhiteSpace(c) && !"<>()\",;:".Contains(c), 254,
        v => EmailValido(v) ? null : "não é um e-mail válido (ex.: nome@empresa.com.br).", v => v.ToLowerInvariant());

    /// <summary>Remetente de e-mail: "Nome &lt;email&gt;" ou só o e-mail.</summary>
    public static readonly Formato Remetente = new("Remetente", "aceita \"Nome <email>\" ou só o e-mail.", c => !char.IsControl(c), 200,
        v => Regex.Match(v, @"^(?:[^<>]*<([^<>]+)>|([^<>\s]+))$") is { Success: true } m && EmailValido(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            ? null : "deve ser um e-mail ou \"Nome <email>\", ex.: ERP Loja <nao-responda@sualoja.com.br>.");

    public static readonly Formato Uf = new("UF", "aceita só as 2 letras da UF.", Letra, 2,
        v => Fiscal.Ufs.Contains(v.ToUpperInvariant()) ? null : "não é uma UF válida (ex.: SP, RJ, MG).", Maiusculas: true);

    public static readonly Formato Cidade = new("Cidade", "aceita letras, espaços, apóstrofo e hífen.", c => Letra(c) || c is ' ' or '\'' or '-' or '.', 60);

    public static readonly Formato DataBr = new("Data", "aceita só números e barras (dd/mm/aaaa).", Digitos("/"), 10,
        v => DateTime.TryParseExact(v, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? null : "deve ser uma data válida no formato dd/mm/aaaa.");

    public static readonly Formato UrlHttps = new("Endereço HTTPS", "não aceita espaços.", c => !char.IsControl(c) && !char.IsWhiteSpace(c), 300,
        v => Uri.TryCreate(v, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps ? null : "deve ser um endereço https:// completo (conexão segura).");

    // ------------------------------------------------------------------ produtos e fiscal
    public static readonly Formato CodigoProduto = new("Código do produto", "aceita letras, números, ponto, hífen, barra e sublinhado.", c => char.IsLetterOrDigit(c) || "-._/".Contains(c), 30);

    /// <summary>Entrada do PDV: código, código de barras ou "quantidade*código".</summary>
    public static readonly Formato EntradaPdv = new("Código ou quantidade*código", "aceita código, código de barras ou quantidade*código (ex.: 3*7891234567895).",
        c => char.IsLetterOrDigit(c) || "-._/*,xX ".Contains(c), 60);

    /// <summary>Código, código de barras ou parte do nome (para lançar itens e buscar produtos).</summary>
    public static readonly Formato CodigoOuNome = Texto(80);

    public static readonly Formato CodigoBarras = new("Código de barras", "aceita só números (EAN/GTIN de 8, 12, 13 ou 14 dígitos).", Digito, 14,
        v => RegrasCadastro.GtinValido(v) ? null : "não é um código de barras válido (8, 12, 13 ou 14 dígitos com dígito verificador correto).");

    public static readonly Formato Unidade = new("Unidade", "aceita só letras (ex.: UN, KG, CX).", Letra, 6, Maiusculas: true);

    public static readonly Formato Ncm = DigitosFixos("NCM", ".", "8 dígitos", 8);
    public static readonly Formato Cest = DigitosFixos("CEST", ".", "7 dígitos", 7);
    public static readonly Formato Cfop = DigitosFixos("CFOP", ".", "4 dígitos", 4);
    public static readonly Formato Cst = DigitosEntre("CST/CSOSN", 2, 3, "2 ou 3 dígitos, ex.: 00 ou 102");
    public static readonly Formato CstIbsCbs = DigitosEntre("CST IBS/CBS", 3, 3, "3 dígitos, ex.: 000");
    public static readonly Formato CClassTrib = DigitosEntre("cClassTrib", 6, 6, "6 dígitos, ex.: 000001");
    public static readonly Formato NumeroNota = DigitosEntre("Número da nota", 1, 9, "1 a 9 dígitos");
    public static readonly Formato ChaveAcesso = DigitosFixos("Chave de acesso", " ", "44 dígitos", 44);

    // ------------------------------------------------------------------ transporte
    public static readonly Formato Placa = new("Placa", "aceita só letras e números, sem hífen (ex.: ABC1D23).", c => char.IsAsciiLetterOrDigit(c), 7,
        v => Regex.IsMatch(v, @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$") ? null : "deve estar no padrão ABC1D23 (Mercosul) ou ABC1234, sem hífen.", Maiusculas: true);

    public static readonly Formato Renavam = DigitosEntre("RENAVAM", 9, 11, "9 a 11 dígitos");
    public static readonly Formato Rntrc = DigitosEntre("RNTRC", 8, 9, "8 ou 9 dígitos");
    public static readonly Formato Cnh = DigitosEntre("CNH", 11, 11, "11 dígitos");

    // ------------------------------------------------------------------ bancos
    public static readonly Formato CodigoBanco = DigitosEntre("Código do banco", 3, 3, "3 dígitos, ex.: 001");
    public static readonly Formato Agencia = DigitosEntre("Agência", 1, 5, "1 a 5 dígitos, sem o dígito verificador");
    public static readonly Formato ContaCorrente = DigitosEntre("Conta corrente", 1, 12, "1 a 12 dígitos, sem o dígito verificador");
    public static readonly Formato DigitoVerificador = new("Dígito", "aceita um número ou X.", c => Digito(c) || c is 'x' or 'X', 1, Maiusculas: true);
    public static readonly Formato Carteira = DigitosEntre("Carteira", 1, 3, "1 a 3 dígitos");
    public static readonly Formato Convenio = DigitosEntre("Convênio", 1, 20, "até 20 dígitos");
    public static readonly Formato EspecieTitulo = DigitosEntre("Espécie do título", 2, 2, "2 dígitos, ex.: 02");
    public static readonly Formato ModeloCampoLivre = new("Modelo do campo livre", "aceita as marcações {agencia:4}, {conta:7}, {nossonumero:11}...", c => !char.IsControl(c) && !char.IsWhiteSpace(c), 200,
        v => Cobranca.ValidarModeloCampoLivre(v));

    // ------------------------------------------------------------------ acesso
    public static readonly Formato Username = new("Username", "aceita letras sem acento, números, ponto, hífen e sublinhado.", c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_', 30,
        v => v.Length >= 3 ? null : "deve ter de 3 a 30 caracteres.");

    public static readonly Formato UsernameOuEmail = new("Username ou e-mail", "não aceita espaços.", c => !char.IsControl(c) && !char.IsWhiteSpace(c), 254);

    public static readonly Formato CodigoVerificacao = DigitosEntre("Código", 6, 6, "6 dígitos");
}
