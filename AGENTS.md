# 📄 AGENTS.md — Sistema de Sócio Torcedor (Single Tenant)

## 🧠 Visão Geral

Este projeto é um sistema de **sócio torcedor para um único clube**, desenvolvido como um **monólito modular**.

### Stack principal

* **Backend:** .NET 10 + ASP.NET Core + Identity + CQRS + SQL Server
* **Frontend Web:** React + Vite + Axios (SPA)
* **Mobile:** Flutter

---

## 🎯 Objetivo do Sistema

Permitir:

* cadastro externo de usuários
* gestão de sócios e não sócios
* controle de planos e assinaturas
* pagamentos recorrentes (PIX e cartão)
* carteirinha digital
* resgate e visualização de ingressos
* notícias e notificações
* ranking/fidelidade
* atendimento/chamados
* controle administrativo completo com permissões granulares

---

## 🏗️ Diretrizes Arquiteturais

### Estilo

* Monólito
* Modularização por domínio
* CQRS
* Clean Architecture (por módulo)

### NÃO utilizar

* Multitenancy
* Microservices
* Arquitetura distribuída

---

## ⚠️ Princípios Obrigatórios

### 1. TDD (Test Driven Development)

Este projeto **DEVE obrigatoriamente seguir TDD**.

#### Regras:

* Nenhuma funcionalidade pode ser implementada sem teste
* Sempre seguir ciclo:

  1. Escrever teste (falhando)
  2. Implementar código mínimo
  3. Refatorar

#### Tipos de testes obrigatórios:

* Unitários (Domain + Application)
* Integração (Infrastructure)
* Testes de API (Controllers)

---

### 2. Atualização de documentação (MANDATÓRIO)

Ao final de **cada execução do agente**, deve-se:

* Atualizar documentação do projeto
* Registrar:

  * funcionalidades criadas
  * decisões técnicas
  * endpoints adicionados
  * mudanças em regras de negócio
* Garantir consistência entre código e documentação

---

### 3. Separação de responsabilidades

O sistema deve separar claramente:

* **Conta (User)**
* **Permissões (Roles/Permissions)**
* **Associação (Membership)**

Esses conceitos **NÃO podem ser acoplados**.

---

## 👤 Modelo de Usuário

### Regras

* qualquer pessoa pode se cadastrar
* nem todo usuário é sócio
* nem todo usuário tem acesso administrativo
* nem todo sócio é administrador

---

## 🧩 Camadas conceituais

### 1. Conta (Identity)

Responsável por:

* login
* senha
* autenticação
* login social

---

### 2. Perfil funcional

Define o que o usuário pode fazer:

* administrador
* financeiro
* atendimento
* marketing
* operador
* torcedor

---

### 3. Associação (Membership)

Define se o usuário é sócio:

* não associado
* ativo
* inadimplente
* suspenso
* cancelado

---

## 🔐 Perfis do sistema

* Administrador Master (seed inicial)
* Administrador
* Financeiro
* Atendimento
* Marketing
* Operador
* Torcedor

---

## 🔑 Permissões (granular)

Exemplo:

```text
Usuarios.Visualizar
Usuarios.Editar
Socios.Gerenciar
Planos.Criar
Pagamentos.Estornar
Jogos.Criar
Ingressos.Gerenciar
Noticias.Publicar
Chamados.Responder
Configuracoes.Editar
```

---

## 🧱 Módulos do Sistema

### 1. Identity

* autenticação
* login social
* roles
* permissões
* LGPD

### 2. Users

* cadastro
* dados pessoais
* ativação/inativação

### 3. Membership

* status de sócio
* vínculo com plano
* histórico

### 4. Plans

* planos
* benefícios
* regras

### 5. Payments

* PIX
* cartão
* recorrência
* inadimplência

### 6. Digital Card

* carteirinha
* exibição

### 7. Games

* jogos
* regras

### 8. Tickets

