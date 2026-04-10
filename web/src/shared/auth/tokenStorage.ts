import type { AuthResultDto } from './types'

const STORAGE_KEY = 'socioTorcedor.auth.session'

export type StoredAuthSession = {
  accessToken: string
  expiresAtUtc: string
}

function isExpired(isoUtc: string): boolean {
  const exp = Date.parse(isoUtc)
  if (Number.isNaN(exp)) return true
  return exp <= Date.now()
}

export function loadSession(): StoredAuthSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as StoredAuthSession
    if (!parsed?.accessToken || !parsed?.expiresAtUtc) return null
    if (isExpired(parsed.expiresAtUtc)) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    return parsed
  } catch {
    sessionStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function saveSessionFromAuthResult(dto: AuthResultDto): void {
  const session: StoredAuthSession = {
    accessToken: dto.accessToken,
    expiresAtUtc: dto.expiresAtUtc,
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
