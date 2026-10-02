using System.Security.Cryptography;
using System.Text;

namespace ErpLoja.Data;

public enum ModalidadeTef { Debito, Credito, Pix }

/// <summary>Resultado de uma transação TEF (o comprovante segue o modelo das adquirentes).</summary>
public record ResultadoTef(bool Aprovada, string Mensagem, string? Nsu = null, string? Autorizacao = null, string? Bandeira = null,
    string ComprovanteCliente = "", string ComprovanteLoja = "")
{
    public long TransacaoId { get; init; }
}

/// <summary>Provedor de TEF (cliente instalado na máquina que conversa com a adquirente).</summary>
public interface IProvedorTef
{
    string Nome { get; }
    /// <summary>Executa o pagamento (o provedor conduz a interação com o operador e o pinpad).</summary>
    ResultadoTef Pagar(IWin32Window dono, decimal valor, ModalidadeTef modalidade, int parcelas, string documento);
    /// <summary>Cancela uma transação já confirmada (estorno no mesmo dia, em geral).</summary>
    ResultadoTef Cancelar(IWin32Window dono, string nsu, decimal valor, DateTime data);
}

/// <summary>Provedores reais ainda não homologados: explicam o que falta em vez de simular.</summary>
public class ProvedorTefNaoInstalado(string nome, string requisito) : IProvedorTef
{
    public string Nome => nome;
    ResultadoTef Falha() => new(false, $"TEF {nome} não disponível: {requisito} Use o SIMULADOR em Configurações > TEF ou receba na maquininha.");
    public ResultadoTef Pagar(IWin32Window dono, decimal valor, ModalidadeTef modalidade, int parcelas, string documento) => Falha();
    public ResultadoTef Cancelar(IWin32Window dono, string nsu, decimal valor, DateTime data) => Falha();
}

/// <summary>
/// Orquestra o TEF com o fluxo padrão de duas fases: a transação aprovada fica PENDENTE até a venda ser gravada;
/// então é CONFIRMADA (venda gravada) ou DESFEITA (falha ao gravar). Toda transação fica registrada em transacoes_tef.
/// </summary>
public static class Tef
{
    public static IProvedorTef Provedor() => Parametros.TefProvedor switch
    {
        "SITEF" => new ProvedorTefNaoInstalado("SiTef", "a integração exige a CliSiTef (Software Express) instalada e homologada para esta automação."),
        "PAYGO" => new ProvedorTefNaoInstalado("PayGo", "a integração exige o PayGo Integrado instalado e credenciado."),
        _ => new TefSimulador(),
    };

    public static string Rotulo(ModalidadeTef m) => m switch
    {
        ModalidadeTef.Debito => "DEBITO",
        ModalidadeTef.Credito => "CREDITO",
        _ => "PIX",
    };

    public static ResultadoTef Pagar(IWin32Window dono, decimal valor, ModalidadeTef modalidade, int parcelas)
    {
        var provedor = Provedor();
        var r = provedor.Pagar(dono, valor, modalidade, parcelas, DateTime.Now.ToString("HHmmss"));
        var id = Db.Scalar("""
            INSERT INTO transacoes_tef(data, provedor, modalidade, bandeira, parcelas, valor, nsu, autorizacao, status, mensagem,
                                       comprovante_cliente, comprovante_loja)
            VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11) RETURNING id
            """, DateTime.Now, provedor.Nome, Rotulo(modalidade), r.Bandeira, parcelas, valor, r.Nsu, r.Autorizacao,
            r.Aprovada ? "PENDENTE" : "NEGADA", r.Mensagem, r.ComprovanteCliente, r.ComprovanteLoja);
        return r with { TransacaoId = Convert.ToInt64(id) };
    }

    public static void Confirmar(long transacaoId, long vendaId) =>
        Db.Exec("UPDATE transacoes_tef SET status = 'CONFIRMADA', venda_id = @p0 WHERE id = @p1 AND status = 'PENDENTE'", vendaId, transacaoId);

    public static void Desfazer(long transacaoId) =>
        Db.Exec("UPDATE transacoes_tef SET status = 'DESFEITA' WHERE id = @p0 AND status = 'PENDENTE'", transacaoId);

    /// <summary>Estorna as transações confirmadas de uma venda (usado ao cancelar a venda). Devolve as mensagens.</summary>
    public static List<string> CancelarDaVenda(IWin32Window dono, long vendaId)
    {
        var mensagens = new List<string>();
        foreach (DataRow t in Db.Query("SELECT * FROM transacoes_tef WHERE venda_id = @p0 AND status = 'CONFIRMADA'", vendaId).Rows)
        {
            var r = Provedor().Cancelar(dono, Ui.Txt(t["nsu"]), Ui.Dec(t["valor"]), Db.LerData(t["data"]));
            if (r.Aprovada)
                Db.Exec("UPDATE transacoes_tef SET status = 'CANCELADA', mensagem = @p0 WHERE id = @p1", r.Mensagem, t["id"]);
            mensagens.Add($"{t["modalidade"]} NSU {t["nsu"]}: {r.Mensagem}");
        }
        return mensagens;
    }

    /// <summary>Comprovante no formato usual das adquirentes.</summary>
    public static string Comprovante(string via, string bandeira, ModalidadeTef m, int parcelas, decimal valor, string nsu, string autorizacao)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[C][N]{bandeira}");
        sb.AppendLine($"[C]{via}");
        sb.AppendLine($"[C]{Db.Config("nome_loja")}");
        sb.AppendLine($"CNPJ {Db.Config("cnpj")}");
        sb.AppendLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}   CAIXA {Parametros.CaixaNumero}");
        sb.AppendLine($"{(m == ModalidadeTef.Credito ? (parcelas > 1 ? $"CREDITO PARCELADO LOJA {parcelas}X" : "CREDITO A VISTA") : m == ModalidadeTef.Debito ? "DEBITO A VISTA" : "PIX")}");
        sb.AppendLine($"[N]VALOR: R$ {valor:N2}");
        sb.AppendLine($"NSU: {nsu}   AUT: {autorizacao}");
        if (Parametros.TefProvedor == "SIMULADOR")
            sb.AppendLine("[C]*** TRANSACAO SIMULADA - SEM VALOR ***");
        return sb.ToString();
    }

    internal static string NovoNsu() => RandomNumberGenerator.GetInt32(100_000_000, 999_999_999).ToString();
    internal static string NovaAutorizacao() => RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
}
