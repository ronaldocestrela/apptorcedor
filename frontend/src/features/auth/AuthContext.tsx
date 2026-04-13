import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api } from '../../shared/api/http'
import { authStorage } from '../../shared/auth/authStorage'

export type Me = {
  id: string
  email: string
  name: string
  roles: string[]
  permissions: string[]
}

type AuthContextValue = {
  user: Me | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshProfile: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Me | null>(null)
  const [loading, setLoading] = useState(true)

  const refreshProfile = useCallback(async () => {
    const { data } = await api.get<Me>('/api/auth/me')
    setUser({
      ...data,
      permissions: data.permissions ?? [],
    })
  }, [])

  useEffect(() => {
    const boot = async () => {
      if (!authStorage.getAccess()) {
        setLoading(false)
        return
      }
      try {
        await refreshProfile()
      } catch {
        authStorage.clear()
        setUser(null)
      } finally {
        setLoading(false)
      }
    }
    void boot()
  }, [refreshProfile])

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await api.post<{
      accessToken: string
      refreshToken: string }>('/api/auth/login', { email, password })
    authStorage.setTokens(data.accessToken, data.refreshToken)
    await refreshProfile()
  }, [refreshProfile])

  const logout = useCallback(async () => {
    const rt = authStorage.getRefresh()
    if (rt) {
      try {
        await api.post('/api/auth/logout', { refreshToken: rt })
      } catch {
        /* ignore */
      }
    }
    authStorage.clear()
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({ user, loading, login, logout, refreshProfile }),
    [user, loading, login, logout, refreshProfile],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

/** Paired with `AuthProvider` for the same context instance. */
// eslint-disable-next-line react-refresh/only-export-components -- hook must live beside provider
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx)
    throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
