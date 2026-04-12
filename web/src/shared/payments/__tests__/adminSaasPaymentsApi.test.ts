import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../http/client'
import {
  attachAdminSaasCard,
  createAdminSaasSetupIntent,
  deleteAdminSaasCard,
  getAdminSaasStripeConfig,
  getAdminSaasSubscription,
  listAdminSaasCards,
  listAdminSaasInvoices,
} from '../adminSaasPaymentsApi'

vi.mock('../../http/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockedGet = vi.mocked(apiClient.get)
const mockedPost = vi.mocked(apiClient.post)
const mockedDelete = vi.mocked(apiClient.delete)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('adminSaasPaymentsApi', () => {
  it('getAdminSaasStripeConfig', async () => {
    mockedGet.mockResolvedValue({ data: { publishableKey: 'pk_test' } } as never)
    const r = await getAdminSaasStripeConfig()
    expect(r.publishableKey).toBe('pk_test')
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/saas/stripe-config')
  })

  it('getAdminSaasSubscription', async () => {
    mockedGet.mockResolvedValue({ data: null })
    await getAdminSaasSubscription()
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/saas/subscription')
  })

  it('listAdminSaasInvoices', async () => {
    mockedGet.mockResolvedValue({
      data: { items: [], totalCount: 0, page: 1, pageSize: 20 },
    } as never)
    await listAdminSaasInvoices(1, 10)
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/saas/invoices', {
      params: { page: 1, pageSize: 10 },
    })
  })

  it('listAdminSaasCards', async () => {
    mockedGet.mockResolvedValue({ data: [] })
    await listAdminSaasCards()
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/saas/cards')
  })

  it('createAdminSaasSetupIntent', async () => {
    mockedPost.mockResolvedValue({
      data: { clientSecret: 'sec', setupIntentId: 'seti_1' },
    } as never)
    const r = await createAdminSaasSetupIntent()
    expect(r.clientSecret).toBe('sec')
    expect(mockedPost).toHaveBeenCalledWith('/api/payments/admin/saas/cards/setup-intent')
  })

  it('attachAdminSaasCard', async () => {
    mockedPost.mockResolvedValue({ data: undefined } as never)
    await attachAdminSaasCard({ paymentMethodId: 'pm_1', setAsDefault: true })
    expect(mockedPost).toHaveBeenCalledWith('/api/payments/admin/saas/cards', {
      paymentMethodId: 'pm_1',
      setAsDefault: true,
    })
  })

  it('deleteAdminSaasCard', async () => {
    mockedDelete.mockResolvedValue({ data: undefined } as never)
    await deleteAdminSaasCard('pm_1')
    expect(mockedDelete).toHaveBeenCalledWith('/api/payments/admin/saas/cards/pm_1')
  })
})
