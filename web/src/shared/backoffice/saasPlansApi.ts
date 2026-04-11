import { backofficeClient } from '../http/backofficeClient'
import type { PagedResult, SaaSPlanDetailDto, SaaSPlanDto } from './types'

export type SaaSPlanFeatureBody = {
  key: string
  description?: string | null
  value?: string | null
}

export type CreateSaaSPlanBody = {
  name: string
  description?: string | null
  monthlyPrice: number
  yearlyPrice?: number | null
  maxMembers: number
  stripePriceMonthlyId?: string | null
  stripePriceYearlyId?: string | null
  features?: SaaSPlanFeatureBody[] | null
}

export type UpdateSaaSPlanBody = CreateSaaSPlanBody

export async function createSaaSPlan(body: CreateSaaSPlanBody): Promise<{ id: string }> {
  const { data } = await backofficeClient.post<{ id: string }>('/api/backoffice/plans', body)
  return data
}

export async function listSaaSPlans(page = 1, pageSize = 20): Promise<PagedResult<SaaSPlanDto>> {
  const { data } = await backofficeClient.get<PagedResult<SaaSPlanDto>>('/api/backoffice/plans', {
    params: { page, pageSize },
  })
  return data
}

export async function getSaaSPlanById(id: string): Promise<SaaSPlanDetailDto> {
  const { data } = await backofficeClient.get<SaaSPlanDetailDto>(`/api/backoffice/plans/${id}`)
  return data
}

export async function updateSaaSPlan(id: string, body: UpdateSaaSPlanBody): Promise<void> {
  await backofficeClient.put(`/api/backoffice/plans/${id}`, body)
}

export async function toggleSaaSPlan(id: string): Promise<void> {
  await backofficeClient.patch(`/api/backoffice/plans/${id}/toggle`)
}
