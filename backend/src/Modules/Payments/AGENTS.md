# Payments (Fase 4)

## Descrição
Cobrança em dois contextos: **SaaS** (clube paga a plataforma, banco master) e **sócio** (clube cobra o sócio, banco por tenant).

## Estrutura
- `SocioTorcedor.Modules.Payments.Domain` — assinaturas, faturas, inbox de webhooks (SaaS e Connect)
- `SocioTorcedor.Modules.Payments.Application` — comandos/queries (MediatR), repositórios abstratos
- `SocioTorcedor.Modules.Payments.Infrastructure` — `PaymentsMasterDbContext`, `TenantPaymentsDbContext`, EF, `StubPaymentProvider` ou **`StripePaymentProvider`** (conforme `Payments:Gateway`)
- `SocioTorcedor.Modules.Payments.Api` — controladores backoffice, sócio e **webhooks Stripe**

## Rotas
- **Backoffice** (`X-Api-Key`): `api/backoffice/payments/saas/...` (assinatura SaaS, faturas, webhook legado com API key onde aplicável)
- **Backoffice Stripe Connect** (`X-Api-Key`): `api/backoffice/payments/stripe/connect/...` — onboarding Express, status da conta
- **Tenant** (`X-Tenant-Id` + JWT): `api/payments/member/...` — assinatura sócio, PIX, **sessão Checkout Stripe** quando o gateway Stripe está ativo
- **`GET api/payments/member/me/subscription`**: assinatura ativa (ou `null`); inclui **`memberPlanId`** e **`planName`**
- **Webhooks Stripe** (corpo bruto + assinatura `Stripe-Signature`; buffering do body habilitado no host para `/api/webhooks/*`):
  - **`POST /api/webhooks/stripe/saas`** — eventos da conta da plataforma (Billing SaaS dos tenants); idempotência por `event.id`
  - **`POST /api/webhooks/stripe/connect`** — Connect (conta conectada + assinaturas/faturas do sócio no tenant)
- Webhook sócio (gateway stub / legado): `POST api/payments/member/webhooks` com `X-Payments-Webhook-Secret` (`Payments:MemberWebhookSecret`)
- Webhook SaaS legado (JSON custom): conforme rotas backoffice documentadas em OpenAPI

## Contrato de gateway
- `IPaymentProvider` em `BuildingBlocks.Application` (`Payments/`).
- **`Payments:Gateway`** em configuração: `Stub` (padrão desenvolvimento) ou **`Stripe`**.
- Com **Stripe**: chaves e segredos em `Payments` (`StripeSecretKey`, `StripePublishableKey`, `StripeSaasWebhookSecret`, `StripeConnectWebhookSecret`, `PublicAppBaseUrl`, etc. — ver `PaymentsOptions` e `appsettings`.

## SaaS (master)
- Planos `SaaSPlan` podem referenciar **Price IDs** Stripe (mensal/anual) para `StartTenantSaasBilling`.
- Assinatura `TenantBillingSubscription` guarda `ExternalSubscriptionId`, opcionalmente `StripePriceId` e `CurrentPeriodEndUtc`; webhooks `invoice.paid`, `invoice.payment_failed`, `customer.subscription.*` atualizam estado e faturas abertas.

## Connect (sócio)
- `TenantStripeConnectAccount` no master; onboarding via API backoffice; webhooks Connect atualizam conta e, com metadados `tenant_id` / `member_profile_id` / `member_plan_id`, o contexto do tenant (assinatura sócio, perfil).

## Dependências
- Host: `AddPaymentsModule`, migrations após Backoffice no master; após Membership em cada tenant (`DatabaseMigrationExtensions`, `TenantDatabaseProvisioner`).

## Testes
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Application.Tests` — cenários de webhook SaaS (`ProcessTenantSaasWebhookHandler`) e billing do sócio (`GetMyMemberBillingHandler`).
