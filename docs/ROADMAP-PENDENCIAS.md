# Roadmap de pendências — App Torcedor

Documento de referência com o que **ainda falta implementar** em relação ao escopo descrito em [AGENTS.md](../AGENTS.md). A ordem abaixo prioriza **capacidades de gestão, administração e informação**; o **fluxo do torcedor para contratar planos** fica **explicitamente ao final**.

**Já entregue (baseline):** autenticação JWT (access + refresh), Identity com `ApplicationUser`, roles base e seed do Administrador Master, persistência de refresh token, migrations, SPA com login e rotas por perfil, testes iniciais da API e documentação da fase de auth.

**Parte A (fundação técnica):** ver [docs/architecture/parte-a-fundacao.md](architecture/parte-a-fundacao.md) — projeto `AppTorcedor.Application` (CQRS/MediatR), permissões granulares em JWT + tabelas `AppPermissions` / `AppRolePermissions`, auditoria (`AuditLogs` + interceptor), configurações administráveis (`AppConfigurationEntries`), entidades iniciais de `Membership` / `MembershipPlan` / `Payment`, health checks e correlação (`X-Correlation-Id`).

---

## Como usar este arquivo

- Itens marcados com `- [ ]` são **pendentes**.
- Ao concluir um item, marque `- [x]` e, se possível, registre na documentação do módulo.
- Respeitar **TDD**, separação **Conta / Permissões / Associação (Membership)** e atualização de documentação ao final de cada entrega (conforme AGENTS.md).

---

## Parte A — Fundação técnica e governança (gestão do produto técnico)

Ordem sugerida para evitar retrabalho estrutural.

- [x] **Arquitetura modular + CQRS:** organizar pastas/projetos por módulo (Application/Domain/Infrastructure por domínio), introduzir MediatR (ou pipeline equivalente) para commands/queries.
- [x] **Permissões granulares:** modelo de permissões (ex.: `Usuarios.Visualizar`, `Planos.Criar`) separado de roles; políticas de autorização na API; seed ou UI para atribuição.
- [x] **Auditoria e rastreabilidade (mínimo):** quem alterou o quê em entidades críticas (planos, status de sócio, pagamentos) — definir escopo e armazenamento.
- [x] **Configurações da aplicação:** parâmetros operacionais (prazos, limites, textos legais versionados, feature flags simples) expostos para perfil administrativo.
- [x] **Observabilidade:** health checks, logs estruturados, correlação de requisições (definir padrão do projeto).

---

## Parte B — Administração e gestão interna (informação e controle)

Foco em **quem opera o clube**: administradores, financeiro, atendimento, marketing, operador.

### B.1 Módulo Administration (backoffice)

- [x] **Gestão de usuários internos:** convite/cadastro de staff (token + `POST /api/auth/accept-staff-invite`), vínculo com **roles** (permissões herdadas da matriz role × permissão), ativação/desativação (`Usuarios.Editar`).
- [x] **Gestão de permissões:** API e SPA para consultar e **editar** matriz perfil × permissão (`PUT /api/admin/role-permissions`, `Configuracoes.Editar`); sem acoplar Membership.
- [x] **Painel inicial admin:** dashboard com KPIs de sócios ativos e inadimplentes (`GET /api/admin/dashboard`). **Chamados abertos:** `openSupportTickets` conta chamados em aberto (Open, InProgress, WaitingUser); ver B.11.

### B.2 LGPD (lado gestão e compliance)

- [x] **Cadastro e versionamento de documentos:** termos, política de privacidade, registros de versão.
- [x] **Consentimentos:** modelo de aceite com data/hora e versão do documento; consulta por usuário (atendimento/admin).
- [x] **Exportação / anonimização:** fluxos mínimos exigidos pela operação (definir com jurídico).

### B.3 Users (visão administrativa)

Detalhes técnicos: [docs/architecture/parte-b3-users-admin.md](architecture/parte-b3-users-admin.md).

- [x] **Listagem e busca de usuários** (torcedores e não associados).
- [x] **Visualização de perfil** (`UserProfile`: documento, data nascimento, foto, endereço).
- [x] **Ativação / inativação de conta** e flags administrativas alinhadas a regras de negócio.
- [x] **Histórico de alterações** relevantes do perfil (se aplicável à LGPD/auditoria).

### B.4 Membership (visão administrativa)

