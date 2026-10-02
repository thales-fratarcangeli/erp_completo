namespace ErpLoja.Data;

/// <summary>
/// Gera dados fictícios coerentes para demonstração e testes (semente fixa: o resultado é reproduzível).
/// Usa as próprias regras do sistema (vendas, compras, produção, CIOT simulado, boletos, notas) e depois ajusta as datas
/// para espalhar o movimento pelos últimos 6 meses. Não altera os dados da empresa se já estiverem preenchidos.
/// </summary>
public static class DadosDemo
{
    static readonly string[] Nomes = ["Ana", "Bruno", "Carla", "Diego", "Eduarda", "Felipe", "Gabriela", "Henrique", "Isabela", "João",
        "Karina", "Lucas", "Mariana", "Nicolas", "Olívia", "Paulo", "Quitéria", "Rafael", "Sofia", "Thiago", "Ursula", "Vinícius", "Yasmin", "Zeca"];
    static readonly string[] Sobrenomes = ["Silva", "Souza", "Oliveira", "Santos", "Pereira", "Lima", "Carvalho", "Ferreira", "Rodrigues",
        "Almeida", "Costa", "Gomes", "Martins", "Araújo", "Ribeiro", "Barbosa", "Rocha", "Dias"];
    static readonly string[] Ruas = ["Rua das Flores", "Av. Brasil", "Rua XV de Novembro", "Rua São José", "Av. Paulista", "Rua Sete de Setembro", "Rua da Paz"];
    static readonly (string Cidade, string Uf)[] Cidades = [("Campinas", "SP"), ("Campinas", "SP"), ("Campinas", "SP"), ("Valinhos", "SP"),
        ("Sumaré", "SP"), ("Jundiaí", "SP"), ("Belo Horizonte", "MG"), ("Curitiba", "PR")];

