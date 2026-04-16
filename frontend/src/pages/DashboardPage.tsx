import { Link } from 'react-router-dom'
import {
  Newspaper,
  Calendar,
  CreditCard,
  Trophy,
  Gift,
  Ticket,
  Headphones,
  LogOut,
  ShieldCheck,
  AlertTriangle,
} from 'lucide-react'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import { ADMIN_AREA_PERMISSIONS } from '../shared/auth/applicationPermissions'
import { canAccessAdminArea } from '../shared/auth/permissionUtils'
import { useAuth } from '../features/auth/AuthContext'
import './AppShell.css'

const QUICK_LINKS = [
  { to: '/news', label: 'Notícias', icon: <Newspaper size={20} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={20} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={20} /> },
  { to: '/plans', label: 'Planos', icon: <ShieldCheck size={20} /> },
  { to: '/loyalty', label: 'Fidelidade', icon: <Trophy size={20} /> },
  { to: '/benefits', label: 'Benefícios', icon: <Gift size={20} /> },
  { to: '/tickets', label: 'Ingressos', icon: <Ticket size={20} /> },
  { to: '/support', label: 'Chamados', icon: <Headphones size={20} /> },
]

function UserAvatar({ name }: { name: string }) {
  const initials = name
    .split(' ')
    .slice(0, 2)
    .map(p => p[0])
    .join('')
    .toUpperCase()
  return <span className="dash-avatar">{initials}</span>
}

export function DashboardPage() {
  const { user, logout } = useAuth()
  const showAdmin = canAccessAdminArea(user, ADMIN_AREA_PERMISSIONS)
  const firstName = user?.name?.split(' ')[0] ?? ''

  return (
    <div className="dash-root">
      <header className="dash-header">
        <span className="dash-header__logo-text">AppTorcedor</span>
        <div className="dash-header__right">
          {user?.name ? <UserAvatar name={user.name} /> : null}
          <button
            type="button"
            className="dash-header__logout"
            aria-label="Sair"
            onClick={() => void logout()}
          >
            <LogOut size={18} />
          </button>
        </div>
      </header>

      <main className="dash-content">
        <div className="dash-hero">
          <p className="dash-hero__greeting">Olá,</p>
          <h1 className="dash-hero__name">{firstName}</h1>
        </div>

        {user?.requiresProfileCompletion ? (
          <Link to="/account" className="dash-alert">
            <AlertTriangle size={16} />
            Complete seu perfil para liberar todas as funcionalidades.
          </Link>
        ) : null}

        {showAdmin ? (
          <Link to="/admin" className="dash-admin-badge">
            <ShieldCheck size={16} />
            Painel administrativo
          </Link>
        ) : null}

        <p className="dash-section-title">Acessos rápidos</p>
        <nav className="dash-quick-grid" aria-label="Acessos rápidos">
          {QUICK_LINKS.map(link => (
            <Link key={link.to} to={link.to} className="dash-quick-card">
              <span className="dash-quick-card__icon">{link.icon}</span>
              <span className="dash-quick-card__label">{link.label}</span>
            </Link>
          ))}
        </nav>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
