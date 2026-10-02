using System.Globalization;
using System.Text;

namespace ErpLoja.UI;

/// <summary>Área do sistema, com a cor que identifica as telas dela (faixa superior das janelas e menu).</summary>
public record Modulo(string Nome, Color Cor);

/// <summary>
/// Uma entrada de navegação. Chave = chave do tópico de ajuda da tela (a mesma passada em Ui.Estilizar),
/// o que liga o menu, o localizador de telas, a cor do módulo e a ajuda.
/// </summary>
public record Tela(string Chave, string Titulo, string Modulo, string Palavras, Func<Form>? Criar = null,
    Action<IWin32Window>? Acao = null, Keys Atalho = Keys.None, bool SoAdmin = false);

/// <summary>Fonte única da navegação: o menu lateral e o localizador de telas são montados a partir desta lista.</summary>
public static class Navegacao
{
    public static readonly Modulo[] Modulos =
    [
        new("Vendas", ColorTranslator.FromHtml("#2563eb")),
        new("Compras", ColorTranslator.FromHtml("#ea580c")),
        new("Produção", ColorTranslator.FromHtml("#0d9488")),
        new("Logística", ColorTranslator.FromHtml("#7c3aed")),
        new("Estoque", ColorTranslator.FromHtml("#0891b2")),
        new("Financeiro", ColorTranslator.FromHtml("#16a34a")),
        new("Fiscal", ColorTranslator.FromHtml("#ca8a04")),
        new("Gestão", ColorTranslator.FromHtml("#db2777")),
        new("Cadastros", ColorTranslator.FromHtml("#475569")),
        new("Sistema", ColorTranslator.FromHtml("#64748b")),
    ];

    /// <summary>Ação para abrir uma tela: o MainForm registra aqui o seu "Abrir" (evita janelas duplicadas).</summary>
    public static Action<Tela>? Abridor { get; set; }

    public static List<Tela> Telas() =>
    [
        new("PdvForm", "PDV - frente de caixa", "Vendas", "caixa venda balcão cupom cartão pix dinheiro troco tef frente checkout",
            () => new PdvForm(), Atalho: Keys.F2),
        new("pedidos_ORCAMENTO", "Orçamentos", "Vendas", "proposta cotação preço cliente aprovar validade",
            () => new PedidosForm(Pedidos.Orcamento), Atalho: Keys.Control | Keys.O),
        new("pedidos_VENDA", "Pedidos de venda", "Vendas", "pedido faturar faturamento entrega cliente encomenda",
            () => new PedidosForm(Pedidos.Venda), Atalho: Keys.Control | Keys.P),
        new("vendas", "Vendas realizadas", "Vendas", "histórico vendas cancelar venda comprovante reimprimir estorno",
            () => new DocumentosForm(venda: true)),
        new("pedidos_COMPRA", "Pedidos de compra", "Compras", "comprar fornecedor receber mercadoria entrada reposição",
            () => new PedidosForm(Pedidos.Compra), Atalho: Keys.F4),
        new("ImportarXmlForm", "Importar XML de NF-e", "Compras", "xml nota fiscal fornecedor entrada importar nfe danfe",
            Acao: dono => ImportarXmlForm.Executar(dono)),
        new("compras", "Compras realizadas", "Compras", "histórico compras cancelar compra entrada",
            () => new DocumentosForm(venda: false)),
        new("FichasTecnicasForm", "Fichas técnicas", "Produção", "receita composição insumos matéria-prima rendimento custo produção bom estrutura",
            () => new FichasTecnicasForm()),
        new("OrdensProducaoForm", "Ordens de produção", "Produção", "op ordem fabricar produzir produção insumos consumo apontamento",
            () => new OrdensProducaoForm()),
        new("FretesForm", "Fretes e CIOT", "Logística", "frete transporte viagem ciot pamcard antt motorista carga entrega transportador",
            () => new FretesForm()),
        new("cadastro_veiculos", "Veículos", "Logística", "veículo caminhão placa frota renavam carreta",
            Cadastros.Veiculos),
        new("EstoqueForm", "Estoque", "Estoque", "saldo inventário ajuste movimentação reposição mínimo custo médio",
            () => new EstoqueForm(), Atalho: Keys.F6),
        new("FinanceiroForm", "Contas a pagar e receber", "Financeiro", "financeiro contas pagar receber baixa vencidas fluxo caixa",
            () => new FinanceiroForm(), Atalho: Keys.F7),
        new("CobrancaForm", "Cobrança bancária (boletos)", "Financeiro", "boleto banco remessa retorno cnab homologação bancária cobrança",
            () => new CobrancaForm()),
        new("cadastro_contas_bancarias", "Contas bancárias", "Financeiro", "banco agência conta carteira convênio cedente beneficiário",
            Cadastros.ContasBancarias, SoAdmin: true),
        new("NotasForm", "Notas fiscais", "Fiscal", "nota fiscal nfe nfce danfe emitir sefaz cfop imposto ibs cbs reforma",
            () => new NotasForm(), Atalho: Keys.Control | Keys.N),
        new("DashboardForm", "Painel gerencial", "Gestão", "dashboard indicadores gráficos faturamento lucro margem ticket desempenho",
            () => new DashboardForm(), Atalho: Keys.F5),
        new("RelatoriosForm", "Relatórios", "Gestão", "relatório lucro faturamento fluxo caixa exportar csv excel",
            () => new RelatoriosForm(), Atalho: Keys.F8),
        new("cadastro_pessoas", "Clientes e fornecedores", "Cadastros", "cliente fornecedor transportador motorista pessoa cpf cnpj contato parceiro",
            Cadastros.Pessoas),
        new("cadastro_produtos", "Produtos", "Cadastros", "produto mercadoria item preço código barras ncm insumo",
            Cadastros.Produtos),
        new("cadastro_categorias", "Categorias", "Cadastros", "categoria grupo família departamento",
            Cadastros.Categorias),
        new("cadastro_usuarios", "Usuários", "Cadastros", "usuário senha acesso permissão administrador login",
            Cadastros.Usuarios, SoAdmin: true),
        new("ConfiguracoesForm", "Configurações", "Sistema", "parâmetros configuração empresa impressora tef ciot e-mail backup banco senha",
            () => new ConfiguracoesForm()),
        new("LogForm", "Log de atividades", "Sistema", "log auditoria histórico atividades quem fez rastreio registro usuário ações",
            () => new LogForm(), SoAdmin: true),
        new("LocalizarTelaForm", "Localizar tela", "Sistema", "buscar procurar tela menu achar pesquisar",
            Acao: dono => LocalizarTelaForm.Executar(dono), Atalho: Keys.Control | Keys.K),
    ];

