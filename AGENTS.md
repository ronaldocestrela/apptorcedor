# 📄 AGENTS.md — SaaS Sócio Torcedor (MVP)

## 🧠 Visão Geral

Este projeto é um SaaS multitenant para gestão de **sócio torcedor de clubes esportivos**.

Cada clube é um tenant isolado com:

* banco de dados próprio (SQL Server)
* configuração própria
* usuários próprios

O sistema será dividido em:

* Backend: `.NET 10`, `ASP.NET Core`, `Identity`, `CQRS`, `Modular Monolith`
* Frontend Web: `React + Vite + Axios` (SPA)
* Mobile: `Flutter`

---

## 🏗️ Arquitetura Geral

### Estilo Arquitetural

* Modular Monolith
* CQRS (Commands + Queries)
* Clean Architecture por módulo
* Separação clara entre:

  * Domain
  * Application
  * Infrastructure
  * API

---

## 🧩 Multitenancy

### Modelo

* 1 tenant = 1 clube
* 1 banco por tenant
* banco central (master) para gestão do SaaS

### Identificação do Tenant

**Header HTTP `X-Tenant-Id`** (slug do tenant enviado pelo frontend / cliente)

#### Exemplo:

```http
X-Tenant-Id: flamengo
```

### Fluxo de resolução

1. Request chega com o header `X-Tenant-Id` (valor = slug, ex.: `flamengo`)
2. Buscar tenant no banco master pelo slug
3. Resolver:

   * TenantId
   * ConnectionString
   * Configurações
   * AllowedOrigins (CORS)

### API administrativa (Backoffice)

* Rotas **`api/backoffice/*`**: não exigem **`X-Tenant-Id`** (operação central no banco master).
* Autenticação: header **`X-Api-Key`**, conferido com a configuração **`Backoffice:ApiKey`**.
* **Swagger / OpenAPI**: nas rotas de tenant, o parâmetro **`X-Tenant-Id`** continua documentado; no backoffice, o esquema **`BackofficeApiKey`** documenta **`X-Api-Key`** (use **Authorize** na UI do Swagger para essas operações).

---

## 🌐 CORS Dinâmico por Tenant

Cada tenant terá seus próprios domínios autorizados.

### Exemplo (banco master)

```json
{
  "tenant": "flamengo",
  "allowedOrigins": [
    "https://flamengo.meusistema.com",
    "https://app.flamengo.com"
  ]
}
```

### Middleware de CORS dinâmico

* Resolver tenant antes do pipeline
* Aplicar CORS baseado no tenant atual

### Estratégia

```csharp
app.Use(async (context, next) =>
{
    var tenant = context.Items["Tenant"];

    var origins = tenant.AllowedOrigins;

    context.Response.Headers["Access-Control-Allow-Origin"] = origins;
    
    await next();
});
```

### Observação importante

* NÃO usar CORS fixo no `Program.cs`
* CORS deve ser resolvido por request
* Permitir atualização sem rebuild

---

## 🗄️ Banco de Dados

### Banco Master

Responsável por:

* Tenants
* Configuração global
* Planos do SaaS
* Feature flags

#### Tabelas principais:

* Tenants
* TenantSettings
* TenantDomains
* TenantPlans
* SaaSPlans
* Permissions

---

### Banco por Tenant

Cada clube possui seu próprio banco com:

#### Módulos:

* Users / Identity
* MemberProfiles
* Plans
* Subscriptions
* Payments
* Games
* Tickets
* News
* Loyalty
* Benefits
* SupportTickets

---

## 🔐 Autenticação e Autorização

### Autenticação

* **Rotas de sócio / tenant:** ASP.NET Identity + JWT (Bearer).
* **Rotas `api/backoffice/*`:** chave de API **`X-Api-Key`** (config **`Backoffice:ApiKey`**), sem JWT.
* Email + senha
* Login social:

  * Google
  * Facebook
  * Apple

### Autorização

* Role + Permission Claims

#### Exemplo de permissões:

```text
Socios.Criar
Socios.Editar
Socios.Visualizar
Pagamentos.Estornar
Planos.Gerenciar
Noticias.Publicar
Chamados.Responder
```

---

## 📦 Estrutura do Backend

```text
src/

  BuildingBlocks/
    Domain/
    Application/
    Infrastructure/
    Shared/

  Modules/

    Backoffice/
    Identity/
    Tenancy/
    Membership/
    Plans/
    Payments/
    Games/
    Tickets/
    News/
    Loyalty/
    Benefits/
    Support/

  Host/
    Api/
```

