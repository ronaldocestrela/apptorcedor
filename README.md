# App Torcedor (single tenant)

Monólito modular para **sócio torcedor** de um único clube. Esta entrega inicial cobre **Identity**, **JWT (access + refresh)**, **seed do Administrador Master + roles**, API .NET 10 e SPA React (Vite).

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ (para o frontend)
- Docker (opcional, para SQL Server)

## Subir SQL Server (opcional)

```bash
docker compose up -d
```

A connection string padrão em `backend/src/AppTorcedor.Api/appsettings.json` aponta para `localhost,1433` com a senha de exemplo alinhada ao `docker-compose.yml`.

## Backend

```bash
cd backend
dotnet restore
dotnet test
dotnet run --project src/AppTorcedor.Api/AppTorcedor.Api.csproj
```

- API HTTP: `http://localhost:5031` (perfil `http` em `Properties/launchSettings.json`).
- Development usa **banco em memória** por padrão (`UseInMemoryDatabase` em `appsettings.Development.json`). Para SQL Server, defina `UseInMemoryDatabase: false` e garanta o banco acessível.
- OpenAPI (Development): `GET /openapi/v1.json`
- Senha inicial do admin em dev: `Seed:AdminMaster:Password` em `appsettings.Development.json` ou `ADMIN_MASTER_INITIAL_PASSWORD`.

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Configure `VITE_API_URL` (veja `frontend/.env.development`).

## Documentação

- Regras do produto e arquitetura alvo: [AGENTS.md](AGENTS.md)
- Detalhes desta fase de autenticação: [docs/architecture/auth-bootstrap.md](docs/architecture/auth-bootstrap.md)

## Testes

```bash
cd backend && dotnet test
cd frontend && npm test
```
