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
}
