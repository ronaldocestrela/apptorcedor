import { beforeEach, describe, expect, it, vi } from 'vitest'
import { uploadBenefitOfferBanner } from './adminApi'

vi.mock('../../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('adminApi benefits banner upload', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('uploadBenefitOfferBanner posts multipart without forcing Content-Type', async () => {
    const { api } = await import('../../../shared/api/http')
    vi.mocked(api.post).mockResolvedValue({ data: { bannerUrl: '/uploads/benefit-offer-banners/x.png' } })

    const offerId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
    const file = new File([new Uint8Array([1, 2])], 'b.png', { type: 'image/png' })
    const result = await uploadBenefitOfferBanner(offerId, file)

    expect(result.bannerUrl).toBe('/uploads/benefit-offer-banners/x.png')
    expect(api.post).toHaveBeenCalledWith(
      `/api/admin/benefits/offers/${encodeURIComponent(offerId)}/banner`,
      expect.any(FormData),
      expect.objectContaining({
        transformRequest: expect.any(Function) as unknown as (body: unknown, headers: Record<string, string>) => unknown,
      }),
    )

    const [, formData, config] = vi.mocked(api.post).mock.calls[0] as [string, FormData, { transformRequest: (b: unknown, h: Record<string, string>) => unknown }]
    expect(formData.get('file')).toBe(file)
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    config.transformRequest(formData, headers)
    expect(headers['Content-Type']).toBeUndefined()
  })
})
