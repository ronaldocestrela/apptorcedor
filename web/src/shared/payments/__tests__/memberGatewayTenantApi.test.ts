import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../http/client'
import {
  configureTenantStripeDirect,
  getTenantMemberGatewayStatus,
} from '../memberGatewayTenantApi'

vi.mock('../../http/client', () => ({
  apiClient: {
    get: vi.fn(),
    put: vi.fn(),
  },
}))

const mockedGet = vi.mocked(apiClient.get)
const mockedPut = vi.mocked(apiClient.put)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('memberGatewayTenantApi', () => {
  it('getTenantMemberGatewayStatus gets admin member-gateway', async () => {
    mockedGet.mockResolvedValue({
      data: {
        selectedProvider: 'StripeDirect',
        status: 'Ready',
        publishableKeyHint: null,
        webhookSecretConfigured: false,
      },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await getTenantMemberGatewayStatus()
    expect(mockedGet).toHaveBeenCalledWith('/api/payments/admin/member-gateway')
  })

  it('configureTenantStripeDirect puts stripe-direct body', async () => {
    mockedPut.mockResolvedValue({
      data: undefined,
      headers: {},
      status: 204,
      statusText: '',
      config: {} as never,
    })
    await configureTenantStripeDirect({
      secretKey: 'sk_test_1',
      publishableKey: 'pk_test_1',
      webhookSecret: 'whsec_1',
    })
    expect(mockedPut).toHaveBeenCalledWith('/api/payments/admin/member-gateway/stripe-direct', {
      secretKey: 'sk_test_1',
      publishableKey: 'pk_test_1',
      webhookSecret: 'whsec_1',
    })
  })
})
