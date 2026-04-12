import { backofficeClient } from '../http/backofficeClient'
import type { MemberGatewayStatusDto } from './types'

export async function getMemberGatewayStatus(tenantId: string): Promise<MemberGatewayStatusDto> {
  const { data } = await backofficeClient.get<MemberGatewayStatusDto>(
    `/api/backoffice/payments/member-gateway/tenants/${tenantId}/status`,
  )
  return data
}

export async function setMemberGatewayProvider(tenantId: string, provider: string): Promise<void> {
  await backofficeClient.put(`/api/backoffice/payments/member-gateway/tenants/${tenantId}/provider`, {
    provider,
  })
}
