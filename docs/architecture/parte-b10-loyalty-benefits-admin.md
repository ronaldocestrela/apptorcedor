# Parte B.10 — Loyalty e Benefits (gestão / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.10) e [AGENTS.md](../../AGENTS.md): **campanhas de fidelidade** com regras de pontuação (pagamento pago, ingresso comprado, ingresso resgatado), **extrato** e **ranking** (mensal e acumulado), **parceiros e ofertas** com vigência e elegibilidade por plano/status de membership, **resgate administrativo** com auditoria. Consumo pelo torcedor (saldo/ranking/benefícios elegíveis) permanece na Parte **C**.

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `LoyaltyCampaigns` | `Name`, `Description`, `Status` (`Draft` / `Published` / `Unpublished`), `CreatedAt`, `UpdatedAt`, `PublishedAt`, `UnpublishedAt`. |
| `LoyaltyPointRules` | `CampaignId`, `Trigger` (`PaymentPaid`, `TicketPurchased`, `TicketRedeemed`), `Points`, `SortOrder`. |
| `LoyaltyPointLedgerEntries` | `UserId`, `CampaignId?`, `RuleId?`, `Points`, `SourceType` (`Payment`, `TicketPurchase`, `TicketRedeem`, `Manual`), `SourceKey` (único com `SourceType`), `Reason`, `ActorUserId`, `CreatedAt`. |
| `BenefitPartners` | `Name`, `Description`, `IsActive`, `CreatedAt`, `UpdatedAt`. |
| `BenefitOffers` | `PartnerId`, `Title`, `Description`, `IsActive`, `StartAt`, `EndAt`, `CreatedAt`, `UpdatedAt`. |
| `BenefitOfferPlanEligibilities` | `OfferId`, `PlanId` (PK composta). Lista vazia = sem filtro por plano. |
| `BenefitOfferMembershipStatusEligibilities` | `OfferId`, `Status` (PK composta). Lista vazia = sem filtro por status. |
| `BenefitRedemptions` | `OfferId`, `UserId`, `ActorUserId`, `Notes`, `CreatedAt`. |

Migração EF: `PartB10LoyaltyBenefitsAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

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

## Testes

- `AppTorcedor.Application.Tests` — `LoyaltyAdminHandlersTests`, `BenefitsAdminHandlersTests`.
- `AppTorcedor.Api.Tests` — `PartB10LoyaltyBenefitsAdminTests` (autorização, pontos por conciliação, fluxo parceiro/oferta/resgate).

## Relação com outras partes

- **C.2 / C.5:** consumo de benefícios e fidelidade pelo torcedor (benefícios elegíveis: [parte-c2-noticias-beneficios-torcedor.md](parte-c2-noticias-beneficios-torcedor.md)).
- **Conta vs associação:** pontos são da **conta** (`UserId`); regras de benefício usam **Membership** apenas para elegibilidade, sem acoplar permissões administrativas.
