namespace ErpLoja.UI;

/// <summary>Código de barras Intercalado 2 de 5 (ITF), usado nos boletos (44 dígitos, padrão Febraban).</summary>
public static class CodigoBarras
{
    static readonly string[] Padroes = ["nnwwn", "wnnnw", "nwnnw", "wwnnn", "nnwnw", "wnwnn", "nwwnn", "nnnww", "wnnwn", "nwnwn"];

    /// <summary>Desenha o código na área (barra estreita = 1 unidade, larga = 3; quiet zone dos dois lados).</summary>
    public static void Desenhar(Graphics g, string digitos, RectangleF area)
    {
        if (digitos.Length % 2 != 0 || !digitos.All(char.IsDigit))
            throw new ArgumentException("ITF exige quantidade par de dígitos.");
        // início (n n n n), pares (5 barras + 5 espaços intercalados), fim (W n n)
        var elementos = new List<(bool Barra, int Largura)> { (true, 1), (false, 1), (true, 1), (false, 1) };
        for (int i = 0; i < digitos.Length; i += 2)
        {
            var barras = Padroes[digitos[i] - '0'];
            var espacos = Padroes[digitos[i + 1] - '0'];
            for (int k = 0; k < 5; k++)
            {
                elementos.Add((true, barras[k] == 'w' ? 3 : 1));
                elementos.Add((false, espacos[k] == 'w' ? 3 : 1));
            }
        }
        elementos.AddRange([(true, 3), (false, 1), (true, 1)]);
        var unidades = elementos.Sum(e => e.Largura) + 20;
        var unidade = area.Width / unidades;
        var x = area.X + 10 * unidade;
        foreach (var (barra, largura) in elementos)
        {
            if (barra)
                g.FillRectangle(Brushes.Black, x, area.Y, largura * unidade, area.Height);
            x += largura * unidade;
        }
    }
}
