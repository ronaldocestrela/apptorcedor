const ACCESS_KEY = 'apptorcedor_access'
const REFRESH_KEY = 'apptorcedor_refresh'

export const authStorage = {
  getAccess(): string | null {
    return sessionStorage.getItem(ACCESS_KEY)
  },
  getRefresh(): string | null {
    return sessionStorage.getItem(REFRESH_KEY)
  },
  setTokens(accessToken: string, refreshToken: string): void {
    sessionStorage.setItem(ACCESS_KEY, accessToken)
    sessionStorage.setItem(REFRESH_KEY, refreshToken)
  },
  clear(): void {
    sessionStorage.removeItem(ACCESS_KEY)
    sessionStorage.removeItem(REFRESH_KEY)
  },
}
