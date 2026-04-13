# 📄 AGENTS.md — Sistema de Sócio Torcedor (Single Tenant)

## 🧠 Visão Geral

Este projeto é um sistema de **sócio torcedor para um único clube**, desenvolvido como um **monólito modular**.

### Stack principal

* **Backend:** .NET 10 + ASP.NET Core + Identity + CQRS + SQL Server
* **Frontend Web:** React + Vite + Axios (SPA)
* **Mobile:** Flutter

---

## 🎯 Objetivo do Sistema

Permitir:

* cadastro externo de usuários
* gestão de sócios e não sócios
* controle de planos e assinaturas
* pagamentos recorrentes (PIX e cartão)
* carteirinha digital
* resgate e visualização de ingressos
* notícias e notificações
* ranking/fidelidade
* atendimento/chamados
* controle administrativo completo com permissões granulares

---

## 🏗️ Diretrizes Arquiteturais

### Estilo

* Monólito
* Modularização por domínio
* CQRS
* Clean Architecture (por módulo)

### NÃO utilizar

* Multitenancy
* Microservices
* Arquitetura distribuída

---

## ⚠️ Princípios Obrigatórios

### 1. TDD (Test Driven Development)

Este projeto **DEVE obrigatoriamente seguir TDD**.

#### Regras:

* Nenhuma funcionalidade pode ser implementada sem teste
* Sempre seguir ciclo:

  1. Escrever teste (falhando)
  2. Implementar código mínimo
  3. Refatorar

#### Tipos de testes obrigatórios:

* Unitários (Domain + Application)
* Integração (Infrastructure)
* Testes de API (Controllers)

---

### 2. Atualização de documentação (MANDATÓRIO)

Ao final de **cada execução do agente**, deve-se:

* Atualizar documentação do projeto
* Registrar:

  * funcionalidades criadas
  * decisões técnicas
  * endpoints adicionados
  * mudanças em regras de negócio
* Garantir consistência entre código e documentação

---

### 3. Separação de responsabilidades

O sistema deve separar claramente:

* **Conta (User)**
* **Permissões (Roles/Permissions)**
* **Associação (Membership)**

Esses conceitos **NÃO podem ser acoplados**.

---

## 👤 Modelo de Usuário

### Regras

* qualquer pessoa pode se cadastrar
* nem todo usuário é sócio
* nem todo usuário tem acesso administrativo
* nem todo sócio é administrador

---

## 🧩 Camadas conceituais

### 1. Conta (Identity)

Responsável por:

* login
* senha
* autenticação
* login social

---

### 2. Perfil funcional

Define o que o usuário pode fazer:

* administrador
* financeiro
* atendimento
* marketing
* operador
* torcedor

---

### 3. Associação (Membership)

Define se o usuário é sócio:

* não associado
* ativo
* inadimplente
* suspenso
* cancelado

---

## 🔐 Perfis do sistema

* Administrador Master (seed inicial)
* Administrador
* Financeiro
* Atendimento
* Marketing
* Operador
* Torcedor

---

## 🔑 Permissões (granular)

Exemplo:

```text
Usuarios.Visualizar
Usuarios.Editar
Socios.Gerenciar
Planos.Visualizar
Planos.Criar
Pagamentos.Estornar
Jogos.Visualizar
Jogos.Criar
Jogos.Editar
Ingressos.Visualizar
Ingressos.Gerenciar
Noticias.Publicar
Fidelidade.Visualizar
Fidelidade.Gerenciar
Beneficios.Visualizar
Beneficios.Gerenciar
Chamados.Responder
Carteirinha.Visualizar
Carteirinha.Gerenciar
Configuracoes.Editar
```

---

## 🧱 Módulos do Sistema

### 1. Identity

* autenticação
* login social
* roles
* permissões
* LGPD

### 2. Users

* cadastro
* dados pessoais
* ativação/inativação

### 3. Membership

* status de sócio
* vínculo com plano
* histórico

### 4. Plans

* planos
* benefícios
* regras

### 5. Payments

* PIX
* cartão
* recorrência
* inadimplência

### 6. Digital Card

* carteirinha
* exibição

### 7. Games

* jogos
* regras

### 8. Tickets

* integração externa
* resgate
* QR Code

### 9. News

* notícias
* notificações

### 10. Loyalty

* pontos
* ranking

### 11. Benefits

