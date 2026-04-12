import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../http/client'
import {
  getTenantStripeConnectStatus,
  startTenantStripeConnectOnboarding,
  syncTenantStripeConnectStatus,
} from '../stripeConnectTenantApi'

vi.mock('../../http/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

const mockedGet = vi.mocked(apiClient.get)
const mockedPost = vi.mocked(apiClient.post)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('stripeConnectTenantApi', () => {
  it('startTenantStripeConnectOnboarding posts to admin connect onboarding', async () => {
    mockedPost.mockResolvedValue({
      data: { url: 'https://connect.stripe.com/...' },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await startTenantStripeConnectOnboarding({
      refreshUrl: 'https://r',
      returnUrl: 'https://ret',
    })
    expect(mockedPost).toHaveBeenCalledWith('/api/payments/admin/connect/onboarding', {
      refreshUrl: 'https://r',
      returnUrl: 'https://ret',
    })
  })

  it('syncTenantStripeConnectStatus posts to admin connect sync', async () => {
    mockedPost.mockResolvedValue({
      data: {
        isConfigured: true,
        stripeAccountId: 'acct_1',
        onboardingStatus: 2,
        chargesEnabled: true,
        payoutsEnabled: true,
        detailsSubmitted: true,
      },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await syncTenantStripeConnectStatus()
    expect(mockedPost).toHaveBeenCalledWith('/api/payments/admin/connect/sync')
  })

  it('getTenantStripeConnectStatus gets admin connect status', async () => {
    mockedGet.mockResolvedValue({
      data: {
        isConfigured: true,
        stripeAccountId: 'acct_1',
        onboardingStatus: 1,
        chargesEnabled: true,
        payoutsEnabled: false,
        detailsSubmitted: true,
      },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await getTenantStripeConnectStatus()
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/connect/status')
  })
})
