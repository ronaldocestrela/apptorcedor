import axios from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import {
  addTenantDomain,
  addTenantSetting,
  changeTenantStatus,
  createTenant,
  getTenantById,
  listTenants,
  removeTenantDomain,
  removeTenantSetting,
  updateTenant,
  updateTenantSetting,
  validateBackofficeApiKey,
} from '../tenantsApi'
import { TenantStatus } from '../types'

vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>()
  return {
    ...actual,
    default: { ...actual.default, get: vi.fn() },
  }
})

vi.mock('../../http/backofficeClient', () => ({
  backofficeClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockedGet = vi.mocked(backofficeClient.get)
const mockedPost = vi.mocked(backofficeClient.post)
const mockedPut = vi.mocked(backofficeClient.put)
const mockedPatch = vi.mocked(backofficeClient.patch)
const mockedDelete = vi.mocked(backofficeClient.delete)
const mockedAxiosGet = vi.mocked(axios.get)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('validateBackofficeApiKey', () => {
  it('calls GET tenants with X-Api-Key header', async () => {
    mockedAxiosGet.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 1 } })
    await validateBackofficeApiKey('my-key')
    expect(mockedAxiosGet).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/backoffice\/tenants$/),
      expect.objectContaining({
        params: { page: 1, pageSize: 1 },
        headers: expect.objectContaining({ 'X-Api-Key': 'my-key' }),
      }),
    )
  })
})

describe('tenantsApi', () => {
  it('listTenants passes query params', async () => {
    mockedGet.mockResolvedValue({ data: { items: [], totalCount: 0, page: 2, pageSize: 10 } })
    const res = await listTenants({ page: 2, pageSize: 10, search: 'x', status: TenantStatus.Active })
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/tenants', {
      params: { page: 2, pageSize: 10, search: 'x', status: TenantStatus.Active },
    })
    expect(res.page).toBe(2)
  })

  it('getTenantById uses path id', async () => {
    mockedGet.mockResolvedValue({
      data: {
        id: 't1',
        name: 'N',
        slug: 's',
        connectionString: '',
        status: 0,
        createdAt: '',
        domains: [],
        settings: [],
      },
    })
    await getTenantById('t1')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/tenants/t1')
  })

  it('createTenant posts body', async () => {
    mockedPost.mockResolvedValue({ data: { id: 'new-id' }, headers: {}, status: 201, statusText: '', config: {} as never })
    const r = await createTenant({ name: 'A', slug: 'a' })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/tenants', { name: 'A', slug: 'a' })
    expect(r.id).toBe('new-id')
  })

  it('updateTenant puts', async () => {
    mockedPut.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await updateTenant('id', { name: 'B' })
    expect(mockedPut).toHaveBeenCalledWith('/api/backoffice/tenants/id', { name: 'B' })
  })

  it('changeTenantStatus patches', async () => {
    mockedPatch.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await changeTenantStatus('id', { status: TenantStatus.Suspended })
    expect(mockedPatch).toHaveBeenCalledWith('/api/backoffice/tenants/id/status', {
      status: TenantStatus.Suspended,
    })
  })

  it('addTenantDomain posts', async () => {
    mockedPost.mockResolvedValue({
      data: { domainId: 'd1' },
      headers: {},
      status: 201,
      statusText: '',
      config: {} as never,
    })
    await addTenantDomain('tid', { origin: 'https://x.com' })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/tenants/tid/domains', { origin: 'https://x.com' })
  })

  it('removeTenantDomain deletes', async () => {
    mockedDelete.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await removeTenantDomain('tid', 'did')
    expect(mockedDelete).toHaveBeenCalledWith('/api/backoffice/tenants/tid/domains/did')
  })

  it('addTenantSetting posts', async () => {
    mockedPost.mockResolvedValue({
      data: { settingId: 's1' },
      headers: {},
      status: 201,
      statusText: '',
      config: {} as never,
    })
    await addTenantSetting('tid', { key: 'k', value: 'v' })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/tenants/tid/settings', { key: 'k', value: 'v' })
  })

  it('updateTenantSetting puts', async () => {
    mockedPut.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await updateTenantSetting('tid', 'sid', { value: 'nv' })
    expect(mockedPut).toHaveBeenCalledWith('/api/backoffice/tenants/tid/settings/sid', { value: 'nv' })
  })

  it('removeTenantSetting deletes', async () => {
    mockedDelete.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await removeTenantSetting('tid', 'sid')
    expect(mockedDelete).toHaveBeenCalledWith('/api/backoffice/tenants/tid/settings/sid')
  })
})
