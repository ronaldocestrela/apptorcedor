# Frontend — painel administrativo inicial

## Escopo

A SPA consome autenticação, permissões granulares, diagnóstico, configurações, auditoria, matriz role × permissão (leitura e edição), gestão de staff (convites e usuários internos), dashboard administrativo e alteração de status de membership por ID.

## Rotas

| Rota | Permissão mínima (conceito) |
|------|------------------------------|
| `/admin` | Qualquer permissão em `ADMIN_AREA_PERMISSIONS` no frontend (espelha o menu) |
| `/admin/dashboard` | `Usuarios.Visualizar` **ou** `Configuracoes.Visualizar` (alinhado à política `AdminDashboard` na API) |
| `/admin/staff` | `Usuarios.Visualizar` (convites e listagem); edição (convite, ativar/desativar, roles) exige `Usuarios.Editar` |
| `/admin/diagnostics` | `Administracao.Diagnostics` |
| `/admin/configurations` | `Configuracoes.Visualizar` (edição exige `Configuracoes.Editar`) |
| `/admin/audit-logs` | `Configuracoes.Visualizar` |
| `/admin/role-permissions` | `Configuracoes.Visualizar` (edição exige `Configuracoes.Editar`) |
| `/admin/membership` | `Socios.Gerenciar` |
| `/admin/lgpd/documents` | `Lgpd.Documentos.Visualizar` (edição: `Lgpd.Documentos.Editar`) |
| `/admin/lgpd/consents` | `Lgpd.Consentimentos.Visualizar` (registro: `Lgpd.Consentimentos.Registrar`) |
| `/admin/lgpd/privacy` | `Lgpd.Dados.Exportar` e/ou `Lgpd.Dados.Anonimizar` |
| `/accept-staff-invite` | Anônimo — conclui cadastro com token do convite |

## Autorização

- `GET /api/auth/me` retorna `roles` e **`permissions`** (lista de strings alinhada ao catálogo do backend).
- O frontend usa [`frontend/src/shared/auth/permissionUtils.ts`](../../frontend/src/shared/auth/permissionUtils.ts) e constantes em [`applicationPermissions.ts`](../../frontend/src/shared/auth/applicationPermissions.ts) (nomes idênticos ao backend). `ADMIN_AREA_PERMISSIONS` inclui `Usuarios.Visualizar` / `Usuarios.Editar` para liberar o shell admin a operadores de RH/staff.
- `PermissionRoute` protege a árvore `/admin`; `PermissionGate` protege cada seção contra acesso direto por URL.
- Permissões **LGPD** também entram em `ADMIN_AREA_PERMISSIONS` para permitir acesso ao shell admin a perfis só de compliance (sem `Usuarios.*` / `Configuracoes.*`).

## Integração HTTP

Serviços em [`frontend/src/features/admin/services/adminApi.ts`](../../frontend/src/features/admin/services/adminApi.ts) centralizam chamadas aos endpoints administrativos e ao aceite de convite (`/api/auth/accept-staff-invite`).

LGPD: [`frontend/src/features/admin/services/lgpdApi.ts`](../../frontend/src/features/admin/services/lgpdApi.ts) → `GET/POST /api/admin/lgpd/...` (ver [parte-b2-lgpd.md](../architecture/parte-b2-lgpd.md)).

## Fluxo de convite (staff)

1. Admin com `Usuarios.Editar` cria convite em **Staff**; a API devolve o **token** uma vez (armazenado como hash no servidor).
2. O convidado abre `/accept-staff-invite?token=...` (ou cola o token), define senha e conclui; tokens JWT são gravados como no login.

## Testes

- `npm test` — helpers de permissão (`permissionUtils.test.ts`), `authStorage`, etc.
