# Middleware

## Descrição
Testes unitários dos middlewares do Host: resolução de tenant via header `X-Tenant-Id`, CORS dinâmico (incluindo `X-Tenant-Id` em preflight) e tratamento de exceções (Problem Details).

## Estrutura
- `TenantResolutionMiddlewareTests.cs`
- `DynamicCorsMiddlewareTests.cs` — CORS por tenant; **`/api/backoffice/*`** com `Backoffice:AllowedOrigins`
- `ApiKeyAuthMiddlewareTests.cs` — **`OPTIONS`** no backoffice sem `X-Api-Key`
- `ExceptionHandlingMiddlewareTests.cs`

## Dependências
- Projeto referenciado: `SocioTorcedor.Api`
