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

- **SQL Server**: `ConnectionStrings:DefaultConnection` (ver `docker-compose.yml` na raiz).
- **In-memory (dev rápido)**: `UseInMemoryDatabase: true` em `appsettings.Development.json`.
- **JWT**: seção `Jwt` (`Issuer`, `Audience`, `Key` com **≥ 32 bytes UTF-8**, `AccessTokenMinutes`, `RefreshTokenDays`).
- **Seed admin**: `Seed:AdminMaster:Email` / `Seed:AdminMaster:Password` ou variável de ambiente `ADMIN_MASTER_INITIAL_PASSWORD` (obrigatória fora Development/Testing).

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/api/auth/login` | Anônimo | Retorna access + refresh + roles |
| POST | `/api/auth/refresh` | Anônimo | Rotação do refresh token |
| POST | `/api/auth/logout` | Anônimo | Revoga refresh token informado |
| GET | `/api/auth/me` | Bearer | Perfil + roles |
| GET | `/api/diagnostics/admin-master-only` | Bearer + role | Valida política `Administrador Master` |

### Migrations

Criadas em `AppTorcedor.Infrastructure/Persistence/Migrations`. Design-time usa `AppDbContextFactory` apontando para SQL Server.

### Testes

- `AppTorcedor.Api.Tests`: integração com host `Testing`, banco em memória, JWT e seed via `WebApplicationFactory`.
- `AppTorcedor.Identity.Tests`: contrato das roles iniciais.

## Frontend (`frontend/`)

- **Vite + React + TypeScript**, `axios` com interceptors para **Bearer** e **refresh em 401**.
- Tokens em `sessionStorage` (`shared/auth/authStorage.ts`).
- Rotas: `/login`, `/` (autenticado), `/admin` (role **Administrador Master**).
- Variável `VITE_API_URL` (padrão em `.env.development`).

### Testes (Vitest)

- `npm test`: exemplo unitário em `authStorage.test.ts`.

## Decisões- Refresh token armazenado como **hash SHA256**; rotação na troca (`refresh`).
- OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) em vez de Swashbuckle, por compatibilidade com dependências OpenAPI 2.x no SDK atual.
- `Membership` e permissões granulares **fora** desta fase; apenas **roles** no token.
