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

### `Payments__StripeSecretKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripeSecretKey` |
| **Formato** | `sk_test_...` ou `sk_live_...` |
| **Para que serve** | Chave secreta da API Stripe da **plataforma** (SaaS billing, operações que ainda usam a conta global configurada no host). |
| **Onde obter** | Dashboard Stripe (conta da plataforma) → Developers → API keys. |

### `Payments__StripePublishableKey`

| | |
|---|---|
| **Nome em appsettings** | `Payments:StripePublishableKey` |
| **Para que serve** | Chave publicável para o frontend (Stripe.js / Elements) quando o SaaS ou fluxos globais precisam. |
| **Onde obter** | Conta da plataforma na Stripe. |

### `Payments__StripeEnvironment`

Valores típicos: `test` ou `live` (informativo).

### `Payments__PublicAppBaseUrl`

URL base pública (API ou SPA) para montar success/cancel de Checkout, etc.

### Chaves legadas `Payments:StripeConnectWebhookSecret` / `StripeThinConnectWebhookSecret`

Podem permanecer em `appsettings` por compatibilidade, mas **não** são usadas pelo webhook de sócios atual. O endpoint antigo `POST /api/webhooks/stripe/connect` foi substituído por **`/api/webhooks/stripe/member/{tenantId}`** com segredo por tenant.

---

## Comportamento de validação paralela (shadow mode)

### `Payments__StripeWebhookShadowMode`

Quando `true`, após validar a assinatura e montar o fluxo thin, os **handlers não gravam inbox nem aplicam efeitos de domínio** (útil para sandbox).

---

## Webhook do módulo de pagamentos (stub / interno), não Stripe

### `Payments__MemberWebhookSecret`

Segredo do webhook **interno** do gateway stub (`X-Payments-Webhook-Secret`), não relacionado aos Event Destinations da Stripe.

---

## Migração do gateway stub para Stripe (IDs de assinatura legados)

Ver secção homônima no final do documento histórico e `backend/src/Modules/Payments/AGENTS.md`: o `StripePaymentProvider.CancelAsync` ignora IDs que não são `sub_*` e trata `resource_missing` como idempotente.

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
