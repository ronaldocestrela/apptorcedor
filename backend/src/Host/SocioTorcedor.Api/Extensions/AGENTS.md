# Extensions

## Descrição
Registro de serviços da API, pipeline HTTP e migrations na subida.

## Estrutura
- `MiddlewareExtensions.cs` — ordem: exceções → tenant → **API key backoffice** → CORS dinâmico
- `ServiceCollectionExtensions.cs` — `AddApplicationPart` dos assemblies de API (Identity, Tenancy, Backoffice, **Membership** — inclui `AuthController`, **`LegalDocumentsController`**, `MembersController`, **`PlansController`**), MediatR, **Swagger** (Bearer + **BackofficeApiKey**), `TenantHeaderOperationFilter`, `BackofficeApiKeyOperationFilter`, módulos Tenancy, Identity, **Backoffice**, **Membership**
- `DatabaseMigrationExtensions.cs` — `MasterDbContext` (Tenancy), **`BackofficeMasterDbContext`**, depois bancos tenant (Identity + Membership); após `Migrate` do Identity por tenant, **`LegalDocumentTenantSeed`** garante versões iniciais placeholder se a tabela estiver vazia

## Dependências
- Pasta pai: `src/Host/SocioTorcedor.Api`
