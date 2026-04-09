# Swagger (Host)

## Descrição
Extensões do **Swashbuckle** para o OpenAPI: documentação da API, segurança **Bearer (JWT)**, **API key do backoffice**, e header de tenant onde aplicável.

## Arquivos
- `TenantHeaderOperationFilter.cs` — adiciona o parâmetro de header **`X-Tenant-Id`** (obrigatório) nas operações cujo `RelativePath` **não** começa com `api/backoffice/`, alinhado ao `TenantResolutionMiddleware`. Idempotente (não duplica o parâmetro).
- `BackofficeApiKeyOperationFilter.cs` — nas operações `api/backoffice/*`, define `operation.Security` com o esquema **`BackofficeApiKey`**, substituindo na prática o requisito global JWT **só nessas rotas** (OpenAPI 3: segurança por operação).

## Registro (`ServiceCollectionExtensions`)
- `OpenApiInfo.Description` resume tenant vs backoffice.
- `AddSecurityDefinition("Bearer", ...)` — JWT para rotas do clube.
- `AddSecurityDefinition("BackofficeApiKey", ...)` — tipo **apiKey**, header **`X-Api-Key`** (valor = `Backoffice:ApiKey`).
- `AddSecurityRequirement` global com Bearer (padrão); operações backoffice sobrescrevem via `BackofficeApiKeyOperationFilter`.

## Contexto
Sem `X-Tenant-Id` no Swagger, chamadas como `POST /api/Auth/login` falham com 400. Sem autorizar **BackofficeApiKey**, chamadas a `api/backoffice/*` falham com 401.

## Dependências
- `Swashbuckle.AspNetCore` (`IOperationFilter`, geração OpenAPI)
- `Microsoft.OpenApi` (OpenAPI.NET 2.x, tipos como `OpenApiOperation`; referências de esquema com `OpenApiSecuritySchemeReference`)
