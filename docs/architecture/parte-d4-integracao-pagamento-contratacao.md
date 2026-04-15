# Parte D.4 — Integração de pagamento na contratação (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.4) e [AGENTS.md](../../AGENTS.md): checkout autenticado com **PIX ou cartão**, reutilização de `SubscribeMemberCommand` (D.3), cobrança em `Payments`, `IPaymentProvider` (**Mock** ou **Stripe** conforme `Payments:Provider`) com retorno de instruções, confirmação via **callback legacy** e/ou **webhook Stripe assinado**, e ativação da associação.

## Decisões de produto / técnicas

- **Orquestração:** `TorcedorSubscriptionCheckoutService` (`Infrastructure`) chama `IMediator.Send(SubscribeMemberCommand)` para manter publicação de `MemberSubscribedEvent` e regras D.3; em seguida persiste `PaymentRecord` e chama o gateway.
- **Valor da cobrança:** preço do plano com desconto percentual (`MembershipPlans`), arredondamento com 2 casas (mesma lógica conceitual da simulação no frontend).
- **Instruções de pagamento:** `IPaymentProvider.CreatePixAsync` / `CreateCardAsync` retornam DTOs (`PixPaymentProviderResult`, `CardPaymentProviderResult`). O `MockPaymentProvider` gera payload PIX e URL de checkout determinísticos; o `StripePaymentProvider` cria **Stripe Checkout Session** (modo `payment`, cartão), grava `cs_…` em `PaymentRecord.ExternalReference` e devolve a URL hospedada em `card.checkoutUrl`. Com `Payments:Provider=Stripe`, **PIX não é suportado** nesta fase (`400` `payment_method_not_supported`).
- **Callback (legacy / testes):** `POST /api/subscriptions/payments/callback` é **anônimo** e exige `secret` igual a `Payments:WebhookSecret`. Idempotente: pagamento já `Paid` retorna `204`.
- **Webhook Stripe (produção cartão):** `POST /api/webhooks/stripe` — corpo bruto JSON, cabeçalho `Stripe-Signature`, segredo `Payments:Stripe:WebhookSecret` (`whsec_…`). Processa `checkout.session.completed` com `payment_status=paid`, valida valor/moeda e `metadata.payment_id`, chama `ITorcedorSubscriptionCheckoutPort.ConfirmPaymentAfterProviderSuccessAsync` (atualiza `ExternalReference` para `pi_…` quando aplicável). Idempotência por `event.id` na tabela `ProcessedStripeWebhookEvents`.
- **Ativação:** somente `Membership` em `PendingPayment` + cobrança `Pending`/`Overdue` → `Paid` e `Membership` → `Ativo`; histórico `StatusChanged` com ator nulo (sistema); `ILoyaltyPointsTriggerPort.AwardPointsForPaymentPaidAsync` é chamado após confirmação (paridade com conciliação admin).

## API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/subscriptions` | JWT | Corpo: `{ "planId", "paymentMethod": "Pix" \| "Card" }`. Resposta: `membershipId`, `paymentId`, `amount`, `currency`, `membershipStatus`, `pix` ou `card`. |
| POST | `/api/subscriptions/payments/callback` | Não | Corpo: `{ "paymentId", "secret" }`. Confirma pagamento e ativa sócio. `204` sucesso; `401` segredo inválido; `404` pagamento inexistente; `409` transição inválida. |
| POST | `/api/webhooks/stripe` | Não | Webhook Stripe (assinatura HMAC). `200` se processado ou ignorado; `400` payload/assinatura inválidos; `500` se `Payments:Stripe:WebhookSecret` não configurado. |

Erros de assinatura (D.3) no POST `/api/subscriptions`:

- `400` `plan_not_available` / `membership_status_prevents_subscribe` / `payment_method_not_supported` (ex.: PIX com provedor Stripe)
- `409` `already_active_subscription` / `subscription_pending_payment`

## CQRS (Application)

- `CreateTorcedorSubscriptionCheckoutCommand` + handler → `ITorcedorSubscriptionCheckoutPort.CreateCheckoutAsync`
- `ConfirmTorcedorSubscriptionPaymentCommand` + handler → `ITorcedorSubscriptionCheckoutPort.ConfirmPaymentAsync`
- `ConfirmPaymentAfterProviderSuccessAsync` — uso interno após gateway verificado (webhook Stripe); não substitui validação de assinatura no endpoint público.

## Configuração

```json
"Payments": {
  "Provider": "Mock",
  "WebhookSecret": "dev-change-in-production",
  "Stripe": {
    "ApiKey": "",
    "WebhookSecret": "",
    "SuccessUrl": "",
    "CancelUrl": ""
  }
}
```

- **`Payments:Provider`:** `Mock` (padrão local/testes) ou `Stripe`.
- **Stripe:** em produção preencher `ApiKey`, `WebhookSecret` do endpoint, e URLs HTTPS de retorno do Checkout (`SuccessUrl` / `CancelUrl`).
- **Webhook Stripe:** no Dashboard, apontar apenas para **`POST /api/webhooks/stripe`** na API (não usar paths como `/api/webhooks/stripe/member/...`). Ver [guia-configuracao-stripe.md](../deploy/guia-configuracao-stripe.md).
- **URLs da SPA:** `SuccessUrl` / `CancelUrl` devem usar as rotas do frontend (**`/subscription/confirmation`**, **`/plans`**), não **`/api/...`** no host do Vite (o prefixo `/api` é da API, não da SPA).

Ambiente de testes da API força `Payments:Provider=Mock` e define `Payments:WebhookSecret` via `AppWebApplicationFactory`, garantindo que cenários D.4/D.5/D.6/D.7 usem payloads de pagamento determinísticos (PIX mock e checkout mock) sem depender de chaves Stripe.

## Frontend (SPA)

- Rota: `/plans/:planId/checkout` — `SubscriptionCheckoutPage`
- Serviço: `subscriptionsService.subscribe(planId, paymentMethod)` em `frontend/src/features/plans/subscriptionsService.ts`
- Detalhe do plano: CTA **Contratar** navega para o checkout.

## Testes

- **API:** `PartD4SubscriptionsApiTests` — auth, PIX + callback + idempotência, cartão, conflito `PendingPayment`, segredo inválido; `StripeWebhookApiTests` — comportamento do endpoint quando segredo Stripe não está configurado.
- **Application:** `TorcedorSubscriptionCheckoutCommandHandlerTests` — delegação aos ports.
- **Infrastructure:** `StripePaymentProviderTests`, `StripeWebhookProcessorTests` — configuração e idempotência do processador de eventos.

## Próximos passos (D.5+)

- **D.5 entregue:** ver [parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md](parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md) (`GET /api/account/subscription`, `/subscription/confirmation`, resumo em Minha conta).
- **D.6 entregue:** ver [parte-d6-troca-plano-torcedor.md](parte-d6-troca-plano-torcedor.md) (`PUT /api/account/subscription/plan`, troca com proporcional em `/account`).
- Assinatura **recorrente** nativa Stripe Billing (faturas, `CreateSubscriptionAsync` de produto) e suporte a PIX via Stripe ou segundo provedor, conforme roadmap.
