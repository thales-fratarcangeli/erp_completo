using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace ErpLoja.Data;

/// <summary>
/// Impressão de cupons e documentos.
/// Os textos usam marcações no início da linha: [C] centraliza, [N] negrito, [G] fonte grande (podem ser combinadas: "[C][N]").
/// - Impressora Windows: qualquer impressora instalada (driver), desenhando o texto.
/// - ESC/POS: impressoras térmicas não fiscais instaladas no Windows recebem comandos diretos (RAW) — corte de papel e gaveta.
/// </summary>
public static class Impressao
{
    public static string[] ImpressorasInstaladas()
    {
        try
        {
            return ["", .. PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(n => n)];
        }
        catch
        {
            return [""];
        }
    }

    record Linha(string Texto, bool Centro, bool Negrito, bool Grande);

    static Linha Interpretar(string bruta)
    {
        bool centro = false, negrito = false, grande = false;
        var t = bruta;
        while (t.Length >= 3 && t[0] == '[' && t[2] == ']')
        {
            switch (t[1])
            {
                case 'C': centro = true; break;
                case 'N': negrito = true; break;
                case 'G': grande = true; break;
                default: return new Linha(t, centro, negrito, grande);
            }
            t = t[3..];
        }
        return new Linha(t, centro, negrito, grande);
    }

    /// <summary>Texto sem as marcações (para exibir na tela).</summary>
    public static string SemMarcas(string texto) =>
        string.Join(Environment.NewLine, texto.Replace("\r\n", "\n").Split('\n').Select(l => Interpretar(l).Texto));

    /// <summary>Imprime um cupom (comprovante, TEF) na impressora de cupom configurada.</summary>
    public static void ImprimirCupom(string texto, bool abrirGaveta = false)
    {
        if (Parametros.ImpressoraEscPos)
            Raw.Enviar(Parametros.ImpressoraCupom, EscPos(texto, Parametros.ImpressoraColunas, Parametros.ImpressoraCodepage,
                Parametros.ImpressoraCortar, abrirGaveta && Parametros.ImpressoraGaveta));
        else
            ImprimirWindows(Parametros.ImpressoraCupom, texto, Parametros.ImpressoraColunas, 9F);
    }

    /// <summary>Imprime um documento (DANFE, pedido, boleto em texto) na impressora de documentos.</summary>
    public static void ImprimirDocumento(string texto, int colunas) =>
        ImprimirWindows(Parametros.ImpressoraDocumentos, texto, colunas, colunas > 80 ? 7.5F : 9F);

    /// <summary>Página de teste para conferir a configuração.</summary>
    public static string PaginaDeTeste() => $"""
        [C][G]TESTE DE IMPRESSÃO
        [C]{Db.Config("nome_loja")}
        {new string('-', Parametros.ImpressoraColunas)}
        Acentuação: ÁÉÍÓÚ ÂÊÔ ÃÕ Ç áéíóú âêô ãõ ç
        [N]Linha em negrito
        Colunas configuradas: {Parametros.ImpressoraColunas}
        {new string('=', Parametros.ImpressoraColunas)}
        [C]{DateTime.Now:dd/MM/yyyy HH:mm:ss}
        """;

    // ------------------------------------------------------------------ Windows (driver)

    static void ImprimirWindows(string impressora, string texto, int colunas, float tamanho)
    {
        var linhas = texto.Replace("\r\n", "\n").Split('\n').Select(Interpretar).ToList();
        int proxima = 0;
        using var doc = new PrintDocument { DocumentName = "ERP Loja" };
        if (!string.IsNullOrWhiteSpace(impressora))
            doc.PrinterSettings.PrinterName = impressora;
        if (!doc.PrinterSettings.IsValid)
            throw new InvalidOperationException($"Impressora \"{impressora}\" não encontrada. Verifique Configurações > Impressoras.");
        doc.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            using var normal = new Font("Consolas", tamanho);
            using var negrito = new Font("Consolas", tamanho, FontStyle.Bold);
            using var grande = new Font("Consolas", tamanho * 1.6F, FontStyle.Bold);
            var area = e.MarginBounds;
            // papel estreito (bobina): usa a largura inteira
            if (e.PageBounds.Width < 400)
                area = new Rectangle(4, 4, e.PageBounds.Width - 8, e.PageBounds.Height - 8);
            float y = area.Top;
            while (proxima < linhas.Count)
            {
                var l = linhas[proxima];
                var fonte = l.Grande ? grande : l.Negrito ? negrito : normal;
                var altura = fonte.GetHeight(g);
                if (y + altura > area.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
                var x = (float)area.Left;
                if (l.Centro)
                    x = area.Left + (area.Width - g.MeasureString(l.Texto, fonte).Width) / 2;
                g.DrawString(l.Texto, fonte, Brushes.Black, x, y);
                y += altura;
                proxima++;
            }
        };
        doc.Print();
    }

