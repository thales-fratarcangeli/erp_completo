# 09 — Implantação e operação

| | |
|---|---|
| **Documento** | Guia de implantação, atualização e operação |
| **Sistema** | ERP Loja 1.5.0 |
| **Público** | Implantadores, suporte e administrador da loja |

## 1. Requisitos

| Item | Mínimo |
|---|---|
| Sistema operacional | Windows 10 ou 11, 64 bits |
| Memória | 4 GB (8 GB ou mais se usar o assistente com o Ollama no mesmo computador) |
| Disco | 200 MB + crescimento do banco e dos backups (o Ollama e o modelo local ocupam alguns GB à parte) |
| Tela | 1366 × 768 (recomendado 1920 × 1080 para o PDV) |
| Pré-requisitos | **Nenhum** (o executável já contém o .NET e o SQLite) |
| Internet | Só para e-mail (Resend), CIOT (Pamcard) e o assistente de dúvidas com o provedor CLAUDE (API da Anthropic) |

## 2. Instalação

1. Crie uma pasta de usuário, por exemplo `C:\ErpLoja` (evite `C:\Arquivos de Programas`, que é protegida contra gravação).
2. Copie `ErpLoja.exe` para ela e execute.
3. Na primeira vez, escolha **Criar banco novo** (o `loja.db` fica ao lado do executável) ou **Indicar arquivo** (banco existente).
4. Entre com **admin / admin** e troque a senha em *Configurações > Minha conta*.
   Na primeira execução o sistema cria o atalho **ERP Loja** na área de trabalho.
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
3. Abra o sistema: as migrações pendentes são aplicadas automaticamente, **com backup antes**
   (`backups\loja_<aaaammdd>_<hhmmss>_antes_migracao_vN_para_v16.db`, em que N é a versão em que o banco estava).

| Situação | Comportamento |
|---|---|
| Banco antigo (até v10) aberto pela 1.1.0 ou posterior | Migra até a versão do executável (inclui a unificação de clientes e fornecedores em pessoas, migração 11) |
| Banco da 1.4.x (v14) aberto pela 1.5.0 | Migra até a v16 (ver abaixo) |
| Banco v16 aberto por executável antigo | **Recusado** ("Este banco está na versão 16, mais nova que a deste executável") — use o executável mais novo |
| Falha numa migração | A migração que falhou é desfeita; as anteriores ficam aplicadas; restaure o backup se necessário |

### 3.1 Da 1.4.x para a 1.5.0

Basta trocar o `ErpLoja.exe`. Ao abrir, o sistema faz o backup `..._antes_migracao_v14_para_v16.db` e aplica:

| Migração | O que faz | Efeito para a loja |
|---|---|---|
| 15 — Assistente de dúvidas (IA): parâmetros | Grava os parâmetros `ia_*` com os padrões (`ia_ativa` = NAO, provedor CLAUDE, modelo `claude-opus-5-5`, 30 perguntas por hora, Ollama em `http://localhost:11434`) | Nada muda até um administrador ligar o assistente (seção 4.1) |
| 16 — Ordens de serviço e contatos de cobrança | Cria as tabelas `ordens_servico`, `os_historico` e `cobranca_contatos`, a coluna `os_id` em `contas` e o parâmetro `os_prazo_dias` (padrão 3) | Habilita *Vendas > Ordens de serviço*, a baixa em lote e o acompanhamento de cobrança no Financeiro |

Nenhuma das duas reconstrói tabelas existentes nem altera dados já gravados. Depois de atualizar:

1. Confira em *Sistema > Log de atividades* (categoria SISTEMA) as linhas "Migração 15 aplicada" e "Migração 16 aplicada", com o nome do backup.
2. Ajuste o prazo padrão da OS (seção 4.2) se 3 dias não servir para a loja.
3. Se quiser o assistente, configure-o (seção 4.1); senão, não é preciso fazer nada — ele vem desligado.

Voltar para uma versão anterior: feche o sistema, restaure o backup `antes_migracao` e use o executável antigo
(o que foi lançado depois da atualização, como OS e contatos de cobrança, fica só no banco novo).

## 4. Configuração inicial (lista de verificação)

Tudo em *Configurações* (somente administrador):

