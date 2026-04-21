import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Me } from '../features/auth/AuthContext'
import { LoginPage } from './LoginPage'

const authMock = {
  user: null as Me | null,
  login: vi.fn(),
  googleSignIn: vi.fn(),
}

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => authMock,
}))

vi.mock('../features/account/accountApi', () => ({
  getRegistrationRequirements: vi.fn(),
}))

vi.mock('../features/account/loadGoogleScript', () => ({
  loadGoogleScript: vi.fn(),
}))

vi.mock('../shared/branding/brandingApi', () => ({
  getPublicBranding: vi.fn().mockResolvedValue({ teamShieldUrl: null }),
}))

describe('LoginPage', () => {
  beforeEach(() => {
    authMock.user = null
    authMock.login.mockReset()
    authMock.googleSignIn.mockReset()
  })

  it('renders new two-column layout with hero and social section', () => {
    const { container } = render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    const logo = screen.getByRole('img', { name: /Escudo do clube/i })
    expect(logo).toHaveAttribute('src', expect.stringMatching(/^data:image\/svg\+xml/))
    expect(screen.getByRole('heading', { level: 1, name: /Acesse a sua Conta/i })).toBeInTheDocument()
    expect(screen.getByText(/Insira seu e-mail e senha para prosseguir/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Esqueceu sua senha/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Entrar com Google/i })).toBeDisabled()
    expect(container.querySelector('.login-page__hero')).toBeInTheDocument()
  })

  it('submits credentials via auth context login', async () => {
    const user = userEvent.setup()
    authMock.login.mockResolvedValue(undefined)

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText(/Email/i), 'fulano@test.local')
    await user.type(screen.getByLabelText(/Senha/i), '123456')
    await user.click(screen.getByRole('button', { name: /^Entrar$/i }))

    await waitFor(() => {
      expect(authMock.login).toHaveBeenCalledWith('fulano@test.local', '123456')
    })
  })

  it('redirects to path from query when already authenticated', async () => {
    authMock.user = {
      id: 'u1',
      email: 'e@test',
      name: 'User',
      roles: [],
      permissions: [],
      requiresProfileCompletion: false,
    }
    render(
      <MemoryRouter initialEntries={['/login?redirect=/plans/p1']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/plans/:planId" element={<div>Plan detail</div>} />
        </Routes>
      </MemoryRouter>,
    )
    expect(await screen.findByText('Plan detail')).toBeInTheDocument()
  })
})
