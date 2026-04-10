export type TenantResolutionFailureReason =
  | 'empty_host'
  | 'localhost_or_ip'
  | 'no_subdomain'
  | 'www_reserved'

export type TenantResolutionResult =
  | { ok: true; slug: string }
  | { ok: false; reason: TenantResolutionFailureReason }
