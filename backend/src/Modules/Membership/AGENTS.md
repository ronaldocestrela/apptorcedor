# Membership

## Descrição
Módulo **Fase 3** — cadastro de perfil de sócio torcedor no banco **por tenant** (mesma connection string do Identity).

## Estrutura
- `SocioTorcedor.Modules.Membership.Domain` — `MemberProfile`, value objects `Cpf`, `Address`, enums, regras
- `SocioTorcedor.Modules.Membership.Application` — CQRS, `IMemberProfileRepository`, `ICurrentUserAccessor`
- `SocioTorcedor.Modules.Membership.Infrastructure` — `TenantMembershipDbContext`, migrations em `__EFMembershipMigrationsHistory`
- `SocioTorcedor.Modules.Membership.Api` — `MembersController` (`api/members`)

## Dependências
- Host: `AddMembershipModule`, migrations de tenant após Identity em `DatabaseMigrationExtensions`
- `TenantDatabaseProvisioner` (Identity) aplica também migrations do Membership ao criar tenant
- Rotas admin (`GET /api/members`, `GET /api/members/{id}`) exigem role **`Administrador`**
