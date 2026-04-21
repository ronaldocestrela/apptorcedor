# Guia de Configurações — AppTorcedor

Este documento descreve **todas as configurações disponíveis** no sistema, organizadas em dois grupos:

- **Tipo A — Configurações de Deploy**: definidas em `appsettings.json`, variáveis de ambiente ou `api.env` na VPS. Exigem reinicialização da API para entrar em vigor. Controlam infraestrutura, segurança e integrações externas.
- **Tipo B — Configurações Administrativas**: armazenadas no banco de dados (tabela `AppConfigurationEntries`). Editáveis pelo admin via painel em `/admin/configurations` sem necessidade de redeploy. Controlam regras de negócio operacionais.

---

## Tipo A — Configurações de Deploy

### 1. Banco de Dados

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | *(obrigatório)* | String de conexão completa ao SQL Server. Formato: `Server=host,1433;Database=AppTorcedor;User Id=sa;Password=xxx;TrustServerCertificate=true;` |
| `UseInMemoryDatabase` | `UseInMemoryDatabase` | `false` | Define `true` para usar banco em memória (somente desenvolvimento/testes). Nenhum dado é persistido entre reinicializações. |

**Impacto:** sem conexão válida (e `UseInMemoryDatabase=false`), a API não inicia.

---

### 2. JWT — Autenticação

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Jwt:Key` | `Jwt__Key` | *(obrigatório)* | Chave simétrica HS256. Mínimo 32 bytes em UTF-8. Alterar invalida **todos** os tokens ativos e forçará novo login de todos os usuários. |
| `Jwt:Issuer` | `Jwt__Issuer` | `AppTorcedor` | Emissor declarado no JWT. Deve coincidir na validação. |
| `Jwt:Audience` | `Jwt__Audience` | `AppTorcedor` | Audiência declarada no JWT. Deve coincidir na validação. |
| `Jwt:AccessTokenMinutes` | `Jwt__AccessTokenMinutes` | `15` | Tempo de vida do access token em minutos. Valores muito altos aumentam a janela de risco em caso de token comprometido. |
| `Jwt:RefreshTokenDays` | `Jwt__RefreshTokenDays` | `14` | Tempo de vida do refresh token em dias. Controla por quanto tempo o usuário permanece logado sem precisar inserir senha novamente. |

**Impacto:** `Jwt:Key` é crítico para segurança. Nunca versionar em arquivos Git.

---

### 3. Seed — Usuário Administrador Master

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Seed:AdminMaster:Email` | `Seed__AdminMaster__Email` | `admin@torcedor.local` | E-mail do administrador master criado na primeira inicialização. |
| `Seed:AdminMaster:Password` | `Seed__AdminMaster__Password` / `ADMIN_MASTER_INITIAL_PASSWORD` | *(nulo)* | Senha inicial do admin master. **Obrigatório fora de Development/Testing** — a aplicação recusa iniciar se estiver vazio em produção. Em desenvolvimento, o seed usa `ChangeMe_Integration1!` como fallback. |

**Regra de segurança:** Em produção, prefira a variável de ambiente `ADMIN_MASTER_INITIAL_PASSWORD` em vez de colocar a senha em arquivo de configuração.

---

### 4. CORS — Origens Permitidas

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... | `http://localhost:5173` | Lista de origens HTTP(S) permitidas para chamadas da SPA (React). Em produção, deve conter a URL pública do frontend. Múltiplas origens usam sufixo `__0`, `__1`, etc. |

**Impacto:** origens não listadas recebem erro CORS no browser e não conseguem chamar a API.

---

