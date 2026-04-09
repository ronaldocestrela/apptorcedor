# backend

## Descrição
Raiz do backend .NET 10 do **Sócio Torcedor** (Fase 1 — Fundação): **Modular Monolith**, CQRS (MediatR), multitenancy via header **`X-Tenant-Id`** (slug do tenant), CORS dinâmico por tenant, **ASP.NET Identity** + JWT, Swagger, Docker + SQL Server.

## Estrutura
- `SocioTorcedor.sln` — solution (todos os projetos)
- `Directory.Build.props` / `Directory.Packages.props` — `net10.0`, Central Package Management
- `src/` — Host, BuildingBlocks, Modules
- `tests/` — testes unitários espelhando `src/`
- `Dockerfile`, `docker-compose.yml`, `.dockerignore` — build e execução da API com SQL Server

## Dependências
- .NET 10 SDK
- SQL Server (`ConnectionStrings:MasterDb` no `appsettings` / variáveis de ambiente no Docker)
