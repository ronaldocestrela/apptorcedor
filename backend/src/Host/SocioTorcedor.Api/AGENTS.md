# SocioTorcedor.Api

## Descrição
Host ASP.NET Core: composição de módulos (Tenancy, Identity), pipeline de middlewares, Swagger com JWT e endpoint de health.

## Estrutura
- `Program.cs` — bootstrap, `UseSocioTorcedorMiddleware`, autenticação
- `Middleware/` — `TenantResolutionMiddleware`, `DynamicCorsMiddleware`, `ExceptionHandlingMiddleware`
- `Extensions/` — `ServiceCollectionExtensions`, `MiddlewareExtensions`
- `Tenancy/` — `HttpContextTenantContext` (`ICurrentTenantContext`)
- `appsettings*.json` — connection string master, JWT

**Docker:** build a partir da raiz `backend/` com `docker compose build` (usa `Dockerfile` na raiz do backend).

## Dependências
- `SocioTorcedor.Modules.Tenancy.*`, `SocioTorcedor.Modules.Identity.*`, BuildingBlocks Application/Shared/Domain
- `SocioTorcedor.Modules.Tenancy.Infrastructure` (parser de subdomínio compartilhado com o Host)
