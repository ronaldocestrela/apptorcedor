# Parte A — Fundação técnica e governança (implementado)

Este documento descreve o que foi entregue na **Parte A** do [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md), alinhado a [AGENTS.md](../../AGENTS.md).

## Visão geral

| Área | Entrega |
|------|---------|
| CQRS | Projeto `backend/src/AppTorcedor.Application` com **MediatR**; handlers por feature em `Modules/Administration/...` |
| Permissões | Tabelas `AppPermissions` e `AppRolePermissions`; claims JWT `permission`; políticas `Permission:{nome}`; seed de todas as permissões para a role **Administrador Master** |
| Auditoria | Tabela `AuditLogs`; `AuditSaveChangesInterceptor` para `MembershipRecord`, `MembershipPlanRecord`, `PaymentRecord`, `AppConfigurationEntry` |
| Configurações | Tabela `AppConfigurationEntries` (chave, valor, versão, auditoria); API admin |
| Domínio inicial | Tabelas `Memberships`, `MembershipPlans`, `Payments` (modelo mínimo) |
| Observabilidade | `GET /health/live`, `GET /health/ready` (inclui DB); logs JSON com escopo; header `X-Correlation-Id` e propagação em contexto de auditoria |

## Permissões (catálogo)

Constantes em `AppTorcedor.Identity.ApplicationPermissions` (ex.: `Administracao.Diagnostics`, `Socios.Gerenciar`, `Configuracoes.Editar`). O JWT inclui múltiplas claims `permission` conforme o mapeamento role → permissão no banco.

O endpoint `GET /api/auth/me` devolve a mesma lista em **`permissions`**, para o frontend montar menus e guards sem decodificar o JWT.

**Nota:** A UI para editar a matriz role × permissão ainda não existe; a leitura atual é via `GET /api/admin/role-permissions`. Atribuições podem ser feitas via dados até evoluir o backoffice (Parte B).

## Endpoints administrativos (API)

| Método | Rota | Política (permissão) |
|--------|------|------------------------|
| GET | `/api/diagnostics/admin-master-only` | `Administracao.Diagnostics` |
| PATCH | `/api/admin/memberships/{id}/status` | `Socios.Gerenciar` |
| GET | `/api/admin/config` | `Configuracoes.Visualizar` |
| PUT | `/api/admin/config/{key}` | `Configuracoes.Editar` |
| GET | `/api/admin/role-permissions` | `Configuracoes.Visualizar` |
| GET | `/api/admin/audit-logs` | `Configuracoes.Visualizar` |

Corpo do PATCH de membership: `{ "status": "Ativo" }` (enum `MembershipStatus` como string JSON).

## Migração

- `PartAFoundation` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Testes

- `AppTorcedor.Application.Tests`: handler de diagnóstico (porta de conectividade).
- `AppTorcedor.Api.Tests`: permissão negada para torcedor sem claims; health; governança; auditoria após alteração de membership e configuração.

### Testes e banco in-memory

Cada `WebApplicationFactory` de teste define `Testing:InMemoryDatabaseName` único para evitar compartilhamento do provider InMemory entre hosts paralelos.

### Dados opcionais em Testing

Com `Testing:SeedSampleUsers=true`, o seed cria usuários de exemplo (`torcedor@test.local`, `member@test.local`) e uma `Membership` — ver `AppTorcedor.Infrastructure.Testing.TestingSeedConstants`.

## Decisões

- **Roles permanecem** como agrupadores de negócio; **permissões** são a unidade de autorização na API.
- **Auditoria** no mesmo `SaveChanges` das entidades auditadas (transação única).
- **Configurações** como pares chave/valor (valor string, frequentemente JSON) com `Version` incremental.
