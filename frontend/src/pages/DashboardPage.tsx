import { Link } from 'react-router-dom'
import { ADMIN_AREA_PERMISSIONS } from '../shared/auth/applicationPermissions'
import { canAccessAdminArea } from '../shared/auth/permissionUtils'
import { useAuth } from '../features/auth/AuthContext'
import './AppShell.css'

export function DashboardPage() {
  const { user, logout } = useAuth()
  const showAdmin = canAccessAdminArea(user, ADMIN_AREA_PERMISSIONS)
  const quickLinks = [
    { to: '/account', label: 'Minha conta' },
    { to: '/news', label: 'Notícias' },
    { to: '/benefits', label: 'Benefícios' },
    { to: '/plans', label: 'Planos' },
    { to: '/digital-card', label: 'Carteirinha' },
    { to: '/games', label: 'Jogos' },
    { to: '/tickets', label: 'Ingressos' },
    { to: '/loyalty', label: 'Fidelidade' },
    { to: '/support', label: 'Chamados' },
  ]

  return (
    <main className="app-shell app-shell--wide dashboard-page">
      <section className="app-surface dashboard-page__hero">
        <div>
          <h1 className="app-title">Área autenticada</h1>
          <p className="app-muted">
            <strong>{user?.name}</strong>
            {' '}
            ({user?.email})
          </p>
          <p className="app-muted">Perfis: {user?.roles.join(', ')}</p>
          <p className="app-muted">Permissões: {user?.permissions.length ? user.permissions.join(', ') : '—'}</p>
          {user?.requiresProfileCompletion ? (
            <p className="account-page__alert-warning">Perfil incompleto. Complete seus dados para liberar todas as jornadas.</p>
          ) : null}
          {showAdmin ? (
            <p>
              <Link to="/admin" className="app-back-link">Painel administrativo</Link>
            </p>
          ) : null}
        </div>
        <div>
          <button type="button" className="btn-secondary" onClick={() => void logout()}>
            Sair
          </button>
        </div>
      </section>

      <section className="dashboard-page__links-grid" aria-label="Acessos rápidos">
        {quickLinks.map(link => (
          <Link key={link.to} to={link.to} className="dashboard-page__link-card">
            {link.label}
          </Link>
        ))}
      </section>

      <p style={{ marginTop: 12 }}>
        <Link to="/account" className="app-back-link">Minha conta</Link>
      </p>
    </main>
  )
}
