# Web — Sócio Torcedor

SPA **React + Vite + TypeScript**, com **Axios** e **React Router**.

## Estrutura (`src/`)

| Pasta | Uso |
|--------|-----|
| `app/` | Shell, roteamento (`router/`), **backoffice** (`backoffice/`: rotas `/backoffice/*`), autenticação (`auth/`), tema (`theme/`: `useTheme`, `ThemeToggle`) |
| `shared/` | Código compartilhado (`http` + `backofficeClient`, **`backoffice/`** (APIs `api/backoffice/*`), `tenant`, `auth`, `payments`, `members`, etc.) |
| `features/` | Módulos por domínio (opcional; ainda pouco usado) |
| `pages/` | Páginas (`admin/`, `member/`, **`backoffice/`**, `auth/`, `system/`) |

## UI e tema (claro / escuro)

* Estilos: **`src/index.css`** com variáveis CSS por tema; **`data-theme="light"`** ou **`dark`** no elemento `<html>`.
* Persistência: **`localStorage`** chave **`theme`**; em **`index.html`**, script inline aplica o tema antes do React (fallback: `prefers-color-scheme`).
* **Toggle:** barra do [`AppShell`](src/app/router/AppShell.tsx) (logado); canto superior direito em login, cadastro, [`TenantNotResolvedPage`](src/pages/system/TenantNotResolvedPage.tsx) e [`BackofficeShell`](src/app/backoffice/BackofficeShell.tsx).
* Layout responsivo: navegação principal com menu “hambúrguer” em telas estreitas (~600px).

## Fase 1.2 — Tenant por subdomínio (modo estrito)

O **slug do tenant** é obtido do **primeiro rótulo** do hostname (ex.: `flamengo` em `flamengo.app.exemplo.com`).

Regras:

- É obrigatório haver **subdomínio**; domínio apex sozinho (ex.: `exemplo.com`) **bloqueia** a aplicação.
- O rótulo **`www`** não é aceito como tenant (ex.: `www.exemplo.com`).
- **`localhost`** e **endereços IPv4** não resolvem tenant (sem fallback por env ou query).
- O slug é normalizado (**trim** + **lowercase**) antes de enviar no header `X-Tenant-Id`, alinhado ao backend.

Comportamento:

- Se o host for inválido, a UI mostra [`TenantNotResolvedPage`](src/pages/system/TenantNotResolvedPage.tsx) e **não** monta o roteador (Admin/Sócio).
- Chamadas via [`apiClient`](src/shared/http/client.ts) recebem automaticamente `X-Tenant-Id: <slug>` quando o tenant foi resolvido.

### Exemplos

| Host | Resultado |
|------|-----------|
| `flamengo.localhost` | Tenant `flamengo` |
| `flamengo.app.meudominio.com` | Tenant `flamengo` |
| `localhost` | Bloqueado |
| `127.0.0.1` | Bloqueado |
| `meudominio.com` | Bloqueado (sem subdomínio de tenant) |
| `www.meudominio.com` | Bloqueado |

### Desenvolvimento local

Use um hostname com subdomínio, por exemplo:

1. Entrada em `/etc/hosts`: `127.0.0.1 flamengo.localhost`
2. Abra `http://flamengo.localhost:5173` (porta do Vite conforme o console).

## Fase 1.4 / 1.5 — Login e cadastro (Axios)

- **Login:** `POST /api/auth/login` com `{ email, password }`.
- **Documentos legais (antes do cadastro):** `GET /api/legal-documents/current` (sem JWT; o cliente já envia `X-Tenant-Id`). Resposta inclui as versões vigentes de termos e privacidade (`id`, `kind`, `versionNumber`, `content`, etc.).
- **Cadastro:** `POST /api/auth/register` com `{ email, password, firstName, lastName, acceptedTermsDocumentId, acceptedPrivacyDocumentId }` — os dois GUIDs devem ser os `id` retornados em **current** para `TermsOfUse` e `PrivacyPolicy`. O backend grava IP e User-Agent do request nos consentimentos.
- Resposta: `{ accessToken, expiresAtUtc }` (JSON camelCase).
- O JWT é guardado em **`sessionStorage`** (via [`tokenStorage`](src/shared/auth/tokenStorage.ts)), junto com **`roles`** lidas do payload (claim **`role`**, string ou array; também aceita a claim longa do .NET). Funções auxiliares expõem claims básicas (**`decodeJwtBasicClaims`**: `email`, `sub`) para exibição quando não há perfil de sócio.
- O [`apiClient`](src/shared/http/client.ts) envia:
  - `X-Tenant-Id` (slug do subdomínio)
  - `Authorization: Bearer <accessToken>` quando há sessão válida
