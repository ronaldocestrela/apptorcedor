# Identidade Visual — AppTorcedor

Documento de referência para LLMs e desenvolvedores. Descreve com precisão todas as decisões de design do sistema: paleta de cores, tipografia, tokens CSS, componentes, espaçamentos, animações e padrões de interação.

---

## 1. Filosofia de Design

O sistema possui **dois contextos visuais distintos** que compartilham a mesma paleta base, mas têm layouts e UX completamente diferentes:

| Contexto | Quem usa | Prioridade de layout | Layout principal |
|---|---|---|---|
| **Torcedor (fan)** | Usuário final | Mobile-first | Full-viewport, bottom nav fixa |
| **Admin (backoffice)** | Staff interno | Desktop-first | Sidebar + workspace |

Ambos os contextos usam um **tema escuro único** — não há modo claro. O sistema é inspirado em cores de campo de futebol: verde escuro profundo como base, verde limão brilhante como acento.

### 1.1 Escudo do clube (marca dinâmica)

- O **escudo exibido** no login (`/login`), cadastro (`/register`), sidebar do admin e cabeçalho do dashboard do torcedor vem da API: `GET /api/branding` → `teamShieldUrl` (pode ser `null`).
- **Sem URL configurada:** o frontend usa um **placeholder SVG** embutido (tons `#1a2229` / `#8cd392`, escudo genérico) — não depende de arquivos em `/public/logos/` para essas telas.
- **Com URL configurada:** o admin envia imagem em `/admin/configurations` (JPEG/PNG/WebP); a API grava em storage (`TeamShield:Provider` Local ou Cloudinary) e persiste a URL na entrada `Brand.TeamShieldUrl`.
- O **favicon** em `frontend/index.html` continua apontando para um asset estático em `/logos/` até eventual evolução dedicada.

---

## 2. Paleta de Cores

### 2.1 Cor Base — Fundos

| Nome semântico | Valor hex / rgba | Uso |
|---|---|---|
| `bg-deepest` | `#0b0f14` | Fundo mais escuro; gradiente final torcedor |
| `bg-deep` | `#0e131a` | Fundo base da tela torcedor |
| `bg-main` | `#0e131a` (var `--admin-bg-main`) | Fundo principal do admin |
| `bg-sidebar` | `#0f2a1f` (var `--admin-bg-sidebar`) | Fundo sidebar admin |
| `bg-sidebar-end` | `#0e131a` | Gradiente final da sidebar |
| `bg-card` | `rgba(14, 19, 26, 0.82)` | Cards de acesso rápido (torcedor) |
| `bg-card-hover` | `rgba(15, 42, 31, 0.9)` | Estado hover dos cards |
| `bg-surface` | `rgba(15, 42, 31, 0.56)` a `rgba(11, 15, 20, 0.86)` | KPI cards admin (gradiente diagonal) |
| `bg-header-torcedor` | `rgba(14, 19, 26, 0.9)` | Header sticky torcedor (com blur) |
| `bg-header-admin` | `rgba(14, 19, 26, 0.72)` | Header sticky admin (com blur) |
| `bg-bottom-nav` | `rgba(11, 15, 20, 0.95)` | Bottom nav fixa (com blur) |
| `bg-modal-overlay` | `rgba(11, 15, 20, 0.72)` | Overlay de modais |

### 2.2 Cor de Acento — Verde Limão

O acento primário é um **verde limão vibrante**, usado para CTAs, links ativos, ícones em destaque e textos de valor.

| Token CSS | Valor hex | Uso |
|---|---|---|
| `--admin-accent` | `#8cd392` | Acento principal |
| `--admin-accent-soft` | `rgba(140, 211, 146, 0.2)` | Variante suavizada para fundos e gradientes de destaque |

### 2.3 Cores de Texto

