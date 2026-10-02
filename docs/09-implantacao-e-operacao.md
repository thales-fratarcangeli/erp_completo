# 09 — Implantação e operação

| | |
|---|---|
| **Documento** | Guia de implantação, atualização e operação |
| **Sistema** | ERP Loja 1.1.0 |
| **Público** | Implantadores, suporte e administrador da loja |

## 1. Requisitos

| Item | Mínimo |
|---|---|
| Sistema operacional | Windows 10 ou 11, 64 bits |
| Memória | 4 GB |
| Disco | 200 MB + crescimento do banco e dos backups |
| Tela | 1366 × 768 (recomendado 1920 × 1080 para o PDV) |
| Pré-requisitos | **Nenhum** (o executável já contém o .NET e o SQLite) |
| Internet | Só para e-mail (Resend) e CIOT (Pamcard) |

## 2. Instalação

1. Crie uma pasta de usuário, por exemplo `C:\ErpLoja` (evite `C:\Arquivos de Programas`, que é protegida contra gravação).
2. Copie `ErpLoja.exe` para ela e execute.
3. Na primeira vez, escolha **Criar banco novo** (o `loja.db` fica ao lado do executável) ou **Indicar arquivo** (banco existente).
4. Entre com **admin / admin** e troque a senha em *Configurações > Minha conta*.
5. Siga a lista de configuração inicial (seção 4).

> **SmartScreen**: como o executável não é assinado, o Windows pode mostrar "O Windows protegeu o computador".
> Use *Mais informações > Executar assim mesmo*. Para eliminar o aviso, assine o executável com certificado de assinatura de código.

### Onde o sistema procura o banco

1. O caminho lembrado neste computador (`%LOCALAPPDATA%\ErpLoja\banco.txt`).
2. `loja.db` ao lado do executável.
3. Se nada for encontrado: tela *Banco de dados não encontrado*.

## 3. Atualização de versão

1. Feche o sistema em todos os computadores.
2. Substitua o `ErpLoja.exe` pelo novo (o Windows não deixa substituir um executável aberto).
3. Abra o sistema: as migrações pendentes são aplicadas automaticamente, **com backup antes** (`backups\loja_<data>_<hora>_antes_migracao_vN_para_v11.db`).

| Situação | Comportamento |
|---|---|
| Banco antigo (até v10) aberto pela 1.1.0 | Migra até a v11 (inclui a unificação de clientes e fornecedores em pessoas) |
| Banco v11 aberto por executável antigo | **Recusado** — use o executável mais novo |
| Falha numa migração | A migração é desfeita; o banco fica na versão anterior; restaure o backup se necessário |

Voltar para uma versão anterior: feche o sistema, restaure o backup `antes_migracao` e use o executável antigo.

## 4. Configuração inicial (lista de verificação)

Tudo em *Configurações* (somente administrador):

| Aba | Itens | Necessário para |
|---|---|---|
| Empresa | Razão social, CNPJ, IE, endereço, UF | Notas (chave de acesso, CFOP), boletos, CIOT |
| Fiscal | Regime, séries e próximos números, naturezas, CST padrão | Notas fiscais |
| Reforma tributária | Ligar/desligar, alíquotas CBS/IBS, CST e cClassTrib padrão | IBS/CBS nas notas — **confirmar alíquotas com o contador a cada ano** |
| Vendas e compras | Venda sem estoque, desconto máximo, validade de orçamento, prazos, parcelas, número do caixa, impressão do comprovante | PDV, pedidos |
| Produção | Concluir sem insumo | Ordens de produção |
| Transporte e CIOT | RNTRC, administradora, ambiente, credenciais Pamcard | Fretes com CIOT |
| TEF | Ativo, provedor, via da loja | Cartão/PIX no PDV |
| Impressoras | Impressora de cupom, tipo (WINDOWS/ESCPOS), colunas, página de código, corte, gaveta; impressora de documentos | Comprovantes e documentos — use **Imprimir página de teste** |
| E-mail | Chave do Resend, remetente | Recuperação de senha — use **Enviar e-mail de teste** |
| Banco de dados | Backup diário, quantidade mantida, backup agora, trocar de banco, dados de demonstração | Operação |

Depois: cadastre usuários (com e-mail), categorias, produtos (NCM, CST, campos da reforma), pessoas, veículos e contas bancárias.

## 5. Homologações com parceiros

| Parceiro | Passos |
|---|---|
| Banco (cobrança) | Cadastrar a conta bancária em ambiente HOMOLOGACAO com o modelo de campo livre do manual do banco → emitir boletos de teste → gerar remessa → validar no validador CNAB 240 → enviar ao banco → importar o retorno do banco → após aprovação, mudar a conta para PRODUCAO |
| Pamcard (CIOT) | Credenciamento → receber URL, usuário e senha de homologação → configurar → declarar CIOT de teste → conferir o leiaute com o manual técnico da Pamcard → mudar o ambiente para PRODUCAO com as credenciais definitivas |
| TEF | Contratar provedor (SiTef/PayGo) → implementar o provedor (`IProvedorTef`) → homologar o roteiro do provedor. Até lá, use o SIMULADOR para treino |

## 6. Rotina de operação

| Rotina | Frequência | Como |
|---|---|---|
| Backup automático | Diário (ao abrir) | `backups\loja_*_diario.db`; mantém a quantidade configurada (padrão 15) |
| Cópia externa | Semanal (recomendado) | Copiar a pasta `backups` para pendrive/nuvem — o banco **não** deve ficar em pasta sincronizada |
| Inventário | Conforme a loja | *Estoque > Ajustar > Definir saldo* |
| Conferência de retornos bancários | Diária, se usa cobrança | *Cobrança > Importar retorno* |
| Verificação de alíquotas da reforma | Anual (e quando a legislação mudar) | *Configurações > Reforma tributária* |

### Restaurar um backup
1. Feche o sistema.
2. Copie o backup desejado por cima do `loja.db` (ou use *Configurações > Banco de dados > Trocar de banco*).
3. Abra o sistema; se o backup for de versão anterior, as migrações rodam de novo.

## 7. Linha de comando (suporte e automação)

| Comando | Efeito | Código de saída |
|---|---|---|
| `ErpLoja.exe` | Abre normalmente | — |
| `ErpLoja.exe --criar-banco [caminho]` | Cria (ou atualiza) o banco sem abrir a interface; padrão: `loja.db` ao lado do exe | 0 ok · 2 erro |
| `ErpLoja.exe --dados-demo <caminho>` | Migra o banco e insere dados fictícios de demonstração (clientes, produtos, 6 meses de movimento) | 0 ok · 2 erro |

Variável de ambiente `ERPLOJA_PREFERENCIAS`: muda a pasta onde o caminho do banco é lembrado (usada nos testes; útil para
manter perfis separados no mesmo usuário do Windows).

## 8. Arquivos do sistema

| Arquivo | Local | Conteúdo |
|---|---|---|
| `ErpLoja.exe` | Pasta escolhida | Aplicação |
| `loja.db` | Ao lado do exe (padrão) ou indicado | Todos os dados da loja, inclusive parâmetros e credenciais |
| `backups\*.db` | Pasta do banco | Backups diários, manuais e antes de migrações |
| `banco.txt` | `%LOCALAPPDATA%\ErpLoja` | Caminho do último banco |

**Proteja a pasta do banco**: ela contém dados de clientes e credenciais de integração (ver [SECURITY.md](../SECURITY.md)).

## 9. Problemas

Veja [10 — Falhas e resoluções](10-falhas-e-resolucoes.md).
