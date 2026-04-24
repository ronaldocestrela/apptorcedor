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

* **Backend:** solução em `backend/` — `AppTorcedor.Api` + `AppTorcedor.Application` (CQRS/MediatR) + `AppTorcedor.Identity` + `AppTorcedor.Infrastructure`; Identity com JWT (access + refresh) e claims de **permissões granulares**; seed de **Administrador Master**, roles base e catálogo de permissões (todas atribuídas ao Master); auditoria (`AuditLogs`), configurações (`AppConfigurationEntries`), entidades iniciais de sócio/plano/pagamento; health checks e correlação HTTP; CORS híbrido — `Cors:AllowedOrigins` (deploy) **e** `Cors.AllowedOriginsExtra` (banco, chave administrativa); **storage de mídia por provider** para foto de perfil, **escudo do clube (`TeamShield`)** e anexos (`Local` ou `Cloudinary`) com seleção por configuração; **e-mail transacional** — porta `IEmailSender` com provedor **Mock** (só log) ou **Resend** (SDK oficial); **boas-vindas** após cadastro torcedor (`TorcedorAccountService`); ver `docs/architecture/parte-e1-email-resend.md`; **`GET /api/branding`** (anônimo) e **`POST /api/admin/config/team-shield`** (`Configuracoes.Editar`) persistindo `Brand.TeamShieldUrl`; testes xUnit (API, Application, Infrastructure, Identity).
* **Frontend:** `frontend/` — React + Vite, login, **cadastro público (`/register`)**, **Minha conta (`/account`)**, **catálogo de planos (`/plans`, D.1)** e **checkout de assinatura (`/plans/:planId/checkout`, D.4)**, **carteirinha torcedor (`/digital-card`, C.3)**, login Google opcional (GIS + `VITE_GOOGLE_CLIENT_ID`), convite staff (`/accept-staff-invite`), armazenamento de tokens, interceptor Axios com refresh, rotas protegidas, **`/api/auth/me` com `permissions` e `requiresProfileCompletion`** e painel **`/admin`** (dashboard, staff, diagnóstico, configurações, auditoria, role × permissão editável, **membership admin (B.4)** listagem/histórico/status+motivo, **planos (B.5)** CRUD/publicação e benefícios por plano, **pagamentos (B.6)** listagem/conciliação/cancelamento/estorno, **carteirinha digital (B.7)** listagem/preview/emissão/regeneração/invalidação, **jogos e ingressos (B.8)** CRUD de jogos e gestão de ingressos com provedor mock, **notícias (B.9)**, **fidelidade e benefícios (B.10)**, **suporte/chamados (B.11)** filas/SLA/histórico com `Chamados.Responder`, **LGPD** documentos/consentimentos/dados); ver `docs/frontend/backoffice.md`.
* **Infra local:** `docker-compose.yml` (imagens **API + SPA**; SQL Server em servidor externo via `DATABASE_CONNECTION_STRING`); Dockerfiles em `backend/` e `frontend/`; **CD:** após CI verde no GitHub Actions, job `trigger-jenkins` dispara o Jenkins; pipeline grava `api.env` na VPS a partir do **Jenkins Credentials** e roda `deploy/vps/build-and-deploy.sh` (**git pull** + publish + Vite na VPS); deploy suporta providers de storage por env (`ProfilePhotos__Provider`, `TeamShield__Provider`, `SupportTicketAttachments__Provider`, `Cloudinary__*`), incluindo credenciais Jenkins dedicadas (`support-ticket-attachments-provider`, `cloudinary-cloud-name`, `cloudinary-api-key`, `cloudinary-api-secret`, `email-provider`, `resend-api-key`, `resend-from-address`, `resend-from-name`); ver `docs/architecture/parte-f1-jenkins-cd-pos-ci.md`, `docs/deploy/guia-deploy.md`, `docs/architecture/parte-e1-email-resend.md` e `deploy/vps/api.env.example`; documentação em `docs/architecture/auth-bootstrap.md`, `docs/architecture/parte-a-fundacao.md`, `docs/architecture/parte-b2-lgpd.md`, `docs/architecture/parte-b3-users-admin.md`, `docs/architecture/parte-b4-membership-admin.md`, `docs/architecture/parte-b5-plans-admin.md`, `docs/architecture/parte-b6-payments-admin.md`, `docs/architecture/parte-b7-digital-card-admin.md`, `docs/architecture/parte-b8-games-tickets-admin.md`, `docs/architecture/parte-b9-news-admin.md`, `docs/architecture/parte-b10-loyalty-benefits-admin.md`, `docs/architecture/parte-b11-support-admin.md`, `docs/architecture/parte-c1-cadastro-perfil-torcedor.md`, `docs/architecture/parte-c2-noticias-beneficios-torcedor.md`, `docs/architecture/parte-c3-carteirinha-torcedor.md`, `docs/architecture/parte-c4-jogos-ingressos-torcedor.md`, `docs/architecture/parte-c5-fidelidade-torcedor.md`, `docs/architecture/parte-d1-catalogo-planos-torcedor.md`, `docs/architecture/parte-d2-simulacao-detalhe-plano-torcedor.md`, `docs/architecture/parte-d3-contratacao-backend.md`, `docs/architecture/parte-d4-integracao-pagamento-contratacao.md`, `docs/frontend/backoffice.md` e `README.md`.
* **B.3 Users (admin):** tabela `UserProfiles`, API `GET/PATCH/PUT /api/admin/users...`, SPA `/admin/users`; exportação/anonimização LGPD inclui/remove perfil estendido.
* **B.4 Membership (admin):** tabela `MembershipHistories`, API `GET /api/admin/memberships`, `GET .../{id}`, `GET .../{id}/history`, `PATCH .../{id}/status` com `reason`; SPA `/admin/membership`; ver `docs/architecture/parte-b4-membership-admin.md`.
* **B.5 Plans (admin):** tabela `MembershipPlanBenefits`, campos de publicação em `MembershipPlans`, API `GET/POST /api/admin/plans`, `GET/PUT /api/admin/plans/{id}`; permissões `Planos.Visualizar`, `Planos.Criar`, `Planos.Editar`; SPA `/admin/plans`; ver `docs/architecture/parte-b5-plans-admin.md`.
* **B.6 Payments (admin):** evolução da tabela `Payments`, API `GET /api/admin/payments`, `GET /api/admin/payments/{id}`, `POST .../conciliate`, `.../cancel`, `.../refund`; permissões `Pagamentos.Visualizar`, `Pagamentos.Gerenciar`, `Pagamentos.Estornar`; `IPaymentProvider` mock; sweep de inadimplência (`IPaymentDelinquencySweep` + hosted service); SPA `/admin/payments`; ver `docs/architecture/parte-b6-payments-admin.md`.
* **B.7 Digital Card (admin):** tabela `DigitalCards`, API `GET /api/admin/digital-cards`, `GET .../{id}`, `POST .../issue`, `POST .../{id}/regenerate`, `POST .../{id}/invalidate`; permissões `Carteirinha.Visualizar`, `Carteirinha.Gerenciar`; template de exibição fixo em código na API; SPA `/admin/digital-cards`; ver `docs/architecture/parte-b7-digital-card-admin.md`.
* **B.8 Games & Tickets (admin):** tabelas `Games`, `OpponentLogoAssets`, `Tickets`; campo `Games.OpponentLogoUrl`; API `GET/POST /api/admin/games`, `GET/PUT/DELETE /api/admin/games/{id}` (DELETE desativa), `GET/POST /api/admin/games/opponent-logos` (biblioteca + upload de logo do adversário; política `GamesOpponentLogosUpload` = `Jogos.Criar` **ou** `Jogos.Editar`), `GET /api/admin/tickets`, `GET .../{id}`, `POST .../reserve`, `POST .../{id}/purchase`, `.../sync`, `.../redeem`; permissões `Jogos.Visualizar`, `Jogos.Criar`, `Jogos.Editar`, `Ingressos.Visualizar`, `Ingressos.Gerenciar`; storage `OpponentLogos` (Local/Cloudinary) em `IOpponentLogoStorage`; `ITicketProvider` com `MockTicketProvider` (singleton); SPA `/admin/games`, `/admin/tickets`; ver `docs/architecture/parte-b8-games-tickets-admin.md`.
* **B.9 News (admin):** tabelas `NewsArticles`, `InAppNotifications`; API `GET/POST /api/admin/news`, `GET/PUT /api/admin/news/{id}`, `POST .../publish`, `.../unpublish`, `.../notifications`; permissão `Noticias.Publicar`; notificações **in-app** imediatas ou agendadas (`IInAppNotificationDispatchService` + hosted service); SPA `/admin/news`; ver `docs/architecture/parte-b9-news-admin.md`.
* **B.10 Loyalty & Benefits (admin):** tabelas `LoyaltyCampaigns`, `LoyaltyPointRules`, `LoyaltyPointLedgerEntries`, `BenefitPartners`, `BenefitOffers` (incl. `BannerUrl`), elegibilidades e `BenefitRedemptions`; API `GET/POST/PUT /api/admin/loyalty/...`, rankings, ajuste manual; `GET/POST/PUT /api/admin/benefits/...`, **`POST/DELETE /api/admin/benefits/offers/{id}/banner`** (storage `BenefitOfferBanner`: Local/Cloudinary) e resgate; permissões `Fidelidade.Visualizar`, `Fidelidade.Gerenciar`, `Beneficios.Visualizar`, `Beneficios.Gerenciar`; gatilhos de pontos pós-conciliação de pagamento e pós-compra/resgate de ingresso (`ILoyaltyPointsTriggerPort`); SPA `/admin/loyalty`; **`/admin/benefits`** com CRUD de parceiros e ofertas, **vigência `StartAt`/`EndAt`**, upload de banner, ativar/desativar e exclusão soft (PUT), filtros e status derivado (Vigente/Programada/Expirada/Inativa), testes Vitest em `BenefitsAdminPage.test.tsx` / `benefitsAdminHelpers.test.ts`; ver `docs/architecture/parte-b10-loyalty-benefits-admin.md`.
* **B.11 Support (admin):** tabelas `SupportTickets`, `SupportTicketMessages`, `SupportTicketHistories`; API `GET/POST /api/admin/support/tickets`, `GET .../{id}`, `POST .../{id}/reply`, `.../assign`, `.../status`; permissão `Chamados.Responder`; dashboard `openSupportTickets` com contagem de abertos; SPA `/admin/support`; ver `docs/architecture/parte-b11-support-admin.md`.
* **C.3 Carteirinha (torcedor):** `GET /api/account/digital-card` (JWT); `IDigitalCardTorcedorPort` + `GetMyDigitalCardQuery`; estados `NotAssociated` / `MembershipInactive` / `AwaitingIssuance` / `Active`; SPA `/digital-card` com cache local opcional (`cacheValidUntilUtc`); ver `docs/architecture/parte-c3-carteirinha-torcedor.md`.
* **C.4 Jogos e ingressos (torcedor):** `GET /api/games` (jogos ativos com `GameDate` ≥ UTC agora; itens com `opponentLogoUrl` opcional), `GET /api/tickets`, `GET /api/tickets/{id}`, `POST /api/tickets/{id}/redeem` (JWT); `IGameTorcedorReadPort`, `ITicketTorcedorPort` + queries/commands em `Modules/Torcedor`; SPA `/games` e `/tickets`; ver `docs/architecture/parte-c4-jogos-ingressos-torcedor.md`.
* **C.5 Fidelidade (torcedor):** `GET /api/loyalty/me/summary`, `GET /api/loyalty/rankings/monthly`, `GET /api/loyalty/rankings/all-time` (JWT); `ILoyaltyTorcedorReadPort` + queries em `Modules/Torcedor`; SPA `/loyalty`; ver `docs/architecture/parte-c5-fidelidade-torcedor.md`.
* **D.1 Catálogo de planos (torcedor):** `GET /api/plans` (JWT; sem exigir sócio); `ITorcedorPublishedPlansReadPort` + `ListPublishedPlansQuery`; planos `IsPublished` e `IsActive`; SPA `/plans` (`PlansPage`: wrapper `.plans-root` full-viewport, `subpage-header` com link para `/account`, lista vertical de cards, badge **Mais Popular** no primeiro plano da API, sufixo de preço `/ mês` | `/ ano` | `/ trimestre` conforme `billingCycle`, CTA **Mais detalhes** → `/plans/:planId`) e `plansService.listPublished()`; ver `docs/architecture/parte-d1-catalogo-planos-torcedor.md`.
* **D.2 Detalhe / simulação de plano (torcedor):** `GET /api/plans/{id}` (JWT; sem exigir sócio; só planos publicados e ativos, senão 404); `GetPlanDetailsQuery` + `GetPublishedActiveByIdAsync`; SPA `/plans/:planId` (`PlanDetailsPage`: mesmo wrapper `.plans-root` do catálogo, `subpage-header` com voltar → `/plans`, título **Planos**, engrenagem → `/account`; card `.plan-detail__card` com preço no mesmo padrão do catálogo; badge **Mais Popular** quando `location.state.featured` (definido em **Mais detalhes** do primeiro card em `/plans`); benefícios como linhas de texto centralizadas com divisórias; CTA **Assinar agora** (`subscriptionsService.subscribe` com **Card**) redireciona o browser para `card.checkoutUrl` da resposta de `POST /api/subscriptions`; nota de rodapé sobre checkout externo; `TorcedorBottomNav`) e `plansService.getById()`; ver `docs/architecture/parte-d2-simulacao-detalhe-plano-torcedor.md`.
* **D.3 Contratação — command (torcedor, backend):** `SubscribeMemberCommand` + `SubscribeMemberCommandHandler`; `ITorcedorMembershipSubscriptionPort` + `TorcedorMembershipSubscriptionService`; status inicial **`PendingPayment`**; histórico `MembershipHistoryEventTypes.Subscribed`; evento MediatR `MemberSubscribedEvent`; testes em Application + Infrastructure; ver `docs/architecture/parte-d3-contratacao-backend.md` (endpoint HTTP em D.4).
* **D.4 Checkout e pagamento (torcedor):** `POST /api/subscriptions` (JWT) + `POST /api/subscriptions/payments/callback` (segredo `Payments:WebhookSecret`, legacy) + `POST /api/webhooks/stripe` (Stripe assinado, `Payments:Stripe:WebhookSecret`); `CreateTorcedorSubscriptionCheckoutCommand` / `ConfirmTorcedorSubscriptionPaymentCommand` + `ITorcedorSubscriptionCheckoutPort` + `TorcedorSubscriptionCheckoutService`; `IPaymentProvider` **Mock** ou **Stripe** (`Payments:Provider`); Stripe usa Checkout Session (cartão); PIX com provedor Stripe retorna `payment_method_not_supported`; SPA `/plans/:planId/checkout` e `subscriptionsService.subscribe()`; ver `docs/architecture/parte-d4-integracao-pagamento-contratacao.md`.
* **D.5 Pós-contratação (torcedor):** `GetMySubscriptionSummaryQuery` + `GET /api/account/subscription` (JWT); `ITorcedorSubscriptionSummaryPort` + `TorcedorSubscriptionSummaryReadService` (membership, plano, último pagamento, carteirinha via `IDigitalCardTorcedorPort`); SPA `/subscription/confirmation` (redirect pós-checkout), `subscriptionsService.getMySummary()`, resumo em `/account`; ver `docs/architecture/parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md`.
* **D.6 Troca de plano (torcedor):** `ChangePlanCommand` + `PUT /api/account/subscription/plan` (JWT); `ITorcedorPlanChangePort` + `TorcedorPlanChangeService` (proporcional, cancela cobranças abertas, nova cobrança Pix/cartão); callback D.4 reconhece proporcional em membership **Ativa**; histórico `PlanChanged`; SPA `/account` seção "Trocar plano" com `plansService.listPublished()` e `subscriptionsService.changePlan()`; ver `docs/architecture/parte-d6-troca-plano-torcedor.md`.
* **D.7 Cancelamento (torcedor):** `CancelMembershipCommand` + `DELETE /api/account/subscription` (JWT); `ITorcedorMembershipCancellationPort` + `TorcedorMembershipCancellationService` (política `Membership.CancellationCoolingOffDays` em `AppConfigurationEntries`, `IPaymentProvider.CancelAsync`, histórico `CancelledByMember`; fora do prazo de arrependimento mantém `Ativo` até `NextDueDate` com `EndDate` e sweep `IMembershipScheduledCancellationEffectiveSweep` no `PaymentDelinquencySweep`); SPA `/account` com `subscriptionsService.cancelMembership()`; ver `docs/architecture/parte-d7-cancelamento-torcedor.md`.
* **C.6 Suporte (torcedor):** `GET/POST /api/support/tickets`, `GET /api/support/tickets/{id}`, `POST .../reply`, `.../cancel`, `.../reopen`, `GET .../attachments/{attachmentId}` (JWT; sem exigir sócio); anexos em `SupportTicketMessageAttachments` + `ISupportTicketAttachmentStorage` com provider `Local` ou `Cloudinary`; em Cloudinary, imagem usa resource `image` e PDF usa `raw`; download segue privado via stream autenticado da API (torcedor/staff); SPA `/support` e `/support/:ticketId` com UI mobile-first alinhada à identidade (`.support-root` / `.support-detail-root` em `AppShell.css`, `TorcedorBottomNav`, estado vazio + CTA **Novo chamado**, formulário colapsável via ícone de configurações no header); ver `docs/architecture/parte-c6-suporte-torcedor.md`.
* **C.1 Perfil (torcedor):** `PUT /api/account/profile` exige **CPF único e válido** em `Document` (normalizado 11 dígitos, `CpfNumber` + índice único filtrado); `POST /api/account/profile/photo` com `IProfilePhotoStorage` por provider (`Local` ou `Cloudinary`), atualização de `PhotoUrl` e cleanup best effort via `ShouldDeletePreviousAfterReplace` (no Cloudinary com `public_id` estável e overwrite, não apaga o asset recém carregado quando a URL “antiga” e a nova partilham o mesmo `public_id`); frontend mantém compatibilidade entre URL relativa local (`/uploads/...`) e URL absoluta Cloudinary; ver `docs/architecture/parte-c1-cadastro-perfil-torcedor.md`.
* **C.2 Notícias e benefícios (torcedor):** `GET /api/news`, `GET /api/news/{id}`, `GET /api/benefits/eligible`, `GET /api/benefits/offers/{offerId}` (respostas incluem `bannerUrl` quando houver), `POST /api/benefits/offers/{offerId}/redeem` (JWT); `ITorcedorNewsReadPort`, `ITorcedorBenefitsReadPort`, `ITorcedorBenefitRedemptionPort`; CQRS em `Modules/Torcedor`; SPA `/news`, `/news/:newsId`, `/benefits`, `/benefits/:offerId`, carrossel de benefícios na home (`DashboardPage`, layout `bannerUrl`: proporção `300:148` + texto reduzido); ver `docs/architecture/parte-c2-noticias-beneficios-torcedor.md`.
* **Identidade visual torcedor (frontend):** redesign mobile-first aplicado em `DashboardPage` (`.dash-*`, carrossel `.dash-benefits-carousel` / `.dash-benefit-banner`), `NewsFeedPage` e `NewsDetailPage` (`.news-*`), `GamesPage` — título **Partidas**, cabeçalho `.subpage-header--tri` (conta em `/account`), cartões FanSpot/Figma (`.games-day__match*`, `.game-card-ev*` + `.game-card-ev__cta-footer` dentro do card, toast fixo `.games-page__ingresso-toast` via `torcedorIngressoToast.ts` após resgate em `MyTicketsPage`; sigla da casa `VITE_CLUB_SHORT_NAME`, padrão `FFC`), `MyTicketsPage` (`.tickets-root`), `SupportTicketsPage` / `SupportTicketDetailPage` (`.support-root`, `.support-detail-root`), `DigitalCardPage` (`.digital-card-root`), `LoyaltyPage` (`.loyalty-root`), `BenefitsEligiblePage` (`.benefits-root`, ofertas em `.benefit-offer-card`), `BenefitOfferDetailPage` (`.benefit-detail-root`), `AccountPage` (`.account-root` + `.account-page__*`), `PlansPage` (`.plans-root` + `.plans-page__*`); **escudo do clube** via `TeamShieldLogo` + `GET /api/branding` em login, cadastro, header do dashboard e sidebar admin (placeholder SVG se sem `Brand.TeamShieldUrl`); documento de referência em `docs/architecture/visual-identity.md`; regra de viewport-escape via `#root:has(.{root-class})` em `index.css`; bottom nav fixa (`.dash-bottom-nav`) compartilhada via `frontend/src/shared/torcedorBottomNav.tsx` (5 itens: Início, Notícias, Jogos, Carteirinha, Conta; **Benefícios** e atalho **Fidelidade** em `/account` via “Meus benefícios” e “Fidelidade”), visível em todas as larguras de viewport; cabeçalho de subpágina compartilhado (`.subpage-header`, `.subpage-header__back`, `.subpage-header__title`, `.subpage-header__badge`) e área de conteúdo (`.subpage-content`); cartões de partida em `/games` (`.game-card-ev`), ingressos (`.ticket-card`), exibição de carteirinha (`.digital-card-display`) e ranking de fidelidade (`.loyalty-ranking`, `.loyalty-summary`) definidos em `AppShell.css`; gradientes CSS como substitutos de imagem nas notícias (sem `imageUrl` na API); layout magazine mobile → hero + `.news-featured-secondary` (flex column, `gap: 0.75rem`) + grid 2col; layout desktop ≥640px → grid `2fr 1fr` com `margin-top: 0` no secondary + grid 3col; estilos em `frontend/src/pages/AppShell.css`. Tokens `--admin-*` em `frontend/src/index.css` e cores torcedor/admin em `AppShell.css`, `AdminLayout.css`, `KpiCard.css` e `AdminDashboardPage.css` seguem a paleta do documento (base `#0e131a` / `#080808`, acento `#8cd392`, texto `#f5f7fa` / `#a6b0bf`); skeletons de notícias usam `@keyframes shimmer` definido em `AppShell.css`.
* **Estilização de cadastro (`/register`):** `RegisterPage.tsx` reescrito sem inline styles; novo arquivo `RegisterPage.css` espelhando exatamente o visual de `LoginPage.css` — grid 2 colunas (painel + hero), mesma tipografia/cores/espaçamentos, inputs com foco verde (`#0f6f48`), botão submit com gradiente verde idêntico ao login, seção LGPD com `register-consents`, responsivo (colapsa 1 coluna em ≤900px, hero oculto em ≤768px); logo centralizada (`align-self: center`) em ambas as páginas de login e cadastro. **Celular** obrigatório no cadastro (validação no submit; backend exige `phoneNumber` não vazio). **Requisitos de senha:** checklist em tempo real (8+ caracteres, maiúscula, minúscula, dígito), regras em `frontend/src/features/account/registerPasswordRules.ts` alinhadas ao Identity; submit desabilitado até cumprir regras; erros `400` de `POST /api/account/register` exibem `errors[]` via `formatRegisterApiErrorMessage` em `accountApi.ts`.
* **Conta torcedor (`/account`):** layout FanSpot/Figma *perfil* (`54:387`): `.account-page__figma-top` com **avatar 50px** (foto/iniciais + câmera), nome e engrenagem (configurações); cartão `.account-page__figma-member-card` com borda `#8cd392`, rótulo **Sócio Torcedor**, nome do plano em verde, lista de fatos em `#6c7278`, bloco de iniciais `.account-page__figma-initials-block`, CTA **Expandir Carteirinha** `.account-page__figma-expand`; atalhos **Meus benefícios** / **Fidelidade** `.account-page__benefits-btn--figma`; painel de assinatura/perfil ao abrir configurações; estilos em `AppShell.css`.