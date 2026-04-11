import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BackofficeShell } from '../BackofficeShell'

const clearMock = vi.fn()

vi.mock('../../../shared/backoffice/backofficeSession', () => ({
  clearBackofficeSession: () => clearMock(),
}))

describe('BackofficeShell', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders nav links and outlet', () => {
    render(
      <MemoryRouter initialEntries={['/backoffice']}>
        <Routes>
          <Route path="/backoffice" element={<BackofficeShell />}>
            <Route index element={<span>Home content</span>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )
    expect(screen.getByRole('link', { name: /início/i })).toHaveAttribute('href', '/backoffice')
    expect(screen.getByRole('link', { name: /tenants/i })).toHaveAttribute('href', '/backoffice/tenants')
    expect(screen.getByText('Home content')).toBeInTheDocument()
  })

  it('logout clears session and navigates to login', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/backoffice']}>
        <Routes>
          <Route path="/backoffice" element={<BackofficeShell />}>
            <Route index element={<span>x</span>} />
          </Route>
          <Route path="/backoffice/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>,
    )
    await user.click(screen.getByRole('button', { name: /sair da chave api/i }))
    expect(clearMock).toHaveBeenCalled()
    expect(screen.getByText('Login')).toBeInTheDocument()
  })
})