* parceiros
* vantagens

### 12. Support

* chamados
* atendimento

### 13. Administration

* permissões
* usuários internos
* configurações

---

## 🗄️ Modelagem principal

### ApplicationUser

* Id
* Name
* Email
* PasswordHash
* PhoneNumber
* IsActive
* CreatedAt

---

### UserProfile

* UserId
* Document
* BirthDate
* PhotoUrl
* Address

---

### Membership

* Id
* UserId
* PlanId
* Status
* StartDate
* EndDate
* NextDueDate

#### Status:

```text
NaoAssociado
Ativo
Inadimplente
Suspenso
Cancelado
PendingPayment
```

---

### MembershipPlan

* Id
* Name
* Price
* BillingCycle
* DiscountPercentage
* IsActive

---

### Payment

* Id
* UserId
* MembershipId
* Amount
* Status
* DueDate
* PaidAt

---

### Game

* Id
* Opponent
* Competition
* GameDate

---

### Ticket

* Id
* UserId
* GameId
* ExternalTicketId
* QrCode
* Status

---

## ⚙️ CQRS

### Commands

* CreateUser
* RegisterUser
* SubscribeMember
* ChangePlan
* RegisterPayment
* CreateGame
* RedeemTicket
* PublishNews

---

### Queries

* GetUserProfile
* GetMembershipStatus
* ListPlans
* ListGames
* GetNewsFeed
* GetTickets

---

## 💳 Pagamentos

### Interface obrigatória

```csharp
public interface IPaymentProvider
{
    Task CreateSubscriptionAsync(...);
    Task CreatePixAsync(...);
    Task CreateCardAsync(...);
    Task CancelAsync(...);
}
```

---

## 🎟️ Ingressos

### Interface obrigatória

```csharp
public interface ITicketProvider
{
    Task ReserveAsync(...);
    Task PurchaseAsync(...);
    Task GetAsync(...);
}
```

---

## 🌐 Frontend Web

### Stack

* React
* Vite
* Axios

### Estrutura

```text
src/
  app/
  shared/
  features/
  pages/
```

---

## 📱 Mobile (Flutter)

### Funcionalidades

* login
* cadastro
* assinatura
* carteirinha
* ingressos (QR Code)
* notícias
* perfil
* chamados

---

## ⚖️ LGPD

Obrigatório:

* aceite de termos
* aceite de política
* versionamento
* data/hora do aceite

---

## 🐳 Infraestrutura

* Docker obrigatório
* SQL Server
* API .NET
* React
* Flutter
* VPS

---

## 🚀 Fases de Implementação

### Fase 1 — Fundação

* arquitetura base
* Identity
* seed admin master
* testes iniciais
* Swagger

### Fase 2 — Usuários

* cadastro externo
* perfis
* permissões

### Fase 3 — Sócio

* planos
* assinatura
* status
* carteirinha

### Fase 4 — Pagamentos

* recorrência
* PIX
* cartão

### Fase 5 — Jogos e ingressos

### Fase 6 — Notícias

### Fase 7 — Fidelidade

### Fase 8 — Atendimento

---

## ⚠️ Regras Críticas

### O sistema deve permitir:

* usuários sem plano
* sócios ativos
* administradores sem plano
* regras diferentes por status

---

## ✅ Conclusão

Este sistema deve ser:

* simples (monólito)
* bem estruturado
* altamente testável (TDD)
* desacoplado internamente
* preparado para evolução futura

---

## 📌 Regras finais obrigatórias para o agente

1. Sempre usar TDD
2. Nunca implementar sem testes
3. Atualizar documentação ao final de cada execução
4. Manter separação entre User, Membership e Permissions
5. Não acoplar regras de negócio indevidamente
6. Garantir código limpo e organizado
7. Seguir arquitetura modular definida

---

## Estado do repositório (bootstrap)

