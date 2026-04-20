import { useState, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import {
  LayoutDashboard,
  Activity,
  UserCog,
  Users,
  BadgeCheck,
  Layers,
  Receipt,
  CreditCard,
  CalendarDays,
  Ticket,
  Newspaper,
  Trophy,
  Gift,
  Headphones,
  Settings,
  ClipboardList,
  ShieldCheck,
  Scale,
  FileCheck,
  Database,
  Home,
  Settings2,
  ChevronDown,
} from 'lucide-react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import './AdminLayout.css'

const ICON_SIZE = 18

function NavItem({ to, icon, label }: { to: string; icon: ReactNode; label: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}
    >
      <span className="admin-shell__link-icon">{icon}</span>
      <span className="admin-shell__link-label">{label}</span>
    </NavLink>
  )
}

function SidebarSection({
  label,
  routes,
  children,
}: {
  label: string
  routes: string[]
  children: ReactNode
}) {
  const { pathname } = useLocation()
  const isActive = routes.some((r) => pathname.includes('/' + r))
  const [isOpen, setIsOpen] = useState(isActive)
  const isExpanded = isActive || isOpen

  return (
    <div className="admin-shell__nav-section">
      <button
        type="button"
        className={`admin-shell__nav-section-btn${isActive ? ' has-active' : ''}`}
        onClick={() => setIsOpen((o) => !o)}
        aria-expanded={isExpanded}
      >
        <span className="admin-shell__nav-section-label">{label}</span>
        <ChevronDown
          size={14}
          className={`admin-shell__nav-chevron${isExpanded ? ' is-open' : ''}`}
        />
      </button>
      <div className={`admin-shell__nav-section-body${isExpanded ? ' is-open' : ''}`}>
        <div className="admin-shell__nav-section-inner">{children}</div>
      </div>
    </div>
  )
}

function UserAvatar({ name }: { name: string }) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0].toUpperCase())
    .join('')
  return (
    <span className="admin-shell__user-avatar" aria-hidden="true">
      {initials}
    </span>
  )
}

