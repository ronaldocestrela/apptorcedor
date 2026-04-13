# Parte B.9 — News (gestão de conteúdo / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.9) e [AGENTS.md](../../AGENTS.md): **editoria administrativa** de notícias (criar, editar, publicar, despublicar) e **notificações in-app** com envio imediato ou agendamento por data/hora. O **feed público** do torcedor (`GetNewsFeed`) permanece na Parte **C.2**.

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `NewsArticles` | `Title`, `Summary`, `Content`, `Status` (`Draft` / `Published` / `Unpublished`), `CreatedAt`, `UpdatedAt`, `PublishedAt`, `UnpublishedAt`. |
| `InAppNotifications` | `UserId`, `NewsArticleId`, `Title`, `PreviewText`, `ScheduledAt`, `DispatchedAt`, `Status` (`Pending` / `Dispatched`). |

Migração EF: `PartB9NewsAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões

- `Noticias.Publicar` — todos os endpoints administrativos de notícias e notificações in-app descritos abaixo.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`). Outros perfis recebem `Noticias.Publicar` apenas pela matriz **role × permissão** no backoffice.

## API administrativa

Base: `api/admin/news` (JWT + política por permissão). CQRS em `AppTorcedor.Application`; persistência em `NewsAdministrationService` (`INewsAdministrationPort`). Despacho de pendentes: `IInAppNotificationDispatchService` + `InAppNotificationDispatchHostedService` (intervalo ~30s; inativo no ambiente `Testing`).

### Notícias

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/news?search=&status=&page=&pageSize=` | Lista paginada (`status`: `Draft`, `Published`, `Unpublished`). |
| GET | `/api/admin/news/{newsId}` | Detalhe. |
| POST | `/api/admin/news` | Cria rascunho (`Draft`). Corpo: `{ title, summary?, content }`. Resposta `201` `{ "newsId" }`. |
| PUT | `/api/admin/news/{newsId}` | Atualiza conteúdo. `204`. |
| POST | `/api/admin/news/{newsId}/publish` | Publica (`Draft` ou `Unpublished` → `Published`). `204`. |
| POST | `/api/admin/news/{newsId}/unpublish` | Despublica (`Published` → `Unpublished`). `204`. |

Respostas de erro comuns: `404` não encontrado; `400` validação ou transição inválida.

### Notificações in-app

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/admin/news/{newsId}/notifications` | Corpo: `{ scheduledAt?, userIds? }`. Exige notícia **publicada**. `userIds` vazio/ausente = todas as contas **ativas** (`ApplicationUser.IsActive`). Se `scheduledAt` for nulo ou já vencido, grava como **disparado** na hora; caso contrário, **pendente** até o processador. `204` em sucesso. |

Erros: `400` alvo inválido ou notícia não publicada; `404` notícia inexistente.

## Regras de negócio (resumo)

- Notificação vincula-se à **conta** (`UserId`); não depende de `Membership` ativo.
- Publicação idempotente se já estiver `Published`.
- Despublicação só a partir de `Published`.

## Auditoria

Mutações em `NewsArticleRecord` e `InAppNotificationRecord` geram entradas em `AuditLogs` (interceptor existente).

## Frontend (backoffice)

- Rota `/admin/news` (permissão `Noticias.Publicar`).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.
- `Noticias.Publicar` incluída em `ADMIN_AREA_PERMISSIONS` para liberar o shell admin a perfis só de conteúdo.

## Testes

- `AppTorcedor.Application.Tests` — `NewsAdminHandlersTests`: handlers delegam ao port.
- `AppTorcedor.Api.Tests` — `PartB9NewsAdminTests`: autorização, fluxo editorial, notificação com notícia publicada, processamento de agendadas via `IInAppNotificationDispatchService`.

## Relação com outras partes

- **C.2:** consumo do feed e detalhe pelo torcedor.
- **Conta vs associação:** notícia e notificação não acoplam regras de sócio; segmentação futura é evolução opcional.
