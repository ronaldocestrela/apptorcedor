import { backofficeClient } from '../http/backofficeClient'
import type { BillingCycle, PagedResult, TenantPlanDto, TenantPlanSummaryDto } from './types'

export type AssignTenantPlanBody = {
  tenantId: string
  saaSPlanId: string
  startDate: string
  endDate?: string | null
  billingCycle: BillingCycle
}

export async function assignPlanToTenant(body: AssignTenantPlanBody): Promise<{ id: string }> {
  const { data } = await backofficeClient.post<{ id: string }>('/api/backoffice/tenant-plans', body)
  return data
}

export async function revokeTenantPlan(assignmentId: string): Promise<void> {
  await backofficeClient.delete(`/api/backoffice/tenant-plans/${assignmentId}`)
}

export async function getTenantPlanByTenant(tenantId: string): Promise<TenantPlanDto> {
  const { data } = await backofficeClient.get<TenantPlanDto>(`/api/backoffice/tenant-plans/tenant/${tenantId}`)
  return data
}

export async function listTenantsByPlan(
  planId: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResult<TenantPlanSummaryDto>> {
  const { data } = await backofficeClient.get<PagedResult<TenantPlanSummaryDto>>(
    `/api/backoffice/tenant-plans/plan/${planId}`,
    { params: { page, pageSize } },
  )
  return data
}
