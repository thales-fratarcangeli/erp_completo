using System.Reflection;
using ErpLoja;
using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Cenário de uso: simula uma pessoa usando o sistema pela interface (abre telas, digita, tecla, clica, responde
/// diálogos) e verifica o resultado. Cada classe que implementa ICenario é executada automaticamente pela bateria,
/// num banco novo e exclusivo (com ou sem dados de demonstração).
/// </summary>
public interface ICenario
{
    /// <summary>Área do sistema (aparece no relatório de testes).</summary>
    string Area { get; }

    /// <summary>true = o banco do cenário recebe os dados de demonstração antes de começar.</summary>
    bool ComDadosDemo => true;

    void Executar(Usuario u);
}

/// <summary>
/// O "usuário" do cenário: utilitários para operar as telas como uma pessoa faria, sem travar o teste.
/// Mensagens (Ui.Erro/Info/Confirmar) não abrem caixa: ficam em Mensagens, e Confirmar responde RespostaConfirmacao.
/// Janelas modais (diálogos, editores) são tratadas por QuandoAbrir; modal inesperado é cancelado e reprovado.
/// </summary>
public sealed class Usuario : IDisposable
{
    readonly Action<bool, string> ok;
    readonly string area;
    readonly List<(Func<Form, bool> Filtro, Action<Form> Acao, string Descricao)> pendentes = new();
    readonly HashSet<Form> tratados = new();
    readonly System.Windows.Forms.Timer vigia = new() { Interval = 60 };
    readonly Dictionary<Form, DateTime> modaisSemTratamento = new();

    /// <summary>Mensagens mostradas ao usuário durante o cenário: (ERRO|INFO|CONFIRMAR, texto).</summary>
    public List<(string Tipo, string Texto)> Mensagens { get; } = new();

    /// <summary>Resposta dada às confirmações (Sim = true). Use RespostasConfirmacao para uma sequência.</summary>
    public bool RespostaConfirmacao { get; set; } = true;

    public Queue<bool> RespostasConfirmacao { get; } = new();

    public Usuario(Action<bool, string> ok, string area)
    {
        this.ok = ok;
        this.area = area;
        Ui.Interceptar = (tipo, texto) =>
        {
            Mensagens.Add((tipo, texto));
            return tipo != "CONFIRMAR" || (RespostasConfirmacao.Count > 0 ? RespostasConfirmacao.Dequeue() : RespostaConfirmacao);
        };
        vigia.Tick += (_, _) => Vigiar();
        vigia.Start();
    }

    public void Dispose()
    {
        vigia.Dispose();
        Ui.Interceptar = null;
        foreach (var f in Application.OpenForms.Cast<Form>().ToList())
            if (!f.IsDisposed && f is not Toast)
                try { f.Dispose(); } catch { }
    }

    /// <summary>Registra uma verificação no relatório de testes.</summary>
    public void Ok(bool condicao, string descricao) => ok(condicao, $"[{area}] {descricao}");

    /// <summary>Última mensagem do tipo (ERRO, INFO, CONFIRMAR) ou null.</summary>
    public string? UltimaMensagem(string tipo) => Mensagens.LastOrDefault(m => m.Tipo == tipo).Texto;

    public bool Viu(string tipo, string trecho) => Mensagens.Any(m => m.Tipo == tipo && m.Texto.Contains(trecho, StringComparison.OrdinalIgnoreCase));

    // ------------------------------------------------------------------ telas

    /// <summary>Abre a tela do menu pela chave de navegação (como clicar no menu) e devolve o formulário.</summary>
    public Form AbrirTela(string chave)
    {
        var tela = Navegacao.Telas().First(t => t.Chave == chave);
        var f = tela.Criar!();
        f.StartPosition = FormStartPosition.Manual;
        f.WindowState = FormWindowState.Normal;
        f.Location = new Point(0, 0);
        f.Size = new Size(1400, 860);
        f.Show();
        Esperar();
        return f;
    }

    /// <summary>Processa a fila de mensagens do Windows por um tempo (deixa timers, animações e Load acontecerem).</summary>
    public static void Esperar(int ms = 150)
    {
        var fim = DateTime.Now.AddMilliseconds(ms);
        do
        {
            Application.DoEvents();
            Thread.Sleep(10);
        } while (DateTime.Now < fim);
    }

    public static IEnumerable<Control> Todos(Control raiz) => raiz.Controls.Cast<Control>().SelectMany(c => Todos(c).Prepend(c));

    /// <summary>Primeiro controle do tipo que atende ao filtro (falha se não houver).</summary>
    public static T Achar<T>(Control raiz, Func<T, bool>? filtro = null) where T : Control =>
        Todos(raiz).OfType<T>().FirstOrDefault(c => filtro?.Invoke(c) ?? true)
        ?? throw new InvalidOperationException($"{typeof(T).Name} não encontrado em '{raiz.Text}'.");

    public static Button Botao(Control raiz, string texto) =>
        Achar<Button>(raiz, b => b.Text.Trim().StartsWith(texto, StringComparison.OrdinalIgnoreCase));

    /// <summary>Clica no botão cujo texto começa com o informado.</summary>
    public void Clicar(Control raiz, string texto)
    {
        var b = Botao(raiz, texto);
        if (!b.Enabled)
            throw new InvalidOperationException($"O botão '{texto}' está desabilitado.");
        b.PerformClick();
        Esperar();
    }

    /// <summary>Tecla de atalho na tela (F2, F10, Esc, Ctrl+N...), como se o usuário pressionasse.</summary>
    public void Teclar(Form f, Keys tecla)
    {
        var metodo = typeof(Control).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var msg = Message.Create(f.Handle, 0x0100, (IntPtr)(int)(tecla & Keys.KeyCode), IntPtr.Zero);
        var tratada = (bool)metodo.Invoke(f, [msg, tecla])!;
        if (!tratada)
        {
            // teclas tratadas por eventos KeyDown (KeyPreview) e não por ProcessCmdKey
            var alvo = (Control?)f.ActiveControl ?? f;
            var e = new KeyEventArgs(tecla);
            typeof(Control).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(f.KeyPreview ? f : alvo, [e]);
            if (!e.Handled && f.KeyPreview && alvo != f)
                typeof(Control).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(alvo, [e]);
        }
        Esperar();
    }

    /// <summary>Digita num campo de texto e, se pedido, tecla Enter nele.</summary>
    public void Digitar(TextBox campo, string texto, bool enter = false)
    {
        campo.Focus();
        campo.Text = texto;
        if (enter)
        {
            var e = new KeyEventArgs(Keys.Enter);
            typeof(Control).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(campo, [e]);
        }
        Esperar();
    }

    // ------------------------------------------------------------------ janelas modais

    /// <summary>
    /// Agenda o que fazer quando abrir a próxima janela modal do tipo T (ex.: PagamentoForm) — a ação deve
    /// preencher e fechar a janela (clicar OK/Cancelar ou definir DialogResult).
    /// </summary>
    public void QuandoAbrir<T>(Action<T> acao) where T : Form =>
        pendentes.Add((f => f is T, f => acao((T)f), typeof(T).Name));

    /// <summary>Agenda a ação para a próxima janela modal cujo título contém o texto (ex.: Dialogo "Desconto na venda").</summary>
    public void QuandoAbrirJanela(string tituloContem, Action<Form> acao) =>
        pendentes.Add((f => f.Text.Contains(tituloContem, StringComparison.OrdinalIgnoreCase), acao, $"janela '{tituloContem}'"));

    /// <summary>Quantas ações de janela agendadas ainda não aconteceram.</summary>
    public int JanelasPendentes => pendentes.Count;

    /// <summary>Descarta as ações agendadas que não aconteceram (e as reprova).</summary>
    public void ConferirJanelas()
    {
        foreach (var p in pendentes)
            Ok(false, $"esperava abrir {p.Descricao}, mas não abriu");
        pendentes.Clear();
    }

    void Vigiar()
    {
        foreach (var f in Application.OpenForms.Cast<Form>().ToList())
        {
            if (f.IsDisposed || !f.Visible || !f.Modal || tratados.Contains(f) || f is Toast)
                continue;
            var i = pendentes.FindIndex(p => p.Filtro(f));
            if (i >= 0)
            {
                var (_, acao, descricao) = pendentes[i];
                pendentes.RemoveAt(i);
                tratados.Add(f);
                f.BeginInvoke(() =>
                {
                    try { acao(f); }
                    catch (Exception ex)
                    {
                        Ok(false, $"erro ao operar {descricao}: {ex.Message}");
                        f.DialogResult = DialogResult.Cancel;
                    }
                });
                continue;
            }
            // modal que o cenário não esperava: cancela depois de 2 s para nunca travar
            if (!modaisSemTratamento.TryGetValue(f, out var desde))
                modaisSemTratamento[f] = DateTime.Now;
            else if (DateTime.Now - desde > TimeSpan.FromSeconds(2))
            {
                tratados.Add(f);
                Ok(false, $"janela inesperada '{f.Text}' ({f.GetType().Name}) foi cancelada automaticamente");
                f.DialogResult = DialogResult.Cancel;
            }
        }
    }
}
