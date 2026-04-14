import { Navigate, Outlet } from 'react-router-dom'
import { ADMIN_AREA_PERMISSIONS } from '../../shared/auth/applicationPermissions'
import { hasAnyPermission } from '../../shared/auth/permissionUtils'
import { useAuth } from './AuthContext'

/**
 * Allows access if the user has any of the given permissions.
 * Default `anyOf` is the set that can see at least one admin menu entry.
 */
export function PermissionRoute({
  anyOf = ADMIN_AREA_PERMISSIONS,
}: {
  anyOf?: readonly string[]
}) {
  const { user, loading } = useAuth()

  if (loading)
    return <p>Carregando...</p>

  if (!user)
    return <Navigate to="/login" replace />

  if (!hasAnyPermission(user, anyOf))
    return <Navigate to="/" replace />

  return <Outlet />
}
