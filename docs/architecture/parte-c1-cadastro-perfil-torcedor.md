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
| PUT | `/api/account/profile` | JWT | Atualiza perfil (merge de campos não nulos). |
| POST | `/api/account/profile/photo` | JWT | `multipart/form-data` campo `file` (jpeg/png/webp); persiste arquivo e atualiza `PhotoUrl`. |
| POST | `/api/auth/google` | Anônimo | Corpo `{ idToken, acceptedLegalDocumentVersionIds? }`. Novos usuários **devem** enviar os IDs das versões publicadas (mesmo conjunto do cadastro). |
| GET | `/api/auth/me` | JWT | Inclui `requiresProfileCompletion` (perfil ausente ou documento vazio). |

### Arquitetura

- **CQRS (Application):** `RegisterTorcedorCommand`, `GetRegistrationLegalRequirementsQuery`, `GetMyProfileQuery`, `UpsertMyProfileCommand`.
- **Portas:** `ITorcedorAccountPort`, `IRegistrationLegalReadPort`, `IProfilePhotoStorage`.
- **Armazenamento de fotos:** `LocalProfilePhotoStorage` grava em `wwwroot/uploads/profile-photos/{userId}/` e expõe URL pública `/uploads/...` via `UseStaticFiles`.
- **LGPD:** cadastro e primeiro login Google gravam consentimentos via `ILgpdAdministrationPort.RecordConsentAsync` para as versões publicadas atuais.
- **Seed (dev):** em **Development**, a cada subida da API o `IdentityDataSeeder` garante que existam **dois** documentos (termos + privacidade), cada um com **pelo menos uma versão publicada** — inclusive em bancos já existentes que tinham só rascunhos ou um único tipo. Assim `/api/account/register/requirements` deixa de responder **503** após reiniciar a API. Em **Testing**, o bloco mínimo só é inserido com `Testing:SeedMinimalLegalDocuments=true` e catálogo vazio (testes C.1 sem afetar Part B.2).

### Google

- Pacote `Google.Apis.Auth`; configuração `Google:Auth:ClientId`.
- Em testes de API, `IGoogleIdTokenValidator` é substituído por `FakeGoogleIdTokenValidator` (token fixo `test-google-token`).

## Frontend

- Rotas: `/register`, `/account` (protegida), login com link para cadastro e bloco Google quando `VITE_GOOGLE_CLIENT_ID` está definido.
- Variáveis: `VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID` (opcional).
- Fotos: URLs relativas da API são resolvidas com `resolvePublicAssetUrl` para o host da API.
- Login (`/login`): interface atualizada para layout split-screen (formulário à esquerda e hero visual à direita), mantendo autenticação por e-mail/senha, integração de Google Sign-In condicionada a consentimento LGPD e CTA de cadastro; favicon e logo da página usam `public/logos/ESCUDO_FFC_PNG.png`. Em viewport estreita de celular (≤768px), o painel com a imagem de fundo fica oculto (`display: none`).

## Testes (TDD)

- **Application:** `RegisterTorcedorCommandHandlerTests` (normalização de entrada).
- **API:** `PartC1TorcedorAccountTests` (requisitos, cadastro, perfil, `me`, foto, Google com fake validator).
