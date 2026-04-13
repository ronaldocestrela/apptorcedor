import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { PermissionGate } from '../../auth/PermissionGate'
import { useAuth } from '../../auth/AuthContext'
import {
  getAdminUser,
  listUserAuditLogsForUser,
  setUserAccountActive,
  upsertAdminUserProfile,
  type AdminUserDetail,
  type AuditLogRow,
} from '../services/adminApi'

export function UserDetailPage() {
  const { userId } = useParams<{ userId: string }>()
  const { user } = useAuth()
  const canEdit = hasPermission(user, ApplicationPermissions.UsuariosEditar)

  const [detail, setDetail] = useState<AdminUserDetail | null>(null)
  const [audit, setAudit] = useState<AuditLogRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [doc, setDoc] = useState('')
  const [birthDate, setBirthDate] = useState('')
  const [photoUrl, setPhotoUrl] = useState('')
  const [address, setAddress] = useState('')
  const [adminNote, setAdminNote] = useState('')

  const load = useCallback(async () => {
    if (!userId)
      return
    setLoading(true)
    setError(null)
    try {
      const [d, logs] = await Promise.all([
        getAdminUser(userId),
        listUserAuditLogsForUser(userId, 50),
      ])
      setDetail(d)
      setAudit(logs)
      const p = d.profile
      setDoc(p?.document ?? '')
      setBirthDate(p?.birthDate ? p.birthDate.slice(0, 10) : '')
      setPhotoUrl(p?.photoUrl ?? '')
      setAddress(p?.address ?? '')
      setAdminNote(p?.administrativeNote ?? '')
    } catch (err: unknown) {
      if (isAxiosError(err) && err.response?.status === 404)
        setError('Usuário não encontrado.')
      else
        setError('Falha ao carregar usuário.')
    } finally {
      setLoading(false)
    }
  }, [userId])

  useEffect(() => {
    void load()
  }, [load])

  const saveProfile = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!userId || !canEdit)
      return
    setBusy(true)
    setError(null)
    try {
      await upsertAdminUserProfile(userId, {
        document: doc,
        birthDate: birthDate || null,
        photoUrl: photoUrl || null,
        address: address || null,
        administrativeNote: adminNote || null,
      })
      await load()
    } catch {
      setError('Não foi possível salvar o perfil.')
    } finally {
      setBusy(false)
    }
  }

  const toggleActive = async (next: boolean) => {
    if (!userId || !canEdit)
      return
    setBusy(true)
    setError(null)
    try {
      await setUserAccountActive(userId, next)
      await load()
    } catch {
      setError('Não foi possível alterar o status da conta.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.UsuariosVisualizar]}>
      <p style={{ marginBottom: 16 }}>
        <Link to="/admin/users">← Usuários</Link>
      </p>

      {loading ? <p>Carregando...</p> : null}
      {error ? <p style={{ color: '#b00020' }}>{error}</p> : null}

      {!loading && detail ? (
        <section>
          <h1 style={{ marginTop: 0 }}>{detail.name}</h1>
          <p style={{ color: '#555' }}>{detail.email}</p>

          <div style={{ display: 'grid', gap: 8, maxWidth: 520, marginBottom: 24 }}>
            <div><strong>ID:</strong> {detail.id}</div>
            <div><strong>Telefone:</strong> {detail.phoneNumber ?? '—'}</div>
            <div><strong>Conta ativa:</strong> {detail.isActive ? 'Sim' : 'Não'}</div>
            <div><strong>Staff:</strong> {detail.isStaff ? 'Sim' : 'Não'}</div>
            <div><strong>Roles:</strong> {detail.roles.length ? detail.roles.join(', ') : '—'}</div>
            <div><strong>Criado em:</strong> {new Date(detail.createdAt).toLocaleString()}</div>
          </div>

          {canEdit ? (
            <div style={{ marginBottom: 24 }}>
              <button type="button" disabled={busy} onClick={() => void toggleActive(!detail.isActive)}>
                {detail.isActive ? 'Inativar conta' : 'Ativar conta'}
              </button>
            </div>
          ) : null}

          <h2>Associação (somente leitura)</h2>
          {detail.membership ? (
            <ul style={{ lineHeight: 1.6 }}>
              <li>
                <strong>Status:</strong> {detail.membership.status}
              </li>
              <li>
                <strong>Id membership:</strong> {detail.membership.membershipId}
              </li>
              <li>
                <strong>Plano:</strong> {detail.membership.planId ?? '—'}
              </li>
              <li>
                <strong>Início:</strong> {new Date(detail.membership.startDate).toLocaleString()}
              </li>
            </ul>
          ) : (
            <p>Sem registro de associação.</p>
          )}

          <h2>Perfil estendido</h2>
          {canEdit ? (
            <form onSubmit={(e) => void saveProfile(e)} style={{ maxWidth: 520, display: 'grid', gap: 12 }}>
              <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                Documento
                <input value={doc} onChange={(e) => setDoc(e.target.value)} style={{ marginTop: 4, padding: 8 }} />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                Data de nascimento
                <input type="date" value={birthDate} onChange={(e) => setBirthDate(e.target.value)} style={{ marginTop: 4, padding: 8 }} />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                URL da foto
                <input value={photoUrl} onChange={(e) => setPhotoUrl(e.target.value)} style={{ marginTop: 4, padding: 8 }} />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                Endereço
                <textarea value={address} onChange={(e) => setAddress(e.target.value)} rows={3} style={{ marginTop: 4, padding: 8 }} />
              </label>
              <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                Nota administrativa
                <textarea value={adminNote} onChange={(e) => setAdminNote(e.target.value)} rows={2} style={{ marginTop: 4, padding: 8 }} />
              </label>
              <button type="submit" disabled={busy}>Salvar perfil</button>
            </form>
          ) : (
            <div style={{ lineHeight: 1.6 }}>
              {detail.profile ? (
                <>
                  <p><strong>Documento:</strong> {detail.profile.document ?? '—'}</p>
                  <p><strong>Nascimento:</strong> {detail.profile.birthDate ?? '—'}</p>
                  <p><strong>Foto:</strong> {detail.profile.photoUrl ?? '—'}</p>
                  <p><strong>Endereço:</strong> {detail.profile.address ?? '—'}</p>
                  <p><strong>Nota admin:</strong> {detail.profile.administrativeNote ?? '—'}</p>
                </>
              ) : (
                <p>Sem perfil estendido cadastrado.</p>
              )}
            </div>
          )}

          <h2>LGPD</h2>
          <p>
            <Link to={`/admin/lgpd/consents?userId=${encodeURIComponent(detail.id)}`}>Consentimentos deste usuário</Link>
            {' '}
            ·
            {' '}
            <Link to="/admin/lgpd/privacy">Exportar / anonimizar</Link>
          </p>

          <h2>Histórico (conta e perfil)</h2>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 13 }}>
              <thead>
                <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                  <th style={{ padding: 6 }}>Quando</th>
                  <th style={{ padding: 6 }}>Ação</th>
                  <th style={{ padding: 6 }}>Entidade</th>
                </tr>
              </thead>
              <tbody>
                {audit.map((row) => (
                  <tr key={row.id} style={{ borderBottom: '1px solid #eee' }}>
                    <td style={{ padding: 6 }}>{new Date(row.createdAt).toLocaleString()}</td>
                    <td style={{ padding: 6 }}>{row.action}</td>
                    <td style={{ padding: 6 }}>{row.entityType}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {audit.length === 0 ? <p>Nenhum evento registrado.</p> : null}
        </section>
      ) : null}
    </PermissionGate>
  )
}
