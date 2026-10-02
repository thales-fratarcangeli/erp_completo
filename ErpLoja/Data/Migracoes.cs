using Microsoft.Data.Sqlite;

namespace ErpLoja.Data;

/// <summary>Uma alteração versionada do banco. Nunca altere uma migração já publicada: crie outra.</summary>
public record Migracao(int Versao, string Nome, Action<DbSessao> Aplicar);

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
            using var s = new DbSessao(transacao: true);
            try
            {
                m.Aplicar(s);
                s.Exec("INSERT INTO __migracoes(versao, nome, aplicada_em) VALUES(@p0, @p1, @p2)", m.Versao, m.Nome, DateTime.Now);
                s.Exec($"PRAGMA user_version = {m.Versao}");
                s.Commit();
            }
            catch (Exception ex)
            {
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
}
