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
| **Operação / backoffice da plataforma** | Chama a API com `X-Api-Key` para iniciar onboarding e consultar status por `tenantId`. |
| **Clube (administrador)** | Pode **iniciar o onboarding e ver o status** pela área admin do SPA (`/admin/stripe`) com JWT + `X-Tenant-Id`, ou usar o link gerado pelo operador da plataforma. Completa o cadastro KYC e dados bancários no fluxo hospedado pela Stripe. |
| **Sócio** | Assina plano e paga quando o gateway Stripe está ativo e o clube concluiu o Connect; fluxo em rotas do tenant (`X-Tenant-Id` + JWT). |

---

## Pré-requisitos

1. **Stripe configurada na API**: `Payments:StripeSecretKey` preenchido (e demais chaves descritas na documentação de pagamentos).
2. **Tenant** existente (slug no header `X-Tenant-Id` nas rotas do clube; ou `tenantId` nas rotas de backoffice).

Sem Stripe configurado, a API retorna erro ao tentar iniciar onboarding.

3. **Admin do clube:** usuário com role **`Administrador`** no tenant (JWT) para usar as rotas `api/payments/admin/connect/*`.

### Troca de plano e dados criados com o gateway stub

Se o clube ou o ambiente já tinham assinaturas registradas com o **stub** (IDs internos como `mem_sub_*`), ao ativar Stripe Connect e a chave secreta da Stripe a troca de plano do sócio continua funcionando: o backend não envia esses IDs para a API de assinaturas da Stripe e trata cancelamentos já inexistentes na Stripe como concluídos. Referência técnica: `docs/Stripe/configuracao-chaves-e-webhooks.md` (secção **Migração do gateway stub para Stripe**).

---

## O que o sistema armazena (conceito)

No **banco master** é mantido um registro por clube com:

- identificador da **conta Stripe** (`acct_...`);
- flags vindas da Stripe: cobranças habilitadas, repasses habilitados, formulário enviado;
- status de onboarding agregado (`Pendente`, `Pendente de requisitos`, `Habilitado` — valores numéricos na API).

Atualizações desses dados ocorrem via **webhooks Connect** (thin events) descritos em `docs/Stripe/configuracao-chaves-e-webhooks.md`, ou manualmente pelo admin do clube com **`POST /api/payments/admin/connect/sync`**, que consulta a API da Stripe, grava no master e devolve o DTO atualizado.

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

## API do clube (administrador do tenant)

Todas exigem **`X-Tenant-Id`** (slug), **JWT** (Bearer) e role **`Administrador`**. O `tenantId` é resolvido pelo middleware a partir do slug — o admin **não** envia GUID na URL.

Base path: `api/payments/admin/connect`

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `onboarding` | Mesmo comportamento do backoffice: cria conta Express se necessário e devolve **URL** de onboarding. Corpo JSON: `refreshUrl`, `returnUrl`. |
| `GET` | `status` | Lê o registro no banco master e retorna `StripeConnectStatusDto` (sem chamar a Stripe). |
| `POST` | `sync` | Chama a **API Stripe** para a conta conectada do tenant, atualiza o master (`charges_enabled`, etc.) e retorna o `StripeConnectStatusDto` atualizado. Exige conta Connect já criada; caso contrário responde 404. |

Exemplo: `POST https://<sua-api>/api/payments/admin/connect/onboarding`

Guia passo a passo para o admin do clube: **`docs/user_guid/tenant-stripe-connect.md`**.

---

## Fluxo recomendado

### Pelo operador da plataforma (backoffice)

1. Operador chama **onboarding** com URLs de retorno adequadas (ex.: página admin do clube).
2. O clube abre o **link** retornado e completa os passos na Stripe.
3. A aplicação consulta **status** até `chargesEnabled` / onboarding indicar conta pronta (conforme sua regra de negócio).
4. Webhooks Connect mantêm o estado sincronizado quando a Stripe envia eventos (ex.: `v1.account.updated`).

### Pelo administrador do clube (SPA)

1. Acessar **`/admin/stripe`** no domínio do tenant (com login de **Administrador**).
2. Clicar em **Configurar conta Stripe** (ou **Retomar configuração**), abrir o link na Stripe e concluir dados exigidos.
3. Voltar ao app e usar **Atualizar status** (que dispara o `POST sync` e busca os dados **direto na Stripe**) até indicar conta ativa (`chargesEnabled` / `payoutsEnabled`).
4. Os webhooks Connect continuam atualizando o estado no servidor em paralelo.

---

## Webhooks

Os eventos da **conta conectada** chegam em:

`POST /api/webhooks/stripe/connect`

Configuração de segredos e lista de eventos: **`docs/Stripe/configuracao-chaves-e-webhooks.md`** (seção Connect).

---

## Onde aprofundar no código

- Backoffice: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/BackofficeStripeConnectController.cs`
- Admin do tenant: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Api/Controllers/AdminStripeConnectController.cs`
- Sincronização com a Stripe: `backend/src/Modules/Payments/SocioTorcedor.Modules.Payments.Application/Commands/SyncStripeConnectStatus/SyncStripeConnectStatusHandler.cs`
- Módulo: `backend/src/Modules/Payments/AGENTS.md`
