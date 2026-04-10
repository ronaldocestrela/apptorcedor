# Backoffice

## Descrição
Módulo **Fase 2**: API administrativa SaaS (gestão de tenants no master DB, planos SaaS, vínculo tenant–plano). Endpoints HTTP em `api/backoffice/*`, protegidos por **`X-Api-Key`** (`Backoffice:ApiKey`). Não usam **`X-Tenant-Id`**.

## Estrutura
- `SocioTorcedor.Modules.Backoffice.Domain` — `SaaSPlan`, `SaaSPlanFeature`, `TenantPlan`, enums
- `SocioTorcedor.Modules.Backoffice.Application` — CQRS, DTOs, contratos de repositório (referencia `Tenancy.Application` para `ITenantRepository` onde necessário)
- `SocioTorcedor.Modules.Backoffice.Infrastructure` — `BackofficeMasterDbContext`, migrations (histórico `__EFBackofficeMigrationsHistory` na mesma base `MasterDb`), repositórios
- `SocioTorcedor.Modules.Backoffice.Api` — `BackofficeModule`, controllers `Tenants`, `SaaSPlans`, `TenantPlans`

## Dependências
- Host registra `AddBackofficeModule`, aplica migrations do contexto Backoffice após o `MasterDbContext` do Tenancy
- OpenAPI (Scalar): esquema **`BackofficeApiKey`** (header `X-Api-Key`); rotas backoffice **não** recebem parâmetro `X-Tenant-Id` no spec (`TenantHeaderOperationFilter`)