* integração externa
* resgate
* QR Code

### 9. News

* notícias
* notificações

### 10. Loyalty

* pontos
* ranking

### 11. Benefits

* parceiros
* vantagens

### 12. Support

* chamados
* atendimento

### 13. Administration

* permissões
* usuários internos
* configurações

---

## 🗄️ Modelagem principal

### ApplicationUser

* Id
* Name
* Email
* PasswordHash
* PhoneNumber
* IsActive
* CreatedAt

---

### UserProfile

* UserId
* Document
* BirthDate
* PhotoUrl
* Address

---

### Membership

* Id
* UserId
* PlanId
* Status
* StartDate
* EndDate
* NextDueDate

#### Status:

```text
NaoAssociado
Ativo
Inadimplente
Suspenso
Cancelado
```

---

### MembershipPlan

* Id
* Name
* Price
* BillingCycle
* DiscountPercentage
* IsActive

---

### Payment

* Id
* UserId
* MembershipId
* Amount
* Status
* DueDate
* PaidAt

---

### Game

* Id
* Opponent
* Competition
* GameDate

---

### Ticket

* Id
* UserId
* GameId
* ExternalTicketId
* QrCode
* Status

---

## ⚙️ CQRS

### Commands

* CreateUser
* RegisterUser
* SubscribeMember
* ChangePlan
* RegisterPayment
* CreateGame
* RedeemTicket
* PublishNews

---

### Queries

* GetUserProfile
* GetMembershipStatus
* ListPlans
* ListGames
* GetNewsFeed
* GetTickets

---

## 💳 Pagamentos

### Interface obrigatória

```csharp
public interface IPaymentProvider
{
    Task CreateSubscriptionAsync(...);
    Task CreatePixAsync(...);
    Task CreateCardAsync(...);
    Task CancelAsync(...);
}
```

---

## 🎟️ Ingressos

### Interface obrigatória

```csharp
public interface ITicketProvider
{
    Task ReserveAsync(...);
    Task PurchaseAsync(...);
    Task GetAsync(...);
}
```

---

## 🌐 Frontend Web

### Stack

* React
* Vite
* Axios

### Estrutura

```text
src/
  app/
  shared/
  features/
  pages/
```

---

## 📱 Mobile (Flutter)

### Funcionalidades

* login
* cadastro
* assinatura
* carteirinha
* ingressos (QR Code)
* notícias
* perfil
* chamados

---

## ⚖️ LGPD

Obrigatório:

* aceite de termos
* aceite de política
* versionamento
* data/hora do aceite

---

## 🐳 Infraestrutura

* Docker obrigatório
* SQL Server
* API .NET
* React
* Flutter
* VPS

---

## 🚀 Fases de Implementação

### Fase 1 — Fundação

* arquitetura base
* Identity
* seed admin master
* testes iniciais
* Swagger

### Fase 2 — Usuários

* cadastro externo
* perfis
* permissões

### Fase 3 — Sócio

* planos
* assinatura
* status
* carteirinha

### Fase 4 — Pagamentos

* recorrência
* PIX
* cartão

### Fase 5 — Jogos e ingressos

### Fase 6 — Notícias

### Fase 7 — Fidelidade

### Fase 8 — Atendimento

---

## ⚠️ Regras Críticas

### O sistema deve permitir:

* usuários sem plano
* sócios ativos
* administradores sem plano
* regras diferentes por status

---

## ✅ Conclusão

Este sistema deve ser:

* simples (monólito)
* bem estruturado
* altamente testável (TDD)
* desacoplado internamente
* preparado para evolução futura

---

## 📌 Regras finais obrigatórias para o agente

1. Sempre usar TDD
2. Nunca implementar sem testes
3. Atualizar documentação ao final de cada execução
4. Manter separação entre User, Membership e Permissions
5. Não acoplar regras de negócio indevidamente
6. Garantir código limpo e organizado
7. Seguir arquitetura modular definida