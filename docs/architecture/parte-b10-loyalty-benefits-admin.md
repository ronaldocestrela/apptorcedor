# Parte B.10 — Loyalty e Benefits (gestão / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.10) e [AGENTS.md](../../AGENTS.md): **campanhas de fidelidade** com regras de pontuação (pagamento pago, ingresso comprado, ingresso resgatado), **extrato** e **ranking** (mensal e acumulado), **parceiros e ofertas** com vigência e elegibilidade por plano/status de membership, **resgate administrativo** com auditoria. Consumo pelo torcedor (saldo/ranking/benefícios elegíveis) permanece na Parte **C**.

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `LoyaltyCampaigns` | `Name`, `Description`, `Status` (`Draft` / `Published` / `Unpublished`), `CreatedAt`, `UpdatedAt`, `PublishedAt`, `UnpublishedAt`. |
| `LoyaltyPointRules` | `CampaignId`, `Trigger` (`PaymentPaid`, `TicketPurchased`, `TicketRedeemed`), `Points`, `SortOrder`. |
| `LoyaltyPointLedgerEntries` | `UserId`, `CampaignId?`, `RuleId?`, `Points`, `SourceType` (`Payment`, `TicketPurchase`, `TicketRedeem`, `Manual`), `SourceKey` (único com `SourceType`), `Reason`, `ActorUserId`, `CreatedAt`. |
| `BenefitPartners` | `Name`, `Description`, `IsActive`, `CreatedAt`, `UpdatedAt`. |
| `BenefitOffers` | `PartnerId`, `Title`, `Description`, `IsActive`, `StartAt`, `EndAt`, `BannerUrl` (opcional; URL pública do banner), `CreatedAt`, `UpdatedAt`. |
| `BenefitOfferPlanEligibilities` | `OfferId`, `PlanId` (PK composta). Lista vazia = sem filtro por plano. |
| `BenefitOfferMembershipStatusEligibilities` | `OfferId`, `Status` (PK composta). Lista vazia = sem filtro por status. |
| `BenefitRedemptions` | `OfferId`, `UserId`, `ActorUserId`, `Notes`, `CreatedAt`. |

Migrações EF: `PartB10LoyaltyBenefitsAdmin`; coluna `BannerUrl` em `BenefitOfferBanner` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões

