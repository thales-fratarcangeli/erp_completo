using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace ErpLoja.Data;

/// <summary>Envio de e-mail pela API do Resend (https://resend.com).</summary>
public static class Email
{
    /// <summary>Permite substituir o transporte HTTP nos testes.</summary>
    public static HttpMessageHandler? TransporteParaTestes { get; set; }

    public static async Task Enviar(string para, string assunto, string html)
    {
        var chave = Parametros.ResendApiKey;
        var remetente = Parametros.ResendRemetente;
        if (string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(remetente))
            throw new InvalidOperationException(
                "O envio de e-mail não está configurado. Peça ao administrador para preencher Configurações > E-mail (chave do Resend e remetente).");

        using var http = TransporteParaTestes is null ? new HttpClient() : new HttpClient(TransporteParaTestes, disposeHandler: false);
        http.Timeout = TimeSpan.FromSeconds(20);
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(new System.Text.Json.Nodes.JsonObject
            {
                ["from"] = remetente,
                ["to"] = new System.Text.Json.Nodes.JsonArray(para),
                ["subject"] = assunto,
                ["html"] = html,
            }.ToJsonString(), System.Text.Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", chave);

        HttpResponseMessage resp;
        try
        {
            resp = await http.SendAsync(req);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Não foi possível falar com o Resend (verifique a internet): " + ex.Message, ex);
        }
        using (resp)
        {
            if (resp.IsSuccessStatusCode)
                return;
            var corpo = await resp.Content.ReadAsStringAsync();
            var mensagem = corpo;
            try
            {
                using var json = JsonDocument.Parse(corpo);
                if (json.RootElement.TryGetProperty("message", out var m))
                    mensagem = m.GetString() ?? corpo;
            }
            catch (JsonException) { }
            throw new InvalidOperationException($"O Resend recusou o envio ({(int)resp.StatusCode}): {mensagem}");
        }
    }

    public static string Mascarar(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 1)
            return email;
        return email[0] + new string('*', Math.Min(6, arroba - 1)) + email[arroba..];
    }

    public static bool Valido(string email) =>
        System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}

/// <summary>
/// Recuperação de senha por código enviado ao e-mail do usuário.
/// Código de 6 dígitos, guardado apenas como hash, válido por 15 minutos, no máximo 5 tentativas; um novo código enviado com sucesso invalida os anteriores.
/// </summary>
public static class RecuperacaoSenha
{
    public const int MinutosValidade = 15, MaxTentativas = 5;

    static DataRow? Usuario(string usernameOuEmail) =>
        Db.Query("""
            SELECT id, nome, username, email FROM usuarios
            WHERE ativo = 1 AND (username = @p0 OR email = @p0 COLLATE NOCASE)
            LIMIT 1
            """, usernameOuEmail.Trim()).Rows.Cast<DataRow>().FirstOrDefault();

    /// <summary>Gera e envia o código. Devolve o e-mail mascarado para mostrar ao usuário.</summary>
    public static async Task<string> Solicitar(string usernameOuEmail)
    {
        var u = Usuario(usernameOuEmail)
                ?? throw new InvalidOperationException("Usuário não encontrado ou inativo.");
        var email = Ui.Txt(u["email"]);
        if (!Email.Valido(email))
            throw new InvalidOperationException("Este usuário não tem e-mail cadastrado. Peça a um administrador para redefinir a senha em Cadastros > Usuários.");

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var id = (long)u["id"];
        var expira = DateTime.Now.AddMinutes(MinutosValidade);

        var loja = System.Net.WebUtility.HtmlEncode(Db.Config("nome_loja"));
        await Email.Enviar(email, $"Código para redefinir sua senha - {Db.Config("nome_loja")}", $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#0b0b0b">
              <p>Olá, {System.Net.WebUtility.HtmlEncode(Ui.Txt(u["nome"]))}.</p>
              <p>Use o código abaixo para redefinir sua senha do ERP ({loja}). Usuário: <b>{System.Net.WebUtility.HtmlEncode(Ui.Txt(u["username"]))}</b></p>
              <p style="font-size:28px;font-weight:600;letter-spacing:6px">{codigo}</p>
              <p style="color:#52514e">O código vale por {MinutosValidade} minutos. Se você não pediu, ignore este e-mail.</p>
            </div>
            """);
        // só depois do envio: se o e-mail falhar, o código recebido antes continua valendo
        using (var s = new DbSessao(transacao: true))
        {
            s.Exec("UPDATE recuperacao_senha SET usado = 1 WHERE usuario_id = @p0 AND usado = 0", id);
            s.Exec("INSERT INTO recuperacao_senha(usuario_id, codigo_hash, expira_em) VALUES(@p0, @p1, @p2)",
                id, Senhas.Hash(codigo), expira);
            s.Commit();
        }
        Log.Registrar(Log.Acesso, "Código de recuperação de senha enviado", $"usuário {Ui.Txt(u["username"])} → {Email.Mascarar(email)}", tela: "Recuperar senha");
        return Email.Mascarar(email);
    }

    /// <summary>Confere o código e grava a nova senha. Lança exceção com a mensagem para o usuário se falhar.</summary>
    public static void Redefinir(string usernameOuEmail, string codigo, string novaSenha)
    {
        if (RegrasCadastro.Senha(novaSenha) is string erroSenha)
            throw new InvalidOperationException(erroSenha);
        var u = Usuario(usernameOuEmail) ?? throw new InvalidOperationException("Usuário não encontrado ou inativo.");
        var id = (long)u["id"];

        using var s = new DbSessao(transacao: true);
        var pedido = s.Query("""
            SELECT id, codigo_hash, expira_em, tentativas FROM recuperacao_senha
            WHERE usuario_id = @p0 AND usado = 0 ORDER BY id DESC LIMIT 1
            """, id).Rows.Cast<DataRow>().FirstOrDefault()
            ?? throw new InvalidOperationException("Nenhum código pendente. Solicite um novo código.");

        var pedidoId = (long)pedido["id"];
        if (Db.LerData(pedido["expira_em"]) < DateTime.Now)
        {
            s.Exec("UPDATE recuperacao_senha SET usado = 1 WHERE id = @p0", pedidoId);
            s.Commit();
            throw new InvalidOperationException("O código expirou. Solicite um novo código.");
        }
        if (!Senhas.Verificar(codigo.Trim(), Ui.Txt(pedido["codigo_hash"])))
        {
            var tentativas = (long)pedido["tentativas"] + 1;
            s.Exec("UPDATE recuperacao_senha SET tentativas = @p0, usado = @p1 WHERE id = @p2", tentativas, tentativas >= MaxTentativas, pedidoId);
            s.Commit();
            Log.Registrar(Log.Acesso, "Código de recuperação incorreto", $"usuário {Ui.Txt(u["username"])}, tentativa {tentativas}", tela: "Recuperar senha");
            throw new InvalidOperationException(tentativas >= MaxTentativas
                ? "Código incorreto. Limite de tentativas atingido: solicite um novo código."
                : $"Código incorreto. Restam {MaxTentativas - tentativas} tentativa(s).");
        }
        s.Exec("UPDATE usuarios SET senha = @p0 WHERE id = @p1", Senhas.Hash(novaSenha), id);
        s.Exec("UPDATE recuperacao_senha SET usado = 1 WHERE id = @p0", pedidoId);
        s.Commit();
        Log.Registrar(Log.Acesso, "Senha redefinida por código", $"usuário {Ui.Txt(u["username"])}", tela: "Recuperar senha");
    }
}
