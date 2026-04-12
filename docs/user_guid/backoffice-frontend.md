# Backoffice SaaS — interface web (`/backoffice`)

## Visão geral

O SPA em `web/` inclui uma área **Backoffice** para operadores da plataforma gerenciarem tenants, planos SaaS, vínculos tenant–plano, cobrança SaaS (Stripe) e **gateway de pagamentos de sócios** (escolha de provedor por tenant) — tudo via API `api/backoffice/*` com autenticação **`X-Api-Key`** (`Backoffice:ApiKey` no servidor).

Isso é **independente** da área **Admin** do clube (`/admin/*`), que usa JWT de usuário do tenant e header **`X-Tenant-Id`**.

## Como acessar

1. Suba o frontend (`npm run dev` em `web/`).
2. Abra a aplicação em qualquer host válido para o Vite (ex.: `http://localhost:5173` ou `http://127.0.0.1:5173`).
3. Navegue para **`/backoffice/login`** (ex.: `http://localhost:5173/backoffice/login`).

**Importante:** rotas `/backoffice/*` **não** exigem resolução de tenant por subdomínio. O fluxo normal do app (bloqueio sem slug de tenant) é ignorado quando o caminho começa com `/backoffice`.

## Autenticação

- Na tela de login do backoffice, informe a **mesma chave** configurada em `Backoffice:ApiKey`.
- A chave é validada com `GET /api/backoffice/tenants?page=1&pageSize=1`.
- Em caso de sucesso, a chave fica em **`sessionStorage`** (chave interna `socioTorcedor.backoffice.apiKey`).
- **Sair** remove a chave e redireciona para `/backoffice/login`.
- Respostas **401** limpam a sessão do backoffice e redirecionam para o login.

## Páginas

| Rota | Descrição |
|------|-----------|
| `/backoffice/login` | Entrada da chave de API |
| `/backoffice` | Painel com totais (tenants, planos) |
| `/backoffice/tenants` | Lista, busca, filtro por status, criação de tenant |
| `/backoffice/tenants/:id` | Detalhe: dados, status, domínios, configurações, plano SaaS, pagamentos SaaS, gateway de sócios |
| `/backoffice/plans` | CRUD de planos SaaS (features, preços, Stripe Price IDs) |
| `/backoffice/tenant-plans` | Atribuir plano a tenant; listar tenants por plano |

### Detalhe do tenant — abas

- **Geral:** nome, connection string, alteração de status (`TenantStatus`).
- **Domínios:** origens CORS adicionais (`POST/DELETE` domínios).
- **Configurações:** chaves/valores (`POST/PUT/DELETE` settings).
- **Plano SaaS:** plano ativo (`GET` tenant-plan); atribuir (`POST` tenant-plans) ou revogar (`DELETE`).
- **Pagamentos SaaS:** iniciar billing, assinatura, faturas, portal Stripe.
- **Gateway sócios:** provedor (`None` / `StripeDirect` / outros) e status da configuração no master.

## Variáveis de ambiente

O cliente HTTP do backoffice usa a mesma base da API que o restante do SPA:

- **`VITE_API_BASE_URL`** — URL do backend (ex.: `http://localhost:5000`).

## Testes

No diretório `web/`:

```bash
npm test
npm run test:watch
```

Há testes de unidade para os módulos `shared/backoffice/*`, componentes principais do backoffice, e utilitários existentes (`resolveTenantFromHostname`, `tokenStorage`, interceptors do `apiClient`).

## Referências no repositório

- Cliente HTTP: `web/src/shared/http/backofficeClient.ts`
- APIs: `web/src/shared/backoffice/*.ts`
- Rotas: `web/src/app/backoffice/BackofficeRouter.tsx`
- Entrada do app: `web/src/app/App.tsx` (bypass de tenant para `/backoffice`)
- Backend: `backend/src/Modules/Backoffice/AGENTS.md`, `backend/src/Modules/Payments/AGENTS.md`
