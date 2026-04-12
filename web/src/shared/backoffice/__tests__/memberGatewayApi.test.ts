import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import { getMemberGatewayStatus, setMemberGatewayProvider } from '../memberGatewayApi'

vi.mock('../../http/backofficeClient', () => ({
  backofficeClient: {
    get: vi.fn(),
    put: vi.fn(),
  },
}))

const mockedGet = vi.mocked(backofficeClient.get)
const mockedPut = vi.mocked(backofficeClient.put)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('memberGatewayApi', () => {
  it('setMemberGatewayProvider puts provider', async () => {
    mockedPut.mockResolvedValue({
      data: undefined,
      headers: {},
      status: 204,
      statusText: '',
      config: {} as never,
    })
    await setMemberGatewayProvider('tid', 'StripeDirect')
    expect(mockedPut).toHaveBeenCalledWith('/api/backoffice/payments/member-gateway/tenants/tid/provider', {
      provider: 'StripeDirect',
    })
  })

  it('getMemberGatewayStatus gets tenant status', async () => {
    mockedGet.mockResolvedValue({
      data: {
        selectedProvider: 'StripeDirect',
        status: 'Ready',
        publishableKeyHint: 'pk_live_…',
        webhookSecretConfigured: true,
      },
      headers: {},
      status: 200,
      statusText: '',
      config: {} as never,
    })
    await getMemberGatewayStatus('tid')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/payments/member-gateway/tenants/tid/status')
  })
})
