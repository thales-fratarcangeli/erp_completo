# ADR-0022 — App de celular via API local no exe (proposta)

| | |
|---|---|
| **Status** | Proposta (não implementada) |
| **Data** | 2026-10-02 |

## Contexto
Pedido: pensar um app de celular, mantendo a entrega em "4 coisas: o exe, os 2 bancos e o app".

## Decisão proposta
O celular **não acessa os arquivos `.db`**. O `ErpLoja.exe` hospeda uma API HTTPS local (desligada por padrão) que
reutiliza as regras, validações, permissões e o log do sistema; o app (.NET MAUI) é pareado por QR code com
certificado fixado e aprovação do administrador. Acesso fora da loja por VPN, sem abrir portas no roteador.
Detalhes em [docs/17](../17-app-celular.md).

## Consequências
- Continua um banco por loja, aberto por um único processo (sem risco de corrupção por compartilhamento de rede).
- Exige separar a camada de dados das telas (`ErpLoja.Core`) antes de começar.
- O app depende do exe (ou do modo serviço) estar ligado; o app trabalha offline com fila idempotente.

## Alternativas consideradas
Banco em pasta compartilhada (corrompe o SQLite); banco na nuvem (muda toda a arquitetura e a LGPD); PWA servida
pelo exe (câmera/offline exigem certificado confiável pelo celular).
