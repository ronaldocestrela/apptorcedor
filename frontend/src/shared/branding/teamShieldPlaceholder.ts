/** Default SVG when no club shield is configured in admin (Tipo B — Brand.TeamShieldUrl). */
export function getTeamShieldPlaceholderDataUrl(): string {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none"><rect width="64" height="64" rx="12" fill="#1a2229"/><path d="M32 10 L50 20 V34 C50 48 32 58 32 58 C32 58 14 48 14 34 V20 L32 10 Z" fill="#243038" stroke="#8cd392" stroke-width="2"/></svg>`
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`
}
