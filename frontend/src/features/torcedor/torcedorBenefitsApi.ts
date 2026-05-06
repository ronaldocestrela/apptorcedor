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

export type TorcedorShippingOption = {
  serviceId: number
  serviceName: string
  carrierName: string
  pictureUrl: string
  price: number
  deliveryDays: number
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
  /** `pickup` | `carrier` */
  shippingMethod?: string
  shippingCarrierId?: number
  shippingCarrierName?: string
  shippingServiceName?: string
  shippingPrice?: number
  shippingDeliveryDays?: number
}

export type TorcedorBenefitRedeemResponse = {
  redemptionId: string
}

function redeemPayloadHasValues(payload: TorcedorBenefitRedeemPayload): boolean {
  for (const v of Object.values(payload)) {
    if (v === null || v === undefined)
      continue
    if (typeof v === 'number' && Number.isFinite(v))
      return true
    if (typeof v === 'string' && v.trim() !== '')
      return true
  }
  return false
}

export async function getShippingOptions(cepDigits: string): Promise<TorcedorShippingOption[]> {
  const { data } = await api.get<TorcedorShippingOption[]>('/api/benefits/shipping-options', {
    params: { cep: cepDigits },
  })
  return data ?? []
}

export async function redeemBenefitOffer(
  offerId: string,
  payload?: TorcedorBenefitRedeemPayload | null,
): Promise<TorcedorBenefitRedeemResponse> {
  const hasPayload =
    !!payload && redeemPayloadHasValues(payload as TorcedorBenefitRedeemPayload)
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
        shippingMethod: payload!.shippingMethod?.trim().toLowerCase() || undefined,
        shippingCarrierId: payload!.shippingCarrierId,
        shippingCarrierName: payload!.shippingCarrierName?.trim() || undefined,
        shippingServiceName: payload!.shippingServiceName?.trim() || undefined,
        shippingPrice: payload!.shippingPrice,
        shippingDeliveryDays: payload!.shippingDeliveryDays,
      }
    : undefined
  const { data } = await api.post<TorcedorBenefitRedeemResponse>(
    `/api/benefits/offers/${offerId}/redeem`,
    body,
  )
  return data
}
