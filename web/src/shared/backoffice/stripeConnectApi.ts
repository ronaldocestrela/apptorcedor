import { backofficeClient } from '../http/backofficeClient'
import type { StripeConnectStatusDto, StripeOnboardingLinkDto } from './types'

export type StripeOnboardingBody = {
  refreshUrl: string
  returnUrl: string
}

export async function startStripeConnectOnboarding(
  tenantId: string,
  body: StripeOnboardingBody,
): Promise<StripeOnboardingLinkDto> {
  const { data } = await backofficeClient.post<StripeOnboardingLinkDto>(
    `/api/backoffice/payments/connect/tenants/${tenantId}/onboarding`,
    body,
  )
  return data
}

export async function getStripeConnectStatus(tenantId: string): Promise<StripeConnectStatusDto> {
  const { data } = await backofficeClient.get<StripeConnectStatusDto>(
    `/api/backoffice/payments/connect/tenants/${tenantId}/status`,
  )
  return data
}