### 5. Login Social — Google

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Google:Auth:ClientId` | `Google__Auth__ClientId` | *(vazio = desativado)* | Client ID OAuth 2.0 Web do Google (formato `xxx.apps.googleusercontent.com`). Se vazio, o endpoint `POST /api/auth/google` rejeita qualquer tentativa de login com Google. |

**Impacto:** deixar em branco desativa completamente o login Google sem afetar o login por e-mail/senha.

---

### 6. Gateway de Pagamento

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Payments:Provider` | `Payments__Provider` | `Mock` | Define qual gateway processa pagamentos. Valores: `Mock` (sem cobranças reais, para testes) ou `Stripe` (cobranças reais via Checkout). |
| `Payments:WebhookSecret` | `Payments__WebhookSecret` | *(vazio)* | Segredo do callback legacy `POST /api/subscriptions/payments/callback`. Usado em integrações de teste manual ou legado. |
| `Payments:Stripe:ApiKey` | `Payments__Stripe__ApiKey` | *(vazio)* | Chave secreta do Stripe (`sk_test_…` ou `sk_live_…`). Necessária quando `Provider=Stripe`. |
| `Payments:Stripe:WebhookSecret` | `Payments__Stripe__WebhookSecret` | *(vazio)* | Signing secret do endpoint de webhook no Stripe Dashboard (`whsec_…`). Valida autenticidade dos eventos recebidos. **Diferente** do `Payments:WebhookSecret`. Em respostas de `POST /api/webhooks/stripe`, a API inclui o cabeçalho **`X-Stripe-Webhook-Result`** (`BadSignature`, `InvalidPayload`, `ConfigurationError`, `Ok`, …) para diagnóstico; veja [guia-configuracao-stripe.md](deploy/guia-configuracao-stripe.md) §3.5. |
| `Payments:Stripe:SuccessUrl` | `Payments__Stripe__SuccessUrl` | *(vazio)* | URL para onde o Stripe redireciona o torcedor após pagamento bem-sucedido. Deve apontar para `{SPA}/subscription/confirmation`. |
| `Payments:Stripe:CancelUrl` | `Payments__Stripe__CancelUrl` | *(vazio)* | URL para onde o Stripe redireciona se o torcedor abandonar o Checkout. Normalmente `{SPA}/plans`. |

**Impacto de `Provider=Mock`:** nenhuma cobrança real é realizada. Útil para homologação. Trocar para `Stripe` em produção sem as demais chaves causa erro 500 no checkout.

**Nota sobre PIX:** com `Provider=Stripe`, PIX não é suportado. Tentativas retornam `payment_method_not_supported`.

---

### 7. Armazenamento de Fotos de Perfil

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `ProfilePhotos:Provider` | `ProfilePhotos__Provider` | `Local` | Define onde as fotos de perfil são armazenadas. Valores: `Local` (disco local da API, pasta `wwwroot/uploads/profile-photos`) ou `Cloudinary` (CDN). |
| `ProfilePhotos:RootPath` | `ProfilePhotos__RootPath` | *(vazio = automático)* | Caminho absoluto para armazenamento local. Se vazio, usa `{ContentRoot}/wwwroot/uploads/profile-photos`. |
| `ProfilePhotos:MaxBytes` | `ProfilePhotos__MaxBytes` | `5242880` (5 MB) | Tamanho máximo em bytes para upload de foto de perfil. |
| `ProfilePhotos:Cloudinary:Folder` | `ProfilePhotos__Cloudinary__Folder` | `profile-photos` | Nome da pasta dentro do Cloudinary onde as fotos são salvas. |

**Impacto:** trocar de `Local` para `Cloudinary` exige que as credenciais Cloudinary estejam configuradas. Fotos antigas salvas localmente não são migradas automaticamente.

---

### 7.5. Armazenamento do escudo do clube (TeamShield)

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `TeamShield:Provider` | `TeamShield__Provider` | `Local` | Onde a imagem do **escudo do time** é armazenada após upload pelo admin. Valores: `Local` (disco em `wwwroot/uploads/team-shield`) ou `Cloudinary` (URL absoluta HTTPS). |
| `TeamShield:RootPath` | `TeamShield__RootPath` | *(vazio = automático)* | Caminho absoluto local. Se vazio, usa `{ContentRoot}/wwwroot/uploads/team-shield`. |
| `TeamShield:MaxBytes` | `TeamShield__MaxBytes` | `5242880` (5 MB) | Tamanho máximo do arquivo (JPEG, PNG ou WebP). |
| `TeamShield:Cloudinary:Folder` | `TeamShield__Cloudinary__Folder` | `team-shield` | Pasta no Cloudinary para o escudo (sobrescrita no mesmo `public_id` lógico por upload). |

**Leitura pública:** `GET /api/branding` (anônimo) retorna `{ "teamShieldUrl": "<url ou null>" }`. O valor persistido no banco é a chave administrativa `Brand.TeamShieldUrl` (Tipo B).

