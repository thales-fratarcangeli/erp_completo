# Relatório de testes

> Gerado automaticamente pela bateria `testes/ErpLoja.Testes` a cada execução. Não edite à mão.

| Item | Valor |
|---|---|
| Data da execução | 03/10/2026 13:29 |
| Versão do sistema | 1.5.0.0 (banco v16) |
| Ambiente | Microsoft Windows 10.0.19045 · .NET 10.0.12 |
| Verificações | 1023 |
| Aprovadas | 1023 |
| Reprovadas | 0 |
| Resultado | **APROVADO** |

## Verificações

| # | Resultado | Verificação |
|---:|:---:|---|
| 1 | ✅ | banco legado reconhecido como banco do sistema |
| 2 | ✅ | legado v1: registra a 1 e aplica as migrações 2 a 16 |
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
| 23 | ✅ | banco novo: 16 migrações aplicadas |
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
| 212 | ✅ | tela Ordens de serviço: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 213 | ✅ | tela Ordens de serviço |
| 214 | ✅ | tela Fretes: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 215 | ✅ | tela Fretes |
| 216 | ✅ | tela Cobrança: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 217 | ✅ | tela Cobrança |
| 218 | ✅ | tela Boleto: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 219 | ✅ | tela Boleto |
| 220 | ✅ | tela Localizar tela: botão de ajuda visível (sem botão) |
| 221 | ✅ | tela Localizar tela |
| 222 | ✅ | tela Pagamento: botão de ajuda visível (sem botão) |
| 223 | ✅ | tela Pagamento |
| 224 | ✅ | tela TEF simulador: botão de ajuda visível (sem botão) |
| 225 | ✅ | tela TEF simulador |
| 226 | ✅ | tela Painel gerencial: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 227 | ✅ | tela Painel gerencial |
| 228 | ✅ | tela Log de atividades: botão de ajuda visível [visível=True, índice=0, direita=1358/1370] |
| 229 | ✅ | tela Log de atividades |
| 230 | ✅ | o ? de cada tela abre só a explicação daquela tela |
| 231 | ✅ | a tela abre embutida no espaço à direita do menu (sem janela solta) |
| 232 | ✅ | PDV com fundo branco |
| 233 | ✅ | abrir outra tela substitui a atual (uma tela por vez, sem abas) |
| 234 | ✅ | tela no modelo padrão: opções em lista à direita |
| 235 | ✅ | abrir a tela que já está aberta não cria outra |
| 236 | ✅ | sem telas abertas, volta à tela inicial |
| 237 | ✅ | log gravado em banco separado (loja_log.db) |
| 238 | ✅ | venda gera log de dados (tabelas gravadas e id do registro) |
| 239 | ✅ | migrações registram um resumo (sem cada comando) |
| 240 | ✅ | abrir e fechar telas gera log |
| 241 | ✅ | gravação desfeita (rollback) não aparece no log |
| 242 | ✅ | gravação confirmada aparece no log |
| 243 | ✅ | clique em botão gera log |
| 244 | ✅ | classificação dos comandos SQL para o log |
| 245 | ✅ | senhas não aparecem no log |
| 246 | ✅ | registros antigos são apagados conforme o parâmetro de retenção |
| 247 | ✅ | consulta do log com filtros |
| 248 | ✅ | grade sem registros mostra explicação (estado vazio) |
| 249 | ✅ | atalho .lnk do Windows criado (usado na primeira execução) |
| 250 | ✅ | todos os campos de texto editáveis das telas têm formato de entrada (131 campos conferidos) |
| 251 | ✅ | nenhuma caixa de texto é criada sem formato no código-fonte |
| 252 | ✅ | toda tela aberta tem tópico de ajuda |
| 253 | ✅ | todos os diálogos têm ajuda (10 diálogos) |
| 254 | ✅ | todas as telas do sistema foram abertas no teste |
| 255 | ✅ | manual do usuário gerado em docs/12-manual-do-usuario.md a partir da ajuda |
| 256 | ✅ | dados de demonstração gerados em 3,0 s — Dados de demonstração gerados: 35 clientes, 8 fornecedores, 50 produtos, 48 compras, 77 ordens de produção, 6 ordens de serviço, 1368 vendas, 7 orçamentos/pedidos, 3 fretes, 8 boletos, 5 notas fiscais. |
| 257 | ✅ | dados de demonstração íntegros (chaves estrangeiras) |
| 258 | ✅ | insumos não ficam negativos na demonstração |
| 259 | ✅ | CPF/CNPJ da demonstração são válidos |
| 260 | ✅ | modelo de dados (dicionário + diagrama ER) gerado em docs/06-modelo-de-dados.md |
| 261 | ✅ | [Assistente] desligado: avisa como ligar e não chama a API |
| 262 | ✅ | [Assistente] sem chave: orienta a configurar |
| 263 | ✅ | [Assistente] chave da API gravada cifrada |
| 264 | ✅ | [Assistente] pergunta respondida pela API (resposta lida corretamente) |
| 265 | ✅ | [Assistente] CPF, e-mail e telefone mascarados antes de sair do computador |
| 266 | ✅ | [Assistente] a tela em que a pessoa está vai junto para contextualizar |
| 267 | ✅ | [Assistente] modelo e chave configurados |
| 268 | ✅ | [Assistente] instruções com o manual das telas e proteção contra injeção, em bloco de cache |
| 269 | ✅ | [Assistente] nenhum dado da loja vai na requisição (nome da loja, clientes) |
| 270 | ✅ | [Assistente] modelo reserva em caso de recusa e esforço baixo (respostas rápidas) |
| 271 | ✅ | [Assistente] recusa do modelo vira mensagem para reformular |
| 272 | ✅ | [Assistente] chave recusada pela API: mensagem clara |
| 273 | ✅ | [Assistente] limite de perguntas por hora por usuário |
| 274 | ✅ | [Assistente] perguntas e respostas no log (já mascaradas) |
| 275 | ✅ | [Assistente] Ctrl+I abre o assistente ao lado da tela (sem fechá-la) |
| 276 | ✅ | [Assistente] a conversa mostra a pergunta e a resposta |
| 277 | ✅ | [Assistente] conversar com o assistente não marca a tela como alterada |
| 278 | ✅ | [Assistente] o assistente aparece no Buscar tela (Ctrl+K) |
| 279 | ✅ | [Assistente] pergunta cancelada não vira mensagem de "sem conexão" |
| 280 | ✅ | [Assistente] Ollama: pergunta enviada ao endereço local, com as instruções, sem streaming e com o CPF mascarado |
| 281 | ✅ | [Assistente] Ollama sem o modelo baixado: mensagem diz qual modelo conferir |
| 282 | ✅ | [Assistente] Ollama fechado: mensagem clara |
| 283 | ✅ | [Cadastros] o próprio usuário aparece na lista |
| 284 | ✅ | [Cadastros] administrador não consegue se inativar |
| 285 | ✅ | [Cadastros] continua ativo |
| 286 | ✅ | [Cadastros] administrador não tira o próprio acesso |
| 287 | ✅ | [Cadastros] continua administrador |
| 288 | ✅ | [Cadastros] senha curta é recusada |
| 289 | ✅ | [Cadastros] username repetido (maiúsculas diferentes) é recusado \| Não foi possível salvar: já existe outro registro com este "Username". Informe outro valor. |
| 290 | ✅ | [Cadastros] usuário comum cadastrado |
| 291 | ✅ | [Cadastros] senha gravada como hash e conferível |
| 292 | ✅ | [Cadastros] editar com senha em branco mantém a senha |
| 293 | ✅ | [Cadastros] localizador de telas esconde as telas de administrador do usuário comum |
| 294 | ✅ | [Cadastros] o único administrador ativo não pode ser inativado, rebaixado nem excluído |
| 295 | ✅ | [Cadastros] inativar usuário comum é permitido |
| 296 | ✅ | [Cadastros] havendo outro administrador ativo, o primeiro pode ser rebaixado |
| 297 | ✅ | [Cadastros] seleciona o próprio usuário |
| 298 | ✅ | [Cadastros] não exclui o próprio usuário |
| 299 | ✅ | [Cadastros] CPF com dígito errado é recusado |
| 300 | ✅ | [Cadastros] sem papel marcado é recusado |
| 301 | ✅ | [Cadastros] CPF/CNPJ de pessoa já cadastrada é recusado e a mensagem diz quem é \| Já existe um cadastro com este CPF/CNPJ: "Diego Lima Rocha" (registro nº 1).
Em vez de cadastrar de novo, edite esse registro e marque os papéis que faltam (cliente, fornecedor...). |
| 302 | ✅ | [Cadastros] nenhuma pessoa duplicada gravada |
| 303 | ✅ | [Cadastros] cadastro corrigido é salvo sem erro |
| 304 | ✅ | [Cadastros] pessoa nova gravada uma única vez [47\|Maria Apressada\|529.982.247-25 esperado 529.982.247-25] |
| 305 | ✅ | [Cadastros] pessoa existente aparece na lista |
| 306 | ✅ | [Cadastros] salvar a própria pessoa não acusa CPF duplicado |
| 307 | ✅ | [Cadastros] alteração gravada |
| 308 | ✅ | [Cadastros] cliente com vendas aparece na lista |
| 309 | ✅ | [Cadastros] excluir pessoa com histórico oferece inativar |
| 310 | ✅ | [Cadastros] pessoa inativada e vendas preservadas |
| 311 | ✅ | [Cadastros] filtro Inativos mostra a pessoa inativada |
| 312 | ✅ | [Cadastros] categoria em uso aparece na lista |
| 313 | ✅ | [Cadastros] excluir categoria em uso explica o que fazer e não exclui |
| 314 | ✅ | [Cadastros] categoria repetida (maiúsculas) é recusada \| Não foi possível salvar: já existe outro registro com este "Nome". Informe outro valor. |
| 315 | ✅ | [Cadastros] categoria nova salva |
| 316 | ✅ | [Cadastros] categoria sem produtos é excluída |
| 317 | ✅ | [Cadastros] produto com movimentação aparece na lista |
| 318 | ✅ | [Cadastros] editar o preço não arredonda o custo médio (4 casas) |
| 319 | ✅ | [Cadastros] custo médio de produto com movimentação não é alterado à mão |
| 320 | ✅ | [Cadastros] custo inicial de produto sem movimentação pode ser informado e corrigido |
| 321 | ✅ | [Cadastros] EAN com dígito verificador errado é recusado |
| 322 | ✅ | [Cadastros] código de barras de outro produto é recusado \| O código de barras 7891950522666 já pertence ao produto "Arroz Tipo 1 5kg" (código D001). No PDV a leitura ficaria ambígua: confira o código lido. |
| 323 | ✅ | [Cadastros] NCM incompleto é recusado |
| 324 | ✅ | [Cadastros] CST com letra (O no lugar de zero) é recusado |
| 325 | ✅ | [Cadastros] redução de IBS acima de 100% é recusada |
| 326 | ✅ | [Cadastros] código repetido (minúsculas) é recusado dizendo qual campo \| Não foi possível salvar: já existe outro registro com este "Código". Informe outro valor. |
| 327 | ✅ | [Cadastros] produto corrigido é salvo |
| 328 | ✅ | [Cadastros] produto gravado com o EAN correto |
| 329 | ✅ | [Cadastros] produto existente aparece na lista |
| 330 | ✅ | [Cadastros] editar produto existente não acusa o próprio código de barras |
| 331 | ✅ | [Cadastros] pesquisa pelo código encontra só o produto novo |
| 332 | ✅ | [PDV] lançar 2*código mostra o total da venda |
| 333 | ✅ | [PDV] F10 + dinheiro grava a venda |
| 334 | ✅ | [PDV] total gravado = 2 × preço |
| 335 | ✅ | [PDV] nenhuma mensagem de erro durante a venda |
| 336 | ✅ | [Cobranca] validador aponta quantidade em branco no trailer |
| 337 | ✅ | [Cobranca] validador aponta valor com letras no segmento P |
| 338 | ✅ | [Cobranca] modelo com tamanho absurdo é recusado com mensagem |
| 339 | ✅ | [Cobranca] conta de demonstração na lista |
| 340 | ✅ | [Cobranca] modelo sem o nosso número é recusado \| "Modelo do campo livre (25 posições)" O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro. |
| 341 | ✅ | [Cobranca] espécie com letras (DM) é recusada: a remessa exige código numérico \| "Espécie do título (02 = DM)" aceita só números (2 dígitos, ex.: 02). |
| 342 | ✅ | [Cobranca] nosso número maior que o espaço do modelo é recusado \| O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras. |
| 343 | ✅ | [Cobranca] voltar o último nosso número abaixo dos já emitidos é recusado \| Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números. |
| 344 | ✅ | [Cobranca] dias de protesto acima de 99 é recusado \| Dias para protesto: de 0 (não protestar) a 99. |
| 345 | ✅ | [Cobranca] nada inválido foi gravado |
| 346 | ✅ | [Cobranca] conta válida salva normalmente (ambiente PRODUCAO): "Modelo do campo livre (25 posições)" O modelo precisa conter o nosso número ({nossonumero:N}): é ele que diferencia um boleto do outro.: "Espécie do título (02 = DM)" aceita só números (2 dígitos, ex.: 02).: O nosso número tem 15 dígitos, mas o modelo do campo livre reserva só 11 posições para ele ({nossonumero:11}). Ajuste um dos dois conforme o manual do banco, senão boletos diferentes podem sair com o mesmo código de barras.: Já foram emitidos boletos até o nosso número 8 nesta conta. O "último nosso número usado" não pode ser menor que isso, senão os próximos boletos repetiriam números.: Dias para protesto: de 0 (não protestar) a 99. |
| 347 | ✅ | [Cobranca] boletos de teste recusados em conta de produção |
| 348 | ✅ | [Cobranca] modelo inválido: boletos de teste explicam o problema |
| 349 | ✅ | [Cobranca] nenhuma conta de teste fica órfã sem boleto |
| 350 | ✅ | [Cobranca] boleto registrado pelo banco |
| 351 | ✅ | [Cobranca] conta A na lista |
| 352 | ✅ | [Cobranca] a confirmação do cancelamento avisa que a conta tem boleto |
| 353 | ✅ | [Cobranca] conta A cancelada |
| 354 | ✅ | [Cobranca] boleto ainda não enviado da conta cancelada é cancelado junto |
| 355 | ✅ | [Cobranca] conta B na lista |
| 356 | ✅ | [Cobranca] a janela de baixa avisa que a conta tem boleto em aberto |
| 357 | ✅ | [Cobranca] conta B recebida no caixa |
| 358 | ✅ | [Cobranca] boleto registrado da conta B fica com baixa pedida |
| 359 | ✅ | [Cobranca] o usuário é avisado de que a baixa vai na próxima remessa |
| 360 | ✅ | [Cobranca] retorno que liquida conta já recebida no caixa avisa possível duplicidade |
| 361 | ✅ | [Cobranca] a baixa do caixa não é sobrescrita pelo retorno |
| 362 | ✅ | [Cobranca] rodapé mostra o total vencido a receber (R$ 2.543,62) |
| 363 | ✅ | [Cobranca] mudar as datas do período já atualiza a lista (sem clicar em Pesquisar) |
| 364 | ✅ | [Cobranca] boleto com vencimento passado é recusado com explicação |
| 365 | ✅ | [Cobranca] janela avisa das contas vencidas e pede o novo vencimento |
| 366 | ✅ | [Cobranca] boleto da conta vencida sai com o novo vencimento |
| 367 | ✅ | [Cobranca] fator de vencimento do código de barras usa o novo vencimento |
| 368 | ✅ | [Cobranca] nenhum erro ao emitir |
| 369 | ✅ | [Cobranca] a conta nova aparece para emitir boleto |
| 370 | ✅ | [Cobranca] boleto emitido para a conta escolhida (só ela) |
| 371 | ✅ | [Cobranca] código de barras com 44 dígitos |
| 372 | ✅ | [Cobranca] código começa com o banco e a moeda 9 |
| 373 | ✅ | [Cobranca] fator de vencimento 1608 (contagem reiniciada em 22/02/2025) |
| 374 | ✅ | [Cobranca] valor em centavos no código de barras |
| 375 | ✅ | [Cobranca] DV geral do código de barras (módulo 11) |
| 376 | ✅ | [Cobranca] linha digitável com 47 dígitos |
| 377 | ✅ | [Cobranca] DVs dos três campos da linha digitável (módulo 10) |
| 378 | ✅ | [Cobranca] a linha digitável remonta exatamente o código de barras |
| 379 | ✅ | [Cobranca] nosso número com 11 dígitos dentro do campo livre |
| 380 | ✅ | [Cobranca] boleto duplicado para a mesma conta é recusado |
| 381 | ✅ | [Cobranca] boleto de conta sem cliente é recusado (RN-COB-01) |
| 382 | ✅ | [Cobranca] boleto aparece na lista de ativos |
| 383 | ✅ | [Cobranca] ficha com linha digitável, valor e aviso de homologação |
| 384 | ✅ | [Cobranca] Visualizar / imprimir abre o boleto |
| 385 | ✅ | [Cobranca] nenhum erro na emissão pela tela |
| 386 | ✅ | [Cobranca] 3 boletos de teste emitidos |
| 387 | ✅ | [Cobranca] remessa de teste com os 3 boletos |
| 388 | ✅ | [Cobranca] simulação pela tela informa os 3 liquidados \| Retorno SIMULADO_REMESSA_2.ret: 6 registro(s), 3 entrada(s) confirmada(s), 0 rejeitada(s), 3 liquidado(s) (R$ 13,50), 0 não encontrado(s).

