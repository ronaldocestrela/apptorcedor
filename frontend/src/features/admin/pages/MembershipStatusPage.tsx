import { useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { updateMembershipStatus, type MembershipStatus } from '../services/adminApi'

const statuses: MembershipStatus[] = ['NaoAssociado', 'Ativo', 'Inadimplente', 'Suspenso', 'Cancelado']

export function MembershipStatusPage() {
  const [membershipId, setMembershipId] = useState('')
  const [status, setStatus] = useState<MembershipStatus>('Ativo')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setMessage(null)
    setError(null)
    setBusy(true)
    try {
      await updateMembershipStatus(membershipId.trim(), status)
      setMessage('Status atualizado.')
    } catch {
      setError('Falha ao atualizar (verifique o ID e suas permissões).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.SociosGerenciar]}>
      <h1>Membership — alterar status</h1>
      <p style={{ color: '#555', maxWidth: 560 }}>
        Informe o GUID da associação. A listagem administrativa de memberships será adicionada na Parte B.
      </p>
      <form onSubmit={(e) => void onSubmit(e)} style={{ maxWidth: 480 }}>
        <label style={{ display: 'block', marginBottom: 12 }}>
          ID da membership
          <input
            value={membershipId}
            onChange={(e) => setMembershipId(e.target.value)}
            required
            placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 12 }}>
          Novo status
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value as MembershipStatus)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          >
            {statuses.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </label>
        {message ? <p style={{ color: 'green' }}>{message}</p> : null}
        {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
        <button type="submit" disabled={busy}>{busy ? 'Salvando...' : 'Atualizar'}</button>
      </form>
    </PermissionGate>
  )
}
