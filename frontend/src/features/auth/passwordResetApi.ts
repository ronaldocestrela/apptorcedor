import { api } from '../../shared/api/http'
import { isAxiosError } from 'axios'

export async function requestPasswordReset(email: string): Promise<void> {
  await api.post('/api/auth/forgot-password', { email: email.trim() })
}

export async function resetPassword(email: string, token: string, newPassword: string): Promise<void> {
  await api.post('/api/auth/reset-password', {
    email: email.trim(),
    token,
    newPassword,
  })
}

export function formatResetPasswordApiErrorMessage(err: unknown): string {
  if (!isAxiosError(err))
    return 'Não foi possível redefinir a senha.'
  const data = err.response?.data as { errors?: string[] } | undefined
  if (data?.errors?.length)
    return data.errors.join(' ')
  return 'Não foi possível redefinir a senha.'
}