    // ------------------------------------------------------------------ ESC/POS

    /// <summary>Converte o texto marcado em comandos ESC/POS.</summary>
    public static byte[] EscPos(string texto, int colunas, int codepage, bool cortar, bool abrirGaveta)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(codepage);
        var b = new List<byte> { 0x1B, 0x40 };                       // ESC @  inicializa
        b.AddRange([0x1B, 0x74, (byte)(codepage == 860 ? 3 : 2)]);    // ESC t  página de código
        if (abrirGaveta)
            b.AddRange([0x1B, 0x70, 0x00, 0x19, 0xFA]);              // ESC p  pulso na gaveta
        // caracteres de controle vindos dos dados (ex.: nome de produto) virariam comandos ESC/POS: removidos (OWASP A05)
        texto = new string(texto.Replace("\r\n", "\n").Where(c => c == '\n' || !char.IsControl(c)).ToArray());
        foreach (var bruta in texto.Split('\n'))
        {
            var l = Interpretar(bruta);
            b.AddRange([0x1B, 0x61, (byte)(l.Centro ? 1 : 0)]);      // ESC a  alinhamento
            b.AddRange([0x1B, 0x45, (byte)(l.Negrito || l.Grande ? 1 : 0)]); // ESC E negrito
            b.AddRange([0x1D, 0x21, (byte)(l.Grande ? 0x11 : 0x00)]); // GS !   tamanho
            var t = l.Grande ? l.Texto.Trim() : l.Texto;
            if (!l.Grande && t.Length > colunas)
                t = t[..colunas];
            b.AddRange(enc.GetBytes(t));
            b.Add(0x0A);
        }
        b.AddRange([0x1B, 0x61, 0x00, 0x1B, 0x45, 0x00, 0x1D, 0x21, 0x00]);
        b.AddRange([0x1B, 0x64, 0x04]);                               // ESC d 4 avança
        if (cortar)
            b.AddRange([0x1D, 0x56, 0x42, 0x00]);                     // GS V   corte parcial
        return b.ToArray();
    }

    /// <summary>Envio de bytes direto ao spooler do Windows (impressora instalada, tipo de dado RAW).</summary>
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    static class Raw
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DocInfo
        {
            public string pDocName;
            public string? pOutputFile;
            public string pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "OpenPrinterW")]
        static extern bool OpenPrinter(string nome, out IntPtr handle, IntPtr padrao);

        [DllImport("winspool.drv", SetLastError = true, EntryPoint = "ClosePrinter")]
        static extern bool ClosePrinter(IntPtr handle);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "StartDocPrinterW")]
        static extern int StartDocPrinter(IntPtr handle, int nivel, ref DocInfo info);

        [DllImport("winspool.drv", SetLastError = true, EntryPoint = "EndDocPrinter")]
        static extern bool EndDocPrinter(IntPtr handle);

        [DllImport("winspool.drv", SetLastError = true, EntryPoint = "StartPagePrinter")]
        static extern bool StartPagePrinter(IntPtr handle);

        [DllImport("winspool.drv", SetLastError = true, EntryPoint = "EndPagePrinter")]
        static extern bool EndPagePrinter(IntPtr handle);

        [DllImport("winspool.drv", SetLastError = true, EntryPoint = "WritePrinter")]
        static extern bool WritePrinter(IntPtr handle, byte[] dados, int tamanho, out int escritos);

        public static void Enviar(string impressora, byte[] dados)
        {
            if (string.IsNullOrWhiteSpace(impressora))
                impressora = new PrinterSettings().PrinterName;
            if (!OpenPrinter(impressora, out var h, IntPtr.Zero))
                throw new InvalidOperationException($"Não foi possível abrir a impressora \"{impressora}\" (erro {Marshal.GetLastWin32Error()}).");
            try
            {
                var info = new DocInfo { pDocName = "ERP Loja - cupom", pDataType = "RAW" };
                if (StartDocPrinter(h, 1, ref info) == 0)
                    throw new InvalidOperationException($"A impressora recusou o documento (erro {Marshal.GetLastWin32Error()}).");
                try
                {
                    StartPagePrinter(h);
                    if (!WritePrinter(h, dados, dados.Length, out var escritos) || escritos != dados.Length)
                        throw new InvalidOperationException($"Falha ao enviar os dados à impressora (erro {Marshal.GetLastWin32Error()}).");
                    EndPagePrinter(h);
                }
                finally
                {
                    EndDocPrinter(h);
                }
            }
            finally
            {
                ClosePrinter(h);
            }
        }
    }
}
