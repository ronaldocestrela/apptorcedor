import { apiClient } from '../http/client'
import type { MemberGatewayStatusDto } from '../backoffice/types'

export type ConfigureStripeDirectBody = {
  secretKey: string
  publishableKey?: string | null
  webhookSecret?: string | null
}

export async function getTenantMemberGatewayStatus(): Promise<MemberGatewayStatusDto> {
  const { data } = await apiClient.get<MemberGatewayStatusDto>('/api/payments/admin/member-gateway')
  return data
}

export async function configureTenantStripeDirect(body: ConfigureStripeDirectBody): Promise<void> {
  await apiClient.put('/api/payments/admin/member-gateway/stripe-direct', body)
}