- Rotas **`/admin`** e **`/member`** exigem autenticação ([`RequireAuth`](src/app/router/RequireAuth.tsx)); **`/login`** e **`/register`** são públicas.
- **Navegação por papel:** no [`AppShell`](src/app/router/AppShell.tsx), os links **Admin** e **Faturamento SaaS** só são exibidos se `roles` contiver **`Administrador`**. Quem tem apenas **`Socio`** não vê esses itens (acesso direto à URL ainda é possível; a API aplica autorização).
- **Sair** limpa a sessão e redireciona para `/login`.
- Respostas **401** em rotas autenticadas limpam a sessão e redirecionam para `/login` (exceto nas páginas de login/cadastro).

## Pré-requisitos

- Node.js 20+ (recomendado)

## Configuração

1. Copie o exemplo de variáveis:

   ```bash
   cp .env.example .env
   ```

2. Ajuste `VITE_API_BASE_URL` para a URL do backend (ex.: `http://localhost:5000`).

## Comandos

```bash
npm install
npm run dev
npm run build
npm run preview
npm test
npm run test:watch
```

## Backoffice SaaS (`/backoffice/*`)

Área para **operadores da plataforma** (master DB), autenticada com **`X-Api-Key`** — a mesma chave de `Backoffice:ApiKey` no backend.

- **Não** usa resolução de tenant por subdomínio: em [`App.tsx`](src/app/App.tsx), se o path começa com `/backoffice`, o app monta só o [`BackofficeRouter`](src/app/backoffice/BackofficeRouter.tsx) (pode abrir em `http://localhost:5173/backoffice/login`).
- **Login:** [`BackofficeLoginPage`](src/pages/backoffice/BackofficeLoginPage.tsx) valida a chave e grava em `sessionStorage`.
- **HTTP:** [`backofficeClient`](src/shared/http/backofficeClient.ts) envia apenas `X-Api-Key` (sem `X-Tenant-Id` nem JWT).
- **Guia:** [`docs/user_guid/backoffice-frontend.md`](../docs/user_guid/backoffice-frontend.md).

## Fase 4 — Pagamentos (MVP web)

- **`/member/billing`** — assinatura de plano do clube, PIX e checkout Stripe quando o **gateway** do tenant está configurado (`/api/payments/member/*`). A assinatura atual usa **`GET /api/payments/member/me/subscription`**, que inclui **`planName`** (nome do plano) além do id do plano.
- **`/admin/billing`** — orientação para **admin do clube**: faturamento SaaS ainda é via API backoffice; operadores da plataforma podem usar **`/backoffice`** (UI) ou Scalar com **`X-Api-Key`**.

## Área do sócio — perfil

- **`/member`** — [`MemberHomePage`](src/pages/member/MemberHomePage.tsx): carrega **`GET /api/members/me`** ([`membersApi`](src/shared/members/membersApi.ts)). Exibe identificação, endereço e metadados; em **404** (sem `MemberProfile`), mostra e-mail/roles da sessão e orientação. Link para **`/member/billing`**.

## Rotas

- `/login` — entrar
- `/register` — cadastro
- `/` — redireciona para `/member` (após autenticação)
- `/admin` — início admin com links para subáreas (protegida; link no menu só para **`Administrador`**)
- `/admin/plans` — CRUD de planos de sócio (`/api/plans`; protegida; menu só para **`Administrador`**)
- `/admin/billing` — faturamento SaaS (instruções; protegida; link no menu só para **`Administrador`**)
- `/admin/stripe` — gateway de pagamentos / credenciais Stripe direto para sócios (protegida; menu **Gateway** para **`Administrador`**)
- `/member` — minha conta / perfil do sócio (`GET /api/members/me`; protegida)
- `/member/billing` — pagamentos do sócio (protegida)
- `/backoffice/login` — entrada da chave API (sem tenant)
- `/backoffice` — painel SaaS (protegida pela chave em sessão)
- `/backoffice/tenants` — lista e criação de tenants
- `/backoffice/tenants/:id` — detalhe (domínios, settings, plano, pagamentos, gateway sócios)
- `/backoffice/plans` — planos SaaS
- `/backoffice/tenant-plans` — vínculos tenant–plano

## Cliente HTTP

- **`apiClient`** (`src/shared/http/client.ts`): rotas do tenant — headers `X-Tenant-Id` e `Authorization` quando aplicável.
- **`backofficeClient`** (`src/shared/http/backofficeClient.ts`): rotas `api/backoffice/*` — header `X-Api-Key` a partir da sessão do backoffice.

Ambos são reexportados em `src/shared/http/index.ts`.
