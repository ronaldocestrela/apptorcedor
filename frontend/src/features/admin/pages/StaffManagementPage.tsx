import { useCallback, useEffect, useState } from 'react'
import { isAxiosError } from 'axios'
import {
  ApplicationPermissions,
  STAFF_ASSIGNABLE_ROLES,
} from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { PermissionGate } from '../../auth/PermissionGate'
import { useAuth } from '../../auth/AuthContext'
import {
  createStaffInvite,
  listStaffInvites,
  listStaffUsers,
  replaceStaffUserRoles,
  setStaffUserActive,
  type StaffInviteRow,
  type StaffUserRow,
} from '../services/adminApi'

export function StaffManagementPage() {
  const { user } = useAuth()
  const canEdit = hasPermission(user, ApplicationPermissions.UsuariosEditar)
  const [users, setUsers] = useState<StaffUserRow[]>([])
  const [invites, setInvites] = useState<StaffInviteRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteName, setInviteName] = useState('')
  const [inviteRoles, setInviteRoles] = useState<string[]>(['Operador'])
  const [lastToken, setLastToken] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const reload = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [u, i] = await Promise.all([listStaffUsers(), listStaffInvites()])
      setUsers(u)
      setInvites(i)
    } catch {
      setError('Falha ao carregar usuários internos.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void reload()
  }, [reload])

  const toggleInviteRole = (role: string) => {
    setInviteRoles((prev) => (prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]))
  }

  const submitInvite = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!canEdit)
      return
    setBusy(true)
    setLastToken(null)
    setError(null)
    try {
      if (inviteRoles.length === 0) {
        setError('Selecione ao menos uma role para o convite.')
        return
      }
      const res = await createStaffInvite({
        email: inviteEmail.trim(),
        name: inviteName.trim(),
        roles: inviteRoles,
      })
      setLastToken(res.token)
      setInviteEmail('')
      setInviteName('')
      await reload()
    } catch (err: unknown) {
      const msg = isAxiosError(err) && err.response?.data && typeof err.response.data === 'object' && 'message' in err.response.data
        ? String((err.response.data as { message: string }).message)
        : 'Não foi possível criar o convite.'
      setError(msg)
    } finally {
      setBusy(false)
    }
  }

  const patchActive = async (id: string, isActive: boolean) => {
    if (!canEdit)
      return
    setBusy(true)
    try {
      await setStaffUserActive(id, isActive)
      await reload()
    } catch {
      setError('Falha ao atualizar status.')
    } finally {
      setBusy(false)
    }
  }

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editRoles, setEditRoles] = useState<string[]>([])

  const startEditRoles = (row: StaffUserRow) => {
    setEditingId(row.id)
    setEditRoles([...row.roles])
  }

  const toggleEditRole = (role: string) => {
    setEditRoles((prev) => (prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]))
  }

  const saveRoles = async () => {
    if (!editingId || !canEdit)
      return
    if (editRoles.length === 0) {
      setError('O usuário precisa de ao menos uma role interna.')
      return
    }
    setBusy(true)
    try {
      await replaceStaffUserRoles(editingId, editRoles)
      setEditingId(null)
      await reload()
    } catch (err: unknown) {
      const msg = isAxiosError(err) && err.response?.data && typeof err.response.data === 'object' && 'message' in err.response.data
        ? String((err.response.data as { message: string }).message)
        : 'Falha ao salvar roles.'
      setError(msg)
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.UsuariosVisualizar]}>
      <h1>Usuários internos (staff)</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        Convites geram um token único. Envie o link com
        {' '}
        <code>?token=...</code>
        {' '}
        para
        {' '}
        <a href="/accept-staff-invite">/accept-staff-invite</a>
        {' '}
        ou copie o token manualmente.
      </p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {lastToken ? (
        <div style={{ background: '#e8f4ff', padding: '0.75rem 1rem', borderRadius: 8, marginTop: 8 }}>
          <strong>Token do convite (copie agora):</strong>
          <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all', margin: '0.5rem 0 0' }}>{lastToken}</pre>
        </div>
      ) : null}

      {canEdit ? (
        <form onSubmit={submitInvite} style={{ marginTop: '1.5rem', maxWidth: 520 }}>
          <h2 style={{ fontSize: '1.05rem' }}>Novo convite</h2>
          <label style={{ display: 'block', marginBottom: 8 }}>
            E-mail
            <input
              required
              type="email"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <label style={{ display: 'block', marginBottom: 8 }}>
            Nome
            <input
              required
              value={inviteName}
              onChange={(e) => setInviteName(e.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <fieldset style={{ border: '1px solid #ccc', borderRadius: 8, padding: '0.75rem' }}>
            <legend>Roles</legend>
            {STAFF_ASSIGNABLE_ROLES.map((role) => (
              <label key={role} style={{ display: 'block', marginBottom: 4 }}>
                <input
                  type="checkbox"
                  checked={inviteRoles.includes(role)}
                  onChange={() => toggleInviteRole(role)}
                />
                {' '}
                {role}
              </label>
            ))}
          </fieldset>
          <button type="submit" disabled={busy} style={{ marginTop: 12 }}>
            Criar convite
          </button>
        </form>
      ) : null}

      <h2 style={{ marginTop: '2rem', fontSize: '1.05rem' }}>Convites pendentes</h2>
      {loading ? <p>Carregando...</p> : null}
      {!loading && invites.length === 0 ? <p>Nenhum convite pendente.</p> : null}
      <ul>
        {invites.map((i) => (
          <li key={i.id}>
            {i.email}
            {' '}
            —
            {i.name}
            {' '}
            (expira
            {' '}
            {new Date(i.expiresAt).toLocaleString()}
            )
          </li>
        ))}
      </ul>

      <h2 style={{ fontSize: '1.05rem' }}>Usuários com perfil interno</h2>
      <table style={{ borderCollapse: 'collapse', width: '100%', maxWidth: 900 }}>
        <thead>
          <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
            <th style={{ padding: 8 }}>E-mail</th>
            <th style={{ padding: 8 }}>Nome</th>
            <th style={{ padding: 8 }}>Ativo</th>
            <th style={{ padding: 8 }}>Roles</th>
            {canEdit ? <th style={{ padding: 8 }}>Ações</th> : null}
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id} style={{ borderBottom: '1px solid #eee' }}>
              <td style={{ padding: 8 }}>{u.email}</td>
              <td style={{ padding: 8 }}>{u.name}</td>
              <td style={{ padding: 8 }}>{u.isActive ? 'Sim' : 'Não'}</td>
              <td style={{ padding: 8 }}>{u.roles.join(', ')}</td>
              {canEdit ? (
                <td style={{ padding: 8 }}>
                  <button type="button" disabled={busy} onClick={() => patchActive(u.id, !u.isActive)}>
                    {u.isActive ? 'Desativar' : 'Ativar'}
                  </button>
                  {' '}
                  <button type="button" disabled={busy} onClick={() => startEditRoles(u)}>
                    Editar roles
                  </button>
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>

      {editingId && canEdit ? (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.35)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: 16,
          }}
        >
          <div style={{ background: '#fff', padding: '1.25rem', borderRadius: 8, maxWidth: 420, width: '100%' }}>
            <h3 style={{ marginTop: 0 }}>Editar roles</h3>
            {STAFF_ASSIGNABLE_ROLES.map((role) => (
              <label key={role} style={{ display: 'block', marginBottom: 4 }}>
                <input
                  type="checkbox"
                  checked={editRoles.includes(role)}
                  onChange={() => toggleEditRole(role)}
                />
                {' '}
                {role}
              </label>
            ))}
            <div style={{ marginTop: 12, display: 'flex', gap: 8 }}>
              <button type="button" disabled={busy} onClick={saveRoles}>Salvar</button>
              <button type="button" disabled={busy} onClick={() => setEditingId(null)}>Cancelar</button>
            </div>
          </div>
        </div>
      ) : null}
    </PermissionGate>
  )
}
