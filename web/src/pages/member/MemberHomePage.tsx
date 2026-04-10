import { Link } from 'react-router-dom'

export function MemberHomePage() {
  return (
    <section>
      <h1>Sócio</h1>
      <p>Área do sócio (placeholder — Fase 1.1).</p>
      <p>
        <Link to="/member/billing">Ir para pagamentos e assinatura</Link>
      </p>
    </section>
  )
}