| Token CSS / Nome | Valor | Uso |
|---|---|---|
| `--admin-text-primary` | `#f5f7fa` | Texto principal admin |
| `--admin-text-muted` | `#a6b0bf` | Texto secundário / labels / subtítulos admin |
| `text-primary-torcedor` | `#f5f7fa` | Texto principal torcedor |
| `text-muted-torcedor` | `#a6b0bf` | Texto secundário torcedor (saudação, hints) |
| `text-muted-nav` | `#7b8794` | Ícones/labels inativos do bottom nav |
| `text-card-label` | `#f5f7fa` | Labels nos quick-cards |
| `text-card-body` | `#a6b0bf` | Texto genérico em cards |

### 2.4 Cores Semânticas (KPI Cards)

Usadas nos KPI cards do admin para codificação de significado:

| Variante | Cor do ícone / valor | Background do ícone |
|---|---|---|
| `success` | `#8cd392` (verde — usa `--admin-accent`) | `rgba(140, 211, 146, 0.2)` |
| `warning` | `#f59e0b` (âmbar) | `rgba(245, 158, 11, 0.15)` |
| `info` | `#60a5fa` (azul) | `rgba(96, 165, 250, 0.15)` |
| `danger` | `#f87171` (vermelho) | `rgba(248, 113, 113, 0.15)` |

### 2.5 Cor de Alerta / Aviso

| Nome | Borda | Background | Texto |
|---|---|---|---|
| Alerta de perfil incompleto | `#ab9342` | `rgba(164, 130, 44, 0.18–0.22)` | `#ffe9a8` |

### 2.6 Bordas

| Token CSS | Valor | Uso |
|---|---|---|
| `--admin-border-soft` | `rgba(255, 255, 255, 0.08)` | Bordas sutis (cabeçalhos, separadores) |
| `--admin-border-strong` | `rgba(140, 211, 146, 0.42)` | Bordas visíveis (cards, sidebar) |
| `border-card-torcedor` | `rgba(255, 255, 255, 0.08)` | Bordas dos quick-cards |
| `border-card-torcedor-hover` | `rgba(140, 211, 146, 0.42)` | Hover dos quick-cards |
| `border-bottom-nav` | `rgba(255, 255, 255, 0.08)` | Separador bottom nav |
| `border-header-torcedor` | `rgba(255, 255, 255, 0.08)` | Separador header torcedor |
| `border-link-active` | cor `--admin-accent`, espessura `2px` no lado esquerdo | Link ativo na sidebar |

### 2.7 Botões

| Classe | Background | Cor do texto | Borda |
|---|---|---|---|
| `.btn-primary` | `transparent` | `#8cd392` | `#8cd392` |
| `.btn-secondary` | `rgba(140, 211, 146, 0.2)` | `#f5f7fa` | `rgba(140, 211, 146, 0.42)` |
| `.btn-danger` | `#8d2c34` | `#fff4f4` | `#bb4f58` |

Todos os botões: `border-radius: 10px`, `padding: 0.6rem 1rem`, `font-size: 0.92rem`. Disabled: `opacity: 0.7`.

---

## 3. Tipografia

### 3.1 Fontes

| Contexto | Família | Fallbacks |
|---|---|---|
| App completo (admin + torcedor) | **Outfit** | `'Avenir Next'`, `'Segoe UI'`, `sans-serif` |
| Token CSS | `--admin-font-family` | |

A fonte **Outfit** deve ser carregada via Google Fonts ou bundled. É humanista, geométrica, moderna — combina com o estilo esportivo do produto.

### 3.2 Escala Tipográfica

