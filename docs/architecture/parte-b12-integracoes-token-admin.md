# Parte B.12 — Integrações por Token (admin)

## Objetivo

Permitir que o time administrativo gerencie, no backoffice, os tokens usados por aplicações externas no endpoint de integração Partner Lookup (`GET /api/partner/v1/lookup?phone=...`).

A entrega mantém o contrato externo atual com autenticação por header `X-Api-Key`.

## Permissões

- `Webhooks.Visualizar`: permite listar tokens na área administrativa
- `Webhooks.Gerenciar`: permite gerar e revogar tokens

As permissões são granulares e independentes de role fixa.

## Endpoints administrativos

Base: `/api/admin/partner-keys`

- `GET /api/admin/partner-keys`
  - autorização: policy `WebhooksRead` (`Webhooks.Visualizar` **ou** `Webhooks.Gerenciar`)
  - retorno: lista de tokens com metadados (`id`, `name`, `keyPrefix`, `isActive`, `createdAt`, `lastUsedAtUtc`)
  - não retorna `plaintextKey`

- `POST /api/admin/partner-keys`
  - autorização: `Permission:Webhooks.Gerenciar`
  - payload: `{ "name": "..." }`
  - retorno: inclui `plaintextKey` apenas na resposta da criação

- `DELETE /api/admin/partner-keys/{id}`
  - autorização: `Permission:Webhooks.Gerenciar`
  - revogação imediata (`IsActive = false`)

## Endpoint de consumo externo (inalterado)

- `GET /api/partner/v1/lookup?phone=...`
  - autenticação: scheme `PartnerApiKey` via header `X-Api-Key`
  - resposta: `{ exists, isActiveMember }`
  - LGPD: não expõe dados pessoais além do resultado booleano

## Frontend administrativo

- Rota: `/admin/webhook-tokens`
- Menu: seção **Sistema** → **Integrações — Tokens**
- UI: abas e botões com componentes shadcn mínimos (`Tabs`, `Button`, `Dialog`, `Input`, `Alert`)
- Comportamento seguro:
  - token em texto claro aparece apenas após criação
  - botão de cópia para área de transferência no diálogo de criação
  - listagem mostra apenas `keyPrefix`

## TDD e validação

- API tests: `PartnerApiTests`
  - cobertura do fluxo criar/listar/revogar
  - cobertura de acesso sem token e token inválido no Partner Lookup
  - contrato de autorização do controller admin (split leitura x gerenciamento)

- Frontend tests: `WebhookTokensAdminPage.test.tsx`
  - usuário com `Webhooks.Visualizar` vê listagem sem ações de gestão
  - usuário com `Webhooks.Gerenciar` pode gerar e revogar
  - diálogo exibe token em texto claro após criação

## Decisões técnicas

- Não foi alterado o endpoint Stripe webhook (`/api/webhooks/stripe`) nem callback legacy de pagamentos.
- Não foi introduzido novo tipo de token; a gestão admin reutiliza a infraestrutura de Partner API keys já existente.
- Rotação nesta fase é operacional: gerar novo token e revogar o antigo.
