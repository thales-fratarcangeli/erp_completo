# ADR-0021 — Assistente de dúvidas por IA restrito à documentação

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-02 |

## Contexto
Pedido: "qual é o melhor modo de adicionar uma IA ao sistema para tirar dúvidas sem pôr em risco a segurança?" e,
depois, "eu aprovo a IA". Um assistente com acesso ao banco poderia vazar dados de clientes (para o provedor ou por
injeção de instruções) e executar ações indevidas.

## Decisão
- O assistente responde **só como usar o sistema**. O contexto enviado é a ajuda das telas (`Ajuda.Topicos`) e a
  lista de telas (`Navegacao`), nada do banco. Ele **não executa ações** e não tem ferramentas.
- Antes de sair do computador, a pergunta passa por máscara de CPF, CNPJ, cartão, e-mail e telefone.
- Desligado por padrão; ligado por administrador em Configurações > Assistente (IA). Chave da API cifrada (DPAPI).
- Provedor `CLAUDE` (API da Anthropic, modelo padrão `claude-opus-5-5`, esforço baixo, modelos reserva automáticos
  no servidor quando houver recusa ou indisponibilidade) ou `OLLAMA` (modelo local, nada sai da máquina).
- Limite de perguntas por hora por usuário; perguntas e respostas (já mascaradas) no log.
- Instruções de sistema em bloco com cache (custo menor) e regra explícita contra instruções embutidas na pergunta.

## Consequências
- Risco de vazamento limitado ao que a pessoa digitar, e mesmo assim mascarado; sem risco de ação indevida.
- Não responde perguntas sobre os dados ("quanto vendi ontem?") — para isso existem painel e relatórios.
- Exige internet e custo por uso no modo CLAUDE; o modo OLLAMA exige um computador com o modelo instalado.

## Alternativas consideradas
Assistente com acesso de leitura ao banco por ferramentas (útil, mas expõe dados e amplia a superfície de injeção);
busca só por palavras na ajuda (já existe no Ctrl+K, não entende perguntas em linguagem natural).
