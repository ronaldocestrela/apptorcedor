import axios from 'axios'

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
  // Futuro: injetar slug do tenant, ex.:
  // const tenantId = getTenantSlug()
  // if (tenantId) config.headers.set('X-Tenant-Id', tenantId)
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error),
)
