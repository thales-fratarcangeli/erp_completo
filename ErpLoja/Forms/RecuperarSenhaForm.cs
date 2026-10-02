namespace ErpLoja.Forms;

/// <summary>Recuperação de senha: envia um código ao e-mail (Resend) e redefine a senha.</summary>
public class RecuperarSenhaForm : Form
{
    readonly TextBox usuario = new(), codigo = new() { MaxLength = 6 }, nova = new() { UseSystemPasswordChar = true }, confirma = new() { UseSystemPasswordChar = true };
    readonly Button enviar, redefinir;
    readonly Label situacao = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(440, 0), Margin = new Padding(3, 8, 3, 3) };

    public RecuperarSenhaForm(string usernameInicial)
    {
        Validacao.Aplicar(usuario, Formatos.UsernameOuEmail, "Username ou e-mail");
        Validacao.Aplicar(codigo, Formatos.CodigoVerificacao, "Código");
        Validacao.Aplicar(nova, Formatos.Senha, "Nova senha");
        Validacao.Aplicar(confirma, Formatos.Senha, "Confirmação");
        Text = "Recuperar senha";
        Font = Ui.Fonte;
        Icon = Ui.Icone;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Ajuda.InstalarEmDialogo(this, nameof(RecuperarSenhaForm));

        var campos = new PainelCampos();
        campos.Nota("1. Informe seu username ou e-mail e clique em Enviar código.");
        campos.Adicionar("Username ou e-mail:", usuario);
        enviar = campos.Linha(Ui.Botao("Enviar código", async (_, _) => await Enviar(), primario: true));
        campos.Nota("2. Digite o código recebido por e-mail e a nova senha.");
        campos.Adicionar("Código (6 dígitos):", codigo);
        campos.Adicionar("Nova senha:", nova);
        campos.Adicionar("Confirmar senha:", confirma);
        redefinir = campos.Linha(Ui.Botao("Redefinir senha", (_, _) => Redefinir(), primario: true));
        campos.Linha(situacao);
        Controls.Add(campos);

        usuario.Text = usernameInicial;
        AlternarEtapa2(false);
    }

    void AlternarEtapa2(bool ativa)
    {
        foreach (var c in new Control[] { codigo, nova, confirma, redefinir })
            c.Enabled = ativa;
    }

    async Task Enviar()
    {
        if (string.IsNullOrWhiteSpace(usuario.Text))
        {
            Ui.Erro("Informe o username ou e-mail.");
            return;
        }
        enviar.Enabled = false;
        situacao.Text = "Enviando...";
        try
        {
            var destino = await RecuperacaoSenha.Solicitar(usuario.Text);
            situacao.Text = $"Código enviado para {destino}. Ele vale {RecuperacaoSenha.MinutosValidade} minutos.";
            AlternarEtapa2(true);
            codigo.Focus();
        }
        catch (Exception ex)
        {
            situacao.Text = "";
            Ui.Erro(ex.Message);
        }
        finally
        {
            enviar.Enabled = true;
        }
    }

    void Redefinir()
    {
        if (nova.Text != confirma.Text)
        {
            Ui.Erro("A confirmação não confere com a nova senha.");
            return;
        }
        try
        {
            RecuperacaoSenha.Redefinir(usuario.Text, codigo.Text, nova.Text);
            Ui.Info("Senha redefinida. Entre com a nova senha.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Ui.Erro(ex.Message);
        }
    }
}
