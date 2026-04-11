# Guia do usuário: Stripe Connect

Este documento explica, em linguagem de produto, o que é o **Stripe Connect** neste sistema, para que serve e como se relaciona com as operações do clube (tenant) e dos sócios.

---

## O que é

**Stripe Connect** permite que cada **clube** (tenant) tenha uma **conta conectada Stripe** (modelo **Express** neste projeto). Assim, pagamentos de **sócios** (assinaturas, checkout) podem ser processados **na conta do clube**, e não na conta da plataforma SaaS.

Em resumo:

- **Pagamento SaaS** (outro fluxo): o clube paga a **plataforma** pelo uso do software.
- **Stripe Connect**: o clube **recebe** pagamentos dos próprios sócios via Stripe, com a plataforma orquestrando onboarding e integração.

---

## Quem usa

| Papel | Uso típico |
|--------|------------|
| **Operação / backoffice da plataforma** | Chama a API com `X-Api-Key` para iniciar onboarding e consultar status por tenant. |
| **Clube (administrador)** | Completa o cadastro KYC e dados bancários no fluxo hospedado pela Stripe (links gerados pela API). |
| **Sócio** | Assina plano e paga quando o gateway Stripe está ativo e o clube concluiu o Connect; fluxo em rotas do tenant (`X-Tenant-Id` + JWT). |

---

## Pré-requisitos

1. **Stripe configurada na API**: `Payments:StripeSecretKey` preenchido (e demais chaves descritas na documentação de pagamentos).
2. **Tenant** existente (identificado por `tenantId` nas rotas de backoffice).

Sem Stripe configurado, a API retorna erro ao tentar iniciar onboarding.

---

## O que o sistema armazena (conceito)

No **banco master** é mantido um registro por clube com:

- identificador da **conta Stripe** (`acct_...`);
- flags vindas da Stripe: cobranças habilitadas, repasses habilitados, formulário enviado;
- status de onboarding agregado (`Pendente`, `Pendente de requisitos`, `Habilitado` — valores numéricos na API).

Atualizações desses dados ocorrem via **webhooks Connect** (thin events) descritos em `docs/Stripe/configuracao-chaves-e-webhooks.md`.

---

## API de backoffice (referência)

Todas exigem o header **`X-Api-Key`** (mesma chave do backoffice configurada na API).

Base path: `api/backoffice/payments/connect`

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `tenants/{tenantId}/onboarding` | Inicia ou retoma onboarding: cria conta Express se ainda não existir e devolve **URL** para o clube abrir no navegador. Corpo JSON: `refreshUrl`, `returnUrl` (URLs absolutas para a Stripe redirecionar em caso de atualização ou conclusão). |
| `GET` | `tenants/{tenantId}/status` | Retorna se há conta configurada, `stripeAccountId`, status numérico de onboarding, `chargesEnabled`, `payoutsEnabled`, `detailsSubmitted`. |

Exemplo de URL completa: `POST https://<sua-api>/api/backoffice/payments/connect/tenants/{tenantId}/onboarding`

---

## Fluxo recomendado

1. Operador chama **onboarding** com URLs de retorno adequadas (ex.: página admin do clube).
2. O clube abre o **link** retornado e completa os passos na Stripe.
3. A aplicação consulta **status** até `chargesEnabled` / onboarding indicar conta pronta (conforme sua regra de negócio).
4. Webhooks Connect mantêm o estado sincronizado quando a Stripe envia eventos (ex.: `v1.account.updated`).

---

## Webhooks

Os eventos da **conta conectada** chegam em:

`POST /api/webhooks/stripe/connect`

Configuração de segredos e lista de eventos: **`docs/Stripe/configuracao-chaves-e-webhooks.md`** (seção Connect).

---

## Onde aprofundar no código

- Controller: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/BackofficeStripeConnectController.cs`
- Módulo: `backend/src/Modules/Payments/AGENTS.md`
