using ErpLoja.Data;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Operador homologando: passa no validador um arquivo com campos numéricos em branco (como alguns bancos mandam) e digita
/// um modelo de campo livre absurdo. O sistema tem que apontar o problema, nunca estourar exceção.
/// </summary>
public class CobrancaArquivosInvalidos : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        FinanceiroUtil.ContaReceber("Título para arquivo", 50m, 5);
        Cobranca.Emitir(banco, FinanceiroUtil.UltimoId("contas"));
        var (_, _, conteudo, _) = Cobranca.GerarRemessa(banco);
        var linhas = conteudo.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

        // trailer de arquivo com a quantidade de registros em branco e valor do título com letras
        linhas[^1] = linhas[^1][..23] + new string(' ', 6) + linhas[^1][29..];
        var p = linhas.FindIndex(l => l[13] == 'P');
        linhas[p] = linhas[p][..85] + "00000000000ABCD" + linhas[p][100..];
        List<string> erros;
        try
        {
            erros = Cobranca.ValidarArquivo240(string.Join("\r\n", linhas), remessa: true);
        }
        catch (Exception ex)
        {
            u.Ok(false, $"validador estourou exceção com campo numérico inválido: {ex.GetType().Name}: {ex.Message}");
            return;
        }
        u.Ok(erros.Any(e => e.Contains("quantidade de registros") && e.Contains("numérico")), "validador aponta quantidade em branco no trailer");
        u.Ok(erros.Any(e => e.Contains("valor do título") && e.Contains("numérico")), "validador aponta valor com letras no segmento P");

        try
        {
            var msg = Cobranca.ValidarModeloCampoLivre("{agencia:99999999999}{nossonumero:11}");
            u.Ok(msg != null, "modelo com tamanho absurdo é recusado com mensagem");
        }
        catch (Exception ex)
        {
            u.Ok(false, $"modelo com tamanho absurdo estourou exceção: {ex.GetType().Name}");
        }
    }
}
