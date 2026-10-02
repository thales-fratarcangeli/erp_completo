# 15 — Revisão de segurança (OWASP Top 10:2025)

| | |
|---|---|
| **Documento** | Avaliação do ERP Loja contra o OWASP Top 10 (edição 2025) e validação de entrada de dados |
| **Sistema** | ERP Loja 1.3.0 (banco v13) |
| **Data** | 02/10/2026 |
| **Verificação** | Cenários `SegurancaOwaspTop10` e `ValidacaoFormatosDeCampos` + testes de cobertura da bateria ([08](08-relatorio-de-testes.md)) |

O ERP Loja é uma aplicação **desktop** com banco **local** (SQLite). Por isso alguns riscos típicos da web (sessão HTTP,
CORS, SSRF por URL do usuário) não se aplicam; os equivalentes desktop foram avaliados: acesso ao arquivo do banco,
credenciais guardadas, permissões dentro do programa, arquivos importados/exportados e dependências.

## Resumo

| # | Risco (2025) | Situação antes | O que foi feito | Status |
|---|---|---|---|---|
| A01 | Quebra de controle de acesso | Permissões de administrador conferidas só nas telas | Verificação também na camada de dados (`Permissoes.ExigirAdmin`): cancelar venda/compra/nota, alterar configurações, gravar usuários e contas bancárias, gerar dados de demonstração; telas de administrador bloqueadas mesmo por chamada direta; tentativas negadas vão ao log | ✅ |
| A02 | Configuração insegura | Senha padrão admin/admin; credenciais em texto puro | Troca obrigatória da senha padrão no primeiro acesso; credenciais cifradas (DPAPI); mensagens de erro sem detalhes internos | ✅ |
| A03 | Cadeia de suprimentos | Versões de pacotes sem trava | `packages.lock.json` (versões exatas, inclusive transitivas); fonte única nuget.org (`<clear/>`); pacote desnecessário removido; varredura `dotnet list package --vulnerable` sem achados; ferramenta Obfuscar com versão fixa | ✅ |
| A04 | Falhas criptográficas | PBKDF2 com 100.000 iterações; chaves de API legíveis no banco | PBKDF2-HMAC-SHA256 com **600.000** iterações (recomendação OWASP) e atualização automática dos hashes antigos no login; credenciais com DPAPI (escopo da máquina, entropia própria); integrações só por HTTPS (URL da Pamcard exige `https://`) | ✅ |
| A05 | Injeção | SQL já parametrizado | Auditoria de todo SQL interpolado (só nomes vindos do código); curingas `%`/`_` tratados como texto (`Db.Contem` + `ESCAPE`); **CSV injection** neutralizada na exportação; leitor de XML sem DTD/entidades externas (**XXE**) e com limite de tamanho; caracteres de controle removidos do texto enviado à impressora (comandos ESC/POS); **validação de formato em todos os campos** (ver abaixo) | ✅ |
| A06 | Design inseguro | Regras importantes só na tela | Regras de negócio na camada de dados (72 falhas corrigidas pelos testes exploratórios — [14](14-testes-exploratorios.md)); validação de entrada também na gravação | ✅ |
| A07 | Falhas de autenticação | Sem limite de tentativas; senha mínima de 4 | Bloqueio de 15 min após 5 senhas erradas (desbloqueio ao trocar a senha); senha mínima de 8 sem senhas óbvias; hash refeito no padrão atual; códigos de recuperação aleatórios, só em hash, 15 min, 5 tentativas | ✅ |
| A08 | Integridade de software e dados | Log alterável sem rastro | Log com **cadeia de hashes SHA-256** e botão "Verificar integridade"; `PRAGMA quick_check` do banco a cada abertura; executável ofuscado; SHA-256 do executável publicado na release | ✅ (assinatura de código pendente) |
| A09 | Falhas de log e alerta | Log sem alertas | Alerta ao administrador com as tentativas recusadas desde a última entrada; registro de bloqueios, acessos negados, falhas de integridade e erros inesperados | ✅ |
| A10 | Tratamento de condições excepcionais | Mensagem mostrava `Exception.Message` (podia expor SQL/caminhos) | Erros inesperados mostram mensagem genérica com a hora e o detalhe vai só para o log; captura também de erros fora da thread da tela; verificações em "falha fechada" (hash corrompido nega acesso; credencial ilegível fica em branco) | ✅ |

## Validação de entrada (todos os campos)

Pedido: *"todos os campos de inserção devem verificar se o valor segue os parâmetros (tipo CNPJ tem que ter formação de
CNPJ, onde é pra pôr número não pode pôr texto)"*.

- **Fonte única**: `ErpLoja/Data/Formatos.cs` define cada tipo de dado (CPF, CNPJ, CPF/CNPJ, IE, CEP, telefone, e-mail,
  UF, cidade, data, URL https, código de produto, código de barras com DV, unidade, NCM, CEST, CFOP, CST, CST IBS/CBS,
  cClassTrib, nº de nota, chave de acesso, placa, RENAVAM, RNTRC, CNH, código do banco, agência, conta, dígito,
  carteira, convênio, espécie, modelo do campo livre, username, senha, inteiro, decimal, percentual, texto com tamanho).
- **Na digitação** (`UI/Validacao.cs`): caracteres fora do formato são bloqueados com aviso do que o campo aceita
  (ex.: "Este campo aceita só números (14 dígitos do CNPJ)"); campos numéricos não aceitam letras; colunas fiscais das
  grades (NCM, CFOP, CST...) também filtram.
- **Ao colar**: o texto **não é alterado em silêncio** (remover a letra O de "1O2" deixaria "12", outro valor válido): o
  campo fica vermelho com a explicação e a gravação é recusada até a correção — rejeitar em vez de "consertar".
- **Ao sair do campo**: o valor válido recebe a máscara (CNPJ 00.000.000/0000-00, CEP 00000-000, telefone, maiúsculas
  na placa/UF); o inválido fica marcado com ícone e fundo vermelho.
- **Ao gravar**: a tela confere todos os campos (o primeiro inválido recebe o cursor e a explicação); os parâmetros são
  conferidos de novo na camada de dados.
- **Garantia de cobertura**: a bateria abre todas as telas e **reprova** qualquer caixa de texto editável sem formato,
  e varre o código-fonte procurando caixas criadas sem formato (inclusive em diálogos).

## Riscos remanescentes e recomendações

| Item | Recomendação |
|---|---|
| Executável não assinado | Assinar com certificado de assinatura de código (EV elimina o aviso do SmartScreen). A release traz o SHA-256 para conferência. |
| Banco SQLite não cifrado em repouso | Restringir a pasta do banco por permissões do Windows e usar BitLocker. Cifragem do banco inteiro (SQLCipher) exigiria troca da biblioteca nativa. |
| Usuários do Windows com acesso ao computador | A DPAPI de máquina protege contra cópia do banco para outro computador, não contra outro usuário do mesmo computador; usar contas do Windows separadas e sem privilégio de administrador. |
| Recuperação de senha indica se o usuário existe | Aceito para uma loja com poucos usuários locais (melhor orientação ao funcionário); rever se houver acesso remoto. |
| Ofuscação | Dificulta, não impede, a engenharia reversa (ver [SECURITY.md](../SECURITY.md)). Nenhum segredo fica no código. |

## Como verificar

```powershell
dotnet run --project testes/ErpLoja.Testes          # inclui [Segurança] e [Validação]
dotnet list ErpLoja/ErpLoja.csproj package --vulnerable --include-transitive
```
Na aplicação: *Sistema > Log de atividades > Verificar integridade*.