| Elemento | Tamanho | Peso | Cor | Notas |
|---|---|---|---|---|
| Título dashboard admin | `clamp(1.6rem, 2.8vw, 2.2rem)` | 700 | `--admin-text-primary` | letter-spacing: -0.02em |
| Subtítulo dashboard admin | `0.93rem` | 400 | `--admin-text-muted` | max-width: 52ch |
| Label seção sidebar | `0.68rem` | 700 | uppercase, letter-spacing: 0.1em | |
| Link sidebar | `0.88rem` | 500 | `--admin-text-muted` → ativo: `--admin-accent` | |
| Nome do usuário (header admin) | `0.85rem` | 600 | `--admin-text-primary` | |
| Role do usuário (header admin) | `0.72rem` | 400 | `--admin-text-muted` | capitalize |
| Label KPI card | `0.8rem` | 600 | `--admin-text-muted` | uppercase, letter-spacing: 0.07em |
| Valor KPI card | `2.5rem` | 700 | variant-dependent | line-height: 1 |
| Hint KPI card | `0.8rem` | 400 | `--admin-text-muted` | |
| Título "quick actions" | `0.75rem` | 600 | `--admin-text-muted` | uppercase, letter-spacing: 0.1em |
| Card quick action | `0.88rem` | 500 | `--admin-text-primary` | |
| Saudação torcedor pequena | `0.95rem` | 400 | `#a6b0bf` | "Olá," |
| Nome torcedor | `1.6rem` | 700 | `#f5f7fa` | line-height: 1.1 |
| Label quick-card torcedor | `0.9rem` | 600 | `#f5f7fa` | |
| Label bottom nav | `0.65rem` | 600 | `#7b8794` → ativo: `#8cd392` | |
| Title seção torcedor | `0.82rem` | 600 | `#7b8794` | uppercase, letter-spacing: 0.08em |
| Logo do app no header | `1.1rem` | 700 | `#8cd392` | letter-spacing: 0.03em |

---

## 4. Espaçamento e Layout

### 4.1 Raios de Borda (border-radius)

| Valor | Uso |
|---|---|
| `8px` | Botões sidebar, inputs pequenos, ícone logout |
| `9px` | Links sidebar, quick-cards admin |
| `10px` | Botões globais, ícone de avatar quick-card, admin badge |
| `12px` | Alertas, modais, log cards |
| `14px` | KPI cards, skeleton, plans-page cards |
| `16px` | Quick-cards torcedor |
| `18px` | `.app-surface` (surfaces genéricas) |
| `50%` | Avatares (círculo perfeito) |

### 4.2 Breakpoints

| Breakpoint | Aplicação |
|---|---|
| `max-width: 760px` | Reduz padding do `.app-shell`, ajusta margens |
| `max-width: 900px` | Sidebar admin colapsa para 68px, labels desaparecem, ícones apenas |
| `min-width: 640px` | Quick-grid torcedor passa de 2 para 4 colunas; bottom nav some; padding-bottom: 0 |

### 4.3 Sidebar Admin

- **Largura expandida:** `248px`
- **Largura colapsada (≤900px):** `68px`
- **Padding expandida:** `1.25rem 0.85rem`
- **Padding colapsada:** `1rem 0.5rem`

### 4.4 Header Admin

- **Altura:** `68px` (fixo)
- **Padding horizontal:** `1.5rem`
- **Sticky** com `z-index: 10` e `backdrop-filter: blur(8px)`

### 4.5 Header Torcedor

- **Padding:** `0.75rem 1rem`
- **Sticky** com `z-index: 50` e `backdrop-filter: blur(12px)`

### 4.6 Bottom Navigation (Torcedor)

- **Altura:** `64px`
- **Fixed** com `z-index: 60`
- **Suporte a safe area:** `padding: 0 0.25rem env(safe-area-inset-bottom)`
- **Itens:** 5 (Início, Notícias, Jogos, Carteirinha, Conta)
- **Itens invisíveis em ≥640px**

### 4.7 Quick-Grid Torcedor

- **Mobile (<640px):** `grid-template-columns: repeat(2, 1fr)`, `gap: 0.75rem`
- **Desktop (≥640px):** `grid-template-columns: repeat(4, 1fr)`

### 4.8 Layout de Notícias Torcedor (`.news-*`)

**Mobile (< 640px):**
- `.news-featured-area` → bloco vertical sem grid; `margin-bottom: 0.75rem`
- `.news-featured-secondary` → `display: flex; flex-direction: column; gap: 0.75rem; margin-top: 0.75rem` — empilha os cards abaixo do hero com espaçamento correto
- `.news-grid` → `grid-template-columns: 1fr 1fr; gap: 0.75rem` — 2 colunas para artigos restantes

