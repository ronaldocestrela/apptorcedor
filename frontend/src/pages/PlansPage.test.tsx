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

  it('renders catalog with header, featured badge and MAIS DETALHES links', async () => {
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
      expect(screen.getByRole('heading', { name: /^Planos$/i })).toBeInTheDocument()
    })

    expect(container.querySelector('.plans-root')).toBeInTheDocument()
    expect(container.querySelector('.plans-page')).toBeInTheDocument()
    expect(container.querySelector('.plans-page__list')).toBeInTheDocument()
    expect(container.querySelectorAll('.plans-page__card')).toHaveLength(1)
    expect(screen.getByText('Mais Popular')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Mais detalhes/i })).toHaveAttribute('href', '/plans/p1')
    expect(screen.getByText('99,00')).toBeInTheDocument()
    expect(screen.getByText('/ mês')).toBeInTheDocument()
  })

  it('shows billing period suffix from plan billingCycle', async () => {
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        {
          planId: 'p1',
          name: 'Anual',
          price: 1200,
          billingCycle: 'Yearly',
          discountPercentage: 0,
          summary: null,
          benefits: [],
        },
      ],
    })

    render(
      <MemoryRouter>
        <PlansPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByText('/ ano')).toBeInTheDocument()
    })
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
