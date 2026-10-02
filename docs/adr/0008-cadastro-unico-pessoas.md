# ADR-0008 — Cadastro único de pessoas

| | |
|---|---|
| **Status** | Aceita |
| **Data** | 2026-10-01 |

## Contexto
Clientes e fornecedores precisavam ficar na mesma tela; uma empresa pode ser cliente e fornecedora; transportadores e motoristas foram incluídos.

## Decisão
Tabela `pessoas` com papéis (`eh_cliente`, `eh_fornecedor`, `eh_transportador`, `eh_motorista`). A migração 5 une `clientes` e `fornecedores` (mesmo CPF/CNPJ vira uma pessoa), remapeia os ids de fornecedor e repõe todas as chaves estrangeiras com o procedimento seguro de reconstrução de tabelas.

## Consequências
- Uma tela, sem cadastro duplicado.
- Colunas `cliente_id`/`fornecedor_id` mantidas (apontam para `pessoas`), minimizando mudanças nas regras.

## Alternativas consideradas
Duas tabelas com tela unificada por cima (duplicaria dados de quem tem dois papéis).