**Desktop (≥ 640px):**
- `.news-featured-area` → `display: grid; grid-template-columns: 2fr 1fr; gap: 0.75rem` — hero à esquerda (2/3), dois cards à direita (1/3)
- `.news-featured-secondary` → `margin-top: 0` (sobrescreve mobile); `flex-direction: column; gap: 0.75rem`
- `.news-grid` → `grid-template-columns: repeat(3, 1fr)` — 3 colunas
- Bottom nav oculta; `padding-bottom: 0`

### 4.9 KPI Grid Admin

- `grid-template-columns: repeat(auto-fit, minmax(220px, 1fr))`
- `gap: 1rem`
- `max-width: 920px`

---

---

## 5. Avatares de Usuário

O **painel admin** usa avatar com iniciais no header. O **header do dashboard** (`.dash-header`) e o **cabeçalho da caixa de perfil em Conta** (`/account`, `.account-page__header`) não exibem círculo de usuário — só nome (Conta) e ações. Foto ou iniciais do torcedor em `/account` ficam no **quadrado 64px** do card (sócio ou sem plano).

### Lógica de Iniciais (onde aplicável)

```
initials = nome.split(' ').slice(0, 2).map(p => p[0]).join('').toUpperCase()
// "Ronaldo Silva" → "RS"
// "João" → "J"
```

### Avatar Admin (`.admin-shell__user-avatar`)

- **Tamanho:** `36×36px`
- **Background:** `linear-gradient(135deg, rgba(140, 211, 146, 0.2), #8cd392)`
- **Cor do texto:** `#0b0f14` (escuro — legível sobre fundo verde)
- **Peso:** 700, tamanho 0.78rem, letter-spacing: 0.04em

---

## 6. Animações e Transições

### 6.1 Tokens de Transição

| Token | Valor | Uso |
|---|---|---|
| `--transition-fast` | `120ms ease` | Interações rápidas (hover scale, arrow reveal) |
| `--transition-base` | `200ms ease` | Interações padrão (cores, opacidade) |

### 6.2 Animações Nomeadas

#### `kpi-appear` — entrada dos valores KPI
```css
@keyframes kpi-appear {
  from { opacity: 0; transform: translateY(8px); }
  to   { opacity: 1; transform: translateY(0); }
}
/* Aplicação: animation: kpi-appear 380ms cubic-bezier(0.16, 1, 0.3, 1) both */
```

#### `shimmer` — skeleton loading
```css
@keyframes shimmer {
  0%   { background-position: -600px 0; }
  100% { background-position: 600px 0; }
}
/* Background: gradiente linear 90° com 3 stops de rgba verde
   background-size: 1200px 100%
   animation: shimmer 1.5s infinite linear */
```

### 6.3 Accordion da Sidebar (técnica grid)

O accordion não usa `max-height`. Utiliza a técnica de `grid-template-rows` para animação suave:

```css
/* Fechado */
.admin-shell__nav-section-body {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 220ms ease;
}
/* Aberto */
.admin-shell__nav-section-body.is-open {
  grid-template-rows: 1fr;
}
/* O filho direto deve ter overflow: hidden */
.admin-shell__nav-section-inner {
  overflow: hidden;
}
```

Em mobile (≤900px), o accordion é sempre forçado aberto:
```css
.admin-shell__nav-section-body {
  grid-template-rows: 1fr !important;
}
```

### 6.4 Chevron do Accordion

```css
.admin-shell__nav-chevron {
  transition: transform 220ms ease;
}
.admin-shell__nav-chevron.is-open {
  transform: rotate(180deg);
}
```

### 6.5 Arrow Reveal nos Quick-Cards Admin

```css
.admin-dashboard__quick-arrow {
  opacity: 0;
  transform: translateX(-4px);
  transition: opacity 140ms ease, transform 140ms ease;
}
.admin-dashboard__quick-card:hover .admin-dashboard__quick-arrow {
  opacity: 1;
  transform: translateX(0);
}
```

### 6.6 Tap Feedback Mobile

```css
.dash-quick-card:active {
  transform: scale(0.97);
}
-webkit-tap-highlight-color: transparent; /* em todos os links/botões tocáveis */
```

---

