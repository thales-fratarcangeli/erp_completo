# Relatório de testes

> Gerado automaticamente pela bateria `testes/ErpLoja.Testes` a cada execução. Não edite à mão.

| Item | Valor |
|---|---|
| Data da execução | 02/10/2026 00:39 |
| Versão do sistema | 1.1.0.0 (banco v12) |
| Ambiente | Microsoft Windows 10.0.19045 · .NET 10.0.12 |
| Verificações | 259 |
| Aprovadas | 259 |
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
| 246 | ✅ | atalho .lnk do Windows criado (usado na primeira execução) |
| 247 | ✅ | toda tela aberta tem tópico de ajuda |
| 248 | ✅ | todos os diálogos têm ajuda (9 diálogos) |
| 249 | ✅ | todas as telas do sistema foram abertas no teste |
| 250 | ✅ | manual do usuário gerado em docs/12-manual-do-usuario.md a partir da ajuda |
| 251 | ✅ | dados de demonstração gerados em 2,7 s — Dados de demonstração gerados: 35 clientes, 8 fornecedores, 50 produtos, 48 compras, 77 ordens de produção, 1399 vendas, 7 orçamentos/pedidos, 3 fretes, 8 boletos, 5 notas fiscais. |
| 252 | ✅ | dados de demonstração íntegros (chaves estrangeiras) |
| 253 | ✅ | insumos não ficam negativos na demonstração |
| 254 | ✅ | CPF/CNPJ da demonstração são válidos |
| 255 | ✅ | modelo de dados (dicionário + diagrama ER) gerado em docs/06-modelo-de-dados.md |
| 256 | ✅ | [PDV] lançar 2*código mostra o total da venda |
| 257 | ✅ | [PDV] F10 + dinheiro grava a venda |
| 258 | ✅ | [PDV] total gravado = 2 × preço |
| 259 | ✅ | [PDV] nenhuma mensagem de erro durante a venda |
