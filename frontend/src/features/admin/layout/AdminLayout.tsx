import type { CSSProperties } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'

const linkStyle = ({ isActive }: { isActive: boolean }): CSSProperties => ({
  marginRight: 12,
  fontWeight: isActive ? 600 : 400,
  color: isActive ? '#0b57d0' : '#1a1a1a',
})

export function AdminLayout() {
  const { user } = useAuth()

  return (
    <div style={{ display: 'flex', minHeight: '100vh', fontFamily: 'system-ui' }}>
      <aside
        style={{
          width: 220,
          borderRight: '1px solid #ddd',
          padding: '1rem',
          background: '#f8f9fa',
        }}
      >
        <h2 style={{ fontSize: '1rem', marginTop: 0 }}>Admin</h2>
        <nav style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {(hasPermission(user, ApplicationPermissions.UsuariosVisualizar)
            || hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar)) ? (
            <NavLink to="dashboard" style={linkStyle}>Painel</NavLink>
            ) : null}
          {hasPermission(user, ApplicationPermissions.UsuariosVisualizar) ? (
            <NavLink to="staff" style={linkStyle}>Staff</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.UsuariosVisualizar) ? (
            <NavLink to="users" style={linkStyle}>Usuários</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.AdministracaoDiagnostics) ? (
            <NavLink to="diagnostics" style={linkStyle}>Diagnóstico</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="configurations" style={linkStyle}>Configurações</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="audit-logs" style={linkStyle}>Auditoria</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar) ? (
            <NavLink to="role-permissions" style={linkStyle}>Roles × Permissões</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.SociosGerenciar) ? (
            <NavLink to="membership" style={linkStyle}>Membership</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.PlanosVisualizar)
            || hasPermission(user, ApplicationPermissions.PlanosCriar)
            || hasPermission(user, ApplicationPermissions.PlanosEditar)) ? (
            <NavLink to="plans" style={linkStyle}>Planos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
            || hasPermission(user, ApplicationPermissions.PagamentosGerenciar)
            || hasPermission(user, ApplicationPermissions.PagamentosEstornar)) ? (
            <NavLink to="payments" style={linkStyle}>Pagamentos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.CarteirinhaVisualizar)
            || hasPermission(user, ApplicationPermissions.CarteirinhaGerenciar)) ? (
            <NavLink to="digital-cards" style={linkStyle}>Carteirinha</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.JogosVisualizar)
            || hasPermission(user, ApplicationPermissions.JogosCriar)
            || hasPermission(user, ApplicationPermissions.JogosEditar)) ? (
            <NavLink to="games" style={linkStyle}>Jogos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.IngressosVisualizar)
            || hasPermission(user, ApplicationPermissions.IngressosGerenciar)) ? (
            <NavLink to="tickets" style={linkStyle}>Ingressos</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.NoticiasPublicar) ? (
            <NavLink to="news" style={linkStyle}>Notícias</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.LgpdDocumentosVisualizar) ? (
            <NavLink to="lgpd/documents" style={linkStyle}>LGPD — Documentos</NavLink>
          ) : null}
          {hasPermission(user, ApplicationPermissions.LgpdConsentimentosVisualizar) ? (
            <NavLink to="lgpd/consents" style={linkStyle}>LGPD — Consentimentos</NavLink>
          ) : null}
          {(hasPermission(user, ApplicationPermissions.LgpdDadosExportar)
            || hasPermission(user, ApplicationPermissions.LgpdDadosAnonimizar)) ? (
            <NavLink to="lgpd/privacy" style={linkStyle}>LGPD — Dados</NavLink>
          ) : null}
        </nav>
        <p style={{ marginTop: '2rem', fontSize: 12 }}>
          <NavLink to="/" style={{ color: '#444' }}>← Início</NavLink>
        </p>
      </aside>
      <main style={{ flex: 1, padding: '1.5rem' }}>
        <Outlet />
      </main>
    </div>
  )
}
