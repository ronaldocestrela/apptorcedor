# Parte C.2 — Notícias e benefícios (consumo / torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.2) e [AGENTS.md](../../AGENTS.md): **feed e detalhe de notícias publicadas** e **listagem de ofertas de benefício elegíveis** ao usuário autenticado, com **detalhe da oferta** e **resgate self-service** (uma vez por usuário por oferta), com regras de elegibilidade por plano/status de membership **iguais** ao resgate administrativo (B.10). Não utiliza rotas `api/admin/*`.

## Separação de responsabilidades

- **Conta:** identificação via JWT (`UserId` nas rotas de benefícios).
- **Membership:** apenas para filtrar benefícios (plano + status); notícias não dependem de sócio ativo.
- **Permissões administrativas:** não exigidas; rotas exigem apenas `[Authorize]` (torcedor ou qualquer usuário autenticado).

## Backend

### Portas de leitura e resgate (torcedor)

- `ITorcedorNewsReadPort` — lista e detalhe somente com `NewsEditorialStatus.Published`.
- `ITorcedorBenefitsReadPort` — ofertas ativas, parceiro ativo, vigência atual e elegibilidade resolvida no servidor; inclui `GetEligibleOfferDetailAsync` (detalhe só quando a oferta é elegível ao usuário no momento; inclui `alreadyRedeemed` / `redemptionDateUtc`, `isShirtCustomizationOffer`, `shirtSizes` / `shirtModels`, `redemptionWorkflowStatus` quando aplicável).
- `ITorcedorBenefitRedemptionPort` — resgate self-service: valida parceiro ativo, vigência, elegibilidade e **impede pedido duplicado** quando já existe resgate **Pending** ou **Approved** para o par usuário/oferta; ofertas normais gravam `Approved` imediatamente; **oferta de camisa** exige corpo JSON com tamanho/modelo (catálogo admin), número (0–99), nome (até 10 caracteres) e **endereço de entrega** (CEP 8 dígitos após normalização, rua, número, bairro, cidade, UF) — grava `Pending` até aprovação staff (B.10); `ActorUserId = null`.

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
- `TorcedorBenefitRedemptionService` (resgate self-service; bloqueia novo pedido se já houver `Pending` ou `Approved` para o par usuário/oferta; valida catálogo de camisa, personalização e endereço de entrega contra regras de domínio).

Registro DI: `ITorcedorNewsReadPort`, `ITorcedorBenefitsReadPort`, `ITorcedorBenefitRedemptionPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/news?search=&page=&pageSize=` | JWT | Feed paginado; apenas artigos **publicados**. Ordenação: `PublishedAt` (fallback `UpdatedAt`). |
| GET | `/api/news/{newsId}` | JWT | Detalhe; `404` se não existir ou não estiver publicado. |
| GET | `/api/benefits/eligible?page=&pageSize=` | JWT | Ofertas elegíveis ao **usuário do token** (plano/status + vigência + ativo). Itens incluem `bannerUrl` (nullable). |
| GET | `/api/benefits/offers/{offerId}` | JWT | Detalhe da oferta **se** elegível ao usuário; `404` caso contrário. Corpo inclui `alreadyRedeemed`, `redemptionDateUtc`, `bannerUrl` (nullable), `isShirtCustomizationOffer`, listas `shirtSizes` / `shirtModels` (oferta camisa) e `redemptionWorkflowStatus` (`none` \| `pending` \| `approved` \| `rejected`). |
| POST | `/api/benefits/offers/{offerId}/redeem` | JWT | Resgate self-service; `201` + `{ redemptionId }`; `404` oferta inexistente. Corpo **opcional** em JSON; **oferta camisa** exige campos de camisa **e** `deliveryCep`, `deliveryNeighborhood`, `deliveryStreet`, `deliveryNumber`, `deliveryCity`, `deliveryState` (UF 2 letras; CEP normalizado para 8 dígitos no servidor). `400` com `{ error: "not_eligible" \| "already_redeemed" \| "validation_failed" }`. |

Contratos JSON espelham `AppTorcedor.Api.Contracts` (`TorcedorNewsFeedPageResponse`, `TorcedorEligibleBenefitOfferDetailResponse`, etc.).

## Frontend (SPA)

- Rotas: `/news`, `/news/:newsId`, `/benefits`, `/benefits/:offerId` (dentro de `ProtectedRoute`, fora de `/admin`).
- Serviços: `frontend/src/features/torcedor/torcedorNewsApi.ts`, `torcedorBenefitsApi.ts` (`listEligibleBenefitOffers`, `getEligibleBenefitOfferDetail`, `redeemBenefitOffer` com payload opcional para camisa + entrega); consulta **ViaCEP** em `frontend/src/features/torcedor/viaCep.ts` (autopreenchimento no detalhe da oferta).
- Páginas: `NewsFeedPage`, `NewsDetailPage`, `BenefitsEligiblePage`, `BenefitOfferDetailPage` (formulário de personalização + endereço quando `isShirtCustomizationOffer` e catálogo disponível; mensagens para `pending`/`rejected`); **home** (`DashboardPage`) com carrossel horizontal (scroll-snap) de benefícios elegíveis que levam ao detalhe/resgate.
- **Layout com `bannerUrl`:** área da imagem com `aspect-ratio: 300 / 148` e `object-fit: cover`. Texto no cartão/lista: somente **descrição** (se houver) e **intervalo de vigência**; título, parceiro, eyebrow e CTA “Ver detalhes” ficam ocultos no carrossel e na lista. Na página de detalhe, com banner: imagem no topo; descrição + datas; bloco de resgate inalterado. URLs relativas de upload são resolvidas com `resolvePublicAssetUrl` (`VITE_API_URL`).

## Testes (TDD)

- **Application:** `TorcedorConsumptionHandlersTests`, `BenefitOfferEligibilityTests`.
- **API:** `PartC2TorcedorNewsBenefitsTests` (auth, feed/detalhe draft vs publicado, benefícios abertos vs restrição de plano); `TorcedorBenefitRedemptionApiTests` (auth, detalhe, resgate, segundo resgate rejeitado, fluxo camisa pendente → aprovação admin).
- **Infrastructure:** `TorcedorBenefitRedemptionServiceTests` (resgate, não elegível, já resgatado, detalhe, validação camisa/catálogo).
- **Frontend:** `torcedorConsumptionApi.test.ts` (montagem de URLs/params; benefícios detalhe/resgate com payload camisa + entrega); `viaCep.test.ts` (integração ViaCEP com `fetch` mockado).

## Relação com outras partes

- **B.9:** editoria e notificações in-app permanecem administrativas; feed torcedor é leitura de `NewsArticles` publicados.
- **B.10:** CRUD, catálogo de camisa, fila/aprovação de pedidos e resgate staff em `api/admin/benefits` (resgate staff **bloqueado** para ofertas de camisa); torcedor lista elegíveis em `GET /api/benefits/eligible`, detalha em `GET /api/benefits/offers/{id}` e solicita em `POST /api/benefits/offers/{id}/redeem` (self-service).