Detalhes técnicos: [docs/architecture/parte-b4-membership-admin.md](architecture/parte-b4-membership-admin.md).

- [x] **Consulta de status de associação:** Não associado, Ativo, Inadimplente, Suspenso, Cancelado.
- [x] **Alteração manual de status** (com permissão e motivo/auditoria).
- [x] **Vínculo com plano e datas:** início, fim, próximo vencimento (consulta administrativa; edição manual de plano/datas fora do escopo desta entrega).
- [x] **Histórico de associação** (mudanças de status com motivo; histórico de domínio em `MembershipHistories`; mudanças de plano quando existirem serão refletidas nos eventos futuros).

### B.5 Plans (gestão de oferta)

Detalhes técnicos: [docs/architecture/parte-b5-plans-admin.md](architecture/parte-b5-plans-admin.md).

- [x] **CRUD de planos** (`MembershipPlan`: nome, preço, ciclo de cobrança, desconto, ativo).
- [x] **Benefícios e regras** por plano (benefícios persistidos em `MembershipPlanBenefits`; regras básicas em `RulesNotes`; motor completo de parceiros/vantagens permanece B.10).
- [x] **Publicação / despublicação** de planos para o canal do torcedor (`IsPublished` / `PublishedAt`; catálogo público na Parte D).

### B.6 Payments (visão financeira / admin)

- [x] **Listagem de cobranças e pagamentos** (`Payment`: valores, status, vencimento, pago em).
- [x] **Conciliação básica** e estados de cobrança.
- [x] **Estorno / cancelamento** (permissão `Pagamentos.Estornar` ou equivalente).
- [x] **Inadimplência:** regras automáticas ou gatilhos para mudança de status de Membership.
- [x] **Implementação de `IPaymentProvider`:** integração real ou adaptadores mock para PIX, cartão, assinatura recorrente.

### B.7 Digital Card (gestão)

- [x] **Definição de layout/regra** de exibição da carteirinha (campos obrigatórios, branding).
- [x] **Regeneração / invalidação** em casos administrativos (fraude, troca de documento).

Detalhes: [docs/architecture/parte-b7-digital-card-admin.md](architecture/parte-b7-digital-card-admin.md).

### B.8 Games e Tickets (gestão)

- [x] **CRUD de jogos** (`Game`).
- [ ] **Regras de benefício** vinculadas a jogos/ingressos (se aplicável).
- [x] **Integração `ITicketProvider`:** reserva, compra, consulta; gestão de resgates e QR Code no backoffice.
- [x] **Ingressos:** listagem administrativa, status, vínculo com usuário e jogo.

Detalhes: [docs/architecture/parte-b8-games-tickets-admin.md](architecture/parte-b8-games-tickets-admin.md).

### B.9 News (gestão de conteúdo)

- [x] **Editoria:** criar, editar, publicar e despublicar notícias.
- [x] **Notificações:** disparo ou agendamento **in-app** (sem push/e-mail nesta entrega).

Detalhes: [docs/architecture/parte-b9-news-admin.md](architecture/parte-b9-news-admin.md).

### B.10 Loyalty e Benefits (gestão)

- [x] **Regras de pontuação** e campanhas de fidelidade.
- [x] **Ranking:** critérios e publicação (mensal e acumulado no admin).
- [x] **Parceiros e vantagens** (cadastro, vigência, elegibilidade por plano).

Detalhes: [docs/architecture/parte-b10-loyalty-benefits-admin.md](architecture/parte-b10-loyalty-benefits-admin.md).

### B.11 Support (atendimento)

- [x] **Filas de chamados** para perfis Atendimento/Operador.
- [x] **SLA e estados** do chamado; respostas e histórico.
- [x] **Permissão** `Chamados.Responder` e segregação de dados (API: somente JWT com permissão; torcedor sem permissão recebe 403).

Detalhes: [docs/architecture/parte-b11-support-admin.md](architecture/parte-b11-support-admin.md).

---

## Parte C — Experiência do torcedor e canais (após o backoffice estar maduro)

Funcionalidades voltadas ao **usuário final**, mantendo a regra: administrador pode existir **sem** plano; torcedor pode existir **sem** ser sócio.

### C.1 Cadastro e perfil (torcedor)

