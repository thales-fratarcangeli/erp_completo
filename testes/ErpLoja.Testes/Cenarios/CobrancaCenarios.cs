using System.Data;
using ErpLoja.Data;
using ErpLoja.Forms;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>
/// Operador emitindo boleto pela tela de Cobrança: escolhe a conta na janela "Emitir boletos", confere código de barras e
/// linha digitável (RN-COB-03/04/05) com um cálculo independente, tenta duplicar e emitir sem cliente, e abre o BoletoForm.
/// </summary>
public class CobrancaEmiteBoletoPelaTela : ICenario
{
    public string Area => "Cobranca";

    static int Mod10(string n)
    {
        int soma = 0;
        for (int i = 0; i < n.Length; i++)
        {
            var p = (n[n.Length - 1 - i] - '0') * (i % 2 == 0 ? 2 : 1);
            soma += p / 10 + p % 10;
        }
        return (10 - soma % 10) % 10;
    }

    static int Mod11(string n)
    {
        int soma = 0;
        for (int i = 0; i < n.Length; i++)
            soma += (n[n.Length - 1 - i] - '0') * (2 + i % 8);
        var dv = 11 - soma % 11;
        return dv is 0 or 10 or 11 ? 1 : dv;
    }

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var vencimento = DateTime.Today.AddDays(20);
        var conta = FinanceiroUtil.ContaReceber("Serviço de entrega (boleto)", 150.75m, 20);

        var f = u.AbrirTela("CobrancaForm");
        u.QuandoAbrirJanela("Emitir boletos", janela =>
        {
            var grade = Usuario.Achar<DataGridView>(janela);
            var dt = (DataTable)grade.DataSource!;
            u.Ok(dt.Rows.Cast<DataRow>().Any(r => Convert.ToInt64(r["id"]) == conta), "a conta nova aparece para emitir boleto");
            foreach (DataRow r in dt.Rows)
                r["Emitir"] = Convert.ToInt64(r["id"]) == conta;
            u.Clicar(janela, "Emitir selecionados");
        });
        u.Clicar(f, "Emitir boletos");
        Usuario.Esperar(300);
        u.ConferirJanelas();

        var b = Db.Query("SELECT * FROM boletos WHERE conta_id = @p0", conta).Rows.Cast<DataRow>().FirstOrDefault();
        u.Ok(b != null, "boleto emitido para a conta escolhida (só ela)");
        if (b == null)
            return;
        var codigo = Ui.Txt(b["codigo_barras"]);
        var linha = Fiscal.SoDigitos(Ui.Txt(b["linha_digitavel"]));
        u.Ok(codigo.Length == 44 && codigo.All(char.IsDigit), "código de barras com 44 dígitos");
        u.Ok(codigo[..3] == "237" && codigo[3] == '9', "código começa com o banco e a moeda 9");
        var fator = (vencimento - new DateTime(2025, 2, 22)).Days % 9000 + 1000;
        u.Ok(codigo[5..9] == fator.ToString("0000"), $"fator de vencimento {fator} (contagem reiniciada em 22/02/2025)");
        u.Ok(codigo[9..19] == "0000015075", "valor em centavos no código de barras");
        u.Ok(Mod11(codigo[..4] + codigo[5..]) == codigo[4] - '0', "DV geral do código de barras (módulo 11)");
        u.Ok(linha.Length == 47, "linha digitável com 47 dígitos");
        u.Ok(Mod10(linha[..9]) == linha[9] - '0' && Mod10(linha[10..20]) == linha[20] - '0' && Mod10(linha[21..31]) == linha[31] - '0',
            "DVs dos três campos da linha digitável (módulo 10)");
        u.Ok(linha[..4] + linha[32] + linha[33..47] + linha[4..9] + linha[10..20] + linha[21..31] == codigo, "a linha digitável remonta exatamente o código de barras");
        var nosso = Ui.Txt(b["nosso_numero"]);
        u.Ok(nosso.Length == 11 && codigo.Substring(19, 25).Contains(nosso), "nosso número com 11 dígitos dentro do campo livre");

