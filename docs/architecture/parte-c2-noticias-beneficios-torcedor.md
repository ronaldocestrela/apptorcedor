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
- `ITorcedorBenefitRedemptionPort` — resgate self-service: valida parceiro ativo, vigência, elegibilidade e **impede pedido duplicado** quando já existe resgate **Pending** ou **Approved** para o par usuário/oferta; ofertas normais gravam `Approved` imediatamente; **oferta de camisa** exige corpo JSON com tamanho/modelo (catálogo admin), número (0–99), nome (até 10 caracteres), **`shippingMethod`** (`pickup` \| `carrier`) e, se `carrier`, endereço completo + frete escolhido (`shippingCarrierId`, `shippingCarrierName`, `shippingServiceName`, `shippingPrice`, `shippingDeliveryDays`). Se `pickup`, endereço e frete ficam nulos no registro. Com **`carrier`**, após criar o `BenefitRedemption` em `Pending`, o sistema cria uma cobrança (`Payments`) ligada ao resgate (`ShippingPaymentId`), inicia **Stripe Checkout** para o valor do frete e retorna **`checkoutUrl`** na resposta HTTP — o torcedor é redirecionado para o Stripe; ao confirmar o pagamento, o **webhook** (D.4) marca o frete como pago e **aprova automaticamente** o resgate (sem fila manual). **Retirada (`pickup`)** continua em `Pending` até aprovação staff (B.10); `ActorUserId = null`.

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
- `TorcedorBenefitRedemptionService` (resgate self-service; bloqueia novo pedido se já houver `Pending` ou `Approved` para o par usuário/oferta; valida catálogo de camisa, personalização, **método de entrega** e, em `carrier`, endereço + dados do frete; integra `IPaymentProvider` para Checkout do frete quando aplicável).
- `MelhorEnvioShippingService` (`IMelhorEnvioShippingPort`) — cotação `POST /api/v2/me/shipment/calculate` (Melhor Envio); token vazio ou falha HTTP retornam lista vazia (degradação graciosa). Config: seção `MelhorEnvio` (`Token`, `UserAgent`, `FromPostalCode`, dimensões/peso do pacote).

Registro DI: `ITorcedorNewsReadPort`, `ITorcedorBenefitsReadPort`, `ITorcedorBenefitRedemptionPort`, `IMelhorEnvioShippingPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/news?search=&page=&pageSize=` | JWT | Feed paginado; apenas artigos **publicados**. Ordenação: `PublishedAt` (fallback `UpdatedAt`). |
| GET | `/api/news/{newsId}` | JWT | Detalhe; `404` se não existir ou não estiver publicado. |
| GET | `/api/benefits/eligible?page=&pageSize=` | JWT | Ofertas elegíveis ao **usuário do token** (plano/status + vigência + ativo). Itens incluem `bannerUrl` (nullable). |
| GET | `/api/benefits/offers/{offerId}` | JWT | Detalhe da oferta **se** elegível ao usuário; `404` caso contrário. Corpo inclui `alreadyRedeemed`, `redemptionDateUtc`, `bannerUrl` (nullable), `isShirtCustomizationOffer`, listas `shirtSizes` / `shirtModels` (oferta camisa) e `redemptionWorkflowStatus` (`none` \| `pending` \| `approved` \| `rejected` \| `awaiting_shipping_payment` quando camisa + envio e frete ainda não confirmado pelo provedor). |
| GET | `/api/benefits/shipping-options?cep=` | JWT | Cotação de frete (Melhor Envio) para o CEP informado; resposta: array de opções com preço e prazo; **[]** se token Melhor Envio vazio ou indisponível. |
| POST | `/api/benefits/offers/{offerId}/redeem` | JWT | Resgate self-service; `201` + `{ redemptionId, checkoutUrl? }`. Com **camisa + `carrier`**, `checkoutUrl` aponta para o Stripe Checkout do frete (SPA redireciona na hora). Com **pickup** ou benefício sem camisa, `checkoutUrl` é `null`. `404` oferta inexistente. Corpo **opcional** em JSON; **oferta camisa** exige campos de camisa e `shippingMethod`. Se `carrier`: endereço completo + campos de frete da opção escolhida. Se `pickup`: sem endereço/frete. `400` com `{ error: "not_eligible" \| "already_redeemed" \| "validation_failed" }`. |

