const STORAGE_KEY = 'socioTorcedor.backoffice.apiKey'

export function getBackofficeApiKey(): string | null {
  if (typeof sessionStorage === 'undefined') {
    return null
  }
  const v = sessionStorage.getItem(STORAGE_KEY)
  return v && v.trim().length > 0 ? v.trim() : null
}

export function setBackofficeApiKey(apiKey: string): void {
  sessionStorage.setItem(STORAGE_KEY, apiKey.trim())
}

export function clearBackofficeSession(): void {
  sessionStorage.removeItem(STORAGE_KEY)
}
