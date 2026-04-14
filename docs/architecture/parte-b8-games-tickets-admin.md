# Parte B.8 — Games e Tickets (gestão / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.8) e [AGENTS.md](../../AGENTS.md): **CRUD de jogos** (`Games`), **listagem e gestão administrativa de ingressos** com vínculo a **usuário** e **jogo**, integração via **`ITicketProvider`** com **implementação mock** (reserva, compra, consulta/sincronização) e ações de **resgate** no backoffice. **Regras de benefício** ligadas a jogos/ingressos ficam **fora de escopo** nesta entrega (adiadas a B.10 / fluxo torcedor C.4).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `Games` | `Opponent`, `Competition`, `GameDate`, `IsActive`, `CreatedAt`. Desativação lógica (`IsActive =0`) em vez de exclusão física quando há histórico de ingressos. |
| `Tickets` | `UserId`, `GameId`, `ExternalTicketId`, `QrCode`, `Status` (`Reserved` / `Purchased` / `Redeemed`), `CreatedAt`, `UpdatedAt`, `RedeemedAt`. |

Migração EF: `PartB8GamesTicketsAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões

- `Jogos.Visualizar` — `GET /api/admin/games`, `GET /api/admin/games/{id}`.
- `Jogos.Criar` — `POST /api/admin/games`.
- `Jogos.Editar` — `PUT /api/admin/games/{id}`, `DELETE /api/admin/games/{id}` (desativa).
- `Ingressos.Visualizar` — `GET /api/admin/tickets`, `GET /api/admin/tickets/{id}`.
- `Ingressos.Gerenciar` — `POST /api/admin/tickets/reserve`, `POST /api/admin/tickets/{id}/purchase`, `POST /api/admin/tickets/{id}/sync`, `POST /api/admin/tickets/{id}/redeem`.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`).

## API administrativa

Base: `api/admin/games` e `api/admin/tickets` (JWT + política por permissão). CQRS em `AppTorcedor.Application` e implementação em `GameAdministrationService` / `TicketAdministrationService` (`IGameAdministrationPort`, `ITicketAdministrationPort`).

### Jogos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/games?search=&isActive=&page=&pageSize=` | Lista paginada. |
| GET | `/api/admin/games/{gameId}` | Detalhe. |
| POST | `/api/admin/games` | Cria jogo. |
| PUT | `/api/admin/games/{gameId}` | Atualiza jogo. |
| DELETE | `/api/admin/games/{gameId}` | Desativa (`IsActive = false`). |

Respostas de criação: `201` com `{ "gameId" }`; mutações: `204`; `404` não encontrado; `400` validação.

### Ingressos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/tickets?userId=&gameId=&status=&page=&pageSize=` | Lista paginada (join usuário/jogo). |
| GET | `/api/admin/tickets/{ticketId}` | Detalhe. |
| POST | `/api/admin/tickets/reserve` | Corpo: `{ "userId", "gameId" }`. Cria ingresso, chama `ITicketProvider.ReserveAsync`. |
| POST | `/api/admin/tickets/{ticketId}/purchase` | Confirma compra via `ITicketProvider.PurchaseAsync` (status local `Reserved`). |
| POST | `/api/admin/tickets/{ticketId}/sync` | Consulta provedor `ITicketProvider.GetAsync` e alinha QR/status quando aplicável. |
| POST | `/api/admin/tickets/{ticketId}/redeem` | Marca como resgatado (status `Purchased` → `Redeemed`). |

Respostas: `201` na reserva com `{ "ticketId" }`; demais mutações `204`; `404`; `400` transição/validação; `409` conflito opcional.

## Integração `ITicketProvider`

Interface em `AppTorcedor.Application.Abstractions`; **`MockTicketProvider`** registrado como **singleton** em DI para manter estado do mock entre requisições HTTP (reserva → compra). Substituição futura por provedor real não deve alterar contratos HTTP do admin.

## Regras de negócio (resumo)

- Reserva exige **jogo ativo** e **usuário** existente.
- Compra exige ingresso em **`Reserved`** com `ExternalTicketId` preenchido.
- Sincronização consulta o provedor e atualiza **QR**; promove **`Reserved` → `Purchased`** se o snapshot do provedor indicar compra confirmada; não rebaixa status local.
- Resgate administrativo exige **`Purchased`**; preenche `RedeemedAt`.

## Auditoria

Mutações em `GameRecord` e `TicketRecord` geram entradas em `AuditLogs` (interceptor existente).

## Frontend (backoffice)

- Rotas `/admin/games` e `/admin/tickets` (permissões conforme tabela acima).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.

## Testes

- `AppTorcedor.Application.Tests` — `GameAdminHandlersTests`, `TicketAdminHandlersTests`: delegação aos ports.
- `AppTorcedor.Api.Tests` — `PartB8GamesTicketsAdminTests`: autorização, CRUD de jogos, fluxo reserva → compra → sync → resgate.

## Relação com outras partes

- **B.10 / C.4:** elegibilidade por plano/benefício e visão torcedor de ingressos.
- **Conta vs associação:** ingresso vincula-se à **conta** (`UserId`); não exige `Membership` ativo na B.8.
