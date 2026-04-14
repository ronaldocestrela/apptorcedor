import type { ReactNode } from 'react'
import './KpiCard.css'

export type KpiCardVariant = 'success' | 'warning' | 'info' | 'danger'

type KpiCardProps = {
  label: string
  value: string | number
  hint?: string
  icon?: ReactNode
  variant?: KpiCardVariant
}

export function KpiCard({ label, value, hint, icon, variant = 'success' }: KpiCardProps) {
  return (
    <article className={`admin-kpi-card admin-kpi-card--${variant}`}>
      <div className="admin-kpi-card__header">
        <div className="admin-kpi-card__label">{label}</div>
        {icon ? <div className="admin-kpi-card__icon-wrap">{icon}</div> : null}
      </div>
      <div className="admin-kpi-card__value">{value}</div>
      {hint ? <div className="admin-kpi-card__hint">{hint}</div> : null}
    </article>
  )
}
