using System.Text.RegularExpressions;

namespace ErpLoja.Data;

/// <summary>
/// Uma alteração versionada do banco. Nunca altere uma migração já publicada: crie outra.
/// ReconstroiTabelas = a migração recria tabelas e por isso roda com as chaves estrangeiras desligadas
/// (verificadas com PRAGMA foreign_key_check antes do commit).
/// </summary>
public record Migracao(int Versao, string Nome, Action<DbSessao> Aplicar, bool ReconstroiTabelas = false);

/// <summary>
/// Migrations do banco. Cada migração roda uma única vez, em ordem, dentro de uma transação,
/// e fica registrada na tabela __migracoes. Antes de migrar um banco existente é feito um backup automático.
/// Para evoluir o banco: acrescente uma nova Migracao no fim da lista Todas (ver docs/migracoes.md).
/// </summary>
public static class Migracoes
{
    public static readonly Migracao[] Todas =
    [
        new(1, "Estrutura inicial (cadastros, vendas, compras, estoque, financeiro)", M001),
        new(2, "Pedidos, orçamentos, notas fiscais e dados fiscais", M002),
        new(3, "Usuários com username/e-mail e recuperação de senha", M003),
        new(4, "Parâmetros do sistema", M004),
        new(5, "Cadastro único de pessoas (clientes, fornecedores, transportadores)", M005, ReconstroiTabelas: true),
        new(6, "Produção: fichas técnicas e ordens de produção", M006),
        new(7, "Transporte: veículos, motoristas, fretes e CIOT", M007),
        new(8, "TEF: transações de cartão e PIX", M008),
        new(9, "Cobrança bancária: contas, boletos, remessas e retornos CNAB 240", M009),
        new(10, "Reforma tributária: IBS, CBS e Imposto Seletivo", M010),
        new(11, "Parâmetros de impressoras, TEF, transporte, produção e reforma tributária", M011),
        new(12, "Log de atividades: parâmetro de retenção", M012),
    ];

    public static int VersaoDoSistema => Todas.Max(m => m.Versao);

    /// <summary>Versões já aplicadas no banco atual.</summary>
    public static List<(int Versao, string Nome, string AplicadaEm)> Aplicadas()
    {
        using var s = new DbSessao();
        if (s.Scalar("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__migracoes'") is null)
            return [];
        return s.Query("SELECT versao, nome, aplicada_em FROM __migracoes ORDER BY versao").Rows.Cast<DataRow>()
            .Select(r => ((int)(long)r["versao"], (string)r["nome"], (string)r["aplicada_em"])).ToList();
    }

    /// <summary>
    /// Leva o banco (Db.Caminho) até a versão do sistema. Cria o arquivo se criar = true.
    /// Devolve as migrações aplicadas agora e o caminho do backup feito antes (se houver).
    /// </summary>
    public static (List<Migracao> Aplicadas, string? Backup) Atualizar(bool criar = false)
    {
        using var semLog = Log.Suspender(); // registra um resumo por migração, não cada comando
        int atual;
        using (var s = new DbSessao(transacao: true, criar: criar))
        {
            s.Exec("""
                CREATE TABLE IF NOT EXISTS __migracoes (
                    versao      INTEGER PRIMARY KEY,
                    nome        TEXT NOT NULL,
                    aplicada_em TEXT NOT NULL
                )
                """);
            RegistrarLegado(s);
            atual = Convert.ToInt32(s.Scalar("SELECT COALESCE(MAX(versao), 0) FROM __migracoes"));
            s.Commit();
        }

        if (atual > VersaoDoSistema)
            throw new InvalidOperationException(
                $"Este banco está na versão {atual}, mais nova que a deste executável ({VersaoDoSistema}). " +
                "Use a versão mais recente do ErpLoja.exe.");

        var pendentes = Todas.Where(m => m.Versao > atual).OrderBy(m => m.Versao).ToList();
        if (pendentes.Count == 0)
            return ([], null);

        string? backup = null;
        if (atual > 0)
            backup = Backups.Criar($"antes_migracao_v{atual}_para_v{VersaoDoSistema}");

        foreach (var m in pendentes)
        {
            using var s = new DbSessao(transacao: true, semChavesEstrangeiras: m.ReconstroiTabelas);
            try
            {
                if (m.ReconstroiTabelas && Convert.ToInt64(s.Scalar("PRAGMA foreign_keys")) != 0)
                    throw new InvalidOperationException("Não foi possível desligar as chaves estrangeiras para reconstruir as tabelas.");
                m.Aplicar(s);
                if (m.ReconstroiTabelas && s.Query("PRAGMA foreign_key_check").Rows.Count > 0)
                    throw new InvalidOperationException("A reconstrução deixou referências inválidas entre tabelas.");
                s.Exec("INSERT INTO __migracoes(versao, nome, aplicada_em) VALUES(@p0, @p1, @p2)", m.Versao, m.Nome, DateTime.Now);
                s.Exec($"PRAGMA user_version = {m.Versao}");
                s.Commit();
                Log.Registrar(Log.Sistema, $"Migração {m.Versao} aplicada", m.Nome + (backup is null ? "" : $" (backup: {Path.GetFileName(backup)})"));
            }
            catch (Exception ex)
            {
                Log.Registrar(Log.Erro, $"Falha na migração {m.Versao}", ex.Message);
                throw new InvalidOperationException(
                    $"Falha ao aplicar a migração {m.Versao} ({m.Nome}): {ex.Message}" +
                    (backup is null ? "" : $"\nO banco não foi alterado por esta migração. Backup anterior: {backup}"), ex);
            }
        }
        return (pendentes, backup);
    }

