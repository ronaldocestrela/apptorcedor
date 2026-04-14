# Fase base: autenticação (Identity + JWT)

## Objetivo

Estabelecer o monólito inicial com **ASP.NET Core Identity**, **JWT (access + refresh)** persistido em banco, **seed** do usuário **Administrador Master** e **roles** do `AGENTS.md`, além da SPA React com login e renovação automática de token.

## Backend (`backend/`)

### Projetos

| Projeto | Papel |
|--------|--------|
| `AppTorcedor.Identity` | `ApplicationUser`, constantes `SystemRoles` |
| `AppTorcedor.Infrastructure` | `AppDbContext`, `RefreshToken`, `IRefreshTokenStore`, migrations, `IdentityDataSeeder` |
| `AppTorcedor.Api` | Controllers, JWT, pipeline, OpenAPI (`/openapi/v1.json` em Development) |
| `*.Tests` | xUnit: roles + testes de API com `WebApplicationFactory` |

### Configuração

- **SQL Server**: `ConnectionStrings:DefaultConnection` (appsettings, user secrets ou variáveis de ambiente; em Docker Compose use `DATABASE_CONNECTION_STRING` no `.env` apontando para o servidor SQL externo).
- **In-memory (dev rápido)**: `UseInMemoryDatabase: true` em `appsettings.Development.json`.
- **JWT**: seção `Jwt` (`Issuer`, `Audience`, `Key` com **≥ 32 bytes UTF-8**, `AccessTokenMinutes`, `RefreshTokenDays`).
- **Seed admin**: `Seed:AdminMaster:Email` / `Seed:AdminMaster:Password` ou variável de ambiente `ADMIN_MASTER_INITIAL_PASSWORD` (obrigatória fora Development/Testing).

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/auth/login` | Anônimo | Retorna access + refresh + roles |
| POST | `/api/auth/refresh` | Anônimo | Rotação do refresh token |
| POST | `/api/auth/logout` | Anônimo | Revoga refresh token informado |
| GET | `/api/auth/me` | Bearer | Perfil + roles + **permissions** + `requiresProfileCompletion` (C.1) |
| POST | `/api/auth/google` | Anônimo | Corpo `{ idToken, acceptedLegalDocumentVersionIds? }` — login Google; novos usuários exigem consentimentos (ver [parte-c1-cadastro-perfil-torcedor.md](parte-c1-cadastro-perfil-torcedor.md)) |
| POST | `/api/auth/accept-staff-invite` | Anônimo | Corpo `{ token, password, name? }` — cria usuário staff a partir de convite e retorna access + refresh + roles |
| GET | `/api/diagnostics/admin-master-only` | Bearer + permissão | Política `Administracao.Diagnostics` |

### Migrations

Criadas em `AppTorcedor.Infrastructure/Persistence/Migrations`. Design-time usa `AppDbContextFactory` apontando para SQL Server.

### Testes

- `AppTorcedor.Api.Tests`: integração com host `Testing`, banco em memória, JWT e seed via `WebApplicationFactory`.
- `AppTorcedor.Identity.Tests`: contrato das roles iniciais.

## Frontend (`frontend/`)

- **Vite + React + TypeScript**, `axios` com interceptors para **Bearer** e **refresh em 401**.
- Tokens em `sessionStorage` (`shared/auth/authStorage.ts`).
- Rotas: `/login`, `/` (autenticado), `/admin` e sub-rotas (acesso por **permissões**; ver [docs/frontend/backoffice.md](../frontend/backoffice.md)).
- Variável `VITE_API_URL` (padrão em `.env.development`).

### Testes (Vitest)

- `npm test`: exemplo unitário em `authStorage.test.ts`.

## Decisões- Refresh token armazenado como **hash SHA256**; rotação na troca (`refresh`).
- OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) em vez de Swashbuckle, por compatibilidade com dependências OpenAPI 2.x no SDK atual.
- Permissões granulares: claims `permission` no JWT e lista `permissions` em `/api/auth/me`; o frontend não precisa decodificar o token para montar o menu.
