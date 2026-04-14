import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DashboardPage } from './DashboardPage'
import { ApplicationPermissions } from '../shared/auth/applicationPermissions'

const authMock = {
  user: {
    name: 'Ronaldo',
    email: 'ronaldo@test.local',
    roles: ['Administrador'],
    permissions: [ApplicationPermissions.UsuariosVisualizar],
    requiresProfileCompletion: false,
  },
  logout: vi.fn(),
}

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => authMock,
}))

describe('DashboardPage', () => {
  beforeEach(() => {
    authMock.logout.mockReset()
  })

  it('renders modern responsive shell with quick links and admin access', () => {
    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(container.querySelector('.dashboard-page')).toBeInTheDocument()
    expect(container.querySelector('.dashboard-page__hero')).toBeInTheDocument()
    expect(container.querySelector('.dashboard-page__links-grid')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Painel administrativo/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Sair/i })).toBeInTheDocument()
  })

  it('hides admin link when user has no admin permissions', () => {
    authMock.user.permissions = []

    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(screen.queryByRole('link', { name: /Painel administrativo/i })).not.toBeInTheDocument()

    authMock.user.permissions = [ApplicationPermissions.UsuariosVisualizar]
  })
})
