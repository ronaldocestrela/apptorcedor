import { beforeEach, describe, expect, it, vi } from 'vitest'
import { backofficeClient } from '../../http/backofficeClient'
import {
  createSaaSPlan,
  getSaaSPlanById,
  listSaaSPlans,
  toggleSaaSPlan,
  updateSaaSPlan,
} from '../saasPlansApi'

vi.mock('../../http/backofficeClient', () => ({
  backofficeClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
  },
}))

const mockedGet = vi.mocked(backofficeClient.get)
const mockedPost = vi.mocked(backofficeClient.post)
const mockedPut = vi.mocked(backofficeClient.put)
const mockedPatch = vi.mocked(backofficeClient.patch)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('saasPlansApi', () => {
  it('listSaaSPlans', async () => {
    mockedGet.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } })
    await listSaaSPlans(3, 15)
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/plans', { params: { page: 3, pageSize: 15 } })
  })

  it('getSaaSPlanById', async () => {
    mockedGet.mockResolvedValue({
      data: {
        id: 'p1',
        name: 'P',
        description: null,
        monthlyPrice: 1,
        yearlyPrice: null,
        maxMembers: 10,
        stripePriceMonthlyId: null,
        stripePriceYearlyId: null,
        isActive: true,
        createdAt: '',
        updatedAt: '',
        features: [],
      },
    })
    await getSaaSPlanById('p1')
    expect(mockedGet).toHaveBeenCalledWith('/api/backoffice/plans/p1')
  })

  it('createSaaSPlan', async () => {
    mockedPost.mockResolvedValue({
      data: { id: 'new' },
      headers: {},
      status: 201,
      statusText: '',
      config: {} as never,
    })
    await createSaaSPlan({ name: 'N', monthlyPrice: 9.99, maxMembers: 5 })
    expect(mockedPost).toHaveBeenCalledWith('/api/backoffice/plans', {
      name: 'N',
      monthlyPrice: 9.99,
      maxMembers: 5,
    })
  })

  it('updateSaaSPlan', async () => {
    mockedPut.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await updateSaaSPlan('p1', { name: 'X', monthlyPrice: 1, maxMembers: 1 })
    expect(mockedPut).toHaveBeenCalledWith('/api/backoffice/plans/p1', {
      name: 'X',
      monthlyPrice: 1,
      maxMembers: 1,
    })
  })

  it('toggleSaaSPlan', async () => {
    mockedPatch.mockResolvedValue({ data: undefined, headers: {}, status: 200, statusText: '', config: {} as never })
    await toggleSaaSPlan('p1')
    expect(mockedPatch).toHaveBeenCalledWith('/api/backoffice/plans/p1/toggle')
  })
})