| Aba | Itens | Necessário para |
|---|---|---|
| Empresa | Razão social, CNPJ, IE, endereço, UF | Notas (chave de acesso, CFOP), boletos, CIOT |
| Fiscal | Regime, séries e próximos números, naturezas, CST padrão | Notas fiscais |
| Reforma tributária | Ligar/desligar, alíquotas CBS/IBS, CST e cClassTrib padrão | IBS/CBS nas notas — **confirmar alíquotas com o contador a cada ano** |
| Vendas e compras | Venda sem estoque, desconto máximo, validade de orçamento, prazos, **prazo padrão da ordem de serviço**, parcelas, número do caixa, impressão do comprovante | PDV, pedidos, ordens de serviço |
| Produção | Concluir sem insumo | Ordens de produção |
| Transporte e CIOT | RNTRC, administradora, ambiente, credenciais Pamcard | Fretes com CIOT |
| TEF | Ativo, provedor, via da loja | Cartão/PIX no PDV |
| Impressoras | Impressora de cupom, tipo (WINDOWS/ESCPOS), colunas, página de código, corte, gaveta; impressora de documentos | Comprovantes e documentos — use **Imprimir página de teste** |
| E-mail | Chave do Resend, remetente | Recuperação de senha — use **Enviar e-mail de teste** |
| Assistente (IA) | Ligado, provedor, chave da API, modelo, limite por hora, endereço e modelo do Ollama | Assistente de dúvidas — use **Testar assistente** (seção 4.1) |
| Banco de dados | Backup diário, quantidade mantida, backup agora, trocar de banco, dados de demonstração | Operação |

Depois: cadastre usuários (com e-mail), categorias, produtos (NCM, CST, campos da reforma), pessoas, veículos e contas bancárias.

### 4.1 Assistente de dúvidas (IA)

O assistente (botão **✦ Assistente** no topo da janela ou **Ctrl+I**) responde dúvidas sobre **como usar o sistema**,
só com base na ajuda das telas. Ele não recebe dados da loja, não executa ações e vem **desligado**. Regras em
[04 — RN-IA](04-regras-de-negocio.md), segurança em [15](15-seguranca-owasp.md) e decisão em
[ADR-0021](adr/0021-assistente-ia-somente-documentacao.md).

| Parâmetro | Padrão | Para que serve |
|---|---|---|
| `ia_ativa` — Assistente de dúvidas ligado | NAO | Liga/desliga para todos os usuários |
| `ia_provedor` — Provedor de IA | CLAUDE | CLAUDE (API da Anthropic, pela internet) ou OLLAMA (modelo neste computador) |
| `ia_chave_api` — Chave da API da Anthropic | (vazia) | Só para CLAUDE; mascarada na tela e gravada cifrada (DPAPI) |
| `ia_modelo` — Modelo do Claude | `claude-opus-5-5` | Para reduzir custo: `claude-sonnet-5-5` ou `claude-haiku-4-5` |
| `ia_limite_hora` — Perguntas por hora por usuário | 30 | Limita o uso e o custo (mínimo 1) |
| `ia_ollama_url` — Ollama: endereço | `http://localhost:11434` | Só para OLLAMA; aceita `http://localhost` ou `https://` |
| `ia_ollama_modelo` — Ollama: modelo | (vazio) | Só para OLLAMA; nome do modelo baixado (ex.: `llama3.1`) |

#### Provedor CLAUDE (API da Anthropic)

1. Crie uma conta em **console.anthropic.com**, cadastre a forma de pagamento e gere uma chave em *API Keys*.
   Use uma chave só para a loja, para poder revogá-la sem afetar outros usos.
2. No ERP: *Configurações > Assistente (IA)* → *Assistente de dúvidas ligado* = **SIM**, *Provedor de IA* = **CLAUDE**,
   cole a chave em *Chave da API da Anthropic* (não aceita espaços) e mantenha ou troque o modelo.
