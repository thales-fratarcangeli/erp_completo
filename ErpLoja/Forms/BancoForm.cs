namespace ErpLoja.Forms;

/// <summary>Exibida na abertura quando nenhum banco foi encontrado: criar um novo ou indicar o arquivo.</summary>
public class BancoForm : Form
{
    public string? Caminho { get; private set; }
    public bool CriadoAgora { get; private set; }

    public BancoForm()
    {
        Ui.Estilizar(this, "ERP Loja - banco de dados", 640, 420, nameof(BancoForm));
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var lembrado = LocalBanco.Lembrado();
        var painel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24, 20, 24, 12),
        };
        var cabecalho = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 10) };
        cabecalho.Controls.Add(new PictureBox { Image = Ui.Logo(48), Size = new Size(48, 48), Margin = new Padding(0, 0, 12, 0) });
        cabecalho.Controls.Add(new Label { Text = "Banco de dados não encontrado", Font = Ui.FonteTitulo, AutoSize = true, Margin = new Padding(0, 10, 0, 0) });
        painel.Controls.Add(cabecalho);
        painel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(570, 0),
            Margin = new Padding(0, 0, 0, 14),
            Text = "Procurei o banco do sistema nestes lugares e não encontrei:\n" +
                   (lembrado is { Length: > 0 } ? $"• último banco usado: {lembrado}\n" : "") +
                   $"• ao lado do executável: {LocalBanco.AoLadoDoExecutavel}\n\nO que você quer fazer?",
        });

        var criar = Ui.Botao("Criar banco novo (zerado)", (_, _) => CriarNovo(), primario: true);
        var indicar = Ui.Botao("Indicar o arquivo do banco...", (_, _) => Indicar());
        var sair = Ui.Botao("Sair", (_, _) => Close());
        foreach (var b in new[] { criar, indicar, sair })
        {
            b.MinimumSize = new Size(300, 40);
            painel.Controls.Add(b);
        }
        painel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(570, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 10, 0, 0),
            Text = "Banco novo: acesso inicial com usuário admin e senha admin.",
        });
        Controls.Add(painel);
    }

    void CriarNovo()
    {
        var destino = LocalBanco.AoLadoDoExecutavel;
        if (!LocalBanco.PastaGravavel(AppContext.BaseDirectory) || File.Exists(destino))
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Onde criar o banco novo?",
                Filter = "Banco do ERP (*.db)|*.db",
                FileName = LocalBanco.NomePadrao,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = false,
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            destino = dlg.FileName;
        }
        if (File.Exists(destino))
        {
            Ui.Erro("Já existe um arquivo com esse nome. Use \"Indicar o arquivo do banco\" para abri-lo ou escolha outro nome.");
            return;
        }
        try
        {
            LocalBanco.CriarNovo(destino);
            Caminho = destino;
            CriadoAgora = true;
            Ui.Info($"Banco criado em:\n{destino}\n\nEntre com usuário admin e senha admin e troque a senha em Configurações > Minha conta.");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.Erro("Não foi possível criar o banco:\n" + ex.Message);
        }
    }

    void Indicar()
    {
        using var dlg = new OpenFileDialog { Title = "Selecione o banco do ERP (loja.db)", Filter = "Banco do ERP (*.db)|*.db|Todos os arquivos|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        if (LocalBanco.Validar(dlg.FileName) is string erro)
        {
            Ui.Erro(erro);
            return;
        }
        Caminho = dlg.FileName;
        DialogResult = DialogResult.OK;
    }
}
