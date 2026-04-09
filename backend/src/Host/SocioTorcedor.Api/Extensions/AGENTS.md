# Extensions

## Descrição
Registro de serviços da API, pipeline HTTP e migrations na subida.

## Estrutura
- `MiddlewareExtensions.cs` — ordem: exceções → tenant → **API key backoffice** → CORS dinâmico
- `ServiceCollectionExtensions.cs` — controllers (Identity + Backoffice), MediatR, **Swagger** (Bearer + **BackofficeApiKey**), `TenantHeaderOperationFilter`, `BackofficeApiKeyOperationFilter`, módulos Tenancy, Identity, **Backoffice**
- `DatabaseMigrationExtensions.cs` — `MasterDbContext` (Tenancy), **`BackofficeMasterDbContext`**, depois bancos tenant (Identity)

## Dependências
- Pasta pai: `src/Host/SocioTorcedor.Api`
