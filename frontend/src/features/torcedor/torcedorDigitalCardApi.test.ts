import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  clearDigitalCardCacheForTests,
  DIGITAL_CARD_LOCAL_STORAGE_KEY,
  getMyDigitalCard,
  getMyDigitalCardWithSource,
} from './torcedorDigitalCardApi'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

const sampleActive: Awaited<ReturnType<typeof getMyDigitalCard>> = {
  state: 'Active',
  membershipStatus: 'Ativo',
  message: null,
  membershipId: 'm1',
  digitalCardId: 'c1',
  version: 1,
  cardStatus: 'Active',
  issuedAt: '2026-01-01T00:00:00Z',
  verificationToken: 'TOKENTOKENTOK',
  templatePreviewLines: ['Linha 1'],
  cacheValidUntilUtc: new Date(Date.now() + 60_000).toISOString(),
}

describe('torcedorDigitalCardApi C.3', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    clearDigitalCardCacheForTests()
  })

  afterEach(() => {
    clearDigitalCardCacheForTests()
  })

  it('getMyDigitalCard calls GET /api/account/digital-card', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: sampleActive })
    const r = await getMyDigitalCard()
    expect(r.state).toBe('Active')
    expect(api.get).toHaveBeenCalledWith('/api/account/digital-card')
    expect(localStorage.getItem(DIGITAL_CARD_LOCAL_STORAGE_KEY)).toBeTruthy()
  })

  it('getMyDigitalCardWithSource uses local cache when network fails and allowStaleOnNetworkError', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: sampleActive })
    await getMyDigitalCard()
    vi.mocked(api.get).mockRejectedValueOnce(new Error('network'))
    const r = await getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })
    expect(r.fromCache).toBe(true)
    expect(r.data.state).toBe('Active')
  })

  it('getMyDigitalCardWithSource propagates error when cache expired', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: {
        ...sampleActive,
        cacheValidUntilUtc: new Date(Date.now() - 1000).toISOString(),
      },
    })
    await getMyDigitalCard()
    vi.mocked(api.get).mockRejectedValueOnce(new Error('network'))
    await expect(getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })).rejects.toThrow('network')
  })
})
