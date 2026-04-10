# Identity

## Descrição
Usuários por tenant (ASP.NET Identity), JWT, roles/permissões e **LGPD Fase 3.4**: versões de termos e política de privacidade no banco do tenant, consentimento na criação de usuário.

## Estrutura
- `SocioTorcedor.Modules.Identity.Domain` — enums (ex.: `LegalDocumentKind`)
- `SocioTorcedor.Modules.Identity.Application` — comandos login/register, queries de documentos legais, `ILegalDocumentRepository`
- `SocioTorcedor.Modules.Identity.Infrastructure` — `TenantIdentityDbContext`, entidades `LegalDocumentVersion`, `UserLegalConsent`, seeds `LegalDocumentTenantSeed` (placeholder se não houver versões) e **`RoleTenantSeed`** (roles **`Socio`** e **`Administrador`** por tenant, após migrate)
- `SocioTorcedor.Modules.Identity.Api` — `AuthController`, `LegalDocumentsController`

## API relevante (tenant)
- **`POST /api/auth/register`** — corpo inclui `acceptedTermsDocumentId` e `acceptedPrivacyDocumentId` (obtidos de `GET /api/legal-documents/current`); IP e User-Agent são gravados no consentimento.
- **`GET /api/legal-documents/current`** — anônimo; exige header **`X-Tenant-Id`**.
- **`POST /api/legal-documents`** — publica nova versão; role **`Administrador`**.

## Dependências
- Pasta pai: `src/Modules`
- Referências de projeto: ver `*.csproj` nesta pasta (se existir).
