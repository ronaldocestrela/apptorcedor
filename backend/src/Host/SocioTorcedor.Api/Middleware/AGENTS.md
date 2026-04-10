# Middleware

## Descrição
Pipeline global do Host (ordem em `MiddlewareExtensions`): tratamento de exceções, **CORS dinâmico antes da resolução de tenant**, resolução de tenant, validação da chave do backoffice.

## Estrutura
- `ExceptionHandlingMiddleware.cs` — Problem Details (JSON) para erros e `BusinessRuleValidationException`
- `DynamicCorsMiddleware.cs` — ignora `/health`, `/swagger`, `/scalar` e `/api/backoffice`; resolve `TenantContext` via `ITenantResolver` quando necessário (`X-Tenant-Id` ou, em **OPTIONS** sem header, slug derivado de `Origin` no padrão `{slug}.localhost`); **OPTIONS** responde **204** com CORS se a origem for permitida; demais métodos acrescentam cabeçalhos CORS antes de `next` quando aplicável
- `TenantResolutionMiddleware.cs` — ignora `/health`, `/swagger`, `/scalar` e **`/api/backoffice`**; se `TenantContext` já estiver em `Items` (preenchido pelo CORS), apenas chama `next`; caso contrário **400** se `X-Tenant-Id` ausente; **404** se tenant não encontrado
- `ApiKeyAuthMiddleware.cs` — apenas para `api/backoffice/*`: **401** se `X-Api-Key` ausente ou inválido; **503** se `Backoffice:ApiKey` não configurada

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` (contratos/DTOs), `ITenantResolver` em `Tenancy.Infrastructure`
- `IOptions<BackofficeOptions>` para `ApiKeyAuthMiddleware`
