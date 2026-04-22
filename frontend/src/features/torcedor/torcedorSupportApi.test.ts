import { describe, expect, it, vi } from 'vitest'
import {
  cancelMySupportTicket,
  fetchMySupportAttachmentBlob,
  getMySupportTicket,
  listMySupportTickets,
  reopenMySupportTicket,
} from './torcedorSupportApi'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('torcedorSupportApi', () => {
  it('listMySupportTickets calls GET /api/support/tickets with params', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { totalCount: 0, items: [] } })
    await listMySupportTickets({ status: 'Open', page: 2, pageSize: 10 })
    expect(api.get).toHaveBeenCalledWith('/api/support/tickets', {
      params: { status: 'Open', page: 2, pageSize: 10 },
    })
  })

  it('getMySupportTicket encodes id', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: {} })
    await getMySupportTicket('abc-123')
    expect(api.get).toHaveBeenCalledWith('/api/support/tickets/abc-123')
  })

  it('cancelMySupportTicket posts cancel', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: {} })
    await cancelMySupportTicket('tid')
    expect(api.post).toHaveBeenCalledWith('/api/support/tickets/tid/cancel')
  })

  it('reopenMySupportTicket posts reopen', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: {} })
    await reopenMySupportTicket('tid')
    expect(api.post).toHaveBeenCalledWith('/api/support/tickets/tid/reopen')
  })

  it('fetchMySupportAttachmentBlob GETs path with blob response', async () => {
    const blob = new Blob(['x'], { type: 'image/png' })
    vi.mocked(api.get).mockResolvedValue({ data: blob })
    const out = await fetchMySupportAttachmentBlob('/api/support/tickets/t/attachments/a1')
    expect(api.get).toHaveBeenCalledWith('/api/support/tickets/t/attachments/a1', { responseType: 'blob' })
    expect(out).toBe(blob)
  })
})
