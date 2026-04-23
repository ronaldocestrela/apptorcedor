# Guia completo de deploy — App Torcedor

Este documento descreve, passo a passo, como colocar a aplicação em produção. Há **duas formas principais** alinhadas ao repositório:

1. **Jenkins + VPS** — após o **CI** verde no GitHub Actions, o job **`trigger-jenkins`** dispara o Jenkins; o pipeline grava **`api.env`** na VPS a partir do **Jenkins Credentials** e executa [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh) (**git pull** + **dotnet publish** + **npm build** na própria VPS); API servida por **systemd** + **.NET runtime** (fluxo no [`Jenkinsfile`](../../Jenkinsfile)).
2. **Docker Compose** — **API** e **SPA** em containers no mesmo host (ou orquestrador compatível); **SQL Server permanece em outro servidor**.

O **CI** roda no **GitHub Actions** ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)). O deploy via Jenkins é disparado **somente após** todos os jobs do workflow **`CI`** concluírem com **sucesso** em um **push** à branch **`single-tenant`** (job `trigger-jenkins`).

### CI local equivalente ao GitHub Actions

Para validar localmente os mesmos gates do workflow antes do push, execute na raiz do repositório:

```bash
./scripts/ci-local.sh
```

O script replica backend, frontend, validação de tooling e validação do Compose. Para apenas listar as etapas sem executar, use `./scripts/ci-local.sh --dry-run`. O escopo local não inclui `trigger-jenkins` nem deploy na VPS.

---

## 1. Visão geral da arquitetura

| Camada | Onde roda | Observação |
|--------|-----------|------------|
| SQL Server | **Servidor externo** | Connection string apenas em segredo (VPS ou `.env` do Compose). |
| API (.NET 10) | VPS (systemd) ou container `api` | JWT, webhooks, CORS, seed admin via variáveis de ambiente. |
| SPA (React/Vite) | Embutida em `wwwroot` da API (deploy Jenkins) ou container `web` (Compose) | `VITE_API_URL` é definida **no build**; deve ser a URL que o **navegador** usa. |

Endpoints úteis:

- `GET /health/live` — liveness (sem dependência de banco).
- `GET /health/ready` — readiness (inclui banco).

Arquivos de referência no repositório:

- [`Jenkinsfile`](../../Jenkinsfile)
- [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh)
- [`deploy/vps/deploy.sh`](../../deploy/vps/deploy.sh) (fluxo legado por tarball)
- [`deploy/vps/api.env.example`](../../deploy/vps/api.env.example)
- [`deploy/vps/apptorcedor-api.service.example`](../../deploy/vps/apptorcedor-api.service.example)
- [`docker-compose.yml`](../../docker-compose.yml)
- [`.env.compose.example`](../../.env.compose.example)

---

## 2. Pré-requisitos comuns

### 2.1 Banco de dados (SQL Server remoto)

1. Crie um banco (ou use um existente) e um usuário com permissão adequada.
2. Garanta conectividade de rede a partir de:
   - **VPS da API** (firewall / security group liberando a porta SQL, em geral **1433** ou a porta do seu provedor).
   - **Máquina do agente Jenkins** não precisa acessar o SQL no fluxo padrão (build ocorre na VPS). Migrations em geral rodam na subida da API na VPS.
3. Monte a **connection string** no formato esperado pelo EF Core / SQL Server, por exemplo:

   `Server=seu-host,1433;Database=AppTorcedor;User Id=app;Password=***;TrustServerCertificate=true;Encrypt=true`

   Ajuste `Encrypt`, certificados e timeout conforme a política do seu ambiente.

### 2.2 Migrations (primeira vez e upgrades)

A API aplica migrations pendentes **na subida** quando o banco é relacional (`Program.cs`). Para o **primeiro deploy**, o servidor precisa conseguir **conectar** ao banco com a mesma connection string de produção.

Alternativa operacional: rodar manualmente na pasta `backend`:

```bash
dotnet ef database update \
  --project src/AppTorcedor.Infrastructure/AppTorcedor.Infrastructure.csproj \
  --startup-project src/AppTorcedor.Api/AppTorcedor.Api.csproj \
  --connection "SUA_CONNECTION_STRING"
```

(use uma máquina com .NET SDK e ferramenta `dotnet-ef` instalada, e rede liberada até o SQL).

### 2.3 DNS e TLS (recomendado em produção)