    /// <summary>
    /// Bancos criados antes do controle de migrações (versões 1.x) não têm __migracoes.
    /// Registra como aplicadas as migrações que aquela estrutura já contém.
    /// </summary>
    static void RegistrarLegado(DbSessao s)
    {
        if (Convert.ToInt64(s.Scalar("SELECT COUNT(*) FROM __migracoes")) > 0)
            return;
        if (s.Scalar("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'usuarios'") is null)
            return; // banco novo
        var userVersion = Convert.ToInt64(s.Scalar("PRAGMA user_version"));
        var legado = userVersion >= 2 ? 2 : 1;
        foreach (var m in Todas.Where(m => m.Versao <= legado))
            s.Exec("INSERT INTO __migracoes(versao, nome, aplicada_em) VALUES(@p0, @p1, @p2)", m.Versao, m.Nome + " (banco legado)", DateTime.Now);
    }

    static bool ColunaExiste(DbSessao s, string tabela, string coluna) =>
        s.Query($"PRAGMA table_info({tabela})").Rows.Cast<DataRow>().Any(r => Convert.ToString(r["name"]) == coluna);

    static void AdicionarColuna(DbSessao s, string tabela, string coluna, string definicao)
    {
        if (!ColunaExiste(s, tabela, coluna))
            s.Exec($"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao}");
    }

    static void Config(DbSessao s, string chave, string valor) =>
        s.Exec("INSERT OR IGNORE INTO configuracoes(chave, valor) VALUES(@p0, @p1)", chave, valor);

    /// <summary>
    /// Troca, em todas as tabelas, as chaves estrangeiras que apontam para as tabelas antigas por referências à nova.
    /// Usa o procedimento seguro do SQLite: cria a tabela nova, copia os dados, apaga a antiga e renomeia (FKs desligadas).
    /// </summary>
    static void RepontarReferencias(DbSessao s, string[] antigas, string nova)
    {
        string Padrao(string t) => $@"REFERENCES\s+""?{t}""?\s*\(";
        var tabelas = s.Query("SELECT name, sql FROM sqlite_master WHERE type = 'table' AND sql IS NOT NULL").Rows.Cast<DataRow>()
            .Select(r => ((string)r["name"], (string)r["sql"]))
            .Where(t => antigas.Any(a => Regex.IsMatch(t.Item2, Padrao(a), RegexOptions.IgnoreCase)))
            .ToList();
        foreach (var (nome, sql) in tabelas)
        {
            var temporaria = nome + "__nova";
            var novoSql = sql;
            foreach (var a in antigas)
                novoSql = Regex.Replace(novoSql, Padrao(a), $"REFERENCES {nova}(", RegexOptions.IgnoreCase);
            novoSql = Regex.Replace(novoSql, $@"^CREATE\s+TABLE\s+(IF\s+NOT\s+EXISTS\s+)?""?{nome}""?", $"CREATE TABLE {temporaria}", RegexOptions.IgnoreCase);
            var indices = s.Query("SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = @p0 AND sql IS NOT NULL", nome)
                .Rows.Cast<DataRow>().Select(r => (string)r["sql"]).ToList();
            s.Exec(novoSql);
            s.Exec($"INSERT INTO {temporaria} SELECT * FROM {nome}");
            s.Exec($"DROP TABLE {nome}");
            s.Exec($"ALTER TABLE {temporaria} RENAME TO {nome}");
            foreach (var indice in indices)
                s.Exec(indice);
        }
    }

