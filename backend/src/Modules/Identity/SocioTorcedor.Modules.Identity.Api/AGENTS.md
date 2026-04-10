# SocioTorcedor.Modules.Identity.Api

## Descrição
HTTP do módulo Identity: autenticação e documentos legais por tenant.

## Estrutura
- `IdentityModule.cs`
- `Controllers/AuthController.cs` — `POST /api/auth/register`, `POST /api/auth/login` (register exige IDs das versões vigentes de termos e privacidade)
- `Controllers/LegalDocumentsController.cs` — `GET /api/legal-documents/current` (anônimo), `POST /api/legal-documents` (Administrador)

## Dependências
- Pasta pai: `src/Modules/Identity`
- Referências de projeto: ver `*.csproj` nesta pasta (se existir).