export function AdminLayout() {
  const { user } = useAuth()
  const firstRole = user?.roles?.[0] ?? 'Administrador'

  return (
    <div className="admin-shell">
      <aside className="admin-shell__sidebar">
        <div className="admin-shell__brand">
          <img
            className="admin-shell__brand-logo"
            src="/logos/ESCUDO_FFC_PNG.png"
            alt="AFC"
          />
        </div>
        <nav className="admin-shell__nav" aria-label="Menu administrativo">

          {/* ── Painel ─────────────────────────────── */}
          <SidebarSection label="Painel" routes={['dashboard', 'diagnostics']}>
            {(hasPermission(user, ApplicationPermissions.UsuariosVisualizar)
              || hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar)) ? (
              <NavItem to="dashboard" icon={<LayoutDashboard size={ICON_SIZE} />} label="Painel" />
            ) : null}
            {hasPermission(user, ApplicationPermissions.AdministracaoDiagnostics) ? (
              <NavItem to="diagnostics" icon={<Activity size={ICON_SIZE} />} label="Diagnóstico" />
            ) : null}
          </SidebarSection>

          {/* ── Usuários ───────────────────────────── */}
          {hasPermission(user, ApplicationPermissions.UsuariosVisualizar) ? (
            <SidebarSection label="Usuários" routes={['staff', 'users']}>
              <NavItem to="staff" icon={<UserCog size={ICON_SIZE} />} label="Staff" />
              <NavItem to="users" icon={<Users size={ICON_SIZE} />} label="Usuários" />
            </SidebarSection>
          ) : null}

          {/* ── Financeiro ─────────────────────────── */}
          {(hasPermission(user, ApplicationPermissions.SociosGerenciar)
            || hasPermission(user, ApplicationPermissions.PlanosVisualizar)
            || hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
            || hasPermission(user, ApplicationPermissions.PlanosCriar)
            || hasPermission(user, ApplicationPermissions.PagamentosGerenciar)) ? (
            <SidebarSection label="Financeiro" routes={['membership', 'plans', 'payments']}>
              {hasPermission(user, ApplicationPermissions.SociosGerenciar) ? (
                <NavItem to="membership" icon={<BadgeCheck size={ICON_SIZE} />} label="Membership" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.PlanosVisualizar)
                || hasPermission(user, ApplicationPermissions.PlanosCriar)
                || hasPermission(user, ApplicationPermissions.PlanosEditar)) ? (
                <NavItem to="plans" icon={<Layers size={ICON_SIZE} />} label="Planos" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
                || hasPermission(user, ApplicationPermissions.PagamentosGerenciar)
                || hasPermission(user, ApplicationPermissions.PagamentosEstornar)) ? (
                <NavItem to="payments" icon={<Receipt size={ICON_SIZE} />} label="Pagamentos" />
              ) : null}
            </SidebarSection>
          ) : null}

          {/* ── Conteúdo ───────────────────────────── */}
          {(hasPermission(user, ApplicationPermissions.CarteirinhaVisualizar)
            || hasPermission(user, ApplicationPermissions.JogosVisualizar)
            || hasPermission(user, ApplicationPermissions.IngressosVisualizar)
            || hasPermission(user, ApplicationPermissions.NoticiasPublicar)
            || hasPermission(user, ApplicationPermissions.FidelidadeVisualizar)
            || hasPermission(user, ApplicationPermissions.BeneficiosVisualizar)
            || hasPermission(user, ApplicationPermissions.ChamadosResponder)) ? (
            <SidebarSection label="Conteúdo" routes={['digital-cards', 'games', 'tickets', 'news', 'loyalty', 'benefits', 'support']}>
              {(hasPermission(user, ApplicationPermissions.CarteirinhaVisualizar)
                || hasPermission(user, ApplicationPermissions.CarteirinhaGerenciar)) ? (
                <NavItem to="digital-cards" icon={<CreditCard size={ICON_SIZE} />} label="Carteirinha" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.JogosVisualizar)
                || hasPermission(user, ApplicationPermissions.JogosCriar)
                || hasPermission(user, ApplicationPermissions.JogosEditar)) ? (
                <NavItem to="games" icon={<CalendarDays size={ICON_SIZE} />} label="Jogos" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.IngressosVisualizar)
                || hasPermission(user, ApplicationPermissions.IngressosGerenciar)) ? (
                <NavItem to="tickets" icon={<Ticket size={ICON_SIZE} />} label="Ingressos" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.NoticiasPublicar) ? (
                <NavItem to="news" icon={<Newspaper size={ICON_SIZE} />} label="Notícias" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.FidelidadeVisualizar)
                || hasPermission(user, ApplicationPermissions.FidelidadeGerenciar)) ? (
                <NavItem to="loyalty" icon={<Trophy size={ICON_SIZE} />} label="Fidelidade" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.BeneficiosVisualizar)
                || hasPermission(user, ApplicationPermissions.BeneficiosGerenciar)) ? (
                <NavItem to="benefits" icon={<Gift size={ICON_SIZE} />} label="Benefícios" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.ChamadosResponder) ? (
                <NavItem to="support" icon={<Headphones size={ICON_SIZE} />} label="Chamados" />
              ) : null}
            </SidebarSection>
          ) : null}

          {/* ── Sistema ────────────────────────────── */}
          {(hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar)
            || hasPermission(user, ApplicationPermissions.LgpdDocumentosVisualizar)
            || hasPermission(user, ApplicationPermissions.LgpdConsentimentosVisualizar)
            || hasPermission(user, ApplicationPermissions.LgpdDadosExportar)
            || hasPermission(user, ApplicationPermissions.LgpdDadosAnonimizar)) ? (
            <SidebarSection label="Sistema" routes={['configurations', 'audit-logs', 'role-permissions', 'lgpd']}>
              {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
                <NavItem to="configurations" icon={<Settings size={ICON_SIZE} />} label="Configurações" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
                <NavItem to="audit-logs" icon={<ClipboardList size={ICON_SIZE} />} label="Auditoria" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
                <NavItem to="role-permissions" icon={<ShieldCheck size={ICON_SIZE} />} label="Roles × Permissões" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.LgpdDocumentosVisualizar) ? (
                <NavItem to="lgpd/documents" icon={<Scale size={ICON_SIZE} />} label="LGPD — Documentos" />
              ) : null}
              {hasPermission(user, ApplicationPermissions.LgpdConsentimentosVisualizar) ? (
                <NavItem to="lgpd/consents" icon={<FileCheck size={ICON_SIZE} />} label="LGPD — Consentimentos" />
              ) : null}
              {(hasPermission(user, ApplicationPermissions.LgpdDadosExportar)
                || hasPermission(user, ApplicationPermissions.LgpdDadosAnonimizar)) ? (
                <NavItem to="lgpd/privacy" icon={<Database size={ICON_SIZE} />} label="LGPD — Dados" />
              ) : null}
            </SidebarSection>
          ) : null}

        </nav>
        <div className="admin-shell__sidebar-footer">
          <NavLink to="/" className="admin-shell__back-link">
            <Home size={15} />
            <span>Início</span>
          </NavLink>
        </div>
      </aside>

      <section className="admin-shell__workspace">
        <header className="admin-shell__header">
          <div className="admin-shell__header-left" />
          <div className="admin-shell__header-right">
            {user ? (
              <div className="admin-shell__user-info">
                <UserAvatar name={user.name} />
                <div className="admin-shell__user-details">
                  <span className="admin-shell__user-name">{user.name}</span>
                  <span className="admin-shell__user-role">{firstRole}</span>
                </div>
              </div>
            ) : null}
            <NavLink
              to="configurations"
              className="admin-shell__settings-button"
              aria-label="Configurações"
            >
              <Settings2 size={18} />
            </NavLink>
          </div>
        </header>
        <main className="admin-shell__content">
          <Outlet />
        </main>
      </section>
    </div>
  )
}
