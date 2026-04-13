# Guia completo de deploy — App Torcedor

Este documento descreve, passo a passo, como colocar a aplicação em produção. Há **duas formas principais** alinhadas ao repositório:

1. **Jenkins + VPS** — após o **CI** verde no GitHub Actions, o job **`trigger-jenkins`** dispara o Jenkins; o pipeline grava **`api.env`** na VPS a partir do **Jenkins Credentials** e executa [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh) (**git pull** + **dotnet publish** + **npm build** na própria VPS); API servida por **systemd** + **.NET runtime** (fluxo no [`Jenkinsfile`](../../Jenkinsfile)).
2. **Docker Compose** — **API** e **SPA** em containers no mesmo host (ou orquestrador compatível); **SQL Server permanece em outro servidor**.

O **CI** roda no **GitHub Actions** ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)). O deploy via Jenkins é disparado **somente após** todos os jobs do workflow **`CI`** concluírem com **sucesso** em um **push** à branch **`single-tenant`** (job `trigger-jenkins`).

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
| `Payments__WebhookSecret` | Recomendado | Segredo do callback de pagamento (`POST` de webhook). |
| `ASPNETCORE_ENVIRONMENT` | Sim | `Production`. |
| `ASPNETCORE_URLS` | Recomendado | Onde o Kestrel escuta (ex.: `http://127.0.0.1:5031` atrás do proxy). Alinhar com health check e proxy. |
| `Cors__AllowedOrigins__0` | Sim (se CORS restrito) | URL pública da SPA. Índices `__1`, `__2` para mais origens. |
| `Google__Auth__ClientId` | Opcional | Login Google na web. |
| `Seed__AdminMaster__Email` | Opcional | E-mail do master (há default no código). |

Modelo comentado: [`deploy/vps/api.env.example`](../../deploy/vps/api.env.example). No fluxo **Jenkins + VPS**, o conteúdo de produção é **gerado pelo Jenkins** a cada deploy (não é necessário manter `api.env` manual na VPS).

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
4. Jenkins: **checkout** leve (para `GIT_COMMIT` / `GITHUB_REPOSITORY`), **scp** de `api.env` gerado + arquivo com `VITE_API_URL` + [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh).
5. Na VPS: `sudo install` grava `/etc/apptorcedor/api.env`; o script faz **`git pull`** no clone do repositório, **`dotnet publish`**, **`npm ci` / `npm run build`**, copia o SPA para `wwwroot`, atualiza `releases/<id>`, symlink **`current`**, **`systemctl restart`**, **`curl`** em `/health/live`.
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
| `api-webhook-secret` | Secret text | `Payments__WebhookSecret` |
| `api-cors-origin` | Secret text | `Cors__AllowedOrigins__0` |
| `api-aspnetcore-urls` | Secret text | `ASPNETCORE_URLS` (ex.: `http://127.0.0.1:5031`) |
| `vite-public-api-url` | Secret text | `VITE_API_URL` (build do Vite na VPS) |
| `github-token-deploy-status` | Secret text | PAT com escopo **`repo:status`** |
| `vps-ssh-key` | SSH Username with private key | Usuário SSH + chave privada para a VPS |
| `vps-host` | Secret text | Hostname ou IP da VPS |

### 4.4 Variáveis de job Jenkins (opcionais)

Configuráveis como variáveis de ambiente do job ou do folder:

| Variável | Padrão | Uso |
|----------|--------|-----|
| `DEPLOY_BRANCH` | `single-tenant` | Só executa deploy se a branch do job for esta |
| `DEPLOY_ROOT` | `/opt/apptorcedor` | Raiz: `releases/<id>` e symlink `current` |
| `VPS_REPO_DIR` | `/opt/apptorcedor/repo` | Diretório do **clone git** na VPS |
| `APP_SERVICE_NAME` | `apptorcedor-api` | Unidade systemd |
| `APP_HEALTHCHECK_URL` | `http://127.0.0.1:5031/health/live` | Liveness após restart |
| `VPS_PORT` | `22` | Porta SSH |

### 4.5 Agente Jenkins

O agente precisa apenas de:

- **OpenSSH client** (`ssh`, `scp`)
- **Python 3** (status no GitHub no `post`)
- **Git** (checkout SCM)

