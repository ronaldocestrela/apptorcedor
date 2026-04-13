import { render, screen, waitFor } from '@testing-library/react'
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

function renderAtPlanRoute(planId: string) {
  return render(
    <MemoryRouter initialEntries={[`/plans/${planId}`]}>
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

  it('shows loading then full plan detail with discounted price', async () => {
    vi.mocked(plansService.getById).mockResolvedValue({
      planId: 'p1',
      name: 'Plano Gold',
      price: 100,
      billingCycle: 'Monthly',
      discountPercentage: 10,
      summary: 'Resumo',
      rulesNotes: 'Regra 1',
      benefits: [
        { benefitId: 'b1', sortOrder: 0, title: 'B1', description: 'D1' },
      ],
    })
    renderAtPlanRoute('p1')
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    expect(screen.getByText(/Resumo/)).toBeInTheDocument()
    expect(screen.getByText(/Regra 1/)).toBeInTheDocument()
    expect(screen.getByText(/B1/)).toBeInTheDocument()
    expect(screen.getByText(/R\$\s*90,00/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Contratar/i })).toBeDisabled()
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