---

## 🧱 Estrutura de um Módulo

```text
ModuleName/

  Domain/
    Entities/
    Enums/
    ValueObjects/
    Rules/
    Events/

  Application/
    Commands/
    Queries/
    Handlers/
    DTOs/
    Validators/

  Infrastructure/
    Persistence/
    Repositories/
    Services/
    Integrations/

  Api/
    Controllers/
```

---

## ⚙️ CQRS

### Commands (Write)

* CreateMember
* UpdateMember
* ChangePlan
* CancelSubscription
* RegisterPayment
* CreateGame
* PublishNews
* OpenTicket

### Queries (Read)

* GetMemberProfile
* GetMembership
* ListPlans
* ListGames
* ListTickets
* GetNews
* GetRanking

---

## 👤 Domínio: Sócio Torcedor

### Regras

* 1 plano ativo por usuário
* pode fazer upgrade/downgrade
* status controlado:

```text
Ativo
Inadimplente
Cancelado
Suspenso
```

---

## 💳 Pagamentos

### Requisitos

* PIX
* Cartão
* Recorrência automática

### Arquitetura

Abstração de provider:

```csharp
public interface IPaymentProvider
{
    Task CreateSubscriptionAsync(...);
    Task CreatePixAsync(...);
    Task CreateCardAsync(...);
    Task CancelAsync(...);
    Task GetStatusAsync(...);
}
```

### Observação

* Implementação futura:

  * Asaas
  * Pagar.me
  * Mercado Pago

---

## 🎟️ Ingressos

### Estratégia

* Sistema NÃO emite ingressos
* Integra com fornecedor externo

### Responsabilidades internas

* validar benefício do sócio
* aplicar desconto
* solicitar ingresso ao provider externo
* armazenar metadados
* exibir QR Code

---

## 📱 Mobile (Flutter)

### Funcionalidades

* login
* cadastro
* assinatura de plano
* carteirinha digital
* resgatar ingresso
* visualizar ingressos (QR Code)
* notícias

---

## 🌐 Frontend Web

### Tecnologias

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

### Áreas

#### Admin

* sócios
* planos
* pagamentos
* jogos
* notícias
* suporte

#### Sócio

* perfil
* plano
* carteirinha
* ingressos
* notícias

---

## 🔔 Notificações

### Canais

* Email
* Push

### Eventos

* novo jogo
* vencimento próximo
* inadimplência

---

## ⚖️ LGPD

* aceite de termos obrigatório
* aceite de política de privacidade
* versionamento de documentos
* registro de data/hora

---

## 🚀 Fases de Implementação

**Legenda:** **✅** = já entregue no estado atual do repositório (backend).

### Fase 1 — Fundação

* ✅ estrutura base
* ✅ tenancy por header `X-Tenant-Id` (slug)
* ✅ CORS dinâmico
* ✅ Identity
* ✅ permissões
* ✅ Swagger
* ✅ Docker

### Fase 2 — Backoffice

* ✅ gestão de tenants
* ✅ planos SaaS

### Fase 3 — Sócio

* ✅ cadastro (perfil `MemberProfile`, `api/members`)
* ✅ planos de sócio (`MemberPlan`, vantagens em JSON, `api/plans`) — Fase 3.2
* status
* LGPD

### Fase 4 — Pagamentos

* recorrência
* PIX / cartão
* inadimplência

### Fase 5 — Jogos / Ingressos

* jogos
* benefícios
* integração externa

### Fase 6 — Comunicação

* email
* push
* eventos

### Fase 7 — Fidelidade / Benefícios

### Fase 8 — Atendimento

### Fase 9 — Apps (Web + Mobile)

---

## 🐳 Infraestrutura

* Docker obrigatório
* VPS inicial
* logs estruturados
* pronto para escalar

---

## ⚠️ Pontos Críticos

* resolução de tenant antes de qualquer acesso ao banco
* gestão de múltiplas connection strings
* migrations por tenant
* isolamento de dados
* integração com gateway de pagamento
* integração com sistema de ingressos

---

## ✅ Conclusão

Este sistema deve ser construído como:

* SaaS multitenant robusto
* altamente desacoplado
* preparado para escalar
* simples o suficiente para MVP
* com base sólida para evolução futura