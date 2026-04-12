import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminStripeConnectPage } from '../AdminStripeConnectPage'

const getStatusMock = vi.fn()
const startOnboardingMock = vi.fn()
const syncStatusMock = vi.fn()

vi.mock('../../../shared/payments/stripeConnectTenantApi', () => ({
  getTenantStripeConnectStatus: (...a: unknown[]) => getStatusMock(...a),
  startTenantStripeConnectOnboarding: (...a: unknown[]) => startOnboardingMock(...a),
  syncTenantStripeConnectStatus: (...a: unknown[]) => syncStatusMock(...a),
}))

describe('AdminStripeConnectPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while fetching status', () => {
    getStatusMock.mockReturnValue(new Promise(() => {}))
    render(<AdminStripeConnectPage />)
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
  })

  it('shows not configured when isConfigured is false', async () => {
    getStatusMock.mockResolvedValue({
      isConfigured: false,
      stripeAccountId: null,
      onboardingStatus: 0,
      chargesEnabled: false,
      payoutsEnabled: false,
      detailsSubmitted: false,
    })
    render(<AdminStripeConnectPage />)
    expect(
      await screen.findByText(/Seu clube ainda não configurou uma conta Stripe/i),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Configurar conta Stripe/i })).toBeInTheDocument()
  })

  it('shows pending when details are not submitted', async () => {
    getStatusMock.mockResolvedValue({
      isConfigured: true,
      stripeAccountId: 'acct_1',
      onboardingStatus: 0,
      chargesEnabled: false,
      payoutsEnabled: false,
      detailsSubmitted: false,
    })
    render(<AdminStripeConnectPage />)
    expect(
      await screen.findByText(/Complete o cadastro na Stripe para habilitar cobranças/i),
    ).toBeInTheDocument()
  })

  it('shows active when charges and payouts enabled', async () => {
    getStatusMock.mockResolvedValue({
      isConfigured: true,
      stripeAccountId: 'acct_1',
      onboardingStatus: 2,
      chargesEnabled: true,
      payoutsEnabled: true,
      detailsSubmitted: true,
    })
    render(<AdminStripeConnectPage />)
    expect(
      await screen.findByText(/Conta Stripe ativa\. Seu clube pode receber pagamentos de sócios/i),
    ).toBeInTheDocument()
  })

  it('opens onboarding URL in new tab when configuring', async () => {
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null)
    const notConfigured = {
      isConfigured: false,
      stripeAccountId: null,
      onboardingStatus: 0,
      chargesEnabled: false,
      payoutsEnabled: false,
      detailsSubmitted: false,
    }
    getStatusMock.mockResolvedValue(notConfigured)
    syncStatusMock.mockResolvedValue(notConfigured)
    startOnboardingMock.mockResolvedValue({ url: 'https://connect.stripe.com/setup' })
    const user = userEvent.setup()
    render(<AdminStripeConnectPage />)
    await screen.findByRole('button', { name: /Configurar conta Stripe/i })
    await user.click(screen.getByRole('button', { name: /Configurar conta Stripe/i }))
    expect(startOnboardingMock).toHaveBeenCalledWith({
      refreshUrl: `${window.location.origin}/admin/stripe`,
      returnUrl: `${window.location.origin}/admin/stripe`,
    })
    expect(openSpy).toHaveBeenCalledWith(
      'https://connect.stripe.com/setup',
      '_blank',
      'noopener,noreferrer',
    )
    openSpy.mockRestore()
  })

  it('syncs from Stripe when clicking refresh', async () => {
    getStatusMock.mockResolvedValueOnce({
      isConfigured: false,
      stripeAccountId: null,
      onboardingStatus: 0,
      chargesEnabled: false,
      payoutsEnabled: false,
      detailsSubmitted: false,
    })
    syncStatusMock.mockResolvedValue({
      isConfigured: true,
      stripeAccountId: 'acct_x',
      onboardingStatus: 2,
      chargesEnabled: true,
      payoutsEnabled: true,
      detailsSubmitted: true,
    })
    const user = userEvent.setup()
    render(<AdminStripeConnectPage />)
    await screen.findByText(/Seu clube ainda não configurou uma conta Stripe/i)
    await user.click(screen.getByRole('button', { name: /Atualizar status/i }))
    expect(getStatusMock).toHaveBeenCalledTimes(1)
    expect(syncStatusMock).toHaveBeenCalledTimes(1)
    expect(
      await screen.findByText(/Conta Stripe ativa\. Seu clube pode receber pagamentos de sócios/i),
    ).toBeInTheDocument()
  })
})
