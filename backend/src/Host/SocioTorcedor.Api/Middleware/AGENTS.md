# Middleware

## Descrição
Pipeline global do Host (ordem em `MiddlewareExtensions`): tratamento de exceções, resolução de tenant por `Host` + `ITenantResolver`, CORS dinâmico com base em `TenantContext.AllowedOrigins`.

## Estrutura
- `ExceptionHandlingMiddleware.cs` — Problem Details (JSON) para erros e `BusinessRuleValidationException`
- `TenantResolutionMiddleware.cs` — ignora `/health` e `/swagger`; 404 se subdomínio inválido ou tenant ausente
- `DynamicCorsMiddleware.cs` — valida `Origin`, responde preflight `OPTIONS` com 204

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` (contratos/DTOs), `Tenancy.Infrastructure` (`SubdomainParser`)
