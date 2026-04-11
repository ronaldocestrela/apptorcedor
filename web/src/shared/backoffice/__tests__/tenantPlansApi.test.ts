import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import {
  assignPlanToTenant,
  getTenantPlanByTenant,
  listTenantsByPlan,
  revokeTenantPlan,
} from '../tenantPlansApi'
import { BillingCycle } from '../types'

vi.mock('../../http/backofficeClient', () => ({
  backofficeClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockedGet = vi.mocked(backofficeClient.get)
const mockedPost = vi.mocked(backofficeClient.post)
const mockedDelete = vi.mocked(backofficeClient.delete)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('tenantPlansApi', () => {
  it('assignPlanToTenant posts saaSPlanId', async () => {
    mockedPost.mockResolvedValue({
      data: { id: 'a1' },
      headers: {},
      status: 201,
      statusText: '',
      config: {} as never,
    })
    await assignPlanToTenant({
      tenantId: 't1',
      saaSPlanId: 'p1',
      startDate: '2025-01-01T00:00:00Z',
      billingCycle: BillingCycle.Monthly,
    })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/tenant-plans', {
      tenantId: 't1',
      saaSPlanId: 'p1',
      startDate: '2025-01-01T00:00:00Z',
      billingCycle: BillingCycle.Monthly,
    })
  })

  it('revokeTenantPlan deletes', async () => {
    mockedDelete.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await revokeTenantPlan('x')
    expect(mockedDelete).toHaveBeenCalledWith('/api/backoffice/tenant-plans/x')
  })

  it('getTenantPlanByTenant', async () => {
    mockedGet.mockResolvedValue({
      data: {
        id: 'tp',
        tenantId: 't',
        saaSPlanId: 'p',
        planName: 'Plan',
        startDate: '',
        endDate: null,
        status: 0,
        billingCycle: 0,
      },
    })
    await getTenantPlanByTenant('t')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/tenant-plans/tenant/t')
  })

  it('listTenantsByPlan', async () => {
    mockedGet.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } })
    await listTenantsByPlan('pid', 2, 5)
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/tenant-plans/plan/pid', {
      params: { page: 2, pageSize: 5 },
    })
  })
})
