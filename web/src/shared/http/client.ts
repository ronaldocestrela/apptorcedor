import axios from 'axios'
import { clearSession, getAccessToken } from '../auth/tokenStorage'
import { getResolvedTenantSlug } from '../tenant'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

/**
 * Cliente HTTP único da aplicação. Use este export para chamadas à API.
 * Base URL vem de `VITE_API_BASE_URL` (ver `.env.example`).
 */
export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const tenantSlug = getResolvedTenantSlug()
  if (tenantSlug) {
    config.headers.set('X-Tenant-Id', tenantSlug)
  }
  const token = getAccessToken()
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      clearSession()
      const path = typeof window !== 'undefined' ? window.location.pathname : ''
      if (path !== '/login' && path !== '/register') {
        window.location.assign('/login')
      }
    }
    return Promise.reject(error)
  },
)
