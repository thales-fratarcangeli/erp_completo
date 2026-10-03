# 10 — Falhas e resoluções

| | |
|---|---|
| **Documento** | Base de conhecimento de suporte e registro de falhas de desenvolvimento |
| **Sistema** | ERP Loja 1.5.0 |

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
| "Esta nota já foi importada" | Mesma chave de acesso | Nada a fazer; ela já está em *Notas fiscais*. Para corrigir vínculo/fator, cancele a compra em *Compras realizadas* e importe de novo |
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

### Telas (1.4: uma por vez)
| Situação | Causa | Resolução |
|---|---|---|
| "Cadê a tela que eu estava usando?" | Desde a 1.4 abre uma tela por vez (sem abas) | Abra pelo menu, pelo atalho, pelo Ctrl+K ou por "Continuar de onde parou" na tela inicial |
| O sistema pergunta se quero salvar ao trocar de tela | Há dados digitados e não salvos | *Salvar e sair*, *Descartar e sair* ou *Continuar editando* |
| Cores estranhas / tudo escuro | Modo escuro ligado para o seu usuário | Rodapé da janela: "☀ Modo claro" (ou Configurações > Minha conta > Aparência) |
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

### Assistente de dúvidas (IA)
Configuração passo a passo em [09 — seção 4.1](09-implantacao-e-operacao.md#41-assistente-de-dúvidas-ia). Use
*Configurações > Assistente (IA) > Testar assistente* para conferir; toda falha também fica no *Log de atividades*
("Assistente: sem resposta" ou "Assistente: erro da API").

#### "O assistente está desligado. Um administrador pode ligá-lo em Configurações > Assistente (IA)."
- **Causa:** o parâmetro *Assistente de dúvidas ligado* está em NAO (padrão de instalação e de atualização).
- **Resolução:** um administrador põe **SIM** em *Configurações > Assistente (IA)*, salva e clica em *Testar assistente*. Se o painel já estava aberto, clique em *Nova conversa*.

#### "A chave da API da Anthropic não está configurada (Configurações > Assistente (IA))."
- **Causa:** provedor CLAUDE com o campo *Chave da API da Anthropic* em branco (ou a chave gravada não pôde ser lida neste computador — a cifra DPAPI vale só para a máquina em que foi gravada, então um banco copiado de outro computador chega sem a chave).
- **Resolução:** gerar a chave em console.anthropic.com > *API Keys*, colar no campo, salvar e testar. Quem não quer usar a internet pode trocar o provedor para OLLAMA.

#### "A chave da API da Anthropic foi recusada. Confira em Configurações > Assistente (IA)."
- **Causa:** chave digitada errada, incompleta, revogada ou de uma conta desativada.
- **Resolução:** conferir no console da Anthropic se a chave está ativa; se preciso, gerar outra, colar de novo (sem espaços) e testar.

#### "Limite de N perguntas por hora atingido. Tente de novo mais tarde (o limite é definido pelo administrador)."
- **Causa:** o usuário fez, na última hora, o número de perguntas de *Perguntas por hora por usuário* (padrão 30). O teste em Configurações também conta.
- **Resolução:** esperar (a contagem é da última hora corrida) ou o administrador aumentar o limite. O limite existe para controlar o custo.

#### "O serviço de IA está ocupado (limite de uso da conta). Tente de novo em alguns instantes."
- **Causa:** a própria Anthropic limitou as chamadas da conta (muitas perguntas em pouco tempo ou limite de uso da conta atingido).
- **Resolução:** aguardar alguns instantes; se for frequente, rever os limites e o plano no console da Anthropic.

#### "Sem conexão com o serviço de IA. Verifique a internet e tente de novo." / resposta demora
- **Causa:** sem internet, firewall/antivírus/proxy bloqueando `api.anthropic.com`, ou a chamada foi interrompida (inclusive ao clicar em *Nova conversa* durante a espera). Enquanto espera, o botão mostra "Pensando..."; respostas longas podem levar alguns segundos.
- **Resolução:** conferir a internet; liberar saída HTTPS (porta 443) para `api.anthropic.com` (ver [09 — seção 4.1](09-implantacao-e-operacao.md#41-assistente-de-dúvidas-ia)); tentar de novo. O resto do sistema não depende do assistente.

#### "O serviço de IA retornou um erro. Tente de novo; se continuar, veja o Log de atividades."
- **Causa:** a API recusou a chamada por outro motivo (ex.: nome de modelo inexistente em *Modelo do Claude*, conta sem crédito, serviço com problema). O detalhe técnico fica no log como "Assistente: erro da API".
- **Resolução:** ler a linha do log; conferir o modelo (padrão `claude-opus-5-5`) e o saldo/forma de pagamento no console da Anthropic.

#### "O assistente não pode responder a essa pergunta. Reformule falando apenas do uso do sistema."
- **Causa:** o modelo recusou a pergunta (assunto fora do uso do sistema ou pedido que viola a política de uso).
- **Resolução:** perguntar sobre como fazer algo no sistema (ex.: "como cancelo uma venda?"). O assistente não responde sobre os dados da loja — para números, use o painel e os relatórios.

#### Provedor OLLAMA
| Mensagem | Causa | Resolução |
|---|---|---|
| "Informe o modelo do Ollama em Configurações > Assistente (IA) (ex.: llama3.1)." | Campo *Ollama: modelo* em branco | Preencher com o nome do modelo baixado |
| "O Ollama respondeu com erro (404). Confira se o modelo "..." foi baixado." | Modelo não baixado ou nome diferente | `ollama pull <modelo>` e conferir o nome com `ollama list` |
| "Não foi possível falar com o Ollama neste computador. Ele está instalado e aberto?" | Ollama fechado/não instalado, endereço errado, ou o modelo demorou mais de 3 minutos | Abrir o Ollama; conferir *Ollama: endereço* (`http://localhost:11434`); em computador lento, usar um modelo menor |

### Ordens de serviço
| Mensagem / situação | Causa | Resolução |
|---|---|---|
| "Informe o valor da OS (Editar) antes de faturar." | OS com valor zero | *Editar*, informar o valor e faturar de novo |
| "Esta OS já foi faturada (veja as contas no Financeiro)." | A OS só pode ser faturada uma vez | As contas estão em *Contas a pagar e receber* com a descrição "OS nº N" (ou "OS nº N - parcela x/y") |
| "OS cancelada não pode ser faturada." | OS cancelada | Comportamento esperado; abrir outra OS se o serviço for feito |
| "O 1º vencimento deve ser hoje ou depois (e numa data plausível)." / "Parcelas: de 1 a N." | Data no passado ou número de parcelas fora de 1 a `max_parcelas` | Corrigir no diálogo de faturamento |
| "A OS já foi faturada. Cancele (ou estorne) as contas a receber dela no Financeiro antes de cancelar a OS." | Ao cancelar uma OS faturada que ainda tem contas não canceladas (abertas ou pagas) | No Financeiro, *Estornar baixa* das parcelas já pagas e *Cancelar conta* em todas; depois *Cancelar OS* |
| "A OS já foi faturada: o valor não pode mudar ..." | Mudança de valor depois do faturamento | O valor faturado fica fixo. Para corrigir: cancelar as contas, cancelar a OS e abrir uma nova com o valor certo |
| "Para voltar a OS de "Pronta" para "Em execução", informe o motivo." | *Mudar etapa* para uma etapa anterior sem motivo | Preencher *Observação / motivo*; ele fica no histórico. Avançar não pede motivo |
| "Esta OS já foi entregue (última etapa)." / "OS cancelada não muda de etapa." | *Avançar etapa* numa OS entregue ou cancelada | Comportamento esperado. Para corrigir uma entrega registrada por engano, *Mudar etapa* para a anterior (com motivo) |
| "O prazo de entrega não pode ser antes da abertura da OS (...)." | Prazo anterior à data de abertura | Escolher outro prazo; o padrão vem de *Configurações > Vendas e compras > Prazo padrão da ordem de serviço* |
| "Nenhum cliente cadastrado. ..." | Não há pessoa com papel cliente | Cadastrar em *Cadastros > Clientes e fornecedores* |

### Baixa em lote e acompanhamento de cobrança (Financeiro)
| Mensagem | Causa | Resolução |
|---|---|---|
| "Selecione duas ou mais contas (Ctrl+clique ou Shift+clique). Para uma conta só, use "Baixar"." | Só uma conta selecionada em *Baixar selecionadas* | Selecionar mais contas, ou usar *Baixar (receber/pagar)* |
| "Selecione só contas a receber ou só contas a pagar (não as duas juntas)." | A seleção mistura contas a receber e a pagar | Filtrar *Tipo* = A receber ou A pagar e selecionar de novo |
| "Só contas em aberto podem ser baixadas. Tire da seleção as pagas e canceladas." | Há conta paga ou cancelada na seleção | Filtrar *Situação* = Em aberto (ou Vencidas) e selecionar de novo |
| "A conta nº N não está mais em aberto (pode ter sido baixada ou cancelada). Nenhuma conta foi baixada; ..." | Outra pessoa/estação baixou ou cancelou uma das contas depois da pesquisa | *Pesquisar* e repetir; a baixa em lote é tudo ou nada, nenhuma conta ficou baixada pela metade |
| "A data do pagamento não pode ser futura. ..." | Data de pagamento depois de hoje | Baixar no dia em que o dinheiro entrar ou sair |
| Conta com juros, desconto ou valor diferente | A baixa em lote usa o valor de cada conta | Baixar essa conta sozinha pelo botão *Baixar (receber/pagar)* |
| "A promessa de pagamento deve ser de hoje em diante (e numa data plausível)." | Data da promessa no passado (ou fora da faixa de datas) | Escolher hoje ou uma data futura; uma promessa já vencida aparece no filtro *Promessa vencida* e no alerta da tela inicial |
| "Contatos de cobrança só valem para contas a receber em aberto." | *Registrar contato* numa conta a pagar, paga ou cancelada | Selecionar uma conta a receber em aberto (ou vencida) |
| "Escreva o que foi conversado ..." | Campo *O que foi conversado* em branco | Resumir o contato (ex.: "cliente vai pagar na sexta") |

### Campo de data não aceita o ano digitado
- **Causa:** desde a 1.5 os campos de data só aceitam datas plausíveis: de **01/01/2000** até **hoje + 10 anos** (evita "0201" ou "2099" digitados por engano). Quando a regra é conferida na gravação, a mensagem é "Confira o ...: a data deve estar entre 01/01/2000 e ...".
- **Resolução:** digitar a data com o ano completo e correto. Registros antigos gravados com data fora da faixa continuam abrindo (o campo se estende só até a data deles).

### Tabelas: rolagem e ordenação
| Situação | Causa | Resolução |
|---|---|---|
| Clicar no título da coluna não ordena os itens da venda no PDV | Comportamento esperado: os itens do PDV ficam na ordem em que foram lançados (a ordem da venda e do comprovante) | — Nas demais tabelas, clicar no título ordena e clicar de novo inverte |
| A ordem escolhida "voltou" | A ordem é mantida ao pesquisar ou recarregar a lista, mas não é guardada quando a tela é aberta de novo | Clicar de novo no título da coluna |
| Tabela com barra de rolagem horizontal | Comportamento esperado desde a 1.5: as colunas não ficam espremidas | Rolar para o lado ou aumentar a janela |

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
| 41 | Filtro de colagem transformava "1O2" (letra O) em "12", um CST válido e errado | Caracteres inválidos colados eram removidos em silêncio | O texto colado não é alterado: fica marcado como inválido e a gravação é recusada (rejeitar em vez de "consertar") |
| 42 | Erro "Index was outside the bounds of the array" no painel gerencial ao mover o mouse logo após trocar o período | Áreas de passagem do mouse eram do desenho anterior, com mais categorias | Áreas descartadas quando os dados mudam e verificação de limite |
| 43 | Teste de recuperação de senha interrompia a bateria inteira | A senha de teste ("nova123") deixou de ser aceita pela nova política (mínimo 8) e a exceção não era tratada naquele trecho | Senhas de teste fortes; a infraestrutura de cenários passou a anexar a exceção ao relatório |
| 44 | Barras invertidas e quebras de linha corrompidas ao gerar código por script | Escapes interpretados duas vezes (terminal + Python) | Edições com escapes feitas por arquivo de script ou pelo editor, nunca por bloco "heredoc" |
| 45 | Teste do aviso de dados não salvos perdia o texto digitado | O teste digitava antes de a tela terminar de carregar (a carga inicial limpava o campo) | O teste espera a tela carregar, como uma pessoa faria |
| 46 | Classe de layout não compilava dentro das telas | O nome `Layout` conflita com o evento `Control.Layout` | Classe renomeada para `ModeloDeTela` |
| 47 | No modo escuro, o fundo do menu ficaria claro ao passar o mouse | A conversão de cor misturava as tabelas de fundo e de texto (a cor do menu também é cor de texto) | Conversões separadas: `Tema.Fundo` e `Tema.Texto` |
| 48 | Barra de status continuava clara no modo escuro | O desenho padrão do `StatusStrip` ignora a cor de fundo | No escuro, desenho do sistema (`ToolStripRenderMode.System`) |
| 49 | Arquivos `loja_log.db-wal` e `-shm` na pasta (pergunta do usuário) | O banco de log usava o modo WAL do SQLite, que cria arquivos auxiliares enquanto o sistema está aberto | Log no modo padrão (DELETE): só o `loja_log.db` |
| 50 | SDK da API do Claude: `Fallbacks = [...]` não compilava | No SDK em C#, `Fallbacks` é um tipo união (lista de modelos ou o valor "default"), sem conversão de coleção | `Fallbacks = new Default()` (modelos reserva escolhidos pelo servidor) |
| 51 | `WFO1000` no painel do assistente | Propriedade pública `TelaAtual` (delegate) num controle | `[DesignerSerializationVisibility(Hidden)]` (mesma causa da falha 4) |
| 52 | Teste do assistente não achava o texto das instruções no corpo da requisição | O JSON escapa acentos (`ã`) e o teste procurava o texto cru; depois, procurava a palavra "cache_control" no valor do campo (que é só `{"type":"ephemeral"}`) | Ler o texto com `GetValue<string>()` e conferir o campo `cache_control.type` diretamente |
| 53 | Erro "Object reference not set" ao abrir telas depois da rolagem horizontal das tabelas | Mudar a largura mínima da coluna durante `ColumnAdded`, no meio de uma ordenação ou com a grade ainda sem janela (aba não aberta) quebra o `DataGridView` | Ajuste feito depois do evento (`BeginInvoke`), só quando a largura muda e só com a grade já criada (`HandleCreated`) |
| 54 | Teste de clique no título da coluna não ordenava | Clique simulado por mensagens do Windows só vale com a janela da bateria em primeiro plano (o Windows confere a janela sob o ponteiro) | O teste aciona o mesmo tratador do clique da grade (`OnColumnHeaderMouseClick`), independente da janela em foco |
| 55 | Rodar um cenário só exigia a bateria inteira (≈ 10 min) e regravava os relatórios | Sem filtro de execução | Variável `ERP_CENARIO=<parte do nome>` roda só os cenários correspondentes, sem gerar documentos |
| 56 | Colunas de data (Vencimento, Prazo...) ordenavam pelo dia: 01/12 antes de 15/03 (achado pelo agente de documentação) | O banco devolve a data já como texto "dd/mm/aaaa" e a grade ordena texto | Coluna de data detectada pelo conteúdo; ordena por uma coluna oculta com a data de verdade (`Ui.OrdenarPorData`), mantida ao recarregar e fora da exportação CSV |
| 57 | Teste de ordenação de datas falhava depois de recarregar ("coluna invisível", data "80") | Ao incluir a coluna oculta ou recarregar a lista, a grade recria as colunas e a referência antiga fica inválida | A seta de ordenação é marcada pelo nome da coluna, e o teste busca a coluna de novo a cada leitura |
| 58 | Revisão dos agentes de documentação: OS faturada continuava travada mesmo com as contas canceladas; baixa em lote só conferia o tipo na tela; "Nova conversa" durante a espera escrevia "sem conexão"; Ollama lento parecia "fechado"; dicas de parâmetros imprecisas | Regras apoiadas num campo (`faturada_em`) em vez das contas; validação só na interface; cancelamento tratado como falha de rede | Faturamento vale enquanto houver conta da OS não cancelada (`FaturamentoAtivo`); tipo conferido também em `Operacoes.BaixarLote`; cancelamento e demora com mensagens próprias e resposta descartada; textos corrigidos; testes para cada caso |

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
