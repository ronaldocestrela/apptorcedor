# Parte D.5 — Pós-contratação: confirmação e recibo (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.5) e [AGENTS.md](../../AGENTS.md): após o checkout (D.4), o torcedor visualiza **confirmação**, **recibo** (plano, valor da cobrança inicial, próximo vencimento quando disponível) e o **status da carteirinha digital** (semântica C.3), via `GET /api/account/subscription`.

**Fora de escopo:** troca de plano (D.6), cancelamento (D.7), gateway real de pagamento.

## Decisão de produto

- **JWT obrigatório** em `/api/account/subscription` (mesmo padrão de `/api/account/digital-card`).
- **Sem membership persistido:** resposta com `hasMembership: false` (usuário recém-cadastrado ou sem linha em `Memberships`).
- **Carteirinha:** o resumo reutiliza `IDigitalCardTorcedorPort` para não duplicar regras de estado (C.3).
- **Moeda do último pagamento:** exibida como `BRL` no DTO de leitura (alinhado à cobrança D.4; `PaymentRecord` não persiste moeda).
- **Confirmação na SPA:** `POST /api/subscriptions` redireciona para `/subscription/confirmation` com `location.state` (checkout + metadados do plano); instruções PIX/cartão continuam visíveis nessa página. A página também chama `getMySummary()` para enriquecer próximo vencimento e carteirinha.

## Separação de responsabilidades

- **Conta (Identity):** autenticação JWT; `userId` obtido das claims no controller.
- **Associação (Membership) + Pagamentos:** leitura de `Memberships`, `MembershipPlans`, `Payments` apenas para montagem do DTO (sem misturar permissões administrativas).
- **Permissões:** nenhuma permissão granular no torcedor; endpoint não é admin.

## Backend

### Porta de leitura

- `ITorcedorSubscriptionSummaryPort` — `GetMySubscriptionSummaryAsync(userId, ct)` em `Application/Abstractions/ITorcedorSubscriptionSummaryPort.cs`.
- DTOs: `MySubscriptionSummaryDto`, `MySubscriptionSummaryPlanDto`, `MySubscriptionSummaryPaymentDto`, `MySubscriptionSummaryDigitalCardDto`.

### CQRS (Application)

- `GetMySubscriptionSummaryQuery` + `GetMySubscriptionSummaryQueryHandler` → delega à porta (`Modules/Account/Queries/GetMySubscriptionSummary/`).

### Infraestrutura

- `TorcedorSubscriptionSummaryReadService` (`Infrastructure/Services/Payments/`) — EF: membership mais recente por `StartDate`, plano opcional, último pagamento por `PaidAt ?? CreatedAt`, carteirinha via `IDigitalCardTorcedorPort`.
- Registro DI: `ITorcedorSubscriptionSummaryPort` → `TorcedorSubscriptionSummaryReadService` em `DependencyInjection.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/account/subscription` | JWT | Resumo de assinatura, último pagamento e estado da carteirinha. |

Controller: `AccountController.GetMySubscriptionSummary`.

## Frontend (SPA)

- Rota: `/subscription/confirmation` — `SubscriptionConfirmationPage` (recibo + PIX/cartão a partir do state do router).
- Checkout: `SubscriptionCheckoutPage` navega com `replace: true` após sucesso do `subscribe`.
- Serviço: `subscriptionsService.getMySummary()` em `frontend/src/features/plans/subscriptionsService.ts`.
- **Minha conta:** `AccountPage` exibe bloco “Assinatura” (status, próximo vencimento, plano quando houver) ou texto para usuário sem `hasMembership`.

## Testes (TDD)

- **Application:** `GetMySubscriptionSummaryQueryHandlerTests` — delegação à porta.
- **API:** `PartD5SubscriptionSummaryApiTests` — auth, usuário sem membership (cadastro público), membro seed, fluxo subscribe + callback + resumo.
- **Frontend:** `subscriptionsService.test.ts` (GET), `SubscriptionCheckoutPage.test.tsx` (redirect + confirmação), `AccountPage.test.tsx` (bloco de assinatura).

## Referências

- [parte-d4-integracao-pagamento-contratacao.md](parte-d4-integracao-pagamento-contratacao.md)
- [parte-c3-carteirinha-torcedor.md](parte-c3-carteirinha-torcedor.md)

## Próximos passos (roadmap)

- D.6 — `ChangePlan` (troca de plano): ver [parte-d6-troca-plano-torcedor.md](parte-d6-troca-plano-torcedor.md).
- D.7 — cancelamento pelo torcedor.
