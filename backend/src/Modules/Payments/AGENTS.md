# Payments (Fase 4)

## Descrição
Cobrança em dois contextos: **SaaS** (clube paga a plataforma, banco master) e **sócio** (clube cobra o sócio, banco por tenant).

## Estrutura
- `SocioTorcedor.Modules.Payments.Domain` — assinaturas, faturas, inbox de webhooks (SaaS e Connect)
- `SocioTorcedor.Modules.Payments.Application` — comandos/queries (MediatR), repositórios abstratos
- `SocioTorcedor.Modules.Payments.Infrastructure` — `PaymentsMasterDbContext`, `TenantPaymentsDbContext`, EF, `StubPaymentProvider` ou **`StripePaymentProvider`** quando `StripeSecretKey` está configurado
- `SocioTorcedor.Modules.Payments.Api` — controladores backoffice, sócio e **webhooks Stripe**

## Rotas
- **Backoffice** (`X-Api-Key`): `api/backoffice/payments/saas/...` (assinatura SaaS, faturas, webhook legado com API key onde aplicável)
- **Backoffice Stripe Connect** (`X-Api-Key`): `api/backoffice/payments/connect/...` — onboarding Express, status da conta (`tenants/{tenantId}/onboarding`, `tenants/{tenantId}/status`)
- **Tenant admin — Stripe Connect** (`X-Tenant-Id` + JWT + role `Administrador`): `api/payments/admin/connect/onboarding`, `api/payments/admin/connect/status` (cache no master), `api/payments/admin/connect/sync` (**pull** na API Stripe + `SyncFromStripe` + save). Onboarding compartilha comando com o backoffice; sync é exclusivo do fluxo tenant-admin.
- **Tenant** (`X-Tenant-Id` + JWT): `api/payments/member/...` — assinatura sócio, PIX, **sessão Checkout Stripe** quando o gateway Stripe está ativo
- **`GET api/payments/member/me/subscription`**: assinatura ativa (ou `null`); inclui **`memberPlanId`** e **`planName`**
- **Webhooks Stripe** (corpo bruto + assinatura `Stripe-Signature`; buffering do body habilitado no host para `/api/webhooks/*`):
  - **`POST /api/webhooks/stripe/saas`** — **thin events** (Stripe Event Destinations, `object: v2.core.event`). A API valida com `StripeClient.ParseEventNotification`, busca o evento V2 (`V2.Core.Events.Get`), monta payload no formato snapshot e reutiliza `ProcessTenantSaasWebhookCommand`. Idempotência: `snapshot_event` do evento V2 quando existente, senão o id da notificação thin.
  - **`POST /api/webhooks/stripe/connect`** — idem para Connect (contexto de conta conectada ao buscar recursos na API).
  - **Segredos:** `Payments:StripeThinSaasWebhookSecret` e `Payments:StripeThinConnectWebhookSecret`; se vazios, usam `StripeSaasWebhookSecret` / `StripeConnectWebhookSecret`.
  - **`Payments:StripeWebhookShadowMode`:** quando `true`, validação + fetch thin ainda ocorrem no controller, mas os handlers **não** gravam inbox nem efeitos de domínio (apenas logs no applicator) — útil para validação paralela em sandbox.
  - **Não** são mais aceitos webhooks no formato snapshot V1 (`ConstructEvent`) nessas rotas; configure no Dashboard **thin events** nos Event Destinations.
- Webhook sócio (gateway stub / legado): `POST api/payments/member/webhooks` com `X-Payments-Webhook-Secret` (`Payments:MemberWebhookSecret`)
- Webhook SaaS legado (JSON custom): conforme rotas backoffice documentadas em OpenAPI

