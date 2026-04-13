import { Link } from 'react-router-dom'
import { ADMIN_AREA_PERMISSIONS } from '../shared/auth/applicationPermissions'
import { canAccessAdminArea } from '../shared/auth/permissionUtils'
import { useAuth } from '../features/auth/AuthContext'

export function DashboardPage() {
  const { user, logout } = useAuth()
  const showAdmin = canAccessAdminArea(user, ADMIN_AREA_PERMISSIONS)

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>Área autenticada</h1>
      <p>
        <strong>{user?.name}</strong> ({user?.email})
      </p>
      <p>Perfis: {user?.roles.join(', ')}</p>
      <p>Permissões: {user?.permissions.length ? user.permissions.join(', ') : '—'}</p>
      {showAdmin ? (
        <p>
          <Link to="/admin">Painel administrativo</Link>
        </p>
      ) : null}
      <p>
        <Link to="/account">Minha conta</Link>
        {user?.requiresProfileCompletion ? (
          <span style={{ marginLeft: 8, color: '#856404' }}>(perfil incompleto)</span>
        ) : null}
      </p>
      <p>
        <Link to="/news">Notícias</Link>
        {' · '}
        <Link to="/benefits">Benefícios</Link>
        {' · '}
        <Link to="/plans">Planos</Link>
        {' · '}
        <Link to="/digital-card">Carteirinha</Link>
        {' · '}
        <Link to="/games">Jogos</Link>
        {' · '}
        <Link to="/tickets">Ingressos</Link>
        {' · '}
        <Link to="/loyalty">Fidelidade</Link>
        {' · '}
        <Link to="/support">Chamados</Link>
      </p>
      <p>
        <button type="button" onClick={() => void logout()}>
          Sair
        </button>
      </p>
    </main>
  )
}
