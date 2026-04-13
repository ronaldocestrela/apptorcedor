import { useAuth } from '../features/auth/AuthContext'
import { Link } from 'react-router-dom'

export function DashboardPage() {
  const { user, logout } = useAuth()
  const isMaster = user?.roles.includes('Administrador Master')

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>Área autenticada</h1>
      <p>
        <strong>{user?.name}</strong> ({user?.email})
      </p>
      <p>Perfis: {user?.roles.join(', ')}</p>
      {isMaster ? (
        <p>
          <Link to="/admin">Área somente Administrador Master</Link>
        </p>
      ) : null}
      <p>
        <button type="button" onClick={() => void logout()}>
          Sair
        </button>
      </p>
    </main>
  )
}
