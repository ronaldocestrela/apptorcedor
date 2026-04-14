# Frontend — painel administrativo inicial

## Escopo

A SPA consome autenticação, permissões granulares, diagnóstico, configurações, auditoria, matriz role × permissão (leitura e edição), gestão de staff (convites e usuários internos), dashboard administrativo, **módulo Membership (B.4)** (listagem, detalhe, histórico operacional e alteração de status com motivo obrigatório), **módulo Plans (B.5)** (CRUD de planos, benefícios por plano, publicação para catálogo do torcedor), **módulo Payments (B.6)** (listagem de cobranças, detalhe, conciliação, cancelamento e estorno conforme permissões), **módulo Digital Card (B.7)** (listagem de emissões, preview do template fixo, emissão, regeneração e invalidação conforme permissões), **módulo Games & Tickets (B.8)** (CRUD de jogos, listagem de ingressos, reserva/compra/sync via provedor mock e resgate administrativo conforme permissões), **módulo News (B.9)** (editoria de notícias, publicação/despublicação e disparo ou agendamento de notificações in-app conforme permissão), **módulos Loyalty & Benefits (B.10)** (campanhas/regras de pontos, extrato, ranking mensal/acumulado, parceiros, ofertas e resgates administrativos conforme permissões) e **módulo Support / chamados (B.11)** (filas, SLA, histórico, respostas e gestão de status com `Chamados.Responder`; ver [parte-b11-support-admin.md](../architecture/parte-b11-support-admin.md)).

Rotas do **torcedor** fora do `/admin`: **C.1** — `/register` (cadastro público + LGPD), `/account` (Minha conta: perfil e foto), login com Google opcional (`VITE_GOOGLE_CLIENT_ID`) — [parte-c1-cadastro-perfil-torcedor.md](../architecture/parte-c1-cadastro-perfil-torcedor.md); **C.2** — `/news` (feed), `/news/:newsId` (detalhe), `/benefits` (benefícios elegíveis) — [parte-c2-noticias-beneficios-torcedor.md](../architecture/parte-c2-noticias-beneficios-torcedor.md); **C.3** — `/digital-card` (carteirinha digital + cache local opcional) — [parte-c3-carteirinha-torcedor.md](../architecture/parte-c3-carteirinha-torcedor.md); **C.4** — `/games` (jogos ativos), `/tickets` (meus ingressos e resgate) — [parte-c4-jogos-ingressos-torcedor.md](../architecture/parte-c4-jogos-ingressos-torcedor.md).

## Rotas

| Rota | Permissão mínima (conceito) |
|------|------------------------------|
| `/admin` | Qualquer permissão em `ADMIN_AREA_PERMISSIONS` no frontend (espelha o menu) |
| `/admin/dashboard` | `Usuarios.Visualizar` **ou** `Configuracoes.Visualizar` (alinhado à política `AdminDashboard` na API) |
| `/admin/staff` | `Usuarios.Visualizar` (convites e listagem); edição (convite, ativar/desativar, roles) exige `Usuarios.Editar` |
| `/admin/users` | `Usuarios.Visualizar` — listagem e busca de **todas** as contas (torcedores, não associados e staff); edição de perfil e ativar/inativar conta exige `Usuarios.Editar` |
| `/admin/users/:userId` | `Usuarios.Visualizar` (detalhe, histórico de auditoria da conta/perfil); mutações como em `/admin/users` |
| `/admin/diagnostics` | `Administracao.Diagnostics` |
| `/admin/configurations` | `Configuracoes.Visualizar` (edição exige `Configuracoes.Editar`) |
| `/admin/audit-logs` | `Configuracoes.Visualizar` |
| `/admin/role-permissions` | `Configuracoes.Visualizar` (edição exige `Configuracoes.Editar`) |
| `/admin/membership` | `Socios.Gerenciar` (filtros `?userId=` / `?membershipId=`; ver [parte-b4-membership-admin.md](../architecture/parte-b4-membership-admin.md)) |
| `/admin/plans` | `Planos.Visualizar` (criação `Planos.Criar`, edição/publicação `Planos.Editar`; ver [parte-b5-plans-admin.md](../architecture/parte-b5-plans-admin.md)) |
| `/admin/payments` | `Pagamentos.Visualizar` para listar/detalhar; `Pagamentos.Gerenciar` para conciliar/cancelar; `Pagamentos.Estornar` para estorno (ver [parte-b6-payments-admin.md](../architecture/parte-b6-payments-admin.md)) |
| `/admin/digital-cards` | `Carteirinha.Visualizar` para listar/detalhar; `Carteirinha.Gerenciar` para emitir/regenerar/invalidar (ver [parte-b7-digital-card-admin.md](../architecture/parte-b7-digital-card-admin.md)) |
| `/admin/games` | `Jogos.Visualizar` para listar/detalhar; `Jogos.Criar` / `Jogos.Editar` para criar, editar e desativar (ver [parte-b8-games-tickets-admin.md](../architecture/parte-b8-games-tickets-admin.md)) |
| `/admin/tickets` | `Ingressos.Visualizar` para listar/detalhar; `Ingressos.Gerenciar` para reservar, comprar, sincronizar e resgatar (ver [parte-b8-games-tickets-admin.md](../architecture/parte-b8-games-tickets-admin.md)) |
| `/admin/news` | `Noticias.Publicar` — editoria, publicação/despublicação e notificações in-app (ver [parte-b9-news-admin.md](../architecture/parte-b9-news-admin.md)) |
| `/admin/loyalty` | `Fidelidade.Visualizar` / `Fidelidade.Gerenciar` (ver [parte-b10-loyalty-benefits-admin.md](../architecture/parte-b10-loyalty-benefits-admin.md)) |
| `/admin/benefits` | `Beneficios.Visualizar` / `Beneficios.Gerenciar` (ver [parte-b10-loyalty-benefits-admin.md](../architecture/parte-b10-loyalty-benefits-admin.md)) |
| `/admin/support` | `Chamados.Responder` (ver [parte-b11-support-admin.md](../architecture/parte-b11-support-admin.md)) |
| `/admin/lgpd/documents` | `Lgpd.Documentos.Visualizar` (edição: `Lgpd.Documentos.Editar`) |
| `/admin/lgpd/consents` | `Lgpd.Consentimentos.Visualizar` (registro: `Lgpd.Consentimentos.Registrar`) |
| `/admin/lgpd/privacy` | `Lgpd.Dados.Exportar` e/ou `Lgpd.Dados.Anonimizar` |
| `/accept-staff-invite` | Anônimo — conclui cadastro com token do convite |

