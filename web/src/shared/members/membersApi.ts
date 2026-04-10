import { apiClient } from '../http/client'

export type MemberAddress = {
  street: string
  number: string
  complement?: string | null
  neighborhood: string
  city: string
  state: string
  zipCode: string
}

/** Resposta de `GET /api/members/me` (JSON camelCase). Enums numéricos como no backend. */
export type MemberProfile = {
  id: string
  userId: string
  cpfDigits: string
  dateOfBirth: string
  gender: number
  phone: string
  address: MemberAddress
  status: number
  createdAt: string
  updatedAt?: string | null
}

export async function fetchMyMemberProfile(): Promise<MemberProfile> {
  const { data } = await apiClient.get<MemberProfile>('/api/members/me')
  return data
}