- Defina hostnames públicos para a **API** (ex.: `api.clube.com.br`) e para o **site** (ex.: `app.clube.com.br`).
- Termine TLS no **reverse proxy** (Nginx, Caddy, Traefik, load balancer) com certificados válidos (Let’s Encrypt ou corporativo).
- O valor de **`Cors__AllowedOrigins__0`** (ou múltiplos índices) deve incluir a **origem exata** da SPA (ex.: `https://app.clube.com.br`).

---

## 3. Variáveis de ambiente (API — produção)

Defina no servidor (systemd `EnvironmentFile` ou `environment` do Compose). Em ASP.NET Core, seções do `appsettings` viram variáveis com **`__`** (duplo underscore).

| Variável | Obrigatório | Descrição |
|----------|-------------|-----------|
| `ConnectionStrings__DefaultConnection` | Sim | Connection string do SQL Server remoto. |
| `Jwt__Key` | Sim | Chave simétrica com **≥ 32 bytes UTF-8**. |
| `Jwt__Issuer` | Recomendado | Padrão no código: `AppTorcedor`. |
| `Jwt__Audience` | Recomendado | Padrão: `AppTorcedor`. |
| `ADMIN_MASTER_INITIAL_PASSWORD` | Sim (Production) | Senha inicial do usuário Administrador Master (seed). |
| `Payments__WebhookSecret` | Recomendado | Segredo do callback legacy `POST /api/subscriptions/payments/callback` (testes / integrações manuais). |
| `Payments__Provider` | Opcional | `Mock` (padrão) ou `Stripe`. Em produção com cartão real use `Stripe`. |
| `Payments__Stripe__ApiKey` | Se Stripe | Chave secreta `sk_live_…` ou `sk_test_…`. |
| `Payments__Stripe__WebhookSecret` | Se Stripe | Segredo do endpoint no Dashboard Stripe (`whsec_…`) para `POST /api/webhooks/stripe`. |
| `Payments__Stripe__SuccessUrl` | Se Stripe | URL HTTPS de sucesso após Checkout (ex. página de confirmação da SPA). |
| `Payments__Stripe__CancelUrl` | Se Stripe | URL HTTPS se o utilizador cancelar o Checkout. |
| `ASPNETCORE_ENVIRONMENT` | Sim | `Production`. |
| `ASPNETCORE_URLS` | Recomendado | Onde o Kestrel escuta (ex.: `http://127.0.0.1:5031` atrás do proxy). Alinhar com health check e proxy. |
| `Cors__AllowedOrigins__0` | Sim (se CORS restrito) | URL pública da SPA. Índices `__1`, `__2` para mais origens. |
| `Google__Auth__ClientId` | Opcional | Login Google na web. |
| `Seed__AdminMaster__Email` | Opcional | E-mail do master (há default no código). |

