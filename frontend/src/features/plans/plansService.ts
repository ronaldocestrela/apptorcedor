import { api } from '../../shared/api/http'

export type TorcedorPublishedPlanBenefit = {
  benefitId: string
  title: string
  description: string | null
}

export type TorcedorPublishedPlan = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
  summary: string | null
  benefits: TorcedorPublishedPlanBenefit[]
}

export type TorcedorPublishedPlansCatalog = {
  items: TorcedorPublishedPlan[]
}

export type TorcedorPublishedPlanDetailBenefit = {
  benefitId: string
  sortOrder: number
  title: string
  description: string | null
}

export type TorcedorPublishedPlanDetail = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
  summary: string | null
  rulesNotes: string | null
  benefits: TorcedorPublishedPlanDetailBenefit[]
}

export const plansService = {
  async listPublished(): Promise<TorcedorPublishedPlansCatalog> {
    const { data } = await api.get<TorcedorPublishedPlansCatalog>('/api/plans')
    return data
  },

  async getById(planId: string): Promise<TorcedorPublishedPlanDetail> {
    const { data } = await api.get<TorcedorPublishedPlanDetail>(`/api/plans/${encodeURIComponent(planId)}`)
    return data
  },
}
