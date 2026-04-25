import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listTorcedorGames } from './torcedorGamesApi'
import { getMyTicket, listMyTickets, redeemMyTicket, requestTicket } from './torcedorTicketsApi'

vi.mock('../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('torcedor C.4 games & tickets APIs', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.post).mockReset()
  })

  it('listTorcedorGames calls GET /api/games', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { totalCount: 0, items: [] } })
    await listTorcedorGames({ search: 'x', page: 2, pageSize: 10 })
    expect(api.get).toHaveBeenCalledWith('/api/games', {
      params: { search: 'x', page: 2, pageSize: 10 },
    })
  })

  it('listMyTickets calls GET /api/tickets', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { totalCount: 0, items: [] } })
    await listMyTickets({ gameId: 'g1', status: 'Purchased', page: 1, pageSize: 20 })
    expect(api.get).toHaveBeenCalledWith('/api/tickets', {
      params: { gameId: 'g1', status: 'Purchased', page: 1, pageSize: 20 },
    })
  })

  it('getMyTicket calls GET /api/tickets/:id', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        ticketId: 't1',
        gameId: 'g1',
        opponent: 'A',
        competition: 'B',
        gameDate: '2026-01-01T00:00:00Z',
        status: 'Purchased',
        externalTicketId: null,
        qrCode: 'qr',
        createdAt: '2026-01-01T00:00:00Z',
        updatedAt: '2026-01-01T00:00:00Z',
        redeemedAt: null,
      },
    })
    const d = await getMyTicket('t1')
    expect(d.status).toBe('Purchased')
    expect(api.get).toHaveBeenCalledWith('/api/tickets/t1')
  })

  it('redeemMyTicket calls POST /api/tickets/:id/redeem', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: null })
    await redeemMyTicket('t1')
    expect(api.post).toHaveBeenCalledWith('/api/tickets/t1/redeem')
  })

  it('requestTicket calls POST /api/tickets/request', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: { ticketId: 'tid' } })
    const r = await requestTicket('g1')
    expect(r.ticketId).toBe('tid')
    expect(api.post).toHaveBeenCalledWith('/api/tickets/request', { gameId: 'g1' })
  })
})
