import { beforeEach, describe, expect, it } from 'vitest'
import { authStorage } from './authStorage'

describe('authStorage', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it('persists and clears tokens', () => {
    expect(authStorage.getAccess()).toBeNull()
    authStorage.setTokens('access', 'refresh')
    expect(authStorage.getAccess()).toBe('access')
    expect(authStorage.getRefresh()).toBe('refresh')
    authStorage.clear()
    expect(authStorage.getAccess()).toBeNull()
    expect(authStorage.getRefresh()).toBeNull()
  })
})
