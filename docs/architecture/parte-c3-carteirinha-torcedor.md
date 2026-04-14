# Parte C.3 — Carteirinha digital (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (C.3) e [AGENTS.md](../../AGENTS.md): **leitura da carteirinha** para o usuário autenticado, com matriz de exibição por **status de membership** e **emissão ativa**, sem reutilizar rotas `api/admin/*`. Emissão/regeneração/invalidação permanecem no B.7.

## Separação de responsabilidades

- **Conta:** `UserId` obtido exclusivamente do JWT; não há parâmetro de outro usuário.
- **Membership:** define se o torcedor vê conteúdo completo, mensagem de inelegibilidade ou ausência de associação.
- **Permissões administrativas:** não exigidas; rota exige apenas `[Authorize]`.

## Estados de resposta (`MyDigitalCardViewState`)

| Estado | Condição típica |
|--------|------------------|
| `NotAssociated` | Sem linha em `Memberships` **ou** `MembershipStatus.NaoAssociado`. |
| `MembershipInactive` | `Inadimplente`, `Suspenso` ou `Cancelado`. |
| `AwaitingIssuance` | `Ativo` sem registro `DigitalCards` com `Status == Active`. |
| `Active` | `Ativo` com cartão ativo; inclui `templatePreviewLines`, `verificationToken` e `cacheValidUntilUtc`. |

Regras centralizadas em `MyDigitalCardViewFactory` (Application). O layout de linhas do preview reutiliza `DigitalCardTemplatePreview` (paridade com B.7).

## Backend

### Porta- `IDigitalCardTorcedorPort` — `GetMyDigitalCardAsync(userId, ...)`.

### CQRS

- `GetMyDigitalCardQuery` / `GetMyDigitalCardQueryHandler` → delega à porta.

### Infraestrutura

- `DigitalCardTorcedorReadService` — consulta `Memberships`, `DigitalCards`, `Users`, `UserProfiles`, `MembershipPlans`; TTL de cache sugerido ao cliente: **5 minutos** (`cacheValidUntilUtc`).

Registro DI: `IDigitalCardTorcedorPort` em `Infrastructure/DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|------------|
| GET | `/api/account/digital-card` | JWT | Retorna `MyDigitalCardViewDto` (JSON camelCase + enums como string). |

## Frontend (web)

- Rota: `/digital-card` (`DigitalCardPage`).
- Cliente: `getMyDigitalCard` / `getMyDigitalCardWithSource` em `torcedorDigitalCardApi.ts`.
- **Cache / offline limitado:** após sucesso, persiste em `localStorage` (chave `appTorcedor.digitalCard.cache.v1`) enquanto `cacheValidUntilUtc` for futuro; em falha de rede, `getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })` devolve cache válido e indica `fromCache` na UI.

## Testes

- **Application:** `MyDigitalCardViewFactoryTests`, `GetMyDigitalCardQueryHandlerTests`.
- **API:** `PartC3TorcedorDigitalCardTests` (coleção `DigitalCardAdmin` quando altera o membership de exemplo).
- **Frontend:** `torcedorDigitalCardApi.test.ts`, `DigitalCardPage.test.tsx`.

## Referências

- B.7 (gestão): [parte-b7-digital-card-admin.md](parte-b7-digital-card-admin.md).