## 7. Gradientes de Fundo

### Torcedor Home
```css
background: linear-gradient(160deg, #0e131a 0%, #0b0f14 100%);
```

### Admin Shell
```css
background:
  radial-gradient(circle at 12% 88%, rgba(140, 211, 146, 0.2), transparent 42%),
  radial-gradient(circle at 80% 0%, rgba(140, 211, 146, 0.12), transparent 38%),
  var(--admin-bg-main);
```
Dois gradientes radiais sobrepostos criam um efeito de "luz" nos cantos inferior-esquerdo e superior-direito.

### Admin Sidebar
```css
background: linear-gradient(180deg, var(--admin-bg-sidebar), #0e131a);
```

### `app-surface` (surfaces genéricas)
```css
background: linear-gradient(180deg, rgba(15, 42, 31, 0.58), rgba(11, 15, 20, 0.96));
```

---

## 8. Inputs e Formulários

```css
/* .app-input, .app-select, .app-textarea */
border-radius: 10px;
border: 1px solid rgba(255, 255, 255, 0.08);
background: rgba(14, 19, 26, 0.82);
color: #f5f7fa;
padding: 0.58rem 0.72rem;

/* Focus */
outline: 2px solid rgba(140, 211, 146, 0.42);
outline-offset: 1px;
```

---

## 9. Ícones

A biblioteca utilizada é **`lucide-react`**. Não usar outras bibliotecas de ícones.

### Mapeamento de ícones por funcionalidade

| Funcionalidade | Ícone lucide-react |
|---|---|
| Notícias | `Newspaper` |
| Jogos | `Calendar` |
| Carteirinha | `CreditCard` |
| Planos | `ShieldCheck` |
| Fidelidade | `Trophy` |
| Fidelidade — atalho na Conta (`/account`) | `ListChecks` |
| Benefícios | `Gift` |
| Ingressos | `Ticket` |
| Suporte / Chamados | `Headphones` |
| Logout | `LogOut` |
| Admin badge | `ShieldCheck` |
| Alerta | `AlertTriangle` |
| Início (bottom nav) | `Home` |
| Conta (bottom nav) | `User` |
| Chevron accordion | `ChevronDown` |
| Configurações | `Settings` |
| Voltar | `ArrowLeft` |
| Arrow reveal | `ArrowRight` |

### Tamanhos canônicos por contexto

| Contexto | Tamanho |
|---|---|
| Quick-cards torcedor | `size={20}` |
| Bottom nav torcedor | `size={22}` |
| Links sidebar admin | `size={16}` ou `18px` via `.admin-shell__link-icon` (18×18px) |
| Header admin (settings) | `size={18}` |
| Botão logout torcedor | `size={18}` |
| KPI cards admin | `size={20}` |
| Quick-actions admin | `size={18}` |
| Badges / alertas inline | `size={16}` |

---

## 10. Componentes Reutilizáveis

### 10.1 `<KpiCard>` (Admin)

```tsx
type KpiCardVariant = 'success' | 'warning' | 'info' | 'danger'

interface KpiCardProps {
  label: string       // texto da label superior
  value: string       // valor principal (ex: "142")
  hint?: string       // texto auxiliar abaixo do valor
  icon?: ReactNode    // elemento lucide-react
  variant?: KpiCardVariant
}
```

Estrutura HTML:
```
.admin-kpi-card  .admin-kpi-card--{variant}
  .admin-kpi-card__header
    .admin-kpi-card__label    ← texto uppercase
    .admin-kpi-card__icon-wrap ← ícone
  .admin-kpi-card__value      ← número grande, animated
  .admin-kpi-card__hint       ← texto auxiliar
```

### 10.2 `<KpiSkeletonCard>` (Admin)

Elemento `div.admin-kpi-skeleton` com `height: 128px` e animação shimmer. Exibido enquanto dados carregam.

### 10.3 `<SidebarSection>` (Admin Accordion)

Props:
```tsx
interface SidebarSectionProps {
  label: string           // nome da seção
  icon: ReactNode         // ícone lucide do cabeçalho
  routes: string[]        // rotas que ativam esta seção (para auto-abrir)
  children: ReactNode     // NavItems filhos
}
```

