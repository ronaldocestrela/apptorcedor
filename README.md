# App Torcedor

Monólito modular para **sócio torcedor** de um único clube (single tenant). A base inclui **Identity**, **JWT** (access + refresh), **permissões granulares** (claims `permission` + políticas na API), **CQRS (MediatR)** no projeto `AppTorcedor.Application`, **auditoria** e **configurações** administráveis, além da **SPA** em React (Vite).

## Pré-requisitos

| Ferramenta | Uso |
|------------|-----|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | API e testes |
| Node.js 20+ e npm | Frontend |
| Docker (opcional) | SQL Server local via `docker compose` |

## Estrutura do repositório

```text
backend/          → Solução .NET (API, Identity, Infrastructure, testes)
frontend/         → React + Vite (login, **cadastro e Minha conta (C.1)**, **notícias e benefícios (C.2)** no `/news` e `/benefits`, login Google opcional, convite staff, **usuários admin**, dashboard admin, **pagamentos admin (B.6)**, **notícias admin (B.9)**, **fidelidade/benefícios admin (B.10)**, **chamados/suporte admin (B.11)**, gestão de matriz de permissões, refresh de token)
docs/             → Documentação técnica por fase
docker-compose.yml → SQL Server opcional
AGENTS.md         → Visão de produto, regras e arquitetura alvo
```

## Uso rápido (desenvolvimento)

### 1. Backend (API)

No ambiente **Development** o padrão é **banco em memória** (não precisa de SQL Server para subir a API).

```bash
cd backend
dotnet restore
dotnet run --project src/AppTorcedor.Api/AppTorcedor.Api.csproj
```

- URL HTTP padrão: **http://localhost:5031** (definida em [`backend/src/AppTorcedor.Api/Properties/launchSettings.json`](backend/src/AppTorcedor.Api/Properties/launchSettings.json)).
- Documentação OpenAPI (somente Development): **GET** `http://localhost:5031/openapi/v1.json`.

### 2. Frontend (SPA)

Em outro terminal:

```bash
cd frontend
npm install
npm run dev
```

- A URL do Vite aparece no terminal (geralmente `http://localhost:5173`).
- A API é configurada pela variável **`VITE_API_URL`**. O arquivo [`frontend/.env.development`](frontend/.env.development) já aponta para `http://localhost:5031`. Para outro host/porta, ajuste ou copie de [`frontend/.env.example`](frontend/.env.example).

### 3. Fluxo na interface

1. Com a API e o frontend rodando, abra a URL do Vite.
2. Faça login com o e-mail e senha do seed (veja a seção **Credenciais iniciais** abaixo).
3. Após autenticar, você acessa a área logada (**/**, **/account**, **/news**, **/benefits**); usuários com permissões administrativas podem abrir **/admin** e sub-rotas (ver [docs/frontend/backoffice.md](docs/frontend/backoffice.md)).

## SQL Server com Docker (opcional)

Se quiser persistir dados em SQL Server em vez do banco em memória:

1. Suba o container:

   ```bash
   docker compose up -d
   ```

2. Em [`backend/src/AppTorcedor.Api/appsettings.Development.json`](backend/src/AppTorcedor.Api/appsettings.Development.json):
   - defina **`UseInMemoryDatabase`** como **`false`**;
   - confira se **`ConnectionStrings:DefaultConnection`** em [`appsettings.json`](backend/src/AppTorcedor.Api/appsettings.json) corresponde ao usuário/senha do `docker-compose.yml`.

