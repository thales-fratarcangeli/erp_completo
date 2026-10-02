using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ErpLoja.UI;

/// <summary>
/// Na primeira execução neste usuário do Windows, cria "ERP Loja.lnk" na área de trabalho apontando para o executável.
/// Um marcador em %LOCALAPPDATA%\ErpLoja impede recriar o atalho se o usuário apagá-lo. Perfis isolados
/// (ERPLOJA_PREFERENCIAS, usado nos testes) não criam atalho.
/// </summary>
public static class AtalhoAreaDeTrabalho
{
    public const string NomeArquivo = "ERP Loja.lnk";
    static string Marcador => Path.Combine(LocalBanco.PastaPreferencias, "atalho_criado.txt");

    public static void CriarNaPrimeiraExecucao()
    {
        try
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ERPLOJA_PREFERENCIAS")) || File.Exists(Marcador))
                return;
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return;
            var destino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), NomeArquivo);
            if (!File.Exists(destino))
            {
                Criar(destino, exe);
                Log.Registrar(Log.Sistema, "Atalho criado na área de trabalho", destino, tela: "");
            }
            Directory.CreateDirectory(LocalBanco.PastaPreferencias);
            File.WriteAllText(Marcador, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {destino} -> {exe}");
        }
        catch (Exception ex)
        {
            Log.Registrar(Log.Erro, "Não foi possível criar o atalho na área de trabalho", ex.Message, tela: "");
        }
    }

    /// <summary>Grava um atalho .lnk (IShellLink do Windows).</summary>
    public static void Criar(string arquivoLnk, string alvo)
    {
        var link = (IShellLinkW)new ShellLink();
        link.SetPath(alvo);
        link.SetWorkingDirectory(Path.GetDirectoryName(alvo)!);
        link.SetDescription("ERP Loja - sistema de gestão");
        link.SetIconLocation(alvo, 0);
        ((IPersistFile)link).Save(arquivoLnk, true);
        Marshal.FinalReleaseComObject(link);
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    class ShellLink;

    // a ordem dos métodos é a da vtable do IShellLinkW: não reordenar
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
