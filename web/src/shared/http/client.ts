import axios from 'axios'
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
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error),
)
