import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Users,
  AlertCircle,
  MessageSquare,
  ArrowRight,
  BadgeCheck,
  Layers,
  Receipt,
  Headphones,
} from 'lucide-react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { getAdminDashboard, type AdminDashboardResult } from '../services/adminApi'
import { KpiCard } from '../components/KpiCard'
import './AdminDashboardPage.css'

function KpiSkeletonCard() {
  return <div className="admin-kpi-skeleton" aria-hidden="true" />
}

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
          Visão geral de associação, inadimplência e atendimento.
        </p>

        {error ? <p role="alert" className="admin-dashboard__error">{error}</p> : null}

        {/* KPI grid */}
        <div className="admin-dashboard__kpi-grid">
          {loading ? (
            <>
              <KpiSkeletonCard />
              <KpiSkeletonCard />
              <KpiSkeletonCard />
            </>
          ) : data ? (
            <>
              <KpiCard
                label="Sócios ativos"
                value={data.activeMembersCount}
                icon={<Users size={20} />}
                variant="success"
              />
              <KpiCard
                label="Inadimplentes"
                value={data.delinquentMembersCount}
                icon={<AlertCircle size={20} />}
                variant="warning"
              />
              <KpiCard
                label="Chamados abertos"
                value={data.openSupportTickets}
                icon={<MessageSquare size={20} />}
                variant="info"
              />
            </>
          ) : null}
        </div>

        {/* Quick actions */}
        {!loading ? (
          <div className="admin-dashboard__quick-actions">
            <h2 className="admin-dashboard__quick-title">Ações rápidas</h2>
            <div className="admin-dashboard__quick-grid">
              <Link to="/admin/membership" className="admin-dashboard__quick-card">
                <BadgeCheck size={20} />
                <span>Ver memberships</span>
                <ArrowRight size={15} className="admin-dashboard__quick-arrow" />
              </Link>
              <Link to="/admin/plans" className="admin-dashboard__quick-card">
                <Layers size={20} />
                <span>Gerenciar planos</span>
                <ArrowRight size={15} className="admin-dashboard__quick-arrow" />
              </Link>
              <Link to="/admin/payments" className="admin-dashboard__quick-card">
                <Receipt size={20} />
                <span>Ver pagamentos</span>
                <ArrowRight size={15} className="admin-dashboard__quick-arrow" />
              </Link>
              <Link to="/admin/support" className="admin-dashboard__quick-card">
                <Headphones size={20} />
                <span>Responder chamados</span>
                <ArrowRight size={15} className="admin-dashboard__quick-arrow" />
              </Link>
            </div>
          </div>
        ) : null}
      </section>
    </PermissionGate>
  )
}
