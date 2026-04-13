# Parte C.5 — Fidelidade (torcedor)

Consumo de **saldo de pontos** e **ranking** pelo usuário autenticado (canal torcedor), alinhado ao modelo de ledger e agregações descritos em [parte-b10-loyalty-benefits-admin.md](parte-b10-loyalty-benefits-admin.md) e ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.5).

## Regras de negócio

- Pontos são da **conta** (`UserId` no `LoyaltyPointLedgerEntries`); não há exigência de permissão administrativa nem vínculo com `Membership` para **ler** saldo/ranking.
- Agregação igual à visão admin: soma por usuário, exclui totais **0** do ranking; ordenação **total decrescente**, desempate por **`UserId` crescente** (mesmo critério de `LoyaltyAdministrationService.BuildRankingAsync`).
- **Mês** do ranking e do resumo “mensal” usa intervalo **UTC** \([primeiro dia 00:00, primeiro dia do mês seguinte)\).
- **Posição no ranking** (`monthlyRank` / `allTimeRank` no resumo): `null` se o usuário não entra no ranking (ex.: soma 0 no período ou sem lançamentos).
- Leaderboard torcedor expõe **nome** (`ApplicationUser.Name`) nos itens, sem e-mail.

## API (JWT, `[Authorize]` apenas)

Base: `api/loyalty`.

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/loyalty/me/summary` | `totalPoints`, `monthlyPoints`, `monthlyRank`, `allTimeRank`, `asOfUtc`. |
| GET | `/api/loyalty/rankings/monthly?year=&month=&page=&pageSize=` | Página do ranking mensal + `me` (posição do usuário logado, se houver). |
| GET | `/api/loyalty/rankings/all-time?page=&pageSize=` | Página do ranking acumulado + `me`. |

`month` inválido (`<1` ou `> 12`): resposta com `totalCount = 0`, `items = []`, `me = null` (sem erro HTTP).

## Implementação

- **Porta:** `ILoyaltyTorcedorReadPort` em `AppTorcedor.Application/Abstractions/LoyaltyTorcedorContracts.cs`.
- **Infra:** `LoyaltyTorcedorReadService` em `AppTorcedor.Infrastructure/Services/Loyalty/LoyaltyTorcedorReadService.cs` (registrada em `DependencyInjection`).
- **CQRS (Torcedor):** queries em `AppTorcedor.Application/Modules/Torcedor/Queries/...`.
- **Controller:** `TorcedorLoyaltyController` — contratos HTTP em `AppTorcedor.Api/Contracts/TorcedorConsumptionContracts.cs`.

## Frontend (web)

- Rota protegida: `/loyalty` (`LoyaltyPage`).
- Cliente: `frontend/src/features/torcedor/torcedorLoyaltyApi.ts`.
- Atalho no painel inicial (`DashboardPage`): link “Fidelidade”.

## Testes

- **Application:** `LoyaltyTorcedorHandlersTests` (delegação aos ports).
- **API:** `PartC5TorcedorLoyaltyTests` (401 sem token, fluxo com ajuste manual admin + leitura membro, mês inválido).
- **Frontend:** `torcedorLoyaltyApi.test.ts`, `LoyaltyPage.test.tsx`.
