# Parte B.5 — Plans (gestão de oferta)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.5) e [AGENTS.md](../../AGENTS.md): CRUD administrativo de planos (`MembershipPlan`), benefícios exibíveis por plano, notas de regras operacionais básicas e **publicação** para o catálogo do torcedor (Parte D). Não implementa contratação pública, `SubscribeMember` nem integração de pagamento (B.6 / Parte D).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `MembershipPlans` | Snapshot do plano: `Name`, `Price`, `BillingCycle`, `DiscountPercentage`, `IsActive`, `IsPublished`, `PublishedAt`, `Summary`, `RulesNotes`. |
| `MembershipPlanBenefits` | Itens de benefício por plano: `PlanId`, `SortOrder`, `Title`, `Description` (cascade delete com o plano). |

Migração EF: `PartB5PlansAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

### Ciclo de cobrança

Valores aceitos (normalizados para forma canônica): `Monthly`, `Yearly`, `Quarterly`.

### Ativo vs publicado

- **Ativo (`IsActive`)**: plano utilizável na operação (vínculos existentes, futura contratação interna).
- **Publicado (`IsPublished`)**: visível no **catálogo do torcedor** quando a Parte D expuser `ListPlans`. **Não** é permitido publicar plano inativo (`IsPublished && !IsActive` → `400`).

## Permissões

- `Planos.Visualizar` — listagem e detalhe.
- `Planos.Criar` — `POST /api/admin/plans`.
- `Planos.Editar` — `PUT /api/admin/plans/{id}` (inclui publicar/despublicar e substituir benefícios).

## API administrativa

Base: `api/admin/plans` (JWT + política por permissão). CQRS em `AppTorcedor.Application` e implementação em `PlanAdministrationService` (`IPlansAdministrationPort`).

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/plans?search=&isActive=&isPublished=&page=&pageSize=` | Lista paginada; `benefitCount` por plano. |
| GET | `/api/admin/plans/{planId}` | Detalhe com benefícios ordenados. |
| POST | `/api/admin/plans` | Corpo alinhado a `UpsertPlanRequest` (nome, preço, ciclo, desconto, flags, `summary`, `rulesNotes`, `benefits[]`). |
| PUT | `/api/admin/plans/{planId}` | Atualização completa; benefícios são **substituídos** pelo payload. |

Corpo de escrita (`UpsertPlanRequest`): `name`, `price`, `billingCycle`, `discountPercentage`, `isActive`, `isPublished`, `summary`, `rulesNotes`, `benefits` (`sortOrder`, `title`, `description`).

## Regras de validação (aplicação)

- Nome obrigatório; preço ≥ 0; desconto entre 0 e 100; até 50 benefícios; títulos/descrições com limites de tamanho (ver `PlanWriteValidator`).
- Publicação com plano inativo rejeitada.

## Auditoria

- `MembershipPlanRecord` e `MembershipPlanBenefitRecord` entram em `AuditLogs` via `AuditSaveChangesInterceptor`.

## Frontend (backoffice)

- Rota `/admin/plans` (`Planos.Visualizar` para ver; criar/editar conforme `Planos.Criar` / `Planos.Editar`).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.
- Menu admin e `ADMIN_AREA_PERMISSIONS` incluem as permissões de planos para acesso ao shell.

## Testes

- `AppTorcedor.Application.Tests` / `PlansAdminHandlersTests`: validação de publicação inativa, normalização de ciclo, delegação ao port.
- `AppTorcedor.Api.Tests` / `PartB5PlansAdminTests`: autorização, CRUD, rejeição de publicação inativa, roundtrip de publicação.

## Relação com B.4 e Parte D

- B.4 permanece com **plano e datas somente leitura** na visão de membership; alteração de oferta é feita neste módulo.
- Parte D (`ListPlans`) deve filtrar planos **ativos e publicados** (implementação futura).
