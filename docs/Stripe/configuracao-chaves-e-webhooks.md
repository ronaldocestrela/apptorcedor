# Stripe — Configuração de chaves e webhooks (thin events)

Este documento descreve as variáveis de ambiente / chaves de configuração relacionadas à Stripe no backend (`SocioTorcedor.Api`), o propósito de cada uma e **quais eventos** devem ser assinados em cada **Event Destination** no Dashboard da Stripe.

A API usa **Stripe.net 51** e expõe webhooks **somente no formato thin** (`object: v2.core.event`). É necessário criar **Event Destinations** com a opção **Use thin events**. O formato snapshot clássico (`ConstructEvent` / `object: event`) **não** é aceito nessas rotas.

**Pré-requisito:** thin events para recursos V1 podem estar em *private preview* na Stripe; consulte a [documentação oficial](https://stripe.com/docs) e o processo de acesso da Stripe, se aplicável.

---

## Visão geral: rotas na API da plataforma

| Rota na API | Conta Stripe | Onde fica o signing secret (`whsec_`) |
|-------------|--------------|----------------------------------------|
| `POST /api/webhooks/stripe/saas` | **Plataforma** (Billing SaaS) | Variáveis globais `Payments:StripeThinSaasWebhookSecret` ou fallback `Payments:StripeSaasWebhookSecret` |
| `POST /api/webhooks/stripe/member/{tenantId}` | **Clube** (cobrança de sócios, um destino por tenant) | Credenciais do gateway guardadas no master (**não** são variáveis `Payments:*` compartilhadas) |

Substitua o host pela URL pública da sua API (ex.: `https://api.seudominio.com`). O `{tenantId}` é o **GUID** do tenant no banco master.

---

## 1. Webhook SaaS (conta da plataforma)

### `Payments__StripeThinSaasWebhookSecret` (preferido)

| | |
|---|---|
| **appsettings** | `Payments:StripeThinSaasWebhookSecret` |
| **URL do Event Destination** | `https://<sua-api>/api/webhooks/stripe/saas` |
| **Conta Stripe** | Plataforma (Billing SaaS dos clubes). |
| **Para que serve** | Signing secret (`whsec_...`) do destino **thin** da rota **saas**; é a variável **preferida** para validar `Stripe-Signature` nessa rota. |

**Eventos que esse destino deve incluir:**

1. `v1.invoice.paid`
2. `v1.invoice.payment_failed`
3. `v1.customer.subscription.updated`
4. `v1.customer.subscription.deleted`

### `Payments__StripeSaasWebhookSecret` (fallback)

| | |
|---|---|
| **appsettings** | `Payments:StripeSaasWebhookSecret` |
| **URL** | **A mesma** que o item acima. |
| **Para que serve** | Fallback: usado **apenas quando** `StripeThinSaasWebhookSecret` está vazio. |

**Eventos:** idênticos à lista do item anterior.

---

## 2. Webhook de sócios (conta do clube, por tenant)

Cada clube que use **Stripe direto** cria, na **própria conta Stripe**, um Event Destination apontando para:

`https://<sua-api>/api/webhooks/stripe/member/<tenantId>`

O **signing secret** (`whsec_...`) desse destino é informado pelo **administrador do clube** em `PUT /api/payments/admin/member-gateway/stripe-direct` (campo webhook secret), junto com as chaves `sk_` e `pk_` da conta.

**Eventos:** alinhar aos tipos tratados pelo pipeline de sócios (checkout, fatura, assinatura — ver `MemberStripeWebhookEffectApplicator` e normalização thin no repositório). Em geral incluem, entre outros, eventos equivalentes a checkout completado, assinatura e fatura, conforme a Stripe exibir com prefixo `v1.` no modo thin.

Não há variável de ambiente global única para essa rota: cada tenant pode ter o seu `whsec_` distinto.

---

## Chaves da API e uso geral

### Como gerar as chaves no Stripe Dashboard

Use sempre a **conta Stripe correta** (plataforma para SaaS; conta do **clube** para cobrança de sócios com Stripe direto).

1. Acesse o [Dashboard da Stripe](https://dashboard.stripe.com) e faça login.
2. Vá em **Developers** → **API keys**.
3. **Chaves padrão (recomendado para desenvolvimento / MVP)**  
   - Em **Standard keys**, use **Reveal test key** ou **Reveal live key** para copiar a **Secret key** (`sk_test_...` / `sk_live_...`).  
   - Na mesma página, copie a **Publishable key** (`pk_test_...` / `pk_live_...`) quando o front ou o host precisarem dela.
4. **Chave restrita (recomendado em produção)**  
   - Clique em **Create restricted key** (ou **Create key** com tipo restrito, conforme o layout do painel).  
   - Dê um nome claro (ex.: `SocioTorcedor API host` ou `Clube X backend sócios`).  
   - Defina as **permissões** conforme a tabela abaixo (conta plataforma vs conta do clube).  
   - Após criar, copie a chave — o prefixo costuma ser `rk_test_...` ou `rk_live_...`. Ela substitui a secret `sk_...` nas variáveis onde hoje você coloca a chave secreta (a API Stripe aceita chave restrita no mesmo cabeçalho de autenticação).

Documentação oficial: [API keys](https://docs.stripe.com/keys) e [restricted keys](https://docs.stripe.com/keys#create-api-restricted-key).

### Permissões recomendadas para chaves restritas

Os nomes exatos no painel da Stripe podem mudar (e alguns recursos aparecem como *preview*). Se algo falhar com `403` ou *permission denied*, amplie a permissão correspondente ou use temporariamente uma **secret key padrão** (`sk_...`) para validar o fluxo.

**Se você usar apenas a secret key padrão (`sk_...`), não precisa configurar permissões:** ela já tem escopo completo na conta.

#### Conta da **plataforma** (valor de `Payments:StripeSecretKey` no host)

Esta chave atende Billing SaaS (assinatura do clube na plataforma), portal de cobrança, cartões do tenant na plataforma, PIX quando aplicável, e o processamento de **webhooks thin** que buscam o objeto relacionado (`Invoice`, `Subscription`, `Checkout.Session`, etc.) e o evento em **API v2**.

| Área no Dashboard (referência) | Acesso sugerido | Uso no projeto |
|--------------------------------|-----------------|----------------|
| **Customers** | Read + Write | Cliente Stripe do tenant (SaaS), portal, métodos de pagamento |
| **Subscriptions** | Read + Write | Assinatura SaaS, cancelamento, status |
| **Products** e **Prices** | Read + Write | Criação de produto/preço quando não há `price_id` fixo |
| **Invoices** | Read | Resolução de webhook thin (`GET` de fatura) |
| **Checkout Sessions** | Read + Write | Fluxos que criam sessão na conta plataforma (se houver) |
| **Billing Portal** (sessões do portal) | Write | `CreateBillingPortalSession` |
| **Payment Intents** | Write | Checkout PIX (`CreatePixAsync`) |
| **Setup Intents** | Write | Adicionar cartão (SaaS) |
| **Payment Methods** | Read + Write | Listar / anexar / destacar cartões do customer SaaS |
| **Events / API v2 / Core** | Read | Buscar notificação thin (`V2.Core.Events.Get`) e processar o payload |

Se o painel oferecer permissão explícita para **V2 Core Events** ou **Event notifications**, inclua **leitura** para o pipeline de webhooks thin.

#### Conta do **clube** (secret `sk_` ou restrita `rk_` guardada em `PUT /api/payments/admin/member-gateway/stripe-direct`)

Mesma conta em que o admin do clube cria o Event Destination `.../api/webhooks/stripe/member/<tenantId>`.

| Área no Dashboard (referência) | Acesso sugerido | Uso no projeto |
|--------------------------------|-----------------|----------------|
| **Customers** | Write | Criação de customer antes do Checkout (modo assinatura / Accounts V2) |
| **Checkout Sessions** | Read + Write | Contratação de plano do sócio |
| **Subscriptions** | Read + Write | Assinatura direta (`CreateSubscriptionAsync`), cancelamento, status |
| **Products** e **Prices** | Read + Write | Plano sem `price_id` Stripe fixo |
| **Invoices** | Read | Resolução de webhook thin ligada a faturas |
| **Payment Intents** | Write | PIX para sócios (`CreatePixAsync`) |
| **Events / API v2 / Core** | Read | Igual à plataforma, para thin webhooks na conta do clube |

**Publishable key (`pk_...`):** obtida na mesma página **API keys**; não exige lista de permissões como a secret — use a `pk_` da **mesma conta** (test/live) que a `sk_`/`rk_` correspondente.

### `Payments__StripeSecretKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripeSecretKey` |
| **Formato** | `sk_test_...` ou `sk_live_...` (padrão); ou chave restrita `rk_test_...` / `rk_live_...` |
| **Para que serve** | Chave secreta da API Stripe da **plataforma** (SaaS billing, operações que ainda usam a conta global configurada no host). |
| **Onde obter** | Dashboard Stripe (conta da plataforma) → **Developers** → **API keys** (ver secção *Como gerar as chaves* acima). |

### `Payments__StripePublishableKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripePublishableKey` |
| **Para que serve** | Chave publicável para o frontend (Stripe.js / Elements) quando o SaaS ou fluxos globais precisam. |
| **Onde obter** | Conta da plataforma na Stripe → **Developers** → **API keys** → **Publishable key**. |

### `Payments__StripeEnvironment`

Valores típicos: `test` ou `live` (informativo).

### `Payments__PublicAppBaseUrl`

URL base pública (API ou SPA) para montar success/cancel de Checkout, etc.

---

## Comportamento de validação paralela (shadow mode)

### `Payments__StripeWebhookShadowMode`

Quando `true`, após validar a assinatura e montar o fluxo thin, os **handlers não gravam inbox nem aplicam efeitos de domínio** (útil para sandbox).

---

## Cancelamento de assinatura na API Stripe

O **`StripePaymentProvider.CancelAsync`** só invoca a API de assinaturas da Stripe quando o ID externo começa com `sub_`. Respostas `resource_missing` / *No such subscription* são tratadas como cancelamento idempotente (evita falha em reprocessamentos).

---

## Exemplo de `.env` (Host)

Arquivo de referência: `backend/src/Host/SocioTorcedor.Api/.env.example`.

No ASP.NET Core, chaves aninhadas usam `__` no nome da variável de ambiente (ex.: `Payments__StripeThinSaasWebhookSecret`).

---

## Checklist rápido no Stripe Dashboard

1. **SaaS (conta plataforma):** Event Destination → thin events ON → `.../api/webhooks/stripe/saas` → os **4** eventos da secção SaaS → `whsec_` → `Payments__StripeThinSaasWebhookSecret` (ou fallback).
2. **Sócios (conta de cada clube):** Event Destination → thin events ON → `.../api/webhooks/stripe/member/<tenantId>` → `whsec_` informado pelo admin do clube na UI/API (não obrigatoriamente no `.env` do host).
3. Garantir modo test/live coerente entre chaves e destinos.
4. Em produção, `Payments__StripeWebhookShadowMode=false` após validação.

---

## Referências no repositório

- Opções: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Infrastructure/Options/PaymentsOptions.cs`
- Comportamento shadow: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Application/Options/StripeWebhookHandlingOptions.cs`
- Rotas: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/StripeWebhooksController.cs`
- Documentação do módulo: `backend/src/Modules/Payments/AGENTS.md`