As contas a receber liquidadas foram baixadas no financeiro. |
| 389 | ✅ | [Cobranca] contas de teste baixadas pelo retorno simulado |
| 390 | ✅ | [Cobranca] registro da homologação mostra o retorno processado |
| 391 | ✅ | [Cobranca] remessa de produção recusa boletos de teste esquecidos |
| 392 | ✅ | [Cobranca] boletos de teste continuam só emitidos (nada foi enviado) |
| 393 | ✅ | [Cobranca] em produção a simulação de retorno é recusada (não baixa contas sem dinheiro) |
| 394 | ✅ | [Cobranca] conta liquidada aparece em Pagas |
| 395 | ✅ | [Cobranca] estorno de conta liquidada pelo banco pede confirmação com aviso |
| 396 | ✅ | [Cobranca] conta estornada volta para aberta |
| 397 | ✅ | [Cobranca] conta estornada pode receber boleto novo (o pago não conta como ativo) |
| 398 | ✅ | [Cobranca] boleto registrado na lista |
| 399 | ✅ | [Cobranca] boleto registrado: cancelar pede a baixa |
| 400 | ✅ | [Cobranca] remessa leva o pedido de baixa (movimento 02) |
| 401 | ✅ | [Cobranca] retorno do pedido de baixa deixa o boleto BAIXADO |
| 402 | ✅ | [Cobranca] baixa do boleto não mexe na conta a receber |
| 403 | ✅ | [Cobranca] pedido de baixa atendido |
| 404 | ✅ | [Cobranca] remessa de homologação com prefixo TESTE_ |
| 405 | ✅ | [Cobranca] remessa com o boleto novo e linhas de 240 posições |
| 406 | ✅ | [Cobranca] remessa passa no validador CNAB 240 |
| 407 | ✅ | [Cobranca] boleto fica ENVIADO após a remessa |
| 408 | ✅ | [Cobranca] remessa vazia é recusada com explicação |
| 409 | ✅ | [Cobranca] retorno confirma e liquida o boleto |
| 410 | ✅ | [Cobranca] liquidação baixa a conta a receber |
| 411 | ✅ | [Cobranca] boleto fica PAGO |
| 412 | ✅ | [Cobranca] o mesmo retorno importado de novo é recusado |
| 413 | ✅ | [Cobranca] retorno repetido não é registrado de novo |
| 414 | ✅ | [Cobranca] liquidação repetida em outro retorno não é contada de novo |
| 415 | ✅ | [Cobranca] retorno de outro banco é recusado dizendo os dois bancos |
| 416 | ✅ | [Cobranca] validador aponta a linha com tamanho errado |
| 417 | ✅ | [Cobranca] venda a prazo em 2 parcelas |
| 418 | ✅ | [Cobranca] boleto não enviado da venda cancelada é cancelado |
| 419 | ✅ | [Cobranca] boleto registrado da venda cancelada fica com pedido de baixa para a próxima remessa |
| 420 | ✅ | [Compras] recebimento deu entrada de 5 |
| 421 | ✅ | [Compras] confirmação avisa que o produto ficará com estoque negativo (2 − 5 = −3) |
| 422 | ✅ | [Compras] confirmação avisa da parcela já paga |
| 423 | ✅ | [Compras] respondendo Não, a compra continua |
| 424 | ✅ | [Compras] respondendo Sim, a compra é cancelada |
| 425 | ✅ | [Compras] estoque fica negativo (−3) porque 3 já foram vendidos (RN-EST-04) |
| 426 | ✅ | [Compras] contas da compra canceladas |
| 427 | ✅ | [Compras] pedido de compra volta para ABERTO |
| 428 | ✅ | [Compras] movimentação SAIDA de 5 com saldo −3 |
| 429 | ✅ | [Compras] cancelar de novo avisa que já está cancelada |
| 430 | ✅ | [Compras] salvar avisa que o desconto do item é maior que o valor do item |
| 431 | ✅ | [Compras] pedido com desconto inválido não é gravado |
| 432 | ✅ | [Compras] Pedidos.Salvar recusa item com desconto maior que o valor |
| 433 | ✅ | [Compras] pedido salvo com total 22,00 |
| 434 | ✅ | [Compras] pedido aparece em aberto na lista |
| 435 | ✅ | [Compras] edição grava quantidade 7 e remove o 2º item |
| 436 | ✅ | [Compras] total recalculado 7 × 5,00 = 35,00 |
| 437 | ✅ | [Compras] pedido cancelado |
| 438 | ✅ | [Compras] pedido cancelado não pode ser recebido |
| 439 | ✅ | [Compras] pedido cancelado abre só para consulta |
| 440 | ✅ | [Compras] pergunta se o item sem custo é bonificação |
| 441 | ✅ | [Compras] respondendo Não, nada é recebido |
| 442 | ✅ | [Compras] custo médio e estoque intactos |
| 443 | ✅ | [Compras] pedido fica salvo em aberto para corrigir o custo |
| 444 | ✅ | [Compras] pedido recebido com a NF 4567 |
| 445 | ✅ | [Compras] fornecedor localizado pelo CNPJ com pontuação |
| 446 | ✅ | [Compras] "Lançar compra" vem desmarcado porque a NF já entrou pelo pedido |
| 447 | ✅ | [Compras] a conferência explica o motivo |
| 448 | ✅ | [Compras] nota registrada |
| 449 | ✅ | [Compras] nenhuma compra nova (sem entrada em dobro) |
| 450 | ✅ | [Compras] estoque não dobrou |
| 451 | ✅ | [Compras] receber com NF repetida pede confirmação |
| 452 | ✅ | [Compras] respondendo Não, nada é lançado |
| 453 | ✅ | [Compras] o pedido continua em aberto para receber depois |
| 454 | ✅ | [Compras] o usuário é avisado de que o pedido ficou salvo em aberto |
| 455 | ✅ | [Compras] pedido recebido com total R$ 31,00 (3 × 10 + frete 1) |
| 456 | ✅ | [Compras] compra gravada com o total do pedido (gravou 31) |
| 457 | ✅ | [Compras] 2 parcelas a pagar somando R$ 31,00 (soma 31,0) |
| 458 | ✅ | [Compras] nº da nota fiscal gravado na compra |
| 459 | ✅ | [Compras] estoque do item 1 aumentou 1 |
| 460 | ✅ | [Compras] custo médio do item 1 com frete rateado (23,1158 ≈ 23,1158) |
| 461 | ✅ | [Compras] estoque do item 2 aumentou 1 |
| 462 | ✅ | [Compras] custo médio do item 2 com frete rateado (6,4201 ≈ 6,4201) |
| 463 | ✅ | [Compras] estoque do item 3 aumentou 1 |
| 464 | ✅ | [Compras] custo médio do item 3 com frete rateado (3,2063 ≈ 3,2063) |
| 465 | ✅ | [Compras] nenhum erro |
| 466 | ✅ | [Compras] lixo.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 467 | ✅ | [Compras] vazio.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 468 | ✅ | [Compras] outro.xml: mensagem clara em português (O arquivo não é um XML de NF-e (infNFe não encontrado). Confira se escolheu o XML da nota, e não outro arquivo.) |
| 469 | ✅ | [Compras] truncado.xml: mensagem clara em português (O arquivo não é um XML válido de NF-e (está vazio, incompleto ou é de outro tipo). Baixe novamente o XML enviado pelo fornecedor e tente de novo.) |
| 470 | ✅ | [Compras] não sugere produto inativo pelo código de barras |
| 471 | ✅ | [Compras] não sugere produto inativo pelo vínculo memorizado |
| 472 | ✅ | [Compras] conferência abre sem erro de grade |
| 473 | ✅ | [Compras] importação concluída cadastrando produtos novos |
| 474 | ✅ | [Compras] vínculo com o produto inativo é substituído pelo produto novo |
| 475 | ✅ | [Compras] 1ª importação concluída |
| 476 | ✅ | [Compras] com fator 1 entram só 2 unidades |
| 477 | ✅ | [Compras] item sem vínculo vira produto novo |
| 478 | ✅ | [Compras] duplicatas (2 × 218) lançadas a pagar |
| 479 | ✅ | [Compras] compra importada aparece em Compras realizadas |
| 480 | ✅ | [Compras] compra cancelada |
| 481 | ✅ | [Compras] estoque volta ao saldo anterior |
| 482 | ✅ | [Compras] contas a pagar da compra canceladas |
| 483 | ✅ | [Compras] nota cuja compra foi cancelada pode ser importada de novo |
| 484 | ✅ | [Compras] vínculos da 1ª importação foram memorizados |
| 485 | ✅ | [Compras] 2ª importação concluída |
| 486 | ✅ | [Compras] com fator 10 entram 20 unidades |
| 487 | ✅ | [Compras] reimportação não duplica o produto novo |
| 488 | ✅ | [Compras] só uma nota ativa com a chave |
| 489 | ✅ | [Compras] nota aponta para a nova compra |
| 490 | ✅ | [Compras] depois de reimportada, a nota volta a constar como importada |
| 491 | ✅ | [Estoque] editor abre com os 2 produtos abaixo do mínimo (2) |
| 492 | ✅ | [Estoque] sugestão 10 − 2 = 8 pelo custo médio 4,50 |
| 493 | ✅ | [Estoque] saldo negativo: sugestão 3 − (−1) = 4 |
| 494 | ✅ | [Estoque] pedido de compra gravado |
| 495 | ✅ | [Estoque] pedido de compra ABERTO do fornecedor escolhido |
| 496 | ✅ | [Estoque] total do pedido = 8 × 4,50 + 4 × 7,00 |
| 497 | ✅ | [Estoque] usuário é informado do número do pedido |
| 498 | ✅ | [Estoque] gerar pedido não mexe no estoque (RN-PED-05) |
| 499 | ✅ | [Estoque] sem produtos abaixo do mínimo, avisa e não abre pedido |
| 500 | ✅ | [Estoque] filtro pelo nome mostra o produto |
| 501 | ✅ | [Estoque] resumo acompanha a pesquisa (1 produto(s) · Valor do estoque (custo): R$ 694,27; 1 na grade) |
| 502 | ✅ | [Estoque] produto selecionado na grade |
| 503 | ✅ | [Estoque] venda baixou 2 (saldo real 28) |
| 504 | ✅ | [Estoque] definir saldo grava exatamente o contado (saldo 25) |
| 505 | ✅ | [Estoque] movimentação AJUSTE de 3 com saldo 25 |
| 506 | ✅ | [Estoque] motivo registrado na origem |
| 507 | ✅ | [Estoque] histórico do produto mostra o ajuste logo após aplicar |
| 508 | ✅ | [Estoque] saldo igual ao atual não gera movimentação |
| 509 | ✅ | [Estoque] avisa que nada foi alterado |
| 510 | ✅ | [Estoque] ajuste sem motivo é recusado |
| 511 | ✅ | [Estoque] nada mudou sem motivo |
| 512 | ✅ | [Estoque] definir saldo 0 zera o estoque |
| 513 | ✅ | [Estoque] pergunta antes de deixar o saldo negativo |
| 514 | ✅ | [Estoque] respondendo Não, o saldo não muda |
| 515 | ✅ | [Estoque] saída de 10 deixa saldo 2 |
| 516 | ✅ | [Estoque] somente baixo lista o produto: BAIXO, sugestão de compra 3 |
| 517 | ✅ | [Estoque] somente baixo não lista produtos OK |
| 518 | ✅ | [Financeiro] baixa em lote com uma conta só orienta a usar "Baixar" |
| 519 | ✅ | [Financeiro] a janela mostra quantas contas e o total |
| 520 | ✅ | [Financeiro] as duas contas baixadas, cada uma pelo próprio valor, com a forma escolhida |
| 521 | ✅ | [Financeiro] lote com uma conta já paga: nada é baixado (tudo ou nada) e a mensagem diz qual conta |
| 522 | ✅ | [Financeiro] lote misturando contas a receber e a pagar é recusado também na camada de dados |
| 523 | ✅ | [Financeiro] contato registrado pela tela: meio, conversa e data da promessa |
| 524 | ✅ | [Financeiro] a lista mostra o último contato e a promessa |
| 525 | ✅ | [Financeiro] promessa com data passada: recusado |
| 526 | ✅ | [Financeiro] contato numa conta já paga: recusado |
| 527 | ✅ | [Financeiro] contato sem dizer o que foi conversado: recusado |
| 528 | ✅ | [Financeiro] filtro "Promessa vencida" mostra quem prometeu e não pagou |
| 529 | ✅ | [Financeiro] promessa não cumprida vira alerta na tela inicial |
| 530 | ✅ | [Financeiro] alerta de contas vencidas abre o Financeiro filtrado nas vencidas a receber |
| 531 | ✅ | [Financeiro] filtro Vencidas só mostra contas vencidas |
| 532 | ✅ | [Financeiro] conta vencida há 5 dias aparece com 5 dias de atraso |
| 533 | ✅ | [Financeiro] seleciona a conta vencida |
| 534 | ✅ | [Financeiro] baixa com data futura é recusada com explicação |
| 535 | ✅ | [Financeiro] baixa grava data, valor com juros e forma |
| 536 | ✅ | [Financeiro] baixar duas vezes a mesma conta é recusado |
| 537 | ✅ | [Financeiro] conta aparece em Pagas |
| 538 | ✅ | [Financeiro] estorno volta a conta para aberta e limpa o pagamento (data, valor e forma) |
| 539 | ✅ | [Financeiro] conta estornada volta a aparecer em aberto |
| 540 | ✅ | [Financeiro] cancelar conta aberta |
| 541 | ✅ | [Financeiro] conta cancelada não pode ser baixada |
| 542 | ✅ | [Financeiro] conta continua cancelada |
| 543 | ✅ | [Financeiro] conta na lista |
| 544 | ✅ | [Financeiro] valor 10× maior pede confirmação; "Não" mantém a janela aberta |
| 545 | ✅ | [Financeiro] baixa gravada com o valor corrigido |
| 546 | ✅ | [Financeiro] sem descrição a nova conta não é salva e o motivo é mostrado |
| 547 | ✅ | [Financeiro] 3 parcelas geradas (3) |
| 548 | ✅ | [Financeiro] valores 33,33 / 33,33 / 33,34 (centavos na última) |
| 549 | ✅ | [Financeiro] vencimentos mensais a partir do 1º |
| 550 | ✅ | [Financeiro] descrição "… parcela k/n" |
| 551 | ✅ | [Financeiro] parcelas a receber, em aberto e com o cliente escolhido |
| 552 | ✅ | [Financeiro] a lista é atualizada com as parcelas novas |
| 553 | ✅ | [Fiscal] trocar a UF do destinatário para RJ muda o CFOP para 6102 |
| 554 | ✅ | [Fiscal] CFOP de dentro do estado para cliente de outra UF é barrado na emissão |
| 555 | ✅ | [Fiscal] desconto maior que o valor do item é barrado na emissão |
| 556 | ✅ | [Fiscal] nota emitida para a venda |
| 557 | ✅ | [Fiscal] numeração definitiva e chave de 44 dígitos com DV |
| 558 | ✅ | [Fiscal] itens gravados com CFOP 6102 |
| 559 | ✅ | [Fiscal] venda com nota abre a nota existente sem permitir alterar |
| 560 | ✅ | [Fiscal] camada de dados recusa segunda nota para a mesma venda (RN-FIS-01) |
| 561 | ✅ | [Fiscal] nota emitida aparece na lista |
| 562 | ✅ | [Fiscal] usuário comum não cancela nota emitida |
| 563 | ✅ | [Fiscal] nota selecionada de novo |
| 564 | ✅ | [Fiscal] administrador cancela a nota |
| 565 | ✅ | [Fiscal] venda volta a aceitar nota depois do cancelamento |
| 566 | ✅ | [Fiscal] cancelar duas vezes é recusado na camada de dados |
| 567 | ✅ | [Fiscal] respondendo "Sim" o editor fecha descartando a nota |
| 568 | ✅ | [Fiscal] produto inativo não aparece na busca (F3) |
| 569 | ✅ | [Fiscal] F3 + nome + Enter adiciona o produto pelo custo com CFOP 1102 |
| 570 | ✅ | [Fiscal] fechar o editor com item não salvo pergunta antes e "Não" mantém a nota aberta |
| 571 | ✅ | [Fiscal] trocar o modelo usa a série configurada do modelo (65 → 7, 55 → 1) |
| 572 | ✅ | [Fiscal] Enter no código adiciona o item |
| 573 | ✅ | [Fiscal] rascunho gravado como NFC-e série 7 |
| 574 | ✅ | [Fiscal] CFOP de saída numa nota de entrada é acusado |
| 575 | ✅ | [Fiscal] NFC-e numa nota de entrada é acusada |
| 576 | ✅ | [Fiscal] CNPJ do remetente com dígito errado é acusado |
| 577 | ✅ | [Fiscal] desconto adicional maior que os produtos é acusado |
| 578 | ✅ | [Fiscal] CFOP interestadual com fornecedor do mesmo estado é acusado |
| 579 | ✅ | [Fiscal] nota corrigida não tem pendências:  |
| 580 | ✅ | [Fiscal] rascunho com pendências pode ser salvo |
| 581 | ✅ | [Fiscal] rascunho aparece no filtro Em digitação |
| 582 | ✅ | [Fiscal] emitir rascunho com CFOP errado mostra o motivo e não emite |
| 583 | ✅ | [Fiscal] rascunho selecionado |
| 584 | ✅ | [Fiscal] nota de entrada corrigida é emitida |
| 585 | ✅ | [Fiscal] nota de venda cancelada não é emitida: A venda nº 1 foi cancelada: não emita nota para ela (exclua este rascunho). |
| 586 | ✅ | [Fiscal] rascunho aparece em Em digitação |
| 587 | ✅ | [Fiscal] rascunho e itens excluídos |
| 588 | ✅ | [Gestão] faturamento dos últimos 30 dias confere com o banco (R$ 15.222,29) |
| 589 | ✅ | [Gestão] quantidade de vendas dos últimos 30 dias confere |
| 590 | ✅ | [Gestão] ticket médio = faturamento ÷ vendas |
| 591 | ✅ | [Gestão] período 'Hoje' abre sem erro |
| 592 | ✅ | [Gestão] período 'Últimos 7 dias' abre sem erro |
| 593 | ✅ | [Gestão] período 'Últimos 30 dias' abre sem erro |
| 594 | ✅ | [Gestão] período 'Este mês' abre sem erro |
| 595 | ✅ | [Gestão] período 'Mês anterior' abre sem erro |
| 596 | ✅ | [Gestão] período 'Últimos 90 dias' abre sem erro |
| 597 | ✅ | [Gestão] período 'Este ano' abre sem erro |
| 598 | ✅ | [Gestão] período 'Últimos 12 meses' abre sem erro |
| 599 | ✅ | [Gestão] relatório 'Vendas por dia' em 6 meses gera sem erro |
| 600 | ✅ | [Gestão] relatório 'Produtos mais vendidos' em 6 meses gera sem erro |
| 601 | ✅ | [Gestão] relatório 'Vendas por cliente' em 6 meses gera sem erro |
| 602 | ✅ | [Gestão] relatório 'Vendas por usuário (vendedor)' em 6 meses gera sem erro |
| 603 | ✅ | [Gestão] relatório 'Lucro bruto (resumo do período)' em 6 meses gera sem erro |
| 604 | ✅ | [Gestão] relatório 'Fluxo de caixa (realizado)' em 6 meses gera sem erro |
| 605 | ✅ | [Gestão] relatório 'Fluxo de caixa (previsto - contas em aberto)' em 6 meses gera sem erro |
| 606 | ✅ | [Gestão] relatório 'Produtos sem venda no período' em 6 meses gera sem erro |
| 607 | ✅ | [Gestão] relatório 'Compras por fornecedor' em 6 meses gera sem erro |
| 608 | ✅ | [Gestão] 'Produtos sem venda' não lista insumos de produção (0 insumo(s) na lista) |
| 609 | ✅ | [Gestão] 'Vendas por dia' soma 100.488,76 = banco 100.488,76 |
| 610 | ✅ | [Gestão] trocar o período já atualiza o relatório, sem precisar clicar em Gerar |
| 611 | ✅ | [Gestão] 'Vendas por dia' num período sem movimento: 0 linha(s), sem erro |
| 612 | ✅ | [Gestão] 'Produtos mais vendidos' num período sem movimento: 0 linha(s), sem erro |
| 613 | ✅ | [Gestão] 'Vendas por cliente' num período sem movimento: 0 linha(s), sem erro |
| 614 | ✅ | [Gestão] 'Vendas por usuário (vendedor)' num período sem movimento: 0 linha(s), sem erro |
| 615 | ✅ | [Gestão] 'Lucro bruto (resumo do período)' num período sem movimento: 1 linha(s), sem erro |
| 616 | ✅ | [Gestão] 'Fluxo de caixa (realizado)' num período sem movimento: 0 linha(s), sem erro |
| 617 | ✅ | [Gestão] 'Fluxo de caixa (previsto - contas em aberto)' num período sem movimento: 0 linha(s), sem erro |
| 618 | ✅ | [Gestão] 'Produtos sem venda no período' num período sem movimento: 40 linha(s), sem erro |
| 619 | ✅ | [Gestão] 'Compras por fornecedor' num período sem movimento: 0 linha(s), sem erro |
| 620 | ✅ | [Gestão] datas invertidas avisam que a data final é anterior à inicial |
| 621 | ✅ | [Gestão] sem dados, período 'Hoje' abre sem erro |
| 622 | ✅ | [Gestão] sem dados, 'Hoje' mostra faturamento zero e margem '-' |
| 623 | ✅ | [Gestão] sem dados, período 'Últimos 7 dias' abre sem erro |
| 624 | ✅ | [Gestão] sem dados, 'Últimos 7 dias' mostra faturamento zero e margem '-' |
| 625 | ✅ | [Gestão] sem dados, período 'Últimos 30 dias' abre sem erro |
| 626 | ✅ | [Gestão] sem dados, 'Últimos 30 dias' mostra faturamento zero e margem '-' |
| 627 | ✅ | [Gestão] sem dados, período 'Este mês' abre sem erro |
| 628 | ✅ | [Gestão] sem dados, 'Este mês' mostra faturamento zero e margem '-' |
| 629 | ✅ | [Gestão] sem dados, período 'Mês anterior' abre sem erro |
| 630 | ✅ | [Gestão] sem dados, 'Mês anterior' mostra faturamento zero e margem '-' |
| 631 | ✅ | [Gestão] sem dados, período 'Últimos 90 dias' abre sem erro |
| 632 | ✅ | [Gestão] sem dados, 'Últimos 90 dias' mostra faturamento zero e margem '-' |
| 633 | ✅ | [Gestão] sem dados, período 'Este ano' abre sem erro |
| 634 | ✅ | [Gestão] sem dados, 'Este ano' mostra faturamento zero e margem '-' |
| 635 | ✅ | [Gestão] sem dados, período 'Últimos 12 meses' abre sem erro |
| 636 | ✅ | [Gestão] sem dados, 'Últimos 12 meses' mostra faturamento zero e margem '-' |
| 637 | ✅ | [Gestão] sem dados, relatório 'Vendas por dia' gera sem erro |
| 638 | ✅ | [Gestão] sem dados, relatório 'Produtos mais vendidos' gera sem erro |
| 639 | ✅ | [Gestão] sem dados, relatório 'Vendas por cliente' gera sem erro |
| 640 | ✅ | [Gestão] sem dados, relatório 'Vendas por usuário (vendedor)' gera sem erro |
| 641 | ✅ | [Gestão] sem dados, relatório 'Lucro bruto (resumo do período)' gera sem erro |
| 642 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (realizado)' gera sem erro |
| 643 | ✅ | [Gestão] sem dados, relatório 'Fluxo de caixa (previsto - contas em aberto)' gera sem erro |
| 644 | ✅ | [Gestão] sem dados, relatório 'Produtos sem venda no período' gera sem erro |
| 645 | ✅ | [Gestão] sem dados, relatório 'Compras por fornecedor' gera sem erro |
| 646 | ✅ | [Interface] primeiro acesso com a senha padrão exige senha nova (recusa a padrão, senhas diferentes e curtas) |
| 647 | ✅ | [Interface] senha nova gravada (como hash) e a padrão deixa de valer |
| 648 | ✅ | [Interface] com a senha nova entra direto |
| 649 | ✅ | [Interface] o menu lateral destaca a tela em primeiro plano |
| 650 | ✅ | [Interface] o destaque do menu acompanha a troca de tela |
| 651 | ✅ | [Interface] a tela inicial mostra "Continuar de onde parou" com as telas usadas por último: Contas a pagar e receber, Estoque |
| 652 | ✅ | [Interface] o localizador (Ctrl+K) vazio mostra as telas recentes primeiro |
| 653 | ✅ | [Tabelas] todas as tabelas têm rolagem horizontal e colunas com largura mínima |
| 654 | ✅ | [Tabelas] todas as colunas de texto das tabelas podem ser ordenadas (exceto itens do PDV) |
| 655 | ✅ | [Tabelas] tela estreita: as colunas mantêm a largura mínima e aparece a barra de rolagem horizontal |
| 656 | ✅ | [Tabelas] clicar no título ordena de A a Z; clicar de novo inverte (com a seta no título) |
| 657 | ✅ | [Tabelas] a ordem escolhida continua depois de pesquisar e recarregar a lista |
| 658 | ✅ | [Tabelas] colunas de valor ordenam como número (2 antes de 10), não como texto |
| 659 | ✅ | [Tabelas] coluna de data ordena pela data de verdade (15/03 antes de 01/12), e de novo inverte |
| 660 | ✅ | [Tabelas] a ordem por data continua depois de recarregar, sem coluna auxiliar visível |
| 661 | ✅ | [Tabelas] lista de itens também ordena pelo título (texto e número) |
| 662 | ✅ | [Tabelas] o item selecionado continua selecionado depois de ordenar |
| 663 | ✅ | [Tabelas] no PDV a lista de itens não reordena (fica na ordem da venda) |
| 664 | ✅ | [Logística] fretes: 'Abrir / editar' sem seleção avisa para selecionar |
| 665 | ✅ | [Logística] fretes: 'Imprimir' sem seleção avisa para selecionar |
| 666 | ✅ | [Logística] fretes: 'Cancelar CIOT' sem seleção avisa para selecionar |
| 667 | ✅ | [Logística] OP: Imprimir sem seleção avisa para selecionar |
| 668 | ✅ | [Logística] placa com hífen é recusada com orientação |
| 669 | ✅ | [Logística] RENAVAM com letras é recusado |
| 670 | ✅ | [Logística] RNTRC curto é recusado |
| 671 | ✅ | [Logística] veículo válido é salvo |
| 672 | ✅ | [Logística] a mesma placa em minúsculas não é cadastrada de novo |
| 673 | ✅ | [Logística] frete com CIOT declarado só cancela depois do CIOT |
| 674 | ✅ | [Logística] cancelar CIOT exige motivo com 15 caracteres; com motivo válido cancela |
| 675 | ✅ | [Logística] viagem com CIOT cancelado não é liberada |
| 676 | ✅ | [Logística] cancelar o frete cancela as contas abertas dele |
| 677 | ✅ | [Logística] frete cancelado não gera contas a pagar |
| 678 | ✅ | [Logística] frete cancelado não declara CIOT |
| 679 | ✅ | [Logística] camada de dados exige motivo de 15 caracteres |
| 680 | ✅ | [Logística] transportadora (CNPJ) não exige CIOT |
| 681 | ✅ | [Logística] frete sem valor avisa que falta o valor |
| 682 | ✅ | [Logística] frete sem valor continua podendo gerar contas depois |
| 683 | ✅ | [Logística] frete de transportadora sai sem CIOT |
| 684 | ✅ | [Logística] editar o frete mantém o veículo mesmo depois de desativado |
| 685 | ✅ | [Logística] carga acima da capacidade pergunta; Não mantém o diálogo aberto |
| 686 | ✅ | [Logística] frete de frota própria salvo com o peso corrigido e sem CIOT |
| 687 | ✅ | [Logística] o editor avisa que o CIOT é obrigatório para autônomo: OBRIGATÓRIO (transportador autônomo): declare após salvar |
| 688 | ✅ | [Logística] frete salvo com CIOT pendente |
| 689 | ✅ | [Logística] sem CIOT declarado a viagem não é liberada (em trânsito) |
| 690 | ✅ | [Logística] sem CIOT declarado o frete não vai direto para entregue |
| 691 | ✅ | [Logística] CIOT declarado pelo simulador |
| 692 | ✅ | [Logística] contas a pagar: adiantamento 800 + saldo 1.300 (1.200 + 100 de pedágio) |
| 693 | ✅ | [Logística] as contas do frete são geradas uma única vez |
| 694 | ✅ | [Logística] depois de gerar as contas, valores do frete ficam travados no editor |
| 695 | ✅ | [Logística] com CIOT declarado, a rota (origem/destino declarados à ANTT) fica travada |
| 696 | ✅ | [Logística] com CIOT declarado a viagem sai (em trânsito) |
| 697 | ✅ | [Logística] frete entregue grava a data da entrega |
| 698 | ✅ | [Logística] CIOT encerrado após a entrega |
| 699 | ✅ | [Logística] frete entregue não é cancelado |
| 700 | ✅ | [Logística] Pamcard sem credenciais: orienta a configurar e nada muda |
| 701 | ✅ | [Logística] resposta 'sucesso' sem número de CIOT não marca o frete como declarado: PAMCARD não devolveu o número do CIOT. Confira no portal da administradora antes de declarar de novo. Recebido |
| 702 | ✅ | [Logística] Pamcard com resposta completa grava o CIOT e a administradora |
| 703 | ✅ | [Logística] cancelamento do CIOT pela Pamcard |
| 704 | ✅ | [Produção] a OP planejada aparece em aberto |
| 705 | ✅ | [Produção] Imprimir abre a visualização da OP |
| 706 | ✅ | [Produção] impressão da OP lista os insumos e a quantidade prevista |
| 707 | ✅ | [Produção] iniciar produção muda a situação |
| 708 | ✅ | [Produção] BLOQUEAR: produzir mais que o planejado sem insumo é barrado (cacau continua 3) |
| 709 | ✅ | [Produção] BLOQUEAR também vale direto na camada de dados |
| 710 | ✅ | [Produção] PERGUNTAR mostra a necessidade real (3,300) e, respondendo Não, nada é baixado |
| 711 | ✅ | [Produção] produzir menos que o planejado com insumo suficiente não pergunta nada |
| 712 | ✅ | [Produção] concluir 2 de 4: consome metade (3 kg de farinha, 0,55 kg de cacau) e entra 2 bolos |
| 713 | ✅ | [Produção] custo real: (6 + 5,50 + 4 × 2 ÷ 2) ÷ 2 = R$ 7,75 e vira o custo do bolo |
| 714 | ✅ | [Produção] a OP concluída aparece no filtro Todas |
| 715 | ✅ | [Produção] OP concluída não pode ser cancelada |
| 716 | ✅ | [Produção] segunda produção recalcula o custo médio ponderado (R$ 9,25) |
| 717 | ✅ | [Produção] cancelar OP planejada não mexe no estoque |
| 718 | ✅ | [Produção] o produto novo aparece na lista das fichas técnicas |
| 719 | ✅ | [Produção] insumo repetido soma na mesma linha (2 + 1 = 3 kg) |
| 720 | ✅ | [Produção] resumo mostra custo dos insumos (com perda), do lote, unitário e a margem: Insumos R$ 11,50 + adicional R$ 4,00 = lote R$ 15,50   ·   custo unitário R$ 7,75   ·   preço R$ 30,00   ·   margem 287,1% |
| 721 | ✅ | [Produção] o produto não entra como insumo da própria ficha |
| 722 | ✅ | [Produção] salvar a ficha grava os insumos e torna a mercadoria um produto acabado |
| 723 | ✅ | [Produção] OP criada pela ficha calcula a necessidade (6 kg de farinha; 0,5 × 2 × 1,1 = 1,1 kg de cacau) |
| 724 | ✅ | [Produção] ficha circular é recusada com o nome do insumo: O insumo "Torta teste" é fabricado (direta ou indiretamente) com este produto: a receita ficaria circular. Remova esse insumo ou corrija a ficha técnica dele. |
| 725 | ✅ | [Produção] a ficha anterior continua intacta |
| 726 | ✅ | [Produção] perda negativa é recusada com o nome do insumo |
| 727 | ✅ | [Produção] sem alteração, trocar de produto não pergunta nada |
| 728 | ✅ | [Produção] com insumo não salvo, trocar de produto pergunta; respondendo Não, a ficha em edição continua aberta |
| 729 | ✅ | [Produção] fechar a tela com ficha não salva pergunta; respondendo Não, a tela continua aberta |
| 730 | ✅ | [Produção] respondendo Sim, descarta e abre o outro produto (nada gravado) |
| 731 | ✅ | [Segurança] após 5 senhas erradas o usuário fica bloqueado (nem a senha certa entra) |
| 732 | ✅ | [Segurança] definir senha nova desbloqueia o usuário |
| 733 | ✅ | [Segurança] senhas com PBKDF2-SHA256 de 600.000 iterações |
| 734 | ✅ | [Segurança] hash corrompido nega o acesso (falha fechada) |
| 735 | ✅ | [Segurança] hash antigo (100.000 iterações) é refeito no padrão atual no próximo acesso |
| 736 | ✅ | [Segurança] senha: mínimo 8 caracteres e sem senhas óbvias |
| 737 | ✅ | [Segurança] chave de API gravada cifrada (DPAPI) e lida normalmente pelo sistema |
| 738 | ✅ | [Segurança] credencial cifrada em outro computador não é revelada (fica em branco) |
| 739 | ✅ | [Segurança] usuário comum é barrado na camada de dados (cancelar venda e nota, alterar configurações, gerar dados) |
| 740 | ✅ | [Segurança] exportação CSV neutraliza fórmulas (CSV injection) |
| 741 | ✅ | [Segurança] curingas digitados (% e _) são tratados como texto nas buscas |
| 742 | ✅ | [Segurança] SQL sempre parametrizado (texto malicioso é só texto) |
| 743 | ✅ | [Segurança] XML com DTD/entidade externa é recusado (XXE) |
| 744 | ✅ | [Segurança] caracteres de controle nos dados não viram comandos da impressora |
| 745 | ✅ | [Segurança] log íntegro passa na verificação |
| 746 | ✅ | [Segurança] alteração direta no arquivo do log é detectada |
| 747 | ✅ | [Segurança] verificação de integridade do banco (quick_check) |
| 748 | ✅ | [Ordens de serviço] prazo sugerido pelo parâmetro (dias) e campo de data só aceita datas plausíveis |
| 749 | ✅ | [Ordens de serviço] sem descrever o serviço a OS não é aberta |
| 750 | ✅ | [Ordens de serviço] OS aberta pela tela: etapa Aberta, valor e serviço (com várias linhas) gravados |
| 751 | ✅ | [Ordens de serviço] a OS nova aparece na lista, no prazo |
| 752 | ✅ | [Ordens de serviço] "Avançar etapa" passa para a próxima (Aberta → Aprovada → Em execução) |
| 753 | ✅ | [Ordens de serviço] voltar para uma etapa anterior exige o motivo |
| 754 | ✅ | [Ordens de serviço] histórico com cada mudança de etapa, inclusive o motivo da volta |
| 755 | ✅ | [Ordens de serviço] prazo de entrega antes da abertura é recusado |
| 756 | ✅ | [Ordens de serviço] datas implausíveis (ano 201, daqui a 30 anos) são recusadas |
| 757 | ✅ | [Ordens de serviço] OS atrasada: filtro "Atrasadas", situação ATRASADA e dias de atraso |
| 758 | ✅ | [Ordens de serviço] o filtro mostra só as atrasadas |
| 759 | ✅ | [Ordens de serviço] OS atrasada vira alerta grave da tela inicial |
| 760 | ✅ | [Ordens de serviço] a tela inicial mostra o alerta das OS atrasadas |
| 761 | ✅ | [Ordens de serviço] clicar no alerta abre as OS já filtradas nas atrasadas |
| 762 | ✅ | [Ordens de serviço] entregar grava a data da entrega; entregue depois do prazo fica marcada "com atraso" |
| 763 | ✅ | [Ordens de serviço] faturar gera as contas a receber da OS (3 parcelas somando o valor, para o cliente da OS) |
| 764 | ✅ | [Ordens de serviço] faturar de novo: recusado com explicação |
| 765 | ✅ | [Ordens de serviço] mudar o valor da OS faturada: recusado com explicação |
| 766 | ✅ | [Ordens de serviço] cancelar OS faturada com contas abertas: recusado com explicação |
| 767 | ✅ | [Ordens de serviço] ficha da OS para imprimir |
| 768 | ✅ | [Ordens de serviço] com todas as contas da OS canceladas, o valor pode ser corrigido e a OS faturada de novo |
| 769 | ✅ | [Ordens de serviço] cancelar OS exige o motivo |
| 770 | ✅ | [Ordens de serviço] OS cancelada com motivo |
| 771 | ✅ | [Ordens de serviço] indicador de entregas no prazo nos últimos 90 dias (33,3%) |
| 772 | ✅ | [Ordens de serviço] com dados de demonstração a tela inicial mostra vários alertas (OS, contas vencidas, estoque) |
| 773 | ✅ | [Sistema] Configurações do administrador tem uma aba por grupo + Minha conta |
| 774 | ✅ | [Sistema] salvar a aba 'Empresa' sem mudar nada não dá erro |
| 775 | ✅ | [Sistema] salvar a aba 'Fiscal' sem mudar nada não dá erro |
| 776 | ✅ | [Sistema] salvar a aba 'Reforma tributária' sem mudar nada não dá erro |
| 777 | ✅ | [Sistema] salvar a aba 'Vendas e compras' sem mudar nada não dá erro |
| 778 | ✅ | [Sistema] salvar a aba 'Produção' sem mudar nada não dá erro |
| 779 | ✅ | [Sistema] salvar a aba 'Transporte e CIOT' sem mudar nada não dá erro |
| 780 | ✅ | [Sistema] salvar a aba 'TEF' sem mudar nada não dá erro |
| 781 | ✅ | [Sistema] salvar a aba 'Impressoras' sem mudar nada não dá erro |
| 782 | ✅ | [Sistema] salvar a aba 'E-mail' sem mudar nada não dá erro |
| 783 | ✅ | [Sistema] salvar a aba 'Assistente (IA)' sem mudar nada não dá erro |
| 784 | ✅ | [Sistema] salvar a aba 'Banco de dados' sem mudar nada não dá erro |
| 785 | ✅ | [Sistema] próximo número da NF-e 'abc' é recusado |
| 786 | ✅ | [Sistema] próximo número da NF-e '-1' é recusado |
| 787 | ✅ | [Sistema] próximo número da NF-e '0' é recusado |
| 788 | ✅ | [Sistema] próximo número da NF-e '1,5' é recusado |
| 789 | ✅ | [Sistema] dias de log não numérico é recusado |
| 790 | ✅ | [Sistema] dias de log = 0 (manter tudo) é aceito |
| 791 | ✅ | [Sistema] alíquota de 150% é recusada |
| 792 | ✅ | [Sistema] nome da loja em branco é recusado |
| 793 | ✅ | [Sistema] nome da loja válido é gravado |
| 794 | ✅ | [Sistema] 'Pamcard: senha / token' fica mascarado |
| 795 | ✅ | [Sistema] 'Chave da API do Resend' fica mascarado |
| 796 | ✅ | [Sistema] 'Chave da API da Anthropic' fica mascarado |
| 797 | ✅ | [Sistema] página de teste em impressora inexistente avisa que ela não foi encontrada \| Falha na impressão:
Impressora "Impressora Que Nao Existe ERP" não encontrada. Verifique Configurações > Impressoras. |
| 798 | ✅ | [Sistema] e-mail de teste sem chave diz que o envio não está configurado \| O envio de e-mail não está configurado: preencha a chave da API do Resend e o remetente nesta aba e tente de novo. |
| 799 | ✅ | [Sistema] remetente sem e-mail é recusado antes de enviar |
| 800 | ✅ | [Sistema] e-mail de teste enviado ao e-mail do usuário |
| 801 | ✅ | [Sistema] Fazer backup agora cria o arquivo de backup: %USERPROFILE%\AppData\Local\Temp\erp_testes_7b5e92d822744f5c88b23b799cb02e7f\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261003_132711_manual.db \| Backup salvo em:
%USERPROFILE%\AppData\Local\Temp\erp_testes_7b5e92d822744f5c88b23b799cb02e7f\cenarios\SistemaAdminConfiguraLoja\backups\loja_20261003_132711_manual.db |
| 802 | ✅ | [Sistema] salvar o nome da loja atualiza na hora o cabeçalho da janela principal: ERP Loja - Mercadinho Novo - Configurações |
| 803 | ✅ | [Sistema] o log registra abrir e fechar Relatórios |
| 804 | ✅ | [Sistema] abrir uma tela registra uma linha só (não duplica): 1 |
| 805 | ✅ | [Sistema] salvar configurações registra a gravação com a tela Configurações |
| 806 | ✅ | [Sistema] a tela de log abre listando registros |
| 807 | ✅ | [Sistema] filtro por categoria TELA mostra só TELA |
| 808 | ✅ | [Sistema] a lista de usuários traz quem aparece no log |
| 809 | ✅ | [Sistema] busca por texto filtra (Enter) |
| 810 | ✅ | [Sistema] busca sem resultado deixa a grade vazia sem erro |
| 811 | ✅ | [Sistema] datas invertidas no log avisam o usuário |
| 812 | ✅ | [Sistema] a tela 'PDV - frente de caixa' tem tópico de ajuda |
| 813 | ✅ | [Sistema] a tela 'Orçamentos' tem tópico de ajuda |
| 814 | ✅ | [Sistema] a tela 'Pedidos de venda' tem tópico de ajuda |
| 815 | ✅ | [Sistema] a tela 'Ordens de serviço' tem tópico de ajuda |
| 816 | ✅ | [Sistema] a tela 'Vendas realizadas' tem tópico de ajuda |
| 817 | ✅ | [Sistema] a tela 'Pedidos de compra' tem tópico de ajuda |
| 818 | ✅ | [Sistema] a tela 'Importar XML de NF-e' tem tópico de ajuda |
| 819 | ✅ | [Sistema] a tela 'Compras realizadas' tem tópico de ajuda |
| 820 | ✅ | [Sistema] a tela 'Fichas técnicas' tem tópico de ajuda |
| 821 | ✅ | [Sistema] a tela 'Ordens de produção' tem tópico de ajuda |
| 822 | ✅ | [Sistema] a tela 'Fretes e CIOT' tem tópico de ajuda |
| 823 | ✅ | [Sistema] a tela 'Veículos' tem tópico de ajuda |
| 824 | ✅ | [Sistema] a tela 'Estoque' tem tópico de ajuda |
| 825 | ✅ | [Sistema] a tela 'Contas a pagar e receber' tem tópico de ajuda |
| 826 | ✅ | [Sistema] a tela 'Cobrança bancária (boletos)' tem tópico de ajuda |
| 827 | ✅ | [Sistema] a tela 'Contas bancárias' tem tópico de ajuda |
| 828 | ✅ | [Sistema] a tela 'Notas fiscais' tem tópico de ajuda |
| 829 | ✅ | [Sistema] a tela 'Painel gerencial' tem tópico de ajuda |
| 830 | ✅ | [Sistema] a tela 'Relatórios' tem tópico de ajuda |
| 831 | ✅ | [Sistema] a tela 'Clientes e fornecedores' tem tópico de ajuda |
| 832 | ✅ | [Sistema] a tela 'Produtos' tem tópico de ajuda |
| 833 | ✅ | [Sistema] a tela 'Categorias' tem tópico de ajuda |
| 834 | ✅ | [Sistema] a tela 'Usuários' tem tópico de ajuda |
| 835 | ✅ | [Sistema] a tela 'Configurações' tem tópico de ajuda |
| 836 | ✅ | [Sistema] a tela 'Assistente de dúvidas (IA)' tem tópico de ajuda |
| 837 | ✅ | [Sistema] a tela 'Log de atividades' tem tópico de ajuda |
| 838 | ✅ | [Sistema] a tela 'Localizar tela' tem tópico de ajuda |
| 839 | ✅ | [Sistema] com uma tela aberta só aparece o '?' dela (visíveis: 1) |
| 840 | ✅ | [Sistema] o '?' fica no canto direito da tela embutida |
| 841 | ✅ | [Sistema] o '?' de Relatórios abre a ajuda de Relatórios |
| 842 | ✅ | [Sistema] a janela principal começa na tela inicial |
| 843 | ✅ | [Sistema] F5 abre o Painel gerencial à direita |
| 844 | ✅ | [Sistema] F8 abre Relatórios no lugar do painel (uma tela por vez, sem abas) |
| 845 | ✅ | [Sistema] F6 abre Estoque; o cabeçalho mostra a área e a tela: Estoque  ›  Estoque |
| 846 | ✅ | [Sistema] as opções da tela ficam numa lista vertical com barra de rolagem à direita |
| 847 | ✅ | [Sistema] a listagem fica no meio |
| 848 | ✅ | [Sistema] abrir de novo a tela que está aberta não cria outra |
| 849 | ✅ | [Sistema] Ctrl+W fecha a tela e volta ao início |
| 850 | ✅ | [Sistema] Ctrl+W sem tela aberta não faz nada |
| 851 | ✅ | [Sistema] o cabeçalho tem o × para fechar a tela |
| 852 | ✅ | [Sistema] o × fecha a tela |
| 853 | ✅ | [Sistema] digitar num campo marca a tela com alterações não salvas |
| 854 | ✅ | [Sistema] trocar de tela com dados não salvos pergunta; "Continuar editando" mantém tudo [ativa=CrudForm, texto=Bebidas, mensagens=CONFIRMAR:Você preencheu ou alterou dados em "Categorias" que ainda nã] |
| 855 | ✅ | [Sistema] "Descartar e sair" desfaz o que foi digitado e abre a outra tela |
| 856 | ✅ | [Sistema] o descarte fica registrado no log |
| 857 | ✅ | [Sistema] digitar na pesquisa não conta como alteração |
| 858 | ✅ | [Sistema] nenhuma mensagem de erro |
| 859 | ✅ | [Sistema] no PDV embutido F5 abre o desconto, não o painel |
| 860 | ✅ | [Sistema] com venda em andamento, abrir outra tela pergunta antes; respondendo Não o PDV continua com a venda |
| 861 | ✅ | [Sistema] responder Não à venda em andamento mantém a janela principal aberta |
| 862 | ✅ | [Sistema] o PDV continua na tela para o usuário concluir a venda |
| 863 | ✅ | [Sistema] respondendo Sim a janela principal fecha |
| 864 | ✅ | [Sistema] buscar 'vender' traz 'PDV - frente de caixa' entre os 3 primeiros (posição 1 de 19: PDV - frente de caixa, Painel gerencial, Vendas realizadas) |
| 865 | ✅ | [Sistema] buscar 'como cadastrar cliente' traz 'Clientes e fornecedores' entre os 3 primeiros (posição 1 de 17: Clientes e fornecedores, Contas a pagar e receber, Orçamentos) |
| 866 | ✅ | [Sistema] buscar 'cadastrar produto' traz 'Produtos' entre os 3 primeiros (posição 2 de 19: Importar XML de NF-e, Produtos, Categorias) |
| 867 | ✅ | [Sistema] buscar 'onde fica o estoque' traz 'Estoque' entre os 3 primeiros (posição 1 de 14: Estoque, Compras realizadas, Configurações) |
| 868 | ✅ | [Sistema] buscar 'trocar minha senha' traz 'Configurações' entre os 3 primeiros (posição 1 de 7: Configurações, Assistente de dúvidas (IA), Log de atividades) |
| 869 | ✅ | [Sistema] buscar 'fazer backup' traz 'Configurações' entre os 3 primeiros (posição 1 de 2: Configurações, Log de atividades) |
| 870 | ✅ | [Sistema] buscar 'emitir nota' traz 'Notas fiscais' entre os 3 primeiros (posição 1 de 12: Notas fiscais, Pedidos de venda, Vendas realizadas) |
| 871 | ✅ | [Sistema] buscar 'pagar fornecedor' traz 'Contas a pagar e receber' entre os 3 primeiros (posição 1 de 16: Contas a pagar e receber, Importar XML de NF-e, Pedidos de compra) |
| 872 | ✅ | [Sistema] buscar 'cancelar uma venda' traz 'Vendas realizadas' entre os 3 primeiros (posição 1 de 23: Vendas realizadas, Pedidos de venda, PDV - frente de caixa) |
| 873 | ✅ | [Sistema] buscar 'imprimir boleto' traz 'Cobrança bancária (boletos)' entre os 3 primeiros (posição 1 de 9: Cobrança bancária (boletos), Configurações, Vendas realizadas) |
| 874 | ✅ | [Sistema] buscar 'fabricar' traz 'Ordens de produção' entre os 3 primeiros (posição 1 de 3: Ordens de produção, Fichas técnicas, Produtos) |
| 875 | ✅ | [Sistema] buscar só uma palavra comum ('de') ainda devolve resultados |
| 876 | ✅ | [Sistema] e-mail incompleto é recusado |
| 877 | ✅ | [Sistema] e-mail de outro usuário (mesmo com maiúsculas) é recusado |
| 878 | ✅ | [Sistema] e-mail válido é gravado sem espaços |
| 879 | ✅ | [Sistema] senha atual errada é recusada |
| 880 | ✅ | [Sistema] confirmação diferente é recusada |
| 881 | ✅ | [Sistema] senha alterada e campos limpos |
| 882 | ✅ | [Sistema] a troca de senha fica registrada no log de acesso (categoria ACESSO) |
| 883 | ✅ | [Sistema] busca sem acento ('configuracoes') acha Configurações |
| 884 | ✅ | [Sistema] busca com acento e maiúsculas acha Relatórios |
| 885 | ✅ | [Sistema] busca por 'boleto' acha a cobrança |
| 886 | ✅ | [Sistema] palavra inexistente não acha nada |
| 887 | ✅ | [Sistema] busca só com espaços lista todas as telas |
| 888 | ✅ | [Sistema] buscar 'senha' não dá erro |
| 889 | ✅ | [Sistema] buscar 'pix' não dá erro |
| 890 | ✅ | [Sistema] buscar 'nota fiscal' não dá erro |
| 891 | ✅ | [Sistema] buscar 'ç' não dá erro |
| 892 | ✅ | [Sistema] buscar 'ã' não dá erro |
| 893 | ✅ | [Sistema] buscar 'a e i' não dá erro |
| 894 | ✅ | [Sistema] buscar '()' não dá erro |
| 895 | ✅ | [Sistema] buscar '%' não dá erro |
| 896 | ✅ | [Sistema] buscar ''' não dá erro |
| 897 | ✅ | [Sistema] o localizador lista as telas, sem listar a si mesmo |
| 898 | ✅ | [Sistema] o localizador lista resultados e seleciona o primeiro |
| 899 | ✅ | [Sistema] Ctrl+K, 'estoque minimo' e Enter abre a tela de Estoque |
| 900 | ✅ | [Sistema] sem resultado o localizador diz 'Nenhuma tela encontrada' |
| 901 | ✅ | [Sistema] Enter sem resultado não fecha o localizador |
| 902 | ✅ | [Sistema] Esc no localizador não abre nenhuma tela |
| 903 | ✅ | [Sistema] senha errada não entra |
| 904 | ✅ | [Sistema] senha errada mostra mensagem |
| 905 | ✅ | [Sistema] usuário inativo não entra |
| 906 | ✅ | [Sistema] username com maiúsculas e espaços entra como usuário comum |
| 907 | ✅ | [Sistema] o log registra acessos recusados e a entrada: Entrou no sistema, Acesso recusado (usuário inativo), Acesso recusado |
| 908 | ✅ | [Sistema] o log não guarda senhas digitadas |
| 909 | ✅ | [Sistema] modo escuro aplica cores escuras às telas abertas |
| 910 | ✅ | [Sistema] a escolha fica gravada para o usuário |
| 911 | ✅ | [Sistema] telas abertas depois já vêm no modo escuro |
| 912 | ✅ | [Sistema] voltar ao modo claro restaura as cores originais |
| 913 | ✅ | [Sistema] sem chave do Resend o sistema explica que o e-mail não está configurado |
| 914 | ✅ | [Sistema] usuário sem e-mail recebe orientação para falar com o administrador |
| 915 | ✅ | [Sistema] e-mail (com maiúsculas) recebe o código de 6 dígitos |
| 916 | ✅ | [Sistema] falha no envio mostra a mensagem do Resend |
| 917 | ✅ | [Sistema] código errado avisa quantas tentativas restam |
| 918 | ✅ | [Sistema] o código recebido redefine a senha mesmo depois de um reenvio que falhou |
| 919 | ✅ | [Sistema] a nova senha passa a valer |
| 920 | ✅ | [Sistema] menu do usuário comum não mostra 'Contas bancárias' |
| 921 | ✅ | [Sistema] menu do usuário comum não mostra 'Usuários' |
| 922 | ✅ | [Sistema] menu do usuário comum não mostra 'Log de atividades' |
| 923 | ✅ | [Sistema] menu do usuário comum mostra o PDV |
| 924 | ✅ | [Sistema] o localizador não oferece telas de administrador ao usuário comum |
| 925 | ✅ | [Sistema] usuário comum não consegue abrir o Log de atividades |
| 926 | ✅ | [Sistema] e recebe a explicação de que a tela é só de administradores |
| 927 | ✅ | [Sistema] Configurações do usuário comum tem só a aba Minha conta: Minha conta |
| 928 | ✅ | [Validação] CNPJ: aceita com ou sem pontuação; recusa dígito verificador errado, tamanho errado e letras |
| 929 | ✅ | [Validação] CPF e CNPJ recebem a máscara padrão |
| 930 | ✅ | [Validação] CPF/CNPJ: sequência repetida é recusada |
| 931 | ✅ | [Validação] NCM com 8 e CFOP com 4 dígitos (pontos removidos; a letra O no lugar de zero é recusada) |
| 932 | ✅ | [Validação] campos numéricos recusam texto; percentual só de 0 a 100 |
| 933 | ✅ | [Validação] e-mail no formato nome@dominio |
| 934 | ✅ | [Validação] placa Mercosul/antiga em maiúsculas, sem hífen |
| 935 | ✅ | [Validação] CEP e telefone com a quantidade certa de dígitos e máscara |
| 936 | ✅ | [Validação] código de barras confere o dígito verificador |
| 937 | ✅ | [Validação] endereço de integração só com https |
| 938 | ✅ | [Validação] texto livre tem tamanho máximo e não aceita caracteres de controle |
| 939 | ✅ | [Validação] no campo CNPJ, letra digitada é bloqueada; números e pontuação passam |
| 940 | ✅ | [Validação] texto colado com letras não é alterado em silêncio: fica marcado como inválido |
| 941 | ✅ | [Validação] ao sair do campo, o CNPJ recebe a máscara |
| 942 | ✅ | [Validação] CNPJ inválido fica marcado com a explicação |
| 943 | ✅ | [Validação] a tela não grava com campo inválido |
| 944 | ✅ | [Validação] campo numérico não aceita letras |
| 945 | ✅ | [Validação] cadastro com CNPJ inválido não é gravado |
| 946 | ✅ | [Validação] CNPJ válido é gravado no formato padrão |
| 947 | ✅ | [Validação] parâmetro com formato inválido é recusado também na camada de dados |
| 948 | ✅ | [Vendas] a prazo sem cliente é recusado pedindo o cliente |
| 949 | ✅ | [Vendas] a prazo com cliente dentro do limite grava a venda \| Para vender a prazo, identifique o cliente (F2 no PDV). |
| 950 | ✅ | [Vendas] a prazo em 2x gera 2 parcelas em aberto no nome do cliente |
| 951 | ✅ | [Vendas] as parcelas somam o total da venda |
| 952 | ✅ | [Vendas] usuário comum não vende acima do limite de crédito |
| 953 | ✅ | [Vendas] venda recusada por limite continua no PDV para outra forma de pagamento |
| 954 | ✅ | [Vendas] administrador é avisado do limite excedido; respondendo Não, a venda não sai |
| 955 | ✅ | [Vendas] administrador confirma e vende acima do limite de crédito \| Para vender a prazo, identifique o cliente (F2 no PDV). \| Limite de crédito do cliente excedido (disponível: R$ 30,23, venda: R$ 60,46). Escolha outra forma de pagamento ou peça a um administrador. |
| 956 | ✅ | [Vendas] 3*código + código somam 4 unidades na mesma linha |
| 957 | ✅ | [Vendas] F4 = 2 lança o próximo item com 2 unidades |
| 958 | ✅ | [Vendas] F6 remove o item selecionado |
| 959 | ✅ | [Vendas] quantidade absurda é recusada sem alterar a venda |
| 960 | ✅ | [Vendas] F10 e tecla 2 escolhem PIX (a venda foi gravada como PIX) \| Quantidade "99999999999999999999999999999999" inválida: use de 0,001 a 99.999 (ex.: 3*código). |
| 961 | ✅ | [Vendas] transação TEF aprovada fica CONFIRMADA com a venda |
| 962 | ✅ | [Vendas] PDV volta a ficar livre depois da venda |
| 963 | ✅ | [Vendas] F7 respondendo Não mantém a venda |
| 964 | ✅ | [Vendas] F7 respondendo Sim cancela a venda em andamento |
| 965 | ✅ | [Vendas] ao cancelar venda a prazo com parcela recebida, a confirmação avisa do valor já recebido |
| 966 | ✅ | [Vendas] respondendo Não, a venda continua finalizada |
| 967 | ✅ | [Vendas] comprovante de venda com frete mostra a linha do frete |
| 968 | ✅ | [Vendas] comprovante mostra o total com frete |
| 969 | ✅ | [Vendas] usuário comum: 30% de desconto é recusado |
| 970 | ✅ | [Vendas] a recusa explica o limite |
| 971 | ✅ | [Vendas] desconto recusado não altera o total |
| 972 | ✅ | [Vendas] usuário comum: desconto dentro do limite é aceito |
| 973 | ✅ | [Vendas] removendo item, o desconto que passou do limite é barrado antes do pagamento (F5 para ajustar) |
| 974 | ✅ | [Vendas] administrador pode dar 50% de desconto |
| 975 | ✅ | [Vendas] venda com 50% de desconto do administrador gravada com o total certo |
| 976 | ✅ | [Vendas] desconto do item maior que o item é recusado ao salvar |
| 977 | ✅ | [Vendas] corrigido o desconto, o orçamento é salvo \| O desconto do item "Arroz Tipo 1 5kg" (R$ 35,23) é maior que o valor dele (R$ 30,23). Corrija o desconto do item ou use o desconto geral. |
| 978 | ✅ | [Vendas] nenhum item de orçamento/pedido com total negativo |
| 979 | ✅ | [Vendas] orçamento com 50% de desconto é recusado para usuário comum |
| 980 | ✅ | [Vendas] orçamento com 5% de desconto é salvo \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. |
| 981 | ✅ | [Vendas] código lançado duas vezes soma na mesma linha do orçamento |
| 982 | ✅ | [Vendas] validade padrão do orçamento = hoje + validade_orcamento_dias |
| 983 | ✅ | [Vendas] orçamento não mexe no estoque |
| 984 | ✅ | [Vendas] orçamento aparece em aberto na lista |
| 985 | ✅ | [Vendas] aprovar gera o pedido de venda e marca o orçamento APROVADO |
| 986 | ✅ | [Vendas] aprovar de novo um orçamento já aprovado é recusado (não gera pedido duplicado) |
| 987 | ✅ | [Vendas] pedido gerado aparece em Pedidos de venda |
| 988 | ✅ | [Vendas] faturar gera a venda com o total do pedido \| Desconto de 50,0% acima do limite de 10,0% para usuários comuns. Peça a um administrador. \| Este documento está APROVADO. A operação só vale para documentos em aberto. |
| 989 | ✅ | [Vendas] faturar baixa o estoque |
| 990 | ✅ | [Vendas] venda faturada aparece em Vendas realizadas |
| 991 | ✅ | [Vendas] comprovante mostra o nº da venda e o cliente |
| 992 | ✅ | [Vendas] administrador cancela a venda |
| 993 | ✅ | [Vendas] cancelamento devolve o estoque |
| 994 | ✅ | [Vendas] cancelamento cancela as contas da venda |
| 995 | ✅ | [Vendas] cancelamento reabre o pedido de origem |
| 996 | ✅ | [Vendas] cancelar de novo avisa que já está cancelada |
| 997 | ✅ | [Vendas] usuário comum não fatura pedido a prazo acima do limite de crédito do cliente |
| 998 | ✅ | [Vendas] orçamento vencido não aparece entre os em aberto |
| 999 | ✅ | [Vendas] filtro Vencidos mostra o orçamento como VENCIDO |
| 1000 | ✅ | [Vendas] duplicar o orçamento vencido cria um novo em aberto com validade renovada |
| 1001 | ✅ | [Vendas] aprovar orçamento vencido pede confirmação; respondendo Não, nada muda |
| 1002 | ✅ | [Vendas] confirmando, o orçamento vencido é aprovado |
| 1003 | ✅ | [Vendas] faturar no boleto sem cliente é recusado |
| 1004 | ✅ | [Vendas] faturar no boleto com cliente gera a venda \| Para vender no boleto o pedido precisa ter um cliente. Abra o pedido e escolha o cliente. |
| 1005 | ✅ | [Vendas] venda no boleto fica em aberto no contas a receber (não entra como recebida) |
| 1006 | ✅ | [Vendas] produto sem preço de venda não é lançado no PDV e o motivo é explicado |
| 1007 | ✅ | [Vendas] PERGUNTAR: 3ª unidade (estoque 2) pergunta; respondendo Não, não lança |
| 1008 | ✅ | [Vendas] PERGUNTAR: respondendo Sim, lança |
| 1009 | ✅ | [Vendas] BLOQUEAR: não lança e explica que está bloqueado |
| 1010 | ✅ | [Vendas] PERMITIR: lança sem perguntar |
| 1011 | ✅ | [Vendas] TEF negado não grava a venda |
| 1012 | ✅ | [Vendas] TEF negado avisa o operador |
| 1013 | ✅ | [Vendas] TEF negado mantém os itens no PDV |
| 1014 | ✅ | [Vendas] transação negada registrada como NEGADA |
| 1015 | ✅ | [Vendas] TEF cancelado pelo operador não grava a venda |
| 1016 | ✅ | [Vendas] tecla 4 = crédito, aprovado no TEF, grava a venda \| Pagamento não aprovado: Transação negada pelo emissor (simulação). \| Pagamento não aprovado: Operação cancelada pelo operador. |
| 1017 | ✅ | [Vendas] crédito parcelado em 3x chega ao TEF |
| 1018 | ✅ | [Vendas] crédito no TEF gera a conta a receber já paga (a operadora recebe da adquirente) |
| 1019 | ✅ | [Vendas] cancelar a venda estorna a transação TEF e informa o estorno |
| 1020 | ✅ | [Vendas] administrador dá 100% de desconto |
| 1021 | ✅ | [Vendas] venda de total zero é gravada |
| 1022 | ✅ | [Vendas] venda de total zero não aciona o TEF (nenhuma transação de R$ 0,00) |
| 1023 | ✅ | [Vendas] venda de total zero não gera conta (RN-VEN-09) |
