import { NavLink, Outlet } from 'react-router-dom'

export function AppShell() {
  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <strong className="app-shell__brand">Sócio Torcedor</strong>
        <nav className="app-shell__nav" aria-label="Áreas">
          <NavLink
            to="/admin"
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Admin
          </NavLink>
          <NavLink
            to="/member"
            className={({ isActive }) =>
              isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
            }
          >
            Sócio
          </NavLink>
        </nav>
      </header>
      <main className="app-shell__main">
        <Outlet />
      </main>
    </div>
  )
}