    // código, nome, categoria, unidade, custo, margem %, ncm, tipo
    static readonly (string Nome, string Categoria, string Un, decimal Custo, decimal Margem, string Ncm, string Tipo)[] Catalogo =
    [
        ("Arroz Tipo 1 5kg", "Mercearia", "UN", 22.90m, 32, "10063021", "MERCADORIA"),
        ("Feijão Carioca 1kg", "Mercearia", "UN", 6.40m, 38, "07133319", "MERCADORIA"),
        ("Macarrão Espaguete 500g", "Mercearia", "UN", 3.20m, 45, "19021900", "MERCADORIA"),
        ("Óleo de Soja 900ml", "Mercearia", "UN", 6.10m, 30, "15079011", "MERCADORIA"),
        ("Café Torrado 500g", "Mercearia", "UN", 15.80m, 35, "09012100", "MERCADORIA"),
        ("Açúcar Refinado 1kg", "Mercearia", "UN", 4.10m, 40, "17019900", "MERCADORIA"),
        ("Molho de Tomate 340g", "Mercearia", "UN", 2.30m, 55, "21032010", "MERCADORIA"),
        ("Biscoito Recheado 130g", "Mercearia", "UN", 2.10m, 60, "19053100", "MERCADORIA"),
        ("Farinha de Mandioca 500g", "Mercearia", "UN", 4.50m, 45, "11062000", "MERCADORIA"),
        ("Leite Condensado 395g", "Mercearia", "UN", 5.20m, 42, "04029900", "MERCADORIA"),
        ("Refrigerante Cola 2L", "Bebidas", "UN", 6.90m, 40, "22021000", "MERCADORIA"),
        ("Refrigerante Guaraná 2L", "Bebidas", "UN", 5.90m, 42, "22021000", "MERCADORIA"),
        ("Água Mineral 1,5L", "Bebidas", "UN", 1.40m, 85, "22011000", "MERCADORIA"),
        ("Suco de Laranja 1L", "Bebidas", "UN", 6.20m, 45, "20091200", "MERCADORIA"),
        ("Cerveja Pilsen Lata 350ml", "Bebidas", "UN", 2.80m, 50, "22030000", "MERCADORIA"),
        ("Energético 250ml", "Bebidas", "UN", 5.10m, 55, "22029900", "MERCADORIA"),
        ("Detergente Neutro 500ml", "Limpeza", "UN", 1.90m, 60, "34022000", "MERCADORIA"),
        ("Sabão em Pó 1kg", "Limpeza", "UN", 11.50m, 35, "34022000", "MERCADORIA"),
        ("Água Sanitária 2L", "Limpeza", "UN", 4.20m, 50, "28289011", "MERCADORIA"),
        ("Esponja Multiuso c/3", "Limpeza", "UN", 3.10m, 70, "39249000", "MERCADORIA"),
        ("Desinfetante Lavanda 2L", "Limpeza", "UN", 6.80m, 45, "38089419", "MERCADORIA"),
        ("Papel Higiênico 12 rolos", "Higiene", "UN", 14.90m, 35, "48181000", "MERCADORIA"),
        ("Sabonete 90g", "Higiene", "UN", 1.60m, 75, "34011190", "MERCADORIA"),
        ("Creme Dental 90g", "Higiene", "UN", 3.40m, 60, "33061000", "MERCADORIA"),
        ("Shampoo 350ml", "Higiene", "UN", 9.80m, 50, "33051000", "MERCADORIA"),
        ("Desodorante Aerosol", "Higiene", "UN", 10.50m, 45, "33072010", "MERCADORIA"),
        ("Banana Prata", "Hortifruti", "KG", 3.90m, 60, "08039000", "MERCADORIA"),
        ("Tomate", "Hortifruti", "KG", 5.50m, 55, "07020000", "MERCADORIA"),
        ("Batata", "Hortifruti", "KG", 4.20m, 55, "07019000", "MERCADORIA"),
        ("Cebola", "Hortifruti", "KG", 3.80m, 60, "07031019", "MERCADORIA"),
        ("Maçã Gala", "Hortifruti", "KG", 7.90m, 50, "08081000", "MERCADORIA"),
        ("Queijo Mussarela Fatiado", "Frios e Laticínios", "KG", 39.00m, 40, "04069090", "MERCADORIA"),
        ("Presunto Fatiado", "Frios e Laticínios", "KG", 29.00m, 45, "16024900", "MERCADORIA"),
        ("Iogurte Natural 170g", "Frios e Laticínios", "UN", 2.40m, 60, "04032000", "MERCADORIA"),
        ("Manteiga 200g", "Frios e Laticínios", "UN", 9.90m, 40, "04051000", "MERCADORIA"),
        ("Pilha AA c/4", "Utilidades", "UN", 9.50m, 60, "85061019", "MERCADORIA"),
        ("Vela Comum c/8", "Utilidades", "UN", 4.10m, 70, "34060000", "MERCADORIA"),
        ("Pão Francês", "Padaria", "KG", 0, 0, "19059090", "PRODUTO_ACABADO"),
        ("Bolo de Chocolate", "Padaria", "UN", 0, 0, "19059090", "PRODUTO_ACABADO"),
        ("Pão de Queijo", "Padaria", "KG", 0, 0, "19059090", "PRODUTO_ACABADO"),
        ("Farinha de Trigo (insumo)", "Padaria", "KG", 3.10m, 0, "11010010", "INSUMO"),
        ("Fermento Biológico (insumo)", "Padaria", "KG", 28.00m, 0, "21021090", "INSUMO"),
        ("Sal Refinado (insumo)", "Padaria", "KG", 1.60m, 0, "25010020", "INSUMO"),
        ("Açúcar (insumo)", "Padaria", "KG", 3.90m, 0, "17019900", "INSUMO"),
        ("Ovos (insumo)", "Padaria", "UN", 0.55m, 0, "04072100", "INSUMO"),
        ("Leite Integral (insumo)", "Padaria", "L", 4.30m, 0, "04012010", "INSUMO"),
        ("Chocolate em Pó (insumo)", "Padaria", "KG", 32.00m, 0, "18061000", "INSUMO"),
        ("Polvilho Azedo (insumo)", "Padaria", "KG", 9.50m, 0, "11081400", "INSUMO"),
        ("Queijo Meia Cura (insumo)", "Padaria", "KG", 42.00m, 0, "04069090", "INSUMO"),
        ("Óleo (insumo)", "Padaria", "L", 6.50m, 0, "15079011", "INSUMO"),
    ];

