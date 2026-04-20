# Guia passo a passo — configuração Stripe (Dashboard + aplicação)

Este documento descreve, em ordem lógica, o que configurar **no Stripe** e **na API AppTorcedor** para pagamentos com **cartão** via **Stripe Checkout** (modo pagamento único), confirmação por **webhook assinado** e idempotência no banco.

Leia também: [guia-deploy.md](guia-deploy.md) (variáveis de ambiente e VPS) e [parte-d4-integracao-pagamento-contratacao.md](../architecture/parte-d4-integracao-pagamento-contratacao.md) (fluxo de negócio).

---

## 1. O que a aplicação espera (visão geral)

| Onde | Função |
|------|--------|
| `Payments:Provider` = `Stripe` | Ativa o `StripePaymentProvider` em vez do mock. |
| `Payments:Stripe:ApiKey` | Chave secreta da API (`sk_test_…` ou `sk_live_…`) para criar Checkout Sessions e cancelar/expirar cobranças. |
| `Payments:Stripe:WebhookSecret` | Segredo de assinatura do endpoint de webhook (`whsec_…`), **diferente** do callback legacy. |
| `Payments:Stripe:SuccessUrl` | URL para onde o Stripe redireciona o cliente após pagamento bem-sucedido (normalmente a SPA). |
| `Payments:Stripe:CancelUrl` | URL se o utilizador abandonar o Checkout. |
| `POST /api/webhooks/stripe` | Endpoint público **HTTPS** que o Stripe chama com o corpo bruto do evento e o cabeçalho `Stripe-Signature`. |
| `Payments:WebhookSecret` | Segredo do callback **legacy** `POST /api/subscriptions/payments/callback` (JSON com `secret`); opcional em produção só com Stripe, mas útil para testes manuais. |

**Importante:** com `Provider=Stripe`, **PIX não é suportado** nesta versão; o torcedor deve usar **cartão**. Tentar PIX devolve erro `payment_method_not_supported`.

---

## 2. Pré-requisitos

