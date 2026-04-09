# Middleware

## Descrição
Pipeline global do Host (ordem em `MiddlewareExtensions`): tratamento de exceções, resolução de tenant, validação da chave do backoffice, CORS dinâmico.

## Estrutura
- `ExceptionHandlingMiddleware.cs` — Problem Details (JSON) para erros e `BusinessRuleValidationException`
- `TenantResolutionMiddleware.cs` — ignora `/health`, `/swagger` e **`/api/backoffice`**; **400** se `X-Tenant-Id` ausente nas demais rotas; **404** se tenant não encontrado
- `ApiKeyAuthMiddleware.cs` — apenas para `api/backoffice/*`: **401** se `X-Api-Key` ausente ou inválido; **503** se `Backoffice:ApiKey` não configurada
- `DynamicCorsMiddleware.cs` — ignora também `/api/backoffice`; valida `Origin` quando há `TenantContext`; `Access-Control-Allow-Headers` inclui `X-Tenant-Id` e **`X-Api-Key`**

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` (contratos/DTOs), `ITenantResolver` em `Tenancy.Infrastructure`
- `IOptions<BackofficeOptions>` para `ApiKeyAuthMiddleware`