        try
        {
            Cobranca.Emitir(banco, conta);
            u.Ok(false, "segundo boleto para a mesma conta deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("já tem boleto"), "boleto duplicado para a mesma conta é recusado");
        }

        var semCliente = FinanceiroUtil.ContaReceber("Venda avulsa sem cliente", 80m, 15, comCliente: false);
        try
        {
            Cobranca.Emitir(banco, semCliente);
            u.Ok(false, "boleto de conta sem cliente deveria ser recusado (RN-COB-01)");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("cliente"), "boleto de conta sem cliente é recusado (RN-COB-01)");
        }

        // visualizar: abre o BoletoForm com a ficha (linha digitável e aviso de homologação)
        var grade = Usuario.Todos(f).OfType<DataGridView>().First();
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, Convert.ToInt64(b["id"])), "boleto aparece na lista de ativos");
        var texto = Cobranca.Texto(Convert.ToInt64(b["id"]));
        u.Ok(texto.Contains(Ui.Txt(b["linha_digitavel"])) && texto.Contains("HOMOLOGAÇÃO") && texto.Contains("150,75"), "ficha com linha digitável, valor e aviso de homologação");
        var abriu = false;
        u.QuandoAbrir<BoletoForm>(bf => { abriu = true; bf.Close(); });
        u.Clicar(f, "Visualizar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(abriu, "Visualizar / imprimir abre o boleto");
        u.Ok(u.Mensagens.All(m => m.Tipo != "ERRO"), "nenhum erro na emissão pela tela" + string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        f.Close();
    }
}

/// <summary>
/// Operador homologando com o banco: gera a remessa (TESTE_, 240 posições, validada), simula o retorno com pagamento
/// (conta baixada), tenta importar o mesmo retorno de novo, processa um segundo retorno de liquidação e um retorno de outro banco.
/// </summary>
public class CobrancaRemessaERetornoLiquidam : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var conta = FinanceiroUtil.ContaReceber("Mensalidade (boleto)", 89.90m, 10);
        var boleto = Cobranca.Emitir(banco, conta);

        var (remessa, arquivo, conteudo, quantidade) = Cobranca.GerarRemessa(banco);
        var linhas = conteudo.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        u.Ok(arquivo.StartsWith("TESTE_"), "remessa de homologação com prefixo TESTE_");
        u.Ok(quantidade == 1 && linhas.All(l => l.Length == 240), "remessa com o boleto novo e linhas de 240 posições");
        u.Ok(Cobranca.ValidarArquivo240(conteudo, remessa: true).Count == 0, "remessa passa no validador CNAB 240");
        u.Ok(FinanceiroUtil.Status("boletos", boleto) == "ENVIADO", "boleto fica ENVIADO após a remessa");
        try
        {
            Cobranca.GerarRemessa(banco);
            u.Ok(false, "remessa sem nada novo deveria ser recusada");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("Não há boletos"), "remessa vazia é recusada com explicação");
        }

        var retorno = Cobranca.SimularRetorno(remessa, liquidar: true);
        var r = Cobranca.ProcessarRetorno(banco, "RET1.ret", retorno);
        u.Ok(r.Confirmados == 1 && r.Liquidados == 1 && r.ValorLiquidado == 89.90m, "retorno confirma e liquida o boleto");
        var c = Db.Query("SELECT status, valor_pago, forma_pagamento FROM contas WHERE id = @p0", conta).Rows[0];
        u.Ok(Ui.Txt(c["status"]) == "PAGA" && Ui.Dec(c["valor_pago"]) == 89.90m && Ui.Txt(c["forma_pagamento"]) == "Boleto", "liquidação baixa a conta a receber");
        u.Ok(FinanceiroUtil.Status("boletos", boleto) == "PAGO", "boleto fica PAGO");

        var retornosAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM retornos"));
        try
        {
            Cobranca.ProcessarRetorno(banco, "RET1-copia.ret", retorno);
            u.Ok(false, "importar o mesmo retorno duas vezes deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("já foi importado"), "o mesmo retorno importado de novo é recusado");
        }
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM retornos")) == retornosAntes, "retorno repetido não é registrado de novo");

        // outro arquivo do banco repetindo a liquidação (ex.: retorno do dia seguinte): não soma de novo
        var outro = Cobranca.SimularRetorno(remessa, liquidar: true).Replace("SIMULADO", "SIMULAD2");
        var r2 = Cobranca.ProcessarRetorno(banco, "RET2.ret", outro);
        u.Ok(r2.Liquidados == 0 && r2.ValorLiquidado == 0 && r2.Mensagens.Any(m => m.Contains("já registrada")),
            "liquidação repetida em outro retorno não é contada de novo");

        var deOutroBanco = string.Join("\r\n", retorno.Split("\r\n").Select(l => l.Length == 240 ? "341" + l[3..] : l));
        try
        {
            Cobranca.ProcessarRetorno(banco, "ITAU.ret", deOutroBanco);
            u.Ok(false, "retorno de outro banco deveria ser recusado");
        }
        catch (InvalidOperationException ex)
        {
            u.Ok(ex.Message.Contains("341") && ex.Message.Contains("237"), "retorno de outro banco é recusado dizendo os dois bancos");
        }

        var quebrado = string.Join("\r\n", linhas.Select((l, i) => i == 2 ? l[..200] : l));
        var erros = Cobranca.ValidarArquivo240(quebrado, remessa: true);
        u.Ok(erros.Any(e => e.Contains("Linha 3") && e.Contains("240")), "validador aponta a linha com tamanho errado");
    }
}

