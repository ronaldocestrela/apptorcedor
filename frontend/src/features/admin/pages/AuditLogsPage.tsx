import { useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { listAuditLogs, type AuditLogRow } from '../services/adminApi'

export function AuditLogsPage() {
  const [rows, setRows] = useState<AuditLogRow[]>([])
  const [entityType, setEntityType] = useState('')
  const [take, setTake] = useState(50)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setRows(await listAuditLogs({ entityType: entityType.trim() || undefined, take }))
    } catch {
      setError('Falha ao carregar auditoria.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // Carrega na montagem com valores iniciais; alterações usam o botão "Filtrar".
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ConfiguracoesVisualizar]}>
      <h1>Auditoria</h1>
      <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end', marginBottom: 16, flexWrap: 'wrap' }}>
        <label>
          entityType
          <input value={entityType} onChange={(e) => setEntityType(e.target.value)} style={{ display: 'block', marginTop: 4 }} />
        </label>
        <label>
          take
          <input
            type="number"
            min={1}
            max={500}
            value={take}
            onChange={(e) => setTake(Number(e.target.value))}
            style={{ display: 'block', marginTop: 4, width: 80 }}
          />
        </label>
        <button type="button" onClick={() => void load()}>Filtrar</button>
      </div>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      <div style={{ overflowX: 'auto' }}>
        <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 14 }}>
          <thead>
            <tr>
              {['Quando', 'Ação', 'Tipo', 'Id', 'Correlação', 'Ator'].map((h) => (
                <th key={h} style={{ textAlign: 'left', borderBottom: '1px solid #ccc', padding: '6px 8px' }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.id}>
                <td style={{ padding: '6px 8px', whiteSpace: 'nowrap' }}>{new Date(r.createdAt).toLocaleString()}</td>
                <td style={{ padding: '6px 8px' }}>{r.action}</td>
                <td style={{ padding: '6px 8px' }}>{r.entityType}</td>
                <td style={{ padding: '6px 8px', wordBreak: 'break-all' }}>{r.entityId}</td>
                <td style={{ padding: '6px 8px' }}>{r.correlationId ?? '—'}</td>
                <td style={{ padding: '6px 8px' }}>{r.actorUserId ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </PermissionGate>
  )
}
