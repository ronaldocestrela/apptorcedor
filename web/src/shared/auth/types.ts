/** Resposta de login/cadastro (serialização camelCase da API). */
export type AuthResultDto = {
  accessToken: string
  expiresAtUtc: string
}

export type LoginRequest = {
  email: string
  password: string
}

export type RegisterRequest = {
  email: string
  password: string
  firstName: string
  lastName: string
}

export type ApiErrorBody = {
  code?: string
  message?: string
}