* **Backend:** solução em `backend/` — `AppTorcedor.Api` + `AppTorcedor.Application` (CQRS/MediatR) + `AppTorcedor.Identity` + `AppTorcedor.Infrastructure`; Identity com JWT (access + refresh) e claims de **permissões granulares**; seed de **Administrador Master**, roles base e catálogo de permissões (todas atribuídas ao Master); auditoria (`AuditLogs`), configurações (`AppConfigurationEntries`), entidades iniciais de sócio/plano/pagamento; health checks e correlação HTTP; testes xUnit (API, Application, Infrastructure, Identity).
* **Frontend:** `frontend/` — React + Vite, login, **cadastro público (`/register`)**, **Minha conta (`/account`)**, **catálogo de planos (`/plans`, D.1)** e **checkout de assinatura (`/plans/:planId/checkout`, D.4)**, **carteirinha torcedor (`/digital-card`, C.3)**, login Google opcional (GIS + `VITE_GOOGLE_CLIENT_ID`), convite staff (`/accept-staff-invite`), armazenamento de tokens, interceptor Axios com refresh, rotas protegidas, **`/api/auth/me` com `permissions` e `requiresProfileCompletion`** e painel **`/admin`** (dashboard, staff, diagnóstico, configurações, auditoria, role × permissão editável, **membership admin (B.4)** listagem/histórico/status+motivo, **planos (B.5)** CRUD/publicação e benefícios por plano, **pagamentos (B.6)** listagem/conciliação/cancelamento/estorno, **carteirinha digital (B.7)** listagem/preview/emissão/regeneração/invalidação, **jogos e ingressos (B.8)** CRUD de jogos e gestão de ingressos com provedor mock, **notícias (B.9)**, **fidelidade e benefícios (B.10)**, **suporte/chamados (B.11)** filas/SLA/histórico com `Chamados.Responder`, **LGPD** documentos/consentimentos/dados); ver `docs/frontend/backoffice.md`.
* **Infra local:** `docker-compose.yml` (SQL Server opcional); documentação em `docs/architecture/auth-bootstrap.md`, `docs/architecture/parte-a-fundacao.md`, `docs/architecture/parte-b2-lgpd.md`, `docs/architecture/parte-b3-users-admin.md`, `docs/architecture/parte-b4-membership-admin.md`, `docs/architecture/parte-b5-plans-admin.md`, `docs/architecture/parte-b6-payments-admin.md`, `docs/architecture/parte-b7-digital-card-admin.md`, `docs/architecture/parte-b8-games-tickets-admin.md`, `docs/architecture/parte-b9-news-admin.md`, `docs/architecture/parte-b10-loyalty-benefits-admin.md`, `docs/architecture/parte-b11-support-admin.md`, `docs/architecture/parte-c1-cadastro-perfil-torcedor.md`, `docs/architecture/parte-c2-noticias-beneficios-torcedor.md`, `docs/architecture/parte-c3-carteirinha-torcedor.md`, `docs/architecture/parte-c4-jogos-ingressos-torcedor.md`, `docs/architecture/parte-c5-fidelidade-torcedor.md`, `docs/architecture/parte-d1-catalogo-planos-torcedor.md`, `docs/architecture/parte-d2-simulacao-detalhe-plano-torcedor.md`, `docs/architecture/parte-d3-contratacao-backend.md`, `docs/architecture/parte-d4-integracao-pagamento-contratacao.md`, `docs/frontend/backoffice.md` e `README.md`.
* **B.3 Users (admin):** tabela `UserProfiles`, API `GET/PATCH/PUT /api/admin/users...`, SPA `/admin/users`; exportação/anonimização LGPD inclui/remove perfil estendido.
* **B.4 Membership (admin):** tabela `MembershipHistories`, API `GET /api/admin/memberships`, `GET .../{id}`, `GET .../{id}/history`, `PATCH .../{id}/status` com `reason`; SPA `/admin/membership`; ver `docs/architecture/parte-b4-membership-admin.md`.
* **B.5 Plans (admin):** tabela `MembershipPlanBenefits`, campos de publicação em `MembershipPlans`, API `GET/POST /api/admin/plans`, `GET/PUT /api/admin/plans/{id}`; permissões `Planos.Visualizar`, `Planos.Criar`, `Planos.Editar`; SPA `/admin/plans`; ver `docs/architecture/parte-b5-plans-admin.md`.
* **B.6 Payments (admin):** evolução da tabela `Payments`, API `GET /api/admin/payments`, `GET /api/admin/payments/{id}`, `POST .../conciliate`, `.../cancel`, `.../refund`; permissões `Pagamentos.Visualizar`, `Pagamentos.Gerenciar`, `Pagamentos.Estornar`; `IPaymentProvider` mock; sweep de inadimplência (`IPaymentDelinquencySweep` + hosted service); SPA `/admin/payments`; ver `docs/architecture/parte-b6-payments-admin.md`.
* **B.7 Digital Card (admin):** tabela `DigitalCards`, API `GET /api/admin/digital-cards`, `GET .../{id}`, `POST .../issue`, `POST .../{id}/regenerate`, `POST .../{id}/invalidate`; permissões `Carteirinha.Visualizar`, `Carteirinha.Gerenciar`; template de exibição fixo em código na API; SPA `/admin/digital-cards`; ver `docs/architecture/parte-b7-digital-card-admin.md`.
* **B.8 Games & Tickets (admin):** tabelas `Games`, `Tickets`; API `GET/POST /api/admin/games`, `GET/PUT/DELETE /api/admin/games/{id}` (DELETE desativa), `GET /api/admin/tickets`, `GET .../{id}`, `POST .../reserve`, `POST .../{id}/purchase`, `.../sync`, `.../redeem`; permissões `Jogos.Visualizar`, `Jogos.Criar`, `Jogos.Editar`, `Ingressos.Visualizar`, `Ingressos.Gerenciar`; `ITicketProvider` com `MockTicketProvider` (singleton); SPA `/admin/games`, `/admin/tickets`; ver `docs/architecture/parte-b8-games-tickets-admin.md`.
* **B.9 News (admin):** tabelas `NewsArticles`, `InAppNotifications`; API `GET/POST /api/admin/news`, `GET/PUT /api/admin/news/{id}`, `POST .../publish`, `.../unpublish`, `.../notifications`; permissão `Noticias.Publicar`; notificações **in-app** imediatas ou agendadas (`IInAppNotificationDispatchService` + hosted service); SPA `/admin/news`; ver `docs/architecture/parte-b9-news-admin.md`.
* **B.10 Loyalty & Benefits (admin):** tabelas `LoyaltyCampaigns`, `LoyaltyPointRules`, `LoyaltyPointLedgerEntries`, `BenefitPartners`, `BenefitOffers`, elegibilidades e `BenefitRedemptions`; API `GET/POST/PUT /api/admin/loyalty/...`, rankings, ajuste manual; `GET/POST/PUT /api/admin/benefits/...` e resgate; permissões `Fidelidade.Visualizar`, `Fidelidade.Gerenciar`, `Beneficios.Visualizar`, `Beneficios.Gerenciar`; gatilhos de pontos pós-conciliação de pagamento e pós-compra/resgate de ingresso (`ILoyaltyPointsTriggerPort`); SPA `/admin/loyalty`, `/admin/benefits`; ver `docs/architecture/parte-b10-loyalty-benefits-admin.md`.
* **B.11 Support (admin):** tabelas `SupportTickets`, `SupportTicketMessages`, `SupportTicketHistories`; API `GET/POST /api/admin/support/tickets`, `GET .../{id}`, `POST .../{id}/reply`, `.../assign`, `.../status`; permissão `Chamados.Responder`; dashboard `openSupportTickets` com contagem de abertos; SPA `/admin/support`; ver `docs/architecture/parte-b11-support-admin.md`.
* **C.3 Carteirinha (torcedor):** `GET /api/account/digital-card` (JWT); `IDigitalCardTorcedorPort` + `GetMyDigitalCardQuery`; estados `NotAssociated` / `MembershipInactive` / `AwaitingIssuance` / `Active`; SPA `/digital-card` com cache local opcional (`cacheValidUntilUtc`); ver `docs/architecture/parte-c3-carteirinha-torcedor.md`.
* **C.4 Jogos e ingressos (torcedor):** `GET /api/games`, `GET /api/tickets`, `GET /api/tickets/{id}`, `POST /api/tickets/{id}/redeem` (JWT); `IGameTorcedorReadPort`, `ITicketTorcedorPort` + queries/commands em `Modules/Torcedor`; SPA `/games` e `/tickets`; ver `docs/architecture/parte-c4-jogos-ingressos-torcedor.md`.
* **C.5 Fidelidade (torcedor):** `GET /api/loyalty/me/summary`, `GET /api/loyalty/rankings/monthly`, `GET /api/loyalty/rankings/all-time` (JWT); `ILoyaltyTorcedorReadPort` + queries em `Modules/Torcedor`; SPA `/loyalty`; ver `docs/architecture/parte-c5-fidelidade-torcedor.md`.
* **D.1 Catálogo de planos (torcedor):** `GET /api/plans` (JWT; sem exigir sócio); `ITorcedorPublishedPlansReadPort` + `ListPublishedPlansQuery`; planos `IsPublished` e `IsActive`; SPA `/plans` e `plansService.listPublished()`; ver `docs/architecture/parte-d1-catalogo-planos-torcedor.md`.
* **D.2 Detalhe / simulação de plano (torcedor):** `GET /api/plans/{id}` (JWT; sem exigir sócio; só planos publicados e ativos, senão 404); `GetPlanDetailsQuery` + `GetPublishedActiveByIdAsync`; SPA `/plans/:planId` e `plansService.getById()`; ver `docs/architecture/parte-d2-simulacao-detalhe-plano-torcedor.md`.
* **D.3 Contratação — command (torcedor, backend):** `SubscribeMemberCommand` + `SubscribeMemberCommandHandler`; `ITorcedorMembershipSubscriptionPort` + `TorcedorMembershipSubscriptionService`; status inicial **`PendingPayment`**; histórico `MembershipHistoryEventTypes.Subscribed`; evento MediatR `MemberSubscribedEvent`; testes em Application + Infrastructure; ver `docs/architecture/parte-d3-contratacao-backend.md` (endpoint HTTP em D.4).
* **D.4 Checkout e pagamento (torcedor):** `POST /api/subscriptions` (JWT) + `POST /api/subscriptions/payments/callback` (segredo `Payments:WebhookSecret`); `CreateTorcedorSubscriptionCheckoutCommand` / `ConfirmTorcedorSubscriptionPaymentCommand` + `ITorcedorSubscriptionCheckoutPort` + `TorcedorSubscriptionCheckoutService`; `IPaymentProvider` retorna instruções PIX/cartão (`MockPaymentProvider`); SPA `/plans/:planId/checkout` e `subscriptionsService.subscribe()`; ver `docs/architecture/parte-d4-integracao-pagamento-contratacao.md`.
* **D.5 Pós-contratação (torcedor):** `GetMySubscriptionSummaryQuery` + `GET /api/account/subscription` (JWT); `ITorcedorSubscriptionSummaryPort` + `TorcedorSubscriptionSummaryReadService` (membership, plano, último pagamento, carteirinha via `IDigitalCardTorcedorPort`); SPA `/subscription/confirmation` (redirect pós-checkout), `subscriptionsService.getMySummary()`, resumo em `/account`; ver `docs/architecture/parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md`.
* **D.6 Troca de plano (torcedor):** `ChangePlanCommand` + `PUT /api/account/subscription/plan` (JWT); `ITorcedorPlanChangePort` + `TorcedorPlanChangeService` (proporcional, cancela cobranças abertas, nova cobrança Pix/cartão); callback D.4 reconhece proporcional em membership **Ativa**; histórico `PlanChanged`; SPA `/account` seção "Trocar plano" com `plansService.listPublished()` e `subscriptionsService.changePlan()`; ver `docs/architecture/parte-d6-troca-plano-torcedor.md`.
* **D.7 Cancelamento (torcedor):** `CancelMembershipCommand` + `DELETE /api/account/subscription` (JWT); `ITorcedorMembershipCancellationPort` + `TorcedorMembershipCancellationService` (política `Membership.CancellationCoolingOffDays` em `AppConfigurationEntries`, `IPaymentProvider.CancelAsync`, histórico `CancelledByMember`; fora do prazo de arrependimento mantém `Ativo` até `NextDueDate` com `EndDate` e sweep `IMembershipScheduledCancellationEffectiveSweep` no `PaymentDelinquencySweep`); SPA `/account` com `subscriptionsService.cancelMembership()`; ver `docs/architecture/parte-d7-cancelamento-torcedor.md`.
* **C.6 Suporte (torcedor):** `GET/POST /api/support/tickets`, `GET /api/support/tickets/{id}`, `POST .../reply`, `.../cancel`, `.../reopen`, `GET .../attachments/{attachmentId}` (JWT; sem exigir sócio); anexos em `SupportTicketMessageAttachments` + `ISupportTicketAttachmentStorage`; download staff em `GET /api/admin/support/tickets/{id}/attachments/{attachmentId}`; SPA `/support`; ver `docs/architecture/parte-c6-suporte-torcedor.md`.