# Modules

## Descrição
Módulos verticais do monólito (clean architecture por módulo: Domain → Application → Infrastructure → Api).

## Estrutura
- `Tenancy/` — master DB, resolução de tenant pelo header `X-Tenant-Id` (slug), CRUD de tenant (comandos/queries usados pelo backoffice)
- `Identity/` — usuários, roles, permissões, JWT; documentos legais versionados e consentimentos LGPD (`LegalDocumentVersions`, `UserLegalConsents`); `GET /api/legal-documents/current`, `POST /api/legal-documents`; seed por tenant de roles **`Socio`** e **`Administrador`** (`RoleTenantSeed`, após migrate do Identity)
- `Membership/` — perfil de sócio (`api/members`) e planos de sócio (`api/plans`, `MemberPlan` + vantagens em JSON) no banco por tenant; `TenantMembershipDbContext`
- `Backoffice/` — API SaaS admin: tenants (HTTP), planos SaaS, tenant-plans; ver `Backoffice/AGENTS.md`
- `Payments/` — **Fase 4 (MVP)**: cobrança SaaS no master (`PaymentsMasterDbContext`, `api/backoffice/payments/saas/*`) e cobrança do sócio no banco do tenant (`TenantPaymentsDbContext`, `api/payments/member/*`); `IPaymentProvider` via `RoutingPaymentProvider` (Stripe plataforma + Stripe direto do tenant); webhooks thin com inbox idempotente

## Dependências
- Cada módulo depende dos **BuildingBlocks** e, no Host, é composto via extension methods (`*Module.cs`).