**Não** é necessário .NET SDK nem Node no agente: o build ocorre na VPS.

### 4.6 Preparar a VPS (uma vez)

Execute como root ou com `sudo` onde indicado.

**4.6.1 .NET SDK e Node na VPS**

Instale **.NET SDK 10.x** (para `dotnet publish`), **Node.js 22+**, **npm**, **git** e **curl**.

**4.6.2 Clone do repositório**

Clone o repositório em `VPS_REPO_DIR` (padrão `/opt/apptorcedor/repo`), com permissão de **`git pull`** (deploy key ou credencial HTTPS).

**4.6.3 Diretórios de release**

```bash
sudo mkdir -p /opt/apptorcedor/releases
sudo chown root:root /opt/apptorcedor
```

O deploy cria releases dentro de `releases/` e aponta o symlink `/opt/apptorcedor/current` para a versão ativa.

**4.6.4 Segredos da API**

No fluxo atual, **`/etc/apptorcedor/api.env`** é criado/atualizado pelo Jenkins a cada deploy (`sudo install`). Não é obrigatório preencher manualmente antes do primeiro deploy; mantenha apenas o diretório pai criado se desejar (`sudo mkdir -p /etc/apptorcedor`).

**4.6.5 Systemd**

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

O pipeline faz `ssh` com `sudo install ... api.env` e `sudo bash /tmp/apptorcedor-build-deploy-*.sh`. O usuário da chave SSH precisa poder executar esses comandos (política restrita em `sudoers`) e o script interno reinicia o serviço com `systemctl`.

Exemplo de entrada no `sudoers` (ajuste usuário e valide com `visudo`):

```text
deployuser ALL=(root) NOPASSWD: /usr/bin/install, /bin/bash /tmp/apptorcedor-build-deploy-*.sh, /bin/mkdir, /bin/systemctl restart apptorcedor-api
```

(Ajuste caminhos exatos conforme a sua distro; inclua `/bin/mkdir` se necessário para `mkdir -p /etc/apptorcedor`.)

**4.6.7 Persistência de uploads**

Cada release substitui o conteúdo publicado; diretórios de upload devem **persistir fora** do diretório versionado ou usar storage externo / volume em `wwwroot/uploads`.

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
4. Na VPS: `sudo systemctl status apptorcedor-api` e `journalctl -u apptorcedor-api -f`.
5. Teste `curl -sS http://127.0.0.1:5031/health/live` (ou URL configurada).

### 4.9 Reverse proxy em frente à API (ex.: Nginx)

O Kestrel pode escutar só em `127.0.0.1:5031`; o Nginx termina TLS e faz `proxy_pass` para essa porta. Cabeçalhos úteis: `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`. Configure `ForwardedHeaders` na API se necessário para links e cookies em cenários avançados.

### 4.10 Rollback

O [`deploy/vps/build-and-deploy.sh`](../../deploy/vps/build-and-deploy.sh) (e o fluxo legado [`deploy/vps/deploy.sh`](../../deploy/vps/deploy.sh)) restaura o symlink `current` para a release anterior se o health check falhar após o restart. Para rollback manual:

```bash
sudo ls -la /opt/apptorcedor/releases
sudo ln -sfn /opt/apptorcedor/releases/<release_anterior> /opt/apptorcedor/current
sudo systemctl restart apptorcedor-api
```

---

## 5. Deploy com Docker Compose (passo a passo)

Indicado quando você quer **API + nginx (SPA)** no mesmo host Docker, com SQL remoto.

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

---

## 6. Webhook de pagamento (D.4)

O callback usa o segredo `Payments__WebhookSecret`. O provedor de pagamento deve chamar a URL pública configurada na aplicação; o valor do segredo na VPS / `.env` deve ser o **mesmo** configurado no provedor. Não commite o segredo no Git.

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
- [ ] Estratégia de backup do banco e de arquivos de upload definida.
- [ ] GitHub: secrets `JENKINS_*` para o job `trigger-jenkins`; Jenkins: credenciais de API + PAT `repo:status` + chave SSH só para deploy.

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

Documentação relacionada: [parte-f1-jenkins-cd-pos-ci.md](../architecture/parte-f1-jenkins-cd-pos-ci.md) (visão técnica do Jenkins), [README.md](../../README.md) (visão geral do projeto).