/// <summary>
/// Dono conferindo a inadimplência e o financeiro acertando contas que tinham boleto: cancelar a conta cancela o boleto
/// ainda não enviado; receber no caixa uma conta com boleto registrado pede a baixa ao banco (movimento 02 na remessa,
/// retorno 09 baixa o boleto); se o banco ainda assim liquidar, o retorno avisa da possível duplicidade.
/// </summary>
public class CobrancaContaComBoletoNoFinanceiro : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var contaA = FinanceiroUtil.ContaReceber("Parcela A (boleto)", 300m, 30);
        var contaB = FinanceiroUtil.ContaReceber("Parcela B (boleto)", 450m, 30);
        var boletoB = Cobranca.Emitir(banco, contaB);
        // B vai ao banco e é registrado
        var (remessa1, _, _, _) = Cobranca.GerarRemessa(banco);
        Cobranca.ProcessarRetorno(banco, "CONF.ret", Cobranca.SimularRetorno(remessa1, liquidar: false));
        u.Ok(FinanceiroUtil.Status("boletos", boletoB) == "REGISTRADO", "boleto registrado pelo banco");
        // A: boleto emitido depois da remessa (ainda EMITIDO, não foi ao banco)
        var boletoA2 = Cobranca.Emitir(banco, contaA);

        var f = u.AbrirTela("FinanceiroForm");
        var grade = Usuario.Achar<DataGridView>(f);

        // cancelar a conta A pelo financeiro
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, contaA), "conta A na lista");
        u.Clicar(f, "Cancelar conta");
        u.Ok(u.Viu("CONFIRMAR", "boleto"), "a confirmação do cancelamento avisa que a conta tem boleto");
        u.Ok(FinanceiroUtil.Status("contas", contaA) == "CANCELADA", "conta A cancelada");
        u.Ok(FinanceiroUtil.Status("boletos", boletoA2) == "CANCELADO", "boleto ainda não enviado da conta cancelada é cancelado junto");

        // receber a conta B no caixa (dinheiro): pede a baixa do boleto registrado
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, contaB), "conta B na lista");
        u.QuandoAbrir<Dialogo>(d =>
        {
            u.Ok(Usuario.Todos(d).OfType<Label>().Any(l => l.Text.Contains("boleto")), "a janela de baixa avisa que a conta tem boleto em aberto");
            Usuario.Achar<ComboBox>(d).SelectedItem = "Dinheiro";
            u.Clicar(d, "Confirmar");
        });
        u.Clicar(f, "Baixar");
        Usuario.Esperar(300);
        u.ConferirJanelas();
        u.Ok(FinanceiroUtil.Status("contas", contaB) == "PAGA", "conta B recebida no caixa");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT baixa_solicitada FROM boletos WHERE id = @p0", boletoB)) == 1, "boleto registrado da conta B fica com baixa pedida");
        u.Ok(u.Viu("INFO", "pedido de baixa"), "o usuário é avisado de que a baixa vai na próxima remessa");

        // se o banco ainda liquidar o boleto B (cliente pagou o boleto também), o retorno alerta a duplicidade
        var duplicado = Cobranca.ProcessarRetorno(banco, "PAGO.ret", Cobranca.SimularRetorno(remessa1, liquidar: true));
        u.Ok(duplicado.Mensagens.Any(m => m.Contains("duplicidade")), "retorno que liquida conta já recebida no caixa avisa possível duplicidade");
        u.Ok(Ui.Txt(Db.Scalar("SELECT forma_pagamento FROM contas WHERE id = @p0", contaB)) == "Dinheiro", "a baixa do caixa não é sobrescrita pelo retorno");
        f.Close();
    }
}

