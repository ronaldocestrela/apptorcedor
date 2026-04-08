# BuildingBlocks

## Descrição
**Building blocks** reutilizáveis por todos os módulos (entidades base, Result, MediatR behaviors, `DbContext` base).

## Estrutura
- `SocioTorcedor.BuildingBlocks.Domain`
- `SocioTorcedor.BuildingBlocks.Application`
- `SocioTorcedor.BuildingBlocks.Infrastructure`
- `SocioTorcedor.BuildingBlocks.Shared`

## Dependências
- `Application` → `Shared`, `Domain` → `Shared`; `Infrastructure` → `Shared` + EF Core.
