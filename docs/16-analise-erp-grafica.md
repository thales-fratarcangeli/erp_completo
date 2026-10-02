# 16 — Análise do banco de outro ERP (gráfica) e o que ainda podemos implementar

| | |
|---|---|
| **Documento** | Análise comparativa e roteiro de evolução |
| **Fonte** | `banco de dados de outro ERP` — banco Firebird do ERP **outro ERP** usado por uma gráfica (637 MB) |
| **Data** | 02/10/2026 |
| **Método** | Cópia do arquivo aberta em modo de leitura com o Firebird embarcado; leitura apenas de **estrutura e números agregados** (nenhum dado pessoal de clientes foi copiado para a documentação); comparação com a árvore de opções do outro ERP e com o ERP Loja 1.4 |

## 1. O que é o banco

| Item | Quantidade |
|---|---:|
| Tabelas | 2.247 (**221 com dados**) |
| Views / procedures / triggers | 154 / 217 / 220 |
| Chaves estrangeiras | 3.523 |
| Ordens de serviço (OS) | 44.126 (2009–2026) |
| Títulos a receber / a pagar | 36.590 / 19.395 (+ 40 mil e 31 mil parcelas) |
| NF de saída / entrada | 9.484 / 2.057 |
| Produtos / clientes e fornecedores | 10.560 / 4.479 |
| Usuários | 88 |

O outro ERP é um ERP muito amplo (folha, frota, abate, confinamento, laticínio, obras, comércio exterior, CT-e/MDF-e, PCP,
contabilidade, livros fiscais...). **A gráfica usa uma fração pequena**: dos módulos com tabelas, só têm dados os de
ordem de serviço, financeiro, notas fiscais, cadastros, contabilidade básica e livros fiscais.

## 2. O que a gráfica realmente usa (menus mais usados)

| Posição | Função (outro ERP) | Usos | Temos no ERP Loja? |
|---:|---|---:|---|
| 1 | **Ordem de Serviço** | 10.584 | ❌ (temos ordem de *produção* por ficha técnica, não OS gráfica sob medida) |
| 2 | Notas de Saída | 3.787 | ✅ parcial (sem transmissão à SEFAZ) |
| 3 | Contas a pagar por fornecedor/vencimento | 2.939 | ✅ |
| 4 | Produtos | 2.697 | ✅ |
| 5 | Cliente/Fornecedor | 2.213 | ✅ |
| 6 | Cadastro de títulos | 1.969 | ✅ |
| 7 | **Emissão da OS** (impressão da ficha) | 1.447 | ❌ |
| 8 | **Baixa por número de lote** | 1.089 | ❌ (baixa é uma conta por vez) |
| 9 | **Relatório de Ordem de Serviço** | 785 | ❌ |
| 10 | NF emitidas (simplificado) | 645 | ✅ |
| 11 | **Monitoramento da NF-e** | 390 | ❌ (sem SEFAZ) |
| 12 | Reemissão de DANFE | 232 | ✅ (sem valor fiscal) |
| 15 | **Envio de e-mail das NF-e (XML/PDF)** | 147 | ❌ |
| 19 | Transportadoras | 74 | ✅ (pessoa com papel transportador) |
| 20 | **Cadastro de cores** (artes gráficas) | 55 | ❌ |
| 22 | Alíquotas do Simples Nacional | 49 | ⚠️ parcial (regime e CSOSN; sem tabela de faixas) |
| 34 | Limite de crédito | 20 | ✅ |
| 36 | Orçamento | 17 | ✅ (de produtos; não gráfico) |
| 40 | **Cadastro de papel** | 13 | ❌ |

## 3. A ordem de serviço gráfica (o coração do negócio)

A OS do outro ERP (113 colunas) descreve o **trabalho sob encomenda**:

