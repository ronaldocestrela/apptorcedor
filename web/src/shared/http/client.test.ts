import { describe, expect, it, vi } from 'vitest'

const reqUse = vi.fn()
const resUse = vi.fn()

vi.mock('../auth/tokenStorage', () => ({
  getAccessToken: vi.fn(() => 'jwt-token'),
  clearSession: vi.fn(),
}))

vi.mock('../tenant', () => ({
  getTenantSlugFromBrowser: vi.fn(() => 'meu-clube'),
}))

vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      interceptors: {
        request: { use: (fn: unknown) => reqUse(fn) },
        response: { use: (onFulfilled: unknown, onRejected: unknown) => resUse(onFulfilled, onRejected) },
      },
    })),
    isAxiosError: vi.fn(
      (e: unknown): e is { response?: { status: number } } =>
        typeof e === 'object' && e !== null && 'response' in e,
    ),
  },
}))

describe('apiClient', () => {
  it('request interceptor sets X-Tenant-Id and Authorization', async () => {
    vi.clearAllMocks()
    await import('./client')
    expect(reqUse).toHaveBeenCalled()
    const handler = reqUse.mock.calls[0][0] as (config: {
      headers?: { set?: (n: string, v: string) => void }
    }) => unknown
    const set = vi.fn()
    handler({ headers: { set } })
    expect(set).toHaveBeenCalledWith('X-Tenant-Id', 'meu-clube')
    expect(set).toHaveBeenCalledWith('Authorization', 'Bearer jwt-token')
  })
})
