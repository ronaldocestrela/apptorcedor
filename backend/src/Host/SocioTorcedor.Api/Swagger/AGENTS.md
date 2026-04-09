# Swagger (Host)

## Descrição
Extensões do **Swashbuckle** para o OpenAPI exposto pelo Host: segurança JWT e parâmetros globais usados na UI do Swagger.

## Arquivos
- `TenantHeaderOperationFilter.cs` — `IOperationFilter` que adiciona o header **`X-Tenant-Id`** (obrigatório, string) em **todas** as operações, para o Swagger UI enviar o slug do tenant em login, register e demais endpoints. A aplicação do filtro é **idempotente** (não duplica o parâmetro se `Apply` rodar mais de uma vez).

## Contexto
O `TenantResolutionMiddleware` exige `X-Tenant-Id` fora de `/health` e `/swagger`; sem este filtro, a UI do Swagger não oferece campo para o header e chamadas como `POST /api/auth/login` retornam 400.

## Dependências
- `Swashbuckle.AspNetCore` (`IOperationFilter`, geração OpenAPI)
- `Microsoft.OpenApi.Models` (`OpenApiOperation`, `OpenApiParameter`)
