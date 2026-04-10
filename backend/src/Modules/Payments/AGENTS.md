# Payments (Fase 4)

## Descrição
Cobrança em dois contextos: **SaaS** (clube paga a plataforma, banco master) e **sócio** (clube cobra o sócio, banco por tenant).

## Estrutura
- `SocioTorcedor.Modules.Payments.Domain` — entidades de assinatura, fatura, inbox de webhook
- `SocioTorcedor.Modules.Payments.Application` — comandos/queries (MediatR), repositórios abstratos
- `SocioTorcedor.Modules.Payments.Infrastructure` — `PaymentsMasterDbContext`, `TenantPaymentsDbContext`, EF, `StubPaymentProvider`
- `SocioTorcedor.Modules.Payments.Api` — `BackofficeSaasPaymentsController`, `MemberPaymentsController`

## Rotas
- **Backoffice** (`X-Api-Key`): `api/backoffice/payments/saas/...`
- **Tenant** (`X-Tenant-Id` + JWT): `api/payments/member/...`
- Webhook sócio: `POST api/payments/member/webhooks` com header `X-Payments-Webhook-Secret` (`Payments:MemberWebhookSecret`)

## Contrato de gateway
- `IPaymentProvider` em `BuildingBlocks.Application` (`Payments/`); implementação atual: `StubPaymentProvider`.

## Dependências
- Host: `AddPaymentsModule`, migrations após Backoffice no master; após Membership em cada tenant (`DatabaseMigrationExtensions`, `TenantDatabaseProvisioner`).
