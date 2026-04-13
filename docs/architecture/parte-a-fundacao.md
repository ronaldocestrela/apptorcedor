# Parte A — Fundação técnica e governança (implementado)

Este documento descreve o que foi entregue na **Parte A** do [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md), alinhado a [AGENTS.md](../../AGENTS.md).

## Visão geral

| Área | Entrega |
|------|---------|
| CQRS | Projeto `backend/src/AppTorcedor.Application` com **MediatR**; handlers por feature em `Modules/Administration/...` |
| Permissões | Tabelas `AppPermissions` e `AppRolePermissions`; claims JWT `permission`; políticas `Permission:{nome}`; seed de todas as permissões para a role **Administrador Master** |
| Auditoria | Tabela `AuditLogs`; `AuditSaveChangesInterceptor` para `MembershipRecord`, `MembershipPlanRecord`, `PaymentRecord`, `AppConfigurationEntry`, `StaffInviteRecord`, `AppRolePermission` |
| Configurações | Tabela `AppConfigurationEntries` (chave, valor, versão, auditoria); API admin |
| Domínio inicial | Tabelas `Memberships`, `MembershipPlans`, `Payments` (modelo mínimo) |
| Observabilidade | `GET /health/live`, `GET /health/ready` (inclui DB); logs JSON com escopo; header `X-Correlation-Id` e propagação em contexto de auditoria |

## Permissões (catálogo)

Constantes em `AppTorcedor.Identity.ApplicationPermissions` (ex.: `Administracao.Diagnostics`, `Socios.Gerenciar`, `Configuracoes.Editar`). O JWT inclui múltiplas claims `permission` conforme o mapeamento role → permissão no banco.

O endpoint `GET /api/auth/me` devolve a mesma lista em **`permissions`**, para o frontend montar menus e guards sem decodificar o JWT.

**Nota:** A matriz role × permissão é editável via `PUT /api/admin/role-permissions` (`Configuracoes.Editar`) e na SPA em `/admin/role-permissions`.

## Endpoints administrativos (API)

| Método | Rota | Política (permissão) |
|--------|------|------------------------|
| GET | `/api/diagnostics/admin-master-only` | `Administracao.Diagnostics` |
| GET | `/api/admin/dashboard` | política composta `AdminDashboard` (`Usuarios.Visualizar` **ou** `Configuracoes.Visualizar`) |
| POST | `/api/admin/staff/invites` | `Usuarios.Editar` |
| GET | `/api/admin/staff/invites` | `Usuarios.Visualizar` |
| GET | `/api/admin/staff/users` | `Usuarios.Visualizar` |
| PATCH | `/api/admin/staff/users/{id}/active` | `Usuarios.Editar` |
| PUT | `/api/admin/staff/users/{id}/roles` | `Usuarios.Editar` |
| PATCH | `/api/admin/memberships/{id}/status` | `Socios.Gerenciar` |
| GET | `/api/admin/config` | `Configuracoes.Visualizar` |
| PUT | `/api/admin/config/{key}` | `Configuracoes.Editar` |
| GET | `/api/admin/role-permissions` | `Configuracoes.Visualizar` |
| PUT | `/api/admin/role-permissions` | `Configuracoes.Editar` |
| GET | `/api/admin/audit-logs` | `Configuracoes.Visualizar` |

Corpo do PATCH de membership: `{ "status": "Ativo" }` (enum `MembershipStatus` como string JSON).

## Migração

- `PartAFoundation` e `PartB1StaffInvites` (tabela `StaffInvites`) em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Testes

- `AppTorcedor.Application.Tests`: handler de diagnóstico (porta de conectividade).
- `AppTorcedor.Api.Tests`: permissão negada para torcedor sem claims; health; governança; auditoria após alteração de membership e configuração; Parte B.1 (staff, matriz editável, dashboard).

### Testes e banco in-memory

Cada `WebApplicationFactory` de teste define `Testing:InMemoryDatabaseName` único para evitar compartilhamento do provider InMemory entre hosts paralelos.

### Dados opcionais em Testing

Com `Testing:SeedSampleUsers=true`, o seed cria usuários de exemplo (`torcedor@test.local`, `member@test.local`) e uma `Membership` — ver `AppTorcedor.Infrastructure.Testing.TestingSeedConstants`.

## Decisões

- **Roles permanecem** como agrupadores de negócio; **permissões** são a unidade de autorização na API.
- **Auditoria** no mesmo `SaveChanges` das entidades auditadas (transação única).
- **Configurações** como pares chave/valor (valor string, frequentemente JSON) com `Version` incremental.