    static string Dv11(string numeros, int[] pesos)
    {
        var resto = numeros.Select((c, i) => (c - '0') * pesos[i]).Sum() % 11;
        return (resto < 2 ? 0 : 11 - resto).ToString();
    }

    public static string Cpf(Random r)
    {
        var b = string.Concat(Enumerable.Range(0, 9).Select(_ => r.Next(10)));
        b += Dv11(b, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        b += Dv11(b, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return $"{b[..3]}.{b[3..6]}.{b[6..9]}-{b[9..]}";
    }

    public static string Cnpj(Random r)
    {
        var b = string.Concat(Enumerable.Range(0, 8).Select(_ => r.Next(10))) + "0001";
        b += Dv11(b, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        b += Dv11(b, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return $"{b[..2]}.{b[2..5]}.{b[5..8]}/{b[8..12]}-{b[12..]}";
    }

    public static string Ean13(string doze)
    {
        var soma = doze.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return doze + (10 - soma % 10) % 10;
    }

    static string Telefone(Random r) => $"(19) 9{r.Next(1000, 9999)}-{r.Next(1000, 9999)}";

    public static string Gerar()
    {
        string resumo;
        using (Log.Suspender())
            resumo = GerarDados();
        Log.Registrar(Log.Sistema, "Dados de demonstração gerados", resumo);
        return resumo;
    }

    static string GerarDados()
    {
        var r = new Random(2026);
        var hoje = DateTime.Today;
        var inicio = hoje.AddDays(-180);
        int vendas = 0, compras = 0, ops = 0;

        // ---------------- empresa (só se ainda não configurada)
        if (Db.Config("cnpj").Length == 0)
        {
            Db.SalvarConfig("nome_loja", "Mercado Bom Preço Ltda");
            Db.SalvarConfig("cnpj", Cnpj(new Random(1)));
            Db.SalvarConfig("ie", "244555666111");
            Db.SalvarConfig("endereco", "Av. Brasil, 1500 - Centro");
            Db.SalvarConfig("cidade", "Campinas");
            Db.SalvarConfig("uf", "SP");
            Db.SalvarConfig("telefone", "(19) 3232-1000");
            Db.SalvarConfig("rntrc_empresa", "12345678");
        }

        long Pessoa(DbSessao s, string nome, string doc, bool cliente, bool fornecedor, bool transportador = false, bool motorista = false,
            string? rntrc = null, string? cnh = null, decimal limite = 0)
        {
            var (cidade, uf) = Cidades[r.Next(Cidades.Length)];
            return s.Inserir("""
                INSERT INTO pessoas(nome, cpf_cnpj, telefone, email, endereco, cidade, uf, cep, eh_cliente, eh_fornecedor, eh_transportador,
                                    eh_motorista, rntrc, cnh, cnh_categoria, cnh_validade, limite_credito)
                VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16)
                """, nome, doc, Telefone(r), Navegacao.Normalizar(nome.Split(' ')[0]) + r.Next(10, 99) + "@exemplo.com.br",
                $"{Ruas[r.Next(Ruas.Length)]}, {r.Next(10, 2000)}", cidade, uf, $"13{r.Next(100, 999)}-{r.Next(100, 999)}",
                cliente, fornecedor, transportador, motorista, rntrc, cnh, cnh is null ? null : "E",
                cnh is null ? null : hoje.AddYears(3).ToString("dd/MM/yyyy"), limite);
        }

        var clientes = new List<long>();
        var fornecedores = new List<long>();
        var produtos = new Dictionary<string, long>();
        long transportadorTac, transportadorEtc, motorista1, motorista2, veiculo1, veiculo2, veiculo3;
        using (var s = new DbSessao(transacao: true))
        {
            for (int i = 0; i < 32; i++)
                clientes.Add(Pessoa(s, $"{Nomes[r.Next(Nomes.Length)]} {Sobrenomes[r.Next(Sobrenomes.Length)]} {Sobrenomes[r.Next(Sobrenomes.Length)]}",
                    Cpf(r), cliente: true, fornecedor: false, limite: i % 4 == 0 ? 800 : 0));
            foreach (var empresa in new[] { "Restaurante Sabor da Casa Ltda", "Lanchonete Ponto Certo ME", "Escola Pequeno Príncipe" })
                clientes.Add(Pessoa(s, empresa, Cnpj(r), cliente: true, fornecedor: false, limite: 5000));
            foreach (var nome in new[] { "Atacadão Alimentos Ltda", "Distribuidora de Bebidas Gelada Ltda", "Higiene & Limpeza Distribuidora",
                         "Ceasa Hortifruti Campinas", "Laticínios Serra Azul", "Moinho Trigo Dourado S/A", "Utilidades Brasil Comércio" })
                fornecedores.Add(Pessoa(s, nome, Cnpj(r), cliente: false, fornecedor: true));
            fornecedores.Add(Pessoa(s, "Comercial Duas Vias Ltda", Cnpj(r), cliente: true, fornecedor: true));
            transportadorTac = Pessoa(s, "José Aparecido Transportes (autônomo)", Cpf(r), false, false, transportador: true, motorista: true, rntrc: "01234567", cnh: "04512345678");
            transportadorEtc = Pessoa(s, "Rápido Paulista Transportes Ltda", Cnpj(r), false, true, transportador: true, rntrc: "07654321");
            motorista1 = transportadorTac;
            motorista2 = Pessoa(s, "Marcos Vinícius Lopes", Cpf(r), false, false, motorista: true, cnh: "05598765432");

            veiculo1 = s.Inserir("INSERT INTO veiculos(placa, tipo, carroceria, marca_modelo, ano, uf, capacidade_kg, frota_propria) VALUES('FBP1A23', 'VAN', 'BAU', 'Fiat Ducato', 2021, 'SP', 1500, 1)");
            veiculo2 = s.Inserir("INSERT INTO veiculos(placa, tipo, carroceria, marca_modelo, ano, uf, capacidade_kg, frota_propria, proprietario_id, rntrc) VALUES('JTA4B56', 'TRUCK', 'SIDER', 'VW Constellation', 2018, 'SP', 14000, 0, @p0, '01234567')", transportadorTac);
            veiculo3 = s.Inserir("INSERT INTO veiculos(placa, tipo, carroceria, marca_modelo, ano, uf, capacidade_kg, frota_propria, proprietario_id) VALUES('RPT7C89', 'CARRETA', 'BAU', 'Scania R450', 2022, 'SP', 30000, 0, @p0)", transportadorEtc);

            var categorias = new Dictionary<string, long>();
            foreach (var c in Catalogo.Select(c => c.Categoria).Distinct())
                categorias[c] = s.Scalar("SELECT id FROM categorias WHERE nome = @p0", c) as long?
                                ?? s.Inserir("INSERT INTO categorias(nome) VALUES(@p0)", c);
            int seq = 1;
            foreach (var p in Catalogo)
            {
                var codigo = "D" + (seq++).ToString("000");
                var venda = p.Tipo == "INSUMO" ? 0 : Math.Round(p.Custo * (1 + p.Margem / 100), 2);
                produtos[p.Nome] = s.Inserir("""
                    INSERT INTO produtos(codigo, codigo_barras, nome, categoria_id, unidade, preco_custo, preco_venda, estoque_minimo, ncm, tipo,
                                         cst, cst_ibscbs, cclass_trib)
                    VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, '102', '000', '000001')
                    """, codigo, p.Un == "UN" ? Ean13("7891" + r.Next(10_000_000, 99_999_999)) : null, p.Nome, categorias[p.Categoria], p.Un,
                    p.Custo, venda, p.Un == "KG" ? 10 : 12, p.Ncm, p.Tipo);
            }
            s.Exec("UPDATE produtos SET preco_venda = 16.90 WHERE nome = 'Pão Francês'");
            s.Exec("UPDATE produtos SET preco_venda = 32.00 WHERE nome = 'Bolo de Chocolate'");
            s.Exec("UPDATE produtos SET preco_venda = 49.90 WHERE nome = 'Pão de Queijo'");
            s.Commit();
        }

        // ---------------- fichas técnicas
        void Ficha(string produto, decimal rendimento, decimal adicional, params (string Insumo, decimal Qtd, decimal Perda)[] itens)
        {
            var lista = new BindingList<InsumoFicha>();
            foreach (var (insumo, qtd, perda) in itens)
                lista.Add(new InsumoFicha { InsumoId = produtos[insumo], Quantidade = qtd, PerdaPct = perda });
            Producao.SalvarFicha(new Producao.Ficha(null, produtos[produto], rendimento, adicional, "Receita de demonstração.", lista));
        }
        Ficha("Pão Francês", 10, 12, ("Farinha de Trigo (insumo)", 6.5m, 2), ("Fermento Biológico (insumo)", 0.12m, 0), ("Sal Refinado (insumo)", 0.12m, 0),
            ("Açúcar (insumo)", 0.1m, 0), ("Óleo (insumo)", 0.15m, 0));
        Ficha("Bolo de Chocolate", 4, 8, ("Farinha de Trigo (insumo)", 1.2m, 2), ("Açúcar (insumo)", 1m, 0), ("Ovos (insumo)", 12, 5),
            ("Leite Integral (insumo)", 1m, 0), ("Chocolate em Pó (insumo)", 0.4m, 0), ("Óleo (insumo)", 0.3m, 0));
        Ficha("Pão de Queijo", 5, 10, ("Polvilho Azedo (insumo)", 2.5m, 1), ("Queijo Meia Cura (insumo)", 1.2m, 0), ("Ovos (insumo)", 10, 5),
            ("Leite Integral (insumo)", 1m, 0), ("Óleo (insumo)", 0.5m, 0), ("Sal Refinado (insumo)", 0.05m, 0));

        // ---------------- compras: estoque inicial + reposições mensais (backdated)
        void Datar(DbSessao s, string tabela, long id, DateTime quando, string origem)
        {
            s.Exec($"UPDATE {tabela} SET data = @p0 WHERE id = @p1", quando, id);
            s.Exec("UPDATE movimentacoes_estoque SET data = @p0 WHERE origem = @p1", quando, origem);
        }
        var revenda = Catalogo.Where(c => c.Tipo != "PRODUTO_ACABADO").ToList();
        using (var s = new DbSessao(transacao: true))
        {
            for (var mes = 0; mes < 6; mes++)
            {
                var data = inicio.AddDays(mes * 30 + 1).AddHours(9);
                var indice = 0;
                foreach (var grupo in revenda.GroupBy(c => c.Categoria))
                {
                    var fornecedor = fornecedores[indice++ % (fornecedores.Count - 1)];
                    var itens = grupo.Select(c => new ItemDoc(produtos[c.Nome], c.Tipo == "INSUMO" ? r.Next(250, 450) : c.Un == "KG" || c.Un == "L" ? r.Next(40, 120) : r.Next(60, 180),
                        Math.Round(c.Custo * (decimal)(0.95 + r.NextDouble() * 0.1), 2), 0)).ToList();
                    var aPrazo = r.Next(3) > 0;
                    var id = Operacoes.RegistrarCompra(s, fornecedor, (100000 + compras).ToString(), itens, aPrazo ? Operacoes.APrazo : "PIX",
                        aPrazo ? 2 : 1, data.Date.AddDays(28));
                    Datar(s, "compras", id, data, $"Compra nº {id}");
                    s.Exec("UPDATE contas SET data_pagamento = CASE WHEN data_pagamento IS NULL THEN NULL ELSE @p0 END WHERE compra_id = @p1", Db.Dia(data), id);
                    compras++;
                }
            }
            s.Commit();
        }

        // ---------------- produção semanal (backdated)
        for (var semana = 0; semana < 25; semana++)
        {
            var data = inicio.AddDays(semana * 7 + 2).AddHours(5);
            foreach (var (produto, quantidade) in new[] { ("Pão Francês", 40m + r.Next(0, 20)), ("Bolo de Chocolate", 8m + r.Next(0, 6)), ("Pão de Queijo", 10m + r.Next(0, 8)) })
            {
                var op = Producao.CriarOrdem(produtos[produto], quantidade, data, "Produção semanal");
                Producao.Concluir(op, quantidade);
                Db.Exec("UPDATE ordens_producao SET data = @p0, inicio = @p0, conclusao = @p0, previsao = @p1 WHERE id = @p2", data, Db.Dia(data), op);
                Db.Exec("UPDATE movimentacoes_estoque SET data = @p0 WHERE referencia_id = @p1 AND origem LIKE '% OP nº ' || @p1", data, op);
                ops++;
            }
        }
        var planejada = Producao.CriarOrdem(produtos["Pão Francês"], 50, hoje.AddDays(1), "Produção de amanhã");
        var emAndamento = Producao.CriarOrdem(produtos["Bolo de Chocolate"], 12, hoje, "Encomenda de aniversário");
        Producao.Iniciar(emAndamento);
        ops += 2;

        // ---------------- vendas dos últimos 6 meses
        var vendaveis = Catalogo.Where(c => c.Tipo != "INSUMO").Select(c => (c.Nome, Id: produtos[c.Nome], c.Un)).ToList();
        var formas = new[] { "Dinheiro", "PIX", "PIX", "Cartão de débito", "Cartão de débito", "Cartão de crédito", "Cartão de crédito", Operacoes.APrazo };
        using (var s = new DbSessao(transacao: true))
        {
            for (var dia = inicio; dia <= hoje; dia = dia.AddDays(1))
            {
                var movimento = dia.DayOfWeek switch { DayOfWeek.Sunday => 3, DayOfWeek.Saturday => 11, _ => 7 } + r.Next(-2, 4);
                for (int v = 0; v < movimento; v++)
                {
                    var hora = dia.AddHours(r.Next(100) < 30 ? r.Next(7, 10) : r.Next(100) < 60 ? r.Next(16, 20) : r.Next(10, 16)).AddMinutes(r.Next(60));
                    if (hora > DateTime.Now)
                        continue;
                    var itens = new List<ItemDoc>();
                    foreach (var (_, id, un) in vendaveis.OrderBy(_ => r.Next()).Take(r.Next(1, 7)))
                    {
                        var preco = Convert.ToDecimal(s.Scalar("SELECT preco_venda FROM produtos WHERE id = @p0", id));
                        var custo = Convert.ToDecimal(s.Scalar("SELECT preco_custo FROM produtos WHERE id = @p0", id));
                        itens.Add(new ItemDoc(id, un == "KG" ? Math.Round((decimal)(0.2 + r.NextDouble() * 1.8), 3) : r.Next(1, 4), preco, custo));
                    }
                    var forma = formas[r.Next(formas.Length)];
                    long? cliente = forma == Operacoes.APrazo || r.Next(4) == 0 ? clientes[r.Next(clientes.Count)] : null;
                    var subtotal = itens.Sum(i => i.Total);
                    var desconto = r.Next(10) == 0 ? Math.Round(subtotal * 0.05m, 2) : 0;
                    var id2 = Operacoes.RegistrarVenda(s, cliente, itens, desconto, 0, forma, forma == Operacoes.APrazo ? r.Next(1, 4) : 1, hora.Date.AddDays(30), null);
                    Datar(s, "vendas", id2, hora, $"Venda nº {id2}");
                    s.Exec("UPDATE contas SET criado_em = @p0 WHERE venda_id = @p1", hora, id2);
                    if (forma == Operacoes.APrazo)
                    {
                        var n = 0;
                        foreach (DataRow c in s.Query("SELECT id FROM contas WHERE venda_id = @p0 ORDER BY id", id2).Rows)
                            s.Exec("UPDATE contas SET vencimento = @p0 WHERE id = @p1", Db.Dia(hora.Date.AddDays(30 * ++n)), c["id"]);
                    }
                    else
                    {
                        s.Exec("UPDATE contas SET vencimento = @p0, data_pagamento = @p0 WHERE venda_id = @p1", Db.Dia(hora), id2);
                        if (forma != "Dinheiro")
                        {
                            var modalidade = forma == "PIX" ? "PIX" : forma.Contains("débito") ? "DEBITO" : "CREDITO";
                            s.Exec("""
                                INSERT INTO transacoes_tef(data, venda_id, provedor, modalidade, bandeira, parcelas, valor, nsu, autorizacao, status, mensagem)
                                VALUES(@p0, @p1, 'SIMULADOR', @p2, @p3, 1, @p4, @p5, @p6, 'CONFIRMADA', 'Transação aprovada.')
                                """, hora, id2, modalidade, modalidade == "PIX" ? "PIX" : new[] { "VISA", "MASTERCARD", "ELO" }[r.Next(3)],
                                Math.Max(0, subtotal - desconto), Tef.NovoNsu(), Tef.NovaAutorizacao());
                        }
                    }
                    vendas++;
                }
            }
            // contas a receber a prazo vencidas: a maioria foi paga em dia, algumas ficam em atraso
            foreach (DataRow c in s.Query("SELECT id, valor, vencimento FROM contas WHERE tipo = 'RECEBER' AND status = 'ABERTA' AND vencimento < @p0", Db.Dia(hoje)).Rows)
                if (r.Next(10) < 8)
                    s.Exec("UPDATE contas SET status = 'PAGA', data_pagamento = vencimento, valor_pago = valor, forma_pagamento = 'PIX' WHERE id = @p0", c["id"]);
            // contas a pagar vencidas foram pagas
            s.Exec("UPDATE contas SET status = 'PAGA', data_pagamento = vencimento, valor_pago = valor, forma_pagamento = 'Boleto' WHERE tipo = 'PAGAR' AND status = 'ABERTA' AND vencimento < @p0", Db.Dia(hoje.AddDays(-3)));
            s.Commit();
        }

        // ---------------- despesas fixas mensais
        for (var mes = 0; mes < 7; mes++)
        {
            var venc = new DateTime(inicio.AddMonths(mes).Year, inicio.AddMonths(mes).Month, 10);
            foreach (var (descricao, valor) in new[] { ("Aluguel da loja", 4500m), ("Energia elétrica", 980m + r.Next(0, 300)), ("Internet e telefone", 239.90m), ("Contador", 850m) })
                Operacoes.LancarConta("PAGAR", $"{descricao} {venc:MM/yyyy}", valor, 1, venc, null, null);
        }
        Db.Exec("UPDATE contas SET status = 'PAGA', data_pagamento = vencimento, valor_pago = valor, forma_pagamento = 'Transferência' WHERE tipo = 'PAGAR' AND status = 'ABERTA' AND vencimento < @p0 AND venda_id IS NULL AND compra_id IS NULL", Db.Dia(hoje));

        // ---------------- orçamentos e pedidos
        for (int i = 0; i < 6; i++)
        {
            var p = new Pedido { Tipo = i < 4 ? Pedidos.Orcamento : Pedidos.Venda, ClienteId = clientes[r.Next(clientes.Count - 3, clientes.Count)], Validade = hoje.AddDays(r.Next(-5, 15)), Forma = Operacoes.APrazo, Parcelas = 2, Frete = i % 2 == 0 ? 25 : 0 };
            foreach (var (nome, id, _) in vendaveis.OrderBy(_ => r.Next()).Take(r.Next(3, 8)))
                p.Itens.Add(new PedidoItem { ProdutoId = id, Quantidade = r.Next(5, 30), Unitario = Convert.ToDecimal(Db.Scalar("SELECT preco_venda FROM produtos WHERE id = @p0", id)) });
            Pedidos.Salvar(p);
        }
        var pc = new Pedido { Tipo = Pedidos.Compra, FornecedorId = fornecedores[0], Forma = Operacoes.APrazo, Parcelas = 2, Frete = 120 };
        foreach (var nome in new[] { "Arroz Tipo 1 5kg", "Feijão Carioca 1kg", "Óleo de Soja 900ml" })
            pc.Itens.Add(new PedidoItem { ProdutoId = produtos[nome], Quantidade = 100, Unitario = Catalogo.First(c => c.Nome == nome).Custo });
        Pedidos.Salvar(pc);

        // ---------------- fretes (CIOT simulado)
        var admAnterior = Db.Config("ciot_administradora");
        Db.SalvarConfig("ciot_administradora", "SIMULADOR");
        foreach (var (transportador, motorista, veiculo, destino, uf, valor, entregue) in new[]
                 {
                     (transportadorTac, motorista1, veiculo2, "Belo Horizonte", "MG", 3200m, true),
                     (transportadorEtc, motorista2, veiculo3, "Curitiba", "PR", 4800m, false),
                     (transportadorTac, motorista1, veiculo2, "Ribeirão Preto", "SP", 1450m, false),
                 })
        {
            var id = Convert.ToInt64(Db.Scalar("""
                INSERT INTO fretes(data, modalidade, transportador_id, motorista_id, veiculo_id, origem_cidade, origem_uf, destino_cidade, destino_uf,
                                   distancia_km, peso_kg, mercadoria, valor_frete, valor_pedagio, valor_adiantamento, previsao_entrega, ciot_status, usuario_id)
                VALUES(@p0, 'CONTRATADO', @p1, @p2, @p3, 'Campinas', 'SP', @p4, @p5, @p6, @p7, 'Alimentos não perecíveis', @p8, @p9, @p10, @p11, @p12, 1)
                RETURNING id
                """, hoje.AddDays(entregue ? -12 : -1), transportador, motorista, veiculo, destino, uf, r.Next(200, 900), r.Next(3000, 12000),
                valor, Math.Round(valor * 0.06m, 2), Math.Round(valor * 0.4m, 2), Db.Dia(hoje.AddDays(entregue ? -9 : 3)),
                Fretes.CiotObrigatorio("CONTRATADO", Ui.Txt(Db.Scalar("SELECT cpf_cnpj FROM pessoas WHERE id = @p0", transportador))) ? "PENDENTE" : "NAO_SE_APLICA"));
            if (Ui.Txt(Db.Scalar("SELECT ciot_status FROM fretes WHERE id = @p0", id)) == "PENDENTE")
                Fretes.DeclararCiot(id).GetAwaiter().GetResult();
            Fretes.GerarContas(id);
            Fretes.MudarStatus(id, "EM_TRANSITO");
            if (entregue)
            {
                Fretes.MudarStatus(id, "ENTREGUE");
                if (Ui.Txt(Db.Scalar("SELECT ciot_status FROM fretes WHERE id = @p0", id)) == "DECLARADO")
                    Fretes.EncerrarCiot(id).GetAwaiter().GetResult();
            }
        }
        Db.SalvarConfig("ciot_administradora", admAnterior.Length > 0 ? admAnterior : "PAMCARD");

        // ---------------- cobrança bancária
        var contaBanco = Convert.ToInt64(Db.Scalar("""
            INSERT INTO contas_bancarias(nome, banco_codigo, banco_nome, agencia, agencia_dv, conta, conta_dv, carteira, convenio,
                                         campo_livre_modelo, multa_pct, juros_mes_pct, ambiente)
            VALUES('Cobrança demonstração', '237', 'Banco Exemplo', '1234', '5', '0012345', '6', '09', '1234567', @p0, 2, 1, 'HOMOLOGACAO')
            RETURNING id
            """, Cobranca.ModeloCampoLivrePadrao));
        var paraBoleto = Db.Query("SELECT id FROM contas WHERE tipo = 'RECEBER' AND status = 'ABERTA' AND cliente_id IS NOT NULL ORDER BY vencimento LIMIT 8");
        foreach (DataRow c in paraBoleto.Rows)
            Cobranca.Emitir(contaBanco, (long)c["id"]);
        if (paraBoleto.Rows.Count > 0)
        {
            var (remessa, _, _, _) = Cobranca.GerarRemessa(contaBanco);
            Cobranca.ProcessarRetorno(contaBanco, "SIMULADO_DEMO.ret", Cobranca.SimularRetorno(remessa, liquidar: false));
        }

        // ---------------- notas fiscais de algumas vendas recentes
        var notas = 0;
        foreach (DataRow v in Db.Query("SELECT id FROM vendas WHERE status = 'FINALIZADA' AND cliente_id IS NOT NULL ORDER BY id DESC LIMIT 5").Rows)
        {
            var nota = Fiscal.DeVenda((long)v["id"]);
            var idNota = Fiscal.Salvar(nota);
            Fiscal.Emitir(idNota);
            notas++;
        }

        // ---------------- ajustes de estoque (inventário)
        Operacoes.AjustarEstoque(produtos["Tomate"], "SAIDA", -3.5m, "Perda por avaria (demonstração)");
        Operacoes.AjustarEstoque(produtos["Banana Prata"], "SAIDA", -2.2m, "Produto maduro descartado (demonstração)");

        return $"Dados de demonstração gerados: {clientes.Count} clientes, {fornecedores.Count} fornecedores, {Catalogo.Length} produtos, " +
               $"{compras} compras, {ops} ordens de produção, {vendas} vendas, 7 orçamentos/pedidos, 3 fretes, {paraBoleto.Rows.Count} boletos, {notas} notas fiscais.";
    }
}
