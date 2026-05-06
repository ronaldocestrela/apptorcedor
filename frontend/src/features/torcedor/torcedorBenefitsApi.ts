import { api } from '../../shared/api/http'

export type TorcedorEligibleBenefitOffer = {
  offerId: string
  partnerId: string
  partnerName: string
  title: string
  description: string | null
  startAt: string
  endAt: string
  bannerUrl: string | null
}

export type TorcedorEligibleBenefitsPage = {
  totalCount: number
  items: TorcedorEligibleBenefitOffer[]
}

export async function listEligibleBenefitOffers(params?: {
  page?: number
  pageSize?: number
}): Promise<TorcedorEligibleBenefitsPage> {
  const { data } = await api.get<TorcedorEligibleBenefitsPage>('/api/benefits/eligible', {
    params: {
      page: params?.page ?? 1,
      pageSize: params?.pageSize ?? 20,
    },
  })
  return data
}

export type TorcedorEligibleBenefitOfferDetail = {
  offerId: string
  partnerId: string
  partnerName: string
  title: string
  description: string | null
  startAt: string
  endAt: string
  alreadyRedeemed: boolean
  redemptionDateUtc: string | null
  bannerUrl: string | null
  isShirtCustomizationOffer: boolean
  shirtSizes: string[]
  shirtModels: string[]
  redemptionWorkflowStatus: string
}

export async function getEligibleBenefitOfferDetail(offerId: string): Promise<TorcedorEligibleBenefitOfferDetail> {
  const { data } = await api.get<TorcedorEligibleBenefitOfferDetail>(`/api/benefits/offers/${offerId}`)
  return data
}

export type TorcedorBenefitRedeemPayload = {
  shirtSize?: string
  shirtModel?: string
  shirtNumber?: string
  shirtDisplayName?: string
  deliveryCep?: string
  deliveryNeighborhood?: string
  deliveryStreet?: string
  deliveryNumber?: string
  deliveryCity?: string
  deliveryState?: string
}

export type TorcedorBenefitRedeemResponse = {
  redemptionId: string
}

export async function redeemBenefitOffer(
  offerId: string,
  payload?: TorcedorBenefitRedeemPayload | null,
): Promise<TorcedorBenefitRedeemResponse> {
  const hasPayload =
    !!payload
    && Object.values(payload).some((v) => v != null && String(v).trim() !== '')
  const body = hasPayload
    ? {
        shirtSize: payload!.shirtSize?.trim() || undefined,
        shirtModel: payload!.shirtModel?.trim() || undefined,
        shirtNumber: payload!.shirtNumber?.trim() || undefined,
        shirtDisplayName: payload!.shirtDisplayName?.trim() || undefined,
        deliveryCep: payload!.deliveryCep?.trim() || undefined,
        deliveryNeighborhood: payload!.deliveryNeighborhood?.trim() || undefined,
        deliveryStreet: payload!.deliveryStreet?.trim() || undefined,
        deliveryNumber: payload!.deliveryNumber?.trim() || undefined,
        deliveryCity: payload!.deliveryCity?.trim() || undefined,
        deliveryState: payload!.deliveryState?.trim().toUpperCase() || undefined,
      }
    : undefined
  const { data } = await api.post<TorcedorBenefitRedeemResponse>(
    `/api/benefits/offers/${offerId}/redeem`,
    body,
  )
  return data
}
