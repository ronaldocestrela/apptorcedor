import { useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import { listUserConsents, recordUserConsent, type UserConsentRow } from '../services/lgpdApi'

export function UserConsentsPage() {
  const { user } = useAuth()
  const canRegister = hasPermission(user, ApplicationPermissions.LgpdConsentimentosRegistrar)
  const [userId, setUserId] = useState('')
  const [rows, setRows] = useState<UserConsentRow[]>([])
  const [versionId, setVersionId] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onLoad(e: FormEvent) {
    e.preventDefault()
    setMessage(null)
    setError(null)
    setBusy(true)
    try {
      setRows(await listUserConsents(userId.trim()))
    } catch {
      setError('Falha ao carregar consentimentos.')
      setRows([])
    } finally {
      setBusy(false)
    }
  }

  async function onRecord(e: FormEvent) {
    e.preventDefault()
    setMessage(null)
    setError(null)
    setBusy(true)
    try {
      await recordUserConsent(userId.trim(), versionId.trim())
      setMessage('Consentimento registrado.')
      setRows(await listUserConsents(userId.trim()))
      setVersionId('')
    } catch {
      setError('Falha ao registrar (versão deve estar publicada e consentimento único por versão).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.LgpdConsentimentosVisualizar]}>
      <h1>LGPD — Consentimentos</h1>
      <p style={{ color: '#555', maxWidth: 560 }}>
        Consulta de aceites por usuário (atendimento/admin). Registro manual exige versão publicada.
      </p>
      <form onSubmit={(e) => void onLoad(e)} style={{ maxWidth: 480, marginBottom: 24 }}>
        <label style={{ display: 'block', marginBottom: 12 }}>
          ID do usuário (GUID)
          <input
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            required
            placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <button type="submit" disabled={busy}>{busy ? '...' : 'Carregar'}</button>
      </form>

      {canRegister ? (
        <form onSubmit={(e) => void onRecord(e)} style={{ maxWidth: 480, marginBottom: 24 }}>
          <h2 style={{ fontSize: '1rem' }}>Registrar consentimento</h2>
          <label style={{ display: 'block', marginBottom: 12 }}>
            ID da versão do documento (publicada)
            <input
              value={versionId}
              onChange={(e) => setVersionId(e.target.value)}
              required
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <button type="submit" disabled={busy || !userId.trim()}>Registrar</button>
        </form>
      ) : null}

      {message ? <p style={{ color: 'green' }}>{message}</p> : null}
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}

      {rows.length > 0 ? (
        <table style={{ borderCollapse: 'collapse', width: '100%', maxWidth: 900 }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Documento</th>
              <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Versão</th>
              <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Aceito em</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.id}>
                <td style={{ padding: '8px 0' }}>{r.documentTitle}</td>
                <td style={{ padding: '8px 0' }}>{r.documentVersionNumber}</td>
                <td style={{ padding: '8px 0' }}>{new Date(r.acceptedAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </PermissionGate>
  )
}
