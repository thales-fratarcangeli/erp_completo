# Documentação do ERP Loja

Documentação de engenharia do ERP Loja 1.2.0. Os documentos marcados com ⚙ são **gerados automaticamente** pela bateria
de testes a partir do código (não edite à mão).

| # | Documento | Conteúdo | Público |
|---|---|---|---|
| 01 | [Documento de visão](01-documento-de-visao.md) | Problema, posicionamento, escopo, fora de escopo, restrições | Todos |
| 02 | [Especificação de requisitos](02-especificacao-de-requisitos.md) | Requisitos funcionais, não funcionais e de interface; matriz de rastreabilidade | Produto, desenvolvimento, testes |
| 03 | [Casos de uso](03-casos-de-uso.md) | Atores, fluxos principais e alternativos | Produto, testes |
| 04 | [Regras de negócio](04-regras-de-negocio.md) | Todas as regras com identificadores RN-* | Todos |
| 05 | [Arquitetura](05-arquitetura.md) | Visões C4, componentes, inicialização, migrations, integrações, segurança, build | Desenvolvimento |
| 06 ⚙ | [Modelo de dados](06-modelo-de-dados.md) | Diagrama ER e dicionário de dados do banco atual | Desenvolvimento, suporte |
| 07 | [Plano de testes](07-plano-de-testes.md) | Níveis, técnicas, ambiente e critérios | Desenvolvimento, qualidade |
| 08 ⚙ | [Relatório de testes](08-relatorio-de-testes.md) | Resultado da última execução da bateria | Qualidade |
| 09 | [Implantação e operação](09-implantacao-e-operacao.md) | Instalação, atualização, configuração inicial, homologações, rotinas, linha de comando | Implantação, suporte |
| 10 | [Falhas e resoluções](10-falhas-e-resolucoes.md) | Problemas de uso e registro de falhas de desenvolvimento | Suporte, desenvolvimento |
| 11 | [Glossário](11-glossario.md) | Termos fiscais, bancários, logísticos e técnicos | Todos |
| 12 ⚙ | [Manual do usuário](12-manual-do-usuario.md) | Mesmo conteúdo do botão "?" de cada tela | Usuários |
| 13 | [Histórico de solicitações](13-historico-de-solicitacoes.md) | Todas as mensagens do product owner, literais, e o atendimento de cada uma | Todos |
| — | [Decisões de arquitetura (ADR)](adr/) | 18 decisões registradas | Desenvolvimento |

Na raiz do repositório: [README](../README.md), [CHANGELOG](../CHANGELOG.md), [CONTRIBUTING](../CONTRIBUTING.md), [SECURITY](../SECURITY.md).

## Convenções

- Regras: `RN-<área>-<nn>`; requisitos: `RF-nn`, `RNF-nn`, `RI-nn`; casos de uso: `UC-nn`; decisões: `ADR-nnnn`.
- Toda mudança de comportamento atualiza a regra correspondente em [04](04-regras-de-negocio.md) e ganha verificação na bateria.
- Toda falha relevante encontrada entra no registro de [10](10-falhas-e-resolucoes.md) com causa e resolução.
