# tests

## Descrição
Testes unitários (xUnit, FluentAssertions, NSubstitute onde aplicável), espelhando `src/`.

## Estrutura
- `BuildingBlocks/`, `Modules/`, `Host/` — projetos `*.Tests`

## Dependências
- Cada `*.Tests` referencia o projeto de produção correspondente.
