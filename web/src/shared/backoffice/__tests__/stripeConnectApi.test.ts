import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import { getStripeConnectStatus, startStripeConnectOnboarding } from '../stripeConnectApi'

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

describe('stripeConnectApi', () => {
  it('startStripeConnectOnboarding', async () => {
    mockedPost.mockResolvedValue({
      data: { url: 'https://connect.stripe.com/...' },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await startStripeConnectOnboarding('tid', {
      refreshUrl: 'https://r',
      returnUrl: 'https://ret',
    })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/payments/connect/tenants/tid/onboarding', {
      refreshUrl: 'https://r',
      returnUrl: 'https://ret',
    })
  })

  it('getStripeConnectStatus', async () => {
    mockedGet.mockResolvedValue({
      data: {
        isConfigured: true,
        stripeAccountId: 'acct_1',
        onboardingStatus: 1,
        chargesEnabled: true,
        payoutsEnabled: false,
        detailsSubmitted: true,
      },
    })
    await getStripeConnectStatus('tid')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/payments/connect/tenants/tid/status')
  })
})
