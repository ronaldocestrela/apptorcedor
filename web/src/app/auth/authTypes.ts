import type { StoredAuthSession } from '../../shared/auth/tokenStorage'
import type { LoginRequest, RegisterRequest } from '../../shared/auth/types'

export type AuthContextValue = {
  session: StoredAuthSession | null
  isAuthenticated: boolean
  login: (body: LoginRequest) => Promise<void>
  register: (body: RegisterRequest) => Promise<void>
  logout: () => void
}
