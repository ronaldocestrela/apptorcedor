import './KpiCard.css'

type KpiCardProps = {
  label: string
  value: string | number
  hint?: string
}

export function KpiCard({ label, value, hint }: KpiCardProps) {
  return (
    <article className="admin-kpi-card">
      <div className="admin-kpi-card__label">{label}</div>
      <div className="admin-kpi-card__value">{value}</div>
      {hint ? <div className="admin-kpi-card__hint">{hint}</div> : null}
    </article>
  )
}