    public static Modulo ModuloDe(string nome) => Modulos.FirstOrDefault(m => m.Nome == nome) ?? Modulos[^1];

    /// <summary>Cor do módulo de uma tela, pela chave de ajuda (Ui.Estilizar usa para a faixa da janela).</summary>
    public static Color CorDaTela(string chave) =>
        Telas().FirstOrDefault(t => t.Chave == chave) is { } t ? ModuloDe(t.Modulo).Cor : Ui.Primaria;

    public static string Normalizar(string s)
    {
        var n = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(n.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    /// <summary>
    /// Busca por palavra-chave: cada termo precisa aparecer no título, nas palavras-chave, no módulo ou no texto de ajuda da tela.
    /// Pontuação: título (10, ou 15 se começar com o termo) &gt; palavras-chave (6) &gt; módulo (3) &gt; ajuda (1).
    /// </summary>
    public static List<(Tela Tela, int Pontos, string Trecho)> Buscar(string termo, bool admin)
    {
        var termos = Normalizar(termo).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var resultado = new List<(Tela, int, string)>();
        foreach (var t in Telas().Where(t => admin || !t.SoAdmin))
        {
            var ajuda = Ajuda.Topicos.FirstOrDefault(a => a.Chave == t.Chave)?.Texto ?? "";
            string titulo = Normalizar(t.Titulo), palavras = Normalizar(t.Palavras), modulo = Normalizar(t.Modulo), texto = Normalizar(ajuda);
            int pontos = 0;
            var todos = true;
            foreach (var x in termos)
            {
                var p = (titulo.StartsWith(x) ? 15 : titulo.Contains(x) ? 10 : 0) + (palavras.Contains(x) ? 6 : 0) +
                        (modulo.Contains(x) ? 3 : 0) + (texto.Contains(x) ? 1 : 0);
                if (p == 0)
                {
                    todos = false;
                    break;
                }
                pontos += p;
            }
            if (!todos)
                continue;
            var trecho = "";
            if (termos.Length > 0 && texto.IndexOf(termos[0], StringComparison.Ordinal) is var i and >= 0)
            {
                var inicio = Math.Max(0, i - 40);
                trecho = (inicio > 0 ? "…" : "") + ajuda.Substring(inicio, Math.Min(110, ajuda.Length - inicio)).Replace('\n', ' ').Replace("\r", "").Trim() + "…";
            }
            resultado.Add((t, pontos, trecho));
        }
        return resultado.OrderByDescending(r => r.Item2).ThenBy(r => r.Item1.Titulo).ToList();
    }
}
