import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { ThemeToggle } from '../theme/ThemeToggle'
import { clearBackofficeSession } from '../../shared/backoffice/backofficeSession'

export function BackofficeShell() {
  const navigate = useNavigate()

  function logout() {
    clearBackofficeSession()
    navigate('/backoffice/login', { replace: true })
  }

  return (
    <div className="bo-shell">
      <aside className="bo-shell__aside" aria-label="Navegação backoffice">
        <div className="bo-shell__brand">
          <strong>Backoffice</strong>
          <span className="bo-shell__brand-sub">Plataforma SaaS</span>
        </div>
        <nav className="bo-shell__nav">
          <NavLink
            to="/backoffice"
            end
            className={({ isActive }) => (isActive ? 'bo-shell__link bo-shell__link--active' : 'bo-shell__link')}
          >
            Início
          </NavLink>
          <NavLink
            to="/backoffice/tenants"
            className={({ isActive }) => (isActive ? 'bo-shell__link bo-shell__link--active' : 'bo-shell__link')}
          >
            Tenants
          </NavLink>
          <NavLink
            to="/backoffice/plans"
            className={({ isActive }) => (isActive ? 'bo-shell__link bo-shell__link--active' : 'bo-shell__link')}
          >
            Planos SaaS
          </NavLink>
          <NavLink
            to="/backoffice/tenant-plans"
            className={({ isActive }) => (isActive ? 'bo-shell__link bo-shell__link--active' : 'bo-shell__link')}
          >
            Vínculos
          </NavLink>
        </nav>
        <div className="bo-shell__aside-footer">
          <button type="button" className="bo-shell__logout" onClick={logout}>
            Sair da chave API
          </button>
        </div>
      </aside>
      <div className="bo-shell__main-wrap">
        <header className="bo-shell__topbar">
          <ThemeToggle />
        </header>
        <main className="bo-shell__main">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
