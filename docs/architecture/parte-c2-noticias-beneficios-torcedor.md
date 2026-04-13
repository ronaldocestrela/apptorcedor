# Parte C.2 — Notícias e benefícios (consumo / torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.2) e [AGENTS.md](../../AGENTS.md): **feed e detalhe de notícias publicadas** e **listagem de ofertas de benefício elegíveis** ao usuário autenticado, com regras de elegibilidade por plano/status de membership **iguais** ao resgate administrativo (B.10). Não utiliza rotas `api/admin/*`.

## Separação de responsabilidades

- **Conta:** identificação via JWT (`UserId` nas rotas de benefícios).
- **Membership:** apenas para filtrar benefícios (plano + status); notícias não dependem de sócio ativo.
- **Permissões administrativas:** não exigidas; rotas exigem apenas `[Authorize]` (torcedor ou qualquer usuário autenticado).

## Backend

### Portas de leitura

- `ITorcedorNewsReadPort` — lista e detalhe somente com `NewsEditorialStatus.Published`.
- `ITorcedorBenefitsReadPort` — ofertas ativas, parceiro ativo, vigência atual e elegibilidade resolvida no servidor.

Regra compartilhada: `BenefitOfferEligibility.MatchesPlanAndStatus` em `AppTorcedor.Application.Abstractions` (usada também por `BenefitsAdministrationService.RedeemOfferAsync`).

### CQRS (Application)

- `GetNewsFeedQuery` / `GetNewsFeedQueryHandler`
- `GetPublishedNewsDetailQuery` / `GetPublishedNewsDetailQueryHandler`
- `ListEligibleBenefitOffersQuery` / `ListEligibleBenefitOffersQueryHandler`

### Infraestrutura

- `TorcedorNewsReadService` (`INewsArticles` filtradas por publicado).
- `TorcedorBenefitsReadService` (junção ofertas × parceiros, eligibilities em memória após filtro por IDs — compatível com provider InMemory nos testes).

Registro DI: `ITorcedorNewsReadPort`, `ITorcedorBenefitsReadPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/news?search=&page=&pageSize=` | JWT | Feed paginado; apenas artigos **publicados**. Ordenação: `PublishedAt` (fallback `UpdatedAt`). |
| GET | `/api/news/{newsId}` | JWT | Detalhe; `404` se não existir ou não estiver publicado. |
| GET | `/api/benefits/eligible?page=&pageSize=` | JWT | Ofertas elegíveis ao **usuário do token** (plano/status + vigência + ativo). |

Contratos JSON espelham `AppTorcedor.Api.Contracts` (`TorcedorNewsFeedPageResponse`, etc.).

## Frontend (SPA)

- Rotas: `/news`, `/news/:newsId`, `/benefits` (dentro de `ProtectedRoute`, fora de `/admin`).
- Serviços: `frontend/src/features/torcedor/torcedorNewsApi.ts`, `torcedorBenefitsApi.ts`.
- Páginas: `NewsFeedPage`, `NewsDetailPage`, `BenefitsEligiblePage`; links no `DashboardPage`.

## Testes (TDD)

- **Application:** `TorcedorConsumptionHandlersTests`, `BenefitOfferEligibilityTests`.
- **API:** `PartC2TorcedorNewsBenefitsTests` (auth, feed/detalhe draft vs publicado, benefícios abertos vs restrição de plano).
- **Frontend:** `torcedorConsumptionApi.test.ts` (montagem de URLs/params).

## Relação com outras partes

- **B.9:** editoria e notificações in-app permanecem administrativas; feed torcedor é leitura de `NewsArticles` publicados.
- **B.10:** CRUD e resgate staff em `api/admin/benefits`; torcedor consome apenas elegibilidade via `api/benefits/eligible`.
