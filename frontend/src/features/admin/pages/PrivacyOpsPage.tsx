import { useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { anonymizeUser, exportUserData, type PrivacyOperationResult } from '../services/lgpdApi'

export function PrivacyOpsPage() {
  const [userId, setUserId] = useState('')
  const [result, setResult] = useState<PrivacyOperationResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run(op: 'export' | 'anonymize') {
    setError(null)
    setResult(null)
    setBusy(true)
    try {
      const res = op === 'export'
        ? await exportUserData(userId.trim())
        : await anonymizeUser(userId.trim())
      setResult(res)
      if (res.status === 'Failed')
        setError(res.errorMessage ?? 'Operação falhou.')
    } catch {
      setError('Falha na requisição.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.LgpdDadosExportar, ApplicationPermissions.LgpdDadosAnonimizar]}>
      <h1>LGPD — Exportação e anonimização</h1>
      <p style={{ color: '#555', maxWidth: 640 }}>
        Fluxos mínimos para atendimento: exportar dados vinculados à conta e anonimizar PII (e-mail, nome, telefone), desativar conta e revogar refresh tokens.
      </p>
      <form
        onSubmit={(e) => {
          e.preventDefault()
        }}
        style={{ maxWidth: 480 }}
      >
        <label style={{ display: 'block', marginBottom: 12 }}>
          ID do usuário (GUID)
          <input
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            required
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button type="button" disabled={busy} onClick={() => void run('export')}>Exportar dados</button>
          <button type="button" disabled={busy} onClick={() => void run('anonymize')}>Anonimizar</button>
        </div>
      </form>
      {error ? <p role="alert" style={{ color: 'crimson', marginTop: 16 }}>{error}</p> : null}
      {result ? (
        <section style={{ marginTop: 24 }}>
          <h2 style={{ fontSize: '1rem' }}>Resultado</h2>
          <p style={{ fontSize: 14 }}>
            Status:
            {' '}
            <strong>{result.status}</strong>
            {' '}
            (
            {result.kind}
            )
          </p>
          {result.resultJson ? (
            <pre style={{ background: '#f5f5f5', padding: 12, overflow: 'auto', maxHeight: 400, fontSize: 12 }}>
              {result.resultJson}
            </pre>
          ) : null}
        </section>
      ) : null}
    </PermissionGate>
  )
}