**Upload administrativo:** `POST /api/admin/config/team-shield` com `multipart/form-data` campo `file` — exige JWT e permissão `Configuracoes.Editar`. Atualiza `Brand.TeamShieldUrl` e remove o arquivo anterior quando aplicável (best effort).

---

### 7.6. Logos de adversário em jogos (OpponentLogos)

Armazenamento das imagens enviadas para a **biblioteca de logos do adversário** (`POST /api/admin/games/opponent-logos`), no mesmo padrão Local/Cloudinary das demais mídias administrativas.

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `OpponentLogos:Provider` | `OpponentLogos__Provider` | `Local` | `Local` (`wwwroot/uploads/opponent-logos`) ou `Cloudinary`. |
| `OpponentLogos:RootPath` | `OpponentLogos__RootPath` | *(vazio = automático)* | Caminho absoluto local opcional. |
| `OpponentLogos:MaxBytes` | `OpponentLogos__MaxBytes` | `6291456` (6 MB) | Tamanho máximo (JPEG, PNG ou WebP). |
| `OpponentLogos:Cloudinary:Folder` | `OpponentLogos__Cloudinary__Folder` | `opponent-logos` | Pasta no Cloudinary para novos uploads (um `public_id` por arquivo). |

---

### 8. Armazenamento de Anexos de Suporte

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `SupportTicketAttachments:Provider` | `SupportTicketAttachments__Provider` | `Local` | Onde os anexos dos chamados de suporte são armazenados. Valores: `Local` ou `Cloudinary`. |
| `SupportTicketAttachments:RootPath` | `SupportTicketAttachments__RootPath` | *(vazio = automático)* | Caminho absoluto local. Se vazio, usa `{ContentRoot}/Data/support-attachments`. |
| `SupportTicketAttachments:MaxBytesPerFile` | `SupportTicketAttachments__MaxBytesPerFile` | `5242880` (5 MB) | Tamanho máximo por arquivo em bytes. |
| `SupportTicketAttachments:MaxFilesPerMessage` | `SupportTicketAttachments__MaxFilesPerMessage` | `5` | Quantidade máxima de arquivos por mensagem de suporte. |
| `SupportTicketAttachments:Cloudinary:Folder` | `SupportTicketAttachments__Cloudinary__Folder` | `support-attachments` | Pasta no Cloudinary para anexos de suporte. |

**Nota sobre Cloudinary:** imagens são enviadas como `resource_type=image`; PDFs como `resource_type=raw`. O download é sempre feito via stream autenticado pela API (não expõe URL direta ao cliente).

---

### 9. Cloudinary — CDN de Mídia

Necessário quando `ProfilePhotos:Provider=Cloudinary`, `SupportTicketAttachments:Provider=Cloudinary`, `TeamShield:Provider=Cloudinary` ou `OpponentLogos:Provider=Cloudinary`.

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `Cloudinary:CloudName` | `Cloudinary__CloudName` | *(vazio)* | Nome da cloud no Cloudinary (ex.: `apptorcedor`). |
| `Cloudinary:ApiKey` | `Cloudinary__ApiKey` | *(vazio)* | API Key do Cloudinary. |
| `Cloudinary:ApiSecret` | `Cloudinary__ApiSecret` | *(vazio)* | API Secret do Cloudinary. **Nunca versionar.** |

---

### 10. Ambiente de Execução

| Chave | Variável de Ambiente | Padrão | Descrição |
|-------|----------------------|--------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT` | `Production` | Define o ambiente. Valores: `Development`, `Testing`, `Production`. Em `Development`, habilita Swagger/OpenAPI e cria documentos legais de desenvolvimento automaticamente. Em `Testing`, desativa os background services (sweep de inadimplência e dispatcher de notificações). |
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS` | `http://localhost:5031` | Endereço e porta em que o Kestrel escuta. Em VPS, normalmente `http://127.0.0.1:5031` (exposto via reverse proxy). |

---

### 11. Logging

| Chave | Padrão | Descrição |
|-------|--------|-----------|
| `Logging:LogLevel:Default` | `Information` | Nível mínimo de log para todos os componentes. Valores: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Nível de log específico para o framework ASP.NET Core. Reduz ruído nos logs de produção. |

---

### 12. Frontend — Variáveis de Ambiente (Vite)

