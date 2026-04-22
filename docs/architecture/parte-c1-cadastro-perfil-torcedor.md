# Parte C.1 — Cadastro e perfil (torcedor)

## Objetivo

Expor fluxos **self-service** para o torcedor: cadastro público com LGPD, perfil (`UserProfile`), upload de foto, login social (Google) e área **Minha conta** no SPA, sem acoplar **Membership** ou permissões administrativas.

## Backend

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/account/register/requirements` | Anônimo | Versões publicadas obrigatórias (termos + privacidade) para o formulário de cadastro. |
| POST | `/api/account/register` | Anônimo | Cadastro público; retorna o mesmo contrato de sessão que o login (`AuthResponse`). |
| GET | `/api/account/profile` | JWT | Lê o próprio `UserProfile` (sem `AdministrativeNote`). |
| PUT | `/api/account/profile` | JWT | Atualiza perfil (merge de campos não nulos). Quando o campo `document` é enviado e não vazio, deve ser um **CPF válido** (módulo 11) e fica **normalizado** (11 dígitos) em `UserProfiles.Document`. CPFs duplicados em outro usuário retornam **409** com `{ "error": "cpf_already_in_use" }`; CPF inválido retorna **400** com `{ "error": "cpf_invalid", "errors": ["CPF inválido."] }`. |
| POST | `/api/account/profile/photo` | JWT | `multipart/form-data` campo `file` (jpeg/png/webp); persiste foto no provider configurado, atualiza `PhotoUrl` e, quando fizer sentido, tenta remover a foto **anterior e distinta** (best effort; ver abaixo). |
| POST | `/api/auth/google` | Anônimo | Corpo `{ idToken, acceptedLegalDocumentVersionIds? }`. Novos usuários **devem** enviar os IDs das versões publicadas (mesmo conjunto do cadastro). |
| GET | `/api/auth/me` | JWT | Inclui `requiresProfileCompletion` (perfil ausente ou documento vazio). |

### Arquitetura

- **CQRS (Application):** `RegisterTorcedorCommand`, `GetRegistrationLegalRequirementsQuery`, `GetMyProfileQuery`, `UpsertMyProfileCommand`.
- **Portas:** `ITorcedorAccountPort` (`UpsertProfileAsync` retorna `ProfileUpsertResult` com validação/unicidade de CPF), `IRegistrationLegalReadPort`, `IProfilePhotoStorage`.
- **CPF:** `AppTorcedor.Application.Validation.CpfNumber` valida dígitos e normaliza. Índice único filtrado `IX_UserProfiles_Document` em SQL Server (`[Document] IS NOT NULL`); múltiplas linhas com `NULL` seguem permitidas. Dados legados: antes de aplicar a migration, resolver CPFs duplicados (após normalização) ou a criação do índice falha.
- **Armazenamento de fotos (provider):**
	- `ProfilePhotos:Provider=Local` usa `LocalProfilePhotoStorage` (disco em `wwwroot/uploads/profile-photos/{userId}/` e URL relativa `/uploads/...` via `UseStaticFiles`).
	- `ProfilePhotos:Provider=Cloudinary` usa `CloudinaryProfilePhotoStorage` (URL absoluta `https://res.cloudinary.com/...`). O upload usa `public_id` **estável** por usuário (GUID em formato `N`) e `Overwrite: true` na mesma pasta (`ProfilePhotos:Cloudinary:Folder`). Cada troca de foto pode devolver outra `SecureUrl` (ex.: segmento de versão `v…` no path), ainda apontando para o **mesmo** asset lógico. Após persistir a nova `PhotoUrl`, a API só chama delete no storage da URL anterior se `IProfilePhotoStorage.ShouldDeletePreviousAfterReplace` indicar que é outro media (p.ex. outro `public_id` no Cloudinary, ou outro ficheiro local). Assim evita-se apagar o asset recém carregado ao confundir “URL antiga” com “foto substituída com overwrite”.
	- O frontend mantém compatibilidade entre URL relativa e absoluta via `resolvePublicAssetUrl`.