Modelo comentado: [`deploy/vps/api.env.example`](../../deploy/vps/api.env.example). No fluxo **Jenkins + VPS**, o conteúdo de produção é **gerado pelo Jenkins** a cada deploy, incluindo **Stripe** quando configurado nas credenciais (ver [§4.3](#43-credenciais-no-jenkins)).

### 3.1 Docker Compose (ficheiro `.env` na raiz do repo)

O [`docker-compose.yml`](../../docker-compose.yml) lê nomes curtos e injeta na API como `Payments__*`. Use o mesmo ficheiro [`.env.compose.example`](../../.env.compose.example) como modelo.

| Variável no `.env` | Vai para a API como | Quando |
|--------------------|---------------------|--------|
| `PAYMENTS_WEBHOOK_SECRET` | `Payments__WebhookSecret` | Sempre (callback legacy). |
| `PAYMENTS_PROVIDER` | `Payments__Provider` | `Mock` (omissão) ou `Stripe` para cartão real. |
| `STRIPE_API_KEY` | `Payments__Stripe__ApiKey` | Obrigatório se `PAYMENTS_PROVIDER=Stripe`. |
| `STRIPE_WEBHOOK_SECRET` | `Payments__Stripe__WebhookSecret` | Obrigatório se Stripe (`whsec_…` do Dashboard). |
| `STRIPE_SUCCESS_URL` | `Payments__Stripe__SuccessUrl` | Obrigatório se Stripe (HTTPS em produção). |
| `STRIPE_CANCEL_URL` | `Payments__Stripe__CancelUrl` | Obrigatório se Stripe. |

**Detalhes Stripe (Dashboard, webhook `POST /api/webhooks/stripe`, diferença entre os dois segredos):** [guia-configuracao-stripe.md](guia-configuracao-stripe.md).

**SPA — build time**

| Variável | Onde | Descrição |
|----------|------|-----------|
| `VITE_API_URL` | Credencial Jenkins (`vite-public-api-url`) ou `.env` do Compose | URL **pública** da API (ex.: `https://api.clube.com.br`). O navegador chama essa URL. |

---

## 4. Deploy com Jenkins e VPS (passo a passo)

### 4.1 Fluxo resumido

1. Push na branch configurada (padrão **`single-tenant`**).
2. GitHub Actions executa o workflow **`CI`** (backend, frontend, tooling, compose).
3. Se todos os jobs passarem, o job **`trigger-jenkins`** faz `POST` para o Jenkins (**build token** + **API token** do usuário Jenkins).
4. Jenkins: **checkout**, gera `api.env` (referência em `/etc/apptorcedor`) e, conforme o modo:
   - **`DEPLOY_USE_COMPOSE=true`** (padrão): gera também `.env` no formato do [`docker-compose.yml`](../../docker-compose.yml) e executa [`deploy/vps/build-and-deploy-compose.sh`](../../deploy/vps/build-and-deploy-compose.sh) — **`docker compose build`** e **`up -d`** (serviços `api` + `web`). Não usa `dotnet`/`npm` no host.
   - **`DEPLOY_USE_COMPOSE=false`**: fluxo legado com [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh) — `dotnet publish`, `npm run build`, `wwwroot`, `releases/`, **`systemctl`**.
   - **`JENKINS_LOCAL_DEPLOY=true`**: na mesma máquina — sem `ssh`; `WORKSPACE` como repo (Compose) ou como no fluxo systemd.
   - **`JENKINS_LOCAL_DEPLOY=false`**: **`scp`/`ssh`** com `vps-ssh-key` e `vps-host`; no remoto usa `VPS_REPO_DIR` quando não é o workspace.
5. Health check com **`curl`** em `APP_HEALTHCHECK_URL` (API exposta pelo mapeamento de portas do Compose, padrão `5031`).
6. Jenkins envia **commit status** ao GitHub (`jenkins/cd-vps`).

### 4.2 Secrets no GitHub (repositório)

Em **Settings → Secrets and variables → Actions**, configure:

| Secret | Uso |
|--------|-----|
| `JENKINS_URL` | URL base (ex.: `https://jenkins.exemplo.com`, sem `/` no final) |
| `JENKINS_JOB_NAME` | Nome do job (URL-encode se for `pasta/job`) |
| `JENKINS_BUILD_TOKEN` | Token de “Trigger builds remotely” |
| `JENKINS_USER` | Usuário Jenkins |
| `JENKINS_API_TOKEN` | API token desse usuário |

Sem esses secrets, o job `trigger-jenkins` falha no push — configure-os no repositório que dispara o CD.

### 4.3 Credenciais no Jenkins

Crie credenciais (IDs podem ser alterados no [`Jenkinsfile`](../../Jenkinsfile) se necessário):

| ID sugerido | Tipo | Conteúdo |
|-------------|------|-----------|
| `api-connection-string` | Secret text | `ConnectionStrings__DefaultConnection` |
| `api-jwt-key` | Secret text | `Jwt__Key` (≥ 32 bytes UTF-8) |
| `api-admin-password` | Secret text | `ADMIN_MASTER_INITIAL_PASSWORD` |
| `api-webhook-secret` | Secret text | `Payments__WebhookSecret` e, no Compose, `PAYMENTS_WEBHOOK_SECRET` (callback legacy; **não** é o `whsec` do Stripe) |
| `stripe-api-key` | Secret text | `STRIPE_API_KEY` → `Payments__Stripe__ApiKey`. Com só **Mock**, crie credencial **Secret text vazio** (obrigatório existir o ID). |
| `stripe-webhook-secret` | Secret text | `STRIPE_WEBHOOK_SECRET` → `Payments__Stripe__WebhookSecret` (`whsec_…`). Vazio se só Mock. |
| `payments-provider` | Secret text | `PAYMENTS_PROVIDER` no Compose — texto **`Mock`** ou **`Stripe`**. |
| `stripe-success-url` | Secret text | `STRIPE_SUCCESS_URL` — URL HTTPS da SPA após Checkout (ex.: `…/subscription/confirmation`). Vazio se ainda não usar. |
| `stripe-cancel-url` | Secret text | `STRIPE_CANCEL_URL` — URL HTTPS ao cancelar Checkout (ex.: `…/plans`). Vazio se ainda não usar. |
| `support-ticket-attachments-provider` | Secret text | `SupportTicketAttachments__Provider` (use `Local` ou `Cloudinary`). |
| `cloudinary-cloud-name` | Secret text | `Cloudinary__CloudName`. |
| `cloudinary-api-key` | Secret text | `Cloudinary__ApiKey`. |
| `cloudinary-api-secret` | Secret text | `Cloudinary__ApiSecret`. |
| `email-provider` | Secret text | `EMAIL_PROVIDER` — **`Mock`** ou **`Resend`**. |
| `resend-api-key` | Secret text | `RESEND_API_KEY` → `Email__Resend__ApiKey`. Vazio se só Mock. |
| `resend-from-address` | Secret text | `RESEND_FROM_ADDRESS` → `Email__Resend__FromAddress` (domínio verificado no Resend). |
| `resend-from-name` | Secret text | `RESEND_FROM_NAME` → `Email__Resend__FromName` (opcional). |
| `api-cors-origin` | Secret text | `Cors__AllowedOrigins__0` |
| `api-aspnetcore-urls` | Secret text | `ASPNETCORE_URLS` (ex.: `http://127.0.0.1:5031`) |
| `vite-public-api-url` | Secret text | `VITE_API_URL` (build do Vite na VPS) |
| `github-token-deploy-status` | Secret text | PAT com escopo **`repo:status`** |
| `vps-ssh-key` | SSH Username with private key | Só se **`JENKINS_LOCAL_DEPLOY=false`**: utilizador + chave privada para `scp`/`ssh` |
| `vps-host` | Secret text | Só se **`JENKINS_LOCAL_DEPLOY=false`**: hostname ou IP (sem `https://`) |

As variáveis do [`docker-compose.yml`](../../docker-compose.yml) (`PAYMENTS_WEBHOOK_SECRET`, `PAYMENTS_PROVIDER`, `STRIPE_*`, `EMAIL_PROVIDER`, `RESEND_*`) são preenchidas pelo pipeline a partir das credenciais da tabela acima (`api-webhook-secret`, `stripe-api-key`, `stripe-webhook-secret`, **`payments-provider`**, **`stripe-success-url`**, **`stripe-cancel-url`**, **`email-provider`**, **`resend-api-key`**, **`resend-from-address`**, **`resend-from-name`**). Detalhes Stripe: [guia-configuracao-stripe.md](guia-configuracao-stripe.md). E-mail Resend: [../architecture/parte-e1-email-resend.md](../architecture/parte-e1-email-resend.md).

Com **`JENKINS_LOCAL_DEPLOY=true`** (Jenkins na mesma VPS), **não** são necessárias `vps-ssh-key` nem `vps-host`. Para deploy remoto por SSH, gera e configura chaves conforme [chave-ssh-gerar-e-configurar.md](chave-ssh-gerar-e-configurar.md).

### 4.4 Variáveis de job Jenkins (opcionais)

Configuráveis como variáveis de ambiente do job ou do folder:

| Variável | Padrão | Uso |
|----------|--------|-----|
| `DEPLOY_BRANCH` | `single-tenant` | Só executa deploy se a branch do job for esta |
| `DEPLOY_ROOT` | `/opt/apptorcedor` | Raiz: `releases/<id>` e symlink `current` |
| `JENKINS_LOCAL_DEPLOY` | `true` | `true` = deploy na mesma máquina (usa `WORKSPACE` como repo git); `false` = `scp`/`ssh` e `VPS_REPO_DIR` |
| `DEPLOY_USE_COMPOSE` | `true` | `true` = **Docker Compose** ([`build-and-deploy-compose.sh`](../../deploy/vps/build-and-deploy-compose.sh)); `false` = systemd + publish no host ([`build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh)). |
| `COMPOSE_FILE` | `docker-compose.yml` | Ficheiro Compose na raiz do repositório. |
| `API_PORT` / `WEB_PORT` | `5031` / `5173` | Publicação no host (`docker-compose.yml`). |
| `NODEJS_HOME` | *(vazio)* | Só **`DEPLOY_USE_COMPOSE=false`**: PATH para npm/dotnet sob `sudo`. |
| `VPS_REPO_DIR` | `/opt/apptorcedor/repo` | Usado com **`JENKINS_LOCAL_DEPLOY=false`** no deploy remoto. |
| `APP_SERVICE_NAME` | `apptorcedor-api` | Só fluxo **systemd** (`DEPLOY_USE_COMPOSE=false`) |
| `APP_HEALTHCHECK_URL` | `http://127.0.0.1:5031/health/live` | Liveness após restart |
| `VPS_PORT` | `22` | Porta SSH (só fluxo remoto) |

### 4.5 Agente Jenkins

**Com `DEPLOY_USE_COMPOSE=true` e `JENKINS_LOCAL_DEPLOY=true`:** **Docker Engine** + plugin **`docker compose`**, **git**, **curl**, **Python 3** (status GitHub), **sudo** para gravar `/etc/apptorcedor/api.env` e para `docker` se o user Jenkins não estiver no grupo `docker`. Não é necessário .NET SDK nem Node no host.

**Com `DEPLOY_USE_COMPOSE=false` e `JENKINS_LOCAL_DEPLOY=true`:** **.NET SDK 10**, **Node 22+**, **npm**, **git**, **curl**, **Python 3**, **sudo** (ver [4.6](#46-preparar-a-vps-uma-vez)).

**Com `JENKINS_LOCAL_DEPLOY=false`:** **OpenSSH** no agente; o build corre na VPS remota.

### 4.6 Preparar a VPS (uma vez)

Execute como root ou com `sudo` onde indicado.

**4.6.1 Docker (deploy Compose — padrão Jenkins)**

Instale **Docker Engine** e o plugin **Compose** (`docker compose version`). Coloque o utilizador do Jenkins no grupo **`docker`** *ou* conceda **`sudo NOPASSWD`** para `docker` (e opcionalmente os mesmos `mkdir`/`install` de `/etc`). O [`docker-compose.yml`](../../docker-compose.yml) faz **build** das imagens a partir de [`backend/Dockerfile`](../../backend/Dockerfile) e [`frontend/Dockerfile`](../../frontend/Dockerfile).

**4.6.1b .NET SDK e Node (só fluxo `DEPLOY_USE_COMPOSE=false`)**

Instale **.NET SDK 10.x**, **Node.js 22+**, **npm**, **git** e **curl** na VPS onde corre o publish nativo.

**4.6.2 Clone do repositório**

- **`JENKINS_LOCAL_DEPLOY=true`:** o pipeline usa o **workspace** do job como repositório (`git pull` no checkout do Jenkins). Garante permissão de **`git pull`** (credencial Git no Jenkins ou acesso HTTPS/SSH ao remoto).
- **`JENKINS_LOCAL_DEPLOY=false`:** mantém um clone em `VPS_REPO_DIR` (padrão `/opt/apptorcedor/repo`) na VPS remota, com **`git pull`** (deploy key ou HTTPS).

**4.6.3 Diretórios de release (fluxo systemd)**

```bash
sudo mkdir -p /opt/apptorcedor/releases
sudo chown root:root /opt/apptorcedor
```

Com **`DEPLOY_USE_COMPOSE=false`**, o deploy cria releases em `releases/` e o symlink `/opt/apptorcedor/current`. Com **Compose**, as imagens são reconstruídas no diretório do repo; não usa essa árvore de releases.

**4.6.4 Segredos da API**

No fluxo atual, **`/etc/apptorcedor/api.env`** é criado/atualizado pelo Jenkins a cada deploy (`sudo install`). Não é obrigatório preencher manualmente antes do primeiro deploy; mantenha apenas o diretório pai criado se desejar (`sudo mkdir -p /etc/apptorcedor`).

**4.6.5 Systemd (só `DEPLOY_USE_COMPOSE=false`)**

Copie e ajuste o exemplo:

```bash
sudo cp deploy/vps/apptorcedor-api.service.example /etc/systemd/system/apptorcedor-api.service
sudo nano /etc/systemd/system/apptorcedor-api.service
```

Confira `User`, `WorkingDirectory`, `ExecStart` e `EnvironmentFile`. O `WorkingDirectory` deve ser o diretório que contém `AppTorcedor.Api.dll` — `/opt/apptorcedor/current`.

```bash
sudo systemctl daemon-reload
sudo systemctl enable apptorcedor-api
```

Na **primeira** vez, o serviço pode falhar até existir `current` e DLL; após o primeiro deploy Jenkins, o symlink será preenchido.

**4.6.6 Sudo para o usuário de deploy**

Com **`JENKINS_LOCAL_DEPLOY=false`**, o pipeline faz `ssh` com `sudo install ... api.env` e `sudo bash` no script de deploy. Com **`JENKINS_LOCAL_DEPLOY=true`**, o mesmo ocorre no agente local.

- **Compose (`DEPLOY_USE_COMPOSE=true`):** o script é `/tmp/apptorcedor-build-deploy-compose-*.sh` (nome gerado pelo Jenkins) e chama [`build-and-deploy-compose.sh`](../../deploy/vps/build-and-deploy-compose.sh). Se o Jenkins não estiver no grupo `docker`, inclua **`/usr/bin/docker`** (ou prefira `usermod -aG docker jenkins` e não use `sudo docker`).
- **Systemd (`DEPLOY_USE_COMPOSE=false`):** `apptorcedor-build-deploy-*.sh` e `systemctl restart`.

Exemplo de entrada no `sudoers` (ajuste utilizador e valide com `visudo`):

```text
deployuser ALL=(root) NOPASSWD: /usr/bin/install, /bin/bash /tmp/apptorcedor-build-deploy-compose-*.sh, /bin/bash /tmp/apptorcedor-build-deploy-*.sh, /bin/mkdir, /usr/bin/docker, /bin/systemctl restart apptorcedor-api
```

(Ajuste caminhos exatos conforme a distro; inclua apenas as linhas que o seu fluxo usa.)

**4.6.7 Persistência de uploads**

Cada release substitui o conteúdo publicado; diretórios de upload devem **persistir fora** do diretório versionado ou usar storage externo / volume em `wwwroot/uploads`.

**4.6.8 Storage em Cloudinary (opcional, recomendado para produção)**

Para eliminar dependência de disco local em uploads de foto/anexos:

- `ProfilePhotos__Provider=Cloudinary`
- `SupportTicketAttachments__Provider=Cloudinary`
- `Cloudinary__CloudName=<cloud_name>`
- `Cloudinary__ApiKey=<api_key>`
- `Cloudinary__ApiSecret=<api_secret>`
- opcional: `ProfilePhotos__Cloudinary__Folder=profile-photos`
- opcional: `SupportTicketAttachments__Cloudinary__Folder=support-attachments`

Com provider `Local`, os fluxos continuam funcionando como antes. Isso permite rollout gradual por ambiente e rollback rápido apenas trocando configuração.

No Jenkins, os campos `SupportTicketAttachments__Provider`, `Cloudinary__CloudName`, `Cloudinary__ApiKey` e `Cloudinary__ApiSecret` são preenchidos por credenciais dedicadas (`support-ticket-attachments-provider`, `cloudinary-cloud-name`, `cloudinary-api-key`, `cloudinary-api-secret`) e gravados automaticamente no `api.env` de deploy.

**4.6.9 Estratégia recomendada de migração (sem lote)**

Para a migração em produção sem copiar arquivos legados em massa:

1. habilite Cloudinary primeiro em staging;
2. valide upload/troca de foto (`/api/account/profile/photo`) e anexos (`/api/support/tickets`);
3. promova para produção com os mesmos envs;
4. mantenha registros legados com URL local (`/uploads/...`) coexistindo com novos registros Cloudinary;
5. em caso de incidente, faça rollback apenas alterando `ProfilePhotos__Provider` e `SupportTicketAttachments__Provider` para `Local`.

Observação: na estratégia sem lote, fotos/anexos antigos locais continuam válidos enquanto os novos passam a ser gravados no Cloudinary.

### 4.7 Criar o job no Jenkins

1. Novo item → **Pipeline** (ou **Multibranch Pipeline** apontando para o GitHub).
2. Definição: **Pipeline script from SCM**; SCM Git; branch `single-tenant` (ou a configurada em `DEPLOY_BRANCH`); script path **`Jenkinsfile`**.
3. Marque **Trigger builds remotely** e defina o token (mesmo valor do secret `JENKINS_BUILD_TOKEN` no GitHub).
4. Associe as credenciais com os IDs esperados (ou edite o `Jenkinsfile`).

O disparo após push é feito pelo GitHub Actions (`trigger-jenkins`); não é obrigatório webhook GitHub → Jenkins separado.

### 4.8 Primeiro deploy

1. Configure secrets no GitHub e credenciais no Jenkins.
2. Faça push para `single-tenant` com CI verde ou execute o job Jenkins manualmente (mesma branch).
3. Acompanhe logs: scp → deploy remoto → status no GitHub.
4. Na VPS: com **Compose**, `docker compose ps` e `docker compose logs -f api`; com **systemd**, `sudo systemctl status apptorcedor-api` e `journalctl -u apptorcedor-api -f`.
5. Teste `curl -sS` em `APP_HEALTHCHECK_URL` (ex.: `http://127.0.0.1:5031/health/live` alinhado a `API_PORT`).
6. Se Cloudinary estiver ativo, valide também:
   - upload de foto em `/api/account/profile/photo`;
   - abertura de chamado com anexo em `/api/support/tickets`;
   - download do anexo por torcedor e por staff.

### 4.9 Reverse proxy em frente à API (ex.: Nginx)

O Kestrel pode escutar só em `127.0.0.1:5031`; o Nginx termina TLS e faz `proxy_pass` para essa porta. Cabeçalhos úteis: `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`. Configure `ForwardedHeaders` na API se necessário para links e cookies em cenários avançados.

### 4.10 Rollback

**Systemd:** o [`build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh) (e [`deploy.sh`](../../deploy/vps/deploy.sh)) pode restaurar o symlink `current` para a release anterior se o health check falhar. Rollback manual:

```bash
sudo ls -la /opt/apptorcedor/releases
sudo ln -sfn /opt/apptorcedor/releases/<release_anterior> /opt/apptorcedor/current
sudo systemctl restart apptorcedor-api
```

**Compose:** faça `git checkout` no commit anterior no clone usado pelo deploy, rode de novo o job Jenkins ou execute manualmente `docker compose build && docker compose up -d` no diretório do repo (ou mantenha tags de imagem versionadas e `docker compose up` com tag fixa).

---

## 5. Deploy com Docker Compose (passo a passo manual)

Indicado quando você quer **API + web (SPA)** no mesmo host Docker, com SQL remoto — mesmo stack que o Jenkins usa quando **`DEPLOY_USE_COMPOSE=true`** (ver [§4](#4-deploy-com-jenkins-e-vps-passo-a-passo)).

### 5.1 No servidor

1. Instale Docker Engine e plugin Compose v2.
2. Clone o repositório (ou copie `backend/`, `frontend/`, `docker-compose.yml`, `.env.compose.example`).
3. Copie o exemplo de ambiente:

   ```bash
   cp .env.compose.example .env
   ```

4. Edite `.env`:
   - `DATABASE_CONNECTION_STRING` apontando para o **SQL remoto** acessível **de dentro do container** da API (IP público, hostname interno da VPC, etc.).
   - `JWT_KEY`, `ADMIN_MASTER_INITIAL_PASSWORD`, etc.
   - `VITE_API_URL`: URL que o **navegador** usará para chamar a API (ex.: `https://api.clube.com.br` ou `http://IP:5031` em teste).
   - `CORS_ORIGIN`: origem da SPA (ex.: `https://app.clube.com.br`).

5. Suba:

   ```bash
   docker compose --env-file .env up --build -d
   ```

6. Verifique: `docker compose ps`, logs `docker compose logs api -f`, e `curl` nos health checks nas portas publicadas (`API_PORT` / `WEB_PORT`, padrão 5031 e 5173).

### 5.2 TLS e portas em produção

Em geral você coloca um reverse proxy na frente das portas publicadas ou publica só redes internas e expõe 443 no proxy. Ajuste `CORS_ORIGIN` e `VITE_API_URL` para os hostnames finais.

### 5.3 Dados persistentes

- **Banco**: já está fora do Compose.
- **Uploads em `wwwroot`**: use volume Docker montado no caminho de uploads da API ou configure storage externo; senão, dados podem ser perdidos ao recriar o container.
- **Cloudinary**: ao usar provider Cloudinary para fotos/anexos, o risco de perda por recriação de container é removido para esses ativos.

---

## 6. Webhooks de pagamento (D.4)

| Fluxo | Rota | Segredo na app | Uso |
|--------|------|----------------|-----|
| **Callback legacy** | `POST /api/subscriptions/payments/callback` | `Payments__WebhookSecret` | Corpo JSON `{ "paymentId", "secret" }`; testes e integrações manuais; **não** substitui o webhook assinado do Stripe. |
| **Stripe (recomendado com `Provider=Stripe`)** | `POST /api/webhooks/stripe` | `Payments__Stripe__WebhookSecret` (`whsec_…`) | Corpo bruto + cabeçalho `Stripe-Signature`; evento mínimo **`checkout.session.completed`**. |

- A URL do webhook no Dashboard Stripe deve ser **`https://<host-público-da-API>/api/webhooks/stripe`** — **sem** sufixos extra (ex.: `/member/{id}` não existe na API).
- `Payments__WebhookSecret` e `Payments__Stripe__WebhookSecret` são **dois valores diferentes**; não os misture.

Não commite segredos no Git.

**Guia detalhado (Stripe Dashboard + variáveis da app):** [guia-configuracao-stripe.md](guia-configuracao-stripe.md).

---

## 7. Checklist antes de ir a produção

- [ ] SQL Server acessível da API; usuário e banco criados.
- [ ] Migrations aplicadas (automático na subida ou job manual).
- [ ] `Jwt__Key` forte (≥ 32 bytes) e armazenada só como segredo.
- [ ] `ADMIN_MASTER_INITIAL_PASSWORD` definida e política de troca de senha definida para o master.
- [ ] `Cors__AllowedOrigins__*` com URLs reais da SPA.
- [ ] `VITE_API_URL` no build apontando para a API pública correta.
- [ ] TLS ativo no proxy; HTTP redirecionado para HTTPS se aplicável.
- [ ] `GET /health/live` e `GET /health/ready` OK.
- [ ] Se usar Cloudinary: `ProfilePhotos__Provider` e `SupportTicketAttachments__Provider` definidos para `Cloudinary` + credenciais válidas (`Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`).
- [ ] Se migração sem lote: confirmar coexistência de URLs locais (`/uploads/...`) e URLs Cloudinary nos fluxos de perfil e suporte.
- [ ] Com **Stripe:** `Payments__Provider=Stripe` (ou `PAYMENTS_PROVIDER=Stripe` no `.env` do Compose), `Payments__Stripe__ApiKey`, `Payments__Stripe__WebhookSecret`, `SuccessUrl` / `CancelUrl`, webhook no Dashboard em `https://<API>/api/webhooks/stripe`, migração `ProcessedStripeWebhookEvents` aplicada. Ver [guia-configuracao-stripe.md](guia-configuracao-stripe.md).
- [ ] Estratégia de backup do banco e de arquivos de upload definida.
- [ ] GitHub: secrets `JENKINS_*` para o job `trigger-jenkins`; Jenkins: credenciais de API + PAT `repo:status`; se deploy remoto (`JENKINS_LOCAL_DEPLOY=false`), credenciais SSH + `vps-host`.

---

## 8. Troubleshooting

| Problema | O que verificar |
|----------|-------------------|
| `trigger-jenkins` falha no GitHub | Secrets `JENKINS_URL`, `JENKINS_JOB_NAME`, tokens e usuário corretos; job com “Trigger builds remotely” habilitado. |
| Status `jenkins/cd-vps` não aparece | PAT `github-token-deploy-status` sem `repo:status`; commit/owner divergentes. |
| API não sobe após deploy | `journalctl -u apptorcedor-api -xe`; connection string; firewall até o SQL; JWT curto. |
| Health falha | Porta em `ASPNETCORE_URLS` vs `APP_HEALTHCHECK_URL`; proxy ocupando porta. |
| Rollback em loop | Erro na aplicação (DB, config). Corrija credenciais no Jenkins (geram `api.env`) e redeploy. |
| SPA chama API errada | Rebuild com `VITE_API_URL` correto; limpar cache do navegador. |
| CORS no navegador | Origem da SPA exata em `Cors__AllowedOrigins__0`; HTTPS vs HTTP. |
|502 no proxy | Upstream Kestrel parado; `proxy_pass` para host/porta corretos. |

Documentação relacionada: [chave-ssh-gerar-e-configurar.md](chave-ssh-gerar-e-configurar.md) (gerar e configurar SSH para deploy), [parte-f1-jenkins-cd-pos-ci.md](../architecture/parte-f1-jenkins-cd-pos-ci.md) (visão técnica do Jenkins), [README.md](../../README.md) (visão geral do projeto).
