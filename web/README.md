# Web — Sócio Torcedor (Fase 1.1)

SPA **React + Vite + TypeScript**, com **Axios** e **React Router**.

## Estrutura (`src/`)

| Pasta | Uso |
|--------|-----|
| `app/` | Shell da aplicação e roteamento (`router/`) |
| `shared/` | Código compartilhado (ex.: `shared/http` — cliente Axios) |
| `features/` | Módulos por domínio (vazio nesta fase) |
| `pages/` | Páginas roteadas (ex.: `admin/`, `member/`) |

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

Use `apiClient` exportado de `src/shared/http` para chamadas à API. Interceptors estão preparados para evolução (ex.: header `X-Tenant-Id`).
