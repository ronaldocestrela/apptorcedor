import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ForgotPasswordPage } from './ForgotPasswordPage'

const requestPasswordReset = vi.fn()

vi.mock('../features/auth/passwordResetApi', () => ({
  requestPasswordReset: (...args: unknown[]) => requestPasswordReset(...args),
}))

vi.mock('../shared/branding/brandingApi', () => ({
  getPublicBranding: vi.fn().mockResolvedValue({ teamShieldUrl: null }),
}))

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    requestPasswordReset.mockReset()
    requestPasswordReset.mockResolvedValue(undefined)
  })

  it('submits email to request password reset', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <ForgotPasswordPage />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText(/Email/i), 'a@test.local')
    await user.click(screen.getByRole('button', { name: /Enviar link/i }))

    await waitFor(() => {
      expect(requestPasswordReset).toHaveBeenCalledWith('a@test.local')
    })
    expect(screen.getByRole('status')).toBeInTheDocument()
  })
})
