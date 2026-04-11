import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RequireBackofficeAuth } from '../RequireBackofficeAuth'

const getKeyMock = vi.fn()

vi.mock('../../../shared/backoffice/backofficeSession', () => ({
  getBackofficeApiKey: () => getKeyMock(),
}))

describe('RequireBackofficeAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects to login when no API key', () => {
    getKeyMock.mockReturnValue(null)
    render(
      <MemoryRouter initialEntries={['/backoffice']}>
        <Routes>
          <Route element={<RequireBackofficeAuth />}>
            <Route path="/backoffice" element={<div>Protected</div>} />
          </Route>
          <Route path="/backoffice/login" element={<div>Login page</div>} />
        </Routes>
      </MemoryRouter>,
    )
    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('renders outlet when API key present', () => {
    getKeyMock.mockReturnValue('k')
    render(
      <MemoryRouter initialEntries={['/backoffice']}>
        <Routes>
          <Route element={<RequireBackofficeAuth />}>
            <Route path="/backoffice" element={<div>Protected</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )
    expect(screen.getByText('Protected')).toBeInTheDocument()
  })
})
