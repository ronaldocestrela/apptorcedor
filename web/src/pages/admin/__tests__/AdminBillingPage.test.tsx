import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminBillingPage } from '../AdminBillingPage'

const getConfigMock = vi.fn()
const getSubMock = vi.fn()
const listInvMock = vi.fn()
const listCardsMock = vi.fn()

vi.mock('../../../shared/payments/adminSaasPaymentsApi', () => ({
  getAdminSaasStripeConfig: (...a: unknown[]) => getConfigMock(...a),
  getAdminSaasSubscription: (...a: unknown[]) => getSubMock(...a),
  listAdminSaasInvoices: (...a: unknown[]) => listInvMock(...a),
  listAdminSaasCards: (...a: unknown[]) => listCardsMock(...a),
  createAdminSaasSetupIntent: vi.fn(),
  deleteAdminSaasCard: vi.fn(),
}))

describe('AdminBillingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading initially', () => {
    getConfigMock.mockReturnValue(new Promise(() => {}))
    getSubMock.mockReturnValue(new Promise(() => {}))
    render(<AdminBillingPage />)
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
  })

  it('shows message when there is no SaaS subscription', async () => {
    getConfigMock.mockResolvedValue({ publishableKey: null })
    getSubMock.mockResolvedValue(null)
    render(<AdminBillingPage />)
    expect(
      await screen.findByText(/Não há assinatura ativa de faturamento SaaS/i),
    ).toBeInTheDocument()
    expect(listInvMock).not.toHaveBeenCalled()
  })

  it('shows subscription and invoices when active', async () => {
    getConfigMock.mockResolvedValue({ publishableKey: 'pk_test' })
    getSubMock.mockResolvedValue({
      id: 's1',
      tenantId: 't1',
      tenantPlanId: 'tp',
      saaSPlanId: 'sp',
      billingCycle: 0,
      recurringAmount: 99,
      currency: 'BRL',
      status: 1,
      externalCustomerId: 'cus_1',
      externalSubscriptionId: 'sub_1',
      nextBillingAtUtc: null,
      createdAtUtc: new Date().toISOString(),
    })
    listInvMock.mockResolvedValue({
      items: [
        {
          id: 'i1',
          tenantBillingSubscriptionId: 's1',
          amount: 99,
          currency: 'BRL',
          dueAtUtc: new Date().toISOString(),
          status: 1,
          externalInvoiceId: null,
          paidAtUtc: null,
          createdAtUtc: new Date().toISOString(),
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    })
    listCardsMock.mockResolvedValue([
      {
        id: 'pm_1',
        brand: 'visa',
        last4: '4242',
        expMonth: 12,
        expYear: 2030,
        isDefault: true,
      },
    ])
    render(<AdminBillingPage />)
    expect(await screen.findByRole('heading', { name: /Assinatura/i })).toBeInTheDocument()
    expect(screen.getByRole('table')).toHaveTextContent('99.00')
    expect(screen.getByText(/visa/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Adicionar cartão/i })).toBeInTheDocument()
  })
})
