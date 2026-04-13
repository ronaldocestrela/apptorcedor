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

- [x] **Saldo de pontos** e posição no ranking (conforme regras).

### C.6 Suporte (torcedor)

- [x] **Abertura e acompanhamento de chamados** pelo usuário.

Detalhes: [docs/architecture/parte-c6-suporte-torcedor.md](architecture/parte-c6-suporte-torcedor.md).

---

## Parte D — Contratação de planos pelo torcedor (última etapa sugerida)

Implementar **depois** de: gestão de planos (B.5), pagamentos mínimos (B.6), e membership admin (B.4) — para o fluxo público reutilizar as mesmas regras e evitar inconsistência.

Ordem de execução sugerida: `D.1 → D.2 → D.3 → D.4 → D.5 → D.6 → D.7` (D.6 e D.7 podem ser desenvolvidos em paralelo após D.5).

---

### D.1 — Catálogo de planos (torcedor)

**Backend**
- [x] `D.1.1` Query `ListPublishedPlansQuery` + handler (`Application`) — filtra `IsPublished = true` e `IsActive = true`, retorna nome, preço, ciclo, desconto e lista de benefícios
- [x] `D.1.2` Endpoint `GET /api/plans` (**JWT obrigatório**, alinhado à SPA autenticada) — sem exigir sócio
- [x] `D.1.3` Testes unitários do handler e testes de API do endpoint

**Frontend**
- [x] `D.1.4` Página `/plans` com cards de planos (nome, preço, benefícios resumidos, CTA "Assinar")
- [x] `D.1.5` Serviço Axios `plansService.listPublished()`

---

### D.2 — Simulação / detalhe do plano

**Backend**
- [x] `D.2.1` Query `GetPlanDetailsQuery` + handler — retorna todos os campos do plano + benefícios completos + regras (`RulesNotes`)
- [x] `D.2.2` Endpoint `GET /api/plans/{id}` (**JWT obrigatório**, alinhado ao D.1)
- [x] `D.2.3` Testes unitários e de API

**Frontend**
- [x] `D.2.4` Página `/plans/{id}` com resumo completo: preço, ciclo, desconto calculado, lista de benefícios, botão "Contratar" (desabilitado até D.3/D.4)

---

### D.3 — Command `SubscribeMember` (contratação backend)

**Backend**
- [x] `D.3.1` Command `SubscribeMemberCommand` + handler (`Application`) — cria/atualiza `Membership` com status inicial **`PendingPayment`**
- [x] `D.3.2` Regra: impedir dupla assinatura ativa; validar plano publicado e ativo
- [x] `D.3.3` Publicar evento de domínio `MemberSubscribedEvent` (para desencadear pontos de fidelidade, carteirinha, etc.)
- [x] `D.3.4` Testes unitários do command/handler (incluindo casos de erro: plano inativo, já associado ativo)

---

### D.4 — Integração de pagamento na contratação

**Backend**
- [x] `D.4.1` Endpoint `POST /api/subscriptions` (JWT obrigatório) — recebe `planId` e `paymentMethod` (Pix | Card), chama `SubscribeMemberCommand` e `IPaymentProvider.CreatePixAsync` / `CreateCardAsync` (cobrança inicial; `CreateSubscriptionAsync` permanece para evolução de recorrência/gateway real)
- [x] `D.4.2` Retornar no response: `membershipId`, `paymentId`, instruções de pagamento (payload PIX mock / URL checkout cartão)
- [x] `D.4.3` Webhook/callback de confirmação de pagamento — `POST /api/subscriptions/payments/callback` com `secret` (`Payments:WebhookSecret`); atualiza `Payment` para `Paid` e `Membership` de `PendingPayment` para `Ativo` (`ConfirmTorcedorSubscriptionPaymentCommand`)
- [x] `D.4.4` Testes de integração do fluxo completo com `MockPaymentProvider`

**Frontend**
- [x] `D.4.5` Tela de checkout: seleção de método (PIX / Cartão), resumo do plano, botão "Confirmar"
- [x] `D.4.6` Exibição de instruções PIX (payload / copia-e-cola) ou link de checkout para cartão após confirmação
- [x] `D.4.7` Serviço Axios `subscriptionsService.subscribe(planId, paymentMethod)`

Detalhes: [docs/architecture/parte-d4-integracao-pagamento-contratacao.md](architecture/parte-d4-integracao-pagamento-contratacao.md).

---

### D.5 — Pós-contratação (confirmação e recibo)

Detalhes: [docs/architecture/parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md](architecture/parte-d5-pos-contratacao-confirmacao-recibo-torcedor.md).

**Backend**
- [x] `D.5.1` Query `GetMySubscriptionSummaryQuery` — retorna `Membership` ativo, datas de vencimento, último pagamento, status da carteirinha
- [x] `D.5.2` Endpoint `GET /api/account/subscription` (JWT) — usado pela SPA pós-contratação
- [x] `D.5.3` Testes unitários da query

**Frontend**
- [x] `D.5.4` Página `/subscription/confirmation` (ou redirect pós-checkout) com: confirmação visual, recibo (plano, valor pago, próximo vencimento), link para `/digital-card`
- [x] `D.5.5` Atualizar `/account` ("Minha conta") para exibir status da assinatura e próximo vencimento

---

### D.6 — Troca de plano (`ChangePlan`)

Detalhes: [docs/architecture/parte-d6-troca-plano-torcedor.md](architecture/parte-d6-troca-plano-torcedor.md).

**Backend**
- [x] `D.6.1` Command `ChangePlanCommand` + handler — regras de upgrade/downgrade: calcular proporcional, cancelar cobrança atual, criar nova via `IPaymentProvider`
- [x] `D.6.2` Registrar mudança em `MembershipHistories` com motivo `PlanChanged`
- [x] `D.6.3` Endpoint `PUT /api/account/subscription/plan` (JWT)
- [x] `D.6.4` Testes: Application (`ChangePlanCommandHandlerTests`), Infrastructure (`TorcedorPlanChangeServiceTests`), API (`PartD6ChangePlanApiTests`)

**Frontend**
- [x] `D.6.5` Seção "Trocar plano" em `/account`: lista de outros planos publicados, seleção de método de pagamento, confirmação de troca e instruções pós-sucesso

---

### D.7 — Cancelamento pelo torcedor

**Backend**
- [x] `D.7.1` Command `CancelMembershipCommand` + handler — aplica política de arrependimento (configurável via `AppConfigurationEntries`), chama `IPaymentProvider.CancelAsync`, atualiza `Membership.Status = Cancelado`
- [x] `D.7.2` Registrar em `MembershipHistories` com motivo `CancelledByMember`
- [x] `D.7.3` Endpoint `DELETE /api/account/subscription` (JWT)
- [x] `D.7.4` Testes unitários: dentro do prazo de arrependimento, fora do prazo, membership já cancelada

**Frontend**
- [x] `D.7.5` Botão/seção "Cancelar assinatura" em `/account` com modal de confirmação explicando a política do clube
- [x] `D.7.6` Feedback pós-cancelamento: status atualizado, prazo de acesso restante

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