3. Ajuste *Perguntas por hora por usuário* conforme o orçamento.
4. Clique em **Testar assistente**: o sistema salva a aba e pergunta "Onde eu cadastro um produto novo?". Se aparecer
   "O assistente respondeu: ...", está pronto. Se aparecer uma mensagem de erro, veja
   [10 — Assistente de dúvidas](10-falhas-e-resolucoes.md#assistente-de-dúvidas-ia). O teste conta como uma pergunta
   no limite por hora e fica no log.

**Custo.** A cobrança é da Anthropic, **por uso** (quantidade de texto enviada e recebida, conforme a tabela de preços
do modelo escolhido), na conta do console — o ERP não cobra nada. Cada pergunta leva junto o manual do sistema; esse
trecho é igual em toda pergunta e vai marcado para cache, o que barateia as perguntas seguidas. Para controlar o gasto:
limite por hora baixo, modelo mais barato (`claude-sonnet-5-5` ou `claude-haiku-4-5`) e limite de gastos definido no
próprio console da Anthropic. Acompanhe o consumo no console.

**Internet, proxy e firewall.** O provedor CLAUDE precisa de saída **HTTPS (porta 443) para `api.anthropic.com`**:

- Libere esse domínio no firewall/antivírus/filtro de conteúdo da loja (é o único endereço usado pelo assistente).
- O sistema usa o proxy configurado no Windows (padrão do .NET). Se a rede exige proxy com autenticação ou faz
  inspeção de HTTPS, peça ao responsável pela rede para liberar `api.anthropic.com` sem autenticação e sem inspeção.
- Sem acesso, o assistente responde "Sem conexão com o serviço de IA. Verifique a internet e tente de novo."; o resto
  do sistema continua funcionando normalmente.

#### Provedor OLLAMA (modelo local)

Indicado quando a loja não quer que nenhuma pergunta saia do computador ou não quer custo por uso. Exige um computador
com memória suficiente para o modelo; as respostas costumam ser mais lentas e menos precisas que as do Claude.

1. Instale o Ollama (ollama.com) no mesmo computador do ERP e baixe um modelo, por exemplo `ollama pull llama3.1`.
   O Ollama fica atendendo em `http://localhost:11434`.
2. No ERP: *Assistente de dúvidas ligado* = **SIM**, *Provedor de IA* = **OLLAMA**, *Ollama: modelo* = o nome baixado
   (ex.: `llama3.1`); mantenha *Ollama: endereço* = `http://localhost:11434`.
3. Clique em **Testar assistente**.

O endereço aceita `http://localhost` ou `https://`. Apontar para um Ollama em **outro** computador (por `https://`) faz
as perguntas saírem desta máquina — só faça isso com um servidor da própria loja. O ERP espera a resposta do Ollama por
até 3 minutos.

#### Como desligar

*Configurações > Assistente (IA)* → *Assistente de dúvidas ligado* = **NAO** → *Salvar*. O painel passa a mostrar
"O assistente está desligado. Um administrador pode ligá-lo em Configurações > Assistente (IA)." e nada é enviado.
Para encerrar de vez o uso da API, apague também a chave do campo, salve e **revogue a chave** em
console.anthropic.com > *API Keys*.

#### O que fica registrado

Cada pergunta (já mascarada) e cada resposta ou erro ficam no *Log de atividades* ("Assistente: pergunta",
"Assistente: resposta", "Assistente: sem resposta"). O limite por hora é contado a partir desse log, por usuário.

### 4.2 Ordens de serviço — prazo padrão

| Parâmetro | Aba | Padrão | Efeito |
|---|---|---|---|
| `os_prazo_dias` — Prazo padrão da ordem de serviço (dias) | Vendas e compras | 3 | Ao abrir uma OS, o prazo de entrega já vem como hoje + N dias (de 0 a 365); pode ser mudado em cada OS |

O prazo não pode ser anterior à abertura da OS e precisa ser uma data plausível (até hoje + 10 anos).
Ao faturar uma OS, o 1º vencimento sugerido segue `prazo_vencimento_dias`, e as parcelas respeitam `max_parcelas`.

## 5. Homologações com parceiros

| Parceiro | Passos |
|---|---|
| Banco (cobrança) | Cadastrar a conta bancária em ambiente HOMOLOGACAO com o modelo de campo livre do manual do banco → emitir boletos de teste → gerar remessa → validar no validador CNAB 240 → enviar ao banco → importar o retorno do banco → após aprovação, mudar a conta para PRODUCAO |
| Pamcard (CIOT) | Credenciamento → receber URL, usuário e senha de homologação → configurar → declarar CIOT de teste → conferir o leiaute com o manual técnico da Pamcard → mudar o ambiente para PRODUCAO com as credenciais definitivas |
| TEF | Contratar provedor (SiTef/PayGo) → implementar o provedor (`IProvedorTef`) → homologar o roteiro do provedor. Até lá, use o SIMULADOR para treino |

## 6. Rotina de operação

| Rotina | Frequência | Como |
|---|---|---|
| Conferir os alertas da tela inicial | Diária (ao abrir e ao voltar à tela inicial) | Ver 6.1 |
| Backup automático | Diário (ao abrir) | `backups\loja_*_diario.db`; mantém a quantidade configurada (padrão 15) |
| Cópia externa | Semanal (recomendado) | Copiar a pasta `backups` para pendrive/nuvem — o banco **não** deve ficar em pasta sincronizada |
| Inventário | Conforme a loja | *Estoque > Ajustar > Definir saldo* |
| Conferência de retornos bancários | Diária, se usa cobrança | *Cobrança > Importar retorno* |
| Verificação de alíquotas da reforma | Anual (e quando a legislação mudar) | *Configurações > Reforma tributária* |
| Consumo do assistente (se usa CLAUDE) | Mensal | Console da Anthropic (gasto) e *Log de atividades* filtrando por "Assistente" (quem pergunta e quanto) |
| Auditoria de atividades | Quando necessário | *Sistema > Log de atividades* (filtros por período, usuário, categoria e texto) |

### 6.1 Rotina diária sugerida (alertas da tela inicial)

A tela inicial mostra só o que precisa de atenção hoje, dos mais graves (ponto vermelho) para os informativos
(ponto laranja). Cada alerta é um botão que **abre a tela que resolve, já filtrada**; sem pendências, a faixa de
alertas some. Os alertas são recalculados ao abrir o sistema e sempre que se volta à tela inicial.

| Alerta | Abre | O que fazer |
|---|---|---|
| "N ordem(ns) de serviço atrasada(s)" (grave) | Ordens de serviço › Atrasadas | Avisar o cliente, renegociar o prazo (*Editar*; a mudança fica no histórico) ou priorizar a execução |
| "N conta(s) a receber vencida(s): R$ ..." (grave) | Contas a pagar e receber › A receber / Vencidas | Cobrar e registrar o contato (*Registrar contato*, com promessa se houver); receber as pagas, uma a uma ou em lote (*Baixar selecionadas*) |
| "N promessa(s) de pagamento não cumprida(s)" (grave) | Contas a pagar e receber › A receber / Promessa vencida | Cobrar de novo e registrar o novo contato |
| "N OS com entrega para hoje" | Ordens de serviço › Entrega hoje | Confirmar que ficam prontas; *Avançar etapa* |
| "N OS pronta(s) aguardando entrega" | Ordens de serviço › Prontas para entregar | Avisar o cliente; ao entregar, *Avançar etapa* (Entregue) |
| "N OS entregue(s) sem faturar" | Ordens de serviço › Entregues sem faturar | *Faturar* (gera as contas a receber) |
| "N produto(s) com estoque baixo" | Estoque › só estoque baixo | Fazer o pedido de compra |

Sugestão de ordem: abrir o sistema → tratar os alertas graves (vermelhos) → os informativos (laranja) → seguir o dia.
Ao fim do dia, voltar à tela inicial e conferir se sobrou algum alerta de "entrega hoje".

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
| `loja.db` | Ao lado do exe (padrão) ou indicado | Todos os dados da loja, inclusive parâmetros e credenciais (cifradas), como a chave da API do assistente |
| `backups\*.db` | Pasta do banco | Backups diários, manuais e antes de migrações |
| `loja_log.db` | Pasta do banco | Log de atividades, inclusive perguntas e respostas do assistente (consulta em *Sistema > Log de atividades*); retenção em *Configurações > Banco de dados* |
| `ERP Loja.lnk` | Área de trabalho | Atalho criado na primeira execução |
| `atalho_criado.txt` | `%LOCALAPPDATA%\ErpLoja` | Marca que o atalho já foi criado (apagar faz criar de novo) |
| `banco.txt` | `%LOCALAPPDATA%\ErpLoja` | Caminho do último banco |

**Proteja a pasta do banco**: ela contém dados de clientes e credenciais de integração (ver [SECURITY.md](../SECURITY.md)).

## 9. Problemas

Veja [10 — Falhas e resoluções](10-falhas-e-resolucoes.md).
