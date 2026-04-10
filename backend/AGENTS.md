# backend

## Descrição
Raiz do backend .NET 10 do **Sócio Torcedor**: **Modular Monolith**, CQRS (MediatR), multitenancy via header **`X-Tenant-Id`** (slug), CORS dinâmico por tenant, **ASP.NET Identity** + JWT, **Backoffice SaaS** (`api/backoffice/*` + **`X-Api-Key`**), Swagger (JWT + API key documentados), Docker + SQL Server.

## Estrutura
- `SocioTorcedor.sln` — solution (todos os projetos)
- `Directory.Build.props` / `Directory.Packages.props` — `net10.0`, Central Package Management
- `src/` — Host, BuildingBlocks, Modules
- `tests/` — testes unitários espelhando `src/`
- `Dockerfile`, `docker-compose.yml`, `.dockerignore` — build e execução da API com SQL Server

## Dependências
- .NET 10 SDK
- SQL Server (`ConnectionStrings:MasterDb` no `appsettings` / variáveis de ambiente no Docker)

## Notas de produto (Fase 3)
- **Status do sócio** (`Membership`): enum e rotas admin documentados em `src/Modules/Membership/AGENTS.md`.
- **LGPD / cadastro** (`Identity`): consentimento no register, leitura e publicação de documentos em `src/Modules/Identity/AGENTS.md`.
