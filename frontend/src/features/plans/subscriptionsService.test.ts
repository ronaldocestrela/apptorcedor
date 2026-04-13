import { beforeEach, describe, expect, it, vi } from 'vitest'
import { subscriptionsService } from './subscriptionsService'

vi.mock('../../shared/api/http', () => ({
  api: {
    post: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('subscriptionsService', () => {
  beforeEach(() => {
    vi.mocked(api.post).mockReset()
  })

  it('subscribe posts /api/subscriptions with planId and paymentMethod', async () => {
    vi.mocked(api.post).mockResolvedValue({
      data: {
        membershipId: 'm1',
        paymentId: 'p1',
        paymentMethod: 'Pix',
        amount: 10,
        currency: 'BRL',
        membershipStatus: 'PendingPayment',
        pix: { qrCodePayload: 'x', copyPasteKey: null },
        card: null,
      },
    })
    const r = await subscriptionsService.subscribe('plan-1', 'Pix')
    expect(r.paymentId).toBe('p1')
    expect(api.post).toHaveBeenCalledWith('/api/subscriptions', { planId: 'plan-1', paymentMethod: 'Pix' })
  })
})
