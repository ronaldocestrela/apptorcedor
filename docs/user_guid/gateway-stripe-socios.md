# Guia do usuário: gateway de pagamentos e Stripe (sócios)

Este documento explica, em linguagem de produto, como o **clube** passa a cobrar **sócios** com **Stripe em modo direto** (conta Stripe do próprio clube), e como isso se relaciona com o **faturamento SaaS** (clube ↔ plataforma).

> **Histórico:** versões anteriores do sistema usavam **Stripe Connect Express** por tenant. O modelo atual usa **API keys da conta Stripe do clube** guardadas de forma segura no banco master, sem onboarding Connect na plataforma.

---

## Dois mundos de cobrança

| Contexto | Quem paga | Conta Stripe | Onde configura |
|----------|-----------|----------------|----------------|
| **SaaS** | O clube paga a **plataforma** | Conta **da plataforma** | Backoffice da plataforma (`X-Api-Key`), planos SaaS, billing |
| **Sócios** | O sócio paga o **clube** | Conta **do clube** | Operador escolhe o provedor no backoffice; admin do clube informa chaves na área **Gateway** (`/admin/stripe`) |

São fluxos **independentes**: cartões salvos para o SaaS ficam na conta da plataforma; mensalidades de sócio usam a conta Stripe configurada para o gateway do tenant.

---

## Papéis

| Papel | O que faz |
|--------|-----------|
| **Operador da plataforma (backoffice)** | Define no detalhe do tenant qual **provedor de gateway** os sócios usam (ex.: `StripeDirect`). |
| **Administrador do clube** | Em **`/admin/stripe`**, informa **secret key**, **publishable key** e **webhook signing secret** da conta Stripe do clube (quando o provedor já é Stripe direto). |
| **Sócio** | Assina plano e paga quando o gateway está configurado; rotas do tenant (`X-Tenant-Id` + JWT). |

---

## Webhooks (thin events)

- **SaaS:** `POST /api/webhooks/stripe/saas` — segredos globais em `Payments:*` (ver [`docs/Stripe/configuracao-chaves-e-webhooks.md`](../Stripe/configuracao-chaves-e-webhooks.md)).
- **Sócios:** `POST /api/webhooks/stripe/member/<tenantId>` — o **mesmo** `tenantId` (GUID) do cadastro do clube no master; o **signing secret** é o configurado pelo admin do clube (não uma variável `Payments:*` única para todos os clubes).

---

## Referência técnica (trecho)

- Backoffice gateway: `api/backoffice/payments/member-gateway/tenants/{tenantId}/status`, `.../provider`
- Admin do clube: `api/payments/admin/member-gateway`, `.../stripe-direct`
- Webhook: `StripeWebhooksController` — rotas `saas` e `member/{tenantId}`

Guia passo a passo para o admin do clube: **[`admin-clube-gateway-stripe.md`](admin-clube-gateway-stripe.md)** (gateway Stripe direto).
