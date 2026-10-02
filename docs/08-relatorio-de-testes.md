# Relatório de testes

> Gerado automaticamente pela bateria `testes/ErpLoja.Testes` a cada execução. Não edite à mão.

| Item | Valor |
|---|---|
| Data da execução | 02/10/2026 19:33 |
| Versão do sistema | 1.4.0.0 (banco v14) |
| Ambiente | Microsoft Windows 10.0.19045 · .NET 10.0.12 |
| Verificações | 946 |
| Aprovadas | 946 |
| Reprovadas | 0 |
| Resultado | **APROVADO** |

## Verificações

| # | Resultado | Verificação |
|---:|:---:|---|
| 1 | ✅ | banco legado reconhecido como banco do sistema |
| 2 | ✅ | legado v1: registra a 1 e aplica as migrações 2 a 14 |
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
| 23 | ✅ | banco novo: 14 migrações aplicadas |
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
| 231 | ✅ | abrir outra tela substitui a atual (uma tela por vez, sem abas) |
| 232 | ✅ | tela no modelo padrão: opções em lista à direita |
| 233 | ✅ | abrir a tela que já está aberta não cria outra |
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
| 248 | ✅ | todos os campos de texto editáveis das telas têm formato de entrada (123 campos conferidos) |
| 249 | ✅ | nenhuma caixa de texto é criada sem formato no código-fonte |
| 250 | ✅ | toda tela aberta tem tópico de ajuda |
| 251 | ✅ | todos os diálogos têm ajuda (9 diálogos) |
| 252 | ✅ | todas as telas do sistema foram abertas no teste |
| 253 | ✅ | manual do usuário gerado em docs/12-manual-do-usuario.md a partir da ajuda |
| 254 | ✅ | dados de demonstração gerados em 2,8 s — Dados de demonstração gerados: 35 clientes, 8 fornecedores, 50 produtos, 48 compras, 77 ordens de produção, 1408 vendas, 7 orçamentos/pedidos, 3 fretes, 8 boletos, 5 notas fiscais. |
| 255 | ✅ | dados de demonstração íntegros (chaves estrangeiras) |
| 256 | ✅ | insumos não ficam negativos na demonstração |
| 257 | ✅ | CPF/CNPJ da demonstração são válidos |
| 258 | ✅ | modelo de dados (dicionário + diagrama ER) gerado em docs/06-modelo-de-dados.md |
| 259 | ✅ | [Cadastros] o próprio usuário aparece na lista |
| 260 | ✅ | [Cadastros] administrador não consegue se inativar |
| 261 | ✅ | [Cadastros] continua ativo |
| 262 | ✅ | [Cadastros] administrador não tira o próprio acesso |
| 263 | ✅ | [Cadastros] continua administrador |
| 264 | ✅ | [Cadastros] senha curta é recusada |
| 265 | ✅ | [Cadastros] username repetido (maiúsculas diferentes) é recusado \| Não foi possível salvar: já existe outro registro com este "Username". Informe outro valor. |
| 266 | ✅ | [Cadastros] usuário comum cadastrado |
| 267 | ✅ | [Cadastros] senha gravada como hash e conferível |
| 268 | ✅ | [Cadastros] editar com senha em branco mantém a senha |
| 269 | ✅ | [Cadastros] localizador de telas esconde as telas de administrador do usuário comum |
| 270 | ✅ | [Cadastros] o único administrador ativo não pode ser inativado, rebaixado nem excluído |
| 271 | ✅ | [Cadastros] inativar usuário comum é permitido |
| 272 | ✅ | [Cadastros] havendo outro administrador ativo, o primeiro pode ser rebaixado |
| 273 | ✅ | [Cadastros] seleciona o próprio usuário |
| 274 | ✅ | [Cadastros] não exclui o próprio usuário |
| 275 | ✅ | [Cadastros] CPF com dígito errado é recusado |
| 276 | ✅ | [Cadastros] sem papel marcado é recusado |
| 277 | ✅ | [Cadastros] CPF/CNPJ de pessoa já cadastrada é recusado e a mensagem diz quem é \| Já existe um cadastro com este CPF/CNPJ: "Diego Lima Rocha" (registro nº 1).
Em vez de cadastrar de novo, edite esse registro e marque os papéis que faltam (cliente, fornecedor...). |
| 278 | ✅ | [Cadastros] nenhuma pessoa duplicada gravada |
| 279 | ✅ | [Cadastros] cadastro corrigido é salvo sem erro |
| 280 | ✅ | [Cadastros] pessoa nova gravada uma única vez |
| 281 | ✅ | [Cadastros] pessoa existente aparece na lista |
| 282 | ✅ | [Cadastros] salvar a própria pessoa não acusa CPF duplicado |
| 283 | ✅ | [Cadastros] alteração gravada |
| 284 | ✅ | [Cadastros] cliente com vendas aparece na lista |
| 285 | ✅ | [Cadastros] excluir pessoa com histórico oferece inativar |
| 286 | ✅ | [Cadastros] pessoa inativada e vendas preservadas |
| 287 | ✅ | [Cadastros] filtro Inativos mostra a pessoa inativada |
| 288 | ✅ | [Cadastros] categoria em uso aparece na lista |
| 289 | ✅ | [Cadastros] excluir categoria em uso explica o que fazer e não exclui |
| 290 | ✅ | [Cadastros] categoria repetida (maiúsculas) é recusada \| Não foi possível salvar: já existe outro registro com este "Nome". Informe outro valor. |
| 291 | ✅ | [Cadastros] categoria nova salva |
| 292 | ✅ | [Cadastros] categoria sem produtos é excluída |
| 293 | ✅ | [Cadastros] produto com movimentação aparece na lista |
| 294 | ✅ | [Cadastros] editar o preço não arredonda o custo médio (4 casas) |
| 295 | ✅ | [Cadastros] custo médio de produto com movimentação não é alterado à mão |
| 296 | ✅ | [Cadastros] custo inicial de produto sem movimentação pode ser informado e corrigido |
| 297 | ✅ | [Cadastros] EAN com dígito verificador errado é recusado |
| 298 | ✅ | [Cadastros] código de barras de outro produto é recusado \| O código de barras 7891950522666 já pertence ao produto "Arroz Tipo 1 5kg" (código D001). No PDV a leitura ficaria ambígua: confira o código lido. |
| 299 | ✅ | [Cadastros] NCM incompleto é recusado |
| 300 | ✅ | [Cadastros] CST com letra (O no lugar de zero) é recusado |
| 301 | ✅ | [Cadastros] redução de IBS acima de 100% é recusada |
| 302 | ✅ | [Cadastros] código repetido (minúsculas) é recusado dizendo qual campo \| Não foi possível salvar: já existe outro registro com este "Código". Informe outro valor. |
| 303 | ✅ | [Cadastros] produto corrigido é salvo |
| 304 | ✅ | [Cadastros] produto gravado com o EAN correto |
| 305 | ✅ | [Cadastros] produto existente aparece na lista |
| 306 | ✅ | [Cadastros] editar produto existente não acusa o próprio código de barras |
| 307 | ✅ | [Cadastros] pesquisa pelo código encontra só o produto novo |
| 308 | ✅ | [PDV] lançar 2*código mostra o total da venda |
| 309 | ✅ | [PDV] F10 + dinheiro grava a venda |
| 310 | ✅ | [PDV] total gravado = 2 × preço |
| 311 | ✅ | [PDV] nenhuma mensagem de erro durante a venda |
| 312 | ✅ | [Cobranca] validador aponta quantidade em branco no trailer |
| 313 | ✅ | [Cobranca] validador aponta valor com letras no segmento P |
| 314 | ✅ | [Cobranca] modelo com tamanho absurdo é recusado com mensagem |
| 315 | ✅ | [Cobranca] conta de demonstração na lista |
| 316 | ✅ | [Cobranca] modelo sem o nosso número é recusado \| "Modelo do campo livre (25 posições)" O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro. |
| 317 | ✅ | [Cobranca] espécie com letras (DM) é recusada: a remessa exige código numérico \| "Espécie do título (02 = DM)" aceita só números (2 dígitos, ex.: 02). |
| 318 | ✅ | [Cobranca] nosso número maior que o espaço do modelo é recusado \| O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras. |
| 319 | ✅ | [Cobranca] voltar o último nosso número abaixo dos já emitidos é recusado \| Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números. |
| 320 | ✅ | [Cobranca] dias de protesto acima de 99 é recusado \| Dias para protesto: de 0 (não protestar) a 99. |
| 321 | ✅ | [Cobranca] nada inválido foi gravado |
| 322 | ✅ | [Cobranca] conta válida salva normalmente (ambiente PRODUCAO): "Modelo do campo livre (25 posições)" O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro.: "Espécie do título (02 = DM)" aceita só números (2 dígitos, ex.: 02).: O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras.: Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números.: Dias para protesto: de 0 (não protestar) a 99. |
| 323 | ✅ | [Cobranca] boletos de teste recusados em conta de produção |
| 324 | ✅ | [Cobranca] modelo inválido: boletos de teste explicam o problema |
| 325 | ✅ | [Cobranca] nenhuma conta de teste fica órfã sem boleto |
| 326 | ✅ | [Cobranca] boleto registrado pelo banco |
| 327 | ✅ | [Cobranca] conta A na lista |
| 328 | ✅ | [Cobranca] a confirmação do cancelamento avisa que a conta tem boleto |
| 329 | ✅ | [Cobranca] conta A cancelada |
| 330 | ✅ | [Cobranca] boleto ainda não enviado da conta cancelada é cancelado junto |
| 331 | ✅ | [Cobranca] conta B na lista |
| 332 | ✅ | [Cobranca] a janela de baixa avisa que a conta tem boleto em aberto |
| 333 | ✅ | [Cobranca] conta B recebida no caixa |
| 334 | ✅ | [Cobranca] boleto registrado da conta B fica com baixa pedida |
| 335 | ✅ | [Cobranca] o usuário é avisado de que a baixa vai na próxima remessa |
| 336 | ✅ | [Cobranca] retorno que liquida conta já recebida no caixa avisa possível duplicidade |
| 337 | ✅ | [Cobranca] a baixa do caixa não é sobrescrita pelo retorno |
| 338 | ✅ | [Cobranca] rodapé mostra o total vencido a receber (R$ 2.647,61) |
| 339 | ✅ | [Cobranca] mudar as datas do período já atualiza a lista (sem clicar em Pesquisar) |
| 340 | ✅ | [Cobranca] boleto com vencimento passado é recusado com explicação |
| 341 | ✅ | [Cobranca] janela avisa das contas vencidas e pede o novo vencimento |
| 342 | ✅ | [Cobranca] boleto da conta vencida sai com o novo vencimento |
| 343 | ✅ | [Cobranca] fator de vencimento do código de barras usa o novo vencimento |
| 344 | ✅ | [Cobranca] nenhum erro ao emitir |
| 345 | ✅ | [Cobranca] a conta nova aparece para emitir boleto |
| 346 | ✅ | [Cobranca] boleto emitido para a conta escolhida (só ela) |
| 347 | ✅ | [Cobranca] código de barras com 44 dígitos |
| 348 | ✅ | [Cobranca] código começa com o banco e a moeda 9 |
| 349 | ✅ | [Cobranca] fator de vencimento 1607 (contagem reiniciada em 22/02/2025) |
| 350 | ✅ | [Cobranca] valor em centavos no código de barras |
| 351 | ✅ | [Cobranca] DV geral do código de barras (módulo 11) |
| 352 | ✅ | [Cobranca] linha digitável com 47 dígitos |
| 353 | ✅ | [Cobranca] DVs dos três campos da linha digitável (módulo 10) |
| 354 | ✅ | [Cobranca] a linha digitável remonta exatamente o código de barras |
| 355 | ✅ | [Cobranca] nosso número com 11 dígitos dentro do campo livre |
| 356 | ✅ | [Cobranca] boleto duplicado para a mesma conta é recusado |
| 357 | ✅ | [Cobranca] boleto de conta sem cliente é recusado (RN-COB-01) |
| 358 | ✅ | [Cobranca] boleto aparece na lista de ativos |
| 359 | ✅ | [Cobranca] ficha com linha digitável, valor e aviso de homologação |
| 360 | ✅ | [Cobranca] Visualizar / imprimir abre o boleto |
| 361 | ✅ | [Cobranca] nenhum erro na emissão pela tela |
| 362 | ✅ | [Cobranca] 3 boletos de teste emitidos |
| 363 | ✅ | [Cobranca] remessa de teste com os 3 boletos |
| 364 | ✅ | [Cobranca] simulação pela tela informa os 3 liquidados \| Retorno SIMULADO_REMESSA_2.ret: 6 registro(s), 3 entrada(s) confirmada(s), 0 rejeitada(s), 3 liquidado(s) (R$ 13,50), 0 não encontrado(s).

