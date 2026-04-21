import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError } from 'axios'
import { PlanDetailsPage } from './PlanDetailsPage'

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    getById: vi.fn(),
  },
}))

vi.mock('../features/plans/subscriptionsService', () => ({
  subscriptionsService: {
    subscribe: vi.fn(),
  },
}))

import { plansService } from '../features/plans/plansService'
import { subscriptionsService } from '../features/plans/subscriptionsService'

const defaultPlan = {
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
}

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
    vi.mocked(subscriptionsService.subscribe).mockReset()
    vi.stubGlobal('location', { href: '' } as Location)
  })

  afterEach(() => {
    vi.clearAllMocks()
    vi.unstubAllGlobals()
  })

  it('shows loading then full plan detail with list price and checkout CTA', async () => {
    vi.mocked(plansService.getById).mockResolvedValue(defaultPlan)
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
    expect(within(card as HTMLElement).getByText('B1')).toBeInTheDocument()
    expect(within(card as HTMLElement).getByText('D1')).toBeInTheDocument()
    expect(within(card as HTMLElement).getByText('B2')).toBeInTheDocument()
    expect(within(card as HTMLElement).getByText('D2')).toBeInTheDocument()
    expect(document.querySelector('.plan-detail__price-value')).toHaveTextContent('100,00')
    const cta = screen.getByRole('button', { name: /ASSINAR AGORA/i })
    expect(cta).toBeEnabled()
    expect(screen.getByText(/checkout em uma plataforma externa/i)).toBeInTheDocument()
  })

  it('redirects to card checkout URL when Assinar agora is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(plansService.getById).mockResolvedValue(defaultPlan)
    vi.mocked(subscriptionsService.subscribe).mockResolvedValue({
      membershipId: 'm1',
      paymentId: 'pay1',
      paymentMethod: 'Card',
      amount: 100,
      currency: 'BRL',
      membershipStatus: 'PendingPayment',
      pix: null,
      card: { checkoutUrl: 'https://payments.example/checkout/session' },
    })
    renderAtPlanRoute('p1')
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /ASSINAR AGORA/i }))
    await waitFor(() => {
      expect(subscriptionsService.subscribe).toHaveBeenCalledWith('p1', 'Card')
    })
    expect(window.location.href).toBe('https://payments.example/checkout/session')
  })

  it('shows conflict message on subscribe 409', async () => {
    const user = userEvent.setup()
    vi.mocked(plansService.getById).mockResolvedValue(defaultPlan)
    const err = new AxiosError('Conflict')
    err.response = { status: 409, data: {}, statusText: 'Conflict', headers: {}, config: {} as never }
    vi.mocked(subscriptionsService.subscribe).mockRejectedValue(err)
    renderAtPlanRoute('p1')
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /ASSINAR AGORA/i }))
    await waitFor(() => {
      expect(screen.getByText(/já possui uma assinatura ativa/i)).toBeInTheDocument()
    })
    expect(screen.getByRole('button', { name: /ASSINAR AGORA/i })).toBeEnabled()
  })

  it('shows Aguarde and disables button while subscribe is in flight', async () => {
    const user = userEvent.setup()
    vi.mocked(plansService.getById).mockResolvedValue(defaultPlan)
    let resolveSubscribe!: (v: Awaited<ReturnType<typeof subscriptionsService.subscribe>>) => void
    const subscribePromise = new Promise<Awaited<ReturnType<typeof subscriptionsService.subscribe>>>(resolve => {
      resolveSubscribe = resolve
    })
    vi.mocked(subscriptionsService.subscribe).mockReturnValue(subscribePromise)
    renderAtPlanRoute('p1')
    await waitFor(() => {
      expect(screen.getByText('Plano Gold')).toBeInTheDocument()
    })
    void user.click(screen.getByRole('button', { name: /ASSINAR AGORA/i }))
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Aguarde/i })).toBeDisabled()
    })
    resolveSubscribe({
      membershipId: 'm1',
      paymentId: 'pay1',
      paymentMethod: 'Card',
      amount: 100,
      currency: 'BRL',
      membershipStatus: 'PendingPayment',
      pix: null,
      card: { checkoutUrl: 'https://pay.example/x' },
    })
    await waitFor(() => {
      expect(window.location.href).toBe('https://pay.example/x')
    })
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
