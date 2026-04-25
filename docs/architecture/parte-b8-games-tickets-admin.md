# Parte B.8 — Games e Tickets (gestão / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.8) e [AGENTS.md](../../AGENTS.md): **CRUD de jogos** (`Games`), **listagem e gestão administrativa de ingressos** com vínculo a **usuário** e **jogo**, integração via **`ITicketProvider`** com **implementação mock** (reserva, compra, consulta/sincronização) e ações de **resgate** no backoffice. **Regras de benefício** ligadas a jogos/ingressos ficam **fora de escopo** nesta entrega (adiadas a B.10 / fluxo torcedor C.4).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `Games` | `Opponent`, `Competition`, `OpponentLogoUrl` (opcional; URL pública da logo do adversário), `GameDate`, `IsActive`, `CreatedAt`. Desativação lógica (`IsActive =0`) em vez de exclusão física quando há histórico de ingressos. |
| `OpponentLogoAssets` | Biblioteca de logos enviadas pelo admin: `PublicUrl` (único), `CreatedAt`. Só URLs registradas aqui podem ser associadas a um jogo (`OpponentLogoUrl`). |
| `Tickets` | `UserId`, `GameId` com **índice único composto** (no máximo um ingresso/solicitação por torcedor por partida); `ExternalTicketId`, `QrCode`, `Status` — ciclo operacional com o provedor (`Reserved` / `Purchased` / `Redeemed`); `RequestStatus` — **solicitação / emissão** no backoffice (`Pending` = pendente, `Issued` = emitido), independente do ciclo; `CreatedAt`, `UpdatedAt`, `RedeemedAt`. Solicitações via torcedor (C.4) e reservas via admin alimentam a mesma tabela. |

Migrações EF: `PartB8GamesTicketsAdmin`, `OpponentLogoGameLibrary`, `TicketRequestStatus` e `TicketsUserGameUnique` (índice único `UserId`+`GameId`) em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões

- `Jogos.Visualizar` — `GET /api/admin/games`, `GET /api/admin/games/{id}`, `GET /api/admin/games/opponent-logos` (biblioteca de logos).
- `Jogos.Criar` — `POST /api/admin/games`.
- `Jogos.Editar` — `PUT /api/admin/games/{id}`, `DELETE /api/admin/games/{id}` (desativa).
- **Upload de logo do adversário:** `POST /api/admin/games/opponent-logos` — política `GamesOpponentLogosUpload` (**`Jogos.Criar` ou `Jogos.Editar`**).
- `Ingressos.Visualizar` — `GET /api/admin/tickets`, `GET /api/admin/tickets/{id}`.
- `Ingressos.Gerenciar` — `POST /api/admin/tickets/reserve`, `POST /api/admin/tickets/{id}/purchase`, `POST /api/admin/tickets/{id}/sync`, `POST /api/admin/tickets/{id}/redeem`, `PATCH /api/admin/tickets/{id}/request-status`.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`).

## API administrativa

Base: `api/admin/games` e `api/admin/tickets` (JWT + política por permissão). CQRS em `AppTorcedor.Application` e implementação em `GameAdministrationService` / `TicketAdministrationService` (`IGameAdministrationPort`, `ITicketAdministrationPort`).

### Jogos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/games?search=&isActive=&page=&pageSize=` | Lista paginada. |
| GET | `/api/admin/games/{gameId}` | Detalhe. |
| GET | `/api/admin/games/opponent-logos?page=&pageSize=` | Lista logos já enviadas (reutilização no formulário). |
| POST | `/api/admin/games/opponent-logos` | `multipart/form-data` campo `file` — registra na biblioteca e retorna `{ "url" }`. |
| POST | `/api/admin/games` | Cria jogo. Corpo JSON pode incluir `opponentLogoUrl` opcional (deve existir em `OpponentLogoAssets`). |
| PUT | `/api/admin/games/{gameId}` | Atualiza jogo (incl. `opponentLogoUrl` ou `null` para limpar). |
| DELETE | `/api/admin/games/{gameId}` | Desativa (`IsActive = false`). |

Respostas de criação: `201` com `{ "gameId" }`; mutações: `204`; `404` não encontrado; `400` validação (ex.: `opponentLogoUrl` não registrado na biblioteca).

