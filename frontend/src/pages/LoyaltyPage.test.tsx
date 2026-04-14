import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LoyaltyPage } from './LoyaltyPage'

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'u1',
      email: 'a@b.c',
      name: 'Test',
      roles: [],
      permissions: [],
      requiresProfileCompletion: false,
    },
  }),
}))

vi.mock('../features/torcedor/torcedorLoyaltyApi', () => ({
  getMyLoyaltySummary: vi.fn(),
  getMonthlyLoyaltyRanking: vi.fn(),
  getAllTimeLoyaltyRanking: vi.fn(),
}))

import {
  getAllTimeLoyaltyRanking,
  getMonthlyLoyaltyRanking,
  getMyLoyaltySummary,
} from '../features/torcedor/torcedorLoyaltyApi'

describe('LoyaltyPage', () => {
  beforeEach(() => {
    vi.mocked(getMyLoyaltySummary).mockReset()
    vi.mocked(getMonthlyLoyaltyRanking).mockReset()
    vi.mocked(getAllTimeLoyaltyRanking).mockReset()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('shows summary and rankings', async () => {
    vi.mocked(getMyLoyaltySummary).mockResolvedValue({
      totalPoints: 42,
      monthlyPoints: 10,
      monthlyRank: 1,
      allTimeRank: 2,
      asOfUtc: '2026-04-10T12:00:00Z',
    })
    vi.mocked(getMonthlyLoyaltyRanking).mockResolvedValue({
      totalCount: 1,
      items: [{ rank: 1, userId: 'u1', userName: 'Test', totalPoints: 10 }],
      me: { rank: 1, userId: 'u1', userName: 'Test', totalPoints: 10 },
    })
    vi.mocked(getAllTimeLoyaltyRanking).mockResolvedValue({
      totalCount: 1,
      items: [{ rank: 1, userId: 'u1', userName: 'Test', totalPoints: 42 }],
      me: { rank: 1, userId: 'u1', userName: 'Test', totalPoints: 42 },
    })

    render(
      <MemoryRouter>
        <LoyaltyPage />
      </MemoryRouter>,
    )

    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByRole('main').textContent).toContain('Saldo total:')
      expect(screen.getByRole('main').textContent).toContain('42')
    })
    await waitFor(() => {
      expect(screen.getAllByText(/\(você\)/i).length).toBeGreaterThanOrEqual(2)
    })
  })

  it('shows error when load fails', async () => {
    vi.mocked(getMyLoyaltySummary).mockRejectedValue(new Error('falhou'))
    render(
      <MemoryRouter>
        <LoyaltyPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('falhou')
    })
  })
})
