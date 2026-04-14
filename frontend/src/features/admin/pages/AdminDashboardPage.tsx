import { useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { getAdminDashboard, type AdminDashboardResult } from '../services/adminApi'
import { KpiCard } from '../components/KpiCard'
import './AdminDashboardPage.css'

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
      <section className="admin-dashboard">
        <h1 className="admin-dashboard__title">Painel administrativo</h1>
        <p className="admin-dashboard__subtitle">
          Indicadores mínimos de associação e chamados em aberto.
        </p>
        {error ? <p role="alert" className="admin-dashboard__error">{error}</p> : null}
        {loading ? <p className="admin-dashboard__loading">Carregando...</p> : null}
        {!loading && data ? (
          <div className="admin-dashboard__kpi-grid">
            <KpiCard label="Sócios ativos" value={data.activeMembersCount} />
            <KpiCard label="Inadimplentes" value={data.delinquentMembersCount} />
            <KpiCard label="Chamados abertos" value={data.openSupportTickets} />
          </div>
        ) : null}
      </section>
    </PermissionGate>
  )
}
