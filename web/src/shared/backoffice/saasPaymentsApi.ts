import { backofficeClient } from '../http/backofficeClient'
import type { PagedResult, TenantSaasBillingInvoiceDto, TenantSaasBillingSubscriptionDto, TenantSaasPortalSessionDto } from './types'

export async function startTenantSaasBilling(tenantId: string): Promise<{ id: string }> {
  const { data } = await backofficeClient.post<{ id: string }>(
    `/api/backoffice/payments/saas/tenants/${tenantId}/billing/start`,
  )
  return data
}

export async function getTenantSaasSubscription(
  tenantId: string,
): Promise<TenantSaasBillingSubscriptionDto | null> {
  const { data } = await backofficeClient.get<TenantSaasBillingSubscriptionDto | null>(
    `/api/backoffice/payments/saas/tenants/${tenantId}/subscription`,
  )
  return data
}

export async function listTenantSaasInvoices(
  tenantId: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResult<TenantSaasBillingInvoiceDto>> {
  const { data } = await backofficeClient.get<PagedResult<TenantSaasBillingInvoiceDto>>(
    `/api/backoffice/payments/saas/tenants/${tenantId}/invoices`,
    { params: { page, pageSize } },
  )
  return data
}

export async function createTenantSaasPortalSession(
  tenantId: string,
  returnUrl: string,
): Promise<TenantSaasPortalSessionDto> {
  const { data } = await backofficeClient.post<TenantSaasPortalSessionDto>(
    `/api/backoffice/payments/saas/tenants/${tenantId}/billing/portal`,
    { returnUrl },
  )
  return data
}