/// <summary>Pedido de baixa: a remessa leva o movimento 02 e o retorno simulado devolve 09, deixando o boleto BAIXADO (RN-COB-07/08/09).</summary>
public class CobrancaPedidoDeBaixaNoBanco : ICenario
{
    public string Area => "Cobranca";

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var conta = FinanceiroUtil.ContaReceber("Duplicata (baixa)", 120m, 25);
        var boleto = Cobranca.Emitir(banco, conta);
        var (remessa1, _, _, _) = Cobranca.GerarRemessa(banco);
        Cobranca.ProcessarRetorno(banco, "CONF.ret", Cobranca.SimularRetorno(remessa1, liquidar: false));

        var f = u.AbrirTela("CobrancaForm");
        var grade = Usuario.Todos(f).OfType<DataGridView>().First();
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, boleto), "boleto registrado na lista");
        u.Clicar(f, "Cancelar / pedir baixa");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT baixa_solicitada FROM boletos WHERE id = @p0", boleto)) == 1, "boleto registrado: cancelar pede a baixa");
        f.Close();

        var (remessa2, _, conteudo, _) = Cobranca.GerarRemessa(banco);
        var p = conteudo.Split("\r\n").First(l => l.Length == 240 && l[13] == 'P');
        u.Ok(p.Substring(15, 2) == "02", "remessa leva o pedido de baixa (movimento 02)");
        var r = Cobranca.ProcessarRetorno(banco, "BAIXA.ret", Cobranca.SimularRetorno(remessa2, liquidar: false));
        u.Ok(FinanceiroUtil.Status("boletos", boleto) == "BAIXADO", "retorno do pedido de baixa deixa o boleto BAIXADO");
        u.Ok(r.Liquidados == 0 && FinanceiroUtil.Status("contas", conta) == "ABERTA", "baixa do boleto não mexe na conta a receber");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM boletos WHERE id = @p0 AND baixa_solicitada = 0", boleto)) == 1, "pedido de baixa atendido");
    }
}

/// <summary>Operador cadastrando a conta bancária: validações do modelo do campo livre e dos códigos usados na remessa (RN-COB-06).</summary>
public class CobrancaCadastroContaBancaria : ICenario
{
    public string Area => "Cobranca";

    static Control Editor(Form f, string rotulo)
    {
        var lbl = Usuario.Todos(f).OfType<Label>().First(l => l.Text.StartsWith(rotulo, StringComparison.OrdinalIgnoreCase));
        var irmaos = lbl.Parent!.Controls;
        return irmaos[irmaos.IndexOf(lbl) + 1];
    }

