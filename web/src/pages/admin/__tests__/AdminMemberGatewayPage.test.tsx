import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminMemberGatewayPage } from '../AdminMemberGatewayPage'

const getStatusMock = vi.fn()
const configureMock = vi.fn()

vi.mock('../../../shared/payments/memberGatewayTenantApi', () => ({
  getTenantMemberGatewayStatus: (...a: unknown[]) => getStatusMock(...a),
  configureTenantStripeDirect: (...a: unknown[]) => configureMock(...a),
}))

describe('AdminMemberGatewayPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while fetching status', () => {
    getStatusMock.mockReturnValue(new Promise(() => {}))
    render(<AdminMemberGatewayPage />)
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
  })

  it('shows StripeDirect not assigned when provider is None', async () => {
    getStatusMock.mockResolvedValue({
      selectedProvider: 'None',
      status: 'NotConfigured',
      publishableKeyHint: null,
      webhookSecretConfigured: false,
    })
    render(<AdminMemberGatewayPage />)
    expect(
      await screen.findByText(/ainda não foi atribuído a este clube no backoffice da plataforma/i),
    ).toBeInTheDocument()
  })

  it('submits credentials when provider string is case-insensitive (stripedirect)', async () => {
    getStatusMock.mockResolvedValue({
      selectedProvider: 'stripedirect',
      status: 'Ready',
      publishableKeyHint: null,
      webhookSecretConfigured: false,
    })
    configureMock.mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<AdminMemberGatewayPage />)
    await screen.findByText(/Credenciais Stripe/i)
    await user.type(screen.getByLabelText(/Secret key/i), 'sk_test_abc')
    await user.click(screen.getByRole('button', { name: /Salvar credenciais/i }))
    expect(configureMock).toHaveBeenCalled()
  })

  it('submits credentials when StripeDirect is selected', async () => {
    getStatusMock.mockResolvedValue({
      selectedProvider: 'StripeDirect',
      status: 'Ready',
      publishableKeyHint: 'pk_live_…',
      webhookSecretConfigured: true,
    })
    configureMock.mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<AdminMemberGatewayPage />)
    await screen.findByText(/Credenciais Stripe/i)
    await user.type(screen.getByLabelText(/Secret key/i), 'sk_test_abc')
    await user.type(screen.getByLabelText(/Chave publicável/i), 'pk_test_abc')
    await user.type(screen.getByLabelText(/Webhook signing secret/i), 'whsec_abc')
    await user.click(screen.getByRole('button', { name: /Salvar credenciais/i }))
    expect(configureMock).toHaveBeenCalledWith({
      secretKey: 'sk_test_abc',
      publishableKey: 'pk_test_abc',
      webhookSecret: 'whsec_abc',
    })
  })

  it('shows validation error when secret is empty on submit', async () => {
    getStatusMock.mockResolvedValue({
      selectedProvider: 'StripeDirect',
      status: 'Ready',
      publishableKeyHint: null,
      webhookSecretConfigured: false,
    })
    const user = userEvent.setup()
    render(<AdminMemberGatewayPage />)
    await screen.findByText(/Credenciais Stripe/i)
    await user.click(screen.getByRole('button', { name: /Salvar credenciais/i }))
    expect(configureMock).not.toHaveBeenCalled()
    expect(await screen.findByText(/Informe a secret key/i)).toBeInTheDocument()
  })
})
