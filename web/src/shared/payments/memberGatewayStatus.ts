import type { MemberGatewayStatusDto } from '../backoffice/types'

function pickString(obj: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const k of keys) {
    const v = obj[k]
    if (typeof v === 'string') {
      return v
    }
  }
  return undefined
}

function pickBool(obj: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const k of keys) {
    const v = obj[k]
    if (typeof v === 'boolean') {
      return v
    }
  }
  return undefined
}

/**
 * Normaliza o payload de `MemberGatewayStatusDto` vindo da API (camelCase padrão
 * ou PascalCase) para o formato usado no front.
 */
export function normalizeMemberGatewayStatus(raw: unknown): MemberGatewayStatusDto {
  const r = raw as Record<string, unknown>
  const selected = pickString(r, 'selectedProvider', 'SelectedProvider')?.trim()
  const statusStr = pickString(r, 'status', 'Status')?.trim()
  const pkHint = pickString(r, 'publishableKeyHint', 'PublishableKeyHint')
  const webhook = pickBool(r, 'webhookSecretConfigured', 'WebhookSecretConfigured')

  return {
    selectedProvider: selected && selected.length > 0 ? selected : 'None',
    status: statusStr && statusStr.length > 0 ? statusStr : 'Unknown',
    publishableKeyHint: pkHint && pkHint.length > 0 ? pkHint : null,
    webhookSecretConfigured: webhook ?? false,
  }
}

/** True quando o backoffice atribuiu cobrança de sócios via Stripe direto (conta do clube). */
export function isStripeDirectProvider(provider: string | undefined | null): boolean {
  return String(provider ?? '')
    .trim()
    .toLowerCase() === 'stripedirect'
}
