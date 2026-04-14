# Parte B.4 — Membership (visão administrativa)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.4) e [AGENTS.md](../../AGENTS.md): consulta administrativa da associação, alteração manual de **status** com **motivo obrigatório**, histórico operacional em tabela dedicada e trilha técnica em `AuditLogs`. **Plano e datas** (`PlanId`, `StartDate`, `EndDate`, `NextDueDate`) permanecem **somente leitura** na visão de membership; a gestão da **oferta** (cadastro/publicação do plano) está em [parte-b5-plans-admin.md](parte-b5-plans-admin.md) (edição do vínculo `PlanId` na membership permanece fora deste escopo até regra explícita com B.6).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `Memberships` | Snapshot atual: um registro por usuário (`UserId` único), status, plano opcional, datas. |
| `MembershipHistories` | Histórico append-only de eventos administrativos (ex.: mudança de status), com motivo, ator e espelho do plano no momento da alteração. |

Migração EF: `PartB4MembershipHistory` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

### Eventos

- `EventType`: por ora `StatusChanged` (constante `MembershipHistoryEventTypes.StatusChanged` no backend).
- Campos `FromPlanId` / `ToPlanId` refletem o plano vigente no snapshot no momento da alteração (inalterado quando só o status muda).

## Permissões

- `Socios.Gerenciar` — listagem, detalhe, histórico e `PATCH` de status.

## API administrativa

Base: `api/admin/memberships` (JWT + política por permissão). CQRS: comandos/queries em `AppTorcedor.Application` e implementação em `MembershipAdministrationService`.

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/memberships?status=&userId=&page=&pageSize=` | Lista paginada com filtros opcionais. |
| GET | `/api/admin/memberships/{membershipId}` | Detalhe do snapshot + identificação do usuário. |
| GET | `/api/admin/memberships/{membershipId}/history?take=` | Histórico operacional (`404` se a membership não existir). |
| PATCH | `/api/admin/memberships/{membershipId}/status` | Corpo: `status`, `reason` (obrigatório, até 2000 caracteres). `400` se o status for igual ao atual; `404` se não existir. |

O ator da alteração é obtido do JWT (`NameIdentifier`) e gravado em `MembershipHistories.ActorUserId`.

## Relação com B.3 (Users)

- Em [parte-b3-users-admin.md](parte-b3-users-admin.md), o resumo de membership no detalhe/listagem de usuários continua **somente leitura**.
- O backoffice oferece atalho da ficha do usuário para `/admin/membership?membershipId=...`.

## Frontend (backoffice)

- Rota `/admin/membership` (`Socios.Gerenciar`): lista, detalhe, histórico e formulário de status + motivo.
- Query strings suportadas: `?userId=` e `?membershipId=`.
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.

## Testes

- `AppTorcedor.Application.Tests` / `MembershipAdminHandlersTests`: delegação dos handlers ao port.
- `AppTorcedor.Api.Tests` / `PartB4MembershipAdminTests`: autorização, listagem, detalhe, validação de `reason`, histórico após alteração, status inalterado.
- `PartAGovernanceAndObservabilityTests`: alteração de status com motivo continua gerando auditoria em `MembershipRecord`.
