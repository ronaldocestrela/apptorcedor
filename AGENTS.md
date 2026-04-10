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
* **OpenAPI (Scalar)**: nas rotas de tenant, o parâmetro **`X-Tenant-Id`** continua documentado; no backoffice, o esquema **`BackofficeApiKey`** documenta **`X-Api-Key`**. A referência interativa fica em **`/scalar`** (desenvolvimento ou `EXPOSE_OPENAPI_JSON=true`); use a autenticação da UI (**Bearer** e **BackofficeApiKey**) para testar as operações.

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
* **Novo tenant:** na criação (`CreateTenant`), é registrada automaticamente uma origem permitida a partir de **`CORS_BASE_DOMAIN`** (chave raiz em `appsettings` ou variável de ambiente). Se **ausente ou só espaços**, o fallback é **`http://{slug}.localhost:5173`** (ex.: slug `feira` → `http://feira.localhost:5173`). Se **`CORS_BASE_DOMAIN`** for uma URL completa **sem** o placeholder **`{slug}`**, essa origem é usada igual para todos os novos tenants. Se contiver **`{slug}`**, o placeholder é substituído pelo slug. Se for só **host** (ex. `localhost:5173` ou `app.meuclube.com`), monta-se **`http://{slug}.…`** em ambiente local (host com `localhost`) ou **`https://{slug}.…`** caso contrário. Outras origens continuam via backoffice (`POST /api/backoffice/tenants/{id}/domains`).

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
* status controlado (no backend, enum `MemberStatus`; valores de produto abaixo; existe também `PendingCompletion` para fluxos futuros de cadastro incompleto):

```text
Ativo (Active)
Inadimplente (Delinquent)
Cancelado (Canceled)
Suspenso (Suspended)
```

* alteração de status por **administrador do tenant**: `PATCH /api/members/{id}/status` (JWT + role `Administrador` + header `X-Tenant-Id`); listagem admin com filtro opcional `?status=`

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
    auth/
    router/
    theme/          # useTheme, ThemeToggle; tema em data-theme no <html>
  shared/
    http/
    tenant/
    auth/           # sessão + decodificação de claims do JWT (roles, email)
    payments/
    members/        # GET /api/members/me
  features/
  pages/
```

### UI e tema

* Estilos globais em **`web/src/index.css`**: **CSS Custom Properties** por tema (`:root` claro, `[data-theme="dark"]` escuro), layout responsivo (ex.: menu colapsável no shell abaixo de ~600px).
* **Tema claro/escuro:** preferência persistida em **`localStorage`** (`theme` = `light` | `dark`); script inline em **`web/index.html`** aplica o tema antes do primeiro paint (evita flash); toggle nas páginas autenticadas (**`AppShell`**) e em login/cadastro / **`TenantNotResolvedPage`**.

### Sessão e papéis no SPA

* Além de `accessToken` e `expiresAtUtc`, a sessão em **`sessionStorage`** guarda **`roles`** extraídas do JWT (claim curta **`role`** ou claim longa do .NET); sessões antigas sem `roles` são normalizadas ao carregar, decodificando o token de novo.
* **Navegação:** os links **Admin** e **Faturamento SaaS** só aparecem para usuários com role **`Administrador`**. Usuários apenas **`Socio`** continuam com **Sócio** e **Pagamentos**. Não há bloqueio de rota por papel no front (apenas visibilidade no menu); a API continua a autorizar com JWT + roles.

### Áreas

#### Admin

* sócios
* planos
* pagamentos
* jogos
* notícias
* suporte

#### Sócio

* perfil (**`/member`** — dados de **`GET /api/members/me`**; e-mail da sessão via JWT quando o perfil ainda não existe)
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

* aceite de termos obrigatório e de política de privacidade no **`POST /api/auth/register`** (IDs das versões vigentes do tenant)
* versionamento por tenant: tabelas `LegalDocumentVersions` e `UserLegalConsents` no banco do tenant (módulo Identity)
* registro de data/hora (UTC), IP e user-agent no consentimento
* **Leitura pública (sem JWT)**: `GET /api/legal-documents/current` — retorna as versões atuais de termos e privacidade (exige `X-Tenant-Id`)
* **Publicação (admin do clube)**: `POST /api/legal-documents` — role `Administrador`, corpo com `kind` (`TermsOfUse` / `PrivacyPolicy`) e `content`
* após migrations, tenants sem documentos recebem seed mínimo (placeholder) na subida da API ou na criação do tenant; o clube deve substituir pelo texto oficial
* após migrations do Identity por tenant, o seed **`RoleTenantSeed`** garante as roles **`Socio`** e **`Administrador`** em `AspNetRoles` (idempotente); o primeiro cadastro continua atribuindo **`Socio`**; atribuir **`Administrador`** a um usuário é feito fora do registro público (ex.: operação administrativa ou banco)

---

## 🚀 Fases de Implementação

**Legenda:** **✅** = já entregue no estado atual do repositório (backend).

### Fase 1 — Fundação

* ✅ estrutura base
* ✅ tenancy por header `X-Tenant-Id` (slug)
* ✅ CORS dinâmico
* ✅ Identity
* ✅ permissões
* ✅ OpenAPI + Scalar (referência em `/scalar`; JSON em `/swagger/v1/swagger.json`)
* ✅ Docker

### Fase 2 — Backoffice

* ✅ gestão de tenants
* ✅ planos SaaS

### Fase 3 — Sócio

* ✅ cadastro (perfil `MemberProfile`, `api/members`)
* ✅ planos de sócio (`MemberPlan`, vantagens em JSON, `api/plans`) — Fase 3.2
* ✅ **Fase 3.3** — ciclo de vida do sócio: `MemberStatus` (incl. inadimplente/cancelado/suspenso), regras de transição, `PATCH /api/members/{id}/status`, filtro `GET /api/members?status=`
* ✅ **Fase 3.4** — LGPD no cadastro: documentos versionados, consentimento obrigatório no register, endpoints `GET /api/legal-documents/current` e `POST /api/legal-documents`

### Fase 4 — Pagamentos

* ✅ **MVP backend** — módulo `Payments`: assinatura + faturas SaaS (master) e sócio (tenant); `POST/GET` backoffice em `api/backoffice/payments/saas/*`; `api/payments/member/*` (subscribe, PIX checkout, minha assinatura, faturas); webhooks SaaS (API key) e tenant (`X-Payments-Webhook-Secret`); provider **stub** trocável por gateway real
* ✅ **MVP web** — rotas `/member` (perfil sócio, `GET /api/members/me`), `/member/billing` (fluxo sócio) e `/admin/billing` (orientação SaaS / backoffice); UI responsiva, tema claro/escuro, menu admin visível só para role **`Administrador`**
* recorrência end-to-end com gateway de produção
* cartão (tokenização / 3DS) além do stub
* conciliação e jobs de cobrança

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

* **Web (SPA em `web/`):** UI responsiva, tema claro/escuro, navegação admin condicionada à role **`Administrador`**, área do sócio em **`/member`** com perfil via **`GET /api/members/me`**.

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