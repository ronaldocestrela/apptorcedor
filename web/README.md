# Web — Sócio Torcedor

SPA **React + Vite + TypeScript**, com **Axios** e **React Router**.

## Estrutura (`src/`)

| Pasta | Uso |
|--------|-----|
| `app/` | Shell da aplicação e roteamento (`router/`) |
| `shared/` | Código compartilhado (`http`, `tenant`, etc.) |
| `features/` | Módulos por domínio |
| `pages/` | Páginas roteadas (`admin/`, `member/`, `system/`) |

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
```

## Rotas iniciais

- `/` — redireciona para `/member`
- `/admin` — placeholder área administrativa
- `/member` — placeholder área do sócio

## Cliente HTTP

Use `apiClient` exportado de `src/shared/http` para chamadas à API. O header `X-Tenant-Id` é definido pelo interceptor com base em `shared/tenant`.
