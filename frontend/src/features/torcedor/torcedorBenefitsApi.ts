import { api } from '../../shared/api/http'

export type TorcedorEligibleBenefitOffer = {
  offerId: string
  partnerId: string
  partnerName: string
  title: string
  description: string | null
  startAt: string
  endAt: string
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
}

export async function getEligibleBenefitOfferDetail(offerId: string): Promise<TorcedorEligibleBenefitOfferDetail> {
  const { data } = await api.get<TorcedorEligibleBenefitOfferDetail>(`/api/benefits/offers/${offerId}`)
  return data
}

export type TorcedorBenefitRedeemResponse = {
  redemptionId: string
}

export async function redeemBenefitOffer(offerId: string): Promise<TorcedorBenefitRedeemResponse> {
  const { data } = await api.post<TorcedorBenefitRedeemResponse>(`/api/benefits/offers/${offerId}/redeem`)
  return data
}
