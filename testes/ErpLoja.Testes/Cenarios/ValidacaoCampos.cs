using System.Reflection;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Validação de entrada: cada campo só aceita o tipo de dado dele (CNPJ com formação de CNPJ, número sem letras...),
/// na digitação, ao colar, ao sair do campo e na gravação.
/// </summary>
public class ValidacaoFormatosDeCampos : ICenario
{
    public string Area => "Validação";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        // ---------- regras dos formatos (camada de dados)
        u.Ok(Formatos.Cnpj.Validar("11.222.333/0001-81") is null && Formatos.Cnpj.Validar("11222333000181") is null
             && Formatos.Cnpj.Validar("11.222.333/0001-80") is not null && Formatos.Cnpj.Validar("1122233300018") is not null
             && Formatos.Cnpj.Validar("11.222.333/0001-8A") is not null,
            "CNPJ: aceita com ou sem pontuação; recusa dígito verificador errado, tamanho errado e letras");
        u.Ok(Formatos.Cnpj.Normalizar("11222333000181") == "11.222.333/0001-81" && Formatos.Cpf.Normalizar("52998224725") == "529.982.247-25",
            "CPF e CNPJ recebem a máscara padrão");
        u.Ok(Formatos.CpfCnpj.Validar("529.982.247-25") is null && Formatos.CpfCnpj.Validar("111.111.111-11") is not null,
            "CPF/CNPJ: sequência repetida é recusada");
        u.Ok(Formatos.Ncm.Validar("2106.90.90") is null && Formatos.Ncm.Normalizar("2106.90.90") == "21069090" && Formatos.Ncm.Validar("2106909") is not null
             && Formatos.Cfop.Validar("5102") is null && Formatos.Cfop.Validar("51O2") is not null,
            "NCM com 8 e CFOP com 4 dígitos (pontos removidos; a letra O no lugar de zero é recusada)");
        u.Ok(Formatos.Inteiro().Validar("15") is null && Formatos.Inteiro().Validar("15a") is not null && Formatos.Inteiro().Validar("1,5") is not null
             && Formatos.Percentual.Validar("0,90") is null && Formatos.Percentual.Validar("abc") is not null && Formatos.Percentual.Validar("150") is not null,
            "campos numéricos recusam texto; percentual só de 0 a 100");
        u.Ok(Formatos.Email.Validar("loja@empresa.com.br") is null && Formatos.Email.Validar("loja@empresa") is not null && Formatos.Email.Validar("lo ja@x.com") is not null,
            "e-mail no formato nome@dominio");
        u.Ok(Formatos.Placa.Validar("abc1d23") is null && Formatos.Placa.Normalizar("abc1d23") == "ABC1D23" && Formatos.Placa.Validar("ABC-1D23") is not null,
            "placa Mercosul/antiga em maiúsculas, sem hífen");
        u.Ok(Formatos.Cep.Normalizar("01310100") == "01310-100" && Formatos.Cep.Validar("0131010") is not null
             && Formatos.Telefone.Normalizar("11987654321") == "(11) 98765-4321" && Formatos.Telefone.Validar("98765") is not null,
            "CEP e telefone com a quantidade certa de dígitos e máscara");
        u.Ok(Formatos.CodigoBarras.Validar("7891234567895") is null && Formatos.CodigoBarras.Validar("7891234567890") is not null,
            "código de barras confere o dígito verificador");
        u.Ok(Formatos.UrlHttps.Validar("https://api.exemplo.com.br") is null && Formatos.UrlHttps.Validar("http://api.exemplo.com.br") is not null,
            "endereço de integração só com https");
        u.Ok(Formatos.Texto(10).Validar("12345678901") is not null && Formatos.Texto(50).Validar("abc\u0007def") is not null,
            "texto livre tem tamanho máximo e não aceita caracteres de controle");

        // ---------- comportamento na tela: digitação, colagem, saída do campo
        using var janela = new Form();
        var cnpj = Validacao.Caixa(Formatos.Cnpj, "CNPJ");
        var numero = Ui.Numero(0);
        janela.Controls.AddRange([cnpj, numero]);
        janela.Show();
        Usuario.Esperar();
        bool Digitado(Control c, char ch)
        {
            var e = new KeyPressEventArgs(ch);
            typeof(Control).GetMethod("OnKeyPress", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(c, [e]);
            return !e.Handled;
        }
        u.Ok(!Digitado(cnpj, 'A') && Digitado(cnpj, '5') && Digitado(cnpj, '/'), "no campo CNPJ, letra digitada é bloqueada; números e pontuação passam");
        cnpj.Text = "11a222b333c0001d81"; // colar texto com letras
        u.Ok(cnpj.Text == "11a222b333c0001d81" && Validacao.Erro(cnpj) is string colado && colado.Contains("aceita só números"),
            "texto colado com letras não é alterado em silêncio: fica marcado como inválido");
        cnpj.Text = "11222333000181";
        cnpj.Focus();
        numero.Focus();
        Usuario.Esperar();
        u.Ok(cnpj.Text == "11.222.333/0001-81", "ao sair do campo, o CNPJ recebe a máscara");
        cnpj.Text = "11222333000180";
        u.Ok(!Validacao.Conferir(cnpj) && Validacao.Erro(cnpj)!.Contains("CNPJ válido"), "CNPJ inválido fica marcado com a explicação");
        u.Ok(!Validacao.ConferirTodos(janela, mostrarMensagem: false), "a tela não grava com campo inválido");
        u.Ok(!Digitado(numero, 'x') && Digitado(numero, '7'), "campo numérico não aceita letras");

        // ---------- cadastro de pessoa recusa CNPJ inválido (gravação)
        var pessoas = u.AbrirTela("cadastro_pessoas");
        u.Clicar(pessoas, "Novo");
        var campos = Usuario.Todos(pessoas).OfType<TextBox>().Where(t => Validacao.FormatoDe(t) != null).ToList();
        campos.First(t => Validacao.FormatoDe(t) == Formatos.Nome).Text = "Empresa Teste";
        var doc = campos.First(t => Validacao.FormatoDe(t) == Formatos.CpfCnpj);
        doc.Text = "11222333000180";
        var antes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pessoas"));
        u.Clicar(pessoas, "Salvar");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM pessoas")) == antes && u.Viu("ERRO", "CPF/CNPJ"), "cadastro com CNPJ inválido não é gravado");
        doc.Text = "11222333000181";
        u.Clicar(pessoas, "Salvar");
        u.Ok(Ui.Txt(Db.Scalar("SELECT cpf_cnpj FROM pessoas WHERE nome = 'Empresa Teste'")) == "11.222.333/0001-81",
            "CNPJ válido é gravado no formato padrão");
        pessoas.Close();

        // ---------- configurações: camada de dados também recusa (sem passar pela tela)
        var cnpjLoja = Parametros.Definicoes.First(d => d.Chave == "cnpj");
        var erro = "";
        try { Parametros.Salvar(cnpjLoja, "123"); }
        catch (InvalidOperationException ex) { erro = ex.Message; }
        u.Ok(erro.Contains("CNPJ"), "parâmetro com formato inválido é recusado também na camada de dados");
    }
}
