import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BackofficeLoginPage } from '../BackofficeLoginPage'

const validateMock = vi.fn()
const setKeyMock = vi.fn()
const getKeyMock = vi.fn(() => null)

vi.mock('../../../shared/backoffice', () => ({
  validateBackofficeApiKey: (...a: unknown[]) => validateMock(...a),
  setBackofficeApiKey: (...a: unknown[]) => setKeyMock(...a),
  getBackofficeApiKey: () => getKeyMock(),
}))

describe('BackofficeLoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getKeyMock.mockReturnValue(null)
  })

  it('submits API key and navigates on success', async () => {
    validateMock.mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/backoffice/login']}>
        <Routes>
          <Route path="/backoffice/login" element={<BackofficeLoginPage />} />
          <Route path="/backoffice" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>,
    )
    await user.type(screen.getByLabelText(/chave de api/i), 'secret-key')
    await user.click(screen.getByRole('button', { name: /entrar/i }))
    expect(validateMock).toHaveBeenCalledWith('secret-key')
    expect(setKeyMock).toHaveBeenCalledWith('secret-key')
    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  })

  it('shows error when validation fails', async () => {
    validateMock.mockRejectedValue(new Error('nope'))
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <BackofficeLoginPage />
      </MemoryRouter>,
    )
    await user.type(screen.getByLabelText(/chave de api/i), 'bad')
    await user.click(screen.getByRole('button', { name: /entrar/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent('nope')
    expect(setKeyMock).not.toHaveBeenCalled()
  })
})
