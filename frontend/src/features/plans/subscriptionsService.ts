import { api } from '../../shared/api/http'

export type SubscriptionPaymentMethod = 'Pix' | 'Card'

export type TorcedorSubscriptionCheckoutResponse = {
  membershipId: string
  paymentId: string
  paymentMethod: string
  amount: number
  currency: string
  membershipStatus: string
  pix: { qrCodePayload: string; copyPasteKey: string | null } | null
  card: { checkoutUrl: string } | null
}

export type MySubscriptionSummaryPlan = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
}

export type MySubscriptionSummaryPayment = {
  paymentId: string
  amount: number
  currency: string
  status: string
  paymentMethod: string | null
  paidAt: string | null
  dueDate: string
}

export type MySubscriptionSummaryDigitalCard = {
  state: string
  membershipStatusLabel: string
  message: string | null
}

export type MySubscriptionSummary = {
  hasMembership: boolean
  membershipId: string | null
  membershipStatus: string | null
  startDate: string | null
  endDate: string | null
  nextDueDate: string | null
  plan: MySubscriptionSummaryPlan | null
  lastPayment: MySubscriptionSummaryPayment | null
  digitalCard: MySubscriptionSummaryDigitalCard | null
}

export type ChangePlanPlanSnapshot = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
}

export type ChangePlanResponse = {
  membershipId: string
  membershipStatus: string
  fromPlan: ChangePlanPlanSnapshot
  toPlan: ChangePlanPlanSnapshot
  prorationAmount: number
  paymentId: string | null
  currency: string
  paymentMethod: string | null
  pix: { qrCodePayload: string; copyPasteKey: string | null } | null
  card: { checkoutUrl: string } | null
}

export type CancelMembershipResponse = {
  membershipId: string
  membershipStatus: string
  mode: string
  accessValidUntilUtc: string | null
  message: string
}

export const subscriptionsService = {
  async subscribe(
    planId: string,
    paymentMethod: SubscriptionPaymentMethod,
  ): Promise<TorcedorSubscriptionCheckoutResponse> {
    const { data } = await api.post<TorcedorSubscriptionCheckoutResponse>('/api/subscriptions', {
      planId,
      paymentMethod,
    })
    return data
  },

  async getMySummary(): Promise<MySubscriptionSummary> {
    const { data } = await api.get<MySubscriptionSummary>('/api/account/subscription')
    return data
  },

  async changePlan(
    planId: string,
    paymentMethod: SubscriptionPaymentMethod,
  ): Promise<ChangePlanResponse> {
    const { data } = await api.put<ChangePlanResponse>('/api/account/subscription/plan', {
      planId,
      paymentMethod,
    })
    return data
  },

  async cancelMembership(): Promise<CancelMembershipResponse> {
    const { data } = await api.delete<CancelMembershipResponse>('/api/account/subscription')
    return data
  },
}
