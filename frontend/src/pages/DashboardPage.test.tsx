import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DashboardPage } from './DashboardPage'
import { ApplicationPermissions } from '../shared/auth/applicationPermissions'

const authMock = {
  user: {
    name: 'Ronaldo Silva',
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
    authMock.user.requiresProfileCompletion = false
    authMock.user.permissions = [ApplicationPermissions.UsuariosVisualizar]
  })

  it('renders mobile-first shell with greeting, quick grid and bottom nav', () => {
    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(container.querySelector('.dash-root')).toBeInTheDocument()
    expect(container.querySelector('.dash-hero')).toBeInTheDocument()
    expect(container.querySelector('.dash-quick-grid')).toBeInTheDocument()
    expect(screen.getByText('Ronaldo')).toBeInTheDocument()
    const bottomNav = container.querySelector('.dash-bottom-nav')
    expect(bottomNav).toBeInTheDocument()
    expect(bottomNav!.querySelectorAll('.dash-bottom-nav__item')).toHaveLength(5)
  })

  it('shows admin badge when user has admin permissions', () => {
    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: /Painel administrativo/i })).toBeInTheDocument()
  })

  it('hides admin badge when user has no admin permissions', () => {
    authMock.user.permissions = []

    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(screen.queryByRole('link', { name: /Painel administrativo/i })).not.toBeInTheDocument()
  })

  it('shows profile completion alert when requiresProfileCompletion is true', () => {
    authMock.user.requiresProfileCompletion = true

    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(container.querySelector('.dash-alert')).toBeInTheDocument()
    expect(screen.getByText(/Complete seu perfil/i)).toBeInTheDocument()
  })
})
