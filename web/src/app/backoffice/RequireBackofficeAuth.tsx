import { Navigate, Outlet } from 'react-router-dom'
import { getBackofficeApiKey } from '../../shared/backoffice/backofficeSession'

export function RequireBackofficeAuth() {
  if (!getBackofficeApiKey()) {
    return <Navigate to="/backoffice/login" replace />
  }
  return <Outlet />
}
