import { useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { getDiagnostics } from '../services/adminApi'
import { PermissionGate } from '../../auth/PermissionGate'

export function DiagnosticsPage() {
  const [data, setData] = useState<{ ok: boolean; databaseConnected: boolean } | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        const d = await getDiagnostics()
        setData(d)
      } catch {
        setError('Falha ao carregar diagnóstico.')
      }
    }
    void run()
  }, [])

  return (
    <PermissionGate anyOf={[ApplicationPermissions.AdministracaoDiagnostics]}>
      <h1>Diagnóstico</h1>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {!data && !error ? <p>Carregando...</p> : null}
      {data ? (
        <ul>
          <li>
            OK:
            {' '}
            {String(data.ok)}
          </li>
          <li>
            Banco conectado:
            {' '}
            {String(data.databaseConnected)}
          </li>
        </ul>
      ) : null}
    </PermissionGate>
  )
}
