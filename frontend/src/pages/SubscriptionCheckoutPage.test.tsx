import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError } from 'axios'
import { SubscriptionCheckoutPage } from './SubscriptionCheckoutPage'
import { SubscriptionConfirmationPage } from './SubscriptionConfirmationPage'

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    getById: vi.fn(),
  },
}))

vi.mock('../features/plans/subscriptionsService', () => ({
  subscriptionsService: {
    subscribe: vi.fn(),
    getMySummary: vi.fn(),
  },
}))

import { plansService } from '../features/plans/plansService'
import { subscriptionsService } from '../features/plans/subscriptionsService'

function renderAtCheckout(planId: string) {
  return render(
    <MemoryRouter initialEntries={[`/plans/${planId}/checkout`]}>
      <Routes>
        <Route path="plans/:planId/checkout" element={<SubscriptionCheckoutPage />} />
        <Route path="subscription/confirmation" element={<SubscriptionConfirmationPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('SubscriptionCheckoutPage', () => {
  beforeEach(() => {
    vi.mocked(plansService.getById).mockReset()
    vi.mocked(subscriptionsService.subscribe).mockReset()
    vi.mocked(subscriptionsService.getMySummary).mockReset()
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'PendingPayment',
      startDate: null,
      endDate: null,
      nextDueDate: null,
      plan: null,
      lastPayment: null,
      digitalCard: {
        state: 'MembershipInactive',
        membershipStatusLabel: 'PendingPayment',
        message: 'Aguardando pagamento',
      },
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('loads plan and submits Pix subscription', async () => {
    const user = userEvent.setup()
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Gold',
      price: 100,
      billingCycle: 'Monthly',
      discountPercentage: 0,
      summary: null,
      rulesNotes: null,
      benefits: [],
    })
    vi.mocked(subscriptionsService.subscribe).mockResolvedValue({
      membershipId: 'm1',
      paymentId: 'pay1',
      paymentMethod: 'Pix',
      amount: 100,
      currency: 'BRL',
      membershipStatus: 'PendingPayment',
      pix: { qrCodePayload: 'MOCK_PIX|x', copyPasteKey: null },
      card: null,
    })
    renderAtCheckout('p1')
    await waitFor(() => {
      expect(screen.getByText('Gold')).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /Confirmar contratação/i }))
    await waitFor(() => {
      expect(subscriptionsService.subscribe).toHaveBeenCalledWith('p1', 'Pix')
    })
    expect(await screen.findByText(/Contratação registrada/i)).toBeInTheDocument()
    expect(await screen.findByText(/MOCK_PIX\|x/)).toBeInTheDocument()
  })

  it('shows error on 409', async () => {
    const user = userEvent.setup()
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Gold',
      price: 10,
      billingCycle: 'Monthly',
      discountPercentage: 0,
      summary: null,
      rulesNotes: null,
      benefits: [],
    })
    const err = new AxiosError('Conflict')
    err.response = { status: 409, data: {}, statusText: 'Conflict', headers: {}, config: {} as never }
    vi.mocked(subscriptionsService.subscribe).mockRejectedValue(err)
    renderAtCheckout('p1')
    await waitFor(() => {
      expect(screen.getByText('Gold')).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /Confirmar contratação/i }))
    await waitFor(() => {
      expect(screen.getByText(/pagamento pendente/i)).toBeInTheDocument()
    })
  })
})
