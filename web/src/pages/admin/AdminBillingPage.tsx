/**
 * Cobrança SaaS (tenant paga a plataforma) é feita via API administrativa.
 * Use Swagger ou cliente HTTP com header X-Api-Key (Backoffice:ApiKey).
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
        Esta área do SPA não envia <code>X-Api-Key</code>. Para operar o faturamento do <strong>tenant/clube</strong>,
        use as rotas <code>/api/backoffice/payments/saas/...</code> documentadas no Swagger, com a chave configurada
        em <code>Backoffice:ApiKey</code>.
      </p>
      <p>
        O faturamento dos <strong>sócios</strong> (PIX, assinatura) usa as rotas <code>/api/payments/member/...</code>{' '}
        com JWT e <code>X-Tenant-Id</code> — ver página <strong>Sócio → Pagamentos</strong>.
      </p>
    </section>
  )
}