Comportamento: usa `useLocation().pathname` para detectar se alguma rota do array `routes` está ativa. Se sim, adiciona `has-active` no botão e abre automaticamente via `useEffect`.

### 10.4 `<NavItem>` (Admin Sidebar Link)

```tsx
interface NavItemProps {
  to: string
  icon: ReactNode
  label: string
}
```

Usa `<NavLink>` do react-router com classe `is-active` aplicada automaticamente.

### 10.5 Página de Jogos (`/games`) — cartão de partida

Implementação em `frontend/src/pages/GamesPage.tsx` + estilos em `frontend/src/pages/AppShell.css`.

**Layout**

- Raiz: `.games-root` (full-viewport, mesmo padrão das outras subpáginas torcedor).
- Conteúdo: `.games-page` dentro de `.subpage-content`.
- Agenda agrupada por dia local: `.games-schedule` → seções `.games-day`.
- Cabeçalho do dia: `.games-day__header` com `.games-day__date` (título `h2`, data `dd/mm/aaaa`) e, quando o dia contém o jogo em destaque, selo `.games-day__badge` (“Evento Próximo”).
- Lista do dia: `.games-day__list`.

**Cartão (`.game-card-ev`)**

- Estados: `.game-card-ev--active` (próximo jogo futuro na ordenação global, ou o primeiro da lista se todos forem passados) e `.game-card-ev--muted` (demais jogos — opacidade reduzida, CTA visualmente desabilitado).
- Corpo: `.game-card-ev__body` — título do confronto `.game-card-ev__title` (`{sigla} x {adversário}`; sigla via `VITE_CLUB_SHORT_NAME`, padrão `FFC`), subtítulo `.game-card-ev__subtitle` (`{competition} - {horário}` a partir da API).
- Logos: `.game-card-ev__logos` com escudo da casa (imagem resolvida uma vez via `GET /api/branding` + `resolvePublicAssetUrl`, fallback ao placeholder SVG), separador `.game-card-ev__vs`, logo do adversário ou `.game-card-ev__opponent-fallback` com iniciais.
- CTA somente visual: `button.game-card-ev__cta` (“Ingresso disponível”; `disabled` quando `.game-card-ev--muted`) — no estado ativo, fundo mint `#96d696` e texto escuro; no muted, estilos definidos em `.game-card-ev--muted .game-card-ev__cta`.

**Toast de sucesso (referência)**

- Classe `.game-card-ev__toast` (+ `.game-card-ev__toast-sub`) definida para alinhar a mockups / fluxos futuros; não é obrigatório renderizar na listagem estática.

---

## 11. Estrutura de Namespaces CSS

O projeto usa **Plain CSS com BEM-like naming**. Sem CSS Modules, sem Tailwind.

| Prefixo | Contexto |
|---|---|
| `--admin-*` | Tokens CSS para o painel admin (definidos em `:root` no `index.css`) |
| `.admin-shell__*` | Layout admin (sidebar, header, workspace) |
| `.admin-kpi-card*` | Componente KPI card |
| `.admin-kpi-skeleton` | Skeleton loading KPI |
| `.admin-dashboard__*` | Página dashboard admin |
| `.dash-*` | Tela home do torcedor (mobile-first) |
| `.app-shell*` | Container genérico de páginas públicas/torcedor |
| `.app-surface` | Card/container com fundo translúcido verde |
| `.btn-*` | Botões globais |
| `.app-input`, `.app-select`, `.app-textarea` | Formulários globais |
| `.plans-root`, `.plans-page__*` | Catálogo de planos (`/plans`) — full-viewport, cards verticais, CTA outline |
| `.account-root`, `.account-page__*` | Página de conta (`/account`) — wrapper full-viewport + conteúdo |
| `.news-*` | Páginas de notícias do torcedor (`/news` e `/news/:id`) |
| `.games-page__*`, `.games-schedule`, `.games-day__*`, `.game-card-ev*` | Página de jogos (`/games`) — cartões de partida por dia |
| `.support-root`, `.support-detail-root`, `.support-ticket-card`, `.support-empty`, `.support-new-btn`, `.support-form*`, `.support-msg-card`, `.support-status-badge*` | Chamados do torcedor (`/support`, `/support/:ticketId`) — mesmo gradiente full-viewport que `.tickets-root`, header `.subpage-header`, estado vazio com CTA outline mint (`#66bb6a`), lista em cards escuros, formulário em `.support-form-panel`; botão de ação no header: `.subpage-header__badge-btn` (ícone engrenagem) |

