# src

## Descrição
Código-fonte do monólito modular: blocos compartilhados, módulos de domínio e o **Host** (`SocioTorcedor.Api`).

## Estrutura
- `BuildingBlocks/` — Domain, Application, Infrastructure, Shared
- `Modules/` — Tenancy, Identity (Fase 1)
- `Host/SocioTorcedor.Api/` — pipeline HTTP, middlewares, `Program.cs`

## Dependências
- Projetos referenciam uns aos outros conforme o diagrama em `AGENTS.md` da raiz.
