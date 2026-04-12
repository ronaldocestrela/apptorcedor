import { normalizeMemberGatewayStatus } from '../payments/memberGatewayStatus'
import { backofficeClient } from '../http/backofficeClient'
import type { MemberGatewayStatusDto } from './types'

export async function getMemberGatewayStatus(tenantId: string): Promise<MemberGatewayStatusDto> {
  const { data } = await backofficeClient.get<unknown>(
    `/api/backoffice/payments/member-gateway/tenants/${tenantId}/status`,
  )
  return normalizeMemberGatewayStatus(data)
}

export async function setMemberGatewayProvider(tenantId: string, provider: string): Promise<void> {
  await backofficeClient.put(`/api/backoffice/payments/member-gateway/tenants/${tenantId}/provider`, {
    provider,
  })
}
