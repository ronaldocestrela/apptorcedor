import { apiClient } from '../http/client'
import type { AuthResultDto, LoginRequest, RegisterRequest } from './types'

export async function login(request: LoginRequest): Promise<AuthResultDto> {
  const { data } = await apiClient.post<AuthResultDto>('/api/auth/login', request)
  return data
}

export async function register(request: RegisterRequest): Promise<AuthResultDto> {
  const { data } = await apiClient.post<AuthResultDto>('/api/auth/register', request)
  return data
}
