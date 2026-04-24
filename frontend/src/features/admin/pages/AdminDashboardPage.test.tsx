import { render, screen, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AdminDashboardPage } from './AdminDashboardPage'

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

const getAdminDashboardMock = vi.fn()

vi.mock('../services/adminApi', () => ({
  getAdminDashboard: () => getAdminDashboardMock(),
}))

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminDashboardPage />
    </MemoryRouter>,
  )
}

describe('AdminDashboardPage', () => {
  it('renders dashboard hero and themed KPI cards after loading', async () => {
    getAdminDashboardMock.mockResolvedValue({
      activeMembersCount: 100,
      delinquentMembersCount: 50,
      openSupportTickets: 6,
      totalFaturadoLast30Days: 1234.56,
    })

    const { container } = renderPage()

    expect(container.querySelector('.admin-kpi-skeleton')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Painel administrativo' })).toBeInTheDocument()
    })

    expect(container.querySelector('.admin-dashboard')).toBeInTheDocument()
    expect(container.querySelector('.admin-dashboard__kpi-grid')).toBeInTheDocument()
    expect(container.querySelectorAll('.admin-kpi-card')).toHaveLength(4)
    expect(screen.getByText('100')).toBeInTheDocument()
    expect(screen.getByText('50')).toBeInTheDocument()
    expect(screen.getByText('6')).toBeInTheDocument()
    expect(screen.getByText('R$ 1.234,56')).toBeInTheDocument()
  })

  it('formats zero total faturado as BRL', async () => {
    getAdminDashboardMock.mockResolvedValue({
      activeMembersCount: 0,
      delinquentMembersCount: 0,
      openSupportTickets: 0,
      totalFaturadoLast30Days: 0,
    })

    renderPage()

    await waitFor(() => {
      expect(screen.getByText('R$ 0,00')).toBeInTheDocument()
    })
  })

  it('shows error feedback when API fails', async () => {
    getAdminDashboardMock.mockRejectedValue(new Error('network'))

    renderPage()

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Falha ao carregar o painel.')
    })
  })
})
