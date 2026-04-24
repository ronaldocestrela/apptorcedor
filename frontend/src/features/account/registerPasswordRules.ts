export type PasswordRuleId = 'minLength' | 'uppercase' | 'lowercase' | 'digit'

export type PasswordRuleDefinition = {
  id: PasswordRuleId
  label: string
  test: (password: string) => boolean
}

/**
 * Regras alinhadas ao ASP.NET Identity em
 * `AppTorcedor.Infrastructure/DependencyInjection.cs` (PasswordOptions).
 * Caractere não alfanumérico não é exigido.
 */
export const PUBLIC_REGISTER_PASSWORD_RULES: readonly PasswordRuleDefinition[] = [
  { id: 'minLength', label: 'Mínimo de 8 caracteres', test: (p) => p.length >= 8 },
  { id: 'uppercase', label: 'Pelo menos uma letra maiúscula', test: (p) => /[A-Z]/.test(p) },
  { id: 'lowercase', label: 'Pelo menos uma letra minúscula', test: (p) => /[a-z]/.test(p) },
  { id: 'digit', label: 'Pelo menos um número', test: (p) => /\d/.test(p) },
] as const

export function publicRegisterPasswordMeetsAllRules(password: string): boolean {
  return PUBLIC_REGISTER_PASSWORD_RULES.every((r) => r.test(password))
}

export function evaluatePublicRegisterPasswordRules(password: string): Array<{
  id: PasswordRuleId
  label: string
  met: boolean
}> {
  return PUBLIC_REGISTER_PASSWORD_RULES.map((r) => ({
    id: r.id,
    label: r.label,
    met: r.test(password),
  }))
}