3. Aplique as migrations na primeira vez (a partir da pasta `backend`):

   ```bash
   dotnet ef database update --project src/AppTorcedor.Infrastructure/AppTorcedor.Infrastructure.csproj --startup-project src/AppTorcedor.Api/AppTorcedor.Api.csproj
   ```

   É necessário ter a [CLI do EF](https://learn.microsoft.com/ef/core/cli/dotnet) instalada (`dotnet tool install --global dotnet-ef`).

## Credenciais iniciais (seed)

Na primeira execução, o sistema cria as **roles** (Administrador Master, Administrador, Financeiro, etc.) e o usuário administrador.

| Configuração | Descrição |
|--------------|-----------|
| `Seed:AdminMaster:Email` | E-mail do administrador (padrão em dev: `admin@torcedor.local`) |
| `Seed:AdminMaster:Password` | Senha em desenvolvimento ([`appsettings.Development.json`](backend/src/AppTorcedor.Api/appsettings.Development.json)) |
| `ADMIN_MASTER_INITIAL_PASSWORD` | Variável de ambiente; **use em produção** em vez de senha em arquivo |

Fora dos ambientes Development/Testing, a senha do seed **de** estar em configuração ou em `ADMIN_MASTER_INITIAL_PASSWORD`, caso contrário a aplicação falha na inicialização (comportamento intencional).

## Endpoints principais da API

| Método | Rota | Autenticação | Função |
|--------|------|--------------|--------|
| POST | `/api/auth/login` | Não | Retorna `accessToken`, `refreshToken`, `expiresIn`, `roles` |
| POST | `/api/auth/refresh` | Não | Troca o refresh por um novo par de tokens |
| POST | `/api/auth/logout` | Não | Revoga o refresh informado |
| GET | `/api/auth/me` | Bearer (JWT) | Dados do usuário, roles, **permissions** e `requiresProfileCompletion` |
| POST | `/api/auth/google` | Não | Login Google (`idToken` + consentimentos para novos usuários); retorno igual ao login |
| GET | `/api/account/register/requirements` | Não | IDs das versões publicadas de termos e privacidade (cadastro) |
| POST | `/api/account/register` | Não | Cadastro público (LGPD); retorno igual ao login |
| GET | `/api/account/profile` | Bearer | Perfil do torcedor (`UserProfile` sem nota admin) |
| PUT | `/api/account/profile` | Bearer | Atualiza perfil |
| POST | `/api/account/profile/photo` | Bearer | Upload multipart `file` (foto) |
| GET | `/api/diagnostics/admin-master-only` | Bearer + permissão `Administracao.Diagnostics` | Diagnóstico (via MediatR); JWT inclui claims `permission` conforme role |
| GET | `/health/live` | Não | Liveness (tag `live`) |
| GET | `/health/ready` | Não | Readiness (inclui checagem do banco; tag `ready`) |
| GET | `/api/admin/memberships` | Bearer + `Socios.Gerenciar` | Lista associações (filtros `status`, `userId`, paginação) |
| GET | `/api/admin/memberships/{id}` | Bearer + `Socios.Gerenciar` | Detalhe da associação (snapshot + usuário) |
| GET | `/api/admin/memberships/{id}/history` | Bearer + `Socios.Gerenciar` | Histórico operacional (`MembershipHistories`) |
| PATCH | `/api/admin/memberships/{id}/status` | Bearer + `Socios.Gerenciar` | Atualiza status com `reason` obrigatório (auditoria + histórico de domínio) |
| GET | `/api/admin/config` | Bearer + `Configuracoes.Visualizar` | Lista configurações |
| PUT | `/api/admin/config/{key}` | Bearer + `Configuracoes.Editar` | Cria/atualiza configuração (auditoria) |
| GET | `/api/admin/role-permissions` | Bearer + `Configuracoes.Visualizar` | Matriz role × permissão |
| GET | `/api/admin/audit-logs` | Bearer + `Configuracoes.Visualizar` | Consulta recente de auditoria (query `entityType`, `take`) |
| GET | `/api/admin/lgpd/documents` | Bearer + `Lgpd.Documentos.Visualizar` | Lista documentos legais e versão publicada |
| GET | `/api/admin/lgpd/documents/{id}` | Bearer + `Lgpd.Documentos.Visualizar` | Detalhe com todas as versões |
| POST | `/api/admin/lgpd/documents` | Bearer + `Lgpd.Documentos.Editar` | Cria documento (tipo único: termos / política) |
| POST | `/api/admin/lgpd/documents/{id}/versions` | Bearer + `Lgpd.Documentos.Editar` | Nova versão (rascunho) |
| POST | `/api/admin/lgpd/legal-document-versions/{versionId}/publish` | Bearer + `Lgpd.Documentos.Editar` | Publica versão |
| GET | `/api/admin/lgpd/users/{userId}/consents` | Bearer + `Lgpd.Consentimentos.Visualizar` | Lista consentimentos do usuário |
| POST | `/api/admin/lgpd/users/{userId}/consents` | Bearer + `Lgpd.Consentimentos.Registrar` | Registra aceite (versão publicada) |
| POST | `/api/admin/lgpd/users/{userId}/export` | Bearer + `Lgpd.Dados.Exportar` | Exporta JSON de dados da conta |
| POST | `/api/admin/lgpd/users/{userId}/anonymize` | Bearer + `Lgpd.Dados.Anonimizar` | Anonimiza PII e revoga refresh tokens |
| GET | `/api/admin/digital-cards` | Bearer + `Carteirinha.Visualizar` | Lista emissões da carteirinha (filtros `userId`, `membershipId`, `status`, paginação) |
| GET | `/api/admin/digital-cards/{id}` | Bearer + `Carteirinha.Visualizar` | Detalhe com `token` e linhas de preview do template fixo |
| POST | `/api/admin/digital-cards/issue` | Bearer + `Carteirinha.Gerenciar` | Emite carteirinha (`membershipId`; exige associação **Ativa** e sem emissão ativa) |
| POST | `/api/admin/digital-cards/{id}/regenerate` | Bearer + `Carteirinha.Gerenciar` | Regenera versão (invalida a ativa, novo token) |
| POST | `/api/admin/digital-cards/{id}/invalidate` | Bearer + `Carteirinha.Gerenciar` | Invalida emissão ativa (`reason` obrigatório) |

## Testes automatizados

```bash
# Backend (xUnit)
cd backend
dotnet test

# Frontend (Vitest)
cd frontend
npm test
```

## Configuração JWT (produção)

Em [`appsettings.json`](backend/src/AppTorcedor.Api/appsettings.json) (ou variáveis de ambiente), ajuste a seção **`Jwt`**:

- **`Key`**: chave simétrica com **pelo menos 32 bytes** em UTF-8.
- **`Issuer`** e **`Audience`**: devem bater com a validação configurada na API.

Não commite chaves reais; use secrets ou variáveis de ambiente no deploy.

## Documentação adicional

- [AGENTS.md](AGENTS.md) — escopo do produto, módulos e regras do projeto.
- [docs/architecture/auth-bootstrap.md](docs/architecture/auth-bootstrap.md) — decisões da fase de autenticação, detalhes técnicos e variáveis.
- [docs/architecture/parte-a-fundacao.md](docs/architecture/parte-a-fundacao.md) — Parte A: CQRS, permissões, auditoria, configurações e observabilidade.
- [docs/frontend/backoffice.md](docs/frontend/backoffice.md) — painel administrativo na SPA (rotas e permissões).
- [docs/ROADMAP-PENDENCIAS.md](docs/ROADMAP-PENDENCIAS.md) — backlog do que falta fazer, com prioridade para gestão e contratação de planos pelo torcedor ao final.
- [docs/architecture/parte-b2-lgpd.md](docs/architecture/parte-b2-lgpd.md) — Parte B.2: documentos legais, consentimentos, exportação e anonimização.
- [docs/architecture/parte-b7-digital-card-admin.md](docs/architecture/parte-b7-digital-card-admin.md) — Parte B.7: carteirinha digital (admin), versionamento e token.
- [docs/architecture/parte-b8-games-tickets-admin.md](docs/architecture/parte-b8-games-tickets-admin.md) — Parte B.8: jogos e ingressos (admin), `ITicketProvider` mock e fluxos de reserva/compra/sync/redeem.
