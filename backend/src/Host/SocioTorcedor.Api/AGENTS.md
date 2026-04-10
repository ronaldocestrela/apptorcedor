# SocioTorcedor.Api

## Descrição
Host ASP.NET Core: composição de módulos (Tenancy, Identity, **Backoffice**), pipeline de middlewares, Swagger (JWT + API key do backoffice) e endpoint de health.

## Estrutura
- `Program.cs` — bootstrap, `Configure<BackofficeOptions>`, `UseSocioTorcedorMiddleware`, autenticação
- `Middleware/` — `ExceptionHandlingMiddleware`, `TenantResolutionMiddleware`, **`ApiKeyAuthMiddleware`**, `DynamicCorsMiddleware`
- `Options/` — `BackofficeOptions` (`Backoffice:ApiKey`)
- `Extensions/` — `ServiceCollectionExtensions`, `MiddlewareExtensions`, `DatabaseMigrationExtensions` (Identity + Membership por tenant; seed de documentos legais placeholder após migrate do Identity quando vazio)
- `Swagger/` — `TenantHeaderOperationFilter` (**`X-Tenant-Id`** nas rotas de tenant), **`BackofficeApiKeyOperationFilter`** (segurança **`BackofficeApiKey`** / **`X-Api-Key`** em `api/backoffice/*`)
- `Tenancy/` — `HttpContextTenantContext` (`ICurrentTenantContext`)
- `appsettings*.json` — connection string master, JWT, **`Backoffice:ApiKey`**

**Rotas de tenant:** o slug vai no header **`X-Tenant-Id`**; o middleware resolve o tenant no master. No Swagger, esse header aparece nas operações que **não** são `api/backoffice/*`.

**Rotas backoffice (`api/backoffice/*`):** não exigem tenant; o middleware de tenant ignora esses caminhos. A API exige **`X-Api-Key`** (mesmo valor que `Backoffice:ApiKey`). No Swagger UI use **Authorize** → esquema **BackofficeApiKey** (ou o nome exibido para a chave).

**Docker:** build a partir da raiz `backend/` com `docker compose build` (usa `Dockerfile` na raiz do backend). Compose pode definir `Backoffice__ApiKey`.

## Dependências
- `SocioTorcedor.Modules.Tenancy.*`, `SocioTorcedor.Modules.Identity.*`, **`SocioTorcedor.Modules.Backoffice.*`**, BuildingBlocks Application/Shared/Domain
- `SocioTorcedor.Modules.Tenancy.Infrastructure` (referência ao projeto de infra para DI compartilhada)
