import { useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { getAdminDashboard, type AdminDashboardResult } from '../services/adminApi'

export function AdminDashboardPage() {
  const [data, setData] = useState<AdminDashboardResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      setLoading(true)
      setError(null)
      try {
        setData(await getAdminDashboard())
      } catch {
        setError('Falha ao carregar o painel.')
      } finally {
        setLoading(false)
      }
    }
    void run()
  }, [])

  return (
    <PermissionGate
      anyOf={[
        ApplicationPermissions.UsuariosVisualizar,
        ApplicationPermissions.ConfiguracoesVisualizar,
      ]}
    >
      <h1>Painel administrativo</h1>
      <p style={{ color: '#555', maxWidth: 640 }}>
        Indicadores mínimos de associação. Chamados abertos dependem do módulo de suporte (B.11).
      </p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      {!loading && data ? (
        <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap', marginTop: '1rem' }}>
          <KpiCard label="Sócios ativos" value={data.activeMembersCount} />
          <KpiCard label="Inadimplentes" value={data.delinquentMembersCount} />
          <KpiCard
            label="Chamados abertos"
            value={data.openSupportTickets === null ? '—' : String(data.openSupportTickets)}
            hint={data.openSupportTickets === null ? 'Indisponível (módulo não implementado)' : undefined}
          />
        </div>
      ) : null}
    </PermissionGate>
  )
}

function KpiCard({ label, value, hint }: { label: string; value: string | number; hint?: string }) {
  return (
    <div
      style={{
        border: '1px solid #ddd',
        borderRadius: 8,
        padding: '1rem 1.25rem',
        minWidth: 160,
        background: '#fff',
      }}
    >
      <div style={{ fontSize: 13, color: '#666' }}>{label}</div>
      <div style={{ fontSize: 28, fontWeight: 600, marginTop: 4 }}>{value}</div>
      {hint ? <div style={{ fontSize: 12, color: '#888', marginTop: 8 }}>{hint}</div> : null}
    </div>
  )
}
