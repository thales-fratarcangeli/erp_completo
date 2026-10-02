# Relatório de testes

> Gerado automaticamente pela bateria `testes/ErpLoja.Testes` a cada execução. Não edite à mão.

| Item | Valor |
|---|---|
| Data da execução | 02/10/2026 13:15 |
| Versão do sistema | 1.2.0.0 (banco v12) |
| Ambiente | Microsoft Windows 10.0.19045 · .NET 10.0.12 |
| Verificações | 905 |
| Aprovadas | 905 |
| Reprovadas | 0 |
| Resultado | **APROVADO** |

## Verificações

| # | Resultado | Verificação |
|---:|:---:|---|
| 1 | ✅ | banco legado reconhecido como banco do sistema |
| 2 | ✅ | legado v1: registra a 1 e aplica as migrações 2 a 12 |
| 3 | ✅ | backup automático antes de migrar banco existente |
| 4 | ✅ | __migracoes com todas as versões |
| 5 | ✅ | coluna login renomeada para username |
| 6 | ✅ | parâmetros padrão gravados pela migração 4 |
| 7 | ✅ | banco aberto fica lembrado e é localizado na próxima abertura |
| 8 | ✅ | reabrir não reaplica migrações |
| 9 | ✅ | legado v2: aplica da migração 3 em diante |
| 10 | ✅ | banco de versão mais nova que o executável é recusado |
| 11 | ✅ | banco v4 com dados: aplica da migração 5 em diante |
| 12 | ✅ | clientes e fornecedores viraram pessoas (mesmo CNPJ unificado: 4 cadastros → 3 pessoas) |
| 13 | ✅ | pessoa com os dois papéis após a unificação |
| 14 | ✅ | compras apontam para a pessoa certa |
| 15 | ✅ | contas e vendas apontam para a pessoa certa |
| 16 | ✅ | itens preservados na reconstrução das tabelas (sem exclusão em cascata) |
| 17 | ✅ | integridade referencial ok e nenhuma referência às tabelas antigas |
| 18 | ✅ | vínculos produto × fornecedor preservados |
| 19 | ✅ | chaves estrangeiras ativas apontando para pessoas |
| 20 | ✅ | arquivo que não é SQLite é recusado |
| 21 | ✅ | arquivo inexistente é recusado |
| 22 | ✅ | banco lembrado que sumiu não é usado (cai no loja.db ao lado do exe ou pergunta) |
| 23 | ✅ | banco novo: 12 migrações aplicadas |
| 24 | ✅ | criar banco não sobrescreve arquivo existente |
| 25 | ✅ | login admin/admin (username sem diferenciar maiúsculas) |
| 26 | ✅ | senha errada rejeitada |
| 27 | ✅ | compra soma estoque |
| 28 | ✅ | custo médio ponderado (5 e 7 => 6) |
| 29 | ✅ | compra a prazo gera 2 parcelas a pagar |
| 30 | ✅ | parcelas somam o total da compra |
| 31 | ✅ | total da venda com desconto (90+19,80-5) |
| 32 | ✅ | venda baixa estoque |
| 33 | ✅ | venda à vista gera recebimento pago |
| 34 | ✅ | 3 parcelas 33,33/33,33/33,34 |
| 35 | ✅ | cancelamento devolve estoque |
| 36 | ✅ | cancelamento cancela contas |
| 37 | ✅ | cancelar duas vezes bloqueado |
| 38 | ✅ | baixa de conta |
| 39 | ✅ | estorno de baixa |
| 40 | ✅ | ajuste de inventário |
| 41 | ✅ | histórico de movimentações registrado |
| 42 | ✅ | cancelar compra retira estoque |
| 43 | ✅ | tipagem das colunas |
| 44 | ✅ | relatório "Vendas por dia" (1 linhas) |
| 45 | ✅ | relatório "Produtos mais vendidos" (2 linhas) |
| 46 | ✅ | relatório "Vendas por cliente" (1 linhas) |
| 47 | ✅ | relatório "Vendas por usuário (vendedor)" (1 linhas) |
| 48 | ✅ | relatório "Lucro bruto (resumo do período)" (1 linhas) |
| 49 | ✅ | relatório "Fluxo de caixa (realizado)" (1 linhas) |
| 50 | ✅ | relatório "Fluxo de caixa (previsto - contas em aberto)" (1 linhas) |
| 51 | ✅ | relatório "Produtos sem venda no período" (0 linhas) |
| 52 | ✅ | relatório "Compras por fornecedor" (1 linhas) |
| 53 | ✅ | comprovante gerado |
| 54 | ✅ | backup do banco |
| 55 | ✅ | orçamento: 60 - 1 - 2 + 5 frete = 62 |
| 56 | ✅ | duplicar orçamento |
| 57 | ✅ | aprovar orçamento gera pedido de venda |
| 58 | ✅ | aprovar 2x bloqueado |
| 59 | ✅ | faturar: venda com descontos e frete (62) |
| 60 | ✅ | faturar baixa estoque |
| 61 | ✅ | faturar gera 2 parcelas = 62 |
| 62 | ✅ | pedido FATURADO |
| 63 | ✅ | cancelar venda reabre o pedido e devolve estoque |
| 64 | ✅ | receber pedido de compra: custo médio com frete (5,3333) |
| 65 | ✅ | pedido de compra RECEBIDO |
| 66 | ✅ | DV mod 11 (exemplo do manual da NF-e) |
| 67 | ✅ | nota montada a partir da venda (CFOP 5102, total 104,80) |
| 68 | ✅ | ICMS 18% sobre 90,00 = 16,20 |
| 69 | ✅ | emissão: número 1, situação EMITIDA |
| 70 | ✅ | chave de acesso 44 dígitos com DV válido |
| 71 | ✅ | numeração avançou |
| 72 | ✅ | emitir 2x bloqueado |
| 73 | ✅ | venda reconhece nota existente |
| 74 | ✅ | DANFE gerado |
| 75 | ✅ | validação acusa NCM faltando |
| 76 | ✅ | nota de entrada a partir da compra (CFOP 1102) |
| 77 | ✅ | XML lido (2 itens, 2 duplicatas, total 436) |
| 78 | ✅ | custo do item inclui frete/IPI e desconta desconto |
| 79 | ✅ | vínculo pelo código de barras; item novo sem vínculo |
| 80 | ✅ | 1 produto novo cadastrado |
| 81 | ✅ | fator 10: 2 fardos = 20 unidades no estoque |
| 82 | ✅ | custo médio com o custo efetivo do XML |
| 83 | ✅ | contas a pagar = duplicatas da nota |
| 84 | ✅ | nota marcada como importada |
| 85 | ✅ | importar 2x bloqueado |
| 86 | ✅ | 2ª vez: fornecedor e vínculos (com fator) memorizados |
| 87 | ✅ | DANFE da nota importada |
| 88 | ✅ | parâmetros padrão lidos |
| 89 | ✅ | desconto acima de 10% bloqueado para usuário comum |
| 90 | ✅ | administrador pode dar desconto acima do limite |
| 91 | ✅ | parâmetro alterado é respeitado |
| 92 | ✅ | natureza padrão da nota vem dos parâmetros |
| 93 | ✅ | usuário sem e-mail: recusa com orientação |
| 94 | ✅ | sem chave do Resend: avisa que falta configurar |
| 95 | ✅ | código enviado; e-mail exibido mascarado |
| 96 | ✅ | chamada à API do Resend (URL, chave e destinatário) |
| 97 | ✅ | código de 6 dígitos no e-mail |
| 98 | ✅ | código guardado só como hash |
| 99 | ✅ | código errado conta tentativa |
| 100 | ✅ | código certo redefine a senha |
| 101 | ✅ | código não pode ser reutilizado |
| 102 | ✅ | código expirado é recusado |
| 103 | ✅ | 5 tentativas erradas invalidam o código |
| 104 | ✅ | erro do Resend é mostrado ao usuário |
| 105 | ✅ | validação de CPF/CNPJ pelos dígitos verificadores |
| 106 | ✅ | toda entrada do menu tem tópico de ajuda |
| 107 | ✅ | menu sem entradas repetidas (uma função, um lugar) |
| 108 | ✅ | buscar 'boleto' traz a cobrança bancária primeiro |
| 109 | ✅ | busca sem acento e por palavra-chave (ciot, caminhão) |
| 110 | ✅ | telas de administrador não aparecem para usuário comum |
| 111 | ✅ | termo sem relação não retorna telas |
| 112 | ✅ | crédito disponível = limite − contas em aberto |
| 113 | ✅ | limite zero = sem limite |
| 114 | ✅ | fator de vencimento (inclusive a virada de 22/02/2025) |
| 115 | ✅ | DV do código de barras e linha digitável (exemplo de referência) |
| 116 | ✅ | validação do modelo de campo livre |
| 117 | ✅ | boleto: código de barras de 44 posições, DV e valor; linha digitável de 47 dígitos |
| 118 | ✅ | não emite dois boletos ativos para a mesma conta |
| 119 | ✅ | remessa CNAB 240: header, lote, segmentos P/Q/R e trailers com 240 posições, validada |
| 120 | ✅ | boleto marcado como enviado na remessa |
| 121 | ✅ | validador acusa linha fora do padrão |
| 122 | ✅ | retorno simulado no layout CNAB 240 |
| 123 | ✅ | retorno: entrada confirmada (02) e liquidação (06) |
| 124 | ✅ | liquidação baixa o boleto e a conta a receber |
| 125 | ✅ | boleto de homologação identificado |
| 126 | ✅ | ficha técnica marca o produto como produzido |
| 127 | ✅ | produto não pode ser insumo da própria ficha |
| 128 | ✅ | OP calcula a necessidade pela ficha (20 kg = 2 receitas = 12 kg de farinha) |
| 129 | ✅ | concluir OP: baixa insumos, entra o produto e apura o custo real (48 + 4 + 10 = 62 → R$ 3,10/kg) |
| 130 | ✅ | custo médio do produto acabado atualizado |
| 131 | ✅ | OP concluída não é concluída de novo |
| 132 | ✅ | impressão da OP |
| 133 | ✅ | CIOT obrigatório só para transportador autônomo contratado |
| 134 | ✅ | Pamcard sem credenciais: orienta a configurar |
| 135 | ✅ | Pamcard: envia a declaração (autenticada) e grava o CIOT devolvido |
| 136 | ✅ | frete com CIOT declarado não é cancelado antes do CIOT |
| 137 | ✅ | contas do frete: adiantamento (800) + saldo (1.200 + 100 de pedágio) |
| 138 | ✅ | contas do frete geradas uma única vez |
| 139 | ✅ | frete em trânsito → entregue |
| 140 | ✅ | CIOT pelo simulador |
| 141 | ✅ | cancelamento do CIOT |
| 142 | ✅ | declaração do CIOT exige motorista com CPF válido |
| 143 | ✅ | provedor TEF real ausente: recusa com explicação (não simula) |
| 144 | ✅ | TEF simulador e comprovante |
| 145 | ✅ | ESC/POS: inicializa, centraliza, corta, abre a gaveta e acentua em CP850 (ç = 0x87) |
| 146 | ✅ | marcações removidas na tela; lista de impressoras instaladas |
| 147 | ✅ | IBS/CBS com redução de 60% (CBS 0,9% → 0,36%) |
| 148 | ✅ | IBS/CBS destacados sem somar ao total da nota (regra de 2026) |
| 149 | ✅ | campos da reforma gravados e recarregados |
| 150 | ✅ | reforma desligada não calcula IBS/CBS |
| 151 | ✅ | DANFE com o quadro da reforma |
| 152 | ✅ | tela Principal: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 153 | ✅ | tela Principal |
| 154 | ✅ | tela Clientes e fornecedores: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 155 | ✅ | tela Clientes e fornecedores |
| 156 | ✅ | tela Produtos: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 157 | ✅ | tela Produtos |
| 158 | ✅ | tela Categorias: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 159 | ✅ | tela Categorias |
| 160 | ✅ | tela Usuários: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 161 | ✅ | tela Usuários |
| 162 | ✅ | tela PDV: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 163 | ✅ | tela PDV |
| 164 | ✅ | tela Vendas: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 165 | ✅ | tela Vendas |
| 166 | ✅ | tela Compras: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 167 | ✅ | tela Compras |
| 168 | ✅ | tela Estoque: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 169 | ✅ | tela Estoque |
| 170 | ✅ | tela Financeiro: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 171 | ✅ | tela Financeiro |
| 172 | ✅ | tela Relatórios: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 173 | ✅ | tela Relatórios |
| 174 | ✅ | tela Orçamentos: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 175 | ✅ | tela Orçamentos |
| 176 | ✅ | tela Pedidos de venda: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 177 | ✅ | tela Pedidos de venda |
| 178 | ✅ | tela Pedidos de compra: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 179 | ✅ | tela Pedidos de compra |
| 180 | ✅ | tela Notas fiscais: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 181 | ✅ | tela Notas fiscais |
| 182 | ✅ | tela Editor de orçamento: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 183 | ✅ | tela Editor de orçamento |
| 184 | ✅ | tela Editor de pedido novo: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 185 | ✅ | tela Editor de pedido novo |
| 186 | ✅ | tela Editor de nota: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 187 | ✅ | tela Editor de nota |
| 188 | ✅ | tela Editor de nota nova: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 189 | ✅ | tela Editor de nota nova |
| 190 | ✅ | tela Conferência de XML: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 191 | ✅ | tela Conferência de XML |
| 192 | ✅ | tela Busca de produto: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 193 | ✅ | tela Busca de produto |
| 194 | ✅ | tela Comprovante: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 195 | ✅ | tela Comprovante |
| 196 | ✅ | tela Configurações: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 197 | ✅ | tela Configurações |
| 198 | ✅ | tela Banco não encontrado: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 199 | ✅ | tela Banco não encontrado |
| 200 | ✅ | tela Recuperar senha: botão de ajuda visível (sem botão) |
| 201 | ✅ | tela Recuperar senha |
| 202 | ✅ | tela Ajuda: botão de ajuda visível (sem botão) |
| 203 | ✅ | tela Ajuda |
| 204 | ✅ | tela Veículos: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 205 | ✅ | tela Veículos |
| 206 | ✅ | tela Contas bancárias: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 207 | ✅ | tela Contas bancárias |
| 208 | ✅ | tela Fichas técnicas: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 209 | ✅ | tela Fichas técnicas |
| 210 | ✅ | tela Ordens de produção: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 211 | ✅ | tela Ordens de produção |
| 212 | ✅ | tela Fretes: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 213 | ✅ | tela Fretes |
| 214 | ✅ | tela Cobrança: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 215 | ✅ | tela Cobrança |
| 216 | ✅ | tela Boleto: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 217 | ✅ | tela Boleto |
| 218 | ✅ | tela Localizar tela: botão de ajuda visível (sem botão) |
| 219 | ✅ | tela Localizar tela |
| 220 | ✅ | tela Pagamento: botão de ajuda visível (sem botão) |
| 221 | ✅ | tela Pagamento |
| 222 | ✅ | tela TEF simulador: botão de ajuda visível (sem botão) |
| 223 | ✅ | tela TEF simulador |
| 224 | ✅ | tela Painel gerencial: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 225 | ✅ | tela Painel gerencial |
| 226 | ✅ | tela Log de atividades: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 227 | ✅ | tela Log de atividades |
| 228 | ✅ | o ? de cada tela abre só a explicação daquela tela |
| 229 | ✅ | a tela abre embutida no espaço à direita do menu (sem janela solta) |
| 230 | ✅ | PDV com fundo branco |
| 231 | ✅ | segunda tela vira outra aba; a anterior fica guardada atrás |
| 232 | ✅ | abrir uma tela já aberta só a traz para frente (sem duplicar) |
| 233 | ✅ | fechar a tela remove a aba e volta para a anterior |
| 234 | ✅ | sem telas abertas, volta à tela inicial |
| 235 | ✅ | log gravado em banco separado (loja_log.db) |
| 236 | ✅ | venda gera log de dados (tabelas gravadas e id do registro) |
| 237 | ✅ | migrações registram um resumo (sem cada comando) |
| 238 | ✅ | abrir e fechar telas gera log |
| 239 | ✅ | gravação desfeita (rollback) não aparece no log |
| 240 | ✅ | gravação confirmada aparece no log |
| 241 | ✅ | clique em botão gera log |
| 242 | ✅ | classificação dos comandos SQL para o log |
| 243 | ✅ | senhas não aparecem no log |
| 244 | ✅ | registros antigos são apagados conforme o parâmetro de retenção |
| 245 | ✅ | consulta do log com filtros |
| 246 | ✅ | grade sem registros mostra explicação (estado vazio) |
| 247 | ✅ | atalho .lnk do Windows criado (usado na primeira execução) |
| 248 | ✅ | toda tela aberta tem tópico de ajuda |
| 249 | ✅ | todos os diálogos têm ajuda (9 diálogos) |
| 250 | ✅ | todas as telas do sistema foram abertas no teste |
| 251 | ✅ | manual do usuário gerado em docs/12-manual-do-usuario.md a partir da ajuda |
| 252 | ✅ | dados de demonstração gerados em 2,6 s — Dados de demonstração gerados: 35 clientes, 8 fornecedores, 50 produtos, 48 compras, 77 ordens de produção, 1403 vendas, 7 orçamentos/pedidos, 3 fretes, 8 boletos, 5 notas fiscais. |
| 253 | ✅ | dados de demonstração íntegros (chaves estrangeiras) |
| 254 | ✅ | insumos não ficam negativos na demonstração |
| 255 | ✅ | CPF/CNPJ da demonstração são válidos |
| 256 | ✅ | modelo de dados (dicionário + diagrama ER) gerado em docs/06-modelo-de-dados.md |
| 257 | ✅ | [Cadastros] o próprio usuário aparece na lista |
| 258 | ✅ | [Cadastros] administrador não consegue se inativar |
| 259 | ✅ | [Cadastros] continua ativo |
| 260 | ✅ | [Cadastros] administrador não tira o próprio acesso |
| 261 | ✅ | [Cadastros] continua administrador |
| 262 | ✅ | [Cadastros] senha curta é recusada |
| 263 | ✅ | [Cadastros] username repetido (maiúsculas diferentes) é recusado \| Não foi possível salvar: já existe outro registro com este "Username". Informe outro valor. |
| 264 | ✅ | [Cadastros] usuário comum cadastrado |
| 265 | ✅ | [Cadastros] senha gravada como hash e conferível |
| 266 | ✅ | [Cadastros] editar com senha em branco mantém a senha |
| 267 | ✅ | [Cadastros] localizador de telas esconde as telas de administrador do usuário comum |
| 268 | ✅ | [Cadastros] o único administrador ativo não pode ser inativado, rebaixado nem excluído |
| 269 | ✅ | [Cadastros] inativar usuário comum é permitido |
| 270 | ✅ | [Cadastros] havendo outro administrador ativo, o primeiro pode ser rebaixado |
| 271 | ✅ | [Cadastros] seleciona o próprio usuário |
| 272 | ✅ | [Cadastros] não exclui o próprio usuário |
| 273 | ✅ | [Cadastros] CPF com dígito errado é recusado |
| 274 | ✅ | [Cadastros] sem papel marcado é recusado |
| 275 | ✅ | [Cadastros] CPF/CNPJ de pessoa já cadastrada é recusado e a mensagem diz quem é \| Já existe um cadastro com este CPF/CNPJ: "Diego Lima Rocha" (registro nº 1).
Em vez de cadastrar de novo, edite esse registro e marque os papéis que faltam (cliente, fornecedor...). |
| 276 | ✅ | [Cadastros] nenhuma pessoa duplicada gravada |
| 277 | ✅ | [Cadastros] cadastro corrigido é salvo sem erro |
| 278 | ✅ | [Cadastros] pessoa nova gravada uma única vez |
| 279 | ✅ | [Cadastros] pessoa existente aparece na lista |
| 280 | ✅ | [Cadastros] salvar a própria pessoa não acusa CPF duplicado |
| 281 | ✅ | [Cadastros] alteração gravada |
| 282 | ✅ | [Cadastros] cliente com vendas aparece na lista |
| 283 | ✅ | [Cadastros] excluir pessoa com histórico oferece inativar |
| 284 | ✅ | [Cadastros] pessoa inativada e vendas preservadas |
| 285 | ✅ | [Cadastros] filtro Inativos mostra a pessoa inativada |
| 286 | ✅ | [Cadastros] categoria em uso aparece na lista |
| 287 | ✅ | [Cadastros] excluir categoria em uso explica o que fazer e não exclui |
| 288 | ✅ | [Cadastros] categoria repetida (maiúsculas) é recusada \| Não foi possível salvar: já existe outro registro com este "Nome". Informe outro valor. |
| 289 | ✅ | [Cadastros] categoria nova salva |
| 290 | ✅ | [Cadastros] categoria sem produtos é excluída |
| 291 | ✅ | [Cadastros] produto com movimentação aparece na lista |
| 292 | ✅ | [Cadastros] editar o preço não arredonda o custo médio (4 casas) |
| 293 | ✅ | [Cadastros] custo médio de produto com movimentação não é alterado à mão |
| 294 | ✅ | [Cadastros] custo inicial de produto sem movimentação pode ser informado e corrigido |
| 295 | ✅ | [Cadastros] EAN com dígito verificador errado é recusado |
| 296 | ✅ | [Cadastros] código de barras de outro produto é recusado \| O código de barras 7891950522666 já pertence ao produto "Arroz Tipo 1 5kg" (código D001). No PDV a leitura ficaria ambígua: confira o código lido. |
| 297 | ✅ | [Cadastros] NCM incompleto é recusado |
| 298 | ✅ | [Cadastros] CST com letra (O no lugar de zero) é recusado |
| 299 | ✅ | [Cadastros] redução de IBS acima de 100% é recusada |
| 300 | ✅ | [Cadastros] código repetido (minúsculas) é recusado dizendo qual campo \| Não foi possível salvar: já existe outro registro com este "Código". Informe outro valor. |
| 301 | ✅ | [Cadastros] produto corrigido é salvo |
| 302 | ✅ | [Cadastros] produto gravado com o EAN correto |
| 303 | ✅ | [Cadastros] produto existente aparece na lista |
| 304 | ✅ | [Cadastros] editar produto existente não acusa o próprio código de barras |
| 305 | ✅ | [Cadastros] pesquisa pelo código encontra só o produto novo |
| 306 | ✅ | [PDV] lançar 2*código mostra o total da venda |
| 307 | ✅ | [PDV] F10 + dinheiro grava a venda |
| 308 | ✅ | [PDV] total gravado = 2 × preço |
| 309 | ✅ | [PDV] nenhuma mensagem de erro durante a venda |
| 310 | ✅ | [Cobranca] validador aponta quantidade em branco no trailer |
| 311 | ✅ | [Cobranca] validador aponta valor com letras no segmento P |
| 312 | ✅ | [Cobranca] modelo com tamanho absurdo é recusado com mensagem |
| 313 | ✅ | [Cobranca] conta de demonstração na lista |
| 314 | ✅ | [Cobranca] modelo sem o nosso número é recusado \| O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro. |
| 315 | ✅ | [Cobranca] espécie com letras (DM) é recusada: a remessa exige código numérico \| Espécie do título: informe o código de 2 dígitos do manual do banco (ex.: 02 = duplicata mercantil, 04 = duplicata de serviço). |
| 316 | ✅ | [Cobranca] nosso número maior que o espaço do modelo é recusado \| O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras. |
| 317 | ✅ | [Cobranca] voltar o último nosso número abaixo dos já emitidos é recusado \| Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números. |
| 318 | ✅ | [Cobranca] dias de protesto acima de 99 é recusado \| Dias para protesto: de 0 (não protestar) a 99. |
| 319 | ✅ | [Cobranca] nada inválido foi gravado |
| 320 | ✅ | [Cobranca] conta válida salva normalmente (ambiente PRODUCAO): O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro.: Espécie do título: informe o código de 2 dígitos do manual do banco (ex.: 02 = duplicata mercantil, 04 = duplicata de serviço).: O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras.: Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números.: Dias para protesto: de 0 (não protestar) a 99. |
| 321 | ✅ | [Cobranca] boletos de teste recusados em conta de produção |
| 322 | ✅ | [Cobranca] modelo inválido: boletos de teste explicam o problema |
| 323 | ✅ | [Cobranca] nenhuma conta de teste fica órfã sem boleto |
| 324 | ✅ | [Cobranca] boleto registrado pelo banco |
| 325 | ✅ | [Cobranca] conta A na lista |
| 326 | ✅ | [Cobranca] a confirmação do cancelamento avisa que a conta tem boleto |
| 327 | ✅ | [Cobranca] conta A cancelada |
| 328 | ✅ | [Cobranca] boleto ainda não enviado da conta cancelada é cancelado junto |
| 329 | ✅ | [Cobranca] conta B na lista |
| 330 | ✅ | [Cobranca] a janela de baixa avisa que a conta tem boleto em aberto |
| 331 | ✅ | [Cobranca] conta B recebida no caixa |
| 332 | ✅ | [Cobranca] boleto registrado da conta B fica com baixa pedida |
| 333 | ✅ | [Cobranca] o usuário é avisado de que a baixa vai na próxima remessa |
| 334 | ✅ | [Cobranca] retorno que liquida conta já recebida no caixa avisa possível duplicidade |
| 335 | ✅ | [Cobranca] a baixa do caixa não é sobrescrita pelo retorno |
| 336 | ✅ | [Cobranca] rodapé mostra o total vencido a receber (R$ 2.366,48) |
| 337 | ✅ | [Cobranca] mudar as datas do período já atualiza a lista (sem clicar em Pesquisar) |
| 338 | ✅ | [Cobranca] boleto com vencimento passado é recusado com explicação |
| 339 | ✅ | [Cobranca] janela avisa das contas vencidas e pede o novo vencimento |
| 340 | ✅ | [Cobranca] boleto da conta vencida sai com o novo vencimento |
| 341 | ✅ | [Cobranca] fator de vencimento do código de barras usa o novo vencimento |
| 342 | ✅ | [Cobranca] nenhum erro ao emitir |
| 343 | ✅ | [Cobranca] a conta nova aparece para emitir boleto |
| 344 | ✅ | [Cobranca] boleto emitido para a conta escolhida (só ela) |
| 345 | ✅ | [Cobranca] código de barras com 44 dígitos |
| 346 | ✅ | [Cobranca] código começa com o banco e a moeda 9 |
| 347 | ✅ | [Cobranca] fator de vencimento 1607 (contagem reiniciada em 22/02/2025) |
| 348 | ✅ | [Cobranca] valor em centavos no código de barras |
| 349 | ✅ | [Cobranca] DV geral do código de barras (módulo 11) |
| 350 | ✅ | [Cobranca] linha digitável com 47 dígitos |
| 351 | ✅ | [Cobranca] DVs dos três campos da linha digitável (módulo 10) |
| 352 | ✅ | [Cobranca] a linha digitável remonta exatamente o código de barras |
| 353 | ✅ | [Cobranca] nosso número com 11 dígitos dentro do campo livre |
| 354 | ✅ | [Cobranca] boleto duplicado para a mesma conta é recusado |
| 355 | ✅ | [Cobranca] boleto de conta sem cliente é recusado (RN-COB-01) |
| 356 | ✅ | [Cobranca] boleto aparece na lista de ativos |
| 357 | ✅ | [Cobranca] ficha com linha digitável, valor e aviso de homologação |
| 358 | ✅ | [Cobranca] Visualizar / imprimir abre o boleto |
| 359 | ✅ | [Cobranca] nenhum erro na emissão pela tela |
| 360 | ✅ | [Cobranca] 3 boletos de teste emitidos |
| 361 | ✅ | [Cobranca] remessa de teste com os 3 boletos |
| 362 | ✅ | [Cobranca] simulação pela tela informa os 3 liquidados \| Retorno SIMULADO_REMESSA_2.ret: 6 registro(s), 3 entrada(s) confirmada(s), 0 rejeitada(s), 3 liquidado(s) (R$ 13,50), 0 não encontrado(s).

