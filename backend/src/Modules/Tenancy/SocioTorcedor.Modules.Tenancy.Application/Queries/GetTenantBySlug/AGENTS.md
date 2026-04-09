# GetTenantBySlug

## Descrição
Query MediatR que obtém `TenantContext` pelo slug do tenant (mesmo valor enviado no header `X-Tenant-Id`).

## Estrutura
- `GetTenantBySlugHandler.cs`
- `GetTenantBySlugQuery.cs`

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` — `ITenantRepository`
