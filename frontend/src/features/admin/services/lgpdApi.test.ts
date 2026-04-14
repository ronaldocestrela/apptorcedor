import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listLegalDocuments } from './lgpdApi'

vi.mock('../../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

import { api } from '../../../shared/api/http'

describe('lgpdApi', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.post).mockReset()
  })

  it('listLegalDocuments calls GET /api/admin/lgpd/documents', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: [
        {
          id: 'a',
          type: 'TermsOfUse',
          title: 'Termos',
          createdAt: '2026-01-01T00:00:00Z',
          publishedVersionNumber: 1,
          publishedVersionId: 'v1',
        },
      ],
    })
    const rows = await listLegalDocuments()
    expect(rows).toHaveLength(1)
    expect(rows[0].title).toBe('Termos')
    expect(api.get).toHaveBeenCalledWith('/api/admin/lgpd/documents')
  })
})