Contratos JSON espelham `AppTorcedor.Api.Contracts` (`TorcedorNewsFeedPageResponse`, `TorcedorEligibleBenefitOfferDetailResponse`, etc.).

## Frontend (SPA)

- Rotas: `/news`, `/news/:newsId`, `/benefits`, `/benefits/:offerId` (dentro de `ProtectedRoute`, fora de `/admin`).
- Serviços: `frontend/src/features/torcedor/torcedorNewsApi.ts`, `torcedorBenefitsApi.ts` (`listEligibleBenefitOffers`, `getEligibleBenefitOfferDetail`, `getShippingOptions`, `redeemBenefitOffer` com payload opcional para camisa + entrega/frete; resposta inclui `checkoutUrl` para redirecionar ao Stripe no envio); consulta **ViaCEP** em `frontend/src/features/torcedor/viaCep.ts` (autopreenchimento quando modo **envio**).
- Páginas: `NewsFeedPage`, `NewsDetailPage`, `BenefitsEligiblePage`, `BenefitOfferDetailPage` (formulário de personalização; escolha **retirada na loja** ou **receber em casa**; no envio: endereço + lista de fretes da API; se a API devolver `checkoutUrl`, redirecionamento imediato; mensagens para `pending` / `rejected` / `awaiting_shipping_payment`; ação de cancelamento de resgate exibida apenas para workflows `pending`/`approved`); **home** (`DashboardPage`) com carrossel horizontal (scroll-snap) de benefícios elegíveis que levam ao detalhe/resgate.
- **Layout com `bannerUrl`:** área da imagem com `aspect-ratio: 300 / 148` e `object-fit: cover`. Texto no cartão/lista: somente **descrição** (se houver) e **intervalo de vigência**; título, parceiro, eyebrow e CTA “Ver detalhes” ficam ocultos no carrossel e na lista. Na página de detalhe, com banner: imagem no topo; descrição + datas; bloco de resgate inalterado. URLs relativas de upload são resolvidas com `resolvePublicAssetUrl` (`VITE_API_URL`).

## Testes (TDD)

- **Application:** `TorcedorConsumptionHandlersTests`, `BenefitOfferEligibilityTests`.
- **API:** `PartC2TorcedorNewsBenefitsTests` (auth, feed/detalhe draft vs publicado, benefícios abertos vs restrição de plano); `TorcedorBenefitRedemptionApiTests` (auth, detalhe, resgate, segundo resgate rejeitado, fluxo camisa pendente → aprovação admin, `GET /api/benefits/shipping-options`, camisa com retirada ou frete).
- **Infrastructure:** `TorcedorBenefitRedemptionServiceTests`; `MelhorEnvioShippingServiceTests`.
- **Frontend:** `torcedorConsumptionApi.test.ts` (benefícios, `getShippingOptions`, payload camisa + entrega/frete); `viaCep.test.ts` (integração ViaCEP com `fetch` mockado).

## Relação com outras partes

- **B.9:** editoria e notificações in-app permanecem administrativas; feed torcedor é leitura de `NewsArticles` publicados.
- **B.10:** CRUD, catálogo de camisa, fila/aprovação de pedidos e resgate staff em `api/admin/benefits` (resgate staff **bloqueado** para ofertas de camisa); torcedor lista elegíveis em `GET /api/benefits/eligible`, detalha em `GET /api/benefits/offers/{id}` e solicita em `POST /api/benefits/offers/{id}/redeem` (self-service).
