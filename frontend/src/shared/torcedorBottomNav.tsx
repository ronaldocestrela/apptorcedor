import { NavLink } from 'react-router-dom'
import { Calendar, CreditCard, Gift, Home, Newspaper, User } from 'lucide-react'

const ITEMS = [
  { to: '/', label: 'Início', icon: Home, end: true as const },
  { to: '/news', label: 'Notícias', icon: Newspaper, end: false as const },
  { to: '/games', label: 'Jogos', icon: Calendar, end: false as const },
  { to: '/digital-card', label: 'Carteirinha', icon: CreditCard, end: false as const },
  { to: '/benefits', label: 'Benefícios', icon: Gift, end: false as const },
  { to: '/account', label: 'Conta', icon: User, end: false as const },
] as const

export function TorcedorBottomNav() {
  return (
    <nav className="dash-bottom-nav" aria-label="Navegação principal">
      {ITEMS.map(({ to, label, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end ?? false}
          className={({ isActive }) =>
            `dash-bottom-nav__item${isActive ? ' active' : ''}`
          }
        >
          <Icon size={22} />
          {label}
        </NavLink>
      ))}
    </nav>
  )
}
