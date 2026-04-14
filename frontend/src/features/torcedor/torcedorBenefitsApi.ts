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
