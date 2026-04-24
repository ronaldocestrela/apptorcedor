import { isAxiosError } from 'axios'
import { api } from '../../shared/api/http'

const apiOrigin = () => (import.meta.env.VITE_API_URL ?? 'http://localhost:5031').replace(/\/$/, '')

/** Absolute URL for API-served static files (e.g. profile photos). */
export function resolvePublicAssetUrl(path: string | null | undefined): string | undefined {
  if (!path)
    return undefined
  if (path.startsWith('http://') || path.startsWith('https://'))
    return path
  return `${apiOrigin()}${path.startsWith('/') ? path : `/${path}`}`
}

export type RegistrationRequirements = {
  termsOfUseVersionId: string
  privacyPolicyVersionId: string
  termsTitle: string
  privacyTitle: string
}

export async function getRegistrationRequirements(): Promise<RegistrationRequirements> {
  const { data } = await api.get<RegistrationRequirements>('/api/account/register/requirements')
  return data
}

export type RegisterPayload = {
  name: string
  email: string
  password: string
  phoneNumber: string
  acceptedLegalDocumentVersionIds: string[]
}

export async function registerPublic(payload: RegisterPayload) {
  const { data } = await api.post<{ accessToken: string; refreshToken: string }>(
    '/api/account/register',
    {
      name: payload.name,
      email: payload.email,
      password: payload.password,
      phoneNumber: payload.phoneNumber.trim(),
      acceptedLegalDocumentVersionIds: payload.acceptedLegalDocumentVersionIds,
    },
  )
  return data
}

/** Mensagens de validação em `400 { errors: string[] }` do cadastro público. */
export function formatRegisterApiErrorMessage(err: unknown, fallback: string): string {
  if (!isAxiosError(err))
    return fallback
  const data = err.response?.data
  if (!data || typeof data !== 'object')
    return fallback
  const raw = (data as { errors?: unknown }).errors
  if (!Array.isArray(raw) || raw.length === 0)
    return fallback
  const strings = raw.filter((e): e is string => typeof e === 'string')
  if (strings.length === 0)
    return fallback
  return strings.join(' ')
}

export type MyProfile = {
  document: string | null
  birthDate: string | null
  photoUrl: string | null
  address: string | null
}

export async function getMyProfile(): Promise<MyProfile> {
  const { data } = await api.get<MyProfile>('/api/account/profile')
  return data
}

export async function upsertMyProfile(patch: {
  document?: string | null
  birthDate?: string | null
  photoUrl?: string | null
  address?: string | null
}): Promise<void> {
  await api.put('/api/account/profile', patch)
}

export async function uploadProfilePhoto(file: File): Promise<string> {
  const fd = new FormData()
  fd.append('file', file)
  const { data } = await api.post<{ photoUrl: string }>('/api/account/profile/photo', fd, {
    transformRequest: (body, headers) => {
      if (body instanceof FormData)
        delete headers['Content-Type']
      return body
    },
  })
  return data.photoUrl
}
