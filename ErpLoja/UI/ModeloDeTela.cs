namespace ErpLoja.UI;

/// <summary>
/// Modelo padrão das telas: menu à esquerda (janela principal), listagem no meio e, à direita, as opções da tela numa
/// lista vertical com barra de rolagem. Pesquisa e filtros ficam acima da listagem. As telas continuam sendo montadas
/// com Ui.Barra(); ao abrir pelo menu, os botões da barra vão para o painel "Opções".
/// </summary>
public static class ModeloDeTela
{
    public const int LarguraOpcoes = 240;

    /// <summary>Marca do painel de opções (para testes e para não converter duas vezes).</summary>
    public static readonly object MarcaOpcoes = new();

    /// <summary>Aplica o modelo em todas as barras de filtros da tela que ainda não têm painel à direita.</summary>
    public static void Padronizar(Form f)
    {
        foreach (var barra in Todos(f).OfType<FlowLayoutPanel>()
                     .Where(p => p.Tag == Alteracoes.MarcaFiltros && p.Dock == DockStyle.Top && p.Parent != null).ToList())
        {
            var conteiner = barra.Parent!;
            // telas de cadastro já seguem o modelo (edição à direita); não criar um segundo painel
            if (conteiner.Controls.Cast<Control>().Any(c => c.Dock == DockStyle.Right && c != barra))
                continue;
            var botoes = barra.Controls.OfType<Button>().Where(b => b.Text != "?").ToList();
            if (botoes.Count == 0)
                continue;

            var opcoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = LarguraOpcoes,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 10, 8, 10),
                BackColor = Color.White,
                Tag = MarcaOpcoes,
            };
            opcoes.Controls.Add(new Label
            {
                Text = "OPÇÕES",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.75F),
                ForeColor = Color.FromArgb(107, 114, 128),
                Margin = new Padding(2, 2, 0, 8),
            });
            foreach (var b in botoes)
            {
                barra.Controls.Remove(b);
                b.AutoSize = false;
                b.MinimumSize = Size.Empty;
                b.Size = new Size(LarguraOpcoes - 34, 38);
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.AutoEllipsis = true;
                b.Margin = new Padding(0, 0, 0, 6);
                b.Padding = new Padding(10, 0, 4, 0);
                opcoes.Controls.Add(b);
            }
            // linha separando as opções da listagem
            opcoes.Paint += (_, e) =>
            {
                using var borda = new Pen(Tema.Fundo(Color.FromArgb(229, 231, 235)));
                e.Graphics.DrawLine(borda, 0, 0, 0, opcoes.Height);
            };
            conteiner.Controls.Add(opcoes);
            // ordem de encaixe: barra de filtros (largura toda) > opções (direita) > listagem (resto)
            opcoes.SendToBack();
            barra.SendToBack();
        }
    }

    public static FlowLayoutPanel? PainelDeOpcoes(Control raiz) =>
        Todos(raiz).OfType<FlowLayoutPanel>().FirstOrDefault(p => p.Tag == MarcaOpcoes);

    static IEnumerable<Control> Todos(Control c) => c.Controls.Cast<Control>().SelectMany(x => Todos(x).Prepend(x));
}
