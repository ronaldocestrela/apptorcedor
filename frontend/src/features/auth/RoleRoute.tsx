import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RoleRoute({ roles }: { roles: string[] }) {
  const { user, loading } = useAuth()

  if (loading)
    return <p>Carregando...</p>

  if (!user)
    return <Navigate to="/login" replace />

  const ok = roles.some((r) => user.roles.includes(r))
  if (!ok)
    return <Navigate to="/" replace />

  return <Outlet />
}