## Autorização

- `GET /api/auth/me` retorna `roles` e **`permissions`** (lista de strings alinhada ao catálogo do backend).
- O frontend usa [`frontend/src/shared/auth/permissionUtils.ts`](../../frontend/src/shared/auth/permissionUtils.ts) e constantes em [`applicationPermissions.ts`](../../frontend/src/shared/auth/applicationPermissions.ts) (nomes idênticos ao backend). `ADMIN_AREA_PERMISSIONS` inclui `Usuarios.Visualizar` / `Usuarios.Editar` para liberar o shell admin a operadores de RH/staff, permissões de **planos** para perfis só de produto/oferta e permissões de **pagamentos** para o perfil financeiro.
- `PermissionRoute` protege a árvore `/admin`; `PermissionGate` protege cada seção contra acesso direto por URL.
- Permissões **LGPD** também entram em `ADMIN_AREA_PERMISSIONS` para permitir acesso ao shell admin a perfis só de compliance (sem `Usuarios.*` / `Configuracoes.*`).
- Permissões **Carteirinha** (`Carteirinha.Visualizar` / `Carteirinha.Gerenciar`) entram em `ADMIN_AREA_PERMISSIONS` para operadores que só gerenciam emissões da carteirinha digital.
- Permissões **Jogos** (`Jogos.Visualizar`, `Jogos.Criar`, `Jogos.Editar`) e **Ingressos** (`Ingressos.Visualizar`, `Ingressos.Gerenciar`) entram em `ADMIN_AREA_PERMISSIONS` para operadores dedicados a jogos/ingressos.
- Permissão **Notícias** (`Noticias.Publicar`) entra em `ADMIN_AREA_PERMISSIONS` para perfis só de conteúdo/comunicação.
- Permissões **Fidelidade** (`Fidelidade.Visualizar`, `Fidelidade.Gerenciar`) e **Benefícios** (`Beneficios.Visualizar`, `Beneficios.Gerenciar`) entram em `ADMIN_AREA_PERMISSIONS` para marketing/operação de campanhas e parceiros.
- Permissão **Chamados** (`Chamados.Responder`) entra em `ADMIN_AREA_PERMISSIONS` para perfis só de atendimento operarem `/admin/support` sem outras permissões administrativas.

## Integração HTTP

Serviços em [`frontend/src/features/admin/services/adminApi.ts`](../../frontend/src/features/admin/services/adminApi.ts) centralizam chamadas aos endpoints administrativos (incluindo **usuários** em `/api/admin/users`) e ao aceite de convite (`/api/auth/accept-staff-invite`).

LGPD: [`frontend/src/features/admin/services/lgpdApi.ts`](../../frontend/src/features/admin/services/lgpdApi.ts) → `GET/POST /api/admin/lgpd/...` (ver [parte-b2-lgpd.md](../architecture/parte-b2-lgpd.md)).

## Fluxo de convite (staff)

