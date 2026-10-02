using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Funcionário novo procura do jeito que fala ("como cadastrar cliente", "vender", "trocar minha senha").</summary>
public class SistemaFuncionarioBuscaComoFala : ICenario
{
    public string Area => "Sistema";
    public bool ComDadosDemo => false;

    public void Executar(Usuario u)
    {
        var perguntas = new (string Busca, string Chave)[]
        {
            ("vender", "PdvForm"),
            ("como cadastrar cliente", "cadastro_pessoas"),
            ("cadastrar produto", "cadastro_produtos"),
            ("onde fica o estoque", "EstoqueForm"),
            ("trocar minha senha", "ConfiguracoesForm"),
            ("fazer backup", "ConfiguracoesForm"),
            ("emitir nota", "NotasForm"),
            ("pagar fornecedor", "FinanceiroForm"),
            ("cancelar uma venda", "vendas"),
            ("imprimir boleto", "CobrancaForm"),
            ("fabricar", "OrdensProducaoForm"),
        };
        foreach (var (busca, chave) in perguntas)
        {
            var r = Navegacao.Buscar(busca, admin: true);
            var pos = r.FindIndex(x => x.Tela.Chave == chave);
            u.Ok(pos is >= 0 and < 3, $"buscar '{busca}' traz '{SistemaUtil.Tela(chave).Titulo}' entre os 3 primeiros (posição {pos + 1} de {r.Count}: "
                + string.Join(", ", r.Take(3).Select(x => x.Tela.Titulo)) + ")");
        }
        u.Ok(Navegacao.Buscar("de", admin: true).Count > 0, "buscar só uma palavra comum ('de') ainda devolve resultados");
    }
}
