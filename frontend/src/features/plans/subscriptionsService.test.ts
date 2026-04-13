import { beforeEach, describe, expect, it, vi } from 'vitest'
import { subscriptionsService } from './subscriptionsService'

vi.mock('../../shared/api/http', () => ({
  api: {
    post: vi.fn(),
    get: vi.fn(),
    put: vi.fn(),
  },
}))

import { api } from '../../shared/api/http'

describe('subscriptionsService', () => {
  beforeEach(() => {
    vi.mocked(api.post).mockReset()
    vi.mocked(api.get).mockReset()
    vi.mocked(api.put).mockReset()
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

  it('changePlan puts /api/account/subscription/plan', async () => {
    vi.mocked(api.put).mockResolvedValue({
      data: {
        membershipId: 'm1',
        membershipStatus: 'Ativo',
        fromPlan: { planId: 'a', name: 'A', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
        toPlan: { planId: 'b', name: 'B', price: 100, billingCycle: 'Monthly', discountPercentage: 0 },
        prorationAmount: 12.5,
        paymentId: 'pay2',
        currency: 'BRL',
        paymentMethod: 'Pix',
        pix: { qrCodePayload: 'x', copyPasteKey: null },
        card: null,
      },
    })
    const r = await subscriptionsService.changePlan('plan-b', 'Card')
    expect(r.paymentId).toBe('pay2')
    expect(api.put).toHaveBeenCalledWith('/api/account/subscription/plan', { planId: 'plan-b', paymentMethod: 'Card' })
  })

  it('getMySummary gets /api/account/subscription', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        hasMembership: true,
        membershipId: 'm1',
        membershipStatus: 'Ativo',
        startDate: '2025-01-01T00:00:00Z',
        endDate: null,
        nextDueDate: '2025-02-01T00:00:00Z',
        plan: {
          planId: 'p1',
          name: 'Gold',
          price: 99,
          billingCycle: 'Monthly',
          discountPercentage: 0,
        },
        lastPayment: {
          paymentId: 'pay1',
          amount: 99,
          currency: 'BRL',
          status: 'Paid',
          paymentMethod: 'Pix',
          paidAt: '2025-01-01T12:00:00Z',
          dueDate: '2025-01-02T00:00:00Z',
        },
        digitalCard: {
          state: 'AwaitingIssuance',
          membershipStatusLabel: 'Ativo',
          message: null,
        },
      },
    })
    const r = await subscriptionsService.getMySummary()
    expect(r.membershipStatus).toBe('Ativo')
    expect(api.get).toHaveBeenCalledWith('/api/account/subscription')
  })
})
