# Frontend — painel administrativo inicial

## Escopo

A SPA consome apenas o que a API já expõe na fundação (Parte A): autenticação, permissões granulares, diagnóstico, configurações, auditoria, matriz role × permissão e alteração de status de membership por ID.

## Rotas

| Rota | Permissão mínima (conceito) |
|------|------------------------------|
| `/admin` | Qualquer permissão em `ADMIN_AREA_PERMISSIONS` no frontend (espelha o menu) |
| `/admin/diagnostics` | `Administracao.Diagnostics` |
| `/admin/configurations` | `Configuracoes.Visualizar` (edição exige `Configuracoes.Editar`) |
| `/admin/audit-logs` | `Configuracoes.Visualizar` |
| `/admin/role-permissions` | `Configuracoes.Visualizar` (somente leitura) |
| `/admin/membership` | `Socios.Gerenciar` |

## Autorização

- `GET /api/auth/me` retorna `roles` e **`permissions`** (lista de strings alinhada ao catálogo do backend).
- O frontend usa [`frontend/src/shared/auth/permissionUtils.ts`](../../frontend/src/shared/auth/permissionUtils.ts) e constantes em [`applicationPermissions.ts`](../../frontend/src/shared/auth/applicationPermissions.ts) (nomes idênticos ao backend).
- `PermissionRoute` protege a árvore `/admin`; `PermissionGate` protege cada seção contra acesso direto por URL.

## Integração HTTP

Serviços em [`frontend/src/features/admin/services/adminApi.ts`](../../frontend/src/features/admin/services/adminApi.ts) centralizam chamadas aos endpoints administrativos.

## Testes

- `npm test` — inclui testes de helpers de permissão (`permissionUtils.test.ts`) e o exemplo de `authStorage`.