As contas a receber liquidadas foram baixadas no financeiro. |
| 363 | ✅ | [Cobranca] contas de teste baixadas pelo retorno simulado |
| 364 | ✅ | [Cobranca] registro da homologação mostra o retorno processado |
| 365 | ✅ | [Cobranca] remessa de produção recusa boletos de teste esquecidos |
| 366 | ✅ | [Cobranca] boletos de teste continuam só emitidos (nada foi enviado) |
| 367 | ✅ | [Cobranca] em produção a simulação de retorno é recusada (não baixa contas sem dinheiro) |
| 368 | ✅ | [Cobranca] conta liquidada aparece em Pagas |
| 369 | ✅ | [Cobranca] estorno de conta liquidada pelo banco pede confirmação com aviso |
| 370 | ✅ | [Cobranca] conta estornada volta para aberta |
| 371 | ✅ | [Cobranca] conta estornada pode receber boleto novo (o pago não conta como ativo) |
| 372 | ✅ | [Cobranca] boleto registrado na lista |
| 373 | ✅ | [Cobranca] boleto registrado: cancelar pede a baixa |
| 374 | ✅ | [Cobranca] remessa leva o pedido de baixa (movimento 02) |
| 375 | ✅ | [Cobranca] retorno do pedido de baixa deixa o boleto BAIXADO |
| 376 | ✅ | [Cobranca] baixa do boleto não mexe na conta a receber |
| 377 | ✅ | [Cobranca] pedido de baixa atendido |
| 378 | ✅ | [Cobranca] remessa de homologação com prefixo TESTE_ |
| 379 | ✅ | [Cobranca] remessa com o boleto novo e linhas de 240 posições |
| 380 | ✅ | [Cobranca] remessa passa no validador CNAB 240 |
| 381 | ✅ | [Cobranca] boleto fica ENVIADO após a remessa |
| 382 | ✅ | [Cobranca] remessa vazia é recusada com explicação |
| 383 | ✅ | [Cobranca] retorno confirma e liquida o boleto |
| 384 | ✅ | [Cobranca] liquidação baixa a conta a receber |
| 385 | ✅ | [Cobranca] boleto fica PAGO |
| 386 | ✅ | [Cobranca] o mesmo retorno importado de novo é recusado |
| 387 | ✅ | [Cobranca] retorno repetido não é registrado de novo |
| 388 | ✅ | [Cobranca] liquidação repetida em outro retorno não é contada de novo |
| 389 | ✅ | [Cobranca] retorno de outro banco é recusado dizendo os dois bancos |
| 390 | ✅ | [Cobranca] validador aponta a linha com tamanho errado |
| 391 | ✅ | [Cobranca] venda a prazo em 2 parcelas |
| 392 | ✅ | [Cobranca] boleto não enviado da venda cancelada é cancelado |
| 393 | ✅ | [Cobranca] boleto registrado da venda cancelada fica com pedido de baixa para a próxima remessa |
| 394 | ✅ | [Compras] recebimento deu entrada de 5 |
| 395 | ✅ | [Compras] confirmação avisa que o produto ficará com estoque negativo (2 − 5 = −3) |
| 396 | ✅ | [Compras] confirmação avisa da parcela já paga |
| 397 | ✅ | [Compras] respondendo Não, a compra continua |
| 398 | ✅ | [Compras] respondendo Sim, a compra é cancelada |
| 399 | ✅ | [Compras] estoque fica negativo (−3) porque 3 já foram vendidos (RN-EST-04) |
| 400 | ✅ | [Compras] contas da compra canceladas |
| 401 | ✅ | [Compras] pedido de compra volta para ABERTO |
| 402 | ✅ | [Compras] movimentação SAIDA de 5 com saldo −3 |
| 403 | ✅ | [Compras] cancelar de novo avisa que já está cancelada |
| 404 | ✅ | [Compras] salvar avisa que o desconto do item é maior que o valor do item |
| 405 | ✅ | [Compras] pedido com desconto inválido não é gravado |
| 406 | ✅ | [Compras] Pedidos.Salvar recusa item com desconto maior que o valor |
| 407 | ✅ | [Compras] pedido salvo com total 22,00 |
| 408 | ✅ | [Compras] pedido aparece em aberto na lista |
| 409 | ✅ | [Compras] edição grava quantidade 7 e remove o 2º item |
| 410 | ✅ | [Compras] total recalculado 7 × 5,00 = 35,00 |
| 411 | ✅ | [Compras] pedido cancelado |
| 412 | ✅ | [Compras] pedido cancelado não pode ser recebido |
| 413 | ✅ | [Compras] pedido cancelado abre só para consulta |
| 414 | ✅ | [Compras] pergunta se o item sem custo é bonificação |
| 415 | ✅ | [Compras] respondendo Não, nada é recebido |
| 416 | ✅ | [Compras] custo médio e estoque intactos |
| 417 | ✅ | [Compras] pedido fica salvo em aberto para corrigir o custo |
| 418 | ✅ | [Compras] pedido recebido com a NF 4567 |
| 419 | ✅ | [Compras] fornecedor localizado pelo CNPJ com pontuação |
| 420 | ✅ | [Compras] "Lançar compra" vem desmarcado porque a NF já entrou pelo pedido |
| 421 | ✅ | [Compras] a conferência explica o motivo |
| 422 | ✅ | [Compras] nota registrada |
| 423 | ✅ | [Compras] nenhuma compra nova (sem entrada em dobro) |
| 424 | ✅ | [Compras] estoque não dobrou |
| 425 | ✅ | [Compras] receber com NF repetida pede confirmação |
| 426 | ✅ | [Compras] respondendo Não, nada é lançado |
| 427 | ✅ | [Compras] o pedido continua em aberto para receber depois |
| 428 | ✅ | [Compras] o usuário é avisado de que o pedido ficou salvo em aberto |
| 429 | ✅ | [Compras] pedido recebido com total R$ 31,00 (3 × 10 + frete 1) |
| 430 | ✅ | [Compras] compra gravada com o total do pedido (gravou 31) |
| 431 | ✅ | [Compras] 2 parcelas a pagar somando R$ 31,00 (soma 31,0) |
| 432 | ✅ | [Compras] nº da nota fiscal gravado na compra |
| 433 | ✅ | [Compras] estoque do item 1 aumentou 1 |
| 434 | ✅ | [Compras] custo médio do item 1 com frete rateado (23,1134 ≈ 23,1134) |
| 435 | ✅ | [Compras] estoque do item 2 aumentou 1 |
| 436 | ✅ | [Compras] custo médio do item 2 com frete rateado (6,4204 ≈ 6,4204) |
| 437 | ✅ | [Compras] estoque do item 3 aumentou 1 |
| 438 | ✅ | [Compras] custo médio do item 3 com frete rateado (3,2054 ≈ 3,2054) |
| 439 | ✅ | [Compras] nenhum erro |
| 440 | ✅ | [Compras] lixo.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 441 | ✅ | [Compras] vazio.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 442 | ✅ | [Compras] outro.xml: mensagem clara em português (O arquivo não é um XML de NF-e (infNFe não encontrado). Confira se escolheu o XML da nota, e não outro arquivo.) |
| 443 | ✅ | [Compras] truncado.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 444 | ✅ | [Compras] não sugere produto inativo pelo código de barras |
| 445 | ✅ | [Compras] não sugere produto inativo pelo vínculo memorizado |
| 446 | ✅ | [Compras] conferência abre sem erro de grade |
| 447 | ✅ | [Compras] importação concluída cadastrando produtos novos |
| 448 | ✅ | [Compras] vínculo com o produto inativo é substituído pelo produto novo |
| 449 | ✅ | [Compras] 1ª importação concluída |
| 450 | ✅ | [Compras] com fator 1 entram só 2 unidades |
| 451 | ✅ | [Compras] item sem vínculo vira produto novo |
| 452 | ✅ | [Compras] duplicatas (2 × 218) lançadas a pagar |
| 453 | ✅ | [Compras] compra importada aparece em Compras realizadas |
| 454 | ✅ | [Compras] compra cancelada |
| 455 | ✅ | [Compras] estoque volta ao saldo anterior |
| 456 | ✅ | [Compras] contas a pagar da compra canceladas |
| 457 | ✅ | [Compras] nota cuja compra foi cancelada pode ser importada de novo |
| 458 | ✅ | [Compras] vínculos da 1ª importação foram memorizados |
| 459 | ✅ | [Compras] 2ª importação concluída |
| 460 | ✅ | [Compras] com fator 10 entram 20 unidades |
| 461 | ✅ | [Compras] reimportação não duplica o produto novo |
| 462 | ✅ | [Compras] só uma nota ativa com a chave |
| 463 | ✅ | [Compras] nota aponta para a nova compra |
| 464 | ✅ | [Compras] depois de reimportada, a nota volta a constar como importada |
| 465 | ✅ | [Estoque] editor abre com os 2 produtos abaixo do mínimo (2) |
| 466 | ✅ | [Estoque] sugestão 10 − 2 = 8 pelo custo médio 4,50 |
| 467 | ✅ | [Estoque] saldo negativo: sugestão 3 − (−1) = 4 |
| 468 | ✅ | [Estoque] pedido de compra gravado |
| 469 | ✅ | [Estoque] pedido de compra ABERTO do fornecedor escolhido |
| 470 | ✅ | [Estoque] total do pedido = 8 × 4,50 + 4 × 7,00 |
| 471 | ✅ | [Estoque] usuário é informado do número do pedido |
| 472 | ✅ | [Estoque] gerar pedido não mexe no estoque (RN-PED-05) |
| 473 | ✅ | [Estoque] sem produtos abaixo do mínimo, avisa e não abre pedido |
| 474 | ✅ | [Estoque] filtro pelo nome mostra o produto |
| 475 | ✅ | [Estoque] resumo acompanha a pesquisa (1 produto(s) · Valor do estoque (custo): R$ 694,27; 1 na grade) |
| 476 | ✅ | [Estoque] produto selecionado na grade |
| 477 | ✅ | [Estoque] venda baixou 2 (saldo real 28) |
| 478 | ✅ | [Estoque] definir saldo grava exatamente o contado (saldo 25) |
| 479 | ✅ | [Estoque] movimentação AJUSTE de 3 com saldo 25 |
| 480 | ✅ | [Estoque] motivo registrado na origem |
| 481 | ✅ | [Estoque] histórico do produto mostra o ajuste logo após aplicar |
| 482 | ✅ | [Estoque] saldo igual ao atual não gera movimentação |
| 483 | ✅ | [Estoque] avisa que nada foi alterado |
| 484 | ✅ | [Estoque] ajuste sem motivo é recusado |
| 485 | ✅ | [Estoque] nada mudou sem motivo |
| 486 | ✅ | [Estoque] definir saldo 0 zera o estoque |
| 487 | ✅ | [Estoque] pergunta antes de deixar o saldo negativo |
| 488 | ✅ | [Estoque] respondendo Não, o saldo não muda |
| 489 | ✅ | [Estoque] saída de 10 deixa saldo 2 |
| 490 | ✅ | [Estoque] somente baixo lista o produto: BAIXO, sugestão de compra 3 |
| 491 | ✅ | [Estoque] somente baixo não lista produtos OK |
| 492 | ✅ | [Financeiro] filtro Vencidas só mostra contas vencidas |
| 493 | ✅ | [Financeiro] conta vencida há 5 dias aparece com 5 dias de atraso |
| 494 | ✅ | [Financeiro] seleciona a conta vencida |
| 495 | ✅ | [Financeiro] baixa com data futura é recusada com explicação |
| 496 | ✅ | [Financeiro] baixa grava data, valor com juros e forma |
| 497 | ✅ | [Financeiro] baixar duas vezes a mesma conta é recusado |
| 498 | ✅ | [Financeiro] conta aparece em Pagas |
| 499 | ✅ | [Financeiro] estorno volta a conta para aberta e limpa o pagamento (data, valor e forma) |
| 500 | ✅ | [Financeiro] conta estornada volta a aparecer em aberto |
| 501 | ✅ | [Financeiro] cancelar conta aberta |
| 502 | ✅ | [Financeiro] conta cancelada não pode ser baixada |
| 503 | ✅ | [Financeiro] conta continua cancelada |
| 504 | ✅ | [Financeiro] conta na lista |
| 505 | ✅ | [Financeiro] valor 10× maior pede confirmação; "Não" mantém a janela aberta |
| 506 | ✅ | [Financeiro] baixa gravada com o valor corrigido |
| 507 | ✅ | [Financeiro] sem descrição a nova conta não é salva e o motivo é mostrado |
| 508 | ✅ | [Financeiro] 3 parcelas geradas (3) |
| 509 | ✅ | [Financeiro] valores 33,33 / 33,33 / 33,34 (centavos na última) |
| 510 | ✅ | [Financeiro] vencimentos mensais a partir do 1º |
| 511 | ✅ | [Financeiro] descrição "… parcela k/n" |
| 512 | ✅ | [Financeiro] parcelas a receber, em aberto e com o cliente escolhido |
| 513 | ✅ | [Financeiro] a lista é atualizada com as parcelas novas |
| 514 | ✅ | [Fiscal] trocar a UF do destinatário para RJ muda o CFOP para 6102 |
| 515 | ✅ | [Fiscal] CFOP de dentro do estado para cliente de outra UF é barrado na emissão |
| 516 | ✅ | [Fiscal] desconto maior que o valor do item é barrado na emissão |
| 517 | ✅ | [Fiscal] nota emitida para a venda |
| 518 | ✅ | [Fiscal] numeração definitiva e chave de 44 dígitos com DV |
| 519 | ✅ | [Fiscal] itens gravados com CFOP 6102 |
| 520 | ✅ | [Fiscal] venda com nota abre a nota existente sem permitir alterar |
| 521 | ✅ | [Fiscal] camada de dados recusa segunda nota para a mesma venda (RN-FIS-01) |
| 522 | ✅ | [Fiscal] nota emitida aparece na lista |
| 523 | ✅ | [Fiscal] usuário comum não cancela nota emitida |
| 524 | ✅ | [Fiscal] nota selecionada de novo |
| 525 | ✅ | [Fiscal] administrador cancela a nota |
| 526 | ✅ | [Fiscal] venda volta a aceitar nota depois do cancelamento |
| 527 | ✅ | [Fiscal] cancelar duas vezes é recusado na camada de dados |
| 528 | ✅ | [Fiscal] respondendo "Sim" o editor fecha descartando a nota |
| 529 | ✅ | [Fiscal] produto inativo não aparece na busca (F3) |
| 530 | ✅ | [Fiscal] F3 + nome + Enter adiciona o produto pelo custo com CFOP 1102 |
| 531 | ✅ | [Fiscal] fechar o editor com item não salvo pergunta antes e "Não" mantém a nota aberta |
| 532 | ✅ | [Fiscal] trocar o modelo usa a série configurada do modelo (65 → 7, 55 → 1) |
| 533 | ✅ | [Fiscal] Enter no código adiciona o item |
| 534 | ✅ | [Fiscal] rascunho gravado como NFC-e série 7 |
| 535 | ✅ | [Fiscal] CFOP de saída numa nota de entrada é acusado |
| 536 | ✅ | [Fiscal] NFC-e numa nota de entrada é acusada |
| 537 | ✅ | [Fiscal] CNPJ do remetente com dígito errado é acusado |
| 538 | ✅ | [Fiscal] desconto adicional maior que os produtos é acusado |
| 539 | ✅ | [Fiscal] CFOP interestadual com fornecedor do mesmo estado é acusado |
| 540 | ✅ | [Fiscal] nota corrigida não tem pendências:  |
| 541 | ✅ | [Fiscal] rascunho com pendências pode ser salvo |
| 542 | ✅ | [Fiscal] rascunho aparece no filtro Em digitação |
| 543 | ✅ | [Fiscal] emitir rascunho com CFOP errado mostra o motivo e não emite |
| 544 | ✅ | [Fiscal] rascunho selecionado |
| 545 | ✅ | [Fiscal] nota de entrada corrigida é emitida |
| 546 | ✅ | [Fiscal] nota de venda cancelada não é emitida: A venda nº 1 foi cancelada: não emita nota para ela (exclua este rascunho). |
| 547 | ✅ | [Fiscal] rascunho aparece em Em digitação |
| 548 | ✅ | [Fiscal] rascunho e itens excluídos |
| 549 | ✅ | [Gestão] faturamento dos últimos 30 dias confere com o banco (R$ 17.218,09) |
| 550 | ✅ | [Gestão] quantidade de vendas dos últimos 30 dias confere |
| 551 | ✅ | [Gestão] ticket médio = faturamento ÷ vendas |
| 552 | ✅ | [Gestão] período 'Hoje' abre sem erro |
| 553 | ✅ | [Gestão] período 'Últimos 7 dias' abre sem erro |
| 554 | ✅ | [Gestão] período 'Últimos 30 dias' abre sem erro |
| 555 | ✅ | [Gestão] período 'Este mês' abre sem erro |
| 556 | ✅ | [Gestão] período 'Mês anterior' abre sem erro |
| 557 | ✅ | [Gestão] período 'Últimos 90 dias' abre sem erro |
| 558 | ✅ | [Gestão] período 'Este ano' abre sem erro |
| 559 | ✅ | [Gestão] período 'Últimos 12 meses' abre sem erro |
| 560 | ✅ | [Gestão] relatório 'Vendas por dia' em 6 meses gera sem erro |
| 561 | ✅ | [Gestão] relatório 'Produtos mais vendidos' em 6 meses gera sem erro |
| 562 | ✅ | [Gestão] relatório 'Vendas por cliente' em 6 meses gera sem erro |
| 563 | ✅ | [Gestão] relatório 'Vendas por usuário (vendedor)' em 6 meses gera sem erro |
| 564 | ✅ | [Gestão] relatório 'Lucro bruto (resumo do período)' em 6 meses gera sem erro |
| 565 | ✅ | [Gestão] relatório 'Fluxo de caixa (realizado)' em 6 meses gera sem erro |
| 566 | ✅ | [Gestão] relatório 'Fluxo de caixa (previsto - contas em aberto)' em 6 meses gera sem erro |
| 567 | ✅ | [Gestão] relatório 'Produtos sem venda no período' em 6 meses gera sem erro |
| 568 | ✅ | [Gestão] relatório 'Compras por fornecedor' em 6 meses gera sem erro |
| 569 | ✅ | [Gestão] 'Produtos sem venda' não lista insumos de produção (0 insumo(s) na lista) |
| 570 | ✅ | [Gestão] 'Vendas por dia' soma 101.050,38 = banco 101.050,38 |
| 571 | ✅ | [Gestão] trocar o período já atualiza o relatório, sem precisar clicar em Gerar |
| 572 | ✅ | [Gestão] 'Vendas por dia' num período sem movimento: 0 linha(s), sem erro |
| 573 | ✅ | [Gestão] 'Produtos mais vendidos' num período sem movimento: 0 linha(s), sem erro |
| 574 | ✅ | [Gestão] 'Vendas por cliente' num período sem movimento: 0 linha(s), sem erro |
| 575 | ✅ | [Gestão] 'Vendas por usuário (vendedor)' num período sem movimento: 0 linha(s), sem erro |
| 576 | ✅ | [Gestão] 'Lucro bruto (resumo do período)' num período sem movimento: 1 linha(s), sem erro |
| 577 | ✅ | [Gestão] 'Fluxo de caixa (realizado)' num período sem movimento: 0 linha(s), sem erro |
| 578 | ✅ | [Gestão] 'Fluxo de caixa (previsto - contas em aberto)' num período sem movimento: 0 linha(s), sem erro |
| 579 | ✅ | [Gestão] 'Produtos sem venda no período' num período sem movimento: 40 linha(s), sem erro |
| 580 | ✅ | [Gestão] 'Compras por fornecedor' num período sem movimento: 0 linha(s), sem erro |
| 581 | ✅ | [Gestão] datas invertidas avisam que a data final é anterior à inicial |
| 582 | ✅ | [Gestão] sem dados, período 'Hoje' abre sem erro |
| 583 | ✅ | [Gestão] sem dados, 'Hoje' mostra faturamento zero e margem '-' |
| 584 | ✅ | [Gestão] sem dados, período 'Últimos 7 dias' abre sem erro |
| 585 | ✅ | [Gestão] sem dados, 'Últimos 7 dias' mostra faturamento zero e margem '-' |
| 586 | ✅ | [Gestão] sem dados, período 'Últimos 30 dias' abre sem erro |
| 587 | ✅ | [Gestão] sem dados, 'Últimos 30 dias' mostra faturamento zero e margem '-' |
| 588 | ✅ | [Gestão] sem dados, período 'Este mês' abre sem erro |
| 589 | ✅ | [Gestão] sem dados, 'Este mês' mostra faturamento zero e margem '-' |
| 590 | ✅ | [Gestão] sem dados, período 'Mês anterior' abre sem erro |
| 591 | ✅ | [Gestão] sem dados, 'Mês anterior' mostra faturamento zero e margem '-' |
| 592 | ✅ | [Gestão] sem dados, período 'Últimos 90 dias' abre sem erro |
| 593 | ✅ | [Gestão] sem dados, 'Últimos 90 dias' mostra faturamento zero e margem '-' |
| 594 | ✅ | [Gestão] sem dados, período 'Este ano' abre sem erro |
| 595 | ✅ | [Gestão] sem dados, 'Este ano' mostra faturamento zero e margem '-' |
| 596 | ✅ | [Gestão] sem dados, período 'Últimos 12 meses' abre sem erro |
| 597 | ✅ | [Gestão] sem dados, 'Últimos 12 meses' mostra faturamento zero e margem '-' |
| 598 | ✅ | [Gestão] sem dados, relatório 'Vendas por dia' gera sem erro |
| 599 | ✅ | [Gestão] sem dados, relatório 'Produtos mais vendidos' gera sem erro |
| 600 | ✅ | [Gestão] sem dados, relatório 'Vendas por cliente' gera sem erro |
| 601 | ✅ | [Gestão] sem dados, relatório 'Vendas por usuário (vendedor)' gera sem erro |
| 602 | ✅ | [Gestão] sem dados, relatório 'Lucro bruto (resumo do período)' gera sem erro |
| 603 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (realizado)' gera sem erro |
| 604 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (previsto - contas em aberto)' gera sem erro |
| 605 | ✅ | [Gestão] sem dados, relatório 'Produtos sem venda no período' gera sem erro |
| 606 | ✅ | [Gestão] sem dados, relatório 'Compras por fornecedor' gera sem erro |
| 607 | ✅ | [Interface] primeiro acesso com a senha padrão exige senha nova (recusa a padrão, senhas diferentes e curtas) |
| 608 | ✅ | [Interface] senha nova gravada (como hash) e a padrão deixa de valer |
| 609 | ✅ | [Interface] com a senha nova entra direto |
| 610 | ✅ | [Interface] o menu lateral destaca a tela em primeiro plano |
| 611 | ✅ | [Interface] o destaque do menu acompanha a troca de tela |
| 612 | ✅ | [Interface] a tela inicial mostra "Continuar de onde parou" com as telas usadas por último: Contas a pagar e receber, Estoque |
| 613 | ✅ | [Interface] o localizador (Ctrl+K) vazio mostra as telas recentes primeiro |
| 614 | ✅ | [Logística] fretes: 'Abrir / editar' sem seleção avisa para selecionar |
| 615 | ✅ | [Logística] fretes: 'Imprimir' sem seleção avisa para selecionar |
| 616 | ✅ | [Logística] fretes: 'Cancelar CIOT' sem seleção avisa para selecionar |
| 617 | ✅ | [Logística] OP: Imprimir sem seleção avisa para selecionar |
| 618 | ✅ | [Logística] placa com hífen é recusada com orientação |
| 619 | ✅ | [Logística] RENAVAM com letras é recusado |
| 620 | ✅ | [Logística] RNTRC curto é recusado |
| 621 | ✅ | [Logística] veículo válido é salvo |
| 622 | ✅ | [Logística] a mesma placa em minúsculas não é cadastrada de novo |
| 623 | ✅ | [Logística] frete com CIOT declarado só cancela depois do CIOT |
| 624 | ✅ | [Logística] cancelar CIOT exige motivo com 15 caracteres; com motivo válido cancela |
| 625 | ✅ | [Logística] viagem com CIOT cancelado não é liberada |
| 626 | ✅ | [Logística] cancelar o frete cancela as contas abertas dele |
| 627 | ✅ | [Logística] frete cancelado não gera contas a pagar |
| 628 | ✅ | [Logística] frete cancelado não declara CIOT |
| 629 | ✅ | [Logística] camada de dados exige motivo de 15 caracteres |
| 630 | ✅ | [Logística] transportadora (CNPJ) não exige CIOT |
| 631 | ✅ | [Logística] frete sem valor avisa que falta o valor |
| 632 | ✅ | [Logística] frete sem valor continua podendo gerar contas depois |
| 633 | ✅ | [Logística] frete de transportadora sai sem CIOT |
| 634 | ✅ | [Logística] editar o frete mantém o veículo mesmo depois de desativado |
| 635 | ✅ | [Logística] carga acima da capacidade pergunta; Não mantém o diálogo aberto |
| 636 | ✅ | [Logística] frete de frota própria salvo com o peso corrigido e sem CIOT |
| 637 | ✅ | [Logística] o editor avisa que o CIOT é obrigatório para autônomo: OBRIGATÓRIO (transportador autônomo): declare após salvar |
| 638 | ✅ | [Logística] frete salvo com CIOT pendente |
| 639 | ✅ | [Logística] sem CIOT declarado a viagem não é liberada (em trânsito) |
| 640 | ✅ | [Logística] sem CIOT declarado o frete não vai direto para entregue |
| 641 | ✅ | [Logística] CIOT declarado pelo simulador |
| 642 | ✅ | [Logística] contas a pagar: adiantamento 800 + saldo 1.300 (1.200 + 100 de pedágio) |
| 643 | ✅ | [Logística] as contas do frete são geradas uma única vez |
| 644 | ✅ | [Logística] depois de gerar as contas, valores do frete ficam travados no editor |
| 645 | ✅ | [Logística] com CIOT declarado, a rota (origem/destino declarados à ANTT) fica travada |
| 646 | ✅ | [Logística] com CIOT declarado a viagem sai (em trânsito) |
| 647 | ✅ | [Logística] frete entregue grava a data da entrega |
| 648 | ✅ | [Logística] CIOT encerrado após a entrega |
| 649 | ✅ | [Logística] frete entregue não é cancelado |
| 650 | ✅ | [Logística] Pamcard sem credenciais: orienta a configurar e nada muda |
| 651 | ✅ | [Logística] resposta 'sucesso' sem número de CIOT não marca o frete como declarado: PAMCARD não devolveu o número do CIOT. Confira no portal da administradora antes de declarar de novo. Recebido |
| 652 | ✅ | [Logística] Pamcard com resposta completa grava o CIOT e a administradora |
| 653 | ✅ | [Logística] cancelamento do CIOT pela Pamcard |
| 654 | ✅ | [Produção] a OP planejada aparece em aberto |
| 655 | ✅ | [Produção] Imprimir abre a visualização da OP |
| 656 | ✅ | [Produção] impressão da OP lista os insumos e a quantidade prevista |
| 657 | ✅ | [Produção] iniciar produção muda a situação |
| 658 | ✅ | [Produção] BLOQUEAR: produzir mais que o planejado sem insumo é barrado (cacau continua 3) |
| 659 | ✅ | [Produção] BLOQUEAR também vale direto na camada de dados |
| 660 | ✅ | [Produção] PERGUNTAR mostra a necessidade real (3,300) e, respondendo Não, nada é baixado |
| 661 | ✅ | [Produção] produzir menos que o planejado com insumo suficiente não pergunta nada |
| 662 | ✅ | [Produção] concluir 2 de 4: consome metade (3 kg de farinha, 0,55 kg de cacau) e entra 2 bolos |
| 663 | ✅ | [Produção] custo real: (6 + 5,50 + 4 × 2 ÷ 2) ÷ 2 = R$ 7,75 e vira o custo do bolo |
| 664 | ✅ | [Produção] a OP concluída aparece no filtro Todas |
| 665 | ✅ | [Produção] OP concluída não pode ser cancelada |
| 666 | ✅ | [Produção] segunda produção recalcula o custo médio ponderado (R$ 9,25) |
| 667 | ✅ | [Produção] cancelar OP planejada não mexe no estoque |
| 668 | ✅ | [Produção] o produto novo aparece na lista das fichas técnicas |
| 669 | ✅ | [Produção] insumo repetido soma na mesma linha (2 + 1 = 3 kg) |
| 670 | ✅ | [Produção] resumo mostra custo dos insumos (com perda), do lote, unitário e a margem: Insumos R$ 11,50 + adicional R$ 4,00 = lote R$ 15,50   ·   custo unitário R$ 7,75   ·   preço R$ 30,00   ·   margem 287,1% |
| 671 | ✅ | [Produção] o produto não entra como insumo da própria ficha |
| 672 | ✅ | [Produção] salvar a ficha grava os insumos e torna a mercadoria um produto acabado |
| 673 | ✅ | [Produção] OP criada pela ficha calcula a necessidade (6 kg de farinha; 0,5 × 2 × 1,1 = 1,1 kg de cacau) |
| 674 | ✅ | [Produção] ficha circular é recusada com o nome do insumo: O insumo "Torta teste" é fabricado (direta ou indiretamente) com este produto: a receita ficaria circular. Remova esse insumo ou corrija a ficha técnica dele. |
| 675 | ✅ | [Produção] a ficha anterior continua intacta |
| 676 | ✅ | [Produção] perda negativa é recusada com o nome do insumo |
| 677 | ✅ | [Produção] sem alteração, trocar de produto não pergunta nada |
| 678 | ✅ | [Produção] com insumo não salvo, trocar de produto pergunta; respondendo Não, a ficha em edição continua aberta |
| 679 | ✅ | [Produção] fechar a tela com ficha não salva pergunta; respondendo Não, a tela continua aberta |
| 680 | ✅ | [Produção] respondendo Sim, descarta e abre o outro produto (nada gravado) |
| 681 | ✅ | [Sistema] Configurações do administrador tem uma aba por grupo + Minha conta |
| 682 | ✅ | [Sistema] salvar a aba 'Empresa' sem mudar nada não dá erro |
| 683 | ✅ | [Sistema] salvar a aba 'Fiscal' sem mudar nada não dá erro |
| 684 | ✅ | [Sistema] salvar a aba 'Reforma tributária' sem mudar nada não dá erro |
| 685 | ✅ | [Sistema] salvar a aba 'Vendas e compras' sem mudar nada não dá erro |
| 686 | ✅ | [Sistema] salvar a aba 'Produção' sem mudar nada não dá erro |
| 687 | ✅ | [Sistema] salvar a aba 'Transporte e CIOT' sem mudar nada não dá erro |
| 688 | ✅ | [Sistema] salvar a aba 'TEF' sem mudar nada não dá erro |
| 689 | ✅ | [Sistema] salvar a aba 'Impressoras' sem mudar nada não dá erro |
| 690 | ✅ | [Sistema] salvar a aba 'E-mail' sem mudar nada não dá erro |
| 691 | ✅ | [Sistema] salvar a aba 'Banco de dados' sem mudar nada não dá erro |
| 692 | ✅ | [Sistema] próximo número da NF-e 'abc' é recusado |
| 693 | ✅ | [Sistema] próximo número da NF-e '-1' é recusado |
| 694 | ✅ | [Sistema] próximo número da NF-e '0' é recusado |
| 695 | ✅ | [Sistema] próximo número da NF-e '1,5' é recusado |
| 696 | ✅ | [Sistema] dias de log não numérico é recusado |
| 697 | ✅ | [Sistema] dias de log = 0 (manter tudo) é aceito |
| 698 | ✅ | [Sistema] alíquota de 150% é recusada |
| 699 | ✅ | [Sistema] nome da loja em branco é recusado |
| 700 | ✅ | [Sistema] nome da loja válido é gravado |
| 701 | ✅ | [Sistema] 'Pamcard: senha / token' fica mascarado |
| 702 | ✅ | [Sistema] 'Chave da API do Resend' fica mascarado |
| 703 | ✅ | [Sistema] página de teste em impressora inexistente avisa que ela não foi encontrada \| Falha na impressão:
Impressora "Impressora Que Nao Existe ERP" não encontrada. Verifique Configurações > Impressoras. |
| 704 | ✅ | [Sistema] e-mail de teste sem chave diz que o envio não está configurado \| O envio de e-mail não está configurado: preencha a chave da API do Resend e o remetente nesta aba e tente de novo. |
| 705 | ✅ | [Sistema] remetente sem e-mail é recusado antes de enviar |
| 706 | ✅ | [Sistema] e-mail de teste enviado ao e-mail do usuário |
| 707 | ✅ | [Sistema] Fazer backup agora cria o arquivo de backup: %USERPROFILE%\AppData\Local\Temp\erp_testes_9d2cc834a9304d0dba7867c7e6f36e6a\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261002_131406_manual.db \| Backup salvo em:
%USERPROFILE%\AppData\Local\Temp\erp_testes_9d2cc834a9304d0dba7867c7e6f36e6a\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261002_131406_manual.db |
| 708 | ✅ | [Sistema] salvar o nome da loja atualiza na hora o cabeçalho da janela principal: ERP Loja - Mercadinho Novo - Configurações |
| 709 | ✅ | [Sistema] o log registra abrir e fechar Relatórios |
| 710 | ✅ | [Sistema] abrir uma tela registra uma linha só (não duplica): 1 |
| 711 | ✅ | [Sistema] salvar configurações registra a gravação com a tela Configurações |
| 712 | ✅ | [Sistema] a tela de log abre listando registros |
| 713 | ✅ | [Sistema] filtro por categoria TELA mostra só TELA |
| 714 | ✅ | [Sistema] a lista de usuários traz quem aparece no log |
| 715 | ✅ | [Sistema] busca por texto filtra (Enter) |
| 716 | ✅ | [Sistema] busca sem resultado deixa a grade vazia sem erro |
| 717 | ✅ | [Sistema] datas invertidas no log avisam o usuário |
| 718 | ✅ | [Sistema] a tela 'PDV - frente de caixa' tem tópico de ajuda |
| 719 | ✅ | [Sistema] a tela 'Orçamentos' tem tópico de ajuda |
| 720 | ✅ | [Sistema] a tela 'Pedidos de venda' tem tópico de ajuda |
| 721 | ✅ | [Sistema] a tela 'Vendas realizadas' tem tópico de ajuda |
| 722 | ✅ | [Sistema] a tela 'Pedidos de compra' tem tópico de ajuda |
| 723 | ✅ | [Sistema] a tela 'Importar XML de NF-e' tem tópico de ajuda |
| 724 | ✅ | [Sistema] a tela 'Compras realizadas' tem tópico de ajuda |
| 725 | ✅ | [Sistema] a tela 'Fichas técnicas' tem tópico de ajuda |
| 726 | ✅ | [Sistema] a tela 'Ordens de produção' tem tópico de ajuda |
| 727 | ✅ | [Sistema] a tela 'Fretes e CIOT' tem tópico de ajuda |
| 728 | ✅ | [Sistema] a tela 'Veículos' tem tópico de ajuda |
| 729 | ✅ | [Sistema] a tela 'Estoque' tem tópico de ajuda |
| 730 | ✅ | [Sistema] a tela 'Contas a pagar e receber' tem tópico de ajuda |
| 731 | ✅ | [Sistema] a tela 'Cobrança bancária (boletos)' tem tópico de ajuda |
| 732 | ✅ | [Sistema] a tela 'Contas bancárias' tem tópico de ajuda |
| 733 | ✅ | [Sistema] a tela 'Notas fiscais' tem tópico de ajuda |
| 734 | ✅ | [Sistema] a tela 'Painel gerencial' tem tópico de ajuda |
| 735 | ✅ | [Sistema] a tela 'Relatórios' tem tópico de ajuda |
| 736 | ✅ | [Sistema] a tela 'Clientes e fornecedores' tem tópico de ajuda |
| 737 | ✅ | [Sistema] a tela 'Produtos' tem tópico de ajuda |
| 738 | ✅ | [Sistema] a tela 'Categorias' tem tópico de ajuda |
| 739 | ✅ | [Sistema] a tela 'Usuários' tem tópico de ajuda |
| 740 | ✅ | [Sistema] a tela 'Configurações' tem tópico de ajuda |
| 741 | ✅ | [Sistema] a tela 'Log de atividades' tem tópico de ajuda |
| 742 | ✅ | [Sistema] a tela 'Localizar tela' tem tópico de ajuda |
| 743 | ✅ | [Sistema] com uma tela aberta só aparece o '?' dela (visíveis: 1) |
| 744 | ✅ | [Sistema] o '?' fica no canto direito da tela embutida |
| 745 | ✅ | [Sistema] o '?' de Relatórios abre a ajuda de Relatórios |
| 746 | ✅ | [Sistema] a janela principal começa na tela inicial, sem abas |
| 747 | ✅ | [Sistema] F5 abre o Painel gerencial embutido |
| 748 | ✅ | [Sistema] F8 abre Relatórios |
| 749 | ✅ | [Sistema] F6 abre Estoque |
| 750 | ✅ | [Sistema] seis telas abertas ao mesmo tempo (abertas: 6) |
| 751 | ✅ | [Sistema] só a tela ativa fica visível |
| 752 | ✅ | [Sistema] o título da janela mostra a tela ativa |
| 753 | ✅ | [Sistema] F5 de novo traz o painel já aberto (sem abrir outro) |
| 754 | ✅ | [Sistema] Ctrl+Tab vai para a próxima aba |
| 755 | ✅ | [Sistema] Ctrl+Shift+Tab na primeira aba vai para a última |
| 756 | ✅ | [Sistema] Ctrl+W fecha a tela ativa e mostra outra aba |
| 757 | ✅ | [Sistema] a aba da tela ativa tem o botão × |
| 758 | ✅ | [Sistema] clicar no × fecha a tela da aba |
| 759 | ✅ | [Sistema] fechando todas as abas volta à tela inicial |
| 760 | ✅ | [Sistema] Ctrl+W sem tela aberta não faz nada |
| 761 | ✅ | [Sistema] reabrir uma tela depois de fechá-la funciona |
| 762 | ✅ | [Sistema] nenhuma mensagem de erro |
| 763 | ✅ | [Sistema] no PDV embutido F5 abre o desconto, não o painel |
| 764 | ✅ | [Sistema] com o PDV aberto, F5 fora do PDV abre o painel |
| 765 | ✅ | [Sistema] responder Não à venda em andamento mantém a janela principal aberta |
| 766 | ✅ | [Sistema] o sistema avisa que há uma venda em andamento |
| 767 | ✅ | [Sistema] o PDV volta para frente para o usuário concluir a venda |
| 768 | ✅ | [Sistema] as outras telas continuam abertas depois de desistir de fechar (2) |
| 769 | ✅ | [Sistema] respondendo Sim a janela principal fecha |
| 770 | ✅ | [Sistema] buscar 'vender' traz 'PDV - frente de caixa' entre os 3 primeiros (posição 1 de 17: PDV - frente de caixa, Painel gerencial, Vendas realizadas) |
| 771 | ✅ | [Sistema] buscar 'como cadastrar cliente' traz 'Clientes e fornecedores' entre os 3 primeiros (posição 1 de 15: Clientes e fornecedores, Contas a pagar e receber, Orçamentos) |
| 772 | ✅ | [Sistema] buscar 'cadastrar produto' traz 'Produtos' entre os 3 primeiros (posição 2 de 19: Importar XML de NF-e, Produtos, Categorias) |
| 773 | ✅ | [Sistema] buscar 'onde fica o estoque' traz 'Estoque' entre os 3 primeiros (posição 1 de 14: Estoque, Compras realizadas, Configurações) |
| 774 | ✅ | [Sistema] buscar 'trocar minha senha' traz 'Configurações' entre os 3 primeiros (posição 1 de 6: Configurações, Log de atividades, Usuários) |
| 775 | ✅ | [Sistema] buscar 'fazer backup' traz 'Configurações' entre os 3 primeiros (posição 1 de 2: Configurações, Log de atividades) |
| 776 | ✅ | [Sistema] buscar 'emitir nota' traz 'Notas fiscais' entre os 3 primeiros (posição 1 de 12: Notas fiscais, Pedidos de venda, Vendas realizadas) |
| 777 | ✅ | [Sistema] buscar 'pagar fornecedor' traz 'Contas a pagar e receber' entre os 3 primeiros (posição 1 de 15: Contas a pagar e receber, Importar XML de NF-e, Pedidos de compra) |
| 778 | ✅ | [Sistema] buscar 'cancelar uma venda' traz 'Vendas realizadas' entre os 3 primeiros (posição 1 de 21: Vendas realizadas, Pedidos de venda, PDV - frente de caixa) |
| 779 | ✅ | [Sistema] buscar 'imprimir boleto' traz 'Cobrança bancária (boletos)' entre os 3 primeiros (posição 1 de 8: Cobrança bancária (boletos), Configurações, Vendas realizadas) |
| 780 | ✅ | [Sistema] buscar 'fabricar' traz 'Ordens de produção' entre os 3 primeiros (posição 1 de 3: Ordens de produção, Fichas técnicas, Produtos) |
| 781 | ✅ | [Sistema] buscar só uma palavra comum ('de') ainda devolve resultados |
| 782 | ✅ | [Sistema] e-mail incompleto é recusado |
| 783 | ✅ | [Sistema] e-mail de outro usuário (mesmo com maiúsculas) é recusado |
| 784 | ✅ | [Sistema] e-mail válido é gravado sem espaços |
| 785 | ✅ | [Sistema] senha atual errada é recusada |
| 786 | ✅ | [Sistema] confirmação diferente é recusada |
| 787 | ✅ | [Sistema] senha alterada e campos limpos |
| 788 | ✅ | [Sistema] a troca de senha fica registrada no log de acesso (categoria ACESSO) |
| 789 | ✅ | [Sistema] busca sem acento ('configuracoes') acha Configurações |
| 790 | ✅ | [Sistema] busca com acento e maiúsculas acha Relatórios |
| 791 | ✅ | [Sistema] busca por 'boleto' acha a cobrança |
| 792 | ✅ | [Sistema] palavra inexistente não acha nada |
| 793 | ✅ | [Sistema] busca só com espaços lista todas as telas |
| 794 | ✅ | [Sistema] buscar 'senha' não dá erro |
| 795 | ✅ | [Sistema] buscar 'pix' não dá erro |
| 796 | ✅ | [Sistema] buscar 'nota fiscal' não dá erro |
| 797 | ✅ | [Sistema] buscar 'ç' não dá erro |
| 798 | ✅ | [Sistema] buscar 'ã' não dá erro |
| 799 | ✅ | [Sistema] buscar 'a e i' não dá erro |
| 800 | ✅ | [Sistema] buscar '()' não dá erro |
| 801 | ✅ | [Sistema] buscar '%' não dá erro |
| 802 | ✅ | [Sistema] buscar ''' não dá erro |
| 803 | ✅ | [Sistema] o localizador lista as telas, sem listar a si mesmo |
| 804 | ✅ | [Sistema] o localizador lista resultados e seleciona o primeiro |
| 805 | ✅ | [Sistema] Ctrl+K, 'estoque minimo' e Enter abre a tela de Estoque |
| 806 | ✅ | [Sistema] sem resultado o localizador diz 'Nenhuma tela encontrada' |
| 807 | ✅ | [Sistema] Enter sem resultado não fecha o localizador |
| 808 | ✅ | [Sistema] Esc no localizador não abre nenhuma tela |
| 809 | ✅ | [Sistema] senha errada não entra |
| 810 | ✅ | [Sistema] senha errada mostra mensagem |
| 811 | ✅ | [Sistema] usuário inativo não entra |
| 812 | ✅ | [Sistema] username com maiúsculas e espaços entra como usuário comum |
| 813 | ✅ | [Sistema] o log registra acessos recusados e a entrada: Entrou no sistema, Acesso recusado (usuário inativo), Acesso recusado |
| 814 | ✅ | [Sistema] o log não guarda senhas digitadas |
| 815 | ✅ | [Sistema] sem chave do Resend o sistema explica que o e-mail não está configurado |
| 816 | ✅ | [Sistema] usuário sem e-mail recebe orientação para falar com o administrador |
| 817 | ✅ | [Sistema] e-mail (com maiúsculas) recebe o código de 6 dígitos |
| 818 | ✅ | [Sistema] falha no envio mostra a mensagem do Resend |
| 819 | ✅ | [Sistema] código errado avisa quantas tentativas restam |
| 820 | ✅ | [Sistema] o código recebido redefine a senha mesmo depois de um reenvio que falhou |
| 821 | ✅ | [Sistema] a nova senha passa a valer |
| 822 | ✅ | [Sistema] menu do usuário comum não mostra 'Contas bancárias' |
| 823 | ✅ | [Sistema] menu do usuário comum não mostra 'Usuários' |
| 824 | ✅ | [Sistema] menu do usuário comum não mostra 'Log de atividades' |
| 825 | ✅ | [Sistema] menu do usuário comum mostra o PDV |
| 826 | ✅ | [Sistema] o localizador não oferece telas de administrador ao usuário comum |
| 827 | ✅ | [Sistema] usuário comum não consegue abrir o Log de atividades |
| 828 | ✅ | [Sistema] e recebe a explicação de que a tela é só de administradores |
| 829 | ✅ | [Sistema] Configurações do usuário comum tem só a aba Minha conta: Minha conta |
| 830 | ✅ | [Vendas] a prazo sem cliente é recusado pedindo o cliente |
| 831 | ✅ | [Vendas] a prazo com cliente dentro do limite grava a venda \| Para vender a prazo, identifique o cliente (F2 no PDV). |
| 832 | ✅ | [Vendas] a prazo em 2x gera 2 parcelas em aberto no nome do cliente |
| 833 | ✅ | [Vendas] as parcelas somam o total da venda |
| 834 | ✅ | [Vendas] usuário comum não vende acima do limite de crédito |
| 835 | ✅ | [Vendas] venda recusada por limite continua no PDV para outra forma de pagamento |
| 836 | ✅ | [Vendas] administrador é avisado do limite excedido; respondendo Não, a venda não sai |
| 837 | ✅ | [Vendas] administrador confirma e vende acima do limite de crédito \| Para vender a prazo, identifique o cliente (F2 no PDV). \| Limite de crédito do cliente excedido (disponível: R$ 30,23, venda: R$ 60,46). Escolha outra forma de pagamento ou peça a um administrador. |
| 838 | ✅ | [Vendas] 3*código + código somam 4 unidades na mesma linha |
| 839 | ✅ | [Vendas] F4 = 2 lança o próximo item com 2 unidades |
| 840 | ✅ | [Vendas] F6 remove o item selecionado |
| 841 | ✅ | [Vendas] quantidade absurda é recusada sem alterar a venda |
| 842 | ✅ | [Vendas] F10 e tecla 2 escolhem PIX (a venda foi gravada como PIX) \| Quantidade "99999999999999999999999999999999" inválida: use de 0,001 a 99.999 (ex.: 3*código). |
| 843 | ✅ | [Vendas] transação TEF aprovada fica CONFIRMADA com a venda |
| 844 | ✅ | [Vendas] PDV volta a ficar livre depois da venda |
| 845 | ✅ | [Vendas] F7 respondendo Não mantém a venda |
| 846 | ✅ | [Vendas] F7 respondendo Sim cancela a venda em andamento |
| 847 | ✅ | [Vendas] ao cancelar venda a prazo com parcela recebida, a confirmação avisa do valor já recebido |
| 848 | ✅ | [Vendas] respondendo Não, a venda continua finalizada |
| 849 | ✅ | [Vendas] comprovante de venda com frete mostra a linha do frete |
| 850 | ✅ | [Vendas] comprovante mostra o total com frete |
| 851 | ✅ | [Vendas] usuário comum: 30% de desconto é recusado |
| 852 | ✅ | [Vendas] a recusa explica o limite |
| 853 | ✅ | [Vendas] desconto recusado não altera o total |
| 854 | ✅ | [Vendas] usuário comum: desconto dentro do limite é aceito |
| 855 | ✅ | [Vendas] removendo item, o desconto que passou do limite é barrado antes do pagamento (F5 para ajustar) |
| 856 | ✅ | [Vendas] administrador pode dar 50% de desconto |
| 857 | ✅ | [Vendas] venda com 50% de desconto do administrador gravada com o total certo |
| 858 | ✅ | [Vendas] desconto do item maior que o item é recusado ao salvar |
| 859 | ✅ | [Vendas] corrigido o desconto, o orçamento é salvo \| O desconto do item "Arroz Tipo 1 5kg" (R$ 35,23) é maior que o valor dele (R$ 30,23). Corrija o desconto do item ou use o desconto geral. |
| 860 | ✅ | [Vendas] nenhum item de orçamento/pedido com total negativo |
| 861 | ✅ | [Vendas] orçamento com 50% de desconto é recusado para usuário comum |
| 862 | ✅ | [Vendas] orçamento com 5% de desconto é salvo \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. |
| 863 | ✅ | [Vendas] código lançado duas vezes soma na mesma linha do orçamento |
| 864 | ✅ | [Vendas] validade padrão do orçamento = hoje + validade_orcamento_dias |
| 865 | ✅ | [Vendas] orçamento não mexe no estoque |
| 866 | ✅ | [Vendas] orçamento aparece em aberto na lista |
| 867 | ✅ | [Vendas] aprovar gera o pedido de venda e marca o orçamento APROVADO |
| 868 | ✅ | [Vendas] aprovar de novo um orçamento já aprovado é recusado (não gera pedido duplicado) |
| 869 | ✅ | [Vendas] pedido gerado aparece em Pedidos de venda |
| 870 | ✅ | [Vendas] faturar gera a venda com o total do pedido \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. \| Este documento está APROVADO. A operação só vale para documentos em aberto. |
| 871 | ✅ | [Vendas] faturar baixa o estoque |
| 872 | ✅ | [Vendas] venda faturada aparece em Vendas realizadas |
| 873 | ✅ | [Vendas] comprovante mostra o nº da venda e o cliente |
| 874 | ✅ | [Vendas] administrador cancela a venda |
| 875 | ✅ | [Vendas] cancelamento devolve o estoque |
| 876 | ✅ | [Vendas] cancelamento cancela as contas da venda |
| 877 | ✅ | [Vendas] cancelamento reabre o pedido de origem |
| 878 | ✅ | [Vendas] cancelar de novo avisa que já está cancelada |
| 879 | ✅ | [Vendas] usuário comum não fatura pedido a prazo acima do limite de crédito do cliente |
| 880 | ✅ | [Vendas] orçamento vencido não aparece entre os em aberto |
| 881 | ✅ | [Vendas] filtro Vencidos mostra o orçamento como VENCIDO |
| 882 | ✅ | [Vendas] duplicar o orçamento vencido cria um novo em aberto com validade renovada |
| 883 | ✅ | [Vendas] aprovar orçamento vencido pede confirmação; respondendo Não, nada muda |
| 884 | ✅ | [Vendas] confirmando, o orçamento vencido é aprovado |
| 885 | ✅ | [Vendas] faturar no boleto sem cliente é recusado |
| 886 | ✅ | [Vendas] faturar no boleto com cliente gera a venda \| Para vender no boleto o pedido precisa ter um cliente. Abra o pedido e escolha o cliente. |
| 887 | ✅ | [Vendas] venda no boleto fica em aberto no contas a receber (não entra como recebida) |
| 888 | ✅ | [Vendas] produto sem preço de venda não é lançado no PDV e o motivo é explicado |
| 889 | ✅ | [Vendas] PERGUNTAR: 3ª unidade (estoque 2) pergunta; respondendo Não, não lança |
| 890 | ✅ | [Vendas] PERGUNTAR: respondendo Sim, lança |
| 891 | ✅ | [Vendas] BLOQUEAR: não lança e explica que está bloqueado |
| 892 | ✅ | [Vendas] PERMITIR: lança sem perguntar |
| 893 | ✅ | [Vendas] TEF negado não grava a venda |
| 894 | ✅ | [Vendas] TEF negado avisa o operador |
| 895 | ✅ | [Vendas] TEF negado mantém os itens no PDV |
| 896 | ✅ | [Vendas] transação negada registrada como NEGADA |
| 897 | ✅ | [Vendas] TEF cancelado pelo operador não grava a venda |
| 898 | ✅ | [Vendas] tecla 4 = crédito, aprovado no TEF, grava a venda \| Pagamento não aprovado: Transação negada pelo emissor (simulação). \| Pagamento não aprovado: Operação cancelada pelo operador. |
| 899 | ✅ | [Vendas] crédito parcelado em 3x chega ao TEF |
| 900 | ✅ | [Vendas] crédito no TEF gera a conta a receber já paga (a operadora recebe da adquirente) |
| 901 | ✅ | [Vendas] cancelar a venda estorna a transação TEF e informa o estorno |
| 902 | ✅ | [Vendas] administrador dá 100% de desconto |
| 903 | ✅ | [Vendas] venda de total zero é gravada |
| 904 | ✅ | [Vendas] venda de total zero não aciona o TEF (nenhuma transação de R$ 0,00) |
| 905 | ✅ | [Vendas] venda de total zero não gera conta (RN-VEN-09) |
