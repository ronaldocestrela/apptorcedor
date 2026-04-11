import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TenantStatus } from '../../../shared/backoffice/types'
import { TenantsListPage } from '../TenantsListPage'

const listMock = vi.fn()

vi.mock('../../../shared/backoffice', async () => {
  const actual = await vi.importActual<typeof import('../../../shared/backoffice')>('../../../shared/backoffice')
  return {
    ...actual,
    listTenants: (...a: unknown[]) => listMock(...a),
    createTenant: vi.fn(),
  }
})

describe('TenantsListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listMock.mockResolvedValue({
      items: [
        {
          id: 't1',
          name: 'Clube A',
          slug: 'clubea',
          status: TenantStatus.Active,
          createdAt: '2025-01-01T00:00:00Z',
          domainCount: 2,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    })
  })

  it('renders tenant rows after load', async () => {
    render(
      <MemoryRouter>
        <Routes>
          <Route path="/" element={<TenantsListPage />} />
        </Routes>
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText('Clube A')).toBeInTheDocument()
    })
    expect(screen.getByText('clubea')).toBeInTheDocument()
    expect(listMock).toHaveBeenCalled()
  })

  it('opens create modal', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <TenantsListPage />
      </MemoryRouter>,
    )
    await waitFor(() => screen.getByText('Clube A'))
    await user.click(screen.getByRole('button', { name: /novo tenant/i }))
    expect(screen.getByRole('dialog', { name: /novo tenant/i })).toBeInTheDocument()
  })
})
