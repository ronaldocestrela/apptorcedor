import { afterEach, describe, expect, it, vi } from 'vitest'
import { getTenantSlugFromBrowser } from '../getTenantSlugFromBrowser'

describe('getTenantSlugFromBrowser', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns slug when hostname resolves', () => {
    vi.stubGlobal('window', { location: { hostname: 'feira.localhost' } })
    expect(getTenantSlugFromBrowser()).toBe('feira')
  })

  it('returns null when hostname invalid', () => {
    vi.stubGlobal('window', { location: { hostname: 'localhost' } })
    expect(getTenantSlugFromBrowser()).toBeNull()
  })
})
