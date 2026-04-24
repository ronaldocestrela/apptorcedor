# Manual do Usuário — Perfil Atendimento

> **Sistema:** Sócio Torcedor
> **Perfil:** Atendimento
> **Versão:** 1.0 — Abril/2026

---

## 1. Visão Geral

O perfil **Atendimento** tem acesso ao módulo de suporte do backoffice administrativo. A função principal é gerenciar chamados abertos pelos torcedores: visualizar, responder, reclassificar, atribuir e encerrar tickets.

O acesso é controlado pela permissão **`Chamados.Responder`**. Nenhum outro módulo administrativo é acessível por padrão para este perfil.

---

## 2. Acesso ao Sistema

1. Acesse o endereço do painel administrativo (fornecido pelo administrador do clube).
2. Faça login com o e-mail e senha cadastrados.
3. Após autenticar, o menu lateral exibirá apenas as seções às quais você tem acesso — no caso do perfil Atendimento, o item **Suporte**.

> Se a opção **Suporte** não aparecer no menu, entre em contato com o administrador para verificar se a permissão `Chamados.Responder` está atribuída à sua conta.

---

## 3. Painel de Suporte

### 3.1 Acessar a fila de chamados

No menu lateral, clique em **Suporte**. Você verá a listagem de todos os chamados abertos no sistema.

### 3.2 Informações exibidas na listagem

| Coluna | Descrição |
|--------|-----------|
| **ID** | Identificador único do chamado |
| **Assunto** | Título resumido do chamado |
| **Torcedor** | Nome do solicitante |
| **Fila** | Categoria do chamado (ex.: Financeiro, Ingresso, Geral) |
| **Prioridade** | Normal, Alta ou Urgente |
| **Status** | Estado atual do chamado |
| **SLA** | Prazo de atendimento — exibido em vermelho quando vencido |
| **Responsável** | Agente atribuído ao chamado (pode estar vazio) |

### 3.3 Filtros disponíveis

Utilize os filtros no topo da listagem para localizar chamados específicos:

- **Fila** — filtra por categoria
- **Status** — filtra por estado atual
- **Responsável** — filtra chamados de um agente específico
- **Sem responsável** — exibe apenas chamados não atribuídos
- **SLA vencido** — exibe apenas chamados com prazo ultrapassado

---

## 4. Detalhes do Chamado

Clique em qualquer linha da listagem para abrir o chamado completo.

A tela de detalhe exibe:

- **Cabeçalho:** assunto, fila, prioridade, status, SLA e responsável atual
- **Histórico de mensagens:** conversa completa entre o torcedor e a equipe de atendimento
- **Linha do tempo:** registro de todas as ações realizadas (abertura, respostas, atribuições, mudanças de status)
- **Mensagens internas:** notas visíveis apenas para a equipe de atendimento (não aparecem para o torcedor)

### 4.1 Anexos

Mensagens podem conter arquivos em anexo. Clique na miniatura para visualizar imagens em tela cheia ou no botão **Baixar** para salvar o arquivo.

Tipos aceitos: imagens (JPEG, PNG, WebP) e PDF.

---

## 5. Ações Disponíveis

### 5.1 Responder ao chamado

1. Role até o rodapé do chamado.
2. Clique em **Responder**.
3. Digite a mensagem no campo de texto.
4. Opcionalmente, marque **Mensagem interna** para que a resposta não seja enviada ao torcedor.
5. Clique em **Enviar**.

> Não é possível responder a chamados com status **Fechado**. Para isso, o torcedor deve reabrir o chamado pelo aplicativo.

### 5.2 Atribuir responsável

1. Na tela do chamado, localize o campo **Responsável**.
2. Selecione um agente na lista.
3. Confirme a atribuição.

Para **remover** a atribuição, selecione a opção vazia na lista e confirme.

### 5.3 Alterar status

1. Na tela do chamado, clique no botão de mudança de status.
2. Selecione o novo status desejado.
3. Opcionalmente, informe um motivo no campo **Observação**.
4. Confirme a operação.

---

## 6. Status dos Chamados

| Status | Significado |
|--------|-------------|
| **Aberto** | Chamado recebido, aguardando atendimento |
| **Em andamento** | Chamado em tratamento pela equipe |
| **Aguardando usuário** | Equipe aguarda retorno ou informação do torcedor |
| **Resolvido** | Atendimento concluído; aguarda confirmação do torcedor |
| **Fechado** | Chamado encerrado definitivamente |

### 6.1 Transições permitidas

```
Aberto         → Em andamento / Resolvido / Fechado
Em andamento   → Aguardando usuário / Resolvido / Fechado
Aguardando     → Em andamento / Resolvido / Fechado
Resolvido      → Fechado / Aberto (reabertura pelo torcedor)
Fechado        → Aberto (reabertura pelo torcedor)
```

Transições não listadas acima são bloqueadas pelo sistema e retornam erro de negócio.

---

## 7. SLA — Prazo de Atendimento

O SLA (acordo de nível de serviço) define o prazo máximo para resposta inicial ao chamado. O prazo é calculado automaticamente no momento da abertura do chamado, com base na prioridade:

| Prioridade | Prazo |
|------------|-------|
| **Normal** | 48 horas |
| **Alta** | 24 horas |
| **Urgente** | 4 horas |

Chamados com **SLA vencido** são sinalizados em destaque na listagem e no detalhe. O SLA é zerado (atendido) após a primeira resposta enviada ao torcedor.

---

## 8. Abertura de Chamado pelo Staff

Em situações excepcionais, é possível abrir um chamado em nome de um torcedor diretamente pelo backoffice:

1. Na lista de chamados, clique em **Novo chamado**.
2. Selecione o torcedor solicitante pelo campo de busca de usuário.
3. Preencha: **fila**, **assunto**, **prioridade** e, opcionalmente, a **mensagem inicial**.
4. Clique em **Criar**.

> O chamado criado pelo staff é registrado normalmente e o torcedor poderá acompanhá-lo pelo aplicativo.

---

## 9. Dashboard Administrativo

O painel principal do sistema exibe o indicador **Chamados abertos**, que contabiliza todos os tickets nos status **Aberto**, **Em andamento** ou **Aguardando usuário**. Utilize esse número como referência rápida da demanda atual da fila.

---

## 10. Boas Práticas

- **Responda sempre dentro do SLA.** Chamados com prazo vencido impactam negativamente a experiência do torcedor.
- **Use mensagens internas** para coordenar com outros membros da equipe sem expor informações operacionais ao torcedor.
- **Atribua o chamado** ao agente responsável assim que iniciar o atendimento para evitar respostas duplicadas.
- **Altere o status** conforme o andamento real do chamado. Chamados esquecidos em **Aberto** distorcem o indicador do dashboard.
- **Sempre registre o motivo** ao fechar ou alterar o status de um chamado — isso facilita auditorias e relatórios futuros.

---

## 11. Permissões do Perfil Atendimento

| Permissão | Acesso |
|-----------|--------|
| `Chamados.Responder` | **Sim** — acesso completo ao módulo de suporte |
| Demais módulos administrativos | **Não** — requer perfil Administrador ou permissões adicionais |

Caso necessite de acesso a outros módulos (como visualização de usuários ou planos), solicite ao administrador a concessão das permissões correspondentes.

---

## 12. Suporte Técnico

Em caso de dúvidas sobre o uso do sistema ou problemas técnicos, entre em contato com o administrador responsável pelo clube.
