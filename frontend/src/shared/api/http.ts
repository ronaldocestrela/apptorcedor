import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { authStorage } from '../auth/authStorage'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5031'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

let refreshPromise: Promise<string | null> | null = null

api.interceptors.request.use((config) => {
  const token = authStorage.getAccess()
  if (token)
    config.headers.Authorization = `Bearer ${token}`
  return config
})

type RetryConfig = InternalAxiosRequestConfig & { _retry?: boolean }

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetryConfig | undefined
    if (!original || original._retry)
      return Promise.reject(error)
    if (error.response?.status !== 401)
      return Promise.reject(error)

    const url = original.url ?? ''
    if (url.includes('/api/auth/login') || url.includes('/api/auth/refresh'))
      return Promise.reject(error)

    const refresh = authStorage.getRefresh()
    if (!refresh)
      return Promise.reject(error)

    original._retry = true
    try {
      if (!refreshPromise) {
        refreshPromise = api
          .post<{ accessToken: string; refreshToken: string }>('/api/auth/refresh', {
            refreshToken: refresh,
          })
          .then((res) => {
            const { accessToken, refreshToken } = res.data
            authStorage.setTokens(accessToken, refreshToken)
            return accessToken
          })
          .catch(() => {
            authStorage.clear()
            return null
          })
          .finally(() => {
            refreshPromise = null
          })
      }

      const newAccess = await refreshPromise
      if (!newAccess)
        return Promise.reject(error)

      original.headers.Authorization = `Bearer ${newAccess}`
      return api(original)
    } catch {
      return Promise.reject(error)
    }
  },
)
