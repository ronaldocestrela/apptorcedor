# Parte B.11 — Support (atendimento / chamados) — admin

## Objetivo

Backoffice para **filas de chamados**, **SLA**, **estados**, **respostas** e **histórico** operacional, com autorização por **`Chamados.Responder`**. O vínculo é sempre com **`UserId`** (conta); **Membership** não é usado para autorização.

## Modelo de dados (Infrastructure)

| Tabela | Função |
|--------|--------|
| `SupportTickets` | Chamado: solicitante, fila, assunto, prioridade, status, SLA (`SlaDeadlineUtc`), primeira resposta (`FirstResponseAtUtc`), responsável opcional |
| `SupportTicketMessages` | Mensagens (autor staff ou torcedor, corpo, interna ou não) |
| `SupportTicketMessageAttachments` | Anexos por mensagem (armazenamento em disco; metadados + `StorageKey`) |
| `SupportTicketHistories` | Linha do tempo: `Created`, `StatusChanged`, `Assigned`, `Reply` |

**SLA:** prazo calculado na criação — Normal 48h, High 24h, Urgent 4h (UTC). **SLA estourado** em listagem/detalhe: `UtcNow > SlaDeadlineUtc` e status não é `Resolved` nem `Closed`.

**Estados (`SupportTicketStatus`):** `Open`, `InProgress`, `WaitingUser`, `Resolved`, `Closed`. Transições inválidas retornam erro de negócio na API (`invalid_status_transition`).

## API (`/api/admin/support/tickets`)

Todas as ações exigem política `Permission:Chamados.Responder`.

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/admin/support/tickets` | Lista paginada; query: `queue`, `status`, `assignedUserId`, `unassignedOnly`, `slaBreachedOnly`, `page`, `pageSize` |
| `GET` | `/api/admin/support/tickets/{id}` | Detalhe com mensagens e histórico |
| `POST` | `/api/admin/support/tickets` | Abertura pelo staff (solicitante obrigatório); corpo: `requesterUserId`, `queue`, `subject`, `priority`, `initialMessage?` |
| `POST` | `/api/admin/support/tickets/{id}/reply` | Resposta; corpo: `body`, `isInternal` |
| `POST` | `/api/admin/support/tickets/{id}/assign` | Atribuição; corpo: `agentUserId` (null remove) |
| `POST` | `/api/admin/support/tickets/{id}/status` | Mudança de estado; corpo: `status`, `reason?` |
| `GET` | `/api/admin/support/tickets/{ticketId}/attachments/{attachmentId}` | Download de anexo (com JWT e `Chamados.Responder`) |

Enums JSON em string (configuração global da API com `JsonStringEnumConverter`).

**Mensagens:** cada item em `messages` inclui `attachments[]` com `downloadPath` (URL relativa à API) para uso no SPA com autenticação.

## Dashboard

`GET /api/admin/dashboard` passa a retornar **`openSupportTickets`** como número inteiro: contagem de tickets em `Open`, `InProgress` ou `WaitingUser`.

## Application (CQRS)

- Port: `ISupportAdministrationPort`
- Queries: `ListAdminSupportTickets`, `GetAdminSupportTicket`
- Commands: `CreateAdminSupportTicket`, `ReplyAdminSupportTicket`, `AssignAdminSupportTicket`, `ChangeAdminSupportTicketStatus`

## Frontend

- Rota **`/admin/support`** — permissão `Chamados.Responder`; incluída em `ADMIN_AREA_PERMISSIONS` para liberar o shell admin a perfil só de atendimento.
- Serviços em `frontend/src/features/admin/services/adminApi.ts` (funções `*AdminSupport*`, incluindo `fetchAdminSupportAttachmentBlob` para miniatura e modal de imagens no detalhe do chamado).
- Nas mensagens do detalhe, anexos de imagem usam o mesmo componente de lista que o torcedor (`SupportTicketAttachmentList`), com pele clara no admin.

## Testes

- `backend/tests/AppTorcedor.Application.Tests/SupportAdminHandlersTests.cs` — delegação aos ports.
- `backend/tests/AppTorcedor.Api.Tests/PartB11SupportAdminTests.cs` — autorização, fluxo completo, conflito em resposta a fechado, transição inválida.
- `frontend/src/features/admin/services/adminApi.support.test.ts` — chamada HTTP da listagem e `fetchAdminSupportAttachmentBlob`.

## Observações

- Abertura **pelo torcedor** (self-service) permanece na Parte C.6; B.11 cobre operação administrativa e abertura pelo staff quando necessário.
