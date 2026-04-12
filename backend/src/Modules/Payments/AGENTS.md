# Payments (Fase 4)

## Descrição
Cobrança em dois contextos: **SaaS** (clube paga a plataforma, banco master) e **sócio** (clube cobra o sócio, banco por tenant). O gateway de sócios usa **credenciais Stripe da conta do clube** (modo direto), armazenadas de forma protegida no master.

## Estrutura
- `SocioTorcedor.Modules.Payments.Domain` — assinaturas, faturas, inbox de webhooks SaaS e inbox de webhooks de sócios (`MemberStripeWebhookInbox` no master)
- `SocioTorcedor.Modules.Payments.Application` — comandos/queries (MediatR), repositórios abstratos
- `SocioTorcedor.Modules.Payments.Infrastructure` — `PaymentsMasterDbContext`, `TenantPaymentsDbContext`, EF, **`RoutingPaymentProvider`** (Stripe plataforma + operações na conta do tenant quando `StripeDirect` está configurado)
- `SocioTorcedor.Modules.Payments.Api` — controladores backoffice, admin do tenant, sócio e **webhooks Stripe**

## Rotas
- **Backoffice** (`X-Api-Key`): `api/backoffice/payments/saas/...` (assinatura SaaS, faturas)
- **Backoffice — gateway de sócios** (`X-Api-Key`): `api/backoffice/payments/member-gateway/tenants/{tenantId}/status`, `.../provider`
- **Admin do clube** (`X-Tenant-Id` + JWT + role `Administrador`): `GET/PUT api/payments/admin/member-gateway` — status e credenciais **Stripe direto**
- **Tenant** (`X-Tenant-Id` + JWT): `api/payments/member/...` — assinatura sócio, PIX, sessão Checkout quando o provedor Stripe direto está configurado
- **`GET api/payments/member/me/subscription`**: assinatura ativa (ou `null`); inclui **`memberPlanId`** e **`planName`**
- **Webhooks Stripe** (corpo bruto + assinatura `Stripe-Signature`; buffering do body habilitado no host para `/api/webhooks/*`):
  - **`POST /api/webhooks/stripe/saas`** — thin events, conta **da plataforma**. Segredos: `Payments:StripeThinSaasWebhookSecret` ou fallback `Payments:StripeSaasWebhookSecret`.
  - **`POST /api/webhooks/stripe/member/{tenantId}`** — thin events na **conta Stripe do clube**. O signing secret vem das **credenciais do gateway** desse tenant.
  - **`Payments:StripeWebhookShadowMode`:** quando `true`, validação + fetch thin ocorrem, mas os handlers **não** gravam inbox nem efeitos de domínio.
- SaaS processa efeitos apenas com payload no formato **`type` + `data.object`** (snapshot / thin reconstruído).

## Contrato de gateway
- `IPaymentProvider` em `BuildingBlocks.Application` (`Payments/`).
- **`RoutingPaymentProvider`** delega SaaS à conta da plataforma e operações de sócio a `MemberStripeOperationsResolver` quando o tenant tem gateway **Stripe direto** configurado; caso contrário lança **erro de configuração** (não há mais provider stub).
- **`StripePaymentProvider.CancelAsync`** (via `StripePaymentOperations`) só chama a API Stripe para IDs que começam com `sub_`; trata `resource_missing` / *No such subscription* como cancelamento idempotente.

## SaaS (master)
- Planos `SaaSPlan` podem referenciar **Price IDs** Stripe (mensal/anual) para `StartTenantSaasBilling`.
- Assinatura `TenantBillingSubscription` guarda `ExternalSubscriptionId`; webhooks SaaS atualizam estado e faturas.

## Gateway de sócios (Stripe direto)
- `TenantMemberGatewayConfiguration` no master: provedor, credenciais protegidas, status.
- Webhooks: `MemberStripeWebhookInbox` + `ProcessMemberStripeWebhookHandler` + `MemberStripeWebhookEffectApplicator`.

## Dependências
- Host: `AddPaymentsModule`, migrations após Backoffice no master; após Membership em cada tenant.

## Testes (TDD)
- Novas regras ou remoções de superfície: ajustar/criar testes em **Application** e **Infrastructure** antes ou em paralelo à alteração (`SubscribeMemberPlanHandlerTests`, `ProcessTenantSaasWebhookHandlerTests`, `StripePaymentProviderCancelTests`, webhooks).
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Application.Tests` — webhook SaaS (`ProcessTenantSaasWebhookHandler`), webhook sócios (`ProcessMemberStripeWebhookHandler`), `SubscribeMemberPlanHandler`, etc.
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Infrastructure.Tests` — `StripePaymentProviderCancelTests`, cancelamento e erros Stripe.

## Stripe Dashboard (Event Destinations)
- **SaaS:** `.../api/webhooks/stripe/saas` na conta da plataforma.
- **Sócios:** `.../api/webhooks/stripe/member/<tenantId>` na conta do clube (GUID do tenant no master).
