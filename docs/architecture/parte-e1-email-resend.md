# E.1 — E-mail transacional (Mock / Resend)

## Objetivo

Fornecer uma **porta única** (`IEmailSender`) para envio de e-mails transacionais no backend, com **provedor configurável**: desenvolvimento/testes usam **Mock** (apenas log); produção pode usar **Resend** (API HTTP oficial).

## Componentes

| Peça | Projeto | Função |
|------|---------|--------|
| `IEmailSender`, `EmailMessage` | `AppTorcedor.Application` (`Abstractions`) | Contrato e DTO de mensagem (To, Subject, HtmlBody, PlainText opcional). |
| `EmailOptions`, `EmailResendOptions` | `AppTorcedor.Infrastructure` (`Options`) | `Email:Provider`, `Email:Resend:*`. |
| `MockEmailSender` | `AppTorcedor.Infrastructure` (`Services/Email`) | Não chama rede; regista `Information` com destino e assunto. |
| `ResendEmailSender` | `AppTorcedor.Infrastructure` (`Services/Email`) | Mapeia para `Resend.EmailMessage` e chama `IResend.EmailSendAsync`. |
| Pacote NuGet | `Resend` | Cliente `ResendClient` + `IResend`. |

## Configuração (ASP.NET Core)

```json
"Email": {
  "Provider": "Mock",
  "Resend": {
    "ApiKey": "",
    "FromAddress": "",
    "FromName": ""
  }
}
```

- **`Provider=Resend`**: `Email:Resend:ApiKey` é **obrigatória** na subida da app (senão `InvalidOperationException` no `AddInfrastructure`).
- No envio real, **`FromAddress`** deve estar preenchido (domínio/remetente verificado no painel Resend); caso contrário `InvalidOperationException` em `SendAsync`.

Variáveis de ambiente (duplo underscore), alinhadas ao [`docker-compose.yml`](../../docker-compose.yml):

- `Email__Provider`, `Email__Resend__ApiKey`, `Email__Resend__FromAddress`, `Email__Resend__FromName`
- No `.env` do Compose: `EMAIL_PROVIDER`, `RESEND_API_KEY`, `RESEND_FROM_ADDRESS`, `RESEND_FROM_NAME` (ver [`.env.compose.example`](../../.env.compose.example)).

## Docker Compose e Jenkins

O [`Jenkinsfile`](../../Jenkinsfile) materializa as mesmas chaves em `/etc/apptorcedor/api.env` e no ficheiro temporário usado pelo Compose, a partir destas credenciais:

| ID Jenkins | Uso |
|------------|-----|
| `email-provider` | `Mock` ou `Resend` |
| `resend-api-key` | API key (vazio se só Mock) |
| `resend-from-address` | Remetente verificado |
| `resend-from-name` | Nome exibido (opcional) |

Referência também em [`deploy/vps/api.env.example`](../../deploy/vps/api.env.example) e na tabela de credenciais em [`docs/deploy/guia-deploy.md`](../deploy/guia-deploy.md).

## Testes

- `AppTorcedor.Infrastructure.Tests/Services/Email/MockEmailSenderTests.cs`
- `AppTorcedor.Infrastructure.Tests/Services/Email/ResendEmailSenderTests.cs` (mock de `IResend`)
- `AppTorcedor.Infrastructure.Tests/TorcedorAccountServiceRegisterTests.cs` — boas-vindas após `RegisterAsync` / `RegisterGoogleUserAsync` e resiliência quando o envio falha
- `AppTorcedor.Infrastructure.Tests/Services/Email/WelcomeEmailComposerTests.cs` — placeholders, banner, rejeição de HTML suspeito, fallback
- `AppTorcedor.Api.Tests/PartAGovernanceAndObservabilityTests.cs` — persistência das chaves `Email.Welcome.*` via `PUT /api/admin/config/{key}`

Os testes de API fixam `Email:Provider=Mock` em [`AppWebApplicationFactory`](../../backend/tests/AppTorcedor.Api.Tests/AppWebApplicationFactory.cs) para evitar dependência de chaves Resend.

## Fluxo implementado: boas-vindas no cadastro

Após **commit** bem-sucedido do registo do torcedor, [`TorcedorAccountService`](../../backend/src/AppTorcedor.Infrastructure/Services/Account/TorcedorAccountService.cs) compõe a mensagem com [`IWelcomeEmailComposer`](../../backend/src/AppTorcedor.Application/Abstractions/IWelcomeEmailComposer.cs) / [`WelcomeEmailComposer`](../../backend/src/AppTorcedor.Infrastructure/Services/Email/WelcomeEmailComposer.cs) e chama `IEmailSender.SendAsync`. Cobre:

- cadastro por formulário (`RegisterAsync`);
- primeiro registo via Google (`RegisterGoogleUserAsync`).

### Template administrável (Tipo B)

Três chaves em `AppConfigurationEntries` (editáveis em `/admin/configurations`, secção *E-mail — boas-vindas*):

| Chave | Uso |
|-------|-----|
| `Email.Welcome.Subject` | Assunto. Aceita `{{Name}}` (nome **não** escapado, apenas substituição de texto). |
| `Email.Welcome.Html` | Corpo HTML. Placeholders: `{{Name}}` (nome **escapado** em HTML) e `{{BannerImage}}` (bloco `<img>` centrado quando `Email.Welcome.ImageUrl` for uma URL `http`/`https` absoluta válida). |
| `Email.Welcome.ImageUrl` | URL pública da imagem (banner). Se o HTML não contiver `{{BannerImage}}`, o backend **prefixa** o bloco de imagem ao corpo. |

**Fallback:** se assunto/HTML estiverem vazios, ou o HTML for considerado inválido/perigoso (`<script`, `javascript:`, atributos `onerror`/`onload`), ou a URL da imagem não for `http`/`https` absoluta, o sistema usa o template padrão embutido em código e regista aviso em log — **o cadastro não falha**.

**Exemplo de HTML com banner explícito:**

```html
{{BannerImage}}
<p>Olá, {{Name}}!</p>
<p>Obrigado por se cadastrar no sócio torcedor.</p>
```

Falhas no envio são registadas com **`LogWarning`** e **não** falham o cadastro (utilizador já persistido).

## Outros usos futuros

Para outros eventos (recuperação de senha, recibo, etc.), reutilize `IEmailSender` ou extraia templates para recursos/serviços dedicados.

## Diagnóstico: cadastro sem e-mail visível nos logs

- Com **`Email:Provider=Mock`**, procure no JSON de log mensagens **`E-mail (Mock):`** e, no fluxo de cadastro, **`E-mail de boas-vindas: iniciando`** / **`envio concluído`** em `TorcedorAccountService`.
- Com **`Email:Provider=Resend`**, o remetente **`Email:Resend:FromAddress`** tem de ser um endereço de um **domínio verificado** no Resend; endereços fictícios (ex.: `*.local`) ou domínios não verificados fazem a API falhar. O backend regista **`Resend e-mail falhou (API)`** ou **`Falha ao enviar e-mail de boas-vindas`** com nível **Error** e a mensagem devolvida pela API.
- **Não** coloque chaves `re_…` em ficheiros versionados; use variáveis de ambiente, User Secrets ou Jenkins Credentials.

## Referência Resend

- [Resend — documentação](https://resend.com/docs)
- [resend-dotnet](https://github.com/resend/resend-dotnet)