    public void Executar(Usuario u)
    {
        var banco = FinanceiroUtil.ContaBancariaDemo();
        var f = u.AbrirTela("cadastro_contas_bancarias");
        var grade = Usuario.Achar<DataGridView>(f);
        u.Ok(FinanceiroUtil.SelecionarLinha(grade, banco), "conta de demonstração na lista");
        Usuario.Esperar();

        void TentarSalvar(string rotulo, Action<Control> alterar, string esperado, string descricao)
        {
            var ed = Editor(f, rotulo);
            var original = ed is NumericUpDown n ? (object)n.Value : ed.Text;
            alterar(ed);
            var antes = u.Mensagens.Count;
            u.Clicar(f, "Salvar");
            u.Ok(u.Mensagens.Skip(antes).Any(m => m.Tipo == "ERRO" && m.Texto.Contains(esperado, StringComparison.OrdinalIgnoreCase)),
                descricao + string.Concat(u.Mensagens.Skip(antes).Select(m => " | " + m.Texto)));
            if (ed is NumericUpDown n2) n2.Value = (decimal)original; else ed.Text = (string)original;
        }

        var modeloOriginal = Ui.Txt(Db.Scalar("SELECT campo_livre_modelo FROM contas_bancarias WHERE id = @p0", banco));
        TentarSalvar("Modelo do campo livre", ed => ((TextBox)ed).Text = "{agencia:4}{carteira:2}{conta:7}{zeros:12}", "nosso número",
            "modelo sem o nosso número é recusado");
        TentarSalvar("Espécie do título", ed => ((TextBox)ed).Text = "DM", "Espécie", "espécie com letras (DM) é recusada: a remessa exige código numérico");
        TentarSalvar("Dígitos do nosso número", ed => ((NumericUpDown)ed).Value = 15, "reserva só 11", "nosso número maior que o espaço do modelo é recusado");
        TentarSalvar("Último nosso número", ed => ((NumericUpDown)ed).Value = 0, "não pode ser menor", "voltar o último nosso número abaixo dos já emitidos é recusado");
        TentarSalvar("Dias para protesto", ed => ((NumericUpDown)ed).Value = 150, "protesto", "dias de protesto acima de 99 é recusado");
        u.Ok(Ui.Txt(Db.Scalar("SELECT campo_livre_modelo FROM contas_bancarias WHERE id = @p0", banco)) == modeloOriginal
             && Ui.Txt(Db.Scalar("SELECT especie_titulo FROM contas_bancarias WHERE id = @p0", banco)) == "02", "nada inválido foi gravado");

        // edição válida continua funcionando (passa para produção e volta)
        FinanceiroUtil.SelecionarLinha(grade, banco);
        Usuario.Esperar();
        ((ComboBox)Editor(f, "Ambiente")).SelectedItem = "PRODUCAO";
        var erros = u.Mensagens.Count(m => m.Tipo == "ERRO");
        u.Clicar(f, "Salvar");
        u.Ok(u.Mensagens.Count(m => m.Tipo == "ERRO") == erros && Ui.Txt(Db.Scalar("SELECT ambiente FROM contas_bancarias WHERE id = @p0", banco)) == "PRODUCAO",
            "conta válida salva normalmente (ambiente PRODUCAO)" + string.Concat(u.Mensagens.Skip(0).Where(m => m.Tipo == "ERRO").Select(m => ": " + m.Texto)));
        f.Close();

        // boletos de teste só em homologação; com modelo inválido gravado no banco, explica e não cria contas órfãs
        var c = u.AbrirTela("CobrancaForm");
        Usuario.Achar<TabControl>(c).SelectedIndex = 2; // aba Homologação
        Usuario.Esperar();
        u.Clicar(c, "1. Gerar boletos de teste");
        u.Ok(u.Viu("ERRO", "HOMOLOGACAO"), "boletos de teste recusados em conta de produção");
        Db.Exec("UPDATE contas_bancarias SET ambiente = 'HOMOLOGACAO', campo_livre_modelo = '{agencia:4}{zeros:21}' WHERE id = @p0", banco);
        var contasAntes = Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM contas"));
        u.Clicar(c, "1. Gerar boletos de teste");
        u.Ok(u.Viu("ERRO", "está com o modelo do campo livre inválido"), "modelo inválido: boletos de teste explicam o problema");
        u.Ok(Convert.ToInt64(Db.Scalar("SELECT COUNT(*) FROM contas")) == contasAntes, "nenhuma conta de teste fica órfã sem boleto");
        c.Close();
    }
}
