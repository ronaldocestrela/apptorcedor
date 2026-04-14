import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAllTimeLoyaltyRanking,
  getMonthlyLoyaltyRanking,
  getMyLoyaltySummary,
} from './torcedorLoyaltyApi'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('torcedorLoyaltyApi C.5', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('getMyLoyaltySummary calls GET /api/loyalty/me/summary', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        totalPoints: 10,
        monthlyPoints: 3,
        monthlyRank: 2,
        allTimeRank: 5,
        asOfUtc: '2026-04-01T12:00:00Z',
      },
    })
    const r = await getMyLoyaltySummary()
    expect(r.totalPoints).toBe(10)
    expect(api.get).toHaveBeenCalledWith('/api/loyalty/me/summary')
  })

  it('getMonthlyLoyaltyRanking passes year and month', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { totalCount: 0, items: [], me: null } })
    await getMonthlyLoyaltyRanking({ year: 2026, month: 4, page: 1, pageSize: 10 })
    expect(api.get).toHaveBeenCalledWith('/api/loyalty/rankings/monthly', {
      params: { year: 2026, month: 4, page: 1, pageSize: 10 },
    })
  })

  it('getAllTimeLoyaltyRanking calls rankings/all-time', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { totalCount: 1, items: [], me: null } })
    await getAllTimeLoyaltyRanking({ page: 2, pageSize: 15 })
    expect(api.get).toHaveBeenCalledWith('/api/loyalty/rankings/all-time', {
      params: { page: 2, pageSize: 15 },
    })
  })
})
