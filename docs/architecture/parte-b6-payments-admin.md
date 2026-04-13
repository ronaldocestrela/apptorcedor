# Parte B.6 — Payments (visão financeira / admin)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (B.6) e [AGENTS.md](../../AGENTS.md): listagem administrativa de cobranças (`PaymentRecord`), estados canônicos de cobrança, conciliação manual (marcar pago), cancelamento e estorno com permissões distintas, `IPaymentProvider` com adaptador **mock**, sweep automático de inadimplência (marca cobranças vencidas e atualiza `Membership` quando aplicável) e SPA `/admin/payments`. Não implementa contratação pública nem fluxo do torcedor (Parte D).

## Modelo de dados

| Tabela | Descrição |
|--------|-----------|
| `Payments` | Cobrança: `UserId`, `MembershipId`, `Amount`, `Status`, `DueDate`, `PaidAt`, `PaymentMethod`, `ExternalReference`, `ProviderName`, `CancelledAt`, `RefundedAt`, `CreatedAt`, `UpdatedAt`, `LastProviderSyncAt`, `StatusReason`. |

Migração EF: `PartB6PaymentsAdmin` em `backend/src/AppTorcedor.Infrastructure/Persistence/Migrations/`.

### Estados de cobrança (canônicos)

Valores persistidos em `Status` (string):

- `Pending` — em aberto, dentro ou fora do prazo (vencimento tratado no sweep).
- `Overdue` — vencida e não paga (atribuído pelo sweep a partir de `Pending` com `DueDate` &lt; agora).
- `Paid` — paga (conciliação manual ou fluxo futuro).
- `Cancelled` — cancelada (ação admin + `IPaymentProvider.CancelAsync`).
- `Refunded` — estornada após paga (ação admin + `IPaymentProvider.RefundAsync`).

## Permissões

- `Pagamentos.Visualizar` — `GET /api/admin/payments`, `GET /api/admin/payments/{id}`.
- `Pagamentos.Gerenciar` — `POST .../conciliate`, `POST .../cancel`.
- `Pagamentos.Estornar` — `POST .../refund`.

O Administrador Master recebe todas as permissões do catálogo via seed (`ApplicationPermissions.All`).

## API administrativa

Base: `api/admin/payments` (JWT + política por permissão). CQRS em `AppTorcedor.Application` e implementação em `PaymentAdministrationService` (`IPaymentsAdministrationPort`).

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/admin/payments?status=&userId=&membershipId=&paymentMethod=&dueFrom=&dueTo=&page=&pageSize=` | Lista paginada. |
| GET | `/api/admin/payments/{paymentId}` | Detalhe. |
| POST | `/api/admin/payments/{paymentId}/conciliate` | Corpo opcional: `{ "paidAt": "..." }`. Marca `Paid`, reativa `Membership` para `Ativo` se estiver `Inadimplente` e não houver outras cobranças `Pending`/`Overdue`. |
| POST | `/api/admin/payments/{paymentId}/cancel` | Corpo opcional: `{ "reason": "..." }`. Apenas `Pending` ou `Overdue`. |
| POST | `/api/admin/payments/{paymentId}/refund` | Corpo opcional: `{ "reason": "..." }`. Apenas `Paid`. |

Respostas de mutação: `204` sucesso; `404` não encontrado; `400` transição inválida.

## Provedor de pagamento

- Interface: `AppTorcedor.Application.Abstractions.IPaymentProvider` (`CreateSubscriptionAsync`, `CreatePixAsync`, `CreateCardAsync`, `CancelAsync`, `RefundAsync`).
- Implementação inicial: `MockPaymentProvider` (no-op, para testes e evolução gradual de gateway).

## Inadimplência automática

- Serviço: `IPaymentDelinquencySweep` / `PaymentDelinquencySweep`.
- `Pending` com `DueDate` &lt; UTC agora → atualiza para `Overdue`.
- `Membership` em `Ativo` com pelo menos uma cobrança `Overdue` → `ApplySystemMembershipTransitionAsync` para `Inadimplente` (histórico com `ActorUserId` nulo).
- Execução: `PaymentDelinquencyHostedService` a cada 5 minutos (desativado em ambiente `Testing`; testes chamam o sweep via DI).

## Relação com B.4 e Parte D

- B.4 continua com alteração manual de status com ator; o sweep usa transições de sistema documentadas no histórico.
- Parte D reutilizará `IPaymentProvider` e o modelo de `Payments` para contratação e recorrência.

## Frontend (backoffice)

- Rota `/admin/payments` (qualquer uma de `Pagamentos.Visualizar` / `Gerenciar` / `Estornar` para entrar no shell; listagem exige `Pagamentos.Visualizar`).
- Serviços: `frontend/src/features/admin/services/adminApi.ts`.

## Testes

- `AppTorcedor.Application.Tests` / `PaymentsAdminHandlersTests`: delegação dos handlers ao port.
- `AppTorcedor.Api.Tests` / `PartB6PaymentsAdminTests`: autorização, listagem, sweep, conciliação com reativação de membership, estorno e validação de conciliação em pagamento já pago.
