# Parte B.2 — LGPD (gestão e compliance)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.2) e [AGENTS.md](../../AGENTS.md) (LGPD: aceite, versionamento, data/hora).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `LegalDocuments` | Documento lógico por tipo (`TermsOfUse`, `PrivacyPolicy`); **índice único em `Type`** (um documento por tipo). |
| `LegalDocumentVersions` | Versões numeradas; conteúdo texto; status `Draft` / `Published`. Só uma versão publicada por documento (regra na aplicação ao publicar). |
| `UserConsents` | Aceite: `UserId`, `LegalDocumentVersionId`, `AcceptedAt`, `ClientIp` opcional; **único** `(UserId, LegalDocumentVersionId)`. |
| `PrivacyRequests` | Trilha de operações `Export` / `Anonymize`: solicitante, alvo, status, JSON de resultado ou erro. |

Migração EF: `PartB2Lgpd` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões (JWT / matriz role × permissão)

Constantes em `AppTorcedor.Identity.ApplicationPermissions` (espelhadas no frontend):

- `Lgpd.Documentos.Visualizar` / `Lgpd.Documentos.Editar`
- `Lgpd.Consentimentos.Visualizar` / `Lgpd.Consentimentos.Registrar`
- `Lgpd.Dados.Exportar` / `Lgpd.Dados.Anonimizar`

O **Administrador Master** recebe todas no seed (catálogo `ApplicationPermissions.All`).

## API administrativa

Base: `api/admin/lgpd` (JWT + política por permissão). Implementação: MediatR + `ILgpdAdministrationPort` (`LgpdAdministrationService`).

| Método | Rota | Permissão |
|--------|------|------------|
| GET | `/api/admin/lgpd/documents` | `Lgpd.Documentos.Visualizar` |
| GET | `/api/admin/lgpd/documents/{id}` | `Lgpd.Documentos.Visualizar` |
| POST | `/api/admin/lgpd/documents` | `Lgpd.Documentos.Editar` |
| POST | `/api/admin/lgpd/documents/{documentId}/versions` | `Lgpd.Documentos.Editar` |
| POST | `/api/admin/lgpd/legal-document-versions/{versionId}/publish` | `Lgpd.Documentos.Editar` |
| GET | `/api/admin/lgpd/users/{userId}/consents` | `Lgpd.Consentimentos.Visualizar` |
| POST | `/api/admin/lgpd/users/{userId}/consents` | `Lgpd.Consentimentos.Registrar` |
| POST | `/api/admin/lgpd/users/{userId}/export` | `Lgpd.Dados.Exportar` |
| POST | `/api/admin/lgpd/users/{userId}/anonymize` | `Lgpd.Dados.Anonimizar` |

Corpos JSON principais: criação de documento `{ "type": "TermsOfUse" \| "PrivacyPolicy", "title": "..." }`; nova versão `{ "content": "..." }`; registro de consentimento `{ "documentVersionId": "...", "clientIp": null }`.

## Regras de negócio

- **Publicação:** ao publicar uma versão, as demais do mesmo documento voltam para rascunho.
- **Consentimento:** só em versão **publicada**; um registro por par usuário × versão.
- **Exportação:** JSON com dados da conta (`ApplicationUser`), consentimentos, membership (se houver) e contagem de pagamentos.
- **Anonimização:** atualiza e-mail/username para `anon-{userId:N}@removed.local`, remove telefone, nome genérico, `IsActive = false`, revoga **todos** os refresh tokens do usuário.

## Auditoria

`AuditSaveChangesInterceptor` passa a auditar também entidades LGPD (documentos, versões, consentimentos, requisições de privacidade).

## Frontend (backoffice)

Rotas na SPA: ver [docs/frontend/backoffice.md](../frontend/backoffice.md). Serviço HTTP: `frontend/src/features/admin/services/lgpdApi.ts`.

## Testes

- `AppTorcedor.Api.Tests` / `PartB2LgpdTests`: fluxo documento → versão → publicar → consentimento → exportar → anonimizar; torcedor sem permissão; `/me` inclui permissões LGPD para master.
- `AppTorcedor.Application.Tests` / `PublishLegalDocumentVersionCommandHandlerTests`: handler delega ao port.
- Frontend: `lgpdApi.test.ts`, `permissionUtils.test.ts` (área admin com permissão LGPD).

## Próximos passos (fora do escopo B.2)

- Fluxo do **torcedor** (Parte C.1): aceite no cadastro apontando para a versão publicada vigente.
- Evolução jurídica: políticas de retenção, portabilidade estendida e anonimização em tabelas adicionais quando existirem.
