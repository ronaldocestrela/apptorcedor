# SocioTorcedor.Api

## Descrição
Host ASP.NET Core: composição de módulos (Tenancy, Identity), pipeline de middlewares, Swagger com JWT e endpoint de health.

## Estrutura
- `Program.cs` — bootstrap, `UseSocioTorcedorMiddleware`, autenticação
- `Middleware/` — `TenantResolutionMiddleware`, `DynamicCorsMiddleware`, `ExceptionHandlingMiddleware`
- `Extensions/` — `ServiceCollectionExtensions`, `MiddlewareExtensions`
- `Swagger/` — filtros OpenAPI (ex.: `TenantHeaderOperationFilter` para expor **`X-Tenant-Id`** na UI do Swagger)
- `Tenancy/` — `HttpContextTenantContext` (`ICurrentTenantContext`)
- `appsettings*.json` — connection string master, JWT

O cliente envia o **slug do tenant** no header **`X-Tenant-Id`**; o middleware resolve o tenant no banco master e preenche `TenantContext`. No Swagger, o mesmo header aparece como parâmetro em cada operação (via `TenantHeaderOperationFilter`), permitindo testar login/register e APIs autenticadas sem 400 por header ausente.

**Docker:** build a partir da raiz `backend/` com `docker compose build` (usa `Dockerfile` na raiz do backend).

## Dependências
- `SocioTorcedor.Modules.Tenancy.*`, `SocioTorcedor.Modules.Identity.*`, BuildingBlocks Application/Shared/Domain
- `SocioTorcedor.Modules.Tenancy.Infrastructure` (referência ao projeto de infra para DI compartilhada)
