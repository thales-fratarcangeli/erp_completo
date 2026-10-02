# ADR-0010 — Ofuscação do executável publicado

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Requisito de dificultar a engenharia reversa. Aplicações .NET são facilmente decompiláveis.

## Decisão
Na publicação Release, o alvo `OfuscarAssembly` passa a DLL do sistema pelo Obfuscar (ferramenta local): renomeia tipos/membros (inclusive públicos) com nomes Unicode ilegíveis e cifra as strings. Tipos usados por nome em tempo de execução são marcados com `[Obfuscation(Exclude = true)]`; chaves de ajuda usam `nameof`; JSON é montado sem tipos anônimos.

## Consequências
- DLL publicada sem nomes de métodos nem SQL legíveis (verificado).
- Ofuscação não é proteção absoluta (ver [SECURITY.md](../../SECURITY.md)).
- Todo build publicado passa pelo teste de interface do executável ofuscado.

## Alternativas consideradas
Não ofuscar; ofuscadores comerciais (custo; podem ser adotados depois sem mudar o código).
