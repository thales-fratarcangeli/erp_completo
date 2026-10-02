using System.Drawing.Printing;
using System.Text;

namespace ErpLoja.Forms;

/// <summary>Comprovante de venda (não fiscal) com opção de impressão.</summary>
public class CupomForm : Form
{
    static int Largura => Parametros.ImpressoraColunas;
    readonly string texto;
    readonly bool cupom;
    readonly float tamanhoImpressao;

    CupomForm(string titulo, string texto, int largura, float tamanhoImpressao = 9F, bool cupom = false)
    {
        this.texto = texto;
        this.cupom = cupom;
        this.tamanhoImpressao = tamanhoImpressao;
        Ui.Estilizar(this, titulo, largura, 700, nameof(CupomForm));
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var caixa = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10F),
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Text = Impressao.SemMarcas(texto),
        };
        var barra = Ui.Barra(DockStyle.Bottom);
        barra.Controls.AddRange([Ui.Botao("Imprimir", (_, _) => Imprimir(), primario: true), Ui.Botao("Fechar", (_, _) => Close())]);
        Controls.Add(caixa);
        Controls.Add(barra);
    }

    void Imprimir()
    {
        try
        {
            if (cupom)
                Impressao.ImprimirCupom(texto);
            else
                Impressao.ImprimirDocumento(texto, texto.Split('\n').Max(l => l.TrimEnd('\r').Length));
            Ui.Feito("Enviado para a impressora.");
        }
        catch (Exception ex)
        {
            Ui.Erro("Não foi possível imprimir:\n" + ex.Message);
        }
    }

    public static void Mostrar(IWin32Window dono, long vendaId)
    {
        using var f = new CupomForm("Comprovante de venda", Gerar(vendaId), 520, cupom: true);
        f.ShowDialog(dono);
    }

    /// <summary>Visualiza e imprime qualquer documento em texto (DANFE, orçamento, pedido).</summary>
    public static void Visualizar(IWin32Window dono, string titulo, string texto, int colunas)
    {
        var largura = colunas > 80 ? 1000 : colunas > 42 ? 820 : 480;
        using var f = new CupomForm(titulo, texto, largura, colunas > 80 ? 7.5F : 9F);
        f.ShowDialog(dono);
    }

    /// <summary>Comprovante não fiscal da venda (texto com marcações de impressão).</summary>
    public static string Gerar(long id)
    {
        var v = Db.Query("""
            SELECT v.*, COALESCE(c.nome, 'Consumidor final') AS cliente, c.cpf_cnpj, u.nome AS atendente
            FROM vendas v LEFT JOIN pessoas c ON c.id = v.cliente_id LEFT JOIN usuarios u ON u.id = v.usuario_id
            WHERE v.id = @p0
            """, id).Rows[0];
        var itens = Db.Query("""
            SELECT p.codigo, p.nome, p.unidade, i.quantidade, i.preco_unitario, i.total
            FROM venda_itens i JOIN produtos p ON p.id = i.produto_id
            WHERE i.venda_id = @p0 ORDER BY i.id
            """, id);
        var parcelas = Db.Query("SELECT vencimento, valor FROM contas WHERE venda_id = @p0 AND status <> 'CANCELADA' AND data_pagamento IS NULL ORDER BY vencimento", id);

        var sb = new StringBuilder();
        var traco = new string('-', Largura);
        void Centro(string s) => sb.AppendLine("[C]" + s);
        void Lados(string a, string b) => sb.AppendLine(a + new string(' ', Math.Max(1, Largura - a.Length - b.Length)) + b);
        static string Cortar(string s, int n) => s.Length <= n ? s : s[..n];

        sb.AppendLine("[C][N]" + Db.Config("nome_loja").ToUpper());
        foreach (var chave in new[] { "cnpj", "endereco", "telefone" })
            if (Db.Config(chave) is { Length: > 0 } valor)
                Centro(chave == "cnpj" ? "CNPJ: " + valor : valor);
        sb.AppendLine(traco);
        Centro("COMPROVANTE DE VENDA - NÃO FISCAL");
        Lados($"Venda nº {id}", Db.LerData(v["data"]).ToString("dd/MM/yyyy HH:mm"));
        sb.AppendLine("Cliente: " + Cortar(Ui.Txt(v["cliente"]), Largura - 9));
        if (Ui.Txt(v["cpf_cnpj"]) is { Length: > 0 } doc)
            sb.AppendLine("CPF/CNPJ: " + doc);
        if (Ui.Txt(v["status"]) == "CANCELADA")
            Centro("*** VENDA CANCELADA ***");
        sb.AppendLine(traco);

        foreach (DataRow r in itens.Rows)
        {
            sb.AppendLine(Cortar($"{r["codigo"]} {r["nome"]}", Largura));
            Lados($"   {Ui.Dec(r["quantidade"]):N2} {r["unidade"]} x {Ui.Dec(r["preco_unitario"]):N2}", Ui.Dec(r["total"]).ToString("N2"));
        }

        sb.AppendLine(traco);
        Lados("Subtotal", Ui.Dec(v["subtotal"]).ToString("N2"));
        if (Ui.Dec(v["desconto"]) > 0)
            Lados("Desconto", "-" + Ui.Dec(v["desconto"]).ToString("N2"));
        sb.Append("[N]");
        Lados("TOTAL R$", Ui.Dec(v["total"]).ToString("N2"));
        Lados("Pagamento", Ui.Txt(v["forma_pagamento"]));
        foreach (DataRow r in parcelas.Rows)
            Lados("  Venc. " + Db.LerData(r["vencimento"]).ToString("dd/MM/yyyy"), Ui.Dec(r["valor"]).ToString("N2"));
        sb.AppendLine(traco);
        sb.AppendLine("Atendente: " + Ui.Txt(v["atendente"]));
        Centro("Obrigado pela preferência!");
        return sb.ToString();
    }
}
