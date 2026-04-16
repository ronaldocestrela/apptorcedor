# Parte C.4 — Jogos e ingressos (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.4) e [AGENTS.md](../../AGENTS.md): **catálogo de jogos ativos** e **listagem, detalhe e resgate de ingressos** do usuário autenticado, com **isolamento por conta** (sem parâmetro `userId` no cliente; o `UserId` vem só do JWT). A gestão administrativa permanece em [parte-b8-games-tickets-admin.md](parte-b8-games-tickets-admin.md).

## Separação de responsabilidades

- **Conta:** `UserId` obtido exclusivamente do JWT nas rotas de ingressos; jogos são somente leitura e não exigem escopo além de `[Authorize]`.
- **Permissões de backoffice:** não utilizadas nas rotas torcedor (`Jogos.*` / `Ingressos.*` continuam só em `api/admin/*`).
- **Associação (Membership):** C.4 **não** exige sócio ativo para listar jogos ou ver ingressos já vinculados à conta (paridade com B.8: ingresso amarra-se ao `UserId`).

## Backend

### Portas

| Porta | Implementação | Função |
|--------|----------------|--------|
| `IGameTorcedorReadPort` | `GameTorcedorReadService` | Lista apenas jogos com `IsActive == true`. |
| `ITicketTorcedorPort` | `TicketTorcedorService` | Lista/detalha ingressos do usuário; resgate com checagem de propriedade. |

Registro em `AppTorcedor.Infrastructure/DependencyInjection.cs`.

### CQRS (`AppTorcedor.Application`)

- `ListTorcedorGamesQuery` / `ListTorcedorGamesQueryHandler`
- `ListMyTicketsQuery` / `ListMyTicketsQueryHandler`
- `GetMyTicketQuery` / `GetMyTicketQueryHandler`
- `RedeemMyTicketCommand` / `RedeemMyTicketCommandHandler`

Contratos e DTOs: `AppTorcedor.Application/Abstractions/TorcedorGamesTicketsContracts.cs`.

### API (torcedor)

Base: **`api/games`** e **`api/tickets`** — `[Authorize]` no controlador (mesmo padrão de `api/news`).

| Método | Rota | Descrição |
|--------|------|------------|
| GET | `/api/games?search=&page=&pageSize=` | Jogos **ativos** (paginação). Cada item inclui `opponentLogoUrl` (opcional) para exibição no app. |
| GET | `/api/tickets?gameId=&status=&page=&pageSize=` | Ingressos do usuário autenticado. |
| GET | `/api/tickets/{ticketId}` | Detalhe **somente se** o ingresso pertencer ao usuário; caso contrário `404`. |
| POST | `/api/tickets/{ticketId}/redeem` | Resgate `Purchased` → `Redeemed` (mesma regra de negócio da B.8 + `ILoyaltyPointsTriggerPort`); `404` se ingresso inexistente ou de outro usuário; `400` se transição inválida. |

Contratos HTTP (camelCase): `AppTorcedor.Api/Contracts/TorcedorConsumptionContracts.cs` (tipos `TorcedorGame*`, `TorcedorTicket*`).

Controladores: `TorcedorGamesController`, `TorcedorTicketsController`.

## Frontend (web)

- Rotas: `/games` (`GamesPage`), `/tickets` (`MyTicketsPage`), registradas em `frontend/src/app/App.tsx`; links no `DashboardPage`.
- Clientes: `frontend/src/features/torcedor/torcedorGamesApi.ts`, `torcedorTicketsApi.ts`.

## Testes

- **Application:** `TorcedorGamesTicketsHandlersTests` — delegação aos ports.
- **API:** `PartC4TorcedorGamesTicketsTests` — auth, jogos ativos vs inativos, `opponentLogoUrl` na listagem, fluxo listagem/detalhe/resgate, isolamento entre usuários, resgate inválido em `Reserved`.
- **Frontend:** `torcedorGamesTicketsApi.test.ts` — chamadas Axios.

## Referências

- B.8 (admin): [parte-b8-games-tickets-admin.md](parte-b8-games-tickets-admin.md).
