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
  if (hasPermission(user, ApplicationPermissions.PlanosVisualizar)
    || hasPermission(user, ApplicationPermissions.PlanosCriar)
    || hasPermission(user, ApplicationPermissions.PlanosEditar))
    return <Navigate to="plans" replace />
  if (hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
    || hasPermission(user, ApplicationPermissions.PagamentosGerenciar)
    || hasPermission(user, ApplicationPermissions.PagamentosEstornar))
    return <Navigate to="payments" replace />
  if (hasPermission(user, ApplicationPermissions.CarteirinhaVisualizar)
    || hasPermission(user, ApplicationPermissions.CarteirinhaGerenciar))
    return <Navigate to="digital-cards" replace />
  if (hasPermission(user, ApplicationPermissions.JogosVisualizar)
    || hasPermission(user, ApplicationPermissions.JogosCriar)
    || hasPermission(user, ApplicationPermissions.JogosEditar))
    return <Navigate to="games" replace />
  if (hasPermission(user, ApplicationPermissions.IngressosVisualizar)
    || hasPermission(user, ApplicationPermissions.IngressosGerenciar))
    return <Navigate to="tickets" replace />

  return <p>Nenhuma seção disponível para seu usuário.</p>
}
