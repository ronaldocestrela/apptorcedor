import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listEligibleBenefitOffers } from './torcedorBenefitsApi'
import { getTorcedorNewsDetail, listTorcedorNewsFeed } from './torcedorNewsApi'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('torcedor C.2 APIs', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
  })

  it('listTorcedorNewsFeed calls GET /api/news with params', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { totalCount: 0, items: [] },
    })
    await listTorcedorNewsFeed({ search: 'x', page: 2, pageSize: 10 })
    expect(api.get).toHaveBeenCalledWith('/api/news', {
      params: { search: 'x', page: 2, pageSize: 10 },
    })
  })

  it('getTorcedorNewsDetail calls GET /api/news/:id', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        newsId: 'n1',
        title: 'T',
        summary: null,
        content: 'C',
        publishedAt: '2026-01-01T00:00:00Z',
        updatedAt: '2026-01-01T00:00:00Z',
      },
    })
    const d = await getTorcedorNewsDetail('n1')
    expect(d.title).toBe('T')
    expect(api.get).toHaveBeenCalledWith('/api/news/n1')
  })

  it('listEligibleBenefitOffers calls GET /api/benefits/eligible', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { totalCount: 0, items: [] },
    })
    await listEligibleBenefitOffers({ page: 1, pageSize: 20 })
    expect(api.get).toHaveBeenCalledWith('/api/benefits/eligible', {
      params: { page: 1, pageSize: 20 },
    })
  })
})
