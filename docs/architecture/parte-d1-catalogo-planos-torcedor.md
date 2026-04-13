# Parte D.1 — Catálogo de planos (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.1) e [AGENTS.md](../../AGENTS.md): listagem de **planos publicados e ativos** para o canal do torcedor, com **benefícios do plano** (`MembershipPlanBenefits`). Não implementa contratação, `SubscribeMember` nem pagamento (D.3+).

## Decisão de produto

- O roadmap admite catálogo público ou JWT opcional; nesta entrega o endpoint e a rota `/plans` seguem o padrão das demais rotas torcedor: **`[Authorize]` (JWT obrigatório)**. Não é necessário ser sócio para consultar o catálogo.

## Separação de responsabilidades

- **Conta:** apenas autenticação via JWT.
- **Membership:** não influencia o catálogo (qualquer usuário logado vê a mesma lista de planos publicados).
- **Permissões administrativas:** não exigidas; gestão de oferta permanece em B.5 (`api/admin/plans`).

## Backend

### Porta de leitura

- `ITorcedorPublishedPlansReadPort` — `ListPublishedActiveAsync`: planos com `IsPublished && IsActive`, ordenados por nome; benefícios ordenados por `SortOrder`, depois `Title`.

### CQRS (Application)

- `ListPublishedPlansQuery` / `ListPublishedPlansQueryHandler` em `Modules/Torcedor/Queries/ListPublishedPlans`.

DTOs em `AppTorcedor.Application.Abstractions` (`TorcedorPublishedPlanItemDto`, `TorcedorPublishedPlanBenefitDto`, `TorcedorPublishedPlansCatalogDto`).

### Infraestrutura

- `TorcedorPublishedPlansReadService` em `Infrastructure/Services/Plans/TorcedorPublishedPlansReadService.cs`.
- Registro DI: `ITorcedorPublishedPlansReadPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/plans` | JWT | Catálogo: apenas planos **ativos e publicados**; corpo `{ items: [...] }`. |

Contratos HTTP: `TorcedorPublishedPlansCatalogResponse` e relacionados em `AppTorcedor.Api.Contracts`.

Controller: `TorcedorPlansController`.

## Frontend (SPA)

- Rota `/plans` dentro de `ProtectedRoute`.
- Serviço: `frontend/src/features/plans/plansService.ts` (`plansService.listPublished()`).
- Página: `PlansPage` — cards com nome, preço, ciclo, desconto, resumo, até 5 benefícios; botão **Assinar** desabilitado até D.2/D.4.
- Link no `DashboardPage`.

## Testes (TDD)

- Application: delegação do handler ao port (`TorcedorConsumptionHandlersTests`).
- API: `PartD1TorcedorPlansCatalogTests` — `401` sem token; com torcedor autenticado, apenas planos publicados aparecem; benefícios na ordem esperada.

## Próximos passos (roadmap)

- D.2: detalhe `GET /api/plans/{id}` e página de detalhe.
- D.3–D.4: contratação e checkout.
