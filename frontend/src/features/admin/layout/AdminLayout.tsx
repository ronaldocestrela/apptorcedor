import { NavLink, Outlet } from 'react-router-dom'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import './AdminLayout.css'

export function AdminLayout() {
  const { user } = useAuth()

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
          {(hasPermission(user, ApplicationPermissions.UsuariosVisualizar)
            || hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar)) ? (
            <NavLink to="dashboard" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Painel</NavLink>
            ) : null}
          {hasPermission(user, ApplicationPermissions.UsuariosVisualizar) ? (
            <NavLink to="staff" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Staff</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.UsuariosVisualizar) ? (
            <NavLink to="users" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Usuários</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.AdministracaoDiagnostics) ? (
            <NavLink to="diagnostics" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Diagnóstico</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="configurations" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Configurações</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="audit-logs" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Auditoria</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="role-permissions" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Roles × Permissões</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.SociosGerenciar) ? (
            <NavLink to="membership" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Membership</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.PlanosVisualizar)
            || hasPermission(user, ApplicationPermissions.PlanosCriar)
            || hasPermission(user, ApplicationPermissions.PlanosEditar)) ? (
            <NavLink to="plans" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Planos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
            || hasPermission(user, ApplicationPermissions.PagamentosGerenciar)
            || hasPermission(user, ApplicationPermissions.PagamentosEstornar)) ? (
            <NavLink to="payments" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Pagamentos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.CarteirinhaVisualizar)
            || hasPermission(user, ApplicationPermissions.CarteirinhaGerenciar)) ? (
            <NavLink to="digital-cards" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Carteirinha</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.JogosVisualizar)
            || hasPermission(user, ApplicationPermissions.JogosCriar)
            || hasPermission(user, ApplicationPermissions.JogosEditar)) ? (
            <NavLink to="games" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Jogos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.IngressosVisualizar)
            || hasPermission(user, ApplicationPermissions.IngressosGerenciar)) ? (
            <NavLink to="tickets" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Ingressos</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.NoticiasPublicar) ? (
            <NavLink to="news" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Notícias</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.FidelidadeVisualizar)
            || hasPermission(user, ApplicationPermissions.FidelidadeGerenciar)) ? (
            <NavLink to="loyalty" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Fidelidade</NavLink>
            ) : null}
          {(hasPermission(user, ApplicationPermissions.BeneficiosVisualizar)
            || hasPermission(user, ApplicationPermissions.BeneficiosGerenciar)) ? (
            <NavLink to="benefits" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Benefícios</NavLink>
            ) : null}
          {hasPermission(user, ApplicationPermissions.ChamadosResponder) ? (
            <NavLink to="support" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>Chamados</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.LgpdDocumentosVisualizar) ? (
            <NavLink to="lgpd/documents" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>LGPD — Documentos</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.LgpdConsentimentosVisualizar) ? (
            <NavLink to="lgpd/consents" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>LGPD — Consentimentos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.LgpdDadosExportar)
            || hasPermission(user, ApplicationPermissions.LgpdDadosAnonimizar)) ? (
            <NavLink to="lgpd/privacy" className={({ isActive }) => `admin-shell__link${isActive ? ' is-active' : ''}`}>LGPD — Dados</NavLink>
          ) : null}
        </nav>
        <div className="admin-shell__sidebar-footer">
          <NavLink to="/" className="admin-shell__back-link">Início</NavLink>
        </div>
      </aside>
      <section className="admin-shell__workspace">
        <header className="admin-shell__header">
          <div className="admin-shell__header-badge">Administrador</div>
          <button type="button" className="admin-shell__settings-button" aria-label="Abrir configurações rápidas">Config</button>
        </header>
        <main className="admin-shell__content">
          <Outlet />
        </main>
      </section>
    </div>
  )
}
