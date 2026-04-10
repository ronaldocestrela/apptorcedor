import { Link } from 'react-router-dom'

export function AdminHomePage() {
  return (
    <section>
      <h1>Admin</h1>
      <p>Área administrativa do clube.</p>
      <ul>
        <li>
          <Link to="/admin/plans">Planos de sócio</Link> — cadastro e gestão de planos (`/api/plans`).
        </li>
        <li>
          <Link to="/admin/billing">Faturamento SaaS</Link> — orientação sobre cobrança da plataforma.
        </li>
      </ul>
    </section>
  )
}
