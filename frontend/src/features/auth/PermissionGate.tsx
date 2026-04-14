import { type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { hasAnyPermission } from '../../shared/auth/permissionUtils'
import { useAuth } from './AuthContext'

export function PermissionGate({
  anyOf,
  children,
}: {
  anyOf: readonly string[]
  children: ReactNode
}) {
  const { user, loading } = useAuth()

  if (loading)
    return <p>Carregando...</p>

  if (!user)
    return null

  if (!hasAnyPermission(user, anyOf)) {
    return (
      <section style={{ marginTop: '1rem' }}>
        <p>Sem permissão para esta seção.</p>
        <p>
          <Link to="/admin">Voltar ao painel administrativo</Link>
        </p>
      </section>
    )
  }

  return <>{children}</>
}
