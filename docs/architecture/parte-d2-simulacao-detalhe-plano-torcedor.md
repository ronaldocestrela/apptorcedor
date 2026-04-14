# Parte D.2 — Simulação / detalhe do plano (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.2) e [AGENTS.md](../../AGENTS.md): leitura **detalhada** de um plano publicado e ativo (inclui `RulesNotes` e benefícios com `sortOrder`), com **simulação visual** do valor após desconto na SPA. Não implementa contratação (`SubscribeMember`) nem pagamento (D.3+).

## Decisão de produto

- Endpoint e rota seguem o padrão do D.1: **`[Authorize]` (JWT obrigatório)**. Não é necessário ser sócio.
- Planos **não publicados** ou **inativos** retornam **404** no canal torcedor (mesma visibilidade do catálogo: apenas `IsPublished && IsActive`).

## Separação de responsabilidades

- **Conta:** apenas autenticação via JWT.
- **Membership:** não influencia o detalhe (qualquer usuário logado vê o mesmo conteúdo para um plano elegível).
- **Permissões administrativas:** não exigidas; gestão de oferta permanece em B.5 (`api/admin/plans`).

## Backend

### Porta de leitura

- `ITorcedorPublishedPlansReadPort` — além de `ListPublishedActiveAsync` (D.1), expõe `GetPublishedActiveByIdAsync`: retorna detalhe apenas se `IsPublished && IsActive`; caso contrário `null`.

### CQRS (Application)

- `GetPlanDetailsQuery` / `GetPlanDetailsQueryHandler` em `Modules/Torcedor/Queries/GetPlanDetails`.
- DTOs em `AppTorcedor.Application.Abstractions`: `TorcedorPublishedPlanDetailDto`, `TorcedorPublishedPlanDetailBenefitDto` (inclui `SortOrder`).

### Infraestrutura

- `TorcedorPublishedPlansReadService` — implementação de `GetPublishedActiveByIdAsync` com EF Core (`AsNoTracking`), benefícios ordenados por `SortOrder`, depois `Title`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/plans/{planId}` | JWT | Detalhe do plano **ativo e publicado**; **404** se inexistente ou não elegível no canal torcedor. |

Contratos HTTP: `TorcedorPublishedPlanDetailResponse`, `TorcedorPublishedPlanDetailBenefitResponse` em `AppTorcedor.Api.Contracts`.

Controller: `TorcedorPlansController` (`GetById`).

## Frontend (SPA)

- Rota protegida `plans/:planId` (junto ao catálogo D.1).
- Serviço: `frontend/src/features/plans/plansService.ts` — `plansService.getById(planId)`.
- Página: `PlanDetailsPage` — preço, ciclo, percentual de desconto, **valor simulado** após desconto, resumo, regras, lista completa de benefícios; botão **Contratar** desabilitado até D.3/D.4.
- Listagem `PlansPage`: CTA **Assinar** navega para o detalhe.

## Testes (TDD)

- Application: delegação do handler ao port (`TorcedorConsumptionHandlersTests` — `GetPlanDetails_delegates_to_port`).
- API: `PartD2TorcedorPlansDetailTests` — `401` sem token; `200` com payload completo; `404` para rascunho e id inexistente.
- Frontend: `plansService.test.ts`, `PlanDetailsPage.test.tsx` (loading, sucesso com valor descontado, 404, erro genérico).

## Próximos passos (roadmap)

- D.3–D.4: `SubscribeMember`, checkout e integração de pagamento.
