# Guia do administrador do clube: Stripe Connect

Este guia é para quem administra o **clube** no sistema (role **Administrador**) e precisa **configurar a conta Stripe** para receber pagamentos de sócios (assinaturas, checkout).

Para o conceito geral e referência de API, veja também **`docs/user_guid/stripe-connect.md`**.

---

## O que é e por que configurar

O **Stripe Connect** associa o seu clube a uma **conta Stripe Express**. Sem essa configuração concluída, o sistema não consegue processar cobranças de sócios na conta do clube quando o gateway Stripe está ativo na plataforma.

A **plataforma SaaS** (software) continua separada: o que você configura aqui é **receber dos sócios**, não o faturamento do clube para a plataforma (isso é outro fluxo / backoffice).

---

## Pré-requisitos

1. Conta de usuário com role **Administrador** no seu clube.
2. Acesso ao aplicativo web do tenant (URL do clube, com `X-Tenant-Id` resolvido — normalmente subdomínio ou hostname configurado).
3. A API do ambiente deve ter **Stripe configurada** (`Payments:StripeSecretKey`). Caso contrário, ao gerar o link aparecerá erro da API.

---

## Passo a passo no aplicativo

1. Faça login como **Administrador**.
2. No menu, abra **Stripe Connect** (rota **`/admin/stripe`**).
3. Leia o status exibido:
   - Se ainda não há conta, siga para o passo 4.
   - Se já existe conta mas cobranças não estão habilitadas, use **Retomar configuração**.
4. Clique em **Configurar conta Stripe** ou **Retomar configuração**. Uma nova aba abrirá o fluxo oficial da **Stripe** (cadastro, KYC, dados bancários conforme exigido no seu país).
5. Conclua os passos na Stripe. Ao final, você pode ser redirecionado de volta para a URL de retorno configurada pelo sistema (em geral, de volta à área admin).
6. Na página **Stripe Connect**, clique em **Atualizar status** até aparecer que a conta está **ativa** (cobranças e repasses habilitados, quando aplicável). Esse botão **consulta a Stripe em tempo real**, atualiza o banco da plataforma e atualiza a tela — não depende só do webhook.

O estado no servidor também pode ser atualizado por **webhooks** da Stripe em paralelo.

---

## Como saber se está tudo certo

Na mesma página, verifique:

- **Cobranças habilitadas** e **Repasses habilitados** como *sim* (quando a Stripe já liberou).
- Mensagem indicando que a **conta Stripe está ativa** e o clube pode receber pagamentos de sócios.

Se ainda aparecer pendência, a Stripe costuma indicar no próprio painel o que falta; use **Retomar configuração** para voltar ao fluxo.

---

## Perguntas frequentes

**Preciso da chave de API do backoffice da plataforma (`X-Api-Key`)?**  
Não. O administrador do clube usa apenas **login e senha** do tenant (JWT). A chave do backoffice é só para operadores da plataforma SaaS.

**Posso alterar dados bancários ou empresa depois?**  
Sim. Alterações subsequentes costumam ser feitas pelo fluxo da Stripe (conta conectada). Use **Retomar configuração** no app se o link de onboarding for gerado de novo pela API.

**Quanto tempo leva a aprovação?**  
Depende da Stripe e da completude dos dados (documentos, verificação). Em modo teste, o fluxo é mais rápido; em produção pode levar de minutos a dias.

**O sócio consegue pagar antes do Connect estar ativo?**  
Enquanto a conta conectada não estiver apta a cobrar (`chargesEnabled` etc.), os fluxos de pagamento do sócio que dependem do Stripe podem falhar ou não estar disponíveis — trate a configuração como pré-requisito.

---

## Referência técnica (API)

- `POST /api/payments/admin/connect/onboarding` — corpo: `{ "refreshUrl", "returnUrl" }`
- `GET /api/payments/admin/connect/status` — lê apenas o banco master
- `POST /api/payments/admin/connect/sync` — sincroniza com a Stripe e persiste (sem corpo)

Headers em todas as chamadas: `X-Tenant-Id` (slug do clube), `Authorization: Bearer <token>`.
