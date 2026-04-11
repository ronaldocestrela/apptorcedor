# Middleware

## Descrição
Pipeline global do Host (ordem em `MiddlewareExtensions`): tratamento de exceções, **CORS dinâmico antes da resolução de tenant**, resolução de tenant, validação da chave do backoffice.

## Estrutura
- `ExceptionHandlingMiddleware.cs` — Problem Details (JSON) para erros e `BusinessRuleValidationException`
- `DynamicCorsMiddleware.cs` — ignora `/health`, `/swagger`, `/scalar` e `/api/webhooks`; em **`/api/backoffice/*`** aplica CORS com **`Backoffice:AllowedOrigins`** (sem resolver tenant): **OPTIONS** → **204** (+ CORS se **`Origin`** permitida); demais métodos acrescentam CORS antes de `next` quando aplicável. Nas rotas de tenant, resolve `TenantContext` via `ITenantResolver` (`X-Tenant-Id` ou, em **OPTIONS** sem header, slug derivado de `Origin` no padrão `{slug}.localhost`); **OPTIONS** responde **204** com CORS se a origem do tenant for permitida; demais métodos acrescentam cabeçalhos CORS antes de `next` quando aplicável
- `TenantResolutionMiddleware.cs` — ignora `/health`, `/swagger`, `/scalar` e **`/api/backoffice`**; se `TenantContext` já estiver em `Items` (preenchido pelo CORS), apenas chama `next`; caso contrário **400** se `X-Tenant-Id` ausente; **404** se tenant não encontrado
- `ApiKeyAuthMiddleware.cs` — apenas para `api/backoffice/*`: **`OPTIONS`** (preflight) segue sem validar `X-Api-Key`; demais métodos: **401** se `X-Api-Key` ausente ou inválido; **503** se `Backoffice:ApiKey` não configurada

## Dependências
- `SocioTorcedor.Modules.Tenancy.Application` (contratos/DTOs), `ITenantResolver` em `Tenancy.Infrastructure`
- `IOptions<BackofficeOptions>` para `ApiKeyAuthMiddleware` e `DynamicCorsMiddleware` (origens do backoffice)
