# Frontend — painel administrativo inicial

## Escopo

A SPA consome autenticação, permissões granulares, diagnóstico, configurações, auditoria, matriz role × permissão (leitura e edição), gestão de staff (convites e usuários internos), dashboard administrativo, **módulo Membership (B.4)** (listagem, detalhe, histórico operacional e alteração de status com motivo obrigatório), **módulo Plans (B.5)** (CRUD de planos, benefícios por plano, publicação para catálogo do torcedor), **módulo Payments (B.6)** (listagem de cobranças, detalhe, conciliação, cancelamento e estorno conforme permissões), **módulo Digital Card (B.7)** (listagem de emissões, preview do template fixo, emissão, regeneração e invalidação conforme permissões), **módulo Games & Tickets (B.8)** (CRUD de jogos, listagem de ingressos, reserva/compra/sync via provedor mock e resgate administrativo conforme permissões) e **módulo News (B.9)** (editoria de notícias, publicação/despublicação e disparo ou agendamento de notificações in-app conforme permissão).

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

## Integração HTTP

Serviços em [`frontend/src/features/admin/services/adminApi.ts`](../../frontend/src/features/admin/services/adminApi.ts) centralizam chamadas aos endpoints administrativos (incluindo **usuários** em `/api/admin/users`) e ao aceite de convite (`/api/auth/accept-staff-invite`).

LGPD: [`frontend/src/features/admin/services/lgpdApi.ts`](../../frontend/src/features/admin/services/lgpdApi.ts) → `GET/POST /api/admin/lgpd/...` (ver [parte-b2-lgpd.md](../architecture/parte-b2-lgpd.md)).

## Fluxo de convite (staff)

1. Admin com `Usuarios.Editar` cria convite em **Staff**; a API devolve o **token** uma vez (armazenado como hash no servidor).
2. O convidado abre `/accept-staff-invite?token=...` (ou cola o token), define senha e conclui; tokens JWT são gravados como no login.

## Testes

- `npm test` — helpers de permissão (`permissionUtils.test.ts`), `authStorage`, etc.
