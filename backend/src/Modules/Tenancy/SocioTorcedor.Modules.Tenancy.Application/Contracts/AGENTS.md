# Contracts

## Descrição
Parte do backend Sócio Torcedor; ver módulo pai.

## Estrutura
- `ITenantRepository.cs`
- `ITenantResolver.cs`
- `ITenantAutoCorsOriginProvider.cs` — origem CORS inicial em novos tenants (`CORS_BASE_DOMAIN` / fallback localhost)
- `ITenantConnectionStringGenerator.cs`
- `ITenantDatabaseProvisioner.cs`
- `ITenantSlugCacheInvalidator.cs`

## Dependências
- Pasta pai: `src/Modules/Tenancy/SocioTorcedor.Modules.Tenancy.Application`
- Referências de projeto: ver `*.csproj` nesta pasta (se existir).
