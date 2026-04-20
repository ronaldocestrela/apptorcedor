import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AdminLayout } from './AdminLayout'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'

const authMock = {
  user: {
    id: 'admin-1',
    email: 'admin@test.local',
    name: 'Admin',
    roles: ['Administrador'],
    permissions: [ApplicationPermissions.UsuariosVisualizar],
    requiresProfileCompletion: false,
  },
}

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => authMock,
}))

describe('AdminLayout', () => {
  it('renders themed shell classes and keeps outlet content', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/admin/dashboard']}>
        <Routes>
          <Route path="/admin" element={<AdminLayout />}>
            <Route path="dashboard" element={<div>dashboard-content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(container.querySelector('.admin-shell')).toBeInTheDocument()
    expect(container.querySelector('.admin-shell__sidebar')).toBeInTheDocument()
    expect(container.querySelector('.admin-shell__content')).toBeInTheDocument()
    expect(screen.getByText('dashboard-content')).toBeInTheDocument()
  })

  it('keeps permission-driven menu visibility', () => {
    render(
      <MemoryRouter initialEntries={['/admin/dashboard']}>
        <Routes>
          <Route path="/admin" element={<AdminLayout />}>
            <Route path="dashboard" element={<div>dashboard-content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: 'Painel' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Staff' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Pagamentos' })).not.toBeInTheDocument()
  })
})
