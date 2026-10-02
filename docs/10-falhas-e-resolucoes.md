# 10 — Falhas e resoluções

| | |
|---|---|
| **Documento** | Base de conhecimento de suporte e registro de falhas de desenvolvimento |
| **Sistema** | ERP Loja 1.2.0 |

Duas partes: **(1) problemas que podem acontecer no uso** e como resolver; **(2) falhas encontradas durante o desenvolvimento**
e como foram resolvidas (registro técnico, para não repetir).

---

## 1. Problemas no uso

### "O Windows protegeu o computador" ao abrir o ErpLoja.exe
- **Causa:** o executável não tem assinatura digital de código.
- **Resolução:** *Mais informações > Executar assim mesmo*. Para eliminar o aviso em definitivo é preciso assinar o executável com um certificado de assinatura de código.

### "Banco de dados não encontrado"
- **Causa:** primeira execução, o `loja.db` foi movido/renomeado, ou o caminho lembrado (pasta de rede, pendrive) está indisponível.
- **Resolução:** *Indicar o arquivo do banco* e apontar o `loja.db` correto; ou *Criar banco novo* se for uma instalação nova.
  O caminho lembrado fica em `%LOCALAPPDATA%\ErpLoja\banco.txt` (apagar esse arquivo faz o sistema voltar a procurar ao lado do executável).

### Não consegue criar o banco ao lado do executável
- **Causa:** a pasta é protegida (ex.: `C:\Arquivos de Programas`).
- **Resolução:** o próprio sistema pede outro local. Recomendado: manter o executável e o banco numa pasta do usuário, como `C:\ErpLoja` ou `Documentos\ErpLoja`.

### "O arquivo não é um banco do ERP Loja"
- **Causa:** foi indicado outro arquivo `.db` (de outro programa) ou um arquivo corrompido.
- **Resolução:** indicar o `loja.db` correto ou um arquivo da pasta `backups`.

### "Este banco está na versão X, mais nova que a deste executável"
- **Causa:** o banco foi aberto por uma versão mais nova do sistema e alguém usou um executável antigo.
- **Resolução:** usar o `ErpLoja.exe` mais recente. O sistema recusa abrir para não corromper dados.

### Falha ao aplicar uma migração
- **O que acontece:** a migração que falhou é desfeita por completo (transação); as anteriores ficam aplicadas. A mensagem informa o backup feito antes.
- **Resolução:** anote a mensagem e envie ao suporte. Para voltar ao estado anterior: feche o sistema, copie o arquivo de `backups\..._antes_migracao_...db` por cima do `loja.db`.

### "database is locked" / sistema lento ao gravar
- **Causa:** outro programa (antivírus, backup em nuvem sincronizando o arquivo, outra estação) está com o arquivo aberto.
- **Resolução:** o sistema espera até 10 segundos antes de desistir. Exclua a pasta do banco da verificação em tempo real do antivírus e não deixe o `loja.db` dentro de pastas sincronizadas (OneDrive, Google Drive, Dropbox) — use os backups do sistema.

### Banco em pasta de rede
- O SQLite funciona em rede para uso leve, mas **não é recomendado para vários caixas gravando ao mesmo tempo** (risco de travamentos e, com redes instáveis, de corrupção).
- Para vários computadores, o caminho seguro é evoluir para um banco cliente-servidor (PostgreSQL/SQL Server) — mudança de arquitetura, não de configuração.

