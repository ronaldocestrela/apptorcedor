import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listAdminOpponentLogos, uploadAdminOpponentLogo } from './adminApi'

vi.mock('../../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

describe('adminApi games opponent logos', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('listAdminOpponentLogos calls /api/admin/games/opponent-logos', async () => {
    const { api } = await import('../../../shared/api/http')
    vi.mocked(api.get).mockResolvedValue({
      data: { totalCount: 0, items: [] },
    })

    await listAdminOpponentLogos({ page: 2, pageSize: 30 })

    expect(api.get).toHaveBeenCalledWith('/api/admin/games/opponent-logos', {
      params: { page: 2, pageSize: 30 },
    })
  })

  it('uploadAdminOpponentLogo posts multipart without forcing Content-Type', async () => {
    const { api } = await import('../../../shared/api/http')
    vi.mocked(api.post).mockResolvedValue({ data: { url: '/uploads/opponent-logos/x.png' } })

    const file = new File([new Uint8Array([1, 2])], 'a.png', { type: 'image/png' })
    const url = await uploadAdminOpponentLogo(file)

    expect(url).toBe('/uploads/opponent-logos/x.png')
    expect(api.post).toHaveBeenCalledWith(
      '/api/admin/games/opponent-logos',
      expect.any(FormData),
      expect.objectContaining({
        transformRequest: expect.any(Function) as unknown as (body: unknown, headers: Record<string, string>) => unknown,
      }),
    )
  })
})
