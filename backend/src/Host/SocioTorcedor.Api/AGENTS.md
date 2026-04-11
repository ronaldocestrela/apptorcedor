# SocioTorcedor.Api

## Descrição
Host ASP.NET Core: composição de módulos (Tenancy, Identity, **Backoffice**), pipeline de middlewares, referência OpenAPI com **Scalar** (`/scalar`; JWT + API key do backoffice documentados no spec) e endpoint de health.

## Estrutura
- `Program.cs` — bootstrap, `Configure<BackofficeOptions>`, `UseSocioTorcedorMiddleware`, autenticação
- `Middleware/` — `ExceptionHandlingMiddleware`, `TenantResolutionMiddleware`, **`ApiKeyAuthMiddleware`**, `DynamicCorsMiddleware`
- `Options/` — `BackofficeOptions` (`Backoffice:ApiKey`, `Backoffice:AllowedOrigins` para CORS do SPA do backoffice)
- `Extensions/` — `ServiceCollectionExtensions`, `MiddlewareExtensions`, `DatabaseMigrationExtensions` (Identity + Membership por tenant; após migrate do Identity: seed de documentos legais placeholder quando vazio e seed de roles **`Socio`** / **`Administrador`**)
- `Swagger/` — filtros Swashbuckle/OpenAPI: `TenantHeaderOperationFilter` (**`X-Tenant-Id`** nas rotas de tenant), **`BackofficeApiKeyOperationFilter`** (segurança **`BackofficeApiKey`** / **`X-Api-Key`** em `api/backoffice/*`)
- `Tenancy/` — `HttpContextTenantContext` (`ICurrentTenantContext`)
- `appsettings*.json` — connection string master, JWT, **`Backoffice:ApiKey`**, **`CORS_BASE_DOMAIN`** (origem automática em novos tenants; vazio → `http://{slug}.localhost:5173`; ver `AGENTS.md` raiz)

**Rotas de tenant:** o slug vai no header **`X-Tenant-Id`**; o middleware resolve o tenant no master. No documento OpenAPI (consumido pelo **Scalar**), esse header aparece nas operações que **não** são `api/backoffice/*`.

**Rotas backoffice (`api/backoffice/*`):** não exigem tenant; o middleware de tenant ignora esses caminhos. CORS é aplicado pelo **`DynamicCorsMiddleware`** com base em **`Backoffice:AllowedOrigins`** (preflight **`OPTIONS`** responde **204** com cabeçalhos CORS quando a **`Origin`** está na lista; origem não listada → **204** sem **`Access-Control-Allow-Origin`**). Requisições que não são preflight exigem **`X-Api-Key`** (mesmo valor que `Backoffice:ApiKey`); **`OPTIONS`** não exige a chave. Na UI do **Scalar**, configure a autenticação para o esquema **BackofficeApiKey** (API key / header **`X-Api-Key`**).

**Docker:** build a partir da raiz `backend/` com `docker compose build` (usa `Dockerfile` na raiz do backend). Compose pode definir `Backoffice__ApiKey`.

## Dependências
- `SocioTorcedor.Modules.Tenancy.*`, `SocioTorcedor.Modules.Identity.*`, **`SocioTorcedor.Modules.Backoffice.*`**, BuildingBlocks Application/Shared/Domain
- `SocioTorcedor.Modules.Tenancy.Infrastructure` (referência ao projeto de infra para DI compartilhada)