## Contrato de gateway
- `IPaymentProvider` em `BuildingBlocks.Application` (`Payments/`).
- **Stripe ativo** quando `Payments:StripeSecretKey` está preenchido (`IPaymentsGatewayMetadata.IsStripeEnabled`); caso contrário permanece o provider **stub** onde aplicável.
- Com **Stripe** (SDK **Stripe.net 51**): chaves e segredos em `Payments` (`StripeSecretKey`, `StripePublishableKey`, segredos de webhook thin ou legados, `PublicAppBaseUrl`, `StripeWebhookShadowMode`, etc. — ver `PaymentsOptions`, `StripeWebhookHandlingOptions` e `appsettings`.
- **Compatibilidade com dados legados do stub:** o `StubPaymentProvider` grava `ExternalSubscriptionId` como `mem_sub_*` / `saas_sub_*`. Ao migrar o ambiente para Stripe real, esses IDs não existem na API. O **`StripePaymentProvider.CancelAsync`** ignora qualquer ID que **não** comece com `sub_` (não chama a Stripe) e trata erro Stripe **`resource_missing`** / mensagem *No such subscription* como cancelamento **idempotente** (no-op), permitindo troca de plano e reprocessamentos sem falhar.

## SaaS (master)
- Planos `SaaSPlan` podem referenciar **Price IDs** Stripe (mensal/anual) para `StartTenantSaasBilling`.
- Assinatura `TenantBillingSubscription` guarda `ExternalSubscriptionId`, opcionalmente `StripePriceId` e `CurrentPeriodEndUtc`; webhooks `invoice.paid`, `invoice.payment_failed`, `customer.subscription.*` atualizam estado e faturas abertas.

## Connect (sócio)
- `TenantStripeConnectAccount` no master; onboarding via API backoffice **ou** via `api/payments/admin/connect/*` (admin do clube); webhooks Connect atualizam conta e, com metadados `tenant_id` / `member_profile_id` / `member_plan_id`, o contexto do tenant (assinatura sócio, perfil).

## Dependências
- Host: `AddPaymentsModule`, migrations após Backoffice no master; após Membership em cada tenant (`DatabaseMigrationExtensions`, `TenantDatabaseProvisioner`).

## Testes
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Application.Tests` — webhook SaaS (`ProcessTenantSaasWebhookHandler`, idempotência / shadow), Connect (`ProcessStripeConnectWebhookHandler`), normalização thin (`StripeThinEventTypeNormalizer`), envelope (`StripeWebhookEnvelope`), billing do sócio (`GetMyMemberBillingHandler`), onboarding Connect (`StartStripeConnectOnboardingHandler`), status Connect (`GetStripeConnectStatusHandler`), sync Connect (`SyncStripeConnectStatusHandler`), troca de plano do sócio (`SubscribeMemberPlanHandler`).
  - **`SubscribeMemberPlanHandler`:** `SubscribeMemberPlanHandlerTests` cobre troca de plano com Stripe ativo e assinatura anterior com `ExternalSubscriptionId` no formato legado do stub (`mem_sub_*`): o handler chama `IPaymentProvider.CancelAsync`, marca a assinatura antiga como cancelada e persiste a nova (mock do gateway).
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Infrastructure.Tests` — `StripePaymentProviderCancelTests`: `CancelAsync` sem erro para id vazio, whitespace, `mem_sub_*`, `saas_sub_*` (sem chamada à API Stripe); `ShouldInvokeStripeSubscriptionCancel` para `sub_*`, ids legados e nulos; `IsMissingSubscriptionStripeError` para `resource_missing`, mensagem *No such subscription* e erro não relacionado.

## Stripe Dashboard (Event Destinations)
- Criar destinos com **Use thin events** e apontar para as URLs acima.
- Exemplos de tipos (prefixo `v1.`): SaaS — `v1.invoice.paid`, `v1.invoice.payment_failed`, `v1.customer.subscription.updated`, `v1.customer.subscription.deleted`; Connect — incluir `v1.account.updated`, `v1.checkout.session.completed` e os de fatura/assinatura alinhados ao código.
- Recursos V1 em thin events podem exigir **private preview** na Stripe; ver documentação atual da Stripe.
