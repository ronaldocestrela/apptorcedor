# Middleware

## Descrição
Testes unitários dos middlewares do Host: resolução de tenant via header `X-Tenant-Id`, CORS dinâmico (incluindo `X-Tenant-Id` em preflight) e tratamento de exceções (Problem Details).

## Estrutura
- `TenantResolutionMiddlewareTests.cs`
- `DynamicCorsMiddlewareTests.cs`
- `ExceptionHandlingMiddlewareTests.cs`

## Dependências
- Projeto referenciado: `SocioTorcedor.Api`
