# SocioTorcedor.Modules.Tenancy.Infrastructure

## Descrição
`MasterDbContext`, repositório, `TenantSlugResolver`, cache, **`TenantAutoCorsOriginProvider`** (`ITenantAutoCorsOriginProvider`) — lê **`CORS_BASE_DOMAIN`**; sem valor → **`http://{slug}.localhost:5173`**; URL fixa, placeholder **`{slug}`**, ou host nu (ver `AGENTS.md` raiz).

## Estrutura
- `DependencyInjection.cs`
- `Services/TenantAutoCorsOriginProvider.cs`

## Dependências
- Pasta pai: `src/Modules/Tenancy`
- Referências de projeto: ver `*.csproj` nesta pasta (se existir).
