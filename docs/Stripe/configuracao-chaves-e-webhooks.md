# Stripe — Configuração de chaves e webhooks (thin events)

Este documento descreve as variáveis de ambiente / chaves de configuração relacionadas à Stripe no backend (`SocioTorcedor.Api`), o propósito de cada uma e **quais eventos** devem ser assinados em cada **Event Destination** no Dashboard da Stripe.

A API usa **Stripe.net 51** e expõe webhooks **somente no formato thin** (`object: v2.core.event`). É necessário criar **Event Destinations** com a opção **Use thin events** (thin events). O formato snapshot clássico (`ConstructEvent` / `object: event`) **não** é aceito nessas rotas.

**Pré-requisito:** thin events para recursos V1 podem estar em *private preview* na Stripe; consulte a [documentação oficial](https://stripe.com/docs) e o processo de acesso da Stripe, se aplicável.

---

## Visão geral: duas rotas, quatro variáveis `whsec_`

Existem **duas URLs** de webhook na API; no `.env` aparecem **quatro** chaves `whsec_` porque há par **thin + fallback** para cada rota.

| Rota na API | Variáveis `.env` (ordem de uso pelo código) |
|-------------|--------------------------------------------|
| `POST /api/webhooks/stripe/saas` | 1) `Payments__StripeThinSaasWebhookSecret` — se **não** vazio, é este o secret usado na validação. 2) `Payments__StripeSaasWebhookSecret` — usado **só se** o Thin SaaS estiver vazio. |
| `POST /api/webhooks/stripe/connect` | 1) `Payments__StripeThinConnectWebhookSecret` — se **não** vazio, é este o secret usado. 2) `Payments__StripeConnectWebhookSecret` — usado **só se** o Thin Connect estiver vazio. |

**Recomendação:** preencha os dois **Thin** com o signing secret dos Event Destinations **thin** corretos. Os dois **sem “Thin” no nome** podem ficar vazios ou espelhar o mesmo destino como backup; se **Thin** e **não-Thin** estiverem os dois preenchidos, o backend **sempre prefere o Thin** para validar a assinatura daquela rota.

Substitua o host pela URL pública da sua API (ex.: `https://api.seudominio.com`).

---

## As quatro chaves de webhook no `.env` (cada uma com seus eventos)

Todas as rotas exigem **thin events** no Dashboard. Os tipos abaixo usam o prefixo `v1.` como a Stripe mostra ao assinar eventos thin.

### 1. `Payments__StripeThinSaasWebhookSecret`

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

---

### 2. `Payments__StripeSaasWebhookSecret`

| | |
|---|---|
| **appsettings** | `Payments:StripeSaasWebhookSecret` |
| **URL do Event Destination** | **A mesma** que o item 1: `https://<sua-api>/api/webhooks/stripe/saas` (um único destino thin por ambiente). |
| **Conta Stripe** | Plataforma (mesma do item 1). |
| **Para que serve** | Fallback: usado para validar a rota **saas** **apenas quando** `StripeThinSaasWebhookSecret` está vazio. Nome legado (snapshot V1); o endpoint atual continua esperando payload **thin**. |

**Eventos que esse destino deve incluir** (lista completa — **idêntica** à do item 1):

1. `v1.invoice.paid`
2. `v1.invoice.payment_failed`
3. `v1.customer.subscription.updated`
4. `v1.customer.subscription.deleted`

---

### 3. `Payments__StripeThinConnectWebhookSecret`

| | |
|---|---|
| **appsettings** | `Payments:StripeThinConnectWebhookSecret` |
| **URL do Event Destination** | `https://<sua-api>/api/webhooks/stripe/connect` |
| **Conta Stripe** | Connect (eventos no contexto de contas conectadas). |
| **Para que serve** | Signing secret do destino **thin** da rota **connect**; variável **preferida** para essa rota. |

**Eventos que esse destino deve incluir:**

1. `v1.account.updated`
2. `v1.checkout.session.completed`
3. `v1.customer.subscription.updated`
4. `v1.customer.subscription.deleted`
5. `v1.invoice.paid`
6. `v1.invoice.payment_failed`

---

### 4. `Payments__StripeConnectWebhookSecret`

| | |
|---|---|
| **appsettings** | `Payments:StripeConnectWebhookSecret` |
| **URL do Event Destination** | **A mesma** que o item 3: `https://<sua-api>/api/webhooks/stripe/connect`. |
| **Conta Stripe** | Connect (mesma do item 3). |
| **Para que serve** | Fallback: usado para validar a rota **connect** **apenas quando** `StripeThinConnectWebhookSecret` está vazio. |

**Eventos que esse destino deve incluir** (lista completa — **idêntica** à do item 3):

1. `v1.account.updated`
2. `v1.checkout.session.completed`
3. `v1.customer.subscription.updated`
4. `v1.customer.subscription.deleted`
5. `v1.invoice.paid`
6. `v1.invoice.payment_failed`

---

**Normalização no backend:** os tipos `v1.*` são tratados como `invoice.paid`, `account.updated`, etc. (sem o prefixo `v1.`).