As contas a receber liquidadas foram baixadas no financeiro. |
| 365 | ✅ | [Cobranca] contas de teste baixadas pelo retorno simulado |
| 366 | ✅ | [Cobranca] registro da homologação mostra o retorno processado |
| 367 | ✅ | [Cobranca] remessa de produção recusa boletos de teste esquecidos |
| 368 | ✅ | [Cobranca] boletos de teste continuam só emitidos (nada foi enviado) |
| 369 | ✅ | [Cobranca] em produção a simulação de retorno é recusada (não baixa contas sem dinheiro) |
| 370 | ✅ | [Cobranca] conta liquidada aparece em Pagas |
| 371 | ✅ | [Cobranca] estorno de conta liquidada pelo banco pede confirmação com aviso |
| 372 | ✅ | [Cobranca] conta estornada volta para aberta |
| 373 | ✅ | [Cobranca] conta estornada pode receber boleto novo (o pago não conta como ativo) |
| 374 | ✅ | [Cobranca] boleto registrado na lista |
| 375 | ✅ | [Cobranca] boleto registrado: cancelar pede a baixa |
| 376 | ✅ | [Cobranca] remessa leva o pedido de baixa (movimento 02) |
| 377 | ✅ | [Cobranca] retorno do pedido de baixa deixa o boleto BAIXADO |
| 378 | ✅ | [Cobranca] baixa do boleto não mexe na conta a receber |
| 379 | ✅ | [Cobranca] pedido de baixa atendido |
| 380 | ✅ | [Cobranca] remessa de homologação com prefixo TESTE_ |
| 381 | ✅ | [Cobranca] remessa com o boleto novo e linhas de 240 posições |
| 382 | ✅ | [Cobranca] remessa passa no validador CNAB 240 |
| 383 | ✅ | [Cobranca] boleto fica ENVIADO após a remessa |
| 384 | ✅ | [Cobranca] remessa vazia é recusada com explicação |
| 385 | ✅ | [Cobranca] retorno confirma e liquida o boleto |
| 386 | ✅ | [Cobranca] liquidação baixa a conta a receber |
| 387 | ✅ | [Cobranca] boleto fica PAGO |
| 388 | ✅ | [Cobranca] o mesmo retorno importado de novo é recusado |
| 389 | ✅ | [Cobranca] retorno repetido não é registrado de novo |
| 390 | ✅ | [Cobranca] liquidação repetida em outro retorno não é contada de novo |
| 391 | ✅ | [Cobranca] retorno de outro banco é recusado dizendo os dois bancos |
| 392 | ✅ | [Cobranca] validador aponta a linha com tamanho errado |
| 393 | ✅ | [Cobranca] venda a prazo em 2 parcelas |
| 394 | ✅ | [Cobranca] boleto não enviado da venda cancelada é cancelado |
| 395 | ✅ | [Cobranca] boleto registrado da venda cancelada fica com pedido de baixa para a próxima remessa |
| 396 | ✅ | [Compras] recebimento deu entrada de 5 |
| 397 | ✅ | [Compras] confirmação avisa que o produto ficará com estoque negativo (2 − 5 = −3) |
| 398 | ✅ | [Compras] confirmação avisa da parcela já paga |
| 399 | ✅ | [Compras] respondendo Não, a compra continua |
| 400 | ✅ | [Compras] respondendo Sim, a compra é cancelada |
| 401 | ✅ | [Compras] estoque fica negativo (−3) porque 3 já foram vendidos (RN-EST-04) |
| 402 | ✅ | [Compras] contas da compra canceladas |
| 403 | ✅ | [Compras] pedido de compra volta para ABERTO |
| 404 | ✅ | [Compras] movimentação SAIDA de 5 com saldo −3 |
| 405 | ✅ | [Compras] cancelar de novo avisa que já está cancelada |
| 406 | ✅ | [Compras] salvar avisa que o desconto do item é maior que o valor do item |
| 407 | ✅ | [Compras] pedido com desconto inválido não é gravado |
| 408 | ✅ | [Compras] Pedidos.Salvar recusa item com desconto maior que o valor |
| 409 | ✅ | [Compras] pedido salvo com total 22,00 |
| 410 | ✅ | [Compras] pedido aparece em aberto na lista |
| 411 | ✅ | [Compras] edição grava quantidade 7 e remove o 2º item |
| 412 | ✅ | [Compras] total recalculado 7 × 5,00 = 35,00 |
| 413 | ✅ | [Compras] pedido cancelado |
| 414 | ✅ | [Compras] pedido cancelado não pode ser recebido |
| 415 | ✅ | [Compras] pedido cancelado abre só para consulta |
| 416 | ✅ | [Compras] pergunta se o item sem custo é bonificação |
| 417 | ✅ | [Compras] respondendo Não, nada é recebido |
| 418 | ✅ | [Compras] custo médio e estoque intactos |
| 419 | ✅ | [Compras] pedido fica salvo em aberto para corrigir o custo |
| 420 | ✅ | [Compras] pedido recebido com a NF 4567 |
| 421 | ✅ | [Compras] fornecedor localizado pelo CNPJ com pontuação |
| 422 | ✅ | [Compras] "Lançar compra" vem desmarcado porque a NF já entrou pelo pedido |
| 423 | ✅ | [Compras] a conferência explica o motivo |
| 424 | ✅ | [Compras] nota registrada |
| 425 | ✅ | [Compras] nenhuma compra nova (sem entrada em dobro) |
| 426 | ✅ | [Compras] estoque não dobrou |
| 427 | ✅ | [Compras] receber com NF repetida pede confirmação |
| 428 | ✅ | [Compras] respondendo Não, nada é lançado |
| 429 | ✅ | [Compras] o pedido continua em aberto para receber depois |
| 430 | ✅ | [Compras] o usuário é avisado de que o pedido ficou salvo em aberto |
| 431 | ✅ | [Compras] pedido recebido com total R$ 31,00 (3 × 10 + frete 1) |
| 432 | ✅ | [Compras] compra gravada com o total do pedido (gravou 31) |
| 433 | ✅ | [Compras] 2 parcelas a pagar somando R$ 31,00 (soma 31,0) |
| 434 | ✅ | [Compras] nº da nota fiscal gravado na compra |
| 435 | ✅ | [Compras] estoque do item 1 aumentou 1 |
| 436 | ✅ | [Compras] custo médio do item 1 com frete rateado (23,113 ≈ 23,1130) |
| 437 | ✅ | [Compras] estoque do item 2 aumentou 1 |
| 438 | ✅ | [Compras] custo médio do item 2 com frete rateado (6,4205 ≈ 6,4205) |
| 439 | ✅ | [Compras] estoque do item 3 aumentou 1 |
| 440 | ✅ | [Compras] custo médio do item 3 com frete rateado (3,2055 ≈ 3,2055) |
| 441 | ✅ | [Compras] nenhum erro |
| 442 | ✅ | [Compras] lixo.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 443 | ✅ | [Compras] vazio.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 444 | ✅ | [Compras] outro.xml: mensagem clara em português (O arquivo não é um XML de NF-e (infNFe não encontrado). Confira se escolheu o XML da nota, e não outro arquivo.) |
| 445 | ✅ | [Compras] truncado.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 446 | ✅ | [Compras] não sugere produto inativo pelo código de barras |
| 447 | ✅ | [Compras] não sugere produto inativo pelo vínculo memorizado |
| 448 | ✅ | [Compras] conferência abre sem erro de grade |
| 449 | ✅ | [Compras] importação concluída cadastrando produtos novos |
| 450 | ✅ | [Compras] vínculo com o produto inativo é substituído pelo produto novo |
| 451 | ✅ | [Compras] 1ª importação concluída |
| 452 | ✅ | [Compras] com fator 1 entram só 2 unidades |
| 453 | ✅ | [Compras] item sem vínculo vira produto novo |
| 454 | ✅ | [Compras] duplicatas (2 × 218) lançadas a pagar |
| 455 | ✅ | [Compras] compra importada aparece em Compras realizadas |
| 456 | ✅ | [Compras] compra cancelada |
| 457 | ✅ | [Compras] estoque volta ao saldo anterior |
| 458 | ✅ | [Compras] contas a pagar da compra canceladas |
| 459 | ✅ | [Compras] nota cuja compra foi cancelada pode ser importada de novo |
| 460 | ✅ | [Compras] vínculos da 1ª importação foram memorizados |
| 461 | ✅ | [Compras] 2ª importação concluída |
| 462 | ✅ | [Compras] com fator 10 entram 20 unidades |
| 463 | ✅ | [Compras] reimportação não duplica o produto novo |
| 464 | ✅ | [Compras] só uma nota ativa com a chave |
| 465 | ✅ | [Compras] nota aponta para a nova compra |
| 466 | ✅ | [Compras] depois de reimportada, a nota volta a constar como importada |
| 467 | ✅ | [Estoque] editor abre com os 2 produtos abaixo do mínimo (2) |
| 468 | ✅ | [Estoque] sugestão 10 − 2 = 8 pelo custo médio 4,50 |
| 469 | ✅ | [Estoque] saldo negativo: sugestão 3 − (−1) = 4 |
| 470 | ✅ | [Estoque] pedido de compra gravado |
| 471 | ✅ | [Estoque] pedido de compra ABERTO do fornecedor escolhido |
| 472 | ✅ | [Estoque] total do pedido = 8 × 4,50 + 4 × 7,00 |
| 473 | ✅ | [Estoque] usuário é informado do número do pedido |
| 474 | ✅ | [Estoque] gerar pedido não mexe no estoque (RN-PED-05) |
| 475 | ✅ | [Estoque] sem produtos abaixo do mínimo, avisa e não abre pedido |
| 476 | ✅ | [Estoque] filtro pelo nome mostra o produto |
| 477 | ✅ | [Estoque] resumo acompanha a pesquisa (1 produto(s) · Valor do estoque (custo): R$ 694,27; 1 na grade) |
| 478 | ✅ | [Estoque] produto selecionado na grade |
| 479 | ✅ | [Estoque] venda baixou 2 (saldo real 28) |
| 480 | ✅ | [Estoque] definir saldo grava exatamente o contado (saldo 25) |
| 481 | ✅ | [Estoque] movimentação AJUSTE de 3 com saldo 25 |
| 482 | ✅ | [Estoque] motivo registrado na origem |
| 483 | ✅ | [Estoque] histórico do produto mostra o ajuste logo após aplicar |
| 484 | ✅ | [Estoque] saldo igual ao atual não gera movimentação |
| 485 | ✅ | [Estoque] avisa que nada foi alterado |
| 486 | ✅ | [Estoque] ajuste sem motivo é recusado |
| 487 | ✅ | [Estoque] nada mudou sem motivo |
| 488 | ✅ | [Estoque] definir saldo 0 zera o estoque |
| 489 | ✅ | [Estoque] pergunta antes de deixar o saldo negativo |
| 490 | ✅ | [Estoque] respondendo Não, o saldo não muda |
| 491 | ✅ | [Estoque] saída de 10 deixa saldo 2 |
| 492 | ✅ | [Estoque] somente baixo lista o produto: BAIXO, sugestão de compra 3 |
| 493 | ✅ | [Estoque] somente baixo não lista produtos OK |
| 494 | ✅ | [Financeiro] filtro Vencidas só mostra contas vencidas |
| 495 | ✅ | [Financeiro] conta vencida há 5 dias aparece com 5 dias de atraso |
| 496 | ✅ | [Financeiro] seleciona a conta vencida |
| 497 | ✅ | [Financeiro] baixa com data futura é recusada com explicação |
| 498 | ✅ | [Financeiro] baixa grava data, valor com juros e forma |
| 499 | ✅ | [Financeiro] baixar duas vezes a mesma conta é recusado |
| 500 | ✅ | [Financeiro] conta aparece em Pagas |
| 501 | ✅ | [Financeiro] estorno volta a conta para aberta e limpa o pagamento (data, valor e forma) |
| 502 | ✅ | [Financeiro] conta estornada volta a aparecer em aberto |
| 503 | ✅ | [Financeiro] cancelar conta aberta |
| 504 | ✅ | [Financeiro] conta cancelada não pode ser baixada |
| 505 | ✅ | [Financeiro] conta continua cancelada |
| 506 | ✅ | [Financeiro] conta na lista |
| 507 | ✅ | [Financeiro] valor 10× maior pede confirmação; "Não" mantém a janela aberta |
| 508 | ✅ | [Financeiro] baixa gravada com o valor corrigido |
| 509 | ✅ | [Financeiro] sem descrição a nova conta não é salva e o motivo é mostrado |
| 510 | ✅ | [Financeiro] 3 parcelas geradas (3) |
| 511 | ✅ | [Financeiro] valores 33,33 / 33,33 / 33,34 (centavos na última) |
| 512 | ✅ | [Financeiro] vencimentos mensais a partir do 1º |
| 513 | ✅ | [Financeiro] descrição "… parcela k/n" |
| 514 | ✅ | [Financeiro] parcelas a receber, em aberto e com o cliente escolhido |
| 515 | ✅ | [Financeiro] a lista é atualizada com as parcelas novas |
| 516 | ✅ | [Fiscal] trocar a UF do destinatário para RJ muda o CFOP para 6102 |
| 517 | ✅ | [Fiscal] CFOP de dentro do estado para cliente de outra UF é barrado na emissão |
| 518 | ✅ | [Fiscal] desconto maior que o valor do item é barrado na emissão |
| 519 | ✅ | [Fiscal] nota emitida para a venda |
| 520 | ✅ | [Fiscal] numeração definitiva e chave de 44 dígitos com DV |
| 521 | ✅ | [Fiscal] itens gravados com CFOP 6102 |
| 522 | ✅ | [Fiscal] venda com nota abre a nota existente sem permitir alterar |
| 523 | ✅ | [Fiscal] camada de dados recusa segunda nota para a mesma venda (RN-FIS-01) |
| 524 | ✅ | [Fiscal] nota emitida aparece na lista |
| 525 | ✅ | [Fiscal] usuário comum não cancela nota emitida |
| 526 | ✅ | [Fiscal] nota selecionada de novo |
| 527 | ✅ | [Fiscal] administrador cancela a nota |
| 528 | ✅ | [Fiscal] venda volta a aceitar nota depois do cancelamento |
| 529 | ✅ | [Fiscal] cancelar duas vezes é recusado na camada de dados |
| 530 | ✅ | [Fiscal] respondendo "Sim" o editor fecha descartando a nota |
| 531 | ✅ | [Fiscal] produto inativo não aparece na busca (F3) |
| 532 | ✅ | [Fiscal] F3 + nome + Enter adiciona o produto pelo custo com CFOP 1102 |
| 533 | ✅ | [Fiscal] fechar o editor com item não salvo pergunta antes e "Não" mantém a nota aberta |
| 534 | ✅ | [Fiscal] trocar o modelo usa a série configurada do modelo (65 → 7, 55 → 1) |
| 535 | ✅ | [Fiscal] Enter no código adiciona o item |
| 536 | ✅ | [Fiscal] rascunho gravado como NFC-e série 7 |
| 537 | ✅ | [Fiscal] CFOP de saída numa nota de entrada é acusado |
| 538 | ✅ | [Fiscal] NFC-e numa nota de entrada é acusada |
| 539 | ✅ | [Fiscal] CNPJ do remetente com dígito errado é acusado |
| 540 | ✅ | [Fiscal] desconto adicional maior que os produtos é acusado |
| 541 | ✅ | [Fiscal] CFOP interestadual com fornecedor do mesmo estado é acusado |
| 542 | ✅ | [Fiscal] nota corrigida não tem pendências:  |
| 543 | ✅ | [Fiscal] rascunho com pendências pode ser salvo |
| 544 | ✅ | [Fiscal] rascunho aparece no filtro Em digitação |
| 545 | ✅ | [Fiscal] emitir rascunho com CFOP errado mostra o motivo e não emite |
| 546 | ✅ | [Fiscal] rascunho selecionado |
| 547 | ✅ | [Fiscal] nota de entrada corrigida é emitida |
| 548 | ✅ | [Fiscal] nota de venda cancelada não é emitida: A venda nº 1 foi cancelada: não emita nota para ela (exclua este rascunho). |
| 549 | ✅ | [Fiscal] rascunho aparece em Em digitação |
| 550 | ✅ | [Fiscal] rascunho e itens excluídos |
| 551 | ✅ | [Gestão] faturamento dos últimos 30 dias confere com o banco (R$ 17.889,63) |
| 552 | ✅ | [Gestão] quantidade de vendas dos últimos 30 dias confere |
| 553 | ✅ | [Gestão] ticket médio = faturamento ÷ vendas |
| 554 | ✅ | [Gestão] período 'Hoje' abre sem erro |
| 555 | ✅ | [Gestão] período 'Últimos 7 dias' abre sem erro |
| 556 | ✅ | [Gestão] período 'Últimos 30 dias' abre sem erro |
| 557 | ✅ | [Gestão] período 'Este mês' abre sem erro |
| 558 | ✅ | [Gestão] período 'Mês anterior' abre sem erro |
| 559 | ✅ | [Gestão] período 'Últimos 90 dias' abre sem erro |
| 560 | ✅ | [Gestão] período 'Este ano' abre sem erro |
| 561 | ✅ | [Gestão] período 'Últimos 12 meses' abre sem erro |
| 562 | ✅ | [Gestão] relatório 'Vendas por dia' em 6 meses gera sem erro |
| 563 | ✅ | [Gestão] relatório 'Produtos mais vendidos' em 6 meses gera sem erro |
| 564 | ✅ | [Gestão] relatório 'Vendas por cliente' em 6 meses gera sem erro |
| 565 | ✅ | [Gestão] relatório 'Vendas por usuário (vendedor)' em 6 meses gera sem erro |
| 566 | ✅ | [Gestão] relatório 'Lucro bruto (resumo do período)' em 6 meses gera sem erro |
| 567 | ✅ | [Gestão] relatório 'Fluxo de caixa (realizado)' em 6 meses gera sem erro |
| 568 | ✅ | [Gestão] relatório 'Fluxo de caixa (previsto - contas em aberto)' em 6 meses gera sem erro |
| 569 | ✅ | [Gestão] relatório 'Produtos sem venda no período' em 6 meses gera sem erro |
| 570 | ✅ | [Gestão] relatório 'Compras por fornecedor' em 6 meses gera sem erro |
| 571 | ✅ | [Gestão] 'Produtos sem venda' não lista insumos de produção (0 insumo(s) na lista) |
| 572 | ✅ | [Gestão] 'Vendas por dia' soma 101.721,92 = banco 101.721,92 |
| 573 | ✅ | [Gestão] trocar o período já atualiza o relatório, sem precisar clicar em Gerar |
| 574 | ✅ | [Gestão] 'Vendas por dia' num período sem movimento: 0 linha(s), sem erro |
| 575 | ✅ | [Gestão] 'Produtos mais vendidos' num período sem movimento: 0 linha(s), sem erro |
| 576 | ✅ | [Gestão] 'Vendas por cliente' num período sem movimento: 0 linha(s), sem erro |
| 577 | ✅ | [Gestão] 'Vendas por usuário (vendedor)' num período sem movimento: 0 linha(s), sem erro |
| 578 | ✅ | [Gestão] 'Lucro bruto (resumo do período)' num período sem movimento: 1 linha(s), sem erro |
| 579 | ✅ | [Gestão] 'Fluxo de caixa (realizado)' num período sem movimento: 0 linha(s), sem erro |
| 580 | ✅ | [Gestão] 'Fluxo de caixa (previsto - contas em aberto)' num período sem movimento: 0 linha(s), sem erro |
| 581 | ✅ | [Gestão] 'Produtos sem venda no período' num período sem movimento: 40 linha(s), sem erro |
| 582 | ✅ | [Gestão] 'Compras por fornecedor' num período sem movimento: 0 linha(s), sem erro |
| 583 | ✅ | [Gestão] datas invertidas avisam que a data final é anterior à inicial |
| 584 | ✅ | [Gestão] sem dados, período 'Hoje' abre sem erro |
| 585 | ✅ | [Gestão] sem dados, 'Hoje' mostra faturamento zero e margem '-' |
| 586 | ✅ | [Gestão] sem dados, período 'Últimos 7 dias' abre sem erro |
| 587 | ✅ | [Gestão] sem dados, 'Últimos 7 dias' mostra faturamento zero e margem '-' |
| 588 | ✅ | [Gestão] sem dados, período 'Últimos 30 dias' abre sem erro |
| 589 | ✅ | [Gestão] sem dados, 'Últimos 30 dias' mostra faturamento zero e margem '-' |
| 590 | ✅ | [Gestão] sem dados, período 'Este mês' abre sem erro |
| 591 | ✅ | [Gestão] sem dados, 'Este mês' mostra faturamento zero e margem '-' |
| 592 | ✅ | [Gestão] sem dados, período 'Mês anterior' abre sem erro |
| 593 | ✅ | [Gestão] sem dados, 'Mês anterior' mostra faturamento zero e margem '-' |
| 594 | ✅ | [Gestão] sem dados, período 'Últimos 90 dias' abre sem erro |
| 595 | ✅ | [Gestão] sem dados, 'Últimos 90 dias' mostra faturamento zero e margem '-' |
| 596 | ✅ | [Gestão] sem dados, período 'Este ano' abre sem erro |
| 597 | ✅ | [Gestão] sem dados, 'Este ano' mostra faturamento zero e margem '-' |
| 598 | ✅ | [Gestão] sem dados, período 'Últimos 12 meses' abre sem erro |
| 599 | ✅ | [Gestão] sem dados, 'Últimos 12 meses' mostra faturamento zero e margem '-' |
| 600 | ✅ | [Gestão] sem dados, relatório 'Vendas por dia' gera sem erro |
| 601 | ✅ | [Gestão] sem dados, relatório 'Produtos mais vendidos' gera sem erro |
| 602 | ✅ | [Gestão] sem dados, relatório 'Vendas por cliente' gera sem erro |
| 603 | ✅ | [Gestão] sem dados, relatório 'Vendas por usuário (vendedor)' gera sem erro |
| 604 | ✅ | [Gestão] sem dados, relatório 'Lucro bruto (resumo do período)' gera sem erro |
| 605 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (realizado)' gera sem erro |
| 606 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (previsto - contas em aberto)' gera sem erro |
| 607 | ✅ | [Gestão] sem dados, relatório 'Produtos sem venda no período' gera sem erro |
| 608 | ✅ | [Gestão] sem dados, relatório 'Compras por fornecedor' gera sem erro |
| 609 | ✅ | [Interface] primeiro acesso com a senha padrão exige senha nova (recusa a padrão, senhas diferentes e curtas) |
| 610 | ✅ | [Interface] senha nova gravada (como hash) e a padrão deixa de valer |
| 611 | ✅ | [Interface] com a senha nova entra direto |
| 612 | ✅ | [Interface] o menu lateral destaca a tela em primeiro plano |
| 613 | ✅ | [Interface] o destaque do menu acompanha a troca de tela |
| 614 | ✅ | [Interface] a tela inicial mostra "Continuar de onde parou" com as telas usadas por último: Contas a pagar e receber, Estoque |
| 615 | ✅ | [Interface] o localizador (Ctrl+K) vazio mostra as telas recentes primeiro |
| 616 | ✅ | [Logística] fretes: 'Abrir / editar' sem seleção avisa para selecionar |
| 617 | ✅ | [Logística] fretes: 'Imprimir' sem seleção avisa para selecionar |
| 618 | ✅ | [Logística] fretes: 'Cancelar CIOT' sem seleção avisa para selecionar |
| 619 | ✅ | [Logística] OP: Imprimir sem seleção avisa para selecionar |
| 620 | ✅ | [Logística] placa com hífen é recusada com orientação |
| 621 | ✅ | [Logística] RENAVAM com letras é recusado |
| 622 | ✅ | [Logística] RNTRC curto é recusado |
| 623 | ✅ | [Logística] veículo válido é salvo |
| 624 | ✅ | [Logística] a mesma placa em minúsculas não é cadastrada de novo |
| 625 | ✅ | [Logística] frete com CIOT declarado só cancela depois do CIOT |
| 626 | ✅ | [Logística] cancelar CIOT exige motivo com 15 caracteres; com motivo válido cancela |
| 627 | ✅ | [Logística] viagem com CIOT cancelado não é liberada |
| 628 | ✅ | [Logística] cancelar o frete cancela as contas abertas dele |
| 629 | ✅ | [Logística] frete cancelado não gera contas a pagar |
| 630 | ✅ | [Logística] frete cancelado não declara CIOT |
| 631 | ✅ | [Logística] camada de dados exige motivo de 15 caracteres |
| 632 | ✅ | [Logística] transportadora (CNPJ) não exige CIOT |
| 633 | ✅ | [Logística] frete sem valor avisa que falta o valor |
| 634 | ✅ | [Logística] frete sem valor continua podendo gerar contas depois |
| 635 | ✅ | [Logística] frete de transportadora sai sem CIOT |
| 636 | ✅ | [Logística] editar o frete mantém o veículo mesmo depois de desativado |
| 637 | ✅ | [Logística] carga acima da capacidade pergunta; Não mantém o diálogo aberto |
| 638 | ✅ | [Logística] frete de frota própria salvo com o peso corrigido e sem CIOT |
| 639 | ✅ | [Logística] o editor avisa que o CIOT é obrigatório para autônomo: OBRIGATÓRIO (transportador autônomo): declare após salvar |
| 640 | ✅ | [Logística] frete salvo com CIOT pendente |
| 641 | ✅ | [Logística] sem CIOT declarado a viagem não é liberada (em trânsito) |
| 642 | ✅ | [Logística] sem CIOT declarado o frete não vai direto para entregue |
| 643 | ✅ | [Logística] CIOT declarado pelo simulador |
| 644 | ✅ | [Logística] contas a pagar: adiantamento 800 + saldo 1.300 (1.200 + 100 de pedágio) |
| 645 | ✅ | [Logística] as contas do frete são geradas uma única vez |
| 646 | ✅ | [Logística] depois de gerar as contas, valores do frete ficam travados no editor |
| 647 | ✅ | [Logística] com CIOT declarado, a rota (origem/destino declarados à ANTT) fica travada |
| 648 | ✅ | [Logística] com CIOT declarado a viagem sai (em trânsito) |
| 649 | ✅ | [Logística] frete entregue grava a data da entrega |
| 650 | ✅ | [Logística] CIOT encerrado após a entrega |
| 651 | ✅ | [Logística] frete entregue não é cancelado |
| 652 | ✅ | [Logística] Pamcard sem credenciais: orienta a configurar e nada muda |
| 653 | ✅ | [Logística] resposta 'sucesso' sem número de CIOT não marca o frete como declarado: PAMCARD não devolveu o número do CIOT. Confira no portal da administradora antes de declarar de novo. Recebido |
| 654 | ✅ | [Logística] Pamcard com resposta completa grava o CIOT e a administradora |
| 655 | ✅ | [Logística] cancelamento do CIOT pela Pamcard |
| 656 | ✅ | [Produção] a OP planejada aparece em aberto |
| 657 | ✅ | [Produção] Imprimir abre a visualização da OP |
| 658 | ✅ | [Produção] impressão da OP lista os insumos e a quantidade prevista |
| 659 | ✅ | [Produção] iniciar produção muda a situação |
| 660 | ✅ | [Produção] BLOQUEAR: produzir mais que o planejado sem insumo é barrado (cacau continua 3) |
| 661 | ✅ | [Produção] BLOQUEAR também vale direto na camada de dados |
| 662 | ✅ | [Produção] PERGUNTAR mostra a necessidade real (3,300) e, respondendo Não, nada é baixado |
| 663 | ✅ | [Produção] produzir menos que o planejado com insumo suficiente não pergunta nada |
| 664 | ✅ | [Produção] concluir 2 de 4: consome metade (3 kg de farinha, 0,55 kg de cacau) e entra 2 bolos |
| 665 | ✅ | [Produção] custo real: (6 + 5,50 + 4 × 2 ÷ 2) ÷ 2 = R$ 7,75 e vira o custo do bolo |
| 666 | ✅ | [Produção] a OP concluída aparece no filtro Todas |
| 667 | ✅ | [Produção] OP concluída não pode ser cancelada |
| 668 | ✅ | [Produção] segunda produção recalcula o custo médio ponderado (R$ 9,25) |
| 669 | ✅ | [Produção] cancelar OP planejada não mexe no estoque |
| 670 | ✅ | [Produção] o produto novo aparece na lista das fichas técnicas |
| 671 | ✅ | [Produção] insumo repetido soma na mesma linha (2 + 1 = 3 kg) |
| 672 | ✅ | [Produção] resumo mostra custo dos insumos (com perda), do lote, unitário e a margem: Insumos R$ 11,50 + adicional R$ 4,00 = lote R$ 15,50   ·   custo unitário R$ 7,75   ·   preço R$ 30,00   ·   margem 287,1% |
| 673 | ✅ | [Produção] o produto não entra como insumo da própria ficha |
| 674 | ✅ | [Produção] salvar a ficha grava os insumos e torna a mercadoria um produto acabado |
| 675 | ✅ | [Produção] OP criada pela ficha calcula a necessidade (6 kg de farinha; 0,5 × 2 × 1,1 = 1,1 kg de cacau) |
| 676 | ✅ | [Produção] ficha circular é recusada com o nome do insumo: O insumo "Torta teste" é fabricado (direta ou indiretamente) com este produto: a receita ficaria circular. Remova esse insumo ou corrija a ficha técnica dele. |
| 677 | ✅ | [Produção] a ficha anterior continua intacta |
| 678 | ✅ | [Produção] perda negativa é recusada com o nome do insumo |
| 679 | ✅ | [Produção] sem alteração, trocar de produto não pergunta nada |
| 680 | ✅ | [Produção] com insumo não salvo, trocar de produto pergunta; respondendo Não, a ficha em edição continua aberta |
| 681 | ✅ | [Produção] fechar a tela com ficha não salva pergunta; respondendo Não, a tela continua aberta |
| 682 | ✅ | [Produção] respondendo Sim, descarta e abre o outro produto (nada gravado) |
| 683 | ✅ | [Segurança] após 5 senhas erradas o usuário fica bloqueado (nem a senha certa entra) |
| 684 | ✅ | [Segurança] definir senha nova desbloqueia o usuário |
| 685 | ✅ | [Segurança] senhas com PBKDF2-SHA256 de 600.000 iterações |
| 686 | ✅ | [Segurança] hash corrompido nega o acesso (falha fechada) |
| 687 | ✅ | [Segurança] hash antigo (100.000 iterações) é refeito no padrão atual no próximo acesso |
| 688 | ✅ | [Segurança] senha: mínimo 8 caracteres e sem senhas óbvias |
| 689 | ✅ | [Segurança] chave de API gravada cifrada (DPAPI) e lida normalmente pelo sistema |
| 690 | ✅ | [Segurança] credencial cifrada em outro computador não é revelada (fica em branco) |
| 691 | ✅ | [Segurança] usuário comum é barrado na camada de dados (cancelar venda e nota, alterar configurações, gerar dados) |
| 692 | ✅ | [Segurança] exportação CSV neutraliza fórmulas (CSV injection) |
| 693 | ✅ | [Segurança] curingas digitados (% e _) são tratados como texto nas buscas |
| 694 | ✅ | [Segurança] SQL sempre parametrizado (texto malicioso é só texto) |
| 695 | ✅ | [Segurança] XML com DTD/entidade externa é recusado (XXE) |
| 696 | ✅ | [Segurança] caracteres de controle nos dados não viram comandos da impressora |
| 697 | ✅ | [Segurança] log íntegro passa na verificação |
| 698 | ✅ | [Segurança] alteração direta no arquivo do log é detectada |
| 699 | ✅ | [Segurança] verificação de integridade do banco (quick_check) |
| 700 | ✅ | [Sistema] Configurações do administrador tem uma aba por grupo + Minha conta |
| 701 | ✅ | [Sistema] salvar a aba 'Empresa' sem mudar nada não dá erro |
| 702 | ✅ | [Sistema] salvar a aba 'Fiscal' sem mudar nada não dá erro |
| 703 | ✅ | [Sistema] salvar a aba 'Reforma tributária' sem mudar nada não dá erro |
| 704 | ✅ | [Sistema] salvar a aba 'Vendas e compras' sem mudar nada não dá erro |
| 705 | ✅ | [Sistema] salvar a aba 'Produção' sem mudar nada não dá erro |
| 706 | ✅ | [Sistema] salvar a aba 'Transporte e CIOT' sem mudar nada não dá erro |
| 707 | ✅ | [Sistema] salvar a aba 'TEF' sem mudar nada não dá erro |
| 708 | ✅ | [Sistema] salvar a aba 'Impressoras' sem mudar nada não dá erro |
| 709 | ✅ | [Sistema] salvar a aba 'E-mail' sem mudar nada não dá erro |
| 710 | ✅ | [Sistema] salvar a aba 'Banco de dados' sem mudar nada não dá erro |
| 711 | ✅ | [Sistema] próximo número da NF-e 'abc' é recusado |
| 712 | ✅ | [Sistema] próximo número da NF-e '-1' é recusado |
| 713 | ✅ | [Sistema] próximo número da NF-e '0' é recusado |
| 714 | ✅ | [Sistema] próximo número da NF-e '1,5' é recusado |
| 715 | ✅ | [Sistema] dias de log não numérico é recusado |
| 716 | ✅ | [Sistema] dias de log = 0 (manter tudo) é aceito |
| 717 | ✅ | [Sistema] alíquota de 150% é recusada |
| 718 | ✅ | [Sistema] nome da loja em branco é recusado |
| 719 | ✅ | [Sistema] nome da loja válido é gravado |
| 720 | ✅ | [Sistema] 'Pamcard: senha / token' fica mascarado |
| 721 | ✅ | [Sistema] 'Chave da API do Resend' fica mascarado |
| 722 | ✅ | [Sistema] página de teste em impressora inexistente avisa que ela não foi encontrada \| Falha na impressão:
Impressora "Impressora Que Nao Existe ERP" não encontrada. Verifique Configurações > Impressoras. |
| 723 | ✅ | [Sistema] e-mail de teste sem chave diz que o envio não está configurado \| O envio de e-mail não está configurado: preencha a chave da API do Resend e o remetente nesta aba e tente de novo. |
| 724 | ✅ | [Sistema] remetente sem e-mail é recusado antes de enviar |
| 725 | ✅ | [Sistema] e-mail de teste enviado ao e-mail do usuário |
| 726 | ✅ | [Sistema] Fazer backup agora cria o arquivo de backup: %USERPROFILE%\AppData\Local\Temp\erp_testes_d9f7518f5d454eef99a9a5384ed32e45\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261002_193123_manual.db \| Backup salvo em:
%USERPROFILE%\AppData\Local\Temp\erp_testes_d9f7518f5d454eef99a9a5384ed32e45\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261002_193123_manual.db |
| 727 | ✅ | [Sistema] salvar o nome da loja atualiza na hora o cabeçalho da janela principal: ERP Loja - Mercadinho Novo - Configurações |
| 728 | ✅ | [Sistema] o log registra abrir e fechar Relatórios |
| 729 | ✅ | [Sistema] abrir uma tela registra uma linha só (não duplica): 1 |
| 730 | ✅ | [Sistema] salvar configurações registra a gravação com a tela Configurações |
| 731 | ✅ | [Sistema] a tela de log abre listando registros |
| 732 | ✅ | [Sistema] filtro por categoria TELA mostra só TELA |
| 733 | ✅ | [Sistema] a lista de usuários traz quem aparece no log |
| 734 | ✅ | [Sistema] busca por texto filtra (Enter) |
| 735 | ✅ | [Sistema] busca sem resultado deixa a grade vazia sem erro |
| 736 | ✅ | [Sistema] datas invertidas no log avisam o usuário |
| 737 | ✅ | [Sistema] a tela 'PDV - frente de caixa' tem tópico de ajuda |
| 738 | ✅ | [Sistema] a tela 'Orçamentos' tem tópico de ajuda |
| 739 | ✅ | [Sistema] a tela 'Pedidos de venda' tem tópico de ajuda |
| 740 | ✅ | [Sistema] a tela 'Vendas realizadas' tem tópico de ajuda |
| 741 | ✅ | [Sistema] a tela 'Pedidos de compra' tem tópico de ajuda |
| 742 | ✅ | [Sistema] a tela 'Importar XML de NF-e' tem tópico de ajuda |
| 743 | ✅ | [Sistema] a tela 'Compras realizadas' tem tópico de ajuda |
| 744 | ✅ | [Sistema] a tela 'Fichas técnicas' tem tópico de ajuda |
| 745 | ✅ | [Sistema] a tela 'Ordens de produção' tem tópico de ajuda |
| 746 | ✅ | [Sistema] a tela 'Fretes e CIOT' tem tópico de ajuda |
| 747 | ✅ | [Sistema] a tela 'Veículos' tem tópico de ajuda |
| 748 | ✅ | [Sistema] a tela 'Estoque' tem tópico de ajuda |
| 749 | ✅ | [Sistema] a tela 'Contas a pagar e receber' tem tópico de ajuda |
| 750 | ✅ | [Sistema] a tela 'Cobrança bancária (boletos)' tem tópico de ajuda |
| 751 | ✅ | [Sistema] a tela 'Contas bancárias' tem tópico de ajuda |
| 752 | ✅ | [Sistema] a tela 'Notas fiscais' tem tópico de ajuda |
| 753 | ✅ | [Sistema] a tela 'Painel gerencial' tem tópico de ajuda |
| 754 | ✅ | [Sistema] a tela 'Relatórios' tem tópico de ajuda |
| 755 | ✅ | [Sistema] a tela 'Clientes e fornecedores' tem tópico de ajuda |
| 756 | ✅ | [Sistema] a tela 'Produtos' tem tópico de ajuda |
| 757 | ✅ | [Sistema] a tela 'Categorias' tem tópico de ajuda |
| 758 | ✅ | [Sistema] a tela 'Usuários' tem tópico de ajuda |
| 759 | ✅ | [Sistema] a tela 'Configurações' tem tópico de ajuda |
| 760 | ✅ | [Sistema] a tela 'Log de atividades' tem tópico de ajuda |
| 761 | ✅ | [Sistema] a tela 'Localizar tela' tem tópico de ajuda |
| 762 | ✅ | [Sistema] com uma tela aberta só aparece o '?' dela (visíveis: 1) |
| 763 | ✅ | [Sistema] o '?' fica no canto direito da tela embutida |
| 764 | ✅ | [Sistema] o '?' de Relatórios abre a ajuda de Relatórios |
| 765 | ✅ | [Sistema] a janela principal começa na tela inicial |
| 766 | ✅ | [Sistema] F5 abre o Painel gerencial à direita |
| 767 | ✅ | [Sistema] F8 abre Relatórios no lugar do painel (uma tela por vez, sem abas) |
| 768 | ✅ | [Sistema] F6 abre Estoque; o cabeçalho mostra a área e a tela: Estoque  ›  Estoque |
| 769 | ✅ | [Sistema] as opções da tela ficam numa lista vertical com barra de rolagem à direita |
| 770 | ✅ | [Sistema] a listagem fica no meio |
| 771 | ✅ | [Sistema] abrir de novo a tela que está aberta não cria outra |
| 772 | ✅ | [Sistema] Ctrl+W fecha a tela e volta ao início |
| 773 | ✅ | [Sistema] Ctrl+W sem tela aberta não faz nada |
| 774 | ✅ | [Sistema] o cabeçalho tem o × para fechar a tela |
| 775 | ✅ | [Sistema] o × fecha a tela |
| 776 | ✅ | [Sistema] digitar num campo marca a tela com alterações não salvas |
| 777 | ✅ | [Sistema] trocar de tela com dados não salvos pergunta; "Continuar editando" mantém tudo [ativa=CrudForm, texto=Bebidas, mensagens=CONFIRMAR:Você preencheu ou alterou dados em "Categorias" que ainda nã] |
| 778 | ✅ | [Sistema] "Descartar e sair" desfaz o que foi digitado e abre a outra tela |
| 779 | ✅ | [Sistema] o descarte fica registrado no log |
| 780 | ✅ | [Sistema] digitar na pesquisa não conta como alteração |
| 781 | ✅ | [Sistema] nenhuma mensagem de erro |
| 782 | ✅ | [Sistema] no PDV embutido F5 abre o desconto, não o painel |
| 783 | ✅ | [Sistema] com venda em andamento, abrir outra tela pergunta antes; respondendo Não o PDV continua com a venda |
| 784 | ✅ | [Sistema] responder Não à venda em andamento mantém a janela principal aberta |
| 785 | ✅ | [Sistema] o PDV continua na tela para o usuário concluir a venda |
| 786 | ✅ | [Sistema] respondendo Sim a janela principal fecha |
| 787 | ✅ | [Sistema] buscar 'vender' traz 'PDV - frente de caixa' entre os 3 primeiros (posição 1 de 17: PDV - frente de caixa, Painel gerencial, Vendas realizadas) |
| 788 | ✅ | [Sistema] buscar 'como cadastrar cliente' traz 'Clientes e fornecedores' entre os 3 primeiros (posição 1 de 15: Clientes e fornecedores, Contas a pagar e receber, Orçamentos) |
| 789 | ✅ | [Sistema] buscar 'cadastrar produto' traz 'Produtos' entre os 3 primeiros (posição 2 de 19: Importar XML de NF-e, Produtos, Categorias) |
| 790 | ✅ | [Sistema] buscar 'onde fica o estoque' traz 'Estoque' entre os 3 primeiros (posição 1 de 14: Estoque, Compras realizadas, Configurações) |
| 791 | ✅ | [Sistema] buscar 'trocar minha senha' traz 'Configurações' entre os 3 primeiros (posição 1 de 6: Configurações, Log de atividades, Usuários) |
| 792 | ✅ | [Sistema] buscar 'fazer backup' traz 'Configurações' entre os 3 primeiros (posição 1 de 2: Configurações, Log de atividades) |
| 793 | ✅ | [Sistema] buscar 'emitir nota' traz 'Notas fiscais' entre os 3 primeiros (posição 1 de 12: Notas fiscais, Pedidos de venda, Vendas realizadas) |
| 794 | ✅ | [Sistema] buscar 'pagar fornecedor' traz 'Contas a pagar e receber' entre os 3 primeiros (posição 1 de 15: Contas a pagar e receber, Importar XML de NF-e, Pedidos de compra) |
| 795 | ✅ | [Sistema] buscar 'cancelar uma venda' traz 'Vendas realizadas' entre os 3 primeiros (posição 1 de 21: Vendas realizadas, Pedidos de venda, PDV - frente de caixa) |
| 796 | ✅ | [Sistema] buscar 'imprimir boleto' traz 'Cobrança bancária (boletos)' entre os 3 primeiros (posição 1 de 8: Cobrança bancária (boletos), Configurações, Vendas realizadas) |
| 797 | ✅ | [Sistema] buscar 'fabricar' traz 'Ordens de produção' entre os 3 primeiros (posição 1 de 3: Ordens de produção, Fichas técnicas, Produtos) |
| 798 | ✅ | [Sistema] buscar só uma palavra comum ('de') ainda devolve resultados |
| 799 | ✅ | [Sistema] e-mail incompleto é recusado |
| 800 | ✅ | [Sistema] e-mail de outro usuário (mesmo com maiúsculas) é recusado |
| 801 | ✅ | [Sistema] e-mail válido é gravado sem espaços |
| 802 | ✅ | [Sistema] senha atual errada é recusada |
| 803 | ✅ | [Sistema] confirmação diferente é recusada |
| 804 | ✅ | [Sistema] senha alterada e campos limpos |
| 805 | ✅ | [Sistema] a troca de senha fica registrada no log de acesso (categoria ACESSO) |
| 806 | ✅ | [Sistema] busca sem acento ('configuracoes') acha Configurações |
| 807 | ✅ | [Sistema] busca com acento e maiúsculas acha Relatórios |
| 808 | ✅ | [Sistema] busca por 'boleto' acha a cobrança |
| 809 | ✅ | [Sistema] palavra inexistente não acha nada |
| 810 | ✅ | [Sistema] busca só com espaços lista todas as telas |
| 811 | ✅ | [Sistema] buscar 'senha' não dá erro |
| 812 | ✅ | [Sistema] buscar 'pix' não dá erro |
| 813 | ✅ | [Sistema] buscar 'nota fiscal' não dá erro |
| 814 | ✅ | [Sistema] buscar 'ç' não dá erro |
| 815 | ✅ | [Sistema] buscar 'ã' não dá erro |
| 816 | ✅ | [Sistema] buscar 'a e i' não dá erro |
| 817 | ✅ | [Sistema] buscar '()' não dá erro |
| 818 | ✅ | [Sistema] buscar '%' não dá erro |
| 819 | ✅ | [Sistema] buscar ''' não dá erro |
| 820 | ✅ | [Sistema] o localizador lista as telas, sem listar a si mesmo |
| 821 | ✅ | [Sistema] o localizador lista resultados e seleciona o primeiro |
| 822 | ✅ | [Sistema] Ctrl+K, 'estoque minimo' e Enter abre a tela de Estoque |
| 823 | ✅ | [Sistema] sem resultado o localizador diz 'Nenhuma tela encontrada' |
| 824 | ✅ | [Sistema] Enter sem resultado não fecha o localizador |
| 825 | ✅ | [Sistema] Esc no localizador não abre nenhuma tela |
| 826 | ✅ | [Sistema] senha errada não entra |
| 827 | ✅ | [Sistema] senha errada mostra mensagem |
| 828 | ✅ | [Sistema] usuário inativo não entra |
| 829 | ✅ | [Sistema] username com maiúsculas e espaços entra como usuário comum |
| 830 | ✅ | [Sistema] o log registra acessos recusados e a entrada: Entrou no sistema, Acesso recusado (usuário inativo), Acesso recusado |
| 831 | ✅ | [Sistema] o log não guarda senhas digitadas |
| 832 | ✅ | [Sistema] modo escuro aplica cores escuras às telas abertas |
| 833 | ✅ | [Sistema] a escolha fica gravada para o usuário |
| 834 | ✅ | [Sistema] telas abertas depois já vêm no modo escuro |
| 835 | ✅ | [Sistema] voltar ao modo claro restaura as cores originais |
| 836 | ✅ | [Sistema] sem chave do Resend o sistema explica que o e-mail não está configurado |
| 837 | ✅ | [Sistema] usuário sem e-mail recebe orientação para falar com o administrador |
| 838 | ✅ | [Sistema] e-mail (com maiúsculas) recebe o código de 6 dígitos |
| 839 | ✅ | [Sistema] falha no envio mostra a mensagem do Resend |
| 840 | ✅ | [Sistema] código errado avisa quantas tentativas restam |
| 841 | ✅ | [Sistema] o código recebido redefine a senha mesmo depois de um reenvio que falhou |
| 842 | ✅ | [Sistema] a nova senha passa a valer |
| 843 | ✅ | [Sistema] menu do usuário comum não mostra 'Contas bancárias' |
| 844 | ✅ | [Sistema] menu do usuário comum não mostra 'Usuários' |
| 845 | ✅ | [Sistema] menu do usuário comum não mostra 'Log de atividades' |
| 846 | ✅ | [Sistema] menu do usuário comum mostra o PDV |
| 847 | ✅ | [Sistema] o localizador não oferece telas de administrador ao usuário comum |
| 848 | ✅ | [Sistema] usuário comum não consegue abrir o Log de atividades |
| 849 | ✅ | [Sistema] e recebe a explicação de que a tela é só de administradores |
| 850 | ✅ | [Sistema] Configurações do usuário comum tem só a aba Minha conta: Minha conta |
| 851 | ✅ | [Validação] CNPJ: aceita com ou sem pontuação; recusa dígito verificador errado, tamanho errado e letras |
| 852 | ✅ | [Validação] CPF e CNPJ recebem a máscara padrão |
| 853 | ✅ | [Validação] CPF/CNPJ: sequência repetida é recusada |
| 854 | ✅ | [Validação] NCM com 8 e CFOP com 4 dígitos (pontos removidos; a letra O no lugar de zero é recusada) |
| 855 | ✅ | [Validação] campos numéricos recusam texto; percentual só de 0 a 100 |
| 856 | ✅ | [Validação] e-mail no formato nome@dominio |
| 857 | ✅ | [Validação] placa Mercosul/antiga em maiúsculas, sem hífen |
| 858 | ✅ | [Validação] CEP e telefone com a quantidade certa de dígitos e máscara |
| 859 | ✅ | [Validação] código de barras confere o dígito verificador |
| 860 | ✅ | [Validação] endereço de integração só com https |
| 861 | ✅ | [Validação] texto livre tem tamanho máximo e não aceita caracteres de controle |
| 862 | ✅ | [Validação] no campo CNPJ, letra digitada é bloqueada; números e pontuação passam |
| 863 | ✅ | [Validação] texto colado com letras não é alterado em silêncio: fica marcado como inválido |
| 864 | ✅ | [Validação] ao sair do campo, o CNPJ recebe a máscara |
| 865 | ✅ | [Validação] CNPJ inválido fica marcado com a explicação |
| 866 | ✅ | [Validação] a tela não grava com campo inválido |
| 867 | ✅ | [Validação] campo numérico não aceita letras |
| 868 | ✅ | [Validação] cadastro com CNPJ inválido não é gravado |
| 869 | ✅ | [Validação] CNPJ válido é gravado no formato padrão |
| 870 | ✅ | [Validação] parâmetro com formato inválido é recusado também na camada de dados |
| 871 | ✅ | [Vendas] a prazo sem cliente é recusado pedindo o cliente |
| 872 | ✅ | [Vendas] a prazo com cliente dentro do limite grava a venda \| Para vender a prazo, identifique o cliente (F2 no PDV). |
| 873 | ✅ | [Vendas] a prazo em 2x gera 2 parcelas em aberto no nome do cliente |
| 874 | ✅ | [Vendas] as parcelas somam o total da venda |
| 875 | ✅ | [Vendas] usuário comum não vende acima do limite de crédito |
| 876 | ✅ | [Vendas] venda recusada por limite continua no PDV para outra forma de pagamento |
| 877 | ✅ | [Vendas] administrador é avisado do limite excedido; respondendo Não, a venda não sai |
| 878 | ✅ | [Vendas] administrador confirma e vende acima do limite de crédito \| Para vender a prazo, identifique o cliente (F2 no PDV). \| Limite de crédito do cliente excedido (disponível: R$ 30,23, venda: R$ 60,46). Escolha outra forma de pagamento ou peça a um administrador. |
| 879 | ✅ | [Vendas] 3*código + código somam 4 unidades na mesma linha |
| 880 | ✅ | [Vendas] F4 = 2 lança o próximo item com 2 unidades |
| 881 | ✅ | [Vendas] F6 remove o item selecionado |
| 882 | ✅ | [Vendas] quantidade absurda é recusada sem alterar a venda |
| 883 | ✅ | [Vendas] F10 e tecla 2 escolhem PIX (a venda foi gravada como PIX) \| Quantidade "99999999999999999999999999999999" inválida: use de 0,001 a 99.999 (ex.: 3*código). |
| 884 | ✅ | [Vendas] transação TEF aprovada fica CONFIRMADA com a venda |
| 885 | ✅ | [Vendas] PDV volta a ficar livre depois da venda |
| 886 | ✅ | [Vendas] F7 respondendo Não mantém a venda |
| 887 | ✅ | [Vendas] F7 respondendo Sim cancela a venda em andamento |
| 888 | ✅ | [Vendas] ao cancelar venda a prazo com parcela recebida, a confirmação avisa do valor já recebido |
| 889 | ✅ | [Vendas] respondendo Não, a venda continua finalizada |
| 890 | ✅ | [Vendas] comprovante de venda com frete mostra a linha do frete |
| 891 | ✅ | [Vendas] comprovante mostra o total com frete |
| 892 | ✅ | [Vendas] usuário comum: 30% de desconto é recusado |
| 893 | ✅ | [Vendas] a recusa explica o limite |
| 894 | ✅ | [Vendas] desconto recusado não altera o total |
| 895 | ✅ | [Vendas] usuário comum: desconto dentro do limite é aceito |
| 896 | ✅ | [Vendas] removendo item, o desconto que passou do limite é barrado antes do pagamento (F5 para ajustar) |
| 897 | ✅ | [Vendas] administrador pode dar 50% de desconto |
| 898 | ✅ | [Vendas] venda com 50% de desconto do administrador gravada com o total certo |
| 899 | ✅ | [Vendas] desconto do item maior que o item é recusado ao salvar |
| 900 | ✅ | [Vendas] corrigido o desconto, o orçamento é salvo \| O desconto do item "Arroz Tipo 1 5kg" (R$ 35,23) é maior que o valor dele (R$ 30,23). Corrija o desconto do item ou use o desconto geral. |
| 901 | ✅ | [Vendas] nenhum item de orçamento/pedido com total negativo |
| 902 | ✅ | [Vendas] orçamento com 50% de desconto é recusado para usuário comum |
| 903 | ✅ | [Vendas] orçamento com 5% de desconto é salvo \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. |
| 904 | ✅ | [Vendas] código lançado duas vezes soma na mesma linha do orçamento |
| 905 | ✅ | [Vendas] validade padrão do orçamento = hoje + validade_orcamento_dias |
| 906 | ✅ | [Vendas] orçamento não mexe no estoque |
| 907 | ✅ | [Vendas] orçamento aparece em aberto na lista |
| 908 | ✅ | [Vendas] aprovar gera o pedido de venda e marca o orçamento APROVADO |
| 909 | ✅ | [Vendas] aprovar de novo um orçamento já aprovado é recusado (não gera pedido duplicado) |
| 910 | ✅ | [Vendas] pedido gerado aparece em Pedidos de venda |
| 911 | ✅ | [Vendas] faturar gera a venda com o total do pedido \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. \| Este documento está APROVADO. A operação só vale para documentos em aberto. |
| 912 | ✅ | [Vendas] faturar baixa o estoque |
| 913 | ✅ | [Vendas] venda faturada aparece em Vendas realizadas |
| 914 | ✅ | [Vendas] comprovante mostra o nº da venda e o cliente |
| 915 | ✅ | [Vendas] administrador cancela a venda |
| 916 | ✅ | [Vendas] cancelamento devolve o estoque |
| 917 | ✅ | [Vendas] cancelamento cancela as contas da venda |
| 918 | ✅ | [Vendas] cancelamento reabre o pedido de origem |
| 919 | ✅ | [Vendas] cancelar de novo avisa que já está cancelada |
| 920 | ✅ | [Vendas] usuário comum não fatura pedido a prazo acima do limite de crédito do cliente |
| 921 | ✅ | [Vendas] orçamento vencido não aparece entre os em aberto |
| 922 | ✅ | [Vendas] filtro Vencidos mostra o orçamento como VENCIDO |
| 923 | ✅ | [Vendas] duplicar o orçamento vencido cria um novo em aberto com validade renovada |
| 924 | ✅ | [Vendas] aprovar orçamento vencido pede confirmação; respondendo Não, nada muda |
| 925 | ✅ | [Vendas] confirmando, o orçamento vencido é aprovado |
| 926 | ✅ | [Vendas] faturar no boleto sem cliente é recusado |
| 927 | ✅ | [Vendas] faturar no boleto com cliente gera a venda \| Para vender no boleto o pedido precisa ter um cliente. Abra o pedido e escolha o cliente. |
| 928 | ✅ | [Vendas] venda no boleto fica em aberto no contas a receber (não entra como recebida) |
| 929 | ✅ | [Vendas] produto sem preço de venda não é lançado no PDV e o motivo é explicado |
| 930 | ✅ | [Vendas] PERGUNTAR: 3ª unidade (estoque 2) pergunta; respondendo Não, não lança |
| 931 | ✅ | [Vendas] PERGUNTAR: respondendo Sim, lança |
| 932 | ✅ | [Vendas] BLOQUEAR: não lança e explica que está bloqueado |
| 933 | ✅ | [Vendas] PERMITIR: lança sem perguntar |
| 934 | ✅ | [Vendas] TEF negado não grava a venda |
| 935 | ✅ | [Vendas] TEF negado avisa o operador |
| 936 | ✅ | [Vendas] TEF negado mantém os itens no PDV |
| 937 | ✅ | [Vendas] transação negada registrada como NEGADA |
| 938 | ✅ | [Vendas] TEF cancelado pelo operador não grava a venda |
| 939 | ✅ | [Vendas] tecla 4 = crédito, aprovado no TEF, grava a venda \| Pagamento não aprovado: Transação negada pelo emissor (simulação). \| Pagamento não aprovado: Operação cancelada pelo operador. |
| 940 | ✅ | [Vendas] crédito parcelado em 3x chega ao TEF |
| 941 | ✅ | [Vendas] crédito no TEF gera a conta a receber já paga (a operadora recebe da adquirente) |
| 942 | ✅ | [Vendas] cancelar a venda estorna a transação TEF e informa o estorno |
| 943 | ✅ | [Vendas] administrador dá 100% de desconto |
| 944 | ✅ | [Vendas] venda de total zero é gravada |
| 945 | ✅ | [Vendas] venda de total zero não aciona o TEF (nenhuma transação de R$ 0,00) |
| 946 | ✅ | [Vendas] venda de total zero não gera conta (RN-VEN-09) |
