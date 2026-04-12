# Payments (Fase 4)

## Descrição
Cobrança em dois contextos: **SaaS** (clube paga a plataforma, banco master) e **sócio** (clube cobra o sócio, banco por tenant). O gateway de sócios usa **credenciais Stripe da conta do clube** (modo direto), armazenadas de forma protegida no master — **não** há mais Stripe Connect neste projeto.

## Estrutura
- `SocioTorcedor.Modules.Payments.Domain` — assinaturas, faturas, inbox de webhooks SaaS e inbox de webhooks de sócios (`MemberStripeWebhookInbox` no master)
- `SocioTorcedor.Modules.Payments.Application` — comandos/queries (MediatR), repositórios abstratos
- `SocioTorcedor.Modules.Payments.Infrastructure` — `PaymentsMasterDbContext`, `TenantPaymentsDbContext`, EF, `StubPaymentProvider`, **`RoutingPaymentProvider`** (roteia para Stripe da conta do clube ou stub conforme configuração do tenant)
- `SocioTorcedor.Modules.Payments.Api` — controladores backoffice, admin do tenant, sócio e **webhooks Stripe**

## Rotas
- **Backoffice** (`X-Api-Key`): `api/backoffice/payments/saas/...` (assinatura SaaS, faturas, webhook legado com API key onde aplicável)
- **Backoffice — gateway de sócios** (`X-Api-Key`): `api/backoffice/payments/member-gateway/tenants/{tenantId}/status`, `.../provider` — operador define **qual provedor** o tenant usa (`None`, `StripeDirect`, etc.)
- **Admin do clube** (`X-Tenant-Id` + JWT + role `Administrador`): `GET/PUT api/payments/admin/member-gateway` — status e credenciais **Stripe direto** (`PUT .../stripe-direct`: secret key, publishable key, webhook signing secret)
- **Tenant** (`X-Tenant-Id` + JWT): `api/payments/member/...` — assinatura sócio, PIX, sessão Checkout quando o provedor Stripe direto está configurado
- **`GET api/payments/member/me/subscription`**: assinatura ativa (ou `null`); inclui **`memberPlanId`** e **`planName`**
- **Webhooks Stripe** (corpo bruto + assinatura `Stripe-Signature`; buffering do body habilitado no host para `/api/webhooks/*`):
  - **`POST /api/webhooks/stripe/saas`** — **thin events** (Stripe Event Destinations, `object: v2.core.event`). Conta **da plataforma**. Valida com `Payments:StripeThinSaasWebhookSecret` ou fallback `Payments:StripeSaasWebhookSecret`.
  - **`POST /api/webhooks/stripe/member/{tenantId}`** — **thin events** na **conta Stripe do clube** (mesmo `tenantId` GUID do master). O signing secret vem das **credenciais do gateway** desse tenant (não é variável global `Payments:*`).
  - **`Payments:StripeWebhookShadowMode`:** quando `true`, validação + fetch thin ainda ocorrem no controller, mas os handlers **não** gravam inbox nem efeitos de domínio (apenas logs no applicator) — útil para validação paralela em sandbox.
  - **Não** são mais aceitos webhooks no formato snapshot V1 (`ConstructEvent`) nessas rotas; configure no Dashboard **thin events** nos Event Destinations.
- Webhook interno stub / legado: `POST api/payments/member/webhooks` com `X-Payments-Webhook-Secret` (`Payments:MemberWebhookSecret`)

## Contrato de gateway
- `IPaymentProvider` em `BuildingBlocks.Application` (`Payments/`).
- **`RoutingPaymentProvider`** escolhe **stub** ou operações Stripe com chaves do tenant (`MemberStripeOperationsResolver` + credenciais no master).
- Com **Stripe.net 51**: chaves globais em `Payments` (`StripeSecretKey`, etc.) continuam usadas onde o código ainda referencia a conta da plataforma (SaaS); o fluxo de sócio usa as chaves **por tenant** quando `StripeDirect` está selecionado e credenciais salvas.
- **Compatibilidade com dados legados do stub:** o `StubPaymentProvider` grava `ExternalSubscriptionId` como `mem_sub_*` / `saas_sub_*`. O **`StripePaymentProvider.CancelAsync`** ignora qualquer ID que **não** comece com `sub_` e trata erro **`resource_missing`** / *No such subscription* como cancelamento **idempotente**.

## SaaS (master)
- Planos `SaaSPlan` podem referenciar **Price IDs** Stripe (mensal/anual) para `StartTenantSaasBilling`.
- Assinatura `TenantBillingSubscription` guarda `ExternalSubscriptionId`, opcionalmente `StripePriceId` e `CurrentPeriodEndUtc`; webhooks SaaS atualizam estado e faturas.

## Gateway de sócios (Stripe direto)
- `TenantMemberGatewayConfiguration` no master: provedor selecionado, credenciais protegidas (Stripe Direct), status.
- Webhooks de sócios: `MemberStripeWebhookInbox` no master + `ProcessMemberStripeWebhookHandler` + `MemberStripeWebhookEffectApplicator` (efeitos no banco do tenant quando aplicável).

## Dependências
- Host: `AddPaymentsModule`, migrations após Backoffice no master; após Membership em cada tenant (`DatabaseMigrationExtensions`, `TenantDatabaseProvisioner`).

## Testes
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Application.Tests` — webhook SaaS (`ProcessTenantSaasWebhookHandler`, idempotência / shadow), webhook sócios (`ProcessMemberStripeWebhookHandler`), normalização thin (`StripeThinEventTypeNormalizer`), envelope (`StripeWebhookEnvelope`), billing do sócio (`GetMyMemberBillingHandler`), troca de plano do sócio (`SubscribeMemberPlanHandler`).
  - **`SubscribeMemberPlanHandler`:** `SubscribeMemberPlanHandlerTests` cobre troca de plano com Stripe ativo e assinatura anterior com ID legado do stub (`mem_sub_*`).
- `backend/tests/Modules/Payments/SocioTorcedor.Modules.Payments.Infrastructure.Tests` — `StripePaymentProviderCancelTests`: `CancelAsync` sem erro para id vazio, whitespace, `mem_sub_*`, `saas_sub_*`; `ShouldInvokeStripeSubscriptionCancel` para `sub_*`; `IsMissingSubscriptionStripeError`.

## Stripe Dashboard (Event Destinations)
- Criar destinos com **Use thin events** e apontar para as URLs acima.
- **SaaS:** conta da plataforma → `.../api/webhooks/stripe/saas`.
- **Sócios:** na **conta Stripe do clube** → `.../api/webhooks/stripe/member/<tenantId>` (GUID do tenant no master).
- Exemplos de tipos (prefixo `v1.`): SaaS — `v1.invoice.paid`, `v1.invoice.payment_failed`, `v1.customer.subscription.updated`, `v1.customer.subscription.deleted`; para cobrança de sócios na conta do clube, alinhar aos eventos tratados em `MemberStripeWebhookEffectApplicator` / normalizador (checkout, fatura, assinatura).
