# Parte B.3 — Users (visão administrativa)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.3) e [AGENTS.md](../../AGENTS.md): listagem administrativa de contas, perfil estendido (`UserProfile`), ativação/inativação global da conta e histórico via auditoria. **Não** substitui o módulo B.1 (staff: convites e papéis internos).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `UserProfiles` | 1:1 com `AspNetUsers` (`UserId` PK/FK): `Document` (CPF normalizado, único quando não nulo; índice filtrado), `BirthDate`, `PhotoUrl`, `Address`, `AdministrativeNote`. |

Migração EF: `PartB3UserProfiles` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

## Permissões

- `Usuarios.Visualizar` — listagem, detalhe, histórico de auditoria filtrado por usuário.
- `Usuarios.Editar` — `PATCH .../active`, `PUT .../profile`.

## API administrativa

Base: `api/admin/users` (JWT + política por permissão). Implementação: MediatR + `IUserAdministrationPort` (`UserAdministrationService`).

| Método | Rota | Permissão |
|--------|------|------------|
| GET | `/api/admin/users?search=&isActive=&page=&pageSize=` | `Usuarios.Visualizar` |
| GET | `/api/admin/users/{userId}` | `Usuarios.Visualizar` |
| GET | `/api/admin/users/{userId}/audit-logs?take=` | `Usuarios.Visualizar` |
| PATCH | `/api/admin/users/{userId}/active` | `Usuarios.Editar` |
| PUT | `/api/admin/users/{userId}/profile` | `Usuarios.Editar` |

Corpo do PATCH: `{ "isActive": true }`. Corpo do PUT (campos opcionais; `null` ou omitido = não alterar; string vazia limpa o campo texto): `document`, `birthDate`, `photoUrl`, `address`, `administrativeNote`. O campo `document` segue a mesma regra do torcedor: **só CPF** válido (módulo 11), **normalizado** a 11 dígitos, **único** na tabela (erros `cpf_invalid` / `cpf_already_in_use` como no C.1).

## Regras de negócio

- Listagem inclui **todas** as contas (inclui staff). A coluna `isStaff` indica se o usuário possui alguma role de backoffice (qualquer role exceto apenas `Torcedor`).
- Resumo de **membership** no detalhe e na listagem é **somente leitura** (escopo B.4 permanece separado).
- **Staff** (B.1): convites, troca de roles e ativação restrita a usuários internos continuam em `/api/admin/staff/...`.

## Auditoria

- `UserProfileRecord`: criar/atualizar/remover gera entradas em `AuditLogs` via `AuditSaveChangesInterceptor`.
- `ApplicationUser`: apenas alterações em campos operacionais seguros (`UserName`, `Email`, `Name`, `PhoneNumber`, `IsActive`, `EmailConfirmed`) geram auditoria em `Update`; inclusões/remoções de conta não são auditadas por este interceptor (evita ruído e vazamento de hashes).
- `IAuditLogReadPort.ListForSubjectUserAsync` retorna linhas onde `entityType` é `ApplicationUser` ou `UserProfileRecord` e `entityId` é o `userId`.

## LGPD

- Exportação inclui o objeto `profile` quando existir.
- Anonimização remove a linha em `UserProfiles` após atualizar a conta.

## Frontend (backoffice)

Rotas: [docs/frontend/backoffice.md](../frontend/backoffice.md). Páginas: `UsersListPage`, `UserDetailPage`; serviços em `adminApi.ts`. A página de consentimentos pré-preenche o GUID quando a URL contém `?userId=`.

## Testes

- `AppTorcedor.Application.Tests` / `AdminUsersHandlersTests`: delegação dos handlers ao port.
- `AppTorcedor.Api.Tests` / `PartB3UsersAdminTests`: autorização, busca por documento, detalhe, perfil, auditoria, ativar/desativar torcedor.
