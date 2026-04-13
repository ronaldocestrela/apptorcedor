# Guia do administrador do clube: gateway Stripe direto (sócios)

Este guia é para quem administra o **clube** no sistema (role **Administrador**) e precisa **configurar a conta Stripe do clube** para receber pagamentos de sócios (assinaturas, checkout, PIX quando disponível).

Para visão geral e separação SaaS vs sócios, veja **[`gateway-stripe-socios.md`](gateway-stripe-socios.md)**.

**Chaves, webhooks thin e permissões (conta do clube vs plataforma):** [`docs/Stripe/configuracao-chaves-e-webhooks.md`](../Stripe/configuracao-chaves-e-webhooks.md).

---

## O que é e por que configurar

O sistema cobra sócios usando a **conta Stripe do próprio clube** (chaves de API informadas pelo administrador). O **operador da plataforma** deve primeiro atribuir o provedor **Stripe direto** ao tenant no **backoffice**; sem isso, a página de gateway do clube indica que o provedor ainda não foi definido.

O **faturamento SaaS** (clube paga a plataforma pelo software) é outro fluxo e usa a conta Stripe **da plataforma**.

---

## Pré-requisitos

1. Conta de usuário com role **Administrador** no seu clube.
2. Acesso ao aplicativo web do tenant (hostname / `X-Tenant-Id` configurado).
3. Conta **Stripe** do clube (modo test ou live conforme o ambiente).
4. No backoffice da plataforma: o tenant com **provedor** `StripeDirect` (aba **Gateway sócios** no detalhe do tenant).

---

## Permissões da chave Stripe (conta do clube)

Use sempre chaves da **mesma conta Stripe** em que você configurou o webhook de sócios (`pk_`, `sk_`/`rk_`, `whsec_`).

- **Chave secreta padrão (`sk_test_…` / `sk_live_…`):** tem acesso completo à conta — não precisa escolher permissões no painel; é a opção mais simples para testes ou MVP.
- **Chave restrita (`rk_test_…` / `rk_live_…`):** ao criar em **Developers → API keys → Create restricted key**, ative pelo menos as capacidades abaixo (os nomes exatos no Dashboard da Stripe podem variar ligeiramente; se aparecer `403` / *permission denied*, amplie a permissão correspondente).

| Área no Dashboard da Stripe | Acesso necessário | Para que serve no sistema |
|-----------------------------|-------------------|---------------------------|
| **Customers** | Escrita (*Write*) | Criar cliente antes do Checkout em assinatura |
| **Checkout Sessions** | Leitura e escrita (*Read + Write*) | Sessão de pagamento / contratação de plano |
| **Subscriptions** | Leitura e escrita | Assinatura, cancelamento, consulta de estado |
| **Products** e **Prices** | Leitura e escrita | Quando o plano não usa um `price_id` fixo da Stripe |
| **Invoices** | Leitura (*Read*) | Processar webhooks *thin* ligados a faturas |
| **Payment Intents** | Escrita | Pagamentos PIX para sócios |
| **Events / API v2 / Core** (ou equivalente) | Leitura | Buscar detalhes do evento *thin* no webhook |

Detalhes adicionais (geração de chaves, sandbox, webhooks): [`docs/Stripe/configuracao-chaves-e-webhooks.md`](../Stripe/configuracao-chaves-e-webhooks.md).

---

## Passo a passo no aplicativo

1. Faça login como **Administrador**.
2. Abra **Gateway** no menu (rota **`/admin/stripe`**).
3. Confira o **provedor** e o **status** exibidos no topo.
4. Preencha:
   - **Secret key** (`sk_…` ou chave restrita `rk_…`) — obrigatória em todo envio de salvamento (a API exige a chave completa a cada atualização). Permissões da chave restrita: ver a secção **Permissões da chave Stripe** acima; guia técnico completo no link do topo.
   - **Chave publicável** (`pk_…`) e **webhook signing secret** (`whsec_…`) conforme o painel Stripe do clube.
5. Na Stripe (conta do clube), crie um **Event Destination** com **thin events** apontando para a URL pública da API:  
   `https://<sua-api>/api/webhooks/stripe/member/<tenantId>`  
   onde `<tenantId>` é o **GUID** do clube no banco master (o mesmo exibido internamente / em integrações).
6. Use o **signing secret** desse destino no campo **Webhook signing secret** e salve de novo na aplicação.

---

## Como saber se está tudo certo

- Provedor **StripeDirect** e status indicando configuração pronta (`Ready` ou equivalente no texto da API).
- Chave publicável com máscara exibida; webhook secret marcado como configurado.
- Teste de evento (ping) ou fluxo de pagamento de sócio em ambiente de homologação.

---

## Perguntas frequentes

**Preciso da chave de API do backoffice da plataforma (`X-Api-Key`)?**  
Não para operar como admin do clube. Quem define o provedor no tenant é o operador da plataforma no backoffice.

**Onde fica o `tenantId` para montar a URL do webhook?**  
É o identificador GUID do tenant no banco **master** (não o slug). Operadores podem obtê-lo no detalhe do tenant no backoffice.

**O sócio consegue pagar antes das chaves estarem corretas?**  
Enquanto o gateway não estiver configurado e apto, fluxos que dependem do Stripe podem falhar — conclua provedor + chaves + webhook antes de ir a produção.

---

## Referência técnica (API)

- `GET /api/payments/admin/member-gateway` — status (JWT + `Administrador` + `X-Tenant-Id`)
- `PUT /api/payments/admin/member-gateway/stripe-direct` — credenciais Stripe direto

Headers: `X-Tenant-Id` (slug do clube), `Authorization: Bearer <token>`.
