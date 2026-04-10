# src

## Descrição
Código-fonte do monólito modular: blocos compartilhados, módulos de domínio e o **Host** (`SocioTorcedor.Api`).

## Estrutura
- `BuildingBlocks/` — Domain, Application, Infrastructure, Shared
- `Modules/` — Tenancy, Identity (JWT + LGPD Fase 3.4), Membership (perfil e planos Fase 3), Backoffice
- `Host/SocioTorcedor.Api/` — pipeline HTTP, middlewares, `Program.cs`

## Dependências
- Projetos referenciam uns aos outros conforme o diagrama em `AGENTS.md` da raiz.