Configuradas no arquivo `.env.development`, `.env.production` ou no processo de build (Jenkins).

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `VITE_API_URL` | `http://localhost:5031` | URL base da API consumida pelo browser. Em produção, deve ser a URL pública da API (HTTPS). |
| `VITE_GOOGLE_CLIENT_ID` | *(vazio = desativado)* | Client ID OAuth do Google para o botão de login social no frontend. Se vazio, o botão de login Google não é exibido. Deve coincidir com `Google:Auth:ClientId` na API. |

---

## Tipo B — Configurações Administrativas (banco de dados)

Estas configurações são armazenadas na tabela `AppConfigurationEntries` e gerenciadas pelo painel em `/admin/configurations`. Cada alteração é **auditada automaticamente** (quem mudou, quando, valor anterior e novo).

**API:**
- Listar: `GET /api/admin/config` (permissão: `Configuracoes.Visualizar`)
- Criar/Atualizar: `PUT /api/admin/config/{key}` com `{ "value": "..." }` (permissão: `Configuracoes.Editar`)

O `value` é sempre uma string. Pode conter um número, um booleano em texto ou um JSON serializado.

---

### B.1 — Cancelamento de Associação

#### `Membership.CancellationCoolingOffDays`

| Atributo | Detalhe |
|----------|---------|
| **Tipo do valor** | Inteiro positivo (string numérica) |
| **Padrão (fallback)** | `7` |
| **Máximo aceito** | `365` |
| **Usado em** | `TorcedorMembershipCancellationService` (fluxo D.7) |

**O que faz:**

Define o **prazo de arrependimento** em dias que o torcedor tem para cancelar a associação com efeito **imediato** e sem penalidades após a contratação.

**Comportamento detalhado:**

- Se o torcedor cancelar **dentro** do prazo configurado: a associação é encerrada **imediatamente**, cobranças abertas são canceladas no gateway de pagamento e o status passa para `Cancelado`.
- Se o torcedor cancelar **fora** do prazo: o cancelamento é **agendado** para o fim do ciclo atual (`NextDueDate`). O acesso permanece ativo até lá e o status só muda para `Cancelado` quando o sweep efetivar o cancelamento agendado.
- Contratos em status `PendingPayment` (ainda não pagos) também são encerrados imediatamente, independente do prazo.

**Exemplos:**

```
"Membership.CancellationCoolingOffDays" = "7"   → 7 dias de arrependimento (padrão)
"Membership.CancellationCoolingOffDays" = "14"  → 14 dias (ex.: para alinhar com o CDC)
"Membership.CancellationCoolingOffDays" = "0"   → sem arrependimento; cancelamento sempre agendado
"Membership.CancellationCoolingOffDays" = "30"  → 30 dias de arrependimento
```

**Impacto regulatório:** O Código de Defesa do Consumidor (CDC) prevê 7 dias de arrependimento para compras fora do estabelecimento. Ajuste conforme orientação jurídica do clube.

---

### B.2 — Identidade visual (escudo do clube)

#### `Brand.TeamShieldUrl`

| Atributo | Detalhe |
|----------|---------|
| **Tipo do valor** | String — caminho relativo servido pela API (ex.: `/uploads/team-shield/....jpg`) ou URL absoluta (Cloudinary) |
| **Padrão** | *(ausente)* — o frontend usa um **placeholder SVG** até o primeiro upload |
| **Definido por** | `POST /api/admin/config/team-shield` (recomendado) ou edição manual da chave em `/admin/configurations` |
| **Consumo** | `GET /api/branding`; componente `TeamShieldLogo` no login, cadastro, shell admin e header do torcedor |

**Nota:** alterar apenas o texto da chave sem enviar arquivo por multipart **não** cria arquivo novo no storage; prefira sempre o upload pelo painel (seção *Identidade — escudo do clube*) para manter arquivo e URL alinhados.

---

## Comportamentos Automáticos (não configuráveis pelo admin)

Os itens abaixo são comportamentos fixos no código que operam de forma periódica sem configuração administrativa:

