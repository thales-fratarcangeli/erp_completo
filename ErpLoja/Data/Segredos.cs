using System.Security.Cryptography;
using System.Text;

namespace ErpLoja.Data;

/// <summary>
/// Credenciais de integração (chave do Resend, senha da Pamcard) cifradas com a DPAPI do Windows antes de ir para o banco
/// (OWASP A04). O escopo é a máquina: o próprio computador decifra; uma cópia do loja.db levada para outro computador
/// não revela as credenciais (elas precisam ser informadas de novo em Configurações). Valores antigos em texto puro
/// continuam sendo lidos e são cifrados pela migração 13.
/// </summary>
public static class Segredos
{
    public const string Prefixo = "dpapi:";
    static readonly byte[] Entropia = Encoding.UTF8.GetBytes("ErpLoja/credenciais/v1");

    public static bool Cifrado(string? valor) => valor?.StartsWith(Prefixo, StringComparison.Ordinal) == true;

    public static string Proteger(string? texto)
    {
        if (string.IsNullOrEmpty(texto) || Cifrado(texto))
            return texto ?? "";
        var cifrado = ProtectedData.Protect(Encoding.UTF8.GetBytes(texto), Entropia, DataProtectionScope.LocalMachine);
        return Prefixo + Convert.ToBase64String(cifrado);
    }

    /// <summary>Texto original; vazio se a credencial foi cifrada em outro computador (registra no log).</summary>
    public static string Abrir(string? armazenado)
    {
        if (!Cifrado(armazenado))
            return armazenado ?? "";
        try
        {
            var dados = Convert.FromBase64String(armazenado![Prefixo.Length..]);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(dados, Entropia, DataProtectionScope.LocalMachine));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            Log.Registrar(Log.Erro, "Credencial cifrada não pôde ser lida neste computador",
                "O banco veio de outro computador ou a credencial foi corrompida. Informe-a de novo em Configurações.");
            return "";
        }
    }
}
