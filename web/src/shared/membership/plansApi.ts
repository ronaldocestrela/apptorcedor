import { apiClient } from '../http/client'

export type VantagemItem = {
  descricao: string
}

/** Alinhado a `MemberPlanDto` da API (JSON camelCase). */
export type MemberPlanDto = {
  id: string
  nome: string
  descricao: string | null
  preco: number
  isActive: boolean
  vantagens: VantagemItem[]
  createdAt: string
  updatedAt: string
}

export type PagedMemberPlans = {
  items: MemberPlanDto[]
  totalCount: number
  page: number
  pageSize: number
}

export type CreateMemberPlanBody = {
  nome: string
  descricao?: string | null
  preco: number
  vantagens?: string[] | null
}

export type UpdateMemberPlanBody = CreateMemberPlanBody

export async function listMemberPlans(page = 1, pageSize = 20): Promise<PagedMemberPlans> {
  const { data } = await apiClient.get<PagedMemberPlans>('/api/plans', {
    params: { page, pageSize },
  })
  return data
}

export async function getMemberPlan(id: string): Promise<MemberPlanDto> {
  const { data } = await apiClient.get<MemberPlanDto>(`/api/plans/${id}`)
  return data
}

export async function createMemberPlan(body: CreateMemberPlanBody): Promise<MemberPlanDto> {
  const { data } = await apiClient.post<MemberPlanDto>('/api/plans', body)
  return data
}

export async function updateMemberPlan(id: string, body: UpdateMemberPlanBody): Promise<MemberPlanDto> {
  const { data } = await apiClient.put<MemberPlanDto>(`/api/plans/${id}`, body)
  return data
}

export async function toggleMemberPlan(id: string): Promise<MemberPlanDto> {
  const { data } = await apiClient.patch<MemberPlanDto>(`/api/plans/${id}/toggle`)
  return data
}
