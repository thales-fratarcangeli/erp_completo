# Segurança

## Versões suportadas

| Versão | Suporte |
|---|---|
| 1.3.x | ✅ |
| 1.0.x – 1.2.x | Atualize para 1.3 (a migração é automática e cifra as credenciais já gravadas) |

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
| Credenciais de integração (Resend, Pamcard) | Mascaradas na tela e **cifradas com a DPAPI do Windows** (uma cópia do banco em outro computador não as revela) | Proteger o banco; usar chaves com o menor privilégio; revogar ao trocar de equipe |
| Executável | Ofuscado na publicação ([ADR-0010](docs/adr/0010-ofuscacao.md)) | Baixar só das releases oficiais |
| Comunicação externa | HTTPS obrigatório (Resend, Pamcard) | Liberar apenas os domínios necessários no firewall |
| Log de atividades | Banco separado com cadeia de hashes SHA-256 (adulteração detectável em *Verificar integridade*) | Guardar cópias do log junto com os backups |
| Dados digitados | Formato validado em todos os campos (digitação, colagem, saída do campo e gravação) | — |
| Permissões | Operações de administrador conferidas na tela **e** na camada de dados | Dar perfil de administrador só a quem precisa |
| Dependências | Versões travadas (`packages.lock.json`), só nuget.org, sem vulnerabilidades conhecidas na última verificação | — |

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
