# Guia do usuário: pagamento SaaS (SaaS Payment)

Este documento explica o **faturamento SaaS**: quando o **clube** paga a **plataforma** pela assinatura do software (planos SaaS), como isso aparece na API e o que precisa estar configurado.

> **Nome no código / API:** rotas em `api/backoffice/payments/saas` e assinatura `TenantBillingSubscription` no banco **master**. “SaaS Payment” aqui significa esse contexto (B2B plataforma ↔ clube), não o pagamento do sócio ao clube.

---

## O que é

O **SaaS billing** é a cobrança recorrente do **plano comercial** que o clube contratou com a plataforma (ex.: mensal ou anual). É independente da cobrança de **mensalidades de sócio**, que usa outro fluxo (gateway configurado por tenant — ex.: Stripe direto — e rotas do tenant).

Fluxo resumido:

1. No backoffice existe um **plano SaaS** (`SaaSPlan`) com preço e, se usar Stripe, **Price IDs** Stripe (mensal e/ou anual).
2. O clube recebe uma **atribuição de plano** (tenant vinculado a um `SaaSPlan` e ciclo de faturamento).
3. Alguém com acesso ao **backoffice** dispara o **início da assinatura** de faturamento; o sistema cria assinatura e fatura no master e, com Stripe ativo, integra com a **conta da plataforma** na Stripe.
4. **Webhooks** na conta da plataforma atualizam status de assinatura e faturas (pagamento, falha, mudança de período).

---

## Quem usa

| Papel | Uso típico |
|--------|------------|
| **Operação / backoffice da plataforma** | `X-Api-Key`: iniciar billing, consultar assinatura e faturas, abrir portal de cobrança Stripe. |
| **Clube** | Não chama essas rotas diretamente no MVP típico; o operador da plataforma gerencia o ciclo. |

---

## Pré-requisitos para iniciar cobrança

1. **Plano ativo do tenant**: o clube deve ter vínculo ativo com um plano SaaS (atribuição de plano no módulo Backoffice).
2. **Valores válidos**: preço do plano maior que zero no ciclo escolhido.
3. **Sem assinatura ativa duplicada**: não pode já existir assinatura de billing ativa para o mesmo tenant.
4. **Stripe**: com `Payments:StripeSecretKey` configurado, o fluxo de billing SaaS usa **Price IDs** do `SaaSPlan` (`StripePriceMonthlyId` / `StripePriceYearlyId`) ao criar a assinatura na conta da plataforma. Geração de chaves e permissões (incl. chave restrita): [`docs/Stripe/configuracao-chaves-e-webhooks.md`](../Stripe/configuracao-chaves-e-webhooks.md).

---

## API de backoffice (referência)

Todas exigem **`X-Api-Key`**.

Base path: `api/backoffice/payments/saas`

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `tenants/{tenantId}/billing/start` | Inicia a assinatura SaaS do tenant. Resposta inclui identificador criado (`201 Created`). |
| `GET` | `tenants/{tenantId}/subscription` | Consulta assinatura de billing do tenant (valores, período, Stripe, etc., conforme DTO da API). |
| `GET` | `tenants/{tenantId}/invoices` | Lista faturas com paginação (`page`, `pageSize`). |
| `POST` | `tenants/{tenantId}/billing/portal` | Cria sessão do **Billing Portal** Stripe para o cliente gerenciar método de pagamento e assinatura. Corpo: `returnUrl`. |

---

## Webhooks Stripe (conta da plataforma)

Eventos de assinatura e fatura da **conta da plataforma** na Stripe devem ser enviados para:

`POST /api/webhooks/stripe/saas`

Configuração (segredos e lista de eventos thin): **`docs/Stripe/configuracao-chaves-e-webhooks.md`**.

Esses eventos mantêm alinhados o estado da assinatura SaaS e das faturas abertas no master (ex.: pagamento confirmado, falha, atualização de assinatura).

---

## Relação com a interface web

No SPA, a área **Faturamento SaaS** / admin costuma **orientar** o uso do backoffice e do portal Stripe, sem substituir a API. Rotas e textos podem variar; a fonte de verdade para automação é a API acima.

---

## Onde aprofundar no código

- Controller: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/BackofficeSaasPaymentsController.cs`
- Início de billing: `StartTenantSaasBillingHandler` (plano ativo, Stripe price, assinatura, primeira fatura)
- Módulo: `backend/src/Modules/Payments/AGENTS.md`
