import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  clearSession,
  decodeJwtBasicClaims,
  getAccessToken,
  loadSession,
  saveSessionFromAuthResult,
} from '../tokenStorage'

describe('tokenStorage', () => {
  beforeEach(() => {
    sessionStorage.clear()
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2025-06-01T12:00:00.000Z'))
  })

  afterEach(() => {
    sessionStorage.clear()
    vi.useRealTimers()
  })

  it('saveSessionFromAuthResult and loadSession roundtrip', () => {
    const exp = new Date('2026-01-01T00:00:00.000Z').toISOString()
    saveSessionFromAuthResult({
      accessToken: 'a.b.c',
      expiresAtUtc: exp,
    })
    const s = loadSession()
    expect(s).not.toBeNull()
    expect(s?.accessToken).toBe('a.b.c')
    expect(s?.expiresAtUtc).toBe(exp)
  })

  it('getAccessToken returns token when valid', () => {
    saveSessionFromAuthResult({
      accessToken: 'tok',
      expiresAtUtc: new Date('2026-01-01T00:00:00.000Z').toISOString(),
    })
    expect(getAccessToken()).toBe('tok')
  })

  it('loadSession removes expired session', () => {
    saveSessionFromAuthResult({
      accessToken: 'tok',
      expiresAtUtc: new Date('2024-01-01T00:00:00.000Z').toISOString(),
    })
    expect(loadSession()).toBeNull()
    expect(sessionStorage.getItem('socioTorcedor.auth.session')).toBeNull()
  })

  it('clearSession removes storage', () => {
    saveSessionFromAuthResult({
      accessToken: 'x',
      expiresAtUtc: new Date('2026-01-01T00:00:00.000Z').toISOString(),
    })
    clearSession()
    expect(loadSession()).toBeNull()
  })

  it('decodeJwtBasicClaims reads email and sub', () => {
    const payload = btoa(JSON.stringify({ email: 'a@b.com', sub: 'user-1' }))
    const token = `h.${payload}.s`
    expect(decodeJwtBasicClaims(token)).toEqual({ email: 'a@b.com', sub: 'user-1' })
  })
})
