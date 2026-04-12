# Guia do administrador do clube: gateway Stripe direto (sócios)

Este guia é para quem administra o **clube** no sistema (role **Administrador**) e precisa **configurar a conta Stripe do clube** para receber pagamentos de sócios (assinaturas, checkout, PIX quando disponível).

Para visão geral e separação SaaS vs sócios, veja **`docs/user_guid/stripe-connect.md`**.

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

## Passo a passo no aplicativo

1. Faça login como **Administrador**.
2. Abra **Gateway** no menu (rota **`/admin/stripe`**).
3. Confira o **provedor** e o **status** exibidos no topo.
4. Preencha:
   - **Secret key** (`sk_…`) — obrigatória em todo envio de salvamento (a API exige a chave completa a cada atualização).
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
