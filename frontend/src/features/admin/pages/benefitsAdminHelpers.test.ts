import { describe, expect, it } from 'vitest'
import { deriveOfferUiStatus } from './benefitsAdminHelpers'

describe('deriveOfferUiStatus', () => {

  it('returns Inativa when not active', () => {
    const now = Date.parse('2025-06-15T12:00:00.000Z')
    expect(
      deriveOfferUiStatus(
        {
          isActive: false,
          startAt: '2025-01-01T00:00:00.000Z',
          endAt: '2025-12-31T23:59:59.000Z',
        },
        now,
      ),
    ).toBe('Inativa')
  })

  it('returns Expirada when now > endAt', () => {
    const now = Date.parse('2025-06-15T12:00:00.000Z')
    expect(
      deriveOfferUiStatus(
        {
          isActive: true,
          startAt: '2025-01-01T00:00:00.000Z',
          endAt: '2025-05-01T00:00:00.000Z',
        },
        now,
      ),
    ).toBe('Expirada')
  })

  it('returns Programada when now < startAt', () => {
    const now = Date.parse('2025-06-15T12:00:00.000Z')
    expect(
      deriveOfferUiStatus(
        {
          isActive: true,
          startAt: '2025-07-01T00:00:00.000Z',
          endAt: '2025-12-31T00:00:00.000Z',
        },
        now,
      ),
    ).toBe('Programada')
  })

  it('returns Vigente when active and within window', () => {
    const now = Date.parse('2025-06-15T12:00:00.000Z')
    expect(
      deriveOfferUiStatus(
        {
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2025-06-30T00:00:00.000Z',
        },
        now,
      ),
    ).toBe('Vigente')
  })
})
