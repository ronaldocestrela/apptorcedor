import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/http'
import { getPublicBranding } from './brandingApi'

vi.mock('../api/http', () => ({
  api: { get: vi.fn() },
}))

describe('getPublicBranding', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
  })

  it('returns payload from GET /api/branding', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { teamShieldUrl: '/uploads/team-shield/x.png' } })
    await expect(getPublicBranding()).resolves.toEqual({ teamShieldUrl: '/uploads/team-shield/x.png' })
    expect(api.get).toHaveBeenCalledWith('/api/branding')
  })

  it('returns null url when API omits shield', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { teamShieldUrl: null } })
    await expect(getPublicBranding()).resolves.toEqual({ teamShieldUrl: null })
  })
})
