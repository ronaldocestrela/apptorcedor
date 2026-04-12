import { apiClient } from '../http/client'
import type {
  PagedResult,
  TenantSaasBillingInvoiceDto,
  TenantSaasBillingSubscriptionDto,
  TenantSaasPaymentMethodDto,
  TenantSaasSetupIntentDto,
  TenantSaasStripeConfigDto,
} from '../backoffice/types'

export async function getAdminSaasStripeConfig(): Promise<TenantSaasStripeConfigDto> {
  const { data } = await apiClient.get<TenantSaasStripeConfigDto>('/api/payments/admin/saas/stripe-config')
  return data
}

export async function getAdminSaasSubscription(): Promise<TenantSaasBillingSubscriptionDto | null> {
  const { data } = await apiClient.get<TenantSaasBillingSubscriptionDto | null>(
    '/api/payments/admin/saas/subscription',
  )
  return data
}

export async function listAdminSaasInvoices(
  page = 1,
  pageSize = 20,
): Promise<PagedResult<TenantSaasBillingInvoiceDto>> {
  const { data } = await apiClient.get<PagedResult<TenantSaasBillingInvoiceDto>>(
    '/api/payments/admin/saas/invoices',
    { params: { page, pageSize } },
  )
  return data
}

export async function listAdminSaasCards(): Promise<TenantSaasPaymentMethodDto[]> {
  const { data } = await apiClient.get<TenantSaasPaymentMethodDto[]>('/api/payments/admin/saas/cards')
  return data
}

export async function createAdminSaasSetupIntent(): Promise<TenantSaasSetupIntentDto> {
  const { data } = await apiClient.post<TenantSaasSetupIntentDto>(
    '/api/payments/admin/saas/cards/setup-intent',
  )
  return data
}

export type AttachAdminSaasCardBody = {
  paymentMethodId: string
  setAsDefault?: boolean
}

export async function attachAdminSaasCard(body: AttachAdminSaasCardBody): Promise<void> {
  await apiClient.post('/api/payments/admin/saas/cards', {
    paymentMethodId: body.paymentMethodId,
    setAsDefault: body.setAsDefault ?? true,
  })
}

export async function deleteAdminSaasCard(paymentMethodId: string): Promise<void> {
  await apiClient.delete(`/api/payments/admin/saas/cards/${encodeURIComponent(paymentMethodId)}`)
}
