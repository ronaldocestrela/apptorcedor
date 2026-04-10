import type { TenantResolutionResult } from './types'

const ipv4Regex = /^(?:\d{1,3}\.){3}\d{1,3}$/

function isLikelyIpv4(host: string): boolean {
  if (!ipv4Regex.test(host)) return false
  const octets = host.split('.')
  return octets.every((o) => {
    const n = Number(o)
    return Number.isInteger(n) && n >= 0 && n <= 255
  })
}

/**
 * Extrai o slug do tenant a partir do hostname: primeiro rótulo DNS,
 * exceto quando inválido (`www`, apex, localhost, IP, host vazio).
 * Slug normalizado: trim + lowercase (alinhado ao backend).
 */
export function resolveTenantFromHostname(hostname: string): TenantResolutionResult {
  const host = hostname.trim().toLowerCase()

  if (!host) {
    return { ok: false, reason: 'empty_host' }
  }

  if (host === 'localhost' || isLikelyIpv4(host)) {
    return { ok: false, reason: 'localhost_or_ip' }
  }

  const parts = host.split('.').filter((p) => p.length > 0)
  if (parts.length < 2) {
    return { ok: false, reason: 'no_subdomain' }
  }

  const rawSlug = parts[0]
  if (!rawSlug) {
    return { ok: false, reason: 'no_subdomain' }
  }

  if (rawSlug === 'www') {
    return { ok: false, reason: 'www_reserved' }
  }

  const slug = rawSlug.trim().toLowerCase()
  if (!slug) {
    return { ok: false, reason: 'no_subdomain' }
  }

  return { ok: true, slug }
}