- `Fidelidade.Visualizar` — leitura de campanhas, extrato, rankings.
- `Fidelidade.Gerenciar` — criar/editar campanhas, publicar/despublicar, ajuste manual de pontos.
- `Beneficios.Visualizar` — parceiros, ofertas, resgates.
- `Beneficios.Gerenciar` — CRUD de parceiros/ofertas e resgate administrativo.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`).

## API administrativa — Fidelidade

Base: `api/admin/loyalty` (JWT + política por permissão). CQRS em `AppTorcedor.Application`; persistência em `LoyaltyAdministrationService` (`ILoyaltyAdministrationPort` + `ILoyaltyPointsTriggerPort`).

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| GET | `/api/admin/loyalty/campaigns` | `Fidelidade.Visualizar` | Lista paginada (`status` opcional). |
| GET | `/api/admin/loyalty/campaigns/{id}` | `Fidelidade.Visualizar` | Detalhe com regras. |
| POST | `/api/admin/loyalty/campaigns` | `Fidelidade.Gerenciar` | Cria em `Draft`. |
| PUT | `/api/admin/loyalty/campaigns/{id}` | `Fidelidade.Gerenciar` | Atualiza apenas se não `Published`. |
| POST | `/api/admin/loyalty/campaigns/{id}/publish` | `Fidelidade.Gerenciar` | Exige ao menos uma regra com pontos ≠ 0. |
| POST | `/api/admin/loyalty/campaigns/{id}/unpublish` | `Fidelidade.Gerenciar` | Somente a partir de `Published`. |
| POST | `/api/admin/loyalty/users/{userId}/manual-adjustments` | `Fidelidade.Gerenciar` | Corpo: `points`, `reason`, `campaignId?`. |
| GET | `/api/admin/loyalty/users/{userId}/ledger` | `Fidelidade.Visualizar` | Extrato paginado. |
| GET | `/api/admin/loyalty/rankings/monthly?year=&month=` | `Fidelidade.Visualizar` | Soma dos pontos no mês (UTC). |
| GET | `/api/admin/loyalty/rankings/all-time` | `Fidelidade.Visualizar` | Ranking acumulado. |

### Gatilhos automáticos de pontos

- **Pagamento:** após conciliação bem-sucedida (`PaymentAdministrationService.ConciliatePaymentAsync`), `ILoyaltyPointsTriggerPort.AwardPointsForPaymentPaidAsync`.
- **Ingresso:** após compra (`PurchaseTicketAsync`) ou promoção para comprado via sync (`SyncTicketAsync`), `AwardPointsForTicketPurchasedAsync`; após resgate (`RedeemTicketAsync`), `AwardPointsForTicketRedeemedAsync`.

Idempotência: índice único `(SourceType, SourceKey)` com `SourceKey` = `{entidade:N}|{ruleId:N}` para eventos automáticos; manual usa `SourceKey` = id do lançamento.

## API administrativa — Benefícios

Base: `api/admin/benefits`. Implementação em `BenefitsAdministrationService` (`IBenefitsAdministrationPort`).

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| GET | `/api/admin/benefits/partners` | `Beneficios.Visualizar` | Lista parceiros. |
| GET | `/api/admin/benefits/partners/{id}` | `Beneficios.Visualizar` | Detalhe. |
| POST | `/api/admin/benefits/partners` | `Beneficios.Gerenciar` | Cria parceiro. |
| PUT | `/api/admin/benefits/partners/{id}` | `Beneficios.Gerenciar` | Atualiza. |
| GET | `/api/admin/benefits/offers` | `Beneficios.Visualizar` | Lista ofertas. |
| GET | `/api/admin/benefits/offers/{id}` | `Beneficios.Visualizar` | Detalhe + elegibilidades. |
| POST | `/api/admin/benefits/offers` | `Beneficios.Gerenciar` | Cria oferta e elegibilidades opcionais. |
| PUT | `/api/admin/benefits/offers/{id}` | `Beneficios.Gerenciar` | Atualiza. |
| POST | `/api/admin/benefits/offers/{id}/banner` | `Beneficios.Gerenciar` | `multipart/form-data` com campo `file` (JPEG/PNG/Webp; limite alinhado aos outros uploads). Persiste URL em `BannerUrl` via `IBenefitOfferBannerStorage` (Local ou Cloudinary; configuração `BenefitOfferBanner`). |
| DELETE | `/api/admin/benefits/offers/{id}/banner` | `Beneficios.Gerenciar` | Remove `BannerUrl` e apaga arquivo em **best effort**. |
| POST | `/api/admin/benefits/offers/{id}/redeem` | `Beneficios.Gerenciar` | Resgate administrativo (`userId`, `notes`); valida vigência e elegibilidade. |
| GET | `/api/admin/benefits/redemptions` | `Beneficios.Visualizar` | Lista resgates. |

### Elegibilidade no resgate

- Se existem linhas em `BenefitOfferPlanEligibilities`, o usuário deve ter `Membership` com `PlanId` na lista.
- Se existem linhas em `BenefitOfferMembershipStatusEligibilities`, o `Membership.Status` deve estar na lista.
- Se ambas as listas estão vazias, qualquer usuário válido pode resgatar (desde que oferta ativa e dentro da vigência).

## Auditoria

Mutações nas novas entidades geram entradas em `AuditLogs` via interceptor existente.

## Frontend (backoffice)

- Rotas `/admin/loyalty` e `/admin/benefits` (permissões conforme tabelas acima).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.
- `Fidelidade.*` e `Beneficios.*` incluídas em `ADMIN_AREA_PERMISSIONS`.

### Administração de benefícios (UI) — `/admin/benefits`

Implementação em `frontend/src/features/admin/pages/BenefitsAdminPage.tsx` + helpers `benefitsAdminHelpers.ts`.

- **Parceiros:** listagem com filtro (nome + aplicar; ativo/todos/inativos), criar/editar (`GET /api/admin/benefits/partners/{id}` ao editar), ativar/desativar (via `PUT` preservando nome/descrição carregados do detalhe).
- **Ofertas (`BenefitOffers`):** formulário com parceiro (select), título, descrição, **início/fim de vigência** (`datetime-local` → ISO UTC na API), checkbox **oferta ativa**, elegibilidade opcional (GUIDs de planos separados por vírgula; checkboxes de status de membership alinhados ao enum do backend).
- **Status exibido (derivado no cliente, não persistido):** `Inativa` (`!isActive`); senão `Expirada` se `now > endAt`; senão `Programada` se `now < startAt`; senão `Vigente` se `isActive && startAt ≤ now ≤ endAt`.
- **Filtros:** parceiro na lista de ofertas; status derivado (Vigente / Programada / Expirada / Inativa).
- **Ações por linha:** Editar (carrega `GET /offers/{id}` no formulário), Ativar/Desativar e **Excluir (soft)** — ambos atualizam via `PUT /api/admin/benefits/offers/{id}` com `isActive` adequado, **preservando** `eligiblePlanIds` / `eligibleMembershipStatuses` retornados do GET antes do PUT.
- **Validação client-side:** data final ≥ data inicial; título e parceiro obrigatórios.
- **Resgates administrativos** e listagem de últimos resgates permanecem na mesma página.
- **Banner:** na **nova oferta** ou ao **editar**, envio de imagem (na criação o arquivo fica pendente até o `POST` da oferta devolver `offerId`, depois corre `POST .../banner`) e remoção; proporção recomendada **300×148** (mesma razão do carrossel na home). O `PUT` de oferta não altera `BannerUrl` — só os endpoints de banner.

## Testes

- `AppTorcedor.Application.Tests` — `LoyaltyAdminHandlersTests`, `BenefitsAdminHandlersTests`.
- `AppTorcedor.Api.Tests` — `PartB10LoyaltyBenefitsAdminTests` (autorização, pontos por conciliação, fluxo parceiro/oferta/resgate).
- `frontend` — `benefitsAdminHelpers.test.ts` (regras de status derivado), `BenefitsAdminPage.test.tsx` (lista com badges, criação com vigência e upload de banner após create, validação de datas, desativar com PUT preservando elegibilidade, edição).
- `AppTorcedor.Api.Tests` — `PartB10LoyaltyBenefitsAdminTests.Benefits_offer_banner_upload_get_delete_roundtrip` (POST/GET/DELETE banner).

## Relação com outras partes

- **C.2 / C.5:** consumo de benefícios e fidelidade pelo torcedor (benefícios elegíveis: [parte-c2-noticias-beneficios-torcedor.md](parte-c2-noticias-beneficios-torcedor.md); saldo e ranking: [parte-c5-fidelidade-torcedor.md](parte-c5-fidelidade-torcedor.md)).
- **Conta vs associação:** pontos são da **conta** (`UserId`); regras de benefício usam **Membership** apenas para elegibilidade, sem acoplar permissões administrativas.
