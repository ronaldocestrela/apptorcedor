import { beforeEach, describe, expect, it, vi } from 'vitest'
import { plansService } from './plansService'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('plansService', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
  })

  it('listPublished calls GET /api/plans', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } })
    await plansService.listPublished()
    expect(api.get).toHaveBeenCalledWith('/api/plans')
  })

  it('getById calls GET /api/plans/:id', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        planId: 'p1',
        name: 'Plano',
        price: 10,
        billingCycle: 'Monthly',
        discountPercentage: 0,
        summary: null,
        rulesNotes: null,
        benefits: [],
      },
    })
    const d = await plansService.getById('p1')
    expect(d.name).toBe('Plano')
    expect(api.get).toHaveBeenCalledWith('/api/plans/p1')
  })
})
