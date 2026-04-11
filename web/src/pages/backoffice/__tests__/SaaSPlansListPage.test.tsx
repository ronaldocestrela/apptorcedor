import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { SaaSPlansListPage } from '../SaaSPlansListPage'

const listMock = vi.fn()

vi.mock('../../../shared/backoffice', async () => {
  const actual = await vi.importActual<typeof import('../../../shared/backoffice')>('../../../shared/backoffice')
  return {
    ...actual,
    listSaaSPlans: (...a: unknown[]) => listMock(...a),
    getSaaSPlanById: vi.fn(),
    createSaaSPlan: vi.fn(),
    updateSaaSPlan: vi.fn(),
    toggleSaaSPlan: vi.fn(),
  }
})

describe('SaaSPlansListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listMock.mockResolvedValue({
      items: [
        {
          id: 'p1',
          name: 'Starter',
          description: 'Basic',
          monthlyPrice: 99,
          yearlyPrice: null,
          maxMembers: 500,
          stripePriceMonthlyId: null,
          stripePriceYearlyId: null,
          isActive: true,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    })
  })

  it('lists plans', async () => {
    render(
      <MemoryRouter>
        <SaaSPlansListPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText('Starter')).toBeInTheDocument()
    })
  })

  it('opens create modal', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <SaaSPlansListPage />
      </MemoryRouter>,
    )
    await waitFor(() => screen.getByText('Starter'))
    await user.click(screen.getByRole('button', { name: /novo plano/i }))
    expect(screen.getByRole('dialog', { name: /novo plano saas/i })).toBeInTheDocument()
  })
})
