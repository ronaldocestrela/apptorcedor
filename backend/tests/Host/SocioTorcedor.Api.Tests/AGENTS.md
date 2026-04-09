# SocioTorcedor.Api.Tests

## Descrição
Testes unitários dos middlewares do Host (`DefaultHttpContext`, NSubstitute para `ITenantResolver`; header **`X-Tenant-Id`** para resolução de tenant).

## Estrutura
- `Middleware/TenantResolutionMiddlewareTests.cs`
- `Middleware/DynamicCorsMiddlewareTests.cs`
- `Middleware/ExceptionHandlingMiddlewareTests.cs`
- `Swagger/TenantHeaderOperationFilterTests.cs` — parâmetro `X-Tenant-Id` no OpenAPI + idempotência do filtro
- `GlobalUsings.cs` — `Microsoft.AspNetCore.Http`

## Dependências
- Projeto `SocioTorcedor.Api`; `FrameworkReference` `Microsoft.AspNetCore.App`
