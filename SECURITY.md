# Segurança

## Versões suportadas

| Versão | Suporte |
|---|---|
| 1.5.x | ✅ |
| 1.3.x – 1.4.x | Atualize para 1.5 (troca do executável; a migração é automática, com backup antes) |
| 1.0.x – 1.2.x | Atualize para 1.5 (a migração é automática e cifra as credenciais já gravadas) |

Revisão completa contra o OWASP Top 10:2025: [docs/15-seguranca-owasp.md](docs/15-seguranca-owasp.md).

## Como reportar uma vulnerabilidade

Não abra issue pública. Envie os detalhes (versão, passos para reproduzir, impacto) ao mantenedor do repositório pelo
contato do perfil no GitHub. Retorno inicial em até 5 dias úteis.

## Modelo de segurança

| Ativo | Proteção | Responsabilidade da loja |
|---|---|---|
| Senhas de usuários | PBKDF2-SHA256 (600.000 iterações, salt aleatório), comparação em tempo constante, hashes antigos refeitos no login; mínimo de 8 caracteres sem senhas óbvias; bloqueio de 15 min após 5 erros; troca obrigatória da senha padrão | Usar senhas fortes e pessoais |
| Recuperação de senha | Código de 6 dígitos aleatório criptográfico, só o hash é guardado, validade de 15 min, 5 tentativas, uso único | Cadastrar e-mail dos administradores |
| Banco de dados (`loja.db`) | Transações e chaves estrangeiras; verificação de integridade a cada abertura; **não é criptografado** | Restringir o acesso à pasta do banco e dos backups (permissões do Windows, criptografia de disco/BitLocker) |
| Credenciais de integração (Resend, Pamcard, chave da API do assistente) | Mascaradas na tela e **cifradas com a DPAPI do Windows** (uma cópia do banco em outro computador não as revela) | Proteger o banco; usar chaves com o menor privilégio; revogar ao trocar de equipe |
| Executável | Ofuscado na publicação ([ADR-0010](docs/adr/0010-ofuscacao.md)) | Baixar só das releases oficiais |
| Comunicação externa | HTTPS obrigatório (Resend, Pamcard, API da Anthropic) | Liberar apenas os domínios necessários no firewall (`api.anthropic.com` só se usar o assistente com o provedor CLAUDE) |
| Log de atividades | Banco separado com cadeia de hashes SHA-256 (adulteração detectável em *Verificar integridade*); inclui as perguntas e respostas do assistente, já mascaradas | Guardar cópias do log junto com os backups |
| Dados digitados | Formato validado em todos os campos (digitação, colagem, saída do campo e gravação) | — |
| Permissões | Operações de administrador conferidas na tela **e** na camada de dados | Dar perfil de administrador só a quem precisa |
| Assistente de dúvidas (IA) | Desligado por padrão; só a ajuda das telas como contexto; máscara de dados pessoais; limite por hora; sem ações (ver seção abaixo) | Decidir se liga, escolher o provedor e orientar a equipe a não digitar dados de clientes |
| Dependências | Versões travadas (`packages.lock.json`), só nuget.org, sem vulnerabilidades conhecidas na última verificação | — |

## Assistente de dúvidas por IA (1.5.0)

O assistente responde **só como usar o sistema**. Ele não lê o banco, não executa ações e vem **desligado**. Análise de
riscos (OWASP Top 10 para aplicações com LLM) em [docs/15-seguranca-owasp.md](docs/15-seguranca-owasp.md#assistente-de-dúvidas-por-ia-150);
decisão em [ADR-0021](docs/adr/0021-assistente-ia-somente-documentacao.md); regras RN-IA em
[docs/04](docs/04-regras-de-negocio.md).

**O que pode sair do computador** (somente com o provedor CLAUDE, por HTTPS para `api.anthropic.com`):

| Vai para a Anthropic | Não vai |
|---|---|
| A pergunta digitada (até 1.000 caracteres), com CPF, CNPJ, números de cartão, e-mails e telefones trocados por marcadores (`[CPF]`, `[CNPJ]`, `[NÚMERO]`, `[E-MAIL]`, `[TELEFONE]`) | Dados do banco: clientes, produtos, vendas, valores, nome da loja, usuários |
| Até 10 mensagens anteriores da mesma conversa (também mascaradas) | Senhas e credenciais gravadas |
| O nome da tela em que a pessoa está | Nada é enviado com o assistente desligado |
| O manual do sistema (ajuda das telas, lista de telas e atalhos), igual para todas as lojas | |

A máscara reconhece **padrões** de números e e-mails; nomes, endereços e outros textos livres digitados na pergunta
**saem como foram escritos**. Por isso o painel avisa "Não digite senhas nem dados de clientes" — oriente a equipe.
O tratamento dos dados enviados segue os termos e a política de dados da Anthropic, contratados pela própria loja.

Com o provedor **OLLAMA** e o endereço padrão (`http://localhost:11434`), as perguntas ficam no computador. O endereço
também aceita `https://` para um servidor em outra máquina; nesse caso as perguntas vão para esse servidor.

**Chave da API**: digitada só por administrador em *Configurações > Assistente (IA)*, mascarada na tela, gravada
**cifrada com a DPAPI** (como as demais credenciais) e nunca escrita no log. Use uma chave exclusiva da loja, com limite
de gastos no console da Anthropic, e revogue-a ao trocar de equipe ou se suspeitar de vazamento.

**Como desligar**: *Configurações > Assistente (IA)* → *Assistente de dúvidas ligado* = **NAO** → *Salvar*. A partir daí
nada é enviado. Para encerrar de vez: apague a chave do campo, salve e revogue a chave em console.anthropic.com.

**Uso e custo**: limite de perguntas por hora por usuário (`ia_limite_hora`, padrão 30); perguntas e respostas ficam
no log de atividades, com o usuário, para auditoria.

## Ofuscação: o que protege e o que não protege

A ofuscação **dificulta** a engenharia reversa: nomes de classes, métodos e campos ficam ilegíveis e os textos (incluindo
o SQL) são cifrados no executável. Ela **não torna o código impossível de analisar**: um atacante com tempo e
ferramentas pode reconstruir partes da lógica. Por isso:

- nenhum segredo é embutido no código (chaves e senhas ficam na configuração de cada loja);
- regras sensíveis não dependem do sigilo do código;
- o executável não é assinado digitalmente; assiná-lo com certificado de assinatura de código é a medida recomendada para
  garantir a origem e evitar o aviso do SmartScreen.

## Limitações conhecidas

- O banco SQLite não é criptografado em repouso.
- Não há trilha de auditoria por usuário para todas as alterações de cadastro.
- Usuários administradores têm acesso a todas as configurações, inclusive credenciais.
- A máscara do assistente não reconhece nomes, endereços e outros dados em texto livre digitados na pergunta.
