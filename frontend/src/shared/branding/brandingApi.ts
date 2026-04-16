import { api } from '../api/http'

export type PublicBranding = {
  teamShieldUrl: string | null
}

export async function getPublicBranding(): Promise<PublicBranding> {
  const { data } = await api.get<PublicBranding>('/api/branding')
  return data
}
