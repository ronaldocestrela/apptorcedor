import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ResetPasswordPage } from './ResetPasswordPage'

const resetPassword = vi.fn()

vi.mock('../features/auth/passwordResetApi', () => ({
  resetPassword: (...args: unknown[]) => resetPassword(...args),
  formatResetPasswordApiErrorMessage: () => 'err',
}))

vi.mock('../shared/branding/brandingApi', () => ({
  getPublicBranding: vi.fn().mockResolvedValue({ teamShieldUrl: null }),
}))

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    resetPassword.mockReset()
    resetPassword.mockResolvedValue(undefined)
  })

  it('shows error when email or token missing', () => {
    render(
      <MemoryRouter initialEntries={['/reset-password']}>
        <Routes>
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    )
    expect(screen.getByText(/Link inválido ou incompleto/i)).toBeInTheDocument()
  })

  it('submits new password when params present', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/reset-password?email=u@test.local&token=abc']}>
        <Routes>
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/login" element={<div>Login ok</div>} />
        </Routes>
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText(/^Nova senha/i), 'Newpass9A')
    await user.type(screen.getByLabelText(/^Confirmar senha/i), 'Newpass9A')
    await user.click(screen.getByRole('button', { name: /Salvar nova senha/i }))

    await waitFor(() => {
      expect(resetPassword).toHaveBeenCalledWith('u@test.local', 'abc', 'Newpass9A')
    })
    expect(await screen.findByText('Login ok')).toBeInTheDocument()
  })
})
