namespace ErpLoja.Data;

/// <summary>
/// Datas plausíveis: o sistema não aceita datas como 01/01/0201 ou 2099 digitadas por engano (problema encontrado em
/// bancos de outros sistemas, onde esses erros distorcem prazos, vencimentos e relatórios).
/// </summary>
public static class Datas
{
    public static readonly DateTime Minima = new(2000, 1, 1);
    public static DateTime Maxima => DateTime.Today.AddYears(10);

    public static bool Plausivel(DateTime d) => d >= Minima && d <= Maxima;

    public static string MensagemImplausivel(string campo) =>
        $"Confira o {campo}: a data deve estar entre {Minima:dd/MM/yyyy} e {Maxima:dd/MM/yyyy}.";
}
