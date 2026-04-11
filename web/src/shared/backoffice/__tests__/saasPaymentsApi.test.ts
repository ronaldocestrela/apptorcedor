import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import {
  createTenantSaasPortalSession,
  getTenantSaasSubscription,
  listTenantSaasInvoices,
  startTenantSaasBilling,
} from '../saasPaymentsApi'

vi.mock('../../http/backofficeClient', () => ({
  backofficeClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

const mockedGet = vi.mocked(backofficeClient.get)
const mockedPost = vi.mocked(backofficeClient.post)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('saasPaymentsApi', () => {
  it('startTenantSaasBilling', async () => {
    mockedPost.mockResolvedValue({
      data: { id: 'sub' },
      headers: {},
      status: 201,
      statusText: '',
      config: {} as never,
    })
    await startTenantSaasBilling('tid')
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/payments/saas/tenants/tid/billing/start')
  })

  it('getTenantSaasSubscription', async () => {
    mockedGet.mockResolvedValue({ data: null })
    await getTenantSaasSubscription('tid')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/payments/saas/tenants/tid/subscription')
  })

  it('listTenantSaasInvoices', async () => {
    mockedGet.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } })
    await listTenantSaasInvoices('tid', 1, 10)
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/payments/saas/tenants/tid/invoices', {
      params: { page: 1, pageSize: 10 },
    })
  })

  it('createTenantSaasPortalSession', async () => {
    mockedPost.mockResolvedValue({
      data: { url: 'https://stripe.example' },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await createTenantSaasPortalSession('tid', 'https://app.example/back')
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/payments/saas/tenants/tid/billing/portal', {
      returnUrl: 'https://app.example/back',
    })
  })
})
