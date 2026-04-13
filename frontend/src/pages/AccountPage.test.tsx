import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AccountPage } from './AccountPage'

vi.mock('../features/account/accountApi', () => ({
  getMyProfile: vi.fn(),
  resolvePublicAssetUrl: (u: string) => u,
  upsertMyProfile: vi.fn(),
  uploadProfilePhoto: vi.fn(),
}))

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    listPublished: vi.fn(),
  },
}))

vi.mock('../features/plans/subscriptionsService', () => ({
  subscriptionsService: {
    getMySummary: vi.fn(),
    changePlan: vi.fn(),
  },
}))

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => ({
    user: { name: 'T', email: 't@test.local', requiresProfileCompletion: false },
    refreshProfile: vi.fn(),
  }),
}))

import { getMyProfile } from '../features/account/accountApi'
import { plansService } from '../features/plans/plansService'
import { subscriptionsService } from '../features/plans/subscriptionsService'

describe('AccountPage', () => {
  beforeEach(() => {
    vi.mocked(getMyProfile).mockReset()
    vi.mocked(getMyProfile).mockResolvedValue({
      document: null,
      birthDate: null,
      photoUrl: null,
      address: null,
    })
    vi.mocked(subscriptionsService.getMySummary).mockReset()
    vi.mocked(subscriptionsService.changePlan).mockReset()
    vi.mocked(plansService.listPublished).mockReset()
  })

  it('shows subscription status when user has membership', async () => {
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      startDate: '2025-01-01T00:00:00Z',
      endDate: null,
      nextDueDate: '2025-02-01T12:00:00Z',
      plan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      lastPayment: null,
      digitalCard: null,
    })
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
      ],
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/Assinatura/i)).toBeInTheDocument()
    })
    expect(screen.getByText(/Ativo/)).toBeInTheDocument()
    expect(screen.getByText(/Gold/)).toBeInTheDocument()
  })

  it('plan change section calls changePlan when confirmed', async () => {
    const user = userEvent.setup()
    const summary = {
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      startDate: '2025-01-01T00:00:00Z',
      endDate: null,
      nextDueDate: '2025-02-01T12:00:00Z',
      plan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      lastPayment: null,
      digitalCard: null,
    }
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue(summary)
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
        { planId: 'p2', name: 'Silver', price: 30, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
      ],
    })
    vi.mocked(subscriptionsService.changePlan).mockResolvedValue({
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      fromPlan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      toPlan: { planId: 'p2', name: 'Silver', price: 30, billingCycle: 'Monthly', discountPercentage: 0 },
      prorationAmount: 0,
      paymentId: null,
      currency: 'BRL',
      paymentMethod: null,
      pix: null,
      card: null,
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/Trocar plano/i)).toBeInTheDocument()
    })
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })
    await user.selectOptions(screen.getByRole('combobox'), 'p2')
    await user.click(screen.getByRole('button', { name: /Confirmar troca/i }))
    await waitFor(() => {
      expect(subscriptionsService.changePlan).toHaveBeenCalledWith('p2', 'Pix')
    })
  })

  it('shows message when user has no membership', async () => {
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: false,
      membershipId: null,
      membershipStatus: null,
      startDate: null,
      endDate: null,
      nextDueDate: null,
      plan: null,
      lastPayment: null,
      digitalCard: null,
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/ainda não possui assinatura/i)).toBeInTheDocument()
    })
  })
})