1. Conta Stripe (https://dashboard.stripe.com).
2. API pública em **HTTPS** (Stripe exige URL segura para webhooks em produção).
3. Banco SQL Server com **migrations aplicadas**, incluindo a tabela `ProcessedStripeWebhookEvents` (idempotência de webhooks).
4. Domínio da **SPA** definido para montar `SuccessUrl` / `CancelUrl` e para alinhar **CORS** na API.

---

## 3. Configuração no Stripe Dashboard

### 3.1. Modo Test vs Live

- Use **Test mode** (interruptor no canto superior do Dashboard) para homologação (`sk_test_…`, `pk_test_…`).
- Use **Live mode** apenas quando for para produção real (`sk_live_…`, `pk_live_…`).

Mantenha chaves de teste e live **separadas**; nunca use chaves live em ambiente de desenvolvimento.

### 3.2. Obter a chave secreta da API (`sk_…`)

1. No Dashboard: **Developers** → **API keys**.
2. Em **Secret key**, revele e copie a chave (`sk_test_…` ou `sk_live_…`).
3. Guarde em local seguro (gestor de segredos, Jenkins, `api.env` com permissão restrita).

Esta chave corresponde à variável **`Payments__Stripe__ApiKey`** na aplicação.

**Boas práticas:** restrinja permissões da chave no Stripe (API keys restritas) ao mínimo necessário para Checkout e webhooks, quando a sua conta suportar.

### 3.3. Chave publicável (`pk_…`)

A integração atual da API usa **Checkout hospedado** (URL devolvida em `card.checkoutUrl`); **não é obrigatório** configurar `pk_…` na API. A chave publicável só seria necessária se no futuro incorporar **Stripe.js** ou **Payment Element** no frontend.

### 3.4. Criar o endpoint de webhook

1. **Developers** → **Webhooks** → **Add endpoint**.
2. **Endpoint URL:** URL pública completa até o path da API, por exemplo:  
   `https://api.seudominio.com.br/api/webhooks/stripe`  
   (ajuste ao seu host reverso / prefixo; o path fixo na aplicação é **`/api/webhooks/stripe`** — ver secção abaixo sobre erros comuns).
3. **Events to send:** comece selecionando pelo menos:  
   **`checkout.session.completed`**  
   (é o evento que a aplicação processa para marcar o pagamento como pago e ativar a associação, quando aplicável).
4. Salve o endpoint.
5. Abra o endpoint criado e copie o **Signing secret** (começa com **`whsec_`**).

Este valor corresponde à variável **`Payments__Stripe__WebhookSecret`**.

### 3.4.1. Erro comum: URL do webhook errada

A API expõe **apenas** este endpoint para o Stripe:

| Correto | Incorreto (exemplos que falham) |
|---------|----------------------------------|
| `https://<host-da-api>/api/webhooks/stripe` | `.../api/webhooks/stripe/member/<guid>` (path extra não existe no código) |
| | Misturar URL da **SPA** com path da API (`https://app.../api/webhooks/...` se `/api` for só backend) |

- O identificador do pagamento (`payment_id`) vai em **metadata** da Checkout Session, **não** no path da URL do webhook.
- Se o Dashboard Stripe mostrar **404** nas entregas, confira: host da **API** (não o Vite), HTTPS, e path exatamente `/api/webhooks/stripe`.

### 3.5. Validar entregas no Dashboard Stripe

1. **Developers** → **Webhooks** → selecione o endpoint.
2. Aba **Events** / **Attempted deliveries** (conforme a UI): confira **HTTP 2xx** nas tentativas recentes.
3. **400** costuma indicar assinatura inválida (`whsec` não coincide com o endpoint) ou corpo alterado pelo proxy.
4. **500** na AppTorcedor com segredo Stripe vazio: falta `Payments__Stripe__WebhookSecret`.

### 3.6. Testar o webhook (opcional)

- Com a API a correr localmente, pode usar o **Stripe CLI** (`stripe listen --forward-to localhost:5031/api/webhooks/stripe`) para receber eventos de teste; o CLI mostra um `whsec_` temporário para colocar em `Payments__Stripe__WebhookSecret` **só nesse ambiente**.

---

## 4. Configuração na aplicação (ASP.NET Core)

A secção de configuração é **`Payments`**, com objeto aninhado **`Stripe`**.

### 4.1. Segredos em desenvolvimento local

- **Não commite** `sk_…` nem `whsec_…` no Git. Use variáveis de ambiente ou **User Secrets** do .NET (`dotnet user-secrets set "Payments:Stripe:ApiKey" "sk_test_..."` no projeto `AppTorcedor.Api`).
- O ficheiro [`appsettings.Development.json`](../../backend/src/AppTorcedor.Api/appsettings.Development.json) no repositório mantém `ApiKey` / `Stripe:WebhookSecret` vazios por defeito; preencha localmente via secrets ou env.

### 4.2. Em `appsettings` (exemplo local)

Exemplo (não commite chaves reais):

```json
"Payments": {
  "Provider": "Stripe",
  "WebhookSecret": "opcional-callback-legacy",
  "Stripe": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "SuccessUrl": "http://localhost:5173/subscription/confirmation",
    "CancelUrl": "http://localhost:5173/plans"
  }
}
```

Em produção, prefira **variáveis de ambiente** ou ficheiros fora do Git.

### 4.3. Variáveis de ambiente (produção / Docker / VPS)

O ASP.NET Core mapeia `__` para hierarquia:

| Variável de ambiente | Equivalente em JSON |
|----------------------|---------------------|
| `Payments__Provider` | `Payments:Provider` |
| `Payments__WebhookSecret` | `Payments:WebhookSecret` |
| `Payments__Stripe__ApiKey` | `Payments:Stripe:ApiKey` |
| `Payments__Stripe__WebhookSecret` | `Payments:Stripe:WebhookSecret` |
| `Payments__Stripe__SuccessUrl` | `Payments:Stripe:SuccessUrl` |
| `Payments__Stripe__CancelUrl` | `Payments:Stripe:CancelUrl` |

Referência comentada: [`deploy/vps/api.env.example`](../../deploy/vps/api.env.example).

### 4.4. Docker Compose

No [`docker-compose.yml`](../../docker-compose.yml) as variáveis da API são injetadas a partir do `.env` na raiz com estes nomes (não use `Payments__Stripe__*` diretamente no `.env` — o Compose já faz o mapeamento):

| `.env` (raiz) | Variável dentro do contentor `api` |
|---------------|-----------------------------------|
| `PAYMENTS_PROVIDER` | `Payments__Provider` (`Mock` ou `Stripe`) |
| `PAYMENTS_WEBHOOK_SECRET` | `Payments__WebhookSecret` (callback legacy; distinto do `whsec_`) |
| `STRIPE_API_KEY` | `Payments__Stripe__ApiKey` |
| `STRIPE_WEBHOOK_SECRET` | `Payments__Stripe__WebhookSecret` |
| `STRIPE_SUCCESS_URL` | `Payments__Stripe__SuccessUrl` |
| `STRIPE_CANCEL_URL` | `Payments__Stripe__CancelUrl` |

Modelo: [`.env.compose.example`](../../.env.compose.example). Na VPS com Jenkins, o pipeline **não** grava Stripe — edite o `.env` no clone após o deploy (ou estenda o Jenkins) e execute `docker compose up -d --build` no diretório do repo.

### 4.5. URLs de sucesso e cancelamento (SPA)

Alinhe com as rotas reais do frontend:

- **SuccessUrl:** origem pública da SPA + **`/subscription/confirmation`**  
  Ex.: `https://app.seudominio.com.br/subscription/confirmation`
- **CancelUrl:** típico catálogo de planos + **`/plans`**  
  Ex.: `https://app.seudominio.com.br/plans`

Use **HTTPS** em produção. O domínio deve estar em **`Cors__AllowedOrigins__*`** na API.

**Erro comum:** usar `http://localhost:5173/api/subscription/confirmation` ou `.../api/plans`. O Vite/React define rotas **`/subscription/confirmation`** e **`/plans`** (sem prefixo `/api` — esse prefixo é da API ASP.NET, não da SPA).

### 4.6. Migração de base de dados

Garanta que a migração que cria **`ProcessedStripeWebhookEvents`** foi aplicada (`dotnet ef database update` ou o processo que já usam em deploy). Sem esta tabela, a idempotência de webhooks não persiste corretamente.

### 4.7. Reverse proxy

O proxy (Nginx, Caddy, etc.) deve:

- Encaminhar tráfego HTTPS para a API.
- Não descartar o corpo bruto nem o cabeçalho **`Stripe-Signature`** no `POST /api/webhooks/stripe`.

---

## 5. Ordem recomendada de implementação (checklist)

1. Aplicar migrations no SQL Server.
2. Definir `Payments__Provider=Stripe` e `Payments__Stripe__ApiKey`.
3. Definir `SuccessUrl` e `CancelUrl` com URLs reais da SPA (HTTPS em produção).
4. Configurar CORS na API para a origem da SPA.
5. Expor publicamente `https://<api>/api/webhooks/stripe`.
6. Criar o webhook no Stripe com essa URL e evento `checkout.session.completed`.
7. Copiar `whsec_…` para `Payments__Stripe__WebhookSecret` e reiniciar a API.
8. Fazer um pagamento de teste (modo teste do Stripe + cartão de teste).
9. No Dashboard Stripe, verificar entregas do webhook (sucesso HTTP 2xx).
10. Na aplicação, confirmar que o pagamento passou a `Paid` e a associação a `Ativo` (ou fluxo D.6 de proporcional, se for o caso).

---

## 6. Jenkins e deploy automático

O [`Jenkinsfile`](../../Jenkinsfile) gera `api.env` e o ficheiro de ambiente do Compose com:

- **Credenciais:** `stripe-api-key`, `stripe-webhook-secret`, `api-webhook-secret` (callback legacy), **`payments-provider`** (texto `Mock` ou `Stripe`), **`stripe-success-url`**, **`stripe-cancel-url`** (URLs HTTPS da SPA; podem ser Secret text vazio). Crie **`stripe-api-key`** e **`stripe-webhook-secret`** mesmo usando só **Mock** (Secret text **vazio**), porque o pipeline referencia esses IDs.

Assim as entradas do [`docker-compose.yml`](../../docker-compose.yml) (`PAYMENTS_PROVIDER`, `STRIPE_*`, `PAYMENTS_WEBHOOK_SECRET`) ficam alinhadas ao deploy. Ver [guia-deploy.md §4.3](guia-deploy.md).

Não commite `sk_…` nem `whsec_…` no Git.

---

## 7. Problemas frequentes

| Sintoma | Causa provável |
|---------|----------------|
| **404** nas entregas do webhook no Stripe | Path incorreto (ex.: `/api/webhooks/stripe/member/...`), host errado (SPA em vez de API), ou API não expõe a rota. |
| `500` no webhook com log de segredo vazio | `Payments__Stripe__WebhookSecret` não definido. |
| `400` no webhook | Assinatura inválida (`whsec` de outro endpoint ou modo test/live trocado), corpo alterado pelo proxy, ou leitura dupla do body. |
| Pagamento não confirma | Webhook não configurado, evento errado, ou `checkout.session.completed` não selecionado. |
| Valor não bate | Dados do `PaymentRecord` na API não coincidem com `amount_total` / moeda `brl` da sessão. |
| Redirecionamento após Checkout para página em branco /404 na SPA | `SuccessUrl` / `CancelUrl` com `/api/...` no host da SPA (ver secção 4.5). |
| CORS no browser após Checkout | Origem da SPA não listada em `Cors__AllowedOrigins__*`. |
| PIX escolhido com Stripe | Comportamento esperado: use apenas cartão com `Provider=Stripe`. |

---

## 8. Segurança (resumo)

- Trate `sk_…` e `whsec_…` como segredos; rotação periódica no Stripe.
- Use sempre HTTPS em produção para API e SPA.
- Não registe em log o corpo completo de webhooks nem chaves.
- Limite acesso administrativo ao Dashboard Stripe e ative 2FA na conta.

---

## 9. Referência rápida de endpoints

| Método | Path | Uso |
|--------|------|-----|
| `POST` | `/api/subscriptions` | Inicia contratação (JWT); resposta inclui `card.checkoutUrl`. |
| `POST` | `/api/webhooks/stripe` | Stripe envia eventos assinados (público). |
| `POST` | `/api/subscriptions/payments/callback` | Callback legacy com `secret` no JSON (opcional com Stripe puro). |
