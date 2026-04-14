import { render, screen, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { AdminDashboardPage } from './AdminDashboardPage'

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

const getAdminDashboardMock = vi.fn()

vi.mock('../services/adminApi', () => ({
  getAdminDashboard: () => getAdminDashboardMock(),
}))

describe('AdminDashboardPage', () => {
  it('renders dashboard hero and themed KPI cards after loading', async () => {
    getAdminDashboardMock.mockResolvedValue({
      activeMembersCount: 100,
      delinquentMembersCount: 50,
      openSupportTickets: 6,
    })

    const { container } = render(<AdminDashboardPage />)

    expect(screen.getByText('Carregando...')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Painel administrativo' })).toBeInTheDocument()
    })

    expect(container.querySelector('.admin-dashboard')).toBeInTheDocument()
    expect(container.querySelector('.admin-dashboard__kpi-grid')).toBeInTheDocument()
    expect(container.querySelectorAll('.admin-kpi-card')).toHaveLength(3)
    expect(screen.getByText('100')).toBeInTheDocument()
    expect(screen.getByText('50')).toBeInTheDocument()
    expect(screen.getByText('6')).toBeInTheDocument()
  })

  it('shows error feedback when API fails', async () => {
    getAdminDashboardMock.mockRejectedValue(new Error('network'))

    render(<AdminDashboardPage />)

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Falha ao carregar o painel.')
    })
  })
})
