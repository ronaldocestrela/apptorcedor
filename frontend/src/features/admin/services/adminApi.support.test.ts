import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listAdminSupportTickets } from './adminApi'

vi.mock('../../../shared/api/http', () => ({
  api: {
    get: vi.fn(),
  },
}))

describe('adminApi support (B.11)', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('listAdminSupportTickets calls correct endpoint and maps params', async () => {
    const { api } = await import('../../../shared/api/http')
    vi.mocked(api.get).mockResolvedValue({
      data: { totalCount: 0, items: [] },
    })

    await listAdminSupportTickets({
      queue: 'Geral',
      status: 'Open',
      unassignedOnly: true,
      page: 2,
      pageSize: 10,
    })

    expect(api.get).toHaveBeenCalledWith('/api/admin/support/tickets', {
      params: {
        queue: 'Geral',
        status: 'Open',
        assignedUserId: undefined,
        unassignedOnly: true,
        slaBreachedOnly: undefined,
        page: 2,
        pageSize: 10,
      },
    })
  })
})
