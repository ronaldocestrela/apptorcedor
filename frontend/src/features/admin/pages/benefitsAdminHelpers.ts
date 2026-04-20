export type OfferUiStatus = 'Inativa' | 'Programada' | 'Vigente' | 'Expirada'

/** Status exibido no admin a partir da oferta e do instante de referência (UTC). */
export function deriveOfferUiStatus(
  o: { isActive: boolean; startAt: string; endAt: string },
  nowMs: number = Date.now(),
): OfferUiStatus {
  if (!o.isActive) return 'Inativa'
  const start = Date.parse(o.startAt)
  const end = Date.parse(o.endAt)
  if (Number.isNaN(start) || Number.isNaN(end)) return 'Inativa'
  if (nowMs > end) return 'Expirada'
  if (nowMs < start) return 'Programada'
  return 'Vigente'
}

/** Converte ISO (API) para valor de input datetime-local (horário local). */
export function isoToDatetimeLocalValue(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  const y = d.getFullYear()
  const mo = pad(d.getMonth() + 1)
  const day = pad(d.getDate())
  const h = pad(d.getHours())
  const min = pad(d.getMinutes())
  return `${y}-${mo}-${day}T${h}:${min}`
}

/** Converte valor datetime-local para ISO UTC para a API. */
export function datetimeLocalValueToIso(value: string): string | null {
  if (!value.trim()) return null
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return null
  return d.toISOString()
}

export const MEMBERSHIP_STATUS_OPTIONS = [
  'NaoAssociado',
  'Ativo',
  'Inadimplente',
  'Suspenso',
  'Cancelado',
  'PendingPayment',
] as const

export function parseCommaSeparatedGuids(raw: string): string[] {
  return raw
    .split(/[,;\s]+/)
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}
