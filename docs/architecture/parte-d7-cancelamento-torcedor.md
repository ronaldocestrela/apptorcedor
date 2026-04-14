# Parte D.7 — Cancelamento pelo torcedor

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.7) e [AGENTS.md](../../AGENTS.md): o torcedor autenticado pode solicitar cancelamento da assinatura; a política de **arrependimento** é configurável em `AppConfigurationEntries`; cobranças abertas são canceladas no provedor via `IPaymentProvider.CancelAsync`; o histórico registra o evento `CancelledByMember`.

## Política de negócio

- **Chave de configuração:** `Membership.CancellationCoolingOffDays` — inteiro ≥ 0 (máx. 365). Se ausente ou inválido, usa **7** dias (`TorcedorMembershipCancellationDefaults.DefaultCoolingOffDays`).
- **Janela de arrependimento:** `daysSinceStart = (UtcNow - Membership.StartDate).TotalDays`; dentro da janela se `daysSinceStart <= coolingOffDays`.
- **Dentro da janela** (ou `PendingPayment`): cancelamento **imediato** — `Membership.Status = Cancelado`, `NextDueDate = null`, `EndDate = UtcNow`, cobranças `Pending`/`Overdue` canceladas.
- **Fora da janela** (somente `Ativo`): cancelamento **agendado** — mantém `Status = Ativo`, define `EndDate = NextDueDate` (acesso até essa data), cancela cobranças em aberto do ciclo; ao passar `EndDate`, o sweep efetiva `Cancelado`.
- **Efetivação agendada:** `IMembershipScheduledCancellationEffectiveSweep` — memberships `Ativo` com `EndDate <= UtcNow` passam a `Cancelado` e recebem histórico `StatusChanged` (motivo operacional). Executado no final de `IPaymentDelinquencySweep.RunAsync` (hosted service existente, exceto ambiente de testes que não roda o loop).
- **Idempotência / conflitos:** segundo pedido com cancelamento já agendado (`Ativo` + `EndDate` futuro) → `cancellation_already_scheduled`; `Cancelado` → `membership_already_cancelled`; se `EndDate` já vencido e ainda `Ativo`, o serviço efetiva o encerramento e retorna `membership_already_cancelled`.

## Separação de responsabilidades

- **Conta:** JWT em `/api/account/...`; `userId` nas claims.
- **Associação + pagamentos:** `TorcedorMembershipCancellationService` (Infrastructure) — EF + `IPaymentProvider` + `IAppConfigurationPort`; sem permissões administrativas.

## Backend

### Porta

- `ITorcedorMembershipCancellationPort` — `CancelMembershipAsync(userId, ct)` em `Application/Abstractions/TorcedorMembershipCancellationContracts.cs`.
- DTOs: `CancelMembershipResult`, `CancelMembershipError`, `TorcedorMembershipCancellationMode`.

### CQRS (Application)

- `CancelMembershipCommand` + `CancelMembershipCommandHandler` → delega à porta (`Modules/Account/Commands/CancelMembership/`).

### Infraestrutura

- `TorcedorMembershipCancellationService` (`Infrastructure/Services/Payments/`).
- `MembershipScheduledCancellationEffectiveSweep` — implementa `IMembershipScheduledCancellationEffectiveSweep`.
- Registro DI: `ITorcedorMembershipCancellationPort`, `IMembershipScheduledCancellationEffectiveSweep` em `DependencyInjection.cs`.
- `PaymentDelinquencySweep` chama o sweep de cancelamento efetivo após delinquência.

### Histórico

- Constante `MembershipHistoryEventTypes.CancelledByMember` em `Infrastructure/Entities/MembershipHistoryRecord.cs`.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| DELETE | `/api/account/subscription` | JWT | Cancela ou agenda cancelamento conforme política. |

**200** — corpo `TorcedorCancelMembershipResponse`: `membershipId`, `membershipStatus`, `mode` (`Immediate` \| `ScheduledEndOfCycle`), `accessValidUntilUtc`, `message`.

Erros típicos:

- `404` `membership_not_found`
- `409` `membership_already_cancelled`
- `409` `cancellation_already_scheduled`
- `409` `membership_not_cancellable`
- `409` `missing_billing_context`

Controller: `AccountController.CancelSubscription`.

## Frontend (SPA)

- `/account` — seção **Cancelar assinatura** quando elegível (`Ativo` ou `PendingPayment`, sem cancelamento já agendado); modal de confirmação; `subscriptionsService.cancelMembership()` (`DELETE`).
- Mensagem quando já existe cancelamento agendado (`Ativo` + `endDate`).
- Feedback pós-sucesso com `message` e, se `ScheduledEndOfCycle`, data de acesso.

## Testes (TDD)

- **Application:** `CancelMembershipCommandHandlerTests`.
- **Infrastructure:** `TorcedorMembershipCancellationServiceTests` (imediato, agendado, já cancelado, segundo pedido, sweep, config inválida).
- **API:** `PartD7CancelSubscriptionApiTests`.
- **Frontend:** `subscriptionsService.test.ts`, `AccountPage.test.tsx`.

## Referências

- [parte-d4-integracao-pagamento-contratacao.md](parte-d4-integracao-pagamento-contratacao.md)
- [parte-d6-troca-plano-torcedor.md](parte-d6-troca-plano-torcedor.md)
