import { resolveTenantFromHostname } from './resolveTenantFromHostname'

/**
 * Slug do tenant a partir do hostname atual do navegador.
 * Não depende do cache de `syncTenantFromWindow` — útil para interceptors HTTP.
 */
export function getTenantSlugFromBrowser(): string | null {
  if (typeof window === 'undefined') {
    return null
  }
  const result = resolveTenantFromHostname(window.location.hostname)
  return result.ok ? result.slug : null
}
