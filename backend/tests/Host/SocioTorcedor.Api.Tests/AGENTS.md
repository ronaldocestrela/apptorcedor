# SocioTorcedor.Api.Tests

## Descrição
Testes unitários dos middlewares do Host (`DefaultHttpContext`, NSubstitute para `ITenantResolver`).

## Estrutura
- `Middleware/TenantResolutionMiddlewareTests.cs`
- `Middleware/DynamicCorsMiddlewareTests.cs`
- `Middleware/ExceptionHandlingMiddlewareTests.cs`
- `GlobalUsings.cs` — `Microsoft.AspNetCore.Http`

## Dependências
- Projeto `SocioTorcedor.Api`; `FrameworkReference` `Microsoft.AspNetCore.App`
