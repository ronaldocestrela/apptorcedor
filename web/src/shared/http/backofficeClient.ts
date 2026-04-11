import axios, { type InternalAxiosRequestConfig } from 'axios'
import { clearBackofficeSession, getBackofficeApiKey } from '../backoffice/backofficeSession'

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
 * Cliente HTTP para rotas `api/backoffice/*` — envia `X-Api-Key` (sem JWT nem `X-Tenant-Id`).
 */
export const backofficeClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
})

backofficeClient.interceptors.request.use((config) => {
  const key = getBackofficeApiKey()
  if (key) {
    setRequestHeader(config, 'X-Api-Key', key)
  }
  return config
})

backofficeClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      clearBackofficeSession()
      const path = typeof window !== 'undefined' ? window.location.pathname : ''
      if (!path.startsWith('/backoffice/login')) {
        window.location.assign('/backoffice/login')
      }
    }
    return Promise.reject(error)
  },
)
