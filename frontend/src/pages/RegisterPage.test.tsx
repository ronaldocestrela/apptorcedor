import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import axios from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Me } from '../features/auth/AuthContext'
import { RegisterPage } from './RegisterPage'

const legal = {
  termsOfUseVersionId: 'terms-v1',
  privacyPolicyVersionId: 'privacy-v1',
  termsTitle: 'Termos de uso',
  privacyTitle: 'Política de privacidade',
}

const authMock = {
  user: null as Me | null,
  register: vi.fn(),
}

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => authMock,
}))

vi.mock('../features/account/accountApi', async (importOriginal) => {
  const mod = await importOriginal<typeof import('../features/account/accountApi')>()
  return {
    ...mod,
    getRegistrationRequirements: vi.fn(),
  }
})

vi.mock('../shared/branding/TeamShieldLogo', () => ({
  TeamShieldLogo: ({ alt }: { alt?: string }) => <img src="data:image/svg+xml,%3Csvg/%3E" alt={alt ?? ''} />,
}))

describe('RegisterPage', () => {
  beforeEach(async () => {
    authMock.user = null
    authMock.register.mockReset()
    const { getRegistrationRequirements } = await import('../features/account/accountApi')
    vi.mocked(getRegistrationRequirements).mockResolvedValue(legal)
  })

  it('shows password rules checklist aligned with Identity policy', async () => {
    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    )

    expect(await screen.findByText(/Mínimo de 8 caracteres/i)).toBeInTheDocument()
    expect(screen.getByText(/Pelo menos uma letra maiúscula/i)).toBeInTheDocument()
    expect(screen.getByText(/Pelo menos uma letra minúscula/i)).toBeInTheDocument()
    expect(screen.getByText(/Pelo menos um número/i)).toBeInTheDocument()
  })

  it('does not call register on submit when password rules are not met', async () => {
    const user = userEvent.setup()
    const { container } = render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    )

    await screen.findByLabelText(/^Nome/i)
    await user.type(screen.getByLabelText(/^Nome/i), 'Fulano')
    await user.type(screen.getByLabelText(/^E-mail/i), 'fulano@test.local')
    await user.type(screen.getByLabelText(/^Senha/i), 'weak')
    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Termos de uso/i }))
    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Política de privacidade/i }))

    const form = container.querySelector('form')
    expect(form).toBeTruthy()
    fireEvent.submit(form!)

    expect(authMock.register).not.toHaveBeenCalled()
    expect(screen.getByRole('alert')).toHaveTextContent(/não atende a todos os requisitos abaixo/i)
  })

  it('disables submit until password satisfies all rules', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    )

    await screen.findByRole('button', { name: /Criar conta/i })
    const submit = screen.getByRole('button', { name: /Criar conta/i })
    expect(submit).toBeDisabled()

    await user.type(screen.getByLabelText(/^Senha/i), 'short')
    expect(submit).toBeDisabled()

    await user.clear(screen.getByLabelText(/^Senha/i))
    await user.type(screen.getByLabelText(/^Senha/i), 'ValidPass1')
    expect(submit).not.toBeDisabled()
  })

  it('submits when password is valid and legal consents are accepted', async () => {
    const user = userEvent.setup()
    authMock.register.mockResolvedValue(undefined)

    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    )

    await screen.findByLabelText(/^Nome/i)
    await user.type(screen.getByLabelText(/^Nome/i), 'Fulano')
    await user.type(screen.getByLabelText(/^E-mail/i), 'fulano@test.local')
    await user.type(screen.getByLabelText(/^Senha/i), 'ValidPass1')

    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Termos de uso/i }))
    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Política de privacidade/i }))
    await user.click(screen.getByRole('button', { name: /Criar conta/i }))

    await waitFor(() => {
      expect(authMock.register).toHaveBeenCalledWith({
        name: 'Fulano',
        email: 'fulano@test.local',
        password: 'ValidPass1',
        phoneNumber: undefined,
        acceptedLegalDocumentVersionIds: [legal.termsOfUseVersionId, legal.privacyPolicyVersionId],
      })
    })
  })

  it('shows API validation errors when register fails with 400', async () => {
    const user = userEvent.setup()
    const axiosErr = new axios.AxiosError('Bad Request')
    axiosErr.response = {
      status: 400,
      data: { errors: ['Passwords must have at least one digit.'] },
      statusText: 'Bad Request',
      headers: {},
      config: {} as never,
    }
    authMock.register.mockRejectedValue(axiosErr)

    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    )

    await screen.findByLabelText(/^Nome/i)
    await user.type(screen.getByLabelText(/^Nome/i), 'Fulano')
    await user.type(screen.getByLabelText(/^E-mail/i), 'fulano@test.local')
    await user.type(screen.getByLabelText(/^Senha/i), 'ValidPass1')
    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Termos de uso/i }))
    await user.click(screen.getByRole('checkbox', { name: /Li e aceito: Política de privacidade/i }))
    await user.click(screen.getByRole('button', { name: /Criar conta/i }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent(/Passwords must have at least one digit/i)
  })
})
