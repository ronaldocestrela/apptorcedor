import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ThemeToggle } from '../theme/ThemeToggle'

export function AppShell() {
  const { logout, roles } = useAuth()
  const isAdmin = roles.includes('Administrador')
  const [navOpen, setNavOpen] = useState(false)

  useEffect(() => {
    const mq = window.matchMedia('(min-width: 601px)')
    function onChange() {
      if (mq.matches) {
        setNavOpen(false)
      }
    }
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [])

  function closeNav() {
    setNavOpen(false)
  }

  return (
    <div className={`app-shell${navOpen ? ' app-shell--nav-open' : ''}`}>
      <header className="app-shell__header">
        <div className="app-shell__bar">
          <strong className="app-shell__brand">Sócio Torcedor</strong>
          <nav className="app-shell__nav" aria-label="Áreas">
            {isAdmin ? (
              <NavLink
                to="/admin"
                end
                className={({ isActive }) =>
                  isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
                }
                onClick={closeNav}
              >
                Admin
              </NavLink>
            ) : null}
            {isAdmin ? (
              <NavLink
                to="/admin/plans"
                className={({ isActive }) =>
                  isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
                }
                onClick={closeNav}
              >
                Planos
              </NavLink>
            ) : null}
            {isAdmin ? (
              <NavLink
                to="/admin/billing"
                className={({ isActive }) =>
                  isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
                }
                onClick={closeNav}
              >
                Faturamento SaaS
              </NavLink>
            ) : null}
            {isAdmin ? (
              <NavLink
                to="/admin/stripe"
                className={({ isActive }) =>
                  isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
                }
                onClick={closeNav}
              >
                Gateway
              </NavLink>
            ) : null}
            <NavLink
              to="/member"
              end
              className={({ isActive }) =>
                isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
              }
              onClick={closeNav}
            >
              Sócio
            </NavLink>
            <NavLink
              to="/member/billing"
              className={({ isActive }) =>
                isActive ? 'app-shell__link app-shell__link--active' : 'app-shell__link'
              }
              onClick={closeNav}
            >
              Pagamentos
            </NavLink>
            <button
              type="button"
              className="app-shell__logout"
              onClick={() => {
                closeNav()
                logout()
              }}
            >
              Sair
            </button>
          </nav>
          <div className="app-shell__toolbar">
            <ThemeToggle />
            <button
              type="button"
              className="app-shell__menu-toggle"
              aria-label={navOpen ? 'Fechar menu' : 'Abrir menu'}
              aria-expanded={navOpen}
              onClick={() => setNavOpen((open) => !open)}
            >
              <span className="app-shell__menu-icon" aria-hidden />
            </button>
          </div>
        </div>
      </header>
      <main className="app-shell__main">
        <Outlet />
      </main>
    </div>
  )
}
