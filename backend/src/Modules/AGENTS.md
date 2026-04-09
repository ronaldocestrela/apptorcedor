# Modules

## Descrição
Módulos verticais do monólito (clean architecture por módulo: Domain → Application → Infrastructure → Api).

## Estrutura
- `Tenancy/` — master DB, resolução de tenant pelo header `X-Tenant-Id` (slug), CRUD de tenant (comandos/queries usados pelo backoffice)
- `Identity/` — usuários, roles, permissões, JWT
- `Membership/` — perfil de sócio torcedor (CPF, endereço, etc.) no banco por tenant; `TenantMembershipDbContext` + `api/members`
- `Backoffice/` — API SaaS admin: tenants (HTTP), planos SaaS, tenant-plans; ver `Backoffice/AGENTS.md`

## Dependências
- Cada módulo depende dos **BuildingBlocks** e, no Host, é composto via extension methods (`*Module.cs`).
