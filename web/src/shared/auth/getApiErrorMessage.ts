import axios from 'axios'
import type { ApiErrorBody } from './types'

export function getApiErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as ApiErrorBody | undefined
    if (data?.message && typeof data.message === 'string') {
      return data.message
    }
  }
  if (err instanceof Error && err.message) {
    return err.message
  }
  return fallback
}