1. Admin com `Usuarios.Editar` cria convite em **Staff**; a API devolve o **token** uma vez (armazenado como hash no servidor).
2. O convidado abre `/accept-staff-invite?token=...` (ou cola o token), define senha e conclui; tokens JWT são gravados como no login.

## Testes

- `npm test` — helpers de permissão (`permissionUtils.test.ts`), `authStorage`, `adminApi.support.test.ts`, etc.
- ajuste de tipagem em `DashboardPage.test.tsx` para manter `tsc -b` do frontend verde durante o build.

## Sistema Visual Admin (Fase 1)

Implementado um baseline de visual contínuo para o painel administrativo, inspirado no mock escuro-esverdeado.

### Objetivo desta fase

- alinhar a experiência visual do shell administrativo (`/admin/*`) sem alterar regras de negócio;
- manter intactas permissões granulares, rotas e integrações HTTP existentes;
- criar base reutilizável para estender o mesmo padrão visual às demais páginas.

### Mudanças entregues

- `frontend/src/features/admin/layout/AdminLayout.tsx`
	- migração de estilos inline para classes CSS;
	- preservação da lógica de visibilidade por permissão (`hasPermission(...)` por item de menu);
	- preservação do `Outlet` e da navegação das rotas administrativas existentes.

- `frontend/src/features/admin/layout/AdminLayout.css` (novo)
	- shell admin com sidebar escura, gradientes e estados ativos/hover para links;
	- área de conteúdo com cabeçalho visual e ajustes responsivos básicos.

- `frontend/src/features/admin/pages/AdminDashboardPage.tsx`
	- adoção de classes sem alterar fluxo de dados (`getAdminDashboard`), loading/error e `PermissionGate`.

- `frontend/src/features/admin/pages/AdminDashboardPage.css` (novo)
	- tipografia, espaçamentos e grid de KPIs alinhados ao novo tema.

- `frontend/src/features/admin/components/KpiCard.tsx` + `KpiCard.css` (novos)
	- componente reutilizável para cards de indicadores.

- `frontend/src/index.css`
	- novos design tokens `--admin-*` para tema escuro verde (superfície, texto, destaque e bordas).

### Testes adicionados (TDD)

- `frontend/src/features/admin/layout/AdminLayout.test.tsx`
	- valida estrutura visual base do shell por classes (`.admin-shell`, `.admin-shell__sidebar`, `.admin-shell__content`);
	- valida manutenção da visibilidade de menu orientada por permissões.

- `frontend/src/features/admin/pages/AdminDashboardPage.test.tsx`
	- valida renderização do dashboard com classes novas e 3 cards KPI após carregamento;
	- valida mensagem de erro em falha de API.

### Decisões técnicas

- não houve alteração de contratos de API (`adminApi.ts`) nem de políticas de autorização;
- foco em refatoração visual incremental para reduzir risco de regressão funcional;
- tokens globais `--admin-*` foram preferidos para facilitar reaproveitamento futuro no restante do frontend.

## Modernização Responsiva (Fase 2)

Evolução visual aplicada às telas do torcedor com foco em responsividade, sem mudanças em regras de negócio.

### Escopo entregue

- `frontend/src/pages/AppShell.css` (novo)
	- camada visual reutilizável para páginas autenticadas do torcedor;
	- containers fluidos (`app-shell`), superfícies modernas (`app-surface`), botões, campos e grid responsivo;
	- regras mobile-first para breakpoints até 760px.

- `frontend/src/pages/DashboardPage.tsx`
	- modernização do painel inicial autenticado com hero + acessos rápidos em grid responsivo;
	- manutenção de `canAccessAdminArea(...)`, permissões e logout sem alteração funcional.

- `frontend/src/pages/PlansPage.tsx`
	- catálogo de planos modernizado em cards responsivos;
	- sem alterações no fluxo de `plansService.listPublished()`.

- `frontend/src/pages/AccountPage.tsx`
	- reorganização visual de assinatura, troca/cancelamento e formulário de perfil usando classes reutilizáveis;
	- preservados os fluxos de `subscriptionsService` e `accountApi` (upload/salvar perfil, troca de plano, cancelamento, modal de confirmação).

### Testes adicionados (TDD)

- `frontend/src/pages/DashboardPage.test.tsx`
	- valida estrutura moderna (`.dashboard-page`, hero e grid) e comportamento de visibilidade do link admin por permissão.

- `frontend/src/pages/PlansPage.test.tsx`
	- valida grid e cards do catálogo modernizado, além do estado vazio.

### Compatibilidade e riscos

- nenhum endpoint novo foi criado;
- nenhuma permissão/rota foi alterada;
- lint do frontend foi saneado após remover atualização síncrona de estado em efeito no shell admin e estabilizar dependências do efeito de assinatura em `AccountPage.tsx`.
