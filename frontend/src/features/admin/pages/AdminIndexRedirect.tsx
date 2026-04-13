import { Navigate } from 'react-router-dom'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'

export function AdminIndexRedirect() {
  const { user, loading } = useAuth()

  if (loading)
    return <p>Carregando...</p>

  if (!user)
    return <Navigate to="/login" replace />

  if (hasPermission(user, ApplicationPermissions.UsuariosVisualizar)
    || hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar))
    return <Navigate to="dashboard" replace />
  if (hasPermission(user, ApplicationPermissions.AdministracaoDiagnostics))
    return <Navigate to="diagnostics" replace />
  if (hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar))
    return <Navigate to="configurations" replace />
  if (hasPermission(user, ApplicationPermissions.SociosGerenciar))
    return <Navigate to="membership" replace />

  return <p>Nenhuma seção disponível para seu usuário.</p>
}
