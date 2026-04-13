# Parte B.7 — Digital Card (gestão / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.7) e [AGENTS.md](../../AGENTS.md): **carteirinha digital** na visão administrativa com **template de exibição fixo em código** (sem editor de layout nesta entrega), **versionamento** por associação, **token opaco** por emissão, **regeneração** (invalida a versão ativa e cria nova) e **invalidação** com motivo obrigatório. Emissão e regeneração exigem `Membership.Status == Ativo`. Consumo pelo torcedor (C.3) e branding configurável ficam fora deste escopo.

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `DigitalCards` | Emissões: `UserId`, `MembershipId`, `Version` (sequencial por associação), `Status` (`Active` / `Invalidated`), `Token` (hex opaco, único), `IssuedAt`, `InvalidatedAt`, `InvalidationReason`. |

Migração EF: `PartB7DigitalCardAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

- Índice **único filtrado** em `MembershipId` onde `Status =1` (`Active`): no máximo **uma** carteirinha ativa por associação.
- Unicidade `(MembershipId, Version)` e unicidade de `Token`.

## Permissões

- `Carteirinha.Visualizar` — `GET /api/admin/digital-cards`, `GET /api/admin/digital-cards/{id}`.
- `Carteirinha.Gerenciar` — `POST /api/admin/digital-cards/issue`, `POST .../regenerate`, `POST .../invalidate`.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`).

## API administrativa

Base: `api/admin/digital-cards` (JWT + política por permissão). CQRS em `AppTorcedor.Application` e implementação em `DigitalCardAdministrationService` (`IDigitalCardAdministrationPort`).

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/digital-cards?userId=&membershipId=&status=&page=&pageSize=` | Lista paginada (`status`: `Active` ou `Invalidated`). |
| GET | `/api/admin/digital-cards/{digitalCardId}` | Detalhe incluindo `token` e `templatePreviewLines` (preview do template fixo). |
| POST | `/api/admin/digital-cards/issue` | Corpo: `{ "membershipId": "..." }`. Cria primeira emissão **se** não houver ativa e membership **Ativo**. |
| POST | `/api/admin/digital-cards/{digitalCardId}/regenerate` | Corpo opcional: `{ "reason": "..." }` (padrão interno se omitido). Só `Active`; exige membership **Ativo**. |
| POST | `/api/admin/digital-cards/{digitalCardId}/invalidate` | Corpo: `{ "reason": "..." }` obrigatório. Só `Active`. |

Respostas de mutação: `204` sucesso; `404` não encontrado; `400` elegibilidade / transição / motivo; `409` já existe carteirinha ativa para a associação (emissão duplicada).

## Regras de negócio

- **Template fixo:** linhas de preview definidas no serviço (`DigitalCardAdministrationService`), combinando nome, versão, status da associação, plano, documento mascarado e status da emissão.
- **Regeneração:** invalida o registro atual com motivo (informado ou “Regeneração administrativa”) e cria novo registro com `Version` incrementado e novo `Token`.
- **Invalidação:** encerra a versão ativa sem criar substituta (operador pode emitir de novo depois, se aplicável).
- Alteração de documento no perfil **não** dispara fluxo automático; cenários administrativos usam regeneração/invalidação explícitas.

## Auditoria

Mutações em `DigitalCardRecord` geram entradas em `AuditLogs` (interceptor existente).

## Relação com B.4 e C.3

- B.4: status da associação continua sendo a fonte de elegibilidade para emitir/regenerar.
- C.3: visualização pelo torcedor deve validar a versão **`Active`** e o `Token` (a definir na Parte C).

## Frontend (backoffice)

- Rota `/admin/digital-cards` (`Carteirinha.Visualizar` e/ou `Carteirinha.Gerenciar` para o shell; listagem com `Visualizar`; emitir/regenerar/invalidar com `Gerenciar`).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.

## Testes

- `AppTorcedor.Application.Tests` / `DigitalCardAdminHandlersTests`: delegação dos handlers ao port.
- `AppTorcedor.Api.Tests` / `PartB7DigitalCardAdminTests`: autorização, emissão com conflito, regeneração, invalidação, preview e auditoria; coleção `DigitalCardAdmin` com `DisableParallelization` para isolamento de dados de exemplo.
