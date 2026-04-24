/**
 * Mirrors backend rule: `BillingCycle === "Yearly"` enables installment options at card checkout (e.g. ASAAS link).
 */
export function planOffersCardInstallmentsAtCheckout(billingCycle: string): boolean {
  return billingCycle.trim() === 'Yearly'
}

/** Short hint for catalog / plan detail (card-only checkout). */
export const cardInstallmentsCheckoutShortHint =
  'Plano anual: parcelamento no cartão no checkout (opções conforme seu banco).'

/** Admin-facing explanation when configuring a yearly plan. */
export const cardInstallmentsAdminHelpYearly =
  'Planos com ciclo Yearly (anual): no pagamento com cartão, o link de pagamento do gateway (ex.: ASAAS) pode oferecer parcelamento conforme configuração da conta e do cartão.'
