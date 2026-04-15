import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { PlansPage } from './PlansPage'

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    listPublished: vi.fn(),
  },
}))

import { plansService } from '../features/plans/plansService'

describe('PlansPage', () => {
  beforeEach(() => {
    vi.mocked(plansService.listPublished).mockReset()
  })

  it('renders modern catalog grid with cards and actions', async () => {
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        {
          planId: 'p1',
          name: 'Plano Ouro',
          price: 99,
          billingCycle: 'Monthly',
          discountPercentage: 10,
          summary: 'Acesso completo',
          benefits: [{ benefitId: 'b1', title: 'Fila exclusiva', description: null }],
        },
      ],
    })

    const { container } = render(
      <MemoryRouter>
        <PlansPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Planos de sócio/i })).toBeInTheDocument()
    })

    expect(container.querySelector('.plans-page')).toBeInTheDocument()
    expect(container.querySelector('.plans-page__grid')).toBeInTheDocument()
    expect(container.querySelectorAll('.plans-page__card')).toHaveLength(1)
    expect(screen.getByRole('link', { name: /Assinar/i })).toBeInTheDocument()
  })

  it('shows empty state when no plans are published', async () => {
    vi.mocked(plansService.listPublished).mockResolvedValue({ items: [] })

    render(
      <MemoryRouter>
        <PlansPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByText(/Nenhum plano publicado no momento/i)).toBeInTheDocument()
    })
  })
})
