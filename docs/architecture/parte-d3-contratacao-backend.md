# Parte D.3 — Contratação (backend): `SubscribeMember`

Implementação alinhada ao [ROADMAP-PENDENCIAS.md](../ROADMAP-PENDENCIAS.md) (D.3) e [AGENTS.md](../../AGENTS.md): comando de aplicação para **iniciar contratação** de plano pelo torcedor, persistindo `Membership` em **`PendingPayment`**, registrando histórico operacional e publicando evento interno MediatR. **Não** expõe endpoint HTTP nesta entrega (D.4); **não** integra `IPaymentProvider` aqui.

## Decisão de produto

- Status inicial após “subscrever”: **`MembershipStatus.PendingPayment`** (valor inteiro `5` no armazenamento).
- **Uma linha de `Memberships` por usuário** (`UserId` único): primeira contratação **insere**; reentrada permitida apenas a partir de `NaoAssociado` ou `Cancelado` (atualiza a mesma linha).
- Bloqueios:
  - **`Ativo`**: não permitir nova contratação (`AlreadyActiveSubscription`).
  - **`PendingPayment`**: não permitir segunda contratação paralela (`SubscriptionPendingPayment`).
  - **`Inadimplente`** / **`Suspenso`**: bloqueado até regularização administrativa (`MembershipStatusPreventsSubscribe`).
- Plano deve estar **`IsPublished && IsActive`** (mesma regra do catálogo D.1/D.2); caso contrário `PlanNotFoundOrNotAvailable`.

## Separação de responsabilidades

- **Conta (User):** apenas `UserId` do contexto autenticado (quem chamar o comando no futuro endpoint D.4).
- **Membership:** criação/atualização de `Membership`, histórico em `MembershipHistories`, sem misturar permissões administrativas.
- **Permissões:** não exigidas no command em si; autorização ficará no controller D.4 (`[Authorize]`).

## Backend

### Porta de escrita

- `ITorcedorMembershipSubscriptionPort` + `SubscribeMemberResult` / `SubscribeMemberError` em `AppTorcedor.Application.Abstractions` (`TorcedorMembershipSubscriptionContracts.cs`).
- Implementação: `TorcedorMembershipSubscriptionService` (`Infrastructure/Services/Membership`), registrada em `DependencyInjection` como scoped.

### CQRS (Application)

- `SubscribeMemberCommand` / `SubscribeMemberCommandHandler` em `Modules/Torcedor/Commands/SubscribeMember`.
- Após sucesso da persistência, o handler publica **`MemberSubscribedEvent`** (`INotification`) via **`IPublisher`** (MediatR). Handlers futuros podem assinar o evento (fidelidade, emissão de carteirinha, etc.).

### Histórico

- `MembershipHistoryEventTypes.Subscribed` (`"Subscribed"`), com `FromStatus` / `ToStatus`, `FromPlanId` / `ToPlanId`, `Reason` fixa operacional e `ActorUserId = userId` (autosserviço).

### Migração EF

- `PartD3PendingPaymentMembershipStatus`: **sem alteração de esquema** (status já persistido como `int` em `Memberships.Status`); migration documenta a versão do modelo após o novo valor de enum.

### Testes

- **Application:** `SubscribeMemberCommandHandlerTests` — delegação ao port e publicação (ou não) do evento.
- **Infrastructure:** projeto `AppTorcedor.Infrastructure.Tests` — persistência, histórico e regras de bloqueio com EF InMemory.

## Matriz de erros (`SubscribeMemberError`)

| Valor | Condição |
|--------|-----------|
| `PlanNotFoundOrNotAvailable` | Plano inexistente, inativo ou não publicado no canal torcedor |
| `AlreadyActiveSubscription` | `Membership.Status == Ativo` |
| `SubscriptionPendingPayment` | `Membership.Status == PendingPayment` |
| `MembershipStatusPreventsSubscribe` | `Inadimplente` ou `Suspenso` |

## Impacto em outros fluxos

- **Carteirinha (C.3):** `PendingPayment` tratado como associação inativa na UI da carteirinha (mensagem específica).
- **Sweep de inadimplência:** continua atuando apenas em membros **`Ativo`** com cobrança vencida.
- **Admin / histórico:** eventos `Subscribed` aparecem na listagem de histórico do membership (mesma tabela; `EventType` distinto de `StatusChanged`).

## Próximos passos (D.4+)

- Endpoint HTTP + `IPaymentProvider` e transição `PendingPayment` → `Ativo` após confirmação de pagamento.