| Grupo | Informações |
|---|---|
| Identificação | número, cliente, contato, vendedor, nome do serviço, produto, datas de emissão, aprovação e entrega prometida |
| Especificação | espécie (com chapa), tipo, formato aberto e final, quantidade de peças e folhas, vias, numeração, papel |
| Cores | cores da frente e do verso (cadastro de 520 cores, ex.: escala CMYK e Pantone) — 111 mil registros de cores de frente |
| Pré-impressão | criação, montagem, fotolito, chapa, clichê, tempo |
| Impressão | quantidade em offset / tipografia, impressões por folha, formato da chapa, acerto |
| Acabamentos | vinco, corte, dobra, picote, furo, grampo, costura, colagem, intercalação, carbonado, alto-relevo |
| Valores | mão de obra + material = total; frete, seguro, desconto, acréscimo; condição de pagamento e parcelas |
| Entrega e financeiro | data e quantidade entregues, boleto, NF, se foi pago e quando |
| Histórico | observações múltiplas (190 mil linhas), quem criou e quem alterou |

**Uso dos acabamentos** (quantas OS): colagem 14.731 · corte 12.538 · vinco 12.416 · grampo 2.936 · intercalação 2.642 ·
numeração 2.624 · dobra 2.572 · furo 1.834 · picote 35 · alto-relevo 2 · costura 1.

**Números**: ~2.200–3.200 OS/ano; ticket médio subiu de R$ 491 (2010) para R$ 2.228 (2026); 2026 até agora: 1.453 OS,
R$ 3,2 milhões.

### Problemas que os dados revelam (oportunidades)

| Achado | Impacto | Como o nosso ERP pode resolver |
|---|---|---|
| **83% das OS sem situação** (campo em branco) | Ninguém sabe pelo sistema o que está em produção, pronto ou entregue | Fluxo obrigatório de etapas (orçamento → aprovada → arte → impressão → acabamento → pronta → entregue → faturada) com painel |
| **45% das entregas registradas foram depois do prazo** (7.443 de 16.491) | Atraso recorrente, sem alerta | Painel de prazos com cores (no prazo / vence hoje / atrasada), alerta na tela inicial e prazo por etapa |
| 63% das OS sem registro de entrega | Não dá para medir pontualidade nem faturamento pendente | Entrega como etapa do fluxo (com quantidade e parcial) |
| Datas com ano 201, 206, 1201 | Relatórios por período ficam errados | Validação de datas (já temos validação de formato em todos os campos — RN-SEG-01) |
| Criação, montagem, fotolito e clichê nunca preenchidos | Custos de pré-impressão não são apurados | Custos por etapa na OS e margem real |
| Só 2 centros de custo usados | Rateio não é usado de fato | Manter simples (categoria de despesa) |

## 4. Comparação geral

| Área | outro ERP (usado pela gráfica) | ERP Loja 1.4 | Situação |
|---|---|---|---|
| OS gráfica sob encomenda | Sim (principal) | Não | **Lacuna crítica** |
| Cadastros de artes gráficas (papel, cores, espécies, tipos) | Sim | Não | Lacuna |
| Orçamento gráfico (cálculo de papel, chapas, impressão e acabamentos) | Básico | Orçamento de produtos | Lacuna |
| NF-e com transmissão à SEFAZ, monitoramento, e-mail XML/PDF, carta de correção, inutilização | Sim (muito usado) | Numeração, chave e DANFE sem valor fiscal | **Lacuna crítica** (exige certificado A1) |
| NFS-e (serviços, ISS) | Módulo disponível | Não | Lacuna (gráfica fatura serviço) |
| Baixa de títulos em lote | Sim (1.089 usos) | Uma por vez | Lacuna |
| Follow-up de cobrança (contatos, prorrogações) | Sim (4.729 registros) | Não | Lacuna |
| Contas a pagar/receber, parcelas, boleto, CNAB | Sim | Sim (CNAB 240 genérico) | ✅ |
| Cheques pré-datados (custódia, devolução) | Sim | Não | Lacuna menor |
| Conciliação bancária / movimento de contas | Sim | Não | Lacuna |
| Contabilização (plano de contas, lançamentos, saldos) | Sim (7.222 contas) | Não | Lacuna (pode ser exportação para o contador) |
| Permissões por tela e ação (incluir/alterar/excluir/emitir) | Sim (69 mil regras) | Perfil administrador/comum | Lacuna importante |
| Vendedores e comissões | Sim (vendedor na OS) | Não | Lacuna |
| Condições de pagamento cadastradas | Sim | Parâmetros de parcelas | Melhoria |
| Multiempresa / filiais | Sim | Um banco por empresa | Fora do escopo atual |
| Livros fiscais, SPED, Sintegra, GIA | Sim | Não | Fora do escopo (contador) |
| Folha, frota, abate, laticínio, obras, comércio exterior | Sim (não usados pela gráfica) | Não | Fora do escopo |
| Assistente de IA | Tabelas de conversa, feedback e log de ferramentas | Proposto (documento 13, mensagem 26) | Em avaliação |
| PDV, TEF, CIOT, produção por ficha técnica, reforma tributária, log com integridade, OWASP | Parcial | Sim | **Vantagens do ERP Loja** |