| Serviço | Intervalo | O que faz |
|---------|-----------|-----------|
| `PaymentDelinquencyHostedService` | A cada **5 minutos** | Marca cobranças `Pending` com `DueDate` vencida como `Overdue`. Associações `Ativas` com ao menos uma cobrança `Overdue` passam para `Inadimplente`. |
| `InAppNotificationDispatchHostedService` | A cada **30 segundos** | Processa notificações in-app com `ScheduledAt <= agora` e status `Pending`, marcando-as como `Dispatched`. |
| `MembershipScheduledCancellationEffectiveSweep` | Executado junto ao sweep de inadimplência | Efetiva cancelamentos agendados cujo `EndDate` já foi ultrapassado. |

> Estes serviços são **desativados automaticamente** no ambiente `Testing` para não interferir nos testes automatizados.

---

## Referência rápida: variáveis de ambiente em produção (VPS / Docker)

```bash
# Banco de dados
ConnectionStrings__DefaultConnection="Server=...;Database=AppTorcedor;..."

# JWT (segredo obrigatório)
Jwt__Key="minimo-32-caracteres-utf8-seguro"
Jwt__Issuer="AppTorcedor"
Jwt__Audience="AppTorcedor"
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=14

# Admin master inicial
ADMIN_MASTER_INITIAL_PASSWORD="SenhaForte@Producao1"

# Ambiente
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5031

# CORS (origem da SPA)
Cors__AllowedOrigins__0=https://app.seudominio.com.br

# Google Login (opcional)
Google__Auth__ClientId=xxx.apps.googleusercontent.com

# Pagamentos
Payments__Provider=Stripe
Payments__WebhookSecret=segredo-callback-legacy
Payments__Stripe__ApiKey=sk_live_...
Payments__Stripe__WebhookSecret=whsec_...
Payments__Stripe__SuccessUrl=https://app.seudominio.com.br/subscription/confirmation
Payments__Stripe__CancelUrl=https://app.seudominio.com.br/plans

# Storage de mídia
ProfilePhotos__Provider=Cloudinary
TeamShield__Provider=Cloudinary
OpponentLogos__Provider=Cloudinary
SupportTicketAttachments__Provider=Cloudinary
Cloudinary__CloudName=apptorcedor
Cloudinary__ApiKey=...
Cloudinary__ApiSecret=...
```

---

## Segurança — Resumo das Chaves Sensíveis

| Chave | Risco se exposta |
|-------|------------------|
| `Jwt:Key` | Permite forjar tokens JWT e se autenticar como qualquer usuário |
| `Payments:Stripe:ApiKey` | Acesso total à conta Stripe (criar cobranças, estornar, etc.) |
| `Payments:Stripe:WebhookSecret` | Permite forjar eventos Stripe e confirmar pagamentos falsos |
| `Cloudinary:ApiSecret` | Acesso total ao Cloudinary (upload, delete, leitura de arquivos) |
| `ConnectionStrings:DefaultConnection` | Acesso direto ao banco de dados |
| `ADMIN_MASTER_INITIAL_PASSWORD` | Senha do administrador master |

> **Nenhuma dessas chaves deve ser versionada no Git.** Use variáveis de ambiente, Jenkins Credentials ou gerenciadores de segredos.

---

## Frontend — SEO (arquivos estáticos)

Ajustes manuais por ambiente de produção (não usam `appsettings`):

| Arquivo | O que revisar |
|---------|----------------|
| [`frontend/index.html`](../frontend/index.html) | `og:url` aponta para placeholder `https://seudominio.com.br/` — substituir pela URL pública real da SPA. Para compartilhamento social, prefira URL **absoluta** também em `og:image` (ex.: `https://app.seudominio.com.br/logos/ESCUDO_FFC_PNG.png`). |
| [`frontend/public/robots.txt`](../frontend/public/robots.txt) | Diretiva `Sitemap:` usa o mesmo placeholder — alinhar ao domínio real. |
| Rotas públicas | `/news`, `/news/:newsId`, `/plans`, `/plans/:planId` são acessíveis sem login na SPA; `GET /api/news` e `GET /api/plans` (listagem e detalhe) são **anônimos** na API. O fluxo **Assinar agora** exige login e redireciona para `/login?redirect=/plans/{planId}`. |
| Títulos de página | Valor padrão alinhado ao `<title>` do `index.html` em [`frontend/src/shared/seo.ts`](../frontend/src/shared/seo.ts) (`DEFAULT_DOCUMENT_TITLE`). |

---

*Documento gerado em 16/04/2026. Atualizar sempre que novas configurações forem adicionadas ao sistema.*
