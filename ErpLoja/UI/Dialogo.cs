namespace ErpLoja.UI;

/// <summary>Grade de pares rótulo/campo (usada nos diálogos e na tela de configurações).</summary>
public class PainelCampos : TableLayoutPanel
{
    public PainelCampos()
    {
        ColumnCount = 2;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    public T Adicionar<T>(string rotulo, T controle, int largura = 300) where T : Control
    {
        if (controle is TextBox or ComboBox && controle.Width < largura)
            controle.Width = largura;
        controle.Margin = new Padding(3, 4, 3, 4);
        int linha = RowCount++;
        Controls.Add(new Label
        {
            Text = rotulo,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 10, 3),
        }, 0, linha);
        Controls.Add(controle, 1, linha);
        return controle;
    }

    public Label Nota(string texto, int larguraMaxima = 520)
    {
        var lbl = new Label { Text = texto, AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(larguraMaxima, 0), Margin = new Padding(3, 8, 3, 3) };
        int linha = RowCount++;
        Controls.Add(lbl, 0, linha);
        SetColumnSpan(lbl, 2);
        return lbl;
    }

    public T Linha<T>(T controle) where T : Control
    {
        int linha = RowCount++;
        Controls.Add(controle, 0, linha);
        SetColumnSpan(controle, 2);
        return controle;
    }
}

/// <summary>Janela simples de formulário: pares rótulo/campo + OK e Cancelar, com "?" de ajuda na barra de título.</summary>
public class Dialogo : Form
{
    readonly PainelCampos campos = new();

    /// <summary>Chamado ao clicar em OK; retornando false a janela continua aberta.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<bool>? Validar { get; set; }

    /// <summary>Ao fechar sem confirmar, perguntar se descarta o que foi digitado (desligado em diálogos rápidos do caixa).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ConfirmarAoSair { get; set; } = true;

    /// <param name="ajuda">Chave do tópico de ajuda (ver Ajuda.cs).</param>
    public Dialogo(string titulo, string ajuda, string textoOk = "OK")
    {
        Text = titulo;
        Font = Ui.Fonte;
        BackColor = Color.White;
        Icon = Ui.Icone;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Ajuda.InstalarEmDialogo(this, ajuda);
        Alteracoes.Monitorar(this);
        Tema.Acompanhar(this);
        FormClosing += (_, e) =>
        {
            if (ConfirmarAoSair && DialogResult != DialogResult.OK && !Alteracoes.PodeSair(this, "fechar esta janela", permitirSalvar: false))
            {
                e.Cancel = true;
                DialogResult = DialogResult.None;
            }
        };

        var ok = Ui.Botao(textoOk, (_, _) =>
        {
            // formato de cada campo do diálogo primeiro (o primeiro inválido recebe o cursor), depois a regra do diálogo
            if (Validacao.ConferirTodos(this) && (Validar?.Invoke() ?? true))
                DialogResult = DialogResult.OK;
        }, primario: true);
        var cancelar = Ui.Botao("Cancelar");
        cancelar.DialogResult = DialogResult.Cancel;

        var botoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 12, 0, 0),
        };
        botoes.Controls.AddRange([cancelar, ok]);

        var raiz = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        raiz.Controls.Add(campos);
        raiz.Controls.Add(botoes);
        Controls.Add(raiz);

        AcceptButton = ok;
        CancelButton = cancelar;
    }

    public T Adicionar<T>(string rotulo, T controle) where T : Control => campos.Adicionar(rotulo, controle);

    public void Nota(string texto) => campos.Nota(texto);

    public LinkLabel Link(string texto, Action acao)
    {
        var link = new LinkLabel { Text = texto, AutoSize = true, Margin = new Padding(3, 8, 3, 3), LinkColor = Ui.Primaria };
        link.LinkClicked += (_, _) => acao();
        return campos.Linha(link);
    }
}