    // ------------------------------------------------------------------ 001
    static void M001(DbSessao s)
    {
        s.Exec("""
        CREATE TABLE IF NOT EXISTS configuracoes (
            chave TEXT PRIMARY KEY,
            valor TEXT
        );

        CREATE TABLE IF NOT EXISTS usuarios (
            id     INTEGER PRIMARY KEY AUTOINCREMENT,
            nome   TEXT NOT NULL,
            login  TEXT NOT NULL UNIQUE COLLATE NOCASE,
            senha  TEXT NOT NULL,
            admin  INTEGER NOT NULL DEFAULT 0,
            ativo  INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS categorias (
            id   INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL UNIQUE COLLATE NOCASE
        );

        CREATE TABLE IF NOT EXISTS clientes (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            nome        TEXT NOT NULL,
            cpf_cnpj    TEXT,
            telefone    TEXT,
            email       TEXT,
            endereco    TEXT,
            cidade      TEXT,
            observacoes TEXT,
            ativo       INTEGER NOT NULL DEFAULT 1,
            criado_em   TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
        );

        CREATE TABLE IF NOT EXISTS fornecedores (
            id        INTEGER PRIMARY KEY AUTOINCREMENT,
            nome      TEXT NOT NULL,
            cnpj      TEXT,
            telefone  TEXT,
            email     TEXT,
            endereco  TEXT,
            cidade    TEXT,
            contato   TEXT,
            ativo     INTEGER NOT NULL DEFAULT 1,
            criado_em TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
        );

        CREATE TABLE IF NOT EXISTS produtos (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            codigo         TEXT NOT NULL UNIQUE COLLATE NOCASE,
            codigo_barras  TEXT,
            nome           TEXT NOT NULL,
            categoria_id   INTEGER REFERENCES categorias(id),
            unidade        TEXT NOT NULL DEFAULT 'UN',
            preco_custo    REAL NOT NULL DEFAULT 0,
            preco_venda    REAL NOT NULL DEFAULT 0,
            estoque        REAL NOT NULL DEFAULT 0,
            estoque_minimo REAL NOT NULL DEFAULT 0,
            ativo          INTEGER NOT NULL DEFAULT 1,
            criado_em      TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
        );
        CREATE INDEX IF NOT EXISTS ix_produtos_barras ON produtos(codigo_barras);

        CREATE TABLE IF NOT EXISTS vendas (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            data            TEXT NOT NULL,
            cliente_id      INTEGER REFERENCES clientes(id),
            usuario_id      INTEGER REFERENCES usuarios(id),
            subtotal        REAL NOT NULL DEFAULT 0,
            desconto        REAL NOT NULL DEFAULT 0,
            total           REAL NOT NULL DEFAULT 0,
            forma_pagamento TEXT NOT NULL,
            status          TEXT NOT NULL DEFAULT 'FINALIZADA' CHECK (status IN ('FINALIZADA', 'CANCELADA')),
            observacoes     TEXT
        );
        CREATE INDEX IF NOT EXISTS ix_vendas_data ON vendas(data);

        CREATE TABLE IF NOT EXISTS venda_itens (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            venda_id       INTEGER NOT NULL REFERENCES vendas(id) ON DELETE CASCADE,
            produto_id     INTEGER NOT NULL REFERENCES produtos(id),
            quantidade     REAL NOT NULL,
            preco_unitario REAL NOT NULL,
            custo_unitario REAL NOT NULL DEFAULT 0,
            total          REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_venda_itens_venda ON venda_itens(venda_id);

        CREATE TABLE IF NOT EXISTS compras (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            data            TEXT NOT NULL,
            fornecedor_id   INTEGER NOT NULL REFERENCES fornecedores(id),
            usuario_id      INTEGER REFERENCES usuarios(id),
            numero_nota     TEXT,
            total           REAL NOT NULL DEFAULT 0,
            forma_pagamento TEXT NOT NULL,
            status          TEXT NOT NULL DEFAULT 'FINALIZADA' CHECK (status IN ('FINALIZADA', 'CANCELADA')),
            observacoes     TEXT
        );

        CREATE TABLE IF NOT EXISTS compra_itens (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            compra_id      INTEGER NOT NULL REFERENCES compras(id) ON DELETE CASCADE,
            produto_id     INTEGER NOT NULL REFERENCES produtos(id),
            quantidade     REAL NOT NULL,
            custo_unitario REAL NOT NULL,
            total          REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_compra_itens_compra ON compra_itens(compra_id);

        CREATE TABLE IF NOT EXISTS movimentacoes_estoque (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            data          TEXT NOT NULL,
            produto_id    INTEGER NOT NULL REFERENCES produtos(id),
            tipo          TEXT NOT NULL CHECK (tipo IN ('ENTRADA', 'SAIDA', 'AJUSTE')),
            quantidade    REAL NOT NULL,
            saldo         REAL NOT NULL,
            origem        TEXT,
            referencia_id INTEGER,
            usuario_id    INTEGER REFERENCES usuarios(id)
        );
        CREATE INDEX IF NOT EXISTS ix_mov_produto ON movimentacoes_estoque(produto_id);

        CREATE TABLE IF NOT EXISTS contas (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            tipo            TEXT NOT NULL CHECK (tipo IN ('RECEBER', 'PAGAR')),
            descricao       TEXT NOT NULL,
            valor           REAL NOT NULL,
            vencimento      TEXT NOT NULL,
            data_pagamento  TEXT,
            valor_pago      REAL,
            forma_pagamento TEXT,
            status          TEXT NOT NULL DEFAULT 'ABERTA' CHECK (status IN ('ABERTA', 'PAGA', 'CANCELADA')),
            cliente_id      INTEGER REFERENCES clientes(id),
            fornecedor_id   INTEGER REFERENCES fornecedores(id),
            venda_id        INTEGER REFERENCES vendas(id),
            compra_id       INTEGER REFERENCES compras(id),
            criado_em       TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
        );
        CREATE INDEX IF NOT EXISTS ix_contas_status ON contas(status, vencimento);
        """);
        if (Convert.ToInt64(s.Scalar("SELECT COUNT(*) FROM usuarios")) == 0)
            s.Exec("INSERT INTO usuarios(nome, login, senha, admin) VALUES('Administrador', 'admin', @p0, 1)", Senhas.Hash("admin"));
        if (Convert.ToInt64(s.Scalar("SELECT COUNT(*) FROM categorias")) == 0)
            s.Exec("INSERT INTO categorias(nome) VALUES('Geral')");
        Config(s, "nome_loja", "Minha Loja");
    }

