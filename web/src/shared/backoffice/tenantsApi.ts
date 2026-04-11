import axios from 'axios'
import { backofficeClient } from '../http/backofficeClient'
import type { PagedResult, TenantDetailDto, TenantListItemDto, TenantStatus } from './types'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

/** Valida a chave sem persistir (evita limpar sessão em fluxo de login). */
export async function validateBackofficeApiKey(apiKey: string): Promise<void> {
  await axios.get<PagedResult<TenantListItemDto>>(`${baseURL}/api/backoffice/tenants`, {
    params: { page: 1, pageSize: 1 },
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': apiKey.trim(),
    },
  })
}

export type CreateTenantBody = {
  name: string
  slug: string
}

export type UpdateTenantBody = {
  name?: string | null
  connectionString?: string | null
}

export type ChangeTenantStatusBody = {
  status: TenantStatus
}

export type AddDomainBody = {
  origin: string
}

export type AddSettingBody = {
  key: string
  value: string
}

export type UpdateSettingBody = {
  value: string
}

export async function createTenant(body: CreateTenantBody): Promise<{ id: string }> {
  const { data, headers } = await backofficeClient.post<{ id: string }>('/api/backoffice/tenants', body)
  if (data?.id) return data
  const loc = headers.location
  if (loc) {
    const m = /([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/i.exec(loc)
    if (m?.[1]) return { id: m[1] }
  }
  throw new Error('Resposta de criação de tenant sem id.')
}

export async function listTenants(params: {
  page?: number
  pageSize?: number
  search?: string | null
  status?: TenantStatus | null
}): Promise<PagedResult<TenantListItemDto>> {
  const { data } = await backofficeClient.get<PagedResult<TenantListItemDto>>('/api/backoffice/tenants', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      search: params.search || undefined,
      status: params.status ?? undefined,
    },
  })
  return data
}

export async function getTenantById(id: string): Promise<TenantDetailDto> {
  const { data } = await backofficeClient.get<TenantDetailDto>(`/api/backoffice/tenants/${id}`)
  return data
}

export async function updateTenant(id: string, body: UpdateTenantBody): Promise<void> {
  await backofficeClient.put(`/api/backoffice/tenants/${id}`, body)
}

export async function changeTenantStatus(id: string, body: ChangeTenantStatusBody): Promise<void> {
  await backofficeClient.patch(`/api/backoffice/tenants/${id}/status`, body)
}

export async function addTenantDomain(tenantId: string, body: AddDomainBody): Promise<{ domainId: string }> {
  const { data } = await backofficeClient.post<{ domainId: string }>(
    `/api/backoffice/tenants/${tenantId}/domains`,
    body,
  )
  return data
}

export async function removeTenantDomain(tenantId: string, domainId: string): Promise<void> {
  await backofficeClient.delete(`/api/backoffice/tenants/${tenantId}/domains/${domainId}`)
}

export async function addTenantSetting(tenantId: string, body: AddSettingBody): Promise<{ settingId: string }> {
  const { data } = await backofficeClient.post<{ settingId: string }>(
    `/api/backoffice/tenants/${tenantId}/settings`,
    body,
  )
  return data
}

export async function updateTenantSetting(
  tenantId: string,
  settingId: string,
  body: UpdateSettingBody,
): Promise<void> {
  await backofficeClient.put(`/api/backoffice/tenants/${tenantId}/settings/${settingId}`, body)
}

export async function removeTenantSetting(tenantId: string, settingId: string): Promise<void> {
  await backofficeClient.delete(`/api/backoffice/tenants/${tenantId}/settings/${settingId}`)
}
