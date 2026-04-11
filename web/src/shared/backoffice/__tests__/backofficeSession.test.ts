import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { clearBackofficeSession, getBackofficeApiKey, setBackofficeApiKey } from '../backofficeSession'

describe('backofficeSession', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  afterEach(() => {
    sessionStorage.clear()
  })

  it('returns null when empty', () => {
    expect(getBackofficeApiKey()).toBeNull()
  })

  it('stores and reads trimmed key', () => {
    setBackofficeApiKey('  secret ')
    expect(getBackofficeApiKey()).toBe('secret')
  })

  it('clearBackofficeSession removes key', () => {
    setBackofficeApiKey('k')
    clearBackofficeSession()
    expect(getBackofficeApiKey()).toBeNull()
  })
})
