# SocioTorcedor.Api.Tests

## Descrição
Testes unitários dos middlewares do Host (`DefaultHttpContext`, NSubstitute para `ITenantResolver`; header **`X-Tenant-Id`** para resolução de tenant).

## Estrutura
- `Middleware/TenantResolutionMiddlewareTests.cs` — inclui bypass `/api/backoffice`
- `Middleware/DynamicCorsMiddlewareTests.cs`
- `Middleware/ExceptionHandlingMiddlewareTests.cs`
- `Middleware/ApiKeyAuthMiddlewareTests.cs`
- `Swagger/TenantHeaderOperationFilterTests.cs` — `X-Tenant-Id` onde aplicável; exclusão de rotas backoffice
- `Swagger/BackofficeApiKeyOperationFilterTests.cs` — requisito de segurança `BackofficeApiKey` em `api/backoffice/*`
- `GlobalUsings.cs` — `Microsoft.AspNetCore.Http`

## Dependências
- Projeto `SocioTorcedor.Api`; `FrameworkReference` `Microsoft.AspNetCore.App`