- [x] **Cadastro público** (`RegisterUser` / fluxo equivalente) com validações.
- [x] **Completar perfil** (`UserProfile`) e upload de foto.
- [x] **Login social** (Google ID token + JWT/refresh alinhados ao login local).
- [x] **Área “Minha conta”** no web (paridade Flutter: Parte E).

### C.2 Notícias e benefícios (consumo)

- [x] **Feed de notícias** e detalhe (`GetNewsFeed`).
- [x] **Listagem de benefícios** elegíveis ao usuário/plano.

Detalhes: [docs/architecture/parte-c2-noticias-beneficios-torcedor.md](architecture/parte-c2-noticias-beneficios-torcedor.md).

### C.3 Carteirinha digital (torcedor)

- [x] **Visualização da carteirinha** para sócio ativo (e regras diferentes por status, se aplicável).
- [x] **Offline / cache** (opcional, especialmente no Flutter) — **web:** cache local com validade alinhada a `cacheValidUntilUtc` e fallback em falha de rede; Flutter permanece na Parte E.

Detalhes: [docs/architecture/parte-c3-carteirinha-torcedor.md](architecture/parte-c3-carteirinha-torcedor.md).

### C.4 Jogos e ingressos (torcedor)

- [x] **Lista de jogos** disponíveis (`ListGames`).
- [x] **Resgate/visualização de ingressos** e QR Code (`GetTickets`, `RedeemTicket`).

Detalhes: [docs/architecture/parte-c4-jogos-ingressos-torcedor.md](architecture/parte-c4-jogos-ingressos-torcedor.md).

### C.5 Fidelidade (torcedor)

- [ ] **Saldo de pontos** e posição no ranking (conforme regras).

### C.6 Suporte (torcedor)

- [ ] **Abertura e acompanhamento de chamados** pelo usuário.

---

## Parte D — Contratação de planos pelo torcedor (última etapa sugerida)

Implementar **depois** de: gestão de planos (B.5), pagamentos mínimos (B.6), e membership admin (B.4) — para o fluxo público reutilizar as mesmas regras e evitar inconsistência.

- [ ] **Catálogo de planos** na área logada do torcedor (`ListPlans` — somente planos ativos/publicados).
- [ ] **Simulação / resumo** do plano (preço, ciclo, desconto, benefícios principais).
- [ ] **Command `SubscribeMember`:** criar/atualizar `Membership`, status inicial coerente (ex.: aguardando pagamento ou ativo conforme regra).
- [ ] **Integração com pagamento na contratação:** escolha PIX ou cartão, criação de assinatura recorrente via `IPaymentProvider`.
- [ ] **Pós-contratação:** confirmação, recibo, próximos vencimentos, acesso à carteirinha quando **Ativo**.
- [ ] **Troca de plano** (`ChangePlan`) com regras de upgrade/downgrade e impacto financeiro.
- [ ] **Cancelamento** pelo torcedor (com política de arrependimento/cancelamento definida pelo clube).

---

## Parte E — Mobile FlutterParidade gradual com o web (pode seguir depois das Partes B e D para o núcleo sócio).

- [ ] Autenticação e cadastro.
- [ ] Assinatura e gestão de plano.
- [ ] Carteirinha e ingressos (QR Code).
- [ ] Notícias, perfil e chamados.

---

## Parte F — Infraestrutura e deploy

- [ ] **Dockerfile** da API e pipeline de build.
- [ ] **Ambiente VPS** (reverse proxy, HTTPS, variáveis de ambiente, secrets).
- [ ] **Estratégia de migrations em produção** (job dedicado vs. startup — alinhar com política de disponibilidade).

---

## Referência rápida — comandos e queries (AGENTS.md)

Implementação futura deve cobrir, entre outros:

| Tipo | Exemplos |
|------|----------|
| Commands | `CreateUser`, `RegisterUser`, `SubscribeMember`, `ChangePlan`, `RegisterPayment`, `CreateGame`, `RedeemTicket`, `PublishNews` |
| Queries | `GetUserProfile`, `GetMembershipStatus`, `ListPlans`, `ListGames`, `GetNewsFeed`, `GetTickets` |

---

## Observação sobre ordem de negócio

Esta ordem **não** substitui validação com o produto/clube: alguns itens de marketing ou notícias podem ser antecipados para comunicação externa; o bloco **D** permanece por último na lista **estratégica** para garantir que planos, pagamentos e status de sócio estejam consistentes antes do autosserviço de contratação.