### Restaurar um backup
1. Feche o sistema em todos os computadores.
2. Copie o arquivo desejado de `backups\` para o lugar do `loja.db` (ou use *Configurações > Banco de dados > Trocar de banco* apontando para ele).
3. Abra o sistema. Se o backup for de uma versão anterior, as migrações são aplicadas de novo automaticamente.

### Recuperação de senha não envia o e-mail
| Mensagem | Causa | Resolução |
|---|---|---|
| "O envio de e-mail não está configurado" | Chave do Resend ou remetente em branco | Administrador preenche *Configurações > E-mail* |
| "Este usuário não tem e-mail cadastrado" | Usuário sem e-mail | Administrador cadastra o e-mail em *Cadastros > Usuários* (ou redefine a senha ali) |
| "O Resend recusou o envio (403): The domain is not verified" | Domínio do remetente não verificado no Resend | Verificar o domínio no painel do Resend ou usar um remetente de domínio verificado |
| "O Resend recusou o envio (401)..." | Chave inválida/revogada | Gerar nova chave em resend.com > API Keys |
| "Não foi possível falar com o Resend" | Sem internet / firewall | Verificar a conexão; liberar `api.resend.com` (HTTPS 443) |

Use *Configurações > E-mail > Enviar e-mail de teste* para validar a configuração.

### Esqueci a senha do administrador e ele não tem e-mail
- Se houver outro administrador: ele redefine a senha em *Cadastros > Usuários* (senha nova no campo Senha).
- Se não houver: restaure um backup anterior em que a senha era conhecida. **Limitação conhecida:** não existe redefinição sem e-mail e sem outro administrador. Cadastre o e-mail de todos os administradores.

### Nota fiscal não emite
- A mensagem lista o que falta (NCM com 8 dígitos, CFOP com 4 dígitos, CST/CSOSN, destinatário, natureza).
- Preencha NCM/CST no **cadastro do produto** para não precisar digitar em cada nota.
- **Chave de acesso em branco:** CNPJ (14 dígitos) ou UF da empresa não configurados em *Configurações > Empresa*.
- Lembrete: a nota não é transmitida à SEFAZ (limitação conhecida).

### Importação de XML
| Situação | Causa | Resolução |
|---|---|---|
| "O arquivo não é um XML de NF-e" | Arquivo de outro tipo (ex.: XML de evento/cancelamento, PDF renomeado) | Pedir ao fornecedor o XML da nota (procNFe) |
| "Esta nota já foi importada" | Mesma chave de acesso | Nada a fazer; ela já está em *Notas fiscais* |
| Estoque entrou com quantidade errada | Unidade do fornecedor diferente da sua (caixa x unidade) | Cancelar a compra, reimportar ajustando o **Fator**. O fator fica memorizado |
| Produto novo com preço zero | Comportamento esperado | Ajustar o preço de venda em *Cadastros > Produtos* |

### Estoque negativo
- **Causas possíveis:** venda sem estoque permitida/confirmada; cancelamento de compra após vender os itens; entrada lançada com quantidade menor.
- **Resolução:** conferir o histórico em *Estoque* e fazer *Ajustar estoque > Definir saldo (inventário)* com a contagem física.

### Cancelei a venda mas a nota continua emitida
- Comportamento esperado: venda e nota são documentos separados. Cancele a nota em *Notas fiscais*.

### TEF (cartão e PIX no PDV)
| Situação | Causa | Resolução |
|---|---|---|
| "TEF SITEF/PAYGO não disponível: ..." | Provedor real escolhido sem a integração do provedor | Usar o SIMULADOR (treino) ou desligar o TEF em *Configurações > TEF* e registrar o cartão manualmente |
| Transação aprovada mas a venda não gravou | Falha ao gravar (ex.: banco bloqueado) | Automático: a transação é **desfeita** (situação DESFEITA); o cliente não é cobrado |
| Cancelei a venda paga com cartão | — | O cancelamento da venda estorna a transação TEF (situação CANCELADA) |
| Comprovante não sai | Impressora de cupom não configurada | Ver "Impressoras" abaixo; o comprovante também aparece na tela |

### CIOT e fretes
| Situação | Causa | Resolução |
|---|---|---|
| "A integração com a Pamcard não está configurada..." | Credenciais em branco | Preencher em *Configurações > Transporte e CIOT* com os dados do credenciamento |
| "A integração com X ainda não foi implementada" | e-Frete/Repom/Target escolhida | Usar PAMCARD ou SIMULADOR; ou declarar no portal da administradora |
| "Corrija antes de declarar o CIOT" (CPF do motorista, RNTRC, origem/destino, valores) | Cadastro ou frete incompleto | Completar a pessoa (papel motorista), o veículo/transportador (RNTRC) e o frete |
| "Cancele o CIOT antes de cancelar o frete." | CIOT declarado | Cancelar o CIOT primeiro (com motivo), depois o frete |
| Pamcard devolve erro de leiaute | Leiaute do adaptador diferente do manual vigente | Ajustar `PamcardAdministradora.MontarDeclaracao/Interpretar` conforme o manual técnico (só o adaptador muda — [ADR-0012](adr/0012-ciot-administradora-abstrata.md)) |

### Cobrança bancária
| Situação | Causa | Resolução |
|---|---|---|
| "Modelo do campo livre inválido na conta bancária." | Marcações erradas ou soma diferente de 25 posições | Corrigir o modelo da conta conforme o manual do banco (ex.: `{agencia:4}{carteira:2}{nossonumero:11}{conta:7}{zeros:1}`) |
| Banco recusa a remessa | Posições específicas do banco | Validar no validador CNAB 240; comparar com o manual do banco; homologar antes de usar em produção |
| "O retorno é do banco X, e a conta selecionada é do banco Y." | Arquivo de outra conta/banco | Selecionar a conta bancária correta antes de importar |
| Retorno importado não baixou contas | Nosso número diferente / ocorrência não é liquidação | Conferir as ocorrências do retorno (02 entrada confirmada, 06 liquidação, 09 baixa) |
| Linha digitável não lê no banco | Fator de vencimento, valor ou campo livre | Conferir o vencimento (a partir de 22/02/2025 o fator reinicia em 1000) e o modelo da conta |

### Produção
| Situação | Resolução |
|---|---|
| Falta de insumo ao concluir a OP | Comprar/ajustar o estoque do insumo ou mudar *Configurações > Produção* (perguntar/permitir) |
| Custo do produto produzido estranho | Conferir o custo médio dos insumos, as perdas e o rendimento da ficha técnica |
| Produto não pode ser insumo de si mesmo | Comportamento esperado (evita ciclo) |

### Impressoras
| Situação | Causa | Resolução |
|---|---|---|
| Cupom sai com acentos trocados | Página de código diferente da impressora | Trocar entre 850 e 860 em *Configurações > Impressoras* |
| Cupom sai com símbolos estranhos | Tipo ESCPOS numa impressora que não entende ESC/POS | Usar o tipo WINDOWS |
| Texto cortado ou quebrado | Colunas erradas | 80 mm = 48 (ou 42) colunas; 58 mm = 32 |
| Gaveta não abre | Impressora não ESC/POS ou parâmetro desligado | Tipo ESCPOS e *Abrir a gaveta* = SIM |

Use **Imprimir página de teste** em *Configurações > Impressoras*.

### Reforma tributária
- **IBS/CBS zerados**: reforma desligada, alíquotas zeradas ou produto com CST de isenção/imunidade.
- **"reforma tributária exige CST IBS/CBS (3 dígitos) e cClassTrib (6 dígitos)"**: preencher no produto (ou definir os padrões em *Configurações > Reforma tributária*).
- Em 2026 os valores são **destacados, mas não somam ao total** (ano de teste). Confirme as alíquotas com o contador.

### Dados de demonstração
- Gere **somente em banco de teste**: o sistema faz backup antes, mas os dados fictícios se misturam aos reais.
- Por linha de comando: `ErpLoja.exe --dados-demo C:\teste\demo.db`.

### Telas e abas (1.2)
| Situação | Causa | Resolução |
|---|---|---|
| "Cadê a janela que eu abri?" | As telas abrem como abas à direita do menu, não mais como janelas soltas | Clique na aba no topo; Ctrl+Tab alterna; abrir de novo pelo menu traz a aba para a frente |
| Não consigo fechar o sistema | Há venda em andamento no PDV | Finalize ou cancele a venda (F7) e feche de novo; o sistema mostra a aba que impediu |
| Atalho F2/F5 não abre a tela esperada | O PDV está em primeiro plano e usa as teclas de função para si | Troque de aba (Ctrl+Tab) ou use o menu |

### Log de atividades
| Situação | Causa | Resolução |
|---|---|---|
| Tela de log vazia | Período sem registros ou banco de log ainda não criado | Ajuste o período; o arquivo `<banco>_log.db` é criado na primeira ação |
| Log cresceu demais | Retenção alta ou 0 (manter tudo) | *Configurações > Banco de dados > Dias de log mantidos*; a limpeza ocorre ao abrir o sistema |
| Precisa auditar outro computador | Cada banco tem o seu log ao lado | Abra o mesmo banco; o log mostra a coluna Máquina |

### Atalho na área de trabalho
- **Não apareceu**: o sistema só cria na primeira execução de cada usuário do Windows. Para criar de novo, apague `%LOCALAPPDATA%\ErpLoja\atalho_criado.txt` e abra o sistema.
- **Aponta para o lugar antigo** (executável movido): apague o atalho e o marcador acima e abra o executável no novo local.

---

## 2. Registro de falhas no desenvolvimento

| # | Falha | Causa | Resolução |
|---|---|---|---|
| 1 | Não era possível compilar | A máquina tinha só o runtime do .NET, sem SDK | SDK .NET 10 instalado no perfil do usuário (`%USERPROFILE%\.dotnet`) via `dotnet-install.ps1` |
| 2 | `NU1100: Não foi possível resolver Microsoft.Data.Sqlite` | `NuGet.Config` do usuário sem nenhuma fonte de pacotes | `nuget.config` na raiz do repositório apontando para nuget.org (sem alterar a configuração global) |
| 3 | Alerta `NU1903` de vulnerabilidade alta em `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (GHSA-2m69-gcr7-jv3q) | Versão do SQLite nativo trazida por Microsoft.Data.Sqlite 10.0.0 | Atualizado para Microsoft.Data.Sqlite 10.0.12 (SQLitePCLRaw 2.1.12) |
| 4 | Erro `WFO1000` (propriedade sem configuração de serialização) | Analisador do WinForms exige atributo em propriedades públicas de controles | `[DesignerSerializationVisibility(Hidden)]` nas propriedades que não são de designer |
| 5 | Acentos corrompidos ("formulÃ¡rio") em arquivos do projeto | O PowerShell 5.1 lê UTF-8 sem BOM como ANSI e regrava convertido | Arquivos reconvertidos (1252 → UTF-8). Regra: editar fontes só com ferramentas que preservam UTF-8 |
| 6 | Valores decimais gravados como texto no SQLite | `Microsoft.Data.Sqlite` envia `decimal` como TEXT | Conversão de `decimal` para `double` em todos os parâmetros (`Db.Converter`) |
| 7 | Grades ordenando números como texto / erros de restrição ao carregar | Tipagem dinâmica do SQLite; `DataTable.Load` herdava chaves das tabelas | `DbSessao.Query` monta o `DataTable` à mão, deduzindo o tipo de cada coluna |
| 8 | Teste de vínculo por código de barras falhando | O dado de teste usava EAN de 6 dígitos (inválido) | Corrigido o dado de teste; a regra (EAN de 8/12/13/14 dígitos) foi mantida |
| 9 | DANFE com linhas vazias (" - /SP Fone:") e cabeçalho cortado | Campos da empresa em branco concatenados sem filtro | Montagem só com as partes preenchidas; cabeçalho "VALOR TOTAL" cabe na coluna |
| 10 | Rótulo "Endereço:" separado da caixa no editor de nota | Quebra automática do `FlowLayoutPanel` | Quebra de linha forçada antes do endereço (`SetFlowBreak`) |
| 11 | Prazos fixos (15/30 dias) ignorando os parâmetros em "Duplicar orçamento" e na importação de XML sem duplicatas | Valores escritos no código antes de existirem os parâmetros | Passaram a usar `validade_orcamento_dias` e `prazo_vencimento_dias` |
| 12 | Banco antigo sem controle de versão | Versões 1.x usavam só `PRAGMA user_version` | `Migracoes.RegistrarLegado` reconhece bancos antigos e registra as migrações já contidas neles; testado com banco v1 real |
| 13 | Coluna `login` precisava virar `username` sem perder dados | Novo requisito de cadastro de usuário | Migração 3 com `ALTER TABLE ... RENAME COLUMN` |
| 14 | Botão "?" não aparecia nas capturas de tela dos testes | `Control.DrawToBitmap` desenha os controles filhos em ordem invertida | Verificado visualmente com captura real; o teste passou a conferir o botão pela estrutura da tela (visível, na frente, dentro da janela) |
| 15 | Captura real de tela nos testes registrou outras janelas da área de trabalho | `CopyFromScreen` copia o que está na tela durante a animação de abertura | Voltou-se ao `DrawToBitmap` (só desenha a própria janela) e as capturas indevidas foram apagadas |
| 16 | Menu lateral com barra de rolagem em monitores de 768 px | Itens demais na coluna | Cadastros agrupados num submenu |
| 17 | Textos de comparação cortados nos indicadores do painel | Cartões estreitos com o menu lateral | Textos curtos ("▲ 3,2% vs anterior") e reticências automáticas |
| 18 | Projeto de testes não carregava (`MSB4025`) | Comentário XML no `.csproj` continha `--` | Texto do comentário ajustado |
| 19 | `ErpLoja.exe --criar-banco <caminho>` encerrava com erro (0xE0434352) quando a pasta não existia | A linha de comando chamava a migração direto, sem criar a pasta (a tela de abertura criava) | A pasta passa a ser criada; erros devolvem código de saída 2 em vez de derrubar o programa |
| 20 | Não foi possível substituir `dist\ErpLoja.exe` ao publicar (`Access denied`) | O sistema estava aberto, e o Windows bloqueia o executável em uso | Publicar com o sistema fechado (ou em outra pasta e copiar depois) |
| 21 | Funções duplicadas em várias telas (compra manual em duas telas, importação de XML em dois menus, notas a partir de três lugares, indicadores repetidos na tela inicial e no dashboard, relatórios repetindo telas) | Crescimento incremental do sistema | Revisão de redundância: um ponto de entrada por função (ver tabela abaixo) |
| 22 | Logo gerada saiu laranja em vez de azul | Bytes de cor gravados na ordem R,G,B; o bitmap usa B,G,R | Ordem corrigida no script `ferramentas/gerar-logo.ps1` |
| 23 | Nome `Ui.Sucesso` conflitava com a cor de mesmo nome | Método e campo com o mesmo identificador | Método renomeado para `Ui.Feito` |
| 24 | Erro ao abrir o editor de nota: "A coluna DataGridView associada a um campo somente leitura deve ter ReadOnly definida como True" (relatado pelo usuário) | Colunas calculadas eram marcadas `ReadOnly = false` por regra genérica | O editor não força mais o `ReadOnly` das colunas; as calculadas seguem a fonte de dados |
| 25 | Migração para `pessoas` poderia apagar itens de venda | Apagar a tabela-pai com chaves estrangeiras ligadas dispara `ON DELETE CASCADE` | Migrações de reconstrução rodam com FKs desligadas (`semChavesEstrangeiras`); `RepontarReferencias` recria as tabelas filhas e `foreign_key_check` valida antes do commit; teste com banco v4 com dados |
| 26 | Risco de a interface quebrar no executável ofuscado (grades vazias, ajuda não encontrada, JSON do Resend vazio) | A ofuscação renomeia propriedades ligadas por nome, chaves de ajuda baseadas em nome de tipo e tipos anônimos | `[Obfuscation(Exclude = true)]` nas classes ligadas a grades e no P/Invoke; chaves de ajuda com `nameof`; JSON com `JsonObject`; teste de interface no exe ofuscado |
| 27 | Obfuscar não encontrava as referências do WinForms | Caminhos de busca de assemblies ausentes | O alvo de publicação gera `obfuscar.xml` com os diretórios de todas as DLLs resolvidas |
| 28 | Teste de interface travava ao clicar em botão que abre janela modal | `InvokePattern.Invoke` (UI Automation) só retorna quando o modal fecha | Clique enviado com `PostMessage(BM_CLICK)` |
| 29 | Teste de interface não achava as janelas filhas | Janelas com dono ficam aninhadas sob a janela dona na árvore de automação | Busca em largura pelas janelas filhas |
| 30 | Script de teste com acentos falhava no PowerShell 5.1 | Arquivo UTF-8 sem BOM lido como ANSI | Script gravado em UTF-8 com BOM |
| 31 | Tela inicial "poluída" com indicadores (relatado pelo usuário) | Painel gerencial embutido na tela inicial | Tela inicial limpa (logo, saudação, busca); indicadores na tela *Painel gerencial* (F5) |
| 32 | Botão "?" abria a ajuda de todas as telas (relatado pelo usuário) | Janela de ajuda com índice de tópicos | A ajuda mostra apenas o tópico da tela de origem; teste verifica |
| 33 | Comandos longos com "heredoc" falhavam no terminal ("unexpected EOF") | Aspas e acentos dentro do bloco confundiam o interpretador do terminal da ferramenta | Scripts de edição gravados em arquivos `.py` e executados |
| 34 | Teste de interface: "Os tipos de argumento não correspondem" | `New-Object OrCondition(a, b)` no PowerShell 5.1 não monta o vetor de condições | `-ArgumentList (, [Condition[]]@(a, b))` |
| 35 | Teste de interface: mesmo erro ao percorrer a lista de janelas | `@($lista)` sobre `List<object>` no PowerShell 5.1 | `$lista.ToArray()` |
| 36 | Risco de tela embutida invisível/preta | A entrada suave (Opacity) usa janela em camadas, que não se aplica a janelas filhas | Transparência só em janelas soltas (`TopLevel`) |
| 37 | Aviso "usuário sem e-mail" travava testes sem supervisão (relatado pelo usuário) | `MessageBox` modal após o login | Virou notificação não bloqueante; o teste de interface fecha sozinho caixas informativas |
| 38 | Cenários automatizados travariam em qualquer caixa de mensagem ou janela modal | `MessageBox.Show` e `ShowDialog` esperam clique | `Ui.Interceptar` nos testes; `Usuario.QuandoAbrir` responde janelas esperadas e cancela (e reprova) as inesperadas após 2 s |
| 39 | Primeira rodada de agentes de teste parou no meio | Limite de uso da sessão (HTTP 429) e cópias de trabalho criadas a partir do último commit **enviado** ao GitHub, sem os commits locais (infraestrutura de cenários ausente) | Commit + push antes de lançar; agentes relançados com prazo e commits frequentes |
| 40 | Cenário de exemplo não compilava (`Ui` desconhecido) | Projeto de testes não tem os `global using` do sistema | `using ErpLoja.UI;` no arquivo do cenário (documentado nas instruções aos agentes) |