---

## 12. Overflow de Viewport — Regra Crítica

O `#root` padrão tem `width: 1126px` para páginas públicas. Páginas que devem ocupar 100% da viewport (admin e torcedor home) **escapam esse container** via `:has()`:

```css
/* Ambas as linhas vivem em src/index.css */

#root:has(.admin-shell) {
  width: 100vw;
  max-width: 100vw;
  margin: 0;
  border-inline: none;
  padding: 0;
}

#root:has(.dash-root) {
  width: 100vw;
  max-width: 100vw;
  margin: 0;
  border-inline: none;
  padding: 0;
}

#root:has(.news-root) {
  width: 100vw;
  max-width: 100vw;
  margin: 0;
  border-inline: none;
  padding: 0;
}

/* Conta (`/account`): `.account-root`; planos (`/plans`): `.plans-root`; chamados (`/support`): `.support-root` e `.support-detail-root` — ver `src/index.css` (mesmo bloco `:has()` das subpáginas torcedor). */
```

**Regra:** toda nova tela que precisar de full-viewport deve adicionar a classe raiz correspondente como seletor `:has()` em `src/index.css` (ex.: `.account-root`, `.plans-root`, `.support-root`).

---

## 13. Bottom Navigation — Comportamento

- Visível apenas em mobile (`< 640px`)
- 5 itens fixos: Início (`/`), Notícias (`/news`), Jogos (`/games`), Carteirinha (`/digital-card`), Conta (`/account`); **Benefícios** não entram na barra — acesso em Conta (`/account`) pelo botão “Meus benefícios” (`/benefits`); **Fidelidade** na mesma tela — botão “Fidelidade” para `/loyalty` (mesmo estilo de cartão que “Meus benefícios”, ícone `ListChecks`); cabeçalho da Conta (`.account-page__header`) com nome e configurações
- Usa `<NavLink>` com classe `active` aplicada via callback
- Altura 64px + `env(safe-area-inset-bottom)` para iPhone X+
- O `.dash-root` tem `padding-bottom: 68px` para não ficar atrás da barra

---

## 14. Admin Sidebar — Comportamento Responsivo

| Largura tela | Comportamento |
|---|---|
| `> 900px` | Sidebar expandida (248px): labels visíveis, chevrons visíveis, accordion funcional |
| `≤ 900px` | Sidebar colapsada (68px): apenas ícones, todos os sections sempre abertos, chevrons ocultos |

Auto-abertura de seção: baseada em `useLocation().pathname` + lista de `routes` da seção. Quando o pathname inclui alguma rota da lista, a seção abre. Isso é implementado com `useEffect` que observa `isActive`.

---

## 15. Paleta Resumida — Referência Rápida

```
VERDE MAIS ESCURO (fundos)     #0b0f14
VERDE ESCURO BASE              #0e131a
VERDE ESCURO SIDEBAR           #0f2a1f
VERDE ACENTO LIMÃO             #8cd392
VERDE ACENTO SUAVE             rgba(140,211,146,0.2)

TEXTO PRINCIPAL                #f5f7fa
TEXTO SECUNDÁRIO               #a6b0bf
TEXTO INATIVO                  #7b8794

BORDA SUAVE                    rgba(255,255,255,0.08)
BORDA FORTE                    rgba(140,211,146,0.42)

WARNING KPI                    #f59e0b
INFO KPI                       #60a5fa
DANGER KPI                     #f87171

BTN PRIMARY                    transparent + #8cd392
BTN DANGER                     #8d2c34
```