- **Configuração Cloudinary:** `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`, `ProfilePhotos:Cloudinary:Folder`.
- **LGPD:** cadastro e primeiro login Google gravam consentimentos via `ILgpdAdministrationPort.RecordConsentAsync` para as versões publicadas atuais.
- **Seed (dev):** em **Development**, a cada subida da API o `IdentityDataSeeder` garante que existam **dois** documentos (termos + privacidade), cada um com **pelo menos uma versão publicada** — inclusive em bancos já existentes que tinham só rascunhos ou um único tipo. Assim `/api/account/register/requirements` deixa de responder **503** após reiniciar a API. Em **Testing**, o bloco mínimo só é inserido com `Testing:SeedMinimalLegalDocuments=true` e catálogo vazio (testes C.1 sem afetar Part B.2).

### Google

- Pacote `Google.Apis.Auth`; configuração `Google:Auth:ClientId`.
- Em testes de API, `IGoogleIdTokenValidator` é substituído por `FakeGoogleIdTokenValidator` (token fixo `test-google-token`).

## Frontend

- Rotas: `/register`, `/account` (protegida), login com link para cadastro e bloco Google quando `VITE_GOOGLE_CLIENT_ID` está definido.
- Variáveis: `VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID` (opcional).
- Fotos: URLs relativas da API são resolvidas com `resolvePublicAssetUrl` para o host da API. A foto de perfil é exibida como avatar circular (120×120 px). Ao clicar na foto (ou no placeholder circular "Adicionar foto"), o seletor de arquivo é aberto — ao escolher uma imagem, um modal de corte 1:1 é exibido com pan + zoom (biblioteca `react-easy-crop`); só após confirmar o corte a imagem é processada via Canvas API (`getCroppedImg` em `src/shared/cropImage.ts`) e enviada para a API. O modal pode ser cancelado sem upload.
- Login (`/login`): interface atualizada para layout split-screen (formulário à esquerda e hero visual à direita), mantendo autenticação por e-mail/senha, integração de Google Sign-In condicionada a consentimento LGPD e CTA de cadastro; favicon e logo da página usam `public/logos/ESCUDO_FFC_PNG.png`. Em viewport estreita de celular (≤768px), o painel com a imagem de fundo fica oculto (`display: none`).

## Testes (TDD)

- **Application:** `RegisterTorcedorCommandHandlerTests` (normalização de entrada); `CpfNumberTests` (CPF com/sem máscara, inválido e repetição de dígitos).
- **API:** `PartC1TorcedorAccountTests` (requisitos, cadastro, perfil, CPF inválido, conflito com CPF do seed, `me`, foto local, Google com fake validator); `PartC1CloudinaryProfilePhotoUploadTests` (foto com provider Cloudinary em memória: não apaga no Cloudinary quando a URL anterior e a nova partilham o mesmo `public_id`; ainda apaga quando o `public_id` antigo é distinto, ex. migração/URL alheia).
- **Infrastructure:** `TorcedorAccountServiceCpfTests` (normalização e rejeição de duplicata in-memory; admin reutiliza a mesma regra em `UserAdministrationService`); `CloudinaryProfilePhotoStorageTests` e `LocalProfilePhotoStorageTests` — `ShouldDeletePreviousAfterReplace` (mesmo `public_id` e versão diferente no URL vs. paths distintos).
- **Frontend — unitário:** `src/shared/cropImage.test.ts` — cobre `getCroppedImg`: retorno de Blob, coordenadas de corte, parâmetros de `toBlob` (jpeg/0.9), indisponibilidade do contexto Canvas, e falha de `toBlob`.
- **Frontend — componente:** `src/pages/AccountPage.test.tsx` — `AccountPage — photo crop flow`: botão de foto clicável, abertura do modal de crop ao selecionar arquivo, cancelamento sem upload e presença do botão Confirmar no modal.
