# SocioTorcedor.Modules.Identity.Infrastructure

## Descrição
Identity EF por tenant, `JwtTokenService`, `IdentityService`.

## Estrutura
- `DependencyInjection.cs`
- `Persistence/` — `TenantIdentityDbContext`, `LegalDocumentTenantSeed`, **`RoleTenantSeed`**, migrations EF
- `Services/` — `IdentityService`, `JwtTokenService`, `TenantDatabaseProvisioner`

## Dependências
- Pasta pai: `src/Modules/Identity`
- Referências de projeto: ver `*.csproj` nesta pasta (se existir).
