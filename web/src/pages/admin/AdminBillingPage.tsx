/**
 * Cobrança SaaS (tenant paga a plataforma) é feita via API administrativa.
 * Use Scalar (/scalar) ou cliente HTTP com header X-Api-Key (Backoffice:ApiKey).
 *
 * Exemplos:
 * - POST /api/backoffice/payments/saas/tenants/{tenantId}/billing/start
 * - GET  /api/backoffice/payments/saas/tenants/{tenantId}/subscription
 * - GET  /api/backoffice/payments/saas/tenants/{tenantId}/invoices
 * - POST /api/backoffice/payments/saas/webhooks (corpo JSON com idempotencyKey, eventType, externalSubscriptionId)
 */
export function AdminBillingPage() {
  return (
    <section>
      <h1>Faturamento SaaS (backoffice)</h1>
      <p>
        Administradores do <strong>clube</strong> não usam a chave da plataforma aqui. Operadores do SaaS podem abrir{' '}
        <a href="/backoffice/login">
          <code>/backoffice</code>
        </a>{' '}
        (login com <code>Backoffice:ApiKey</code>) ou usar <code>/api/backoffice/payments/saas/...</code> no OpenAPI (
        <code>/scalar</code>).
      </p>
      <p>
        O faturamento dos <strong>sócios</strong> (PIX, assinatura) usa as rotas <code>/api/payments/member/...</code>{' '}
        com JWT e <code>X-Tenant-Id</code> — ver página <strong>Sócio → Pagamentos</strong>.
      </p>
    </section>
  )
}
