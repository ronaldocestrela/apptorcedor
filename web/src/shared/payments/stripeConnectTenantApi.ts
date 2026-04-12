import { apiClient } from '../http/client'
import type { StripeConnectStatusDto, StripeOnboardingLinkDto } from '../backoffice/types'

export type TenantStripeOnboardingBody = {
  refreshUrl: string
  returnUrl: string
}

export async function startTenantStripeConnectOnboarding(
  body: TenantStripeOnboardingBody,
): Promise<StripeOnboardingLinkDto> {
  const { data } = await apiClient.post<StripeOnboardingLinkDto>(
    '/api/payments/admin/connect/onboarding',
    body,
  )
  return data
}

export async function getTenantStripeConnectStatus(): Promise<StripeConnectStatusDto> {
  const { data } = await apiClient.get<StripeConnectStatusDto>('/api/payments/admin/connect/status')
  return data
}

/** Consulta a Stripe, persiste no master e retorna o status atualizado. */
export async function syncTenantStripeConnectStatus(): Promise<StripeConnectStatusDto> {
  const { data } = await apiClient.post<StripeConnectStatusDto>('/api/payments/admin/connect/sync')
  return data
}