    // ------------------------------------------------------------------ 002
    static void M002(DbSessao s)
    {
        foreach (var (tabela, coluna, definicao) in new[]
                 {
                     ("produtos", "ncm", "TEXT"),
                     ("produtos", "cest", "TEXT"),
                     ("produtos", "origem", "INTEGER NOT NULL DEFAULT 0"),
                     ("produtos", "cst", "TEXT NOT NULL DEFAULT '102'"),
                     ("produtos", "aliq_icms", "REAL NOT NULL DEFAULT 0"),
                     ("produtos", "aliq_ipi", "REAL NOT NULL DEFAULT 0"),
                     ("produtos", "aliq_pis", "REAL NOT NULL DEFAULT 0"),
                     ("produtos", "aliq_cofins", "REAL NOT NULL DEFAULT 0"),
                     ("clientes", "ie", "TEXT"),
                     ("clientes", "uf", "TEXT"),
                     ("clientes", "cep", "TEXT"),
                     ("fornecedores", "ie", "TEXT"),
                     ("fornecedores", "uf", "TEXT"),
                     ("fornecedores", "cep", "TEXT"),
                     ("vendas", "frete", "REAL NOT NULL DEFAULT 0"),
                 })
            AdicionarColuna(s, tabela, coluna, definicao);

        s.Exec("""
        CREATE TABLE IF NOT EXISTS pedidos (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            tipo            TEXT NOT NULL CHECK (tipo IN ('ORCAMENTO', 'VENDA', 'COMPRA')),
            data            TEXT NOT NULL,
            cliente_id      INTEGER REFERENCES clientes(id),
            fornecedor_id   INTEGER REFERENCES fornecedores(id),
            usuario_id      INTEGER REFERENCES usuarios(id),
            validade        TEXT,
            subtotal        REAL NOT NULL DEFAULT 0,
            desconto        REAL NOT NULL DEFAULT 0,
            frete           REAL NOT NULL DEFAULT 0,
            total           REAL NOT NULL DEFAULT 0,
            forma_pagamento TEXT,
            parcelas        INTEGER NOT NULL DEFAULT 1,
            observacoes     TEXT,
            status          TEXT NOT NULL DEFAULT 'ABERTO'
                            CHECK (status IN ('ABERTO', 'APROVADO', 'RECUSADO', 'FATURADO', 'RECEBIDO', 'CANCELADO')),
            origem_id       INTEGER REFERENCES pedidos(id),
            venda_id        INTEGER REFERENCES vendas(id),
            compra_id       INTEGER REFERENCES compras(id)
        );
        CREATE INDEX IF NOT EXISTS ix_pedidos_tipo ON pedidos(tipo, status);

        CREATE TABLE IF NOT EXISTS pedido_itens (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            pedido_id      INTEGER NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
            produto_id     INTEGER NOT NULL REFERENCES produtos(id),
            quantidade     REAL NOT NULL,
            preco_unitario REAL NOT NULL,
            desconto       REAL NOT NULL DEFAULT 0,
            total          REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_pedido_itens ON pedido_itens(pedido_id);

        CREATE TABLE IF NOT EXISTS notas_fiscais (
            id                INTEGER PRIMARY KEY AUTOINCREMENT,
            tipo              TEXT NOT NULL CHECK (tipo IN ('SAIDA', 'ENTRADA')),
            modelo            TEXT NOT NULL DEFAULT '55',
            serie             INTEGER NOT NULL DEFAULT 1,
            numero            INTEGER,
            chave             TEXT UNIQUE,
            data_emissao      TEXT NOT NULL,
            natureza_operacao TEXT NOT NULL,
            propria           INTEGER NOT NULL DEFAULT 1,
            cliente_id        INTEGER REFERENCES clientes(id),
            fornecedor_id     INTEGER REFERENCES fornecedores(id),
            venda_id          INTEGER REFERENCES vendas(id),
            compra_id         INTEGER REFERENCES compras(id),
            part_nome         TEXT,
            part_documento    TEXT,
            part_ie           TEXT,
            part_endereco     TEXT,
            part_cidade       TEXT,
            part_uf           TEXT,
            valor_produtos    REAL NOT NULL DEFAULT 0,
            desconto          REAL NOT NULL DEFAULT 0,
            frete             REAL NOT NULL DEFAULT 0,
            valor_icms        REAL NOT NULL DEFAULT 0,
            valor_ipi         REAL NOT NULL DEFAULT 0,
            valor_pis         REAL NOT NULL DEFAULT 0,
            valor_cofins      REAL NOT NULL DEFAULT 0,
            valor_total       REAL NOT NULL DEFAULT 0,
            info_complementar TEXT,
            status            TEXT NOT NULL DEFAULT 'DIGITADA' CHECK (status IN ('DIGITADA', 'EMITIDA', 'CANCELADA')),
            xml               TEXT,
            usuario_id        INTEGER REFERENCES usuarios(id),
            criado_em         TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ux_nf_numero ON notas_fiscais(modelo, serie, numero) WHERE propria = 1 AND numero IS NOT NULL;

        CREATE TABLE IF NOT EXISTS nota_itens (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            nota_id        INTEGER NOT NULL REFERENCES notas_fiscais(id) ON DELETE CASCADE,
            produto_id     INTEGER REFERENCES produtos(id),
            codigo         TEXT,
            descricao      TEXT NOT NULL,
            ncm            TEXT,
            cfop           TEXT,
            cst            TEXT,
            unidade        TEXT,
            quantidade     REAL NOT NULL,
            valor_unitario REAL NOT NULL,
            desconto       REAL NOT NULL DEFAULT 0,
            valor_total    REAL NOT NULL,
            aliq_icms      REAL NOT NULL DEFAULT 0,
            valor_icms     REAL NOT NULL DEFAULT 0,
            aliq_ipi       REAL NOT NULL DEFAULT 0,
            valor_ipi      REAL NOT NULL DEFAULT 0,
            aliq_pis       REAL NOT NULL DEFAULT 0,
            valor_pis      REAL NOT NULL DEFAULT 0,
            aliq_cofins    REAL NOT NULL DEFAULT 0,
            valor_cofins   REAL NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS ix_nota_itens ON nota_itens(nota_id);

        CREATE TABLE IF NOT EXISTS produtos_fornecedor (
            fornecedor_id     INTEGER NOT NULL REFERENCES fornecedores(id),
            codigo_fornecedor TEXT NOT NULL,
            produto_id        INTEGER NOT NULL REFERENCES produtos(id),
            fator             REAL NOT NULL DEFAULT 1,
            PRIMARY KEY (fornecedor_id, codigo_fornecedor)
        );
        """);
        foreach (var (chave, valor) in new[] { ("regime", "1 - Simples Nacional"), ("serie_55", "1"), ("proximo_numero_55", "1"),
                     ("serie_65", "1"), ("proximo_numero_65", "1") })
            Config(s, chave, valor);
    }

