# Parte C.2 — Notícias e benefícios (consumo / torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.2) e [AGENTS.md](../../AGENTS.md): **feed e detalhe de notícias publicadas** e **listagem de ofertas de benefício elegíveis** ao usuário autenticado, com **detalhe da oferta** e **resgate self-service** (uma vez por usuário por oferta), com regras de elegibilidade por plano/status de membership **iguais** ao resgate administrativo (B.10). Não utiliza rotas `api/admin/*`.

## Separação de responsabilidades

- **Conta:** identificação via JWT (`UserId` nas rotas de benefícios).
- **Membership:** apenas para filtrar benefícios (plano + status); notícias não dependem de sócio ativo.
- **Permissões administrativas:** não exigidas; rotas exigem apenas `[Authorize]` (torcedor ou qualquer usuário autenticado).

## Backend

### Portas de leitura e resgate (torcedor)

- `ITorcedorNewsReadPort` — lista e detalhe somente com `NewsEditorialStatus.Published`.
- `ITorcedorBenefitsReadPort` — ofertas ativas, parceiro ativo, vigência atual e elegibilidade resolvida no servidor; inclui `GetEligibleOfferDetailAsync` (detalhe só quando a oferta é elegível ao usuário no momento; inclui `alreadyRedeemed` / `redemptionDateUtc`).
- `ITorcedorBenefitRedemptionPort` — resgate self-service: valida parceiro ativo, vigência, elegibilidade e **impede segundo resgate** do mesmo usuário na mesma oferta; persiste `BenefitRedemptions` com `ActorUserId = null`.

Regra compartilhada: `BenefitOfferEligibility.MatchesPlanAndStatus` em `AppTorcedor.Application.Abstractions` (usada também por `BenefitsAdministrationService.RedeemOfferAsync` e pelos serviços torcedor).

### CQRS (Application)

- `GetNewsFeedQuery` / `GetNewsFeedQueryHandler`
- `GetPublishedNewsDetailQuery` / `GetPublishedNewsDetailQueryHandler`
- `ListEligibleBenefitOffersQuery` / `ListEligibleBenefitOffersQueryHandler`
- `GetEligibleBenefitOfferDetailQuery` / `GetEligibleBenefitOfferDetailQueryHandler`
- `RedeemBenefitOfferByTorcedorCommand` / `RedeemBenefitOfferByTorcedorCommandHandler`

### Infraestrutura

- `TorcedorNewsReadService` (`INewsArticles` filtradas por publicado).
- `TorcedorBenefitsReadService` (junção ofertas × parceiros, eligibilities em memória após filtro por IDs — compatível com provider InMemory nos testes).
- `TorcedorBenefitRedemptionService` (resgate self-service; idempotência por existência de linha em `BenefitRedemptions` para o par usuário/oferta).

Registro DI: `ITorcedorNewsReadPort`, `ITorcedorBenefitsReadPort`, `ITorcedorBenefitRedemptionPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/news?search=&page=&pageSize=` | JWT | Feed paginado; apenas artigos **publicados**. Ordenação: `PublishedAt` (fallback `UpdatedAt`). |
| GET | `/api/news/{newsId}` | JWT | Detalhe; `404` se não existir ou não estiver publicado. |
| GET | `/api/benefits/eligible?page=&pageSize=` | JWT | Ofertas elegíveis ao **usuário do token** (plano/status + vigência + ativo). Itens incluem `bannerUrl` (nullable). |
| GET | `/api/benefits/offers/{offerId}` | JWT | Detalhe da oferta **se** elegível ao usuário; `404` caso contrário. Corpo inclui `alreadyRedeemed`, `redemptionDateUtc`, `bannerUrl` (nullable). |
| POST | `/api/benefits/offers/{offerId}/redeem` | JWT | Resgate self-service; `201` + `{ redemptionId }`; `404` oferta inexistente; `400` com `{ error: "not_eligible" \| "already_redeemed" }`. |

Contratos JSON espelham `AppTorcedor.Api.Contracts` (`TorcedorNewsFeedPageResponse`, `TorcedorEligibleBenefitOfferDetailResponse`, etc.).

## Frontend (SPA)

- Rotas: `/news`, `/news/:newsId`, `/benefits`, `/benefits/:offerId` (dentro de `ProtectedRoute`, fora de `/admin`).
- Serviços: `frontend/src/features/torcedor/torcedorNewsApi.ts`, `torcedorBenefitsApi.ts` (`listEligibleBenefitOffers`, `getEligibleBenefitOfferDetail`, `redeemBenefitOffer`).
- Páginas: `NewsFeedPage`, `NewsDetailPage`, `BenefitsEligiblePage`, `BenefitOfferDetailPage`; **home** (`DashboardPage`) com carrossel horizontal (scroll-snap) de benefícios elegíveis que levam ao detalhe/resgate.
- **Layout com `bannerUrl`:** área da imagem com `aspect-ratio: 300 / 148` e `object-fit: cover`. Texto no cartão/lista: somente **descrição** (se houver) e **intervalo de vigência**; título, parceiro, eyebrow e CTA “Ver detalhes” ficam ocultos no carrossel e na lista. Na página de detalhe, com banner: imagem no topo; descrição + datas; bloco de resgate inalterado. URLs relativas de upload são resolvidas com `resolvePublicAssetUrl` (`VITE_API_URL`).

## Testes (TDD)

- **Application:** `TorcedorConsumptionHandlersTests`, `BenefitOfferEligibilityTests`.
- **API:** `PartC2TorcedorNewsBenefitsTests` (auth, feed/detalhe draft vs publicado, benefícios abertos vs restrição de plano); `TorcedorBenefitRedemptionApiTests` (auth, detalhe, resgate, segundo resgate rejeitado).
- **Infrastructure:** `TorcedorBenefitRedemptionServiceTests` (resgate, não elegível, já resgatado, detalhe).
- **Frontend:** `torcedorConsumptionApi.test.ts` (montagem de URLs/params; benefícios detalhe/resgate).

## Relação com outras partes

- **B.9:** editoria e notificações in-app permanecem administrativas; feed torcedor é leitura de `NewsArticles` publicados.
- **B.10:** CRUD e resgate staff em `api/admin/benefits`; torcedor lista elegíveis em `GET /api/benefits/eligible`, detalha em `GET /api/benefits/offers/{id}` e resgata em `POST /api/benefits/offers/{id}/redeem` (self-service, distinto do resgate staff).
