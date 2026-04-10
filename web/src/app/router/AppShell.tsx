import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function AppShell() {
  const { logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <strong className="app-shell__brand">Sócio Torcedor</strong>
        <nav className="app-shell__nav" aria-label="Áreas">
          <NavLink
            to="/admin"
            end
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Admin
          </NavLink>
          <NavLink
            to="/admin/billing"
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Faturamento SaaS
          </NavLink>
          <NavLink
            to="/member"
            end
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Sócio
          </NavLink>
          <NavLink
            to="/member/billing"
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Pagamentos
          </NavLink>
          <button type="button" className="app-shell__logout" onClick={logout}>
            Sair
          </button>
        </nav>
      </header>
      <main className="app-shell__main">
        <Outlet />
      </main>
    </div>
  )
}
