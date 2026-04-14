# Parte C.6 — Suporte (chamados) — torcedor

## Objetivo

Permitir que o **usuário autenticado** abra chamados de suporte, acompanhe status, troque mensagens com o atendimento, anexe arquivos e **cancele** ou **reabra** chamados. **Não** exige `Membership` ativo: basta conta (JWT). Autorização administrativa (`Chamados.Responder`) permanece apenas no backoffice (B.11).

## Relação com B.11

- Mesmas tabelas: `SupportTickets`, `SupportTicketMessages`, `SupportTicketHistories`.
- Nova tabela: `SupportTicketMessageAttachments` (anexos por mensagem; `StorageKey` pode apontar para provider local ou Cloudinary).
- O torcedor **não vê** mensagens internas nem entradas de histórico de respostas internas.

## API (`/api/support/tickets`)

Todas as rotas exigem `[Authorize]` (JWT). Não usam permissões granulares de admin.

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/support/tickets` | Lista paginada dos chamados do usuário (`status`, `page`, `pageSize`) |
| `GET` | `/api/support/tickets/{id}` | Detalhe (mensagens públicas, histórico filtrado, anexos com `downloadUrl`) |
| `POST` | `/api/support/tickets` | Abertura (`multipart/form-data`: `queue`, `subject`, `priority`, `initialMessage?`, `attachments`) |
| `POST` | `/api/support/tickets/{id}/reply` | Resposta (`body?`, `attachments`; texto ou anexo obrigatório) |
| `POST` | `/api/support/tickets/{id}/cancel` | Encerra (`Closed`) quando permitido pela máquina de estados |
| `POST` | `/api/support/tickets/{id}/reopen` | `Closed` → `Open` |
| `GET` | `/api/support/tickets/{id}/attachments/{attachmentId}` | Download (autenticado; só o solicitante do chamado) |

Limite de corpo da requisição: 32 MB no controller. Tipos de anexo aceitos: `image/jpeg`, `image/png`, `image/webp`, `application/pdf` (configurável por armazenamento).

## Staff — download de anexo

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/admin/support/tickets/{ticketId}/attachments/{attachmentId}` | Download com política `Chamados.Responder` |

## Application (CQRS)

- Porta: `ISupportTorcedorPort` (`AppTorcedor.Application.Abstractions`).
- Queries: `ListMySupportTickets`, `GetMySupportTicket`.
- Commands: `CreateMySupportTicket`, `ReplyMySupportTicket`, `CancelMySupportTicket`, `ReopenMySupportTicket`.
- Download (solicitante): `GetSupportAttachmentDownloadQuery` → `ISupportAdministrationPort.GetSupportAttachmentDownloadAsync` + checagem de `RequesterUserId`.
- Download (staff): `GetAdminSupportAttachmentDownloadQuery`.

## Infrastructure

- `SupportTorcedorService` — regras de isolamento por `RequesterUserId`, SLA igual a B.11, transações apenas quando `Database.IsRelational()` (in-memory sem transação; compensação parcial em falhas de anexo na criação).
- `ISupportTicketAttachmentStorage` com seleção por provider (`SupportTicketAttachments:Provider`):
	- `LocalSupportTicketAttachmentStorage` grava em `Data/support-attachments` (padrão).
	- `CloudinarySupportTicketAttachmentStorage` grava em Cloudinary com chave de storage interna (`cloudinary|...`) e mantém o download autenticado via API (sem expor rota pública no contrato).
- Tipos no Cloudinary: imagens como `image` e PDF como `raw`.
- Em falha parcial de upload de múltiplos anexos, o serviço faz cleanup best effort dos arquivos já gravados antes de retornar erro de validação.
- `SupportTicketStateMachine` — SLA e transições compartilhadas com o serviço admin.

## Frontend (SPA)

- Rotas: `/support`, `/support/:ticketId`.
- Cliente: `frontend/src/features/torcedor/torcedorSupportApi.ts` (anexos via `FormData`; download via `blob` + JWT pelo Axios).
- Backoffice: mensagens exibem anexos com `downloadAdminSupportAttachment` (`adminApi.ts`).

## Testes

- `backend/tests/AppTorcedor.Application.Tests/SupportTorcedorHandlersTests.cs`
- `backend/tests/AppTorcedor.Api.Tests/PartC6SupportTorcedorTests.cs`
- `frontend/src/features/torcedor/torcedorSupportApi.test.ts`

## Referências

- [parte-b11-support-admin.md](parte-b11-support-admin.md)
- [AGENTS.md](../../AGENTS.md)
- [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md)
