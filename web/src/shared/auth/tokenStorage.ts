import type { AuthResultDto } from './types'

const STORAGE_KEY = 'socioTorcedor.auth.session'

const ROLE_CLAIM_LONG =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' as const

export type StoredAuthSession = {
  accessToken: string
  expiresAtUtc: string
  roles: string[]
}

function isExpired(isoUtc: string): boolean {
  const exp = Date.parse(isoUtc)
  if (Number.isNaN(exp)) return true
  return exp <= Date.now()
}

function decodeJwtPayloadSegment(segment: string): Record<string, unknown> | null {
  try {
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
    const pad = base64.length % 4
    const padded = pad ? base64 + '='.repeat(4 - pad) : base64
    const json = atob(padded)
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

function decodeJwtRoles(token: string): string[] {
  const parts = token.split('.')
  if (parts.length < 2) return []
  const payload = decodeJwtPayloadSegment(parts[1])
  if (!payload) return []
  const raw = payload['role'] ?? payload[ROLE_CLAIM_LONG]
  if (Array.isArray(raw)) return raw.filter((r): r is string => typeof r === 'string')
  if (typeof raw === 'string') return [raw]
  return []
}

function normalizeSession(parsed: Record<string, unknown>): StoredAuthSession | null {
  const accessToken = parsed['accessToken']
  const expiresAtUtc = parsed['expiresAtUtc']
  if (typeof accessToken !== 'string' || typeof expiresAtUtc !== 'string') return null
  let roles: string[] = []
  const rawRoles = parsed['roles']
  if (Array.isArray(rawRoles)) {
    roles = rawRoles.filter((r): r is string => typeof r === 'string')
  }
  if (roles.length === 0) {
    roles = decodeJwtRoles(accessToken)
  }
  return {
    accessToken,
    expiresAtUtc,
    roles,
  }
}

export function loadSession(): StoredAuthSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as unknown
    if (typeof parsed !== 'object' || parsed === null) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    const session = normalizeSession(parsed as Record<string, unknown>)
    if (!session) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    if (isExpired(session.expiresAtUtc)) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    return session
  } catch {
    sessionStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function saveSessionFromAuthResult(dto: AuthResultDto): void {
  const session: StoredAuthSession = {
    accessToken: dto.accessToken,
    expiresAtUtc: dto.expiresAtUtc,
    roles: decodeJwtRoles(dto.accessToken),
  }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}

export function clearSession(): void {
  sessionStorage.removeItem(STORAGE_KEY)
}

/** Token atual para interceptors; `null` se ausente ou expirado. */
export function getAccessToken(): string | null {
  const s = loadSession()
  return s?.accessToken ?? null
}