### Ingressos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/tickets?userId=&gameId=&status=&requestStatus=&page=&pageSize=` | Lista paginada (join usuário/jogo). `status` filtra o ciclo (`Reserved` / `Purchased` / `Redeemed`); `requestStatus` filtra a solicitação (`Pending` / `Issued`). Cada item inclui `userEmail`, `userName`, `requestStatus` e `membershipPlanName` (última associação com `PlanId` preenchido, por `StartDate` desc.; `null` se inexistente). |
| GET | `/api/admin/tickets/{ticketId}` | Detalhe (mesmos campos de apresentação + `requestStatus`, `membershipPlanName`). |
| POST | `/api/admin/tickets/reserve` | Corpo: `{ "userId", "gameId" }`. Cria ingresso, chama `ITicketProvider.ReserveAsync`, define `RequestStatus = Pending`. **Rejeita** (400) se já existir `Ticket` para o mesmo `userId`+`gameId` (alinhado ao índice único e ao C.4). |
| POST | `/api/admin/tickets/{ticketId}/purchase` | Confirma compra via `ITicketProvider.PurchaseAsync` (status local `Reserved`). |
| POST | `/api/admin/tickets/{ticketId}/sync` | Consulta provedor `ITicketProvider.GetAsync` e alinha QR/status quando aplicável. |
| POST | `/api/admin/tickets/{ticketId}/redeem` | Marca como resgatado (status `Purchased` → `Redeemed`). |
| PATCH | `/api/admin/tickets/{ticketId}/request-status` | Corpo: `{ "requestStatus": "Pending" \| "Issued" }`. Alterna apenas o status de solicitação/ emissão; não altera o ciclo `Reserved`/`Purchased`/`Redeemed`. Requer `Ingressos.Gerenciar`. |

Respostas: `201` na reserva com `{ "ticketId" }`; demais mutações `204`; `404`; `400` transição/validação ou `requestStatus` inválido; `409` conflito opcional.

**Decisão de modelagem:** `TicketStatus` continua a representar o **ciclo** com o provedor (reserva, compra, resgate). `RequestStatus` (enum em `AppTorcedor.Identity`) representa a **solicitação** tratada no atendimento (pendente de emissão vs emitida), exibida no backoffice em português (Pendente / Emitido).

## Integração `ITicketProvider`

Interface em `AppTorcedor.Application.Abstractions`; **`MockTicketProvider`** registrado como **singleton** em DI para manter estado do mock entre requisições HTTP (reserva → compra). Substituição futura por provedor real não deve alterar contratos HTTP do admin.

## Regras de negócio (resumo)

- `opponentLogoUrl` em create/update: se informado, deve ser **exatamente** uma `PublicUrl` existente na tabela `OpponentLogoAssets` (fluxo: upload em `POST .../opponent-logos` ou reutilizar URL listada em `GET .../opponent-logos`).
- Reserva exige **jogo ativo** e **usuário** existente.
- Compra exige ingresso em **`Reserved`** com `ExternalTicketId` preenchido.
- Sincronização consulta o provedor e atualiza **QR**; promove **`Reserved` → `Purchased`** se o snapshot do provedor indicar compra confirmada; não rebaixa status local.
- Resgate administrativo exige **`Purchased`**; preenche `RedeemedAt`.

## Auditoria

Mutações em `GameRecord` e `TicketRecord` geram entradas em `AuditLogs` (interceptor existente).

## Frontend (backoffice)

- Rotas `/admin/games` e `/admin/tickets` (permissões conforme tabela acima).
- Serviços: `frontend/src/features/admin/services/adminApi.ts` (`listAdminTickets` com `requestStatus`, `patchAdminTicketRequestStatus`).
- Tela `TicketsAdminPage`: colunas **Solicitação** (Pendente/Emitido), **Ciclo** (status do provedor), nome e e-mail do torcedor, **Plano** (ou `—`), jogo; filtros de ciclo e de solicitação; com `Ingressos.Gerenciar`, botões **Marcar como emitido** / **Marcar como pendente** no painel de ações (além de compra/sync/resgate). Teste Vitest: `TicketsAdminPage.test.tsx`.

## Testes

- `AppTorcedor.Application.Tests` — `GameAdminHandlersTests`, `TicketAdminHandlersTests`: delegação aos ports.
- `AppTorcedor.Api.Tests` — `PartB8GamesTicketsAdminTests`: autorização, CRUD de jogos, biblioteca/upload de logo do adversário, fluxo reserva → compra → sync → resgate, `requestStatus` + `PATCH .../request-status`.
- `AppTorcedor.Infrastructure.Tests` — `GameAdministrationServiceOpponentLogoTests`: validação de `opponentLogoUrl` vs biblioteca; `TicketAdministrationServiceRequestStatusTests` (plano exibido + alternância de `RequestStatus`).
- `AppTorcedor.Application.Tests` — `UploadOpponentLogoCommandHandlerTests`.

## Relação com outras partes

- **B.10 / C.4:** elegibilidade por plano/benefício e visão torcedor de ingressos.
- **Conta vs associação:** ingresso vincula-se à **conta** (`UserId`); não exige `Membership` ativo na B.8.
