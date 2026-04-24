# Configuração ASAAS (pagamentos)

## Visão geral

Com `Payments:Provider=Asaas`, a API usa **link de pagamento** (`POST /v3/paymentLinks`) para cartão (incluindo parcelamento em planos anuais) e **cobrança PIX** (`POST /v3/payments`) quando o método for PIX.

- **Webhook:** `POST /api/webhooks/asaas` — o ASAAS envia o header `asaas-access-token` com o valor configurado em `Payments:Asaas:WebhookToken`.
- **Idempotência:** eventos processados ficam em `ProcessedWebhookEvents` (coluna `Provider = Asaas`).
- **PIX:** o torcedor precisa ter **CPF** cadastrado no perfil (`UserProfiles.Document`).

## Variáveis de ambiente

Ver comentários em [deploy/vps/api.env.example](../../deploy/vps/api.env.example).

| Chave | Descrição |
|-------|-----------|
| `Payments__Provider` | `Asaas` |
| `Payments__Asaas__ApiKey` | Access token da API (produção ou sandbox) |
| `Payments__Asaas__WebhookToken` | Token do webhook (mesmo valor configurado no painel ASAAS) |
| `Payments__Asaas__SuccessUrl` | URL de retorno após pagamento com sucesso (domínio deve estar liberado no ASAAS) |
| `Payments__Asaas__CancelUrl` | Referência para abandono (mensagens internas) |
| `Payments__Asaas__BaseUrl` | `https://api.asaas.com` ou `https://api-sandbox.asaas.com` |

## Migração de banco

Aplique as migrations após o deploy. A tabela `ProcessedStripeWebhookEvents` foi renomeada para **`ProcessedWebhookEvents`** com chave composta `(Provider, EventId)`.

## Documentação oficial

- [Webhooks](https://docs.asaas.com/docs/webhooks)
- [Parcelamentos](https://docs.asaas.com/docs/installment-payments)
- [Link de pagamentos](https://docs.asaas.com/docs/criando-um-link-de-pagamentos)
