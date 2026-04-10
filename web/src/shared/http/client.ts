import axios, { type InternalAxiosRequestConfig } from 'axios'
import { clearSession, getAccessToken } from '../auth/tokenStorage'
import { getTenantSlugFromBrowser } from '../tenant'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

function setRequestHeader(config: InternalAxiosRequestConfig, name: string, value: string) {
  const headers = config.headers
  if (!headers) {
    return
  }
  if (typeof headers.set === 'function') {
    headers.set(name, value)
    return
  }
  ;(headers as Record<string, string>)[name] = value
}

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
  const tenantSlug = getTenantSlugFromBrowser()
  if (tenantSlug) {
    setRequestHeader(config, 'X-Tenant-Id', tenantSlug)
  }
  const token = getAccessToken()
  if (token) {
    setRequestHeader(config, 'Authorization', `Bearer ${token}`)
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
