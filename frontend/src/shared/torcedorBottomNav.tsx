import { NavLink } from 'react-router-dom'
import { Calendar, CreditCard, Gift, Home, Newspaper, User } from 'lucide-react'

const ITEMS = [
  { to: '/', label: 'Início', icon: Home, end: true as const },
  { to: '/news', label: 'Notícias', icon: Newspaper },
  { to: '/games', label: 'Jogos', icon: Calendar },
  { to: '/digital-card', label: 'Carteirinha', icon: CreditCard },
  { to: '/benefits', label: 'Benefícios', icon: Gift },
  { to: '/account', label: 'Conta', icon: User },
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
