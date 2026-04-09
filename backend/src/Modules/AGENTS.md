# Modules

## Descrição
Módulos verticais do monólito (clean architecture por módulo: Domain → Application → Infrastructure → Api).

## Estrutura
- `Tenancy/` — master DB, resolução de tenant pelo header `X-Tenant-Id` (slug)
- `Identity/` — usuários, roles, permissões, JWT

## Dependências
- Cada módulo depende dos **BuildingBlocks** e, no Host, é composto via extension methods (`*Module.cs`).
