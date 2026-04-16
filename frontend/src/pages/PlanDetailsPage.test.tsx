import { render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError } from 'axios'
import { PlanDetailsPage } from './PlanDetailsPage'

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    getById: vi.fn(),
  },
}))

import { plansService } from '../features/plans/plansService'

function renderAtPlanRoute(planId: string, state?: { featured?: boolean }) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: `/plans/${planId}`, state }]}>
      <Routes>
        <Route path="plans/:planId" element={<PlanDetailsPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('PlanDetailsPage', () => {
  beforeEach(() => {
    vi.mocked(plansService.getById).mockReset()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading then full plan detail with list price and checkout CTA', async () => {
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Plano Gold',
      price: 100,
      billingCycle: 'Monthly',
      discountPercentage: 10,
      summary: 'Resumo',
      rulesNotes: 'Regra 1',
      benefits: [
        { benefitId: 'b2', sortOrder: 1, title: 'B2', description: 'D2' },
        { benefitId: 'b1', sortOrder: 0, title: 'B1', description: 'D1' },
      ],
    })
    renderAtPlanRoute('p1')
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    expect(screen.getByRole('heading', { name: 'Planos' })).toBeInTheDocument()
    expect(document.querySelector('.plans-root')).toBeTruthy()
    expect(document.querySelector('.plan-detail__card')).toBeTruthy()
    expect(screen.getByText(/Resumo/)).toBeInTheDocument()
    expect(screen.queryByText(/Regra 1/)).not.toBeInTheDocument()
    const card = document.querySelector('.plan-detail__card')!
    expect(within(card as HTMLElement).getByText(/B1 — D1/)).toBeInTheDocument()
    expect(within(card as HTMLElement).getByText(/B2 — D2/)).toBeInTheDocument()
    expect(document.querySelector('.plans-page__price-value')).toHaveTextContent('100,00')
    const cta = screen.getByRole('link', { name: /Assinar agora/i })
    expect(cta).toHaveAttribute('href', '/plans/p1/checkout')
    expect(screen.getByText(/checkout em uma plataforma externa/i)).toBeInTheDocument()
  })

  it('shows Mais Popular badge when navigation state featured is true', async () => {
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Plano Gold',
      price: 50,
      billingCycle: 'Monthly',
      discountPercentage: 0,
      summary: '',
      rulesNotes: '',
      benefits: [],
    })
    renderAtPlanRoute('p1', { featured: true })
    await waitFor(() => {
      expect(screen.getByText('Mais Popular')).toBeInTheDocument()
    })
  })

  it('does not show Mais Popular badge without featured state', async () => {
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Plano Gold',
      price: 50,
      billingCycle: 'Monthly',
      discountPercentage: 0,
      summary: '',
      rulesNotes: '',
      benefits: [],
    })
    renderAtPlanRoute('p1')
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    expect(screen.queryByText('Mais Popular')).not.toBeInTheDocument()
  })

  it('shows not found message on 404', async () => {
    const err = new AxiosError('Not Found')
    err.response = { status: 404, data: {}, statusText: 'Not Found', headers: {}, config: {} as never }
    vi.mocked(plansService.getById).mockRejectedValue(err)
    renderAtPlanRoute('missing')
    await waitFor(() => {
      expect(screen.getByText(/não encontrado/i)).toBeInTheDocument()
    })
  })

  it('shows generic error when request fails', async () => {
    vi.mocked(plansService.getById).mockRejectedValue(new Error('falhou'))
    renderAtPlanRoute('p1')
    await waitFor(() => {
      expect(screen.getByText(/falhou/)).toBeInTheDocument()
    })
  })
})
