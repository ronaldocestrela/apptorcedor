# Parte D.6 — Troca de plano (torcedor)

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.6) e [AGENTS.md](../../AGENTS.md): torcedor com assinatura **Ativa** pode trocar para outro plano **publicado e ativo**, com **ajuste proporcional** sobre os dias restantes até `NextDueDate`, **cancelamento** de cobranças `Pending`/`Overdue` do ciclo via `IPaymentProvider.CancelAsync`, e **nova cobrança** Pix/cartão quando o proporcional é maior que zero.

**Fora de escopo:** cancelamento pelo torcedor (D.7), gateway real, crédito em dinheiro no downgrade (proporcional negativo é tratado como zero — sem estorno automático).

## Decisões de produto / técnicas

- **Elegibilidade:** apenas `Membership.Status == Ativo`, com `PlanId` e `NextDueDate` preenchidos (ciclo atual).
- **Proporcional:** \( (P_{novo}^{efetivo} - P_{atual}^{efetivo}) \times \frac{diasRestantes}{diasTotaisDoCiclo} \), com preço efetivo = preço com desconto percentual do plano (mesma ideia do checkout D.4), arredondamento em 2 casas e piso em zero.
- **Ciclo:** `cycleEnd = NextDueDate`; `cycleStart = SubtractBillingCycle(cycleEnd, billingCycle)` (Monthly / Quarterly / Yearly, paridade com D.4).
- **Pagamentos em aberto:** todas as cobranças `Pending` ou `Overdue` da membership são canceladas no provedor e marcadas `Cancelled` antes da nova cobrança.
- **Nova cobrança:** `StatusReason` com prefixo `TorcedorPlanChangePaymentReasons.ProrationPrefix` (`Application/Abstractions/TorcedorPlanChangeContracts.cs`), reconhecida no callback D.4 quando a membership já está **Ativa** (não altera status nem `NextDueDate`; apenas marca pago e dispara fidelidade).
- **Histórico:** `MembershipHistoryEventTypes.PlanChanged`, `FromStatus`/`ToStatus` = `Ativo`, `FromPlanId`/`ToPlanId` e `ActorUserId` = torcedor.

## Separação de responsabilidades

- **Conta:** JWT em `/api/account/...`; `userId` nas claims.
- **Associação + pagamentos:** `TorcedorPlanChangeService` (Infrastructure) orquestra EF + `IPaymentProvider`; sem permissões administrativas.

## Backend

### Porta de escrita

- `ITorcedorPlanChangePort` — `ChangePlanAsync(userId, newPlanId, paymentMethod, ct)` em `Application/Abstractions/TorcedorPlanChangeContracts.cs`.
- DTOs: `ChangePlanResult`, `ChangePlanPlanSnapshotDto`, `ChangePlanError`.

### CQRS (Application)

- `ChangePlanCommand` + `ChangePlanCommandHandler` → delega à porta (`Modules/Account/Commands/ChangePlan/`).

### Infraestrutura

- `TorcedorPlanChangeService` (`Infrastructure/Services/Payments/`) — regras D.6, injeta `TimeProvider` (registro `TimeProvider.System` + testes com relógio fixo).
- Registro DI: `ITorcedorPlanChangePort` → `TorcedorPlanChangeService` em `DependencyInjection.cs`.
- Callback: `TorcedorSubscriptionCheckoutService.ConfirmPaymentAsync` estendido para pagamentos de proporcional em membership **Ativa**.

### API (torcedor)

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| PUT | `/api/account/subscription/plan` | JWT | Corpo: `{ "planId", "paymentMethod": "Pix" \| "Card" }`. Resposta: planos de/para, `prorationAmount`, `paymentId` (nulo se proporcional zero), `pix` ou `card`. |

Erros típicos:

- `404` `membership_not_found`
- `409` `membership_not_active`, `missing_billing_context`
- `400` `plan_not_available`, `same_plan`

Controller: `AccountController.ChangeSubscriptionPlan`.

## Frontend (SPA)

- `/account` — seção **Trocar plano** quando `membershipStatus === 'Ativo'` e há plano atual: catálogo `plansService.listPublished()`, exclusão do plano atual, seleção de método Pix/Cartão, `subscriptionsService.changePlan()`.
- Exibição pós-sucesso: valor proporcional, instruções Pix ou link de checkout cartão (ou mensagem sem cobrança quando proporcional zero).

## Testes (TDD)

- **Application:** `ChangePlanCommandHandlerTests`.
- **Infrastructure:** `TorcedorPlanChangeServiceTests` (upgrade com proporcional, downgrade zero, confirmação de pagamento em membership ativa).
- **API:** `PartD6ChangePlanApiTests`.
- **Frontend:** `subscriptionsService.test.ts` (PUT), `AccountPage.test.tsx` (fluxo de troca).

## Referências

- [parte-d4-integracao-pagamento-contratacao.md](parte-d4-integracao-pagamento-contratacao.md)
- [parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md](parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md)

## Próximos passos (roadmap)

- D.7 — cancelamento pelo torcedor.