### Revisão de redundância (versão 1.0.0)

| Função | Antes | Agora (único lugar) |
|---|---|---|
| Entrada manual de mercadoria | "Nova compra" e "Pedido de compra > Receber" | Pedido de compra (com "Salvar e receber" para entrada imediata) |
| Importar XML | Menu Fiscal e tela de Notas | Menu Compras > Importar XML de NF-e |
| Criar nota de venda/compra | Vendas realizadas, Compras realizadas e Notas | Notas fiscais (e a sugestão automática ao faturar um pedido) |
| Indicadores gerenciais | Cartões da tela inicial + janela Dashboard | Painel na tela inicial |
| Dados da empresa, senha, backup | Itens soltos no menu Sistema | Tela única de Configurações |
| Relatórios "estoque baixo", "inventário", "contas vencidas", "por forma de pagamento", "por categoria" | Repetiam a tela Estoque, o Financeiro e o painel | Removidos dos relatórios; ficam nas telas de origem |
| Navegação | Menu superior + barra de botões com os mesmos itens | Menu lateral único |

### Revisão de redundância (versão 1.1.0)

| Função | Antes | Agora (único lugar) |
|---|---|---|
| Clientes e fornecedores | Duas telas e duas tabelas | *Cadastros > Pessoas* com papéis (cliente, fornecedor, transportador, motorista) |
| Indicadores gerenciais | Tela inicial | *Painel gerencial* (F5) |
| Ajuda | Janela com todos os tópicos | Um tópico por tela, no "?"/F1 da própria tela |
| Menu, atalhos e localizador | Listas separadas | Registro único `Navegacao.Telas()` |
