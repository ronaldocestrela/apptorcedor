# Membership

## Descrição
Módulo **Fase 3** — cadastro de perfil de sócio torcedor e **planos de sócio** no banco **por tenant** (mesma connection string do Identity).

## Estrutura
- `SocioTorcedor.Modules.Membership.Domain` — `MemberProfile`, `MemberPlan`, value objects `Cpf`, `Address`, **`Vantagem`** (vantagens do plano; não é entidade), enums (`MemberStatus`), regras (`CpfMustBeUniqueRule`, `PlanNameMustBeUniqueRule`, **`MemberStatusTransitionRule`**)
- `SocioTorcedor.Modules.Membership.Application` — CQRS, `IMemberProfileRepository`, **`IMemberPlanRepository`**, `ICurrentUserAccessor`
- `SocioTorcedor.Modules.Membership.Infrastructure` — `TenantMembershipDbContext`, migrations em `__EFMembershipMigrationsHistory`; **`MemberPlan.Vantagens`** mapeadas como coluna **JSON** (`OwnsMany` + `ToJson`), sem tabela filha
- `SocioTorcedor.Modules.Membership.Api` — `MembersController` (`api/members`), **`PlansController`** (`api/plans`)

## Planos de sócio (`MemberPlan`)
- Nome único por tenant, preço, descrição opcional, ativo/inativo, lista de vantagens (texto).
- Rotas **`POST` / `PUT` / `PATCH .../toggle`**: role **`Administrador`**.
- Rotas **`GET`** (lista paginada e por id): qualquer usuário autenticado (JWT + tenant resolvido).

## Dependências
- Host: `AddMembershipModule`, migrations de tenant após Identity em `DatabaseMigrationExtensions`
- `TenantDatabaseProvisioner` (Identity) aplica também migrations do Membership ao criar tenant
- Rotas admin de membros (`GET /api/members`, `GET /api/members/{id}`) exigem role **`Administrador`**

## Status do sócio (`MemberProfile`) — Fase 3.3
- Enum **`MemberStatus`**: `PendingCompletion`, `Active`, `Delinquent`, `Canceled`, `Suspended` (persistido como `int`; migration `RemapMemberStatusEnum` converte dados legados `Inactive`/`Suspended` antigos).
- Transições inválidas retornam erro de aplicação `Membership.InvalidStatusTransition`.
- **`PATCH /api/members/{id}/status`** (body: `status`) — role **`Administrador`**.
- **`GET /api/members`** — query opcional **`status`** (filtra por `MemberStatus`).
