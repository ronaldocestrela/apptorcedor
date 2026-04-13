# Parte D.4 — Integração de pagamento na contratação (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.4) e [AGENTS.md](../../AGENTS.md): checkout autenticado com **PIX ou cartão**, reutilização de `SubscribeMemberCommand` (D.3), cobrança em `Payments`, `IPaymentProvider` (mock) com retorno de instruções, e **callback** para confirmar pagamento e ativar a associação.

## Decisões de produto / técnicas

- **Orquestração:** `TorcedorSubscriptionCheckoutService` (`Infrastructure`) chama `IMediator.Send(SubscribeMemberCommand)` para manter publicação de `MemberSubscribedEvent` e regras D.3; em seguida persiste `PaymentRecord` e chama o gateway.
- **Valor da cobrança:** preço do plano com desconto percentual (`MembershipPlans`), arredondamento com 2 casas (mesma lógica conceitual da simulação no frontend).
- **Instruções de pagamento:** `IPaymentProvider.CreatePixAsync` / `CreateCardAsync` retornam DTOs (`PixPaymentProviderResult`, `CardPaymentProviderResult`). O `MockPaymentProvider` gera payload PIX e URL de checkout determinísticos a partir do `paymentId`.
- **Callback:** `POST /api/subscriptions/payments/callback` é **anônimo** e exige `secret` igual a `Payments:WebhookSecret` (configuração). Idempotente: pagamento já `Paid` retorna `204`.
- **Ativação:** somente `Membership` em `PendingPayment` + cobrança `Pending`/`Overdue` → `Paid` e `Membership` → `Ativo`; histórico `StatusChanged` com ator nulo (sistema); `ILoyaltyPointsTriggerPort.AwardPointsForPaymentPaidAsync` é chamado após confirmação (paridade com conciliação admin).

## API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/subscriptions` | JWT | Corpo: `{ "planId", "paymentMethod": "Pix" \| "Card" }`. Resposta: `membershipId`, `paymentId`, `amount`, `currency`, `membershipStatus`, `pix` ou `card`. |
| POST | `/api/subscriptions/payments/callback` | Não | Corpo: `{ "paymentId", "secret" }`. Confirma pagamento e ativa sócio. `204` sucesso; `401` segredo inválido; `404` pagamento inexistente; `409` transição inválida. |

Erros de assinatura (D.3) no POST `/api/subscriptions`:

- `400` `plan_not_available` / `membership_status_prevents_subscribe`
- `409` `already_active_subscription` / `subscription_pending_payment`

## CQRS (Application)

- `CreateTorcedorSubscriptionCheckoutCommand` + handler → `ITorcedorSubscriptionCheckoutPort.CreateCheckoutAsync`
- `ConfirmTorcedorSubscriptionPaymentCommand` + handler → `ITorcedorSubscriptionCheckoutPort.ConfirmPaymentAsync`

## Configuração

```json
"Payments": {
  "WebhookSecret": "dev-change-in-production"
}
```

Ambiente de testes da API define `Payments:WebhookSecret` via `AppWebApplicationFactory`.

## Frontend (SPA)

- Rota: `/plans/:planId/checkout` — `SubscriptionCheckoutPage`
- Serviço: `subscriptionsService.subscribe(planId, paymentMethod)` em `frontend/src/features/plans/subscriptionsService.ts`
- Detalhe do plano: CTA **Contratar** navega para o checkout.

## Testes

- **API:** `PartD4SubscriptionsApiTests` — auth, PIX + callback + idempotência, cartão, conflito `PendingPayment`, segredo inválido.
- **Application:** `TorcedorSubscriptionCheckoutCommandHandlerTests` — delegação aos ports.

## Próximos passos (D.5+)

- **D.5 entregue:** ver [parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md](parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md) (`GET /api/account/subscription`, `/subscription/confirmation`, resumo em Minha conta).
- **D.6 entregue:** ver [parte-d6-troca-plano-torcedor.md](parte-d6-troca-plano-torcedor.md) (`PUT /api/account/subscription/plan`, troca com proporcional em `/account`).
- Gateway real (assinatura recorrente, assinatura + `CreateSubscriptionAsync` conforme produto).