    // ------------------------------------------------------------------ 003
    static void M003(DbSessao s)
    {
        if (ColunaExiste(s, "usuarios", "login"))
            s.Exec("ALTER TABLE usuarios RENAME COLUMN login TO username");
        AdicionarColuna(s, "usuarios", "email", "TEXT");
        s.Exec("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_email ON usuarios(email COLLATE NOCASE) WHERE email IS NOT NULL;

            CREATE TABLE IF NOT EXISTS recuperacao_senha (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                usuario_id  INTEGER NOT NULL REFERENCES usuarios(id),
                codigo_hash TEXT NOT NULL,
                expira_em   TEXT NOT NULL,
                tentativas  INTEGER NOT NULL DEFAULT 0,
                usado       INTEGER NOT NULL DEFAULT 0,
                criado_em   TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            );
            """);
    }

    // ------------------------------------------------------------------ 004
    static void M004(DbSessao s)
    {
        foreach (var p in Parametros.Definicoes)
            Config(s, p.Chave, p.Padrao);
    }

    // ------------------------------------------------------------------ 005
    /// <summary>Clientes e fornecedores viram uma única tabela "pessoas" (uma pessoa pode ter vários papéis).</summary>
    static void M005(DbSessao s)
    {
        s.Exec("""
            CREATE TABLE pessoas (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                nome             TEXT NOT NULL,
                fantasia         TEXT,
                cpf_cnpj         TEXT,
                ie               TEXT,
                telefone         TEXT,
                email            TEXT,
                contato          TEXT,
                endereco         TEXT,
                cidade           TEXT,
                uf               TEXT,
                cep              TEXT,
                eh_cliente       INTEGER NOT NULL DEFAULT 0,
                eh_fornecedor    INTEGER NOT NULL DEFAULT 0,
                eh_transportador INTEGER NOT NULL DEFAULT 0,
                rntrc            TEXT,
                limite_credito   REAL NOT NULL DEFAULT 0,
                observacoes      TEXT,
                ativo            INTEGER NOT NULL DEFAULT 1,
                criado_em        TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            );
            INSERT INTO pessoas(id, nome, cpf_cnpj, ie, telefone, email, endereco, cidade, uf, cep, eh_cliente, observacoes, ativo, criado_em)
                SELECT id, nome, cpf_cnpj, ie, telefone, email, endereco, cidade, uf, cep, 1, observacoes, ativo, criado_em FROM clientes;
            CREATE TEMP TABLE _mapa_fornecedor (antigo INTEGER PRIMARY KEY, novo INTEGER NOT NULL);
            """);

        // fornecedor com o mesmo CPF/CNPJ de um cliente vira a mesma pessoa
        var documentos = s.Query("SELECT id, cpf_cnpj FROM pessoas").Rows.Cast<DataRow>()
            .Select(r => ((long)r["id"], Fiscal.SoDigitos(Ui.Txt(r["cpf_cnpj"])))).Where(x => x.Item2.Length > 0)
            .GroupBy(x => x.Item2).ToDictionary(g => g.Key, g => g.First().Item1);
        foreach (DataRow f in s.Query("SELECT * FROM fornecedores ORDER BY id").Rows)
        {
            var documento = Fiscal.SoDigitos(Ui.Txt(f["cnpj"]));
            long novo;
            if (documento.Length > 0 && documentos.TryGetValue(documento, out var existente))
            {
                s.Exec("""
                    UPDATE pessoas SET eh_fornecedor = 1, contato = COALESCE(contato, @p1), telefone = COALESCE(telefone, @p2),
                                       email = COALESCE(email, @p3), ie = COALESCE(ie, @p4)
                    WHERE id = @p0
                    """, existente, f["contato"], f["telefone"], f["email"], f["ie"]);
                novo = existente;
            }
            else
            {
                novo = s.Inserir("""
                    INSERT INTO pessoas(nome, cpf_cnpj, ie, telefone, email, contato, endereco, cidade, uf, cep, eh_fornecedor, ativo, criado_em)
                    VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, 1, @p10, @p11)
                    """, f["nome"], f["cnpj"], f["ie"], f["telefone"], f["email"], f["contato"], f["endereco"], f["cidade"],
                    f["uf"], f["cep"], f["ativo"], f["criado_em"]);
                if (documento.Length > 0)
                    documentos[documento] = novo;
            }
            s.Exec("INSERT INTO _mapa_fornecedor VALUES(@p0, @p1)", f["id"], novo);
        }
        foreach (var tabela in new[] { "compras", "contas", "pedidos", "notas_fiscais" })
            s.Exec($"UPDATE {tabela} SET fornecedor_id = (SELECT novo FROM _mapa_fornecedor WHERE antigo = {tabela}.fornecedor_id) WHERE fornecedor_id IS NOT NULL");
        s.Exec("UPDATE OR REPLACE produtos_fornecedor SET fornecedor_id = (SELECT novo FROM _mapa_fornecedor WHERE antigo = produtos_fornecedor.fornecedor_id)");
        s.Exec("DROP TABLE _mapa_fornecedor");

        RepontarReferencias(s, ["clientes", "fornecedores"], "pessoas");
        s.Exec("""
            DROP TABLE clientes;
            DROP TABLE fornecedores;
            CREATE INDEX IF NOT EXISTS ix_pessoas_papeis ON pessoas(eh_cliente, eh_fornecedor, eh_transportador);
            CREATE INDEX IF NOT EXISTS ix_pessoas_documento ON pessoas(cpf_cnpj);
            """);
    }

    // ------------------------------------------------------------------ 006
    static void M006(DbSessao s)
    {
        AdicionarColuna(s, "produtos", "tipo", "TEXT NOT NULL DEFAULT 'MERCADORIA'");
        s.Exec("""
            CREATE TABLE IF NOT EXISTS fichas_tecnicas (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                produto_id      INTEGER NOT NULL UNIQUE REFERENCES produtos(id),
                rendimento      REAL NOT NULL DEFAULT 1,
                custo_adicional REAL NOT NULL DEFAULT 0,
                observacoes     TEXT,
                atualizado_em   TEXT
            );
            CREATE TABLE IF NOT EXISTS ficha_itens (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                ficha_id   INTEGER NOT NULL REFERENCES fichas_tecnicas(id) ON DELETE CASCADE,
                insumo_id  INTEGER NOT NULL REFERENCES produtos(id),
                quantidade REAL NOT NULL,
                perda_pct  REAL NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS ordens_producao (
                id                   INTEGER PRIMARY KEY AUTOINCREMENT,
                data                 TEXT NOT NULL,
                produto_id           INTEGER NOT NULL REFERENCES produtos(id),
                quantidade           REAL NOT NULL,
                quantidade_produzida REAL,
                status               TEXT NOT NULL DEFAULT 'PLANEJADA'
                                     CHECK (status IN ('PLANEJADA', 'EM_PRODUCAO', 'CONCLUIDA', 'CANCELADA')),
                previsao             TEXT,
                inicio               TEXT,
                conclusao            TEXT,
                custo_total          REAL,
                custo_unitario       REAL,
                pedido_id            INTEGER REFERENCES pedidos(id),
                observacoes          TEXT,
                usuario_id           INTEGER REFERENCES usuarios(id)
            );
            CREATE TABLE IF NOT EXISTS op_itens (
                id                   INTEGER PRIMARY KEY AUTOINCREMENT,
                op_id                INTEGER NOT NULL REFERENCES ordens_producao(id) ON DELETE CASCADE,
                insumo_id            INTEGER NOT NULL REFERENCES produtos(id),
                quantidade_prevista  REAL NOT NULL,
                quantidade_consumida REAL,
                custo_unitario       REAL
            );
            CREATE INDEX IF NOT EXISTS ix_op_status ON ordens_producao(status);
            """);
    }

    // ------------------------------------------------------------------ 007
    static void M007(DbSessao s)
    {
        AdicionarColuna(s, "pessoas", "eh_motorista", "INTEGER NOT NULL DEFAULT 0");
        AdicionarColuna(s, "pessoas", "cnh", "TEXT");
        AdicionarColuna(s, "pessoas", "cnh_categoria", "TEXT");
        AdicionarColuna(s, "pessoas", "cnh_validade", "TEXT");
        s.Exec("""
            CREATE TABLE IF NOT EXISTS veiculos (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                placa           TEXT NOT NULL UNIQUE COLLATE NOCASE,
                renavam         TEXT,
                tipo            TEXT NOT NULL DEFAULT 'CAMINHAO',
                carroceria      TEXT,
                marca_modelo    TEXT,
                ano             INTEGER,
                uf              TEXT,
                capacidade_kg   REAL NOT NULL DEFAULT 0,
                capacidade_m3   REAL NOT NULL DEFAULT 0,
                tara_kg         REAL NOT NULL DEFAULT 0,
                rntrc           TEXT,
                proprietario_id INTEGER REFERENCES pessoas(id),
                frota_propria   INTEGER NOT NULL DEFAULT 1,
                ativo           INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS fretes (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                data                TEXT NOT NULL,
                modalidade          TEXT NOT NULL DEFAULT 'CONTRATADO' CHECK (modalidade IN ('PROPRIO', 'CONTRATADO')),
                status              TEXT NOT NULL DEFAULT 'ABERTO' CHECK (status IN ('ABERTO', 'EM_TRANSITO', 'ENTREGUE', 'CANCELADO')),
                transportador_id    INTEGER REFERENCES pessoas(id),
                motorista_id        INTEGER REFERENCES pessoas(id),
                veiculo_id          INTEGER REFERENCES veiculos(id),
                origem_cidade       TEXT,
                origem_uf           TEXT,
                destino_cidade      TEXT,
                destino_uf          TEXT,
                distancia_km        REAL NOT NULL DEFAULT 0,
                peso_kg             REAL NOT NULL DEFAULT 0,
                mercadoria          TEXT,
                valor_frete         REAL NOT NULL DEFAULT 0,
                valor_pedagio       REAL NOT NULL DEFAULT 0,
                valor_adiantamento  REAL NOT NULL DEFAULT 0,
                outras_despesas     REAL NOT NULL DEFAULT 0,
                previsao_entrega    TEXT,
                data_entrega        TEXT,
                venda_id            INTEGER REFERENCES vendas(id),
                nota_id             INTEGER REFERENCES notas_fiscais(id),
                contas_geradas      INTEGER NOT NULL DEFAULT 0,
                ciot                TEXT,
                ciot_protocolo      TEXT,
                ciot_status         TEXT NOT NULL DEFAULT 'NAO_SE_APLICA'
                                    CHECK (ciot_status IN ('NAO_SE_APLICA', 'PENDENTE', 'DECLARADO', 'ENCERRADO', 'CANCELADO')),
                ciot_administradora TEXT,
                ciot_mensagem       TEXT,
                observacoes         TEXT,
                usuario_id          INTEGER REFERENCES usuarios(id),
                criado_em           TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            );
            CREATE INDEX IF NOT EXISTS ix_fretes_status ON fretes(status);
            """);
        AdicionarColuna(s, "contas", "frete_id", "INTEGER REFERENCES fretes(id)");
    }

    // ------------------------------------------------------------------ 008
    static void M008(DbSessao s) => s.Exec("""
        CREATE TABLE IF NOT EXISTS transacoes_tef (
            id                  INTEGER PRIMARY KEY AUTOINCREMENT,
            data                TEXT NOT NULL,
            venda_id            INTEGER REFERENCES vendas(id),
            provedor            TEXT NOT NULL,
            modalidade          TEXT NOT NULL CHECK (modalidade IN ('DEBITO', 'CREDITO', 'PIX')),
            bandeira            TEXT,
            parcelas            INTEGER NOT NULL DEFAULT 1,
            valor               REAL NOT NULL,
            nsu                 TEXT,
            autorizacao         TEXT,
            status              TEXT NOT NULL CHECK (status IN ('PENDENTE', 'CONFIRMADA', 'DESFEITA', 'CANCELADA', 'NEGADA')),
            mensagem            TEXT,
            comprovante_cliente TEXT,
            comprovante_loja    TEXT
        );
        CREATE INDEX IF NOT EXISTS ix_tef_venda ON transacoes_tef(venda_id);
        """);

    // ------------------------------------------------------------------ 009
    static void M009(DbSessao s) => s.Exec("""
        CREATE TABLE IF NOT EXISTS contas_bancarias (
            id                  INTEGER PRIMARY KEY AUTOINCREMENT,
            nome                TEXT NOT NULL,
            banco_codigo        TEXT NOT NULL,
            banco_nome          TEXT,
            agencia             TEXT NOT NULL,
            agencia_dv          TEXT,
            conta               TEXT NOT NULL,
            conta_dv            TEXT,
            carteira            TEXT,
            convenio            TEXT,
            campo_livre_modelo  TEXT NOT NULL,
            nosso_numero_digitos INTEGER NOT NULL DEFAULT 11,
            nosso_numero_atual  INTEGER NOT NULL DEFAULT 0,
            remessa_sequencia   INTEGER NOT NULL DEFAULT 0,
            especie_titulo      TEXT NOT NULL DEFAULT '02',
            multa_pct           REAL NOT NULL DEFAULT 0,
            juros_mes_pct       REAL NOT NULL DEFAULT 0,
            dias_protesto       INTEGER NOT NULL DEFAULT 0,
            instrucoes          TEXT,
            ambiente            TEXT NOT NULL DEFAULT 'HOMOLOGACAO' CHECK (ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
            ativo               INTEGER NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS remessas (
            id                INTEGER PRIMARY KEY AUTOINCREMENT,
            conta_bancaria_id INTEGER NOT NULL REFERENCES contas_bancarias(id),
            sequencia         INTEGER NOT NULL,
            data              TEXT NOT NULL,
            arquivo           TEXT NOT NULL,
            conteudo          TEXT NOT NULL,
            quantidade        INTEGER NOT NULL,
            valor_total       REAL NOT NULL,
            homologacao       INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS boletos (
            id                INTEGER PRIMARY KEY AUTOINCREMENT,
            conta_bancaria_id INTEGER NOT NULL REFERENCES contas_bancarias(id),
            conta_id          INTEGER REFERENCES contas(id),
            pagador_id        INTEGER REFERENCES pessoas(id),
            nosso_numero      TEXT NOT NULL,
            numero_documento  TEXT NOT NULL,
            emissao           TEXT NOT NULL,
            vencimento        TEXT NOT NULL,
            valor             REAL NOT NULL,
            codigo_barras     TEXT NOT NULL,
            linha_digitavel   TEXT NOT NULL,
            status            TEXT NOT NULL DEFAULT 'EMITIDO'
                              CHECK (status IN ('EMITIDO', 'ENVIADO', 'REGISTRADO', 'REJEITADO', 'PAGO', 'BAIXADO', 'CANCELADO')),
            remessa_id        INTEGER REFERENCES remessas(id),
            data_pagamento    TEXT,
            valor_pago        REAL,
            ocorrencia        TEXT,
            baixa_solicitada  INTEGER NOT NULL DEFAULT 0,
            UNIQUE (conta_bancaria_id, nosso_numero)
        );
        CREATE TABLE IF NOT EXISTS retornos (
            id                INTEGER PRIMARY KEY AUTOINCREMENT,
            conta_bancaria_id INTEGER NOT NULL REFERENCES contas_bancarias(id),
            data              TEXT NOT NULL,
            arquivo           TEXT NOT NULL,
            conteudo          TEXT NOT NULL,
            registros         INTEGER NOT NULL,
            liquidados        INTEGER NOT NULL,
            valor_liquidado   REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_boletos_conta ON boletos(conta_id);
        """);

    // ------------------------------------------------------------------ 010
    static void M010(DbSessao s)
    {
        foreach (var (tabela, coluna, definicao) in new[]
                 {
                     ("produtos", "cclass_trib", "TEXT"),
                     ("produtos", "cst_ibscbs", "TEXT"),
                     ("produtos", "red_ibs_pct", "REAL NOT NULL DEFAULT 0"),
                     ("produtos", "red_cbs_pct", "REAL NOT NULL DEFAULT 0"),
                     ("produtos", "aliq_is", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "cst_ibscbs", "TEXT"),
                     ("nota_itens", "cclass_trib", "TEXT"),
                     ("nota_itens", "base_ibscbs", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "aliq_ibs_uf", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "aliq_ibs_mun", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "aliq_cbs", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "valor_ibs_uf", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "valor_ibs_mun", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "valor_cbs", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "aliq_is", "REAL NOT NULL DEFAULT 0"),
                     ("nota_itens", "valor_is", "REAL NOT NULL DEFAULT 0"),
                     ("notas_fiscais", "valor_ibs", "REAL NOT NULL DEFAULT 0"),
                     ("notas_fiscais", "valor_cbs", "REAL NOT NULL DEFAULT 0"),
                     ("notas_fiscais", "valor_is", "REAL NOT NULL DEFAULT 0"),
                 })
            AdicionarColuna(s, tabela, coluna, definicao);
    }

    // ------------------------------------------------------------------ 011
    static void M011(DbSessao s)
    {
        foreach (var p in Parametros.Definicoes)
            Config(s, p.Chave, p.Padrao);
    }

    // ------------------------------------------------------------------ 012
    static void M012(DbSessao s) => Config(s, "log_dias_manter", "365");
}
