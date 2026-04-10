import type { TenantResolutionResult } from './types'
import { resolveTenantFromHostname } from './resolveTenantFromHostname'

let cachedSlug: string | null = null

/**
 * Resolve o tenant pelo `window.location.hostname`, atualiza o cache
 * usado por `getResolvedTenantSlug` e pelo interceptor HTTP.
 */
export function syncTenantFromWindow(): TenantResolutionResult {
  if (typeof window === 'undefined') {
    cachedSlug = null
    return { ok: false, reason: 'empty_host' }
  }

  const result = resolveTenantFromHostname(window.location.hostname)
  cachedSlug = result.ok ? result.slug : null
  return result
}

/** Slug em uso após `syncTenantFromWindow` bem-sucedido; caso contrário `null`. */
export function getResolvedTenantSlug(): string | null {
  return cachedSlug
}