---

## Chaves da API e uso geral

### `Payments__StripeSecretKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripeSecretKey` |
| **Formato** | `sk_test_...` ou `sk_live_...` |
| **Para que serve** | Chave secreta da API Stripe usada pelo servidor (checkout, Connect, busca de recursos após notificação thin, etc.). |
| **Onde obter** | Dashboard Stripe → Developers → API keys → Secret key. |
| **Eventos no Dashboard** | Não se aplica (não é webhook). |

### `Payments__StripePublishableKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripePublishableKey` |
| **Formato** | `pk_test_...` ou `pk_live_...` |
| **Para que serve** | Chave publicável para o frontend (Stripe.js / Elements), quando necessário. |
| **Onde obter** | Dashboard Stripe → Developers → API keys → Publishable key. |
| **Eventos no Dashboard** | Não se aplica. |

### `Payments__StripeEnvironment`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripeEnvironment` |
| **Valores típicos** | `test` ou `live` |
| **Para que serve** | Informativo no sistema (ambiente Stripe em uso). |
| **Eventos no Dashboard** | Não se aplica. |

### `Payments__PublicAppBaseUrl`

| | |
|---|---|
| **Nome em appsettings** | `Payments:PublicAppBaseUrl` |
| **Para que serve** | URL base pública (API ou SPA) para montar URLs de retorno de Checkout, Connect, etc. |
| **Eventos no Dashboard** | Não se aplica. |

---

## Comportamento de validação paralela (shadow mode)

### `Payments__StripeWebhookShadowMode`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripeWebhookShadowMode` |
| **Valores** | `true` ou `false` |
| **Para que serve** | Quando `true`, após validar a assinatura e montar o fluxo thin, os **handlers não gravam inbox nem aplicam efeitos de domínio** (útil para testar em sandbox sem marcar eventos como processados ou alterar dados). **Não** é um secret da Stripe. |
| **Eventos no Dashboard** | Não se aplica. |

---

## Webhook do módulo de pagamentos (stub / tenant), não Stripe

### `Payments__MemberWebhookSecret`

| | |
|---|---|
| **Nome em appsettings** | `Payments:MemberWebhookSecret` |
| **Para que serve** | Segredo do webhook **interno** do gateway stub/tenant (`X-Payments-Webhook-Secret`), **não** relacionado aos Event Destinations da Stripe. |
| **Eventos Stripe** | Não se aplica. |

---

## Migração do gateway stub para Stripe (IDs de assinatura legados)

Em ambientes de desenvolvimento ou MVP, o **`StubPaymentProvider`** pode ter gravado `ExternalSubscriptionId` como `mem_sub_*` ou `saas_sub_*` (não são IDs da API Stripe). Ao passar a usar **`Payments:StripeSecretKey`** real, a troca de plano do sócio chama cancelamento da assinatura anterior antes de criar a nova.

O **`StripePaymentProvider.CancelAsync`** trata isso assim:

- IDs **vazios** ou que **não** começam com `sub_` são **ignorados** (não há chamada `SubscriptionService.CancelAsync` na Stripe).
- IDs `sub_...` seguem o fluxo normal de cancelamento; se a Stripe responder com **`resource_missing`** ou mensagem *No such subscription*, o cancelamento é tratado como **idempotente** (sucesso lógico), para não bloquear troca de plano quando o registro local está desatualizado.

Detalhes de implementação e lista de testes: `backend/src/Modules/Payments/AGENTS.md` (seção **Contrato de gateway** e **Testes**).

---

## Exemplo de `.env` (Host)

Arquivo de referência: `backend/src/Host/SocioTorcedor.Api/.env.example`.

No ASP.NET Core, chaves aninhadas usam `__` no nome da variável de ambiente (ex.: `Payments__StripeThinSaasWebhookSecret`).

---

## Checklist rápido no Stripe Dashboard

1. **SaaS (uma URL):** Event Destination → thin events ON → `.../api/webhooks/stripe/saas` → assinar os **4** eventos da seção **1** (e **2**) acima → `whsec_` → preferencialmente `Payments__StripeThinSaasWebhookSecret` (e deixar `Payments__StripeSaasWebhookSecret` vazio ou igual, conforme sua política).
2. **Connect (uma URL):** Event Destination → thin events ON → `.../api/webhooks/stripe/connect` → assinar os **6** eventos da seção **3** (e **4**) acima → `whsec_` → preferencialmente `Payments__StripeThinConnectWebhookSecret`.
3. Garantir `Payments__StripeSecretKey` no mesmo modo (test/live) dos destinos.
4. Em produção, `Payments__StripeWebhookShadowMode=false` após validação.

---

## Referências no repositório

- Opções: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Infrastructure/Options/PaymentsOptions.cs`
- Comportamento shadow: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Application/Options/StripeWebhookHandlingOptions.cs`
- Rotas: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/StripeWebhooksController.cs`
- Documentação do módulo: `backend/src/Modules/Payments/AGENTS.md`