## 5. Roteiro recomendado

### Prioridade 1 — o que a gráfica usa todo dia
| # | Entrega | Por quê | Esforço |
|---|---|---|---|
| 1 | **Ordem de Serviço gráfica**: cadastros de papel, cores, espécies e tipos; OS com especificação, cores frente/verso, acabamentos (checklist), pré-impressão, valores, parcelas, observações com histórico; impressão da ficha da OS | Função nº 1 (10.584 usos) | Alto |
| 2 | **Fluxo de etapas e painel de prazos** (kanban: aprovada → arte → impressão → acabamento → pronta → entregue → faturada), alerta de atraso, entrega parcial | 83% sem situação e 45% de atrasos | Médio |
| 3 | **OS → NF / boleto**: faturar a OS gerando nota, contas a receber e boleto | Fecha o ciclo financeiro da OS | Médio |
| 4 | **Relatórios de OS**: por período, cliente, vendedor, situação, atrasos, acabamentos | 785 usos | Baixo |

### Prioridade 2 — fiscal e financeiro do dia a dia
| # | Entrega | Por quê | Esforço |
|---|---|---|---|
| 5 | **NF-e de verdade**: assinatura com certificado A1, transmissão e consulta na SEFAZ, monitoramento, envio de XML/PDF por e-mail, carta de correção, cancelamento e inutilização | Muito usado; hoje o DANFE sai "sem valor fiscal" | Alto (homologação SEFAZ) |
| 6 | **Baixa de títulos em lote** e **follow-up de cobrança** (registro de contato, promessa, prorrogação) | 1.089 usos e 4.729 registros | Baixo/Médio |
| 7 | **Permissões por tela e ação** (consultar, incluir, alterar, excluir, emitir/cancelar) por usuário ou perfil | 88 usuários com regras finas no outro ERP | Médio |
| 8 | **NFS-e** (nota de serviço, ISS) pelo padrão nacional | Gráfica fatura serviço | Alto (por município / padrão nacional) |

### Prioridade 3 — complementos
| # | Entrega | Esforço |
|---|---|---|
| 9 | Vendedores e comissões (vendedor na OS e no pedido) | Médio |
| 10 | Condições de pagamento cadastradas (30/60/90, dia de fechamento) | Baixo |
| 11 | Conciliação bancária por arquivo OFX | Médio |
| 12 | Cheques pré-datados (custódia, compensação, devolução) | Médio |
| 13 | Exportação contábil para o contador (lançamentos por plano de contas) | Médio |
| 14 | Orçamento gráfico com cálculo automático (papel por formato e aproveitamento, chapas por cor, impressões, acabamentos) | Alto |

### Habilitador — migração de dados do outro ERP
Um **importador** (somente leitura do `.FDB`) para trazer clientes/fornecedores, produtos, papéis, cores, espécies,
títulos em aberto e o histórico de OS, com correção das datas inválidas e mapeamento das situações. Sem isso, a troca
de sistema obriga a recadastrar tudo. O importador depende do Firebird embarcado (cliente `fbclient.dll`) só durante a
importação.

## 6. Observações de privacidade

O banco contém dados pessoais e comerciais de clientes (nomes, documentos, endereços, valores). A análise usou uma
**cópia temporária**, apagada ao final, e este documento traz apenas nomes de tabelas, contagens e totais agregados.
Uma eventual importação deve rodar na máquina da loja, sem enviar dados a terceiros.
