# Middleware

## Descrição
Pipeline global do Host (ordem em `MiddlewareExtensions`): tratamento de exceções, resolução de tenant pelo header **`X-Tenant-Id`** + `ITenantResolver`, CORS dinâmico com base em `TenantContext.AllowedOrigins`.

## Estrutura
- `ExceptionHandlingMiddleware.cs` — Problem Details (JSON) para erros e `BusinessRuleValidationException`
- `TenantResolutionMiddleware.cs` — ignora `/health` e `/swagger`; **400** se `X-Tenant-Id` ausente; **404** se tenant não encontrado
- `DynamicCorsMiddleware.cs` — valida `Origin`; preflight `OPTIONS` com 204; `Access-Control-Allow-Headers` inclui `X-Tenant-Id`

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` (contratos/DTOs), `ITenantResolver` implementado em `Tenancy.Infrastructure`
