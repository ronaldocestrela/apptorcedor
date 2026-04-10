import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { login as loginApi, register as registerApi } from '../../shared/auth/authApi'
import {
  clearSession,
  loadSession,
  saveSessionFromAuthResult,
  type StoredAuthSession,
} from '../../shared/auth/tokenStorage'
import type { LoginRequest, RegisterRequest } from '../../shared/auth/types'
import { AuthContext } from './authContext'
import type { AuthContextValue } from './authTypes'

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [session, setSession] = useState<StoredAuthSession | null>(() => loadSession())

  const login = useCallback(async (body: LoginRequest) => {
    const result = await loginApi(body)
    saveSessionFromAuthResult(result)
    setSession(loadSession())
  }, [])

  const register = useCallback(async (body: RegisterRequest) => {
    const result = await registerApi(body)
    saveSessionFromAuthResult(result)
    setSession(loadSession())
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
    navigate('/login', { replace: true })
  }, [navigate])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: session != null,
      login,
      register,
      logout,
    }),
    [session, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
