import { apiClient } from '../http/client'

export type PaymentMethodKind = 0 | 1 | 2

export interface MemberPlanSummary {
  id: string
  nome: string
  descricao: string | null
  preco: number
  isActive: boolean
}

export interface MemberBillingSubscription {
  id: string
  memberProfileId: string
  memberPlanId: string
  planName: string | null
  recurringAmount: number
  currency: string
  paymentMethod: PaymentMethodKind
  status: number
  externalCustomerId: string | null
  externalSubscriptionId: string | null
  nextBillingAtUtc: string | null
  createdAtUtc: string
}

export interface MemberBillingInvoice {
  id: string
  memberBillingSubscriptionId: string
  amount: number
  currency: string
  paymentMethod: PaymentMethodKind
  dueAtUtc: string
  status: number
  externalInvoiceId: string | null
  pixCopyPaste: string | null
  paidAtUtc: string | null
  createdAtUtc: string
}

export interface MemberPixCheckout {
  invoiceId: string
  externalChargeId: string | null
  pixCopyPaste: string | null
  expiresAtUtc: string | null
}

export interface MemberStripeCheckoutSession {
  sessionId: string
  url: string
}

export async function fetchMemberPlans(page = 1, pageSize = 50) {
  const { data } = await apiClient.get<{
    items: MemberPlanSummary[]
    totalCount: number
    page: number
    pageSize: number
  }>('/api/plans', { params: { page, pageSize } })
  return data
}

export async function subscribeMemberPlan(memberPlanId: string, paymentMethod: PaymentMethodKind = 1) {
  const { data } = await apiClient.post<{ id: string }>('/api/payments/member/subscribe', {
    memberPlanId,
    paymentMethod,
  })
  return data
}

export async function createMemberPixCheckout(memberPlanId: string) {
  const { data } = await apiClient.post<MemberPixCheckout>('/api/payments/member/checkout/pix', {
    memberPlanId,
  })
  return data
}

/** Stripe Checkout (hospedado) para assinatura com cartão — requer gateway Stripe configurado no tenant. */
export async function createMemberStripeCheckoutSession(
  memberPlanId: string,
  successUrl: string,
  cancelUrl: string,
) {
  const { data } = await apiClient.post<MemberStripeCheckoutSession>(
    '/api/payments/member/checkout/stripe-session',
    {
      memberPlanId,
      successUrl,
      cancelUrl,
    },
  )
  return data
}

export async function getMySubscription() {
  const { data } = await apiClient.get<MemberBillingSubscription | null>('/api/payments/member/me/subscription')
  return data
}

export async function listMyInvoices(page = 1, pageSize = 20) {
  const { data } = await apiClient.get<{
    items: MemberBillingInvoice[]
    totalCount: number
    page: number
    pageSize: number
  }>('/api/payments/member/me/invoices', { params: { page, pageSize } })
  return data
}
