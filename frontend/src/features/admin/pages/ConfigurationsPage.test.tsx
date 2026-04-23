import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { ConfigurationsPage } from './ConfigurationsPage'
import { EMAIL_WELCOME_TEMPLATE_KEYS } from '../services/adminApi'

const listConfigurations = vi.fn()
const updateConfiguration = vi.fn()
const uploadTeamShield = vi.fn()

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      permissions: [
        ApplicationPermissions.ConfiguracoesVisualizar,
        ApplicationPermissions.ConfiguracoesEditar,
      ],
    },
  }),
}))

vi.mock('../services/adminApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../services/adminApi')>()
  return {
    ...actual,
    listConfigurations: (...a: unknown[]) => listConfigurations(...a),
    updateConfiguration: (...a: unknown[]) => updateConfiguration(...a),
    uploadTeamShield: (...a: unknown[]) => uploadTeamShield(...a),
  }
})

vi.mock('../../account/accountApi', () => ({
  resolvePublicAssetUrl: (path: string | null | undefined) =>
    path ? `https://resolved.test${path.startsWith('/') ? path : `/${path}`}` : undefined,
}))

vi.mock('../../../shared/branding/TeamShieldLogo', () => ({
  TeamShieldLogo: () => <span data-testid="team-shield-mock" />,
}))

describe('ConfigurationsPage — e-mail de boas-vindas', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listConfigurations.mockResolvedValue([
      {
        key: 'Brand.TeamShieldUrl',
        value: '/uploads/x.png',
        version: 1,
        updatedAt: '',
        updatedByUserId: null,
      },
      {
        key: EMAIL_WELCOME_TEMPLATE_KEYS.Subject,
        value: 'Assunto antigo',
        version: 1,
        updatedAt: '',
        updatedByUserId: null,
      },
      {
        key: EMAIL_WELCOME_TEMPLATE_KEYS.Html,
        value: '<p>Hi {{Name}}</p>',
        version: 1,
        updatedAt: '',
        updatedByUserId: null,
      },
      {
        key: EMAIL_WELCOME_TEMPLATE_KEYS.ImageUrl,
        value: '',
        version: 1,
        updatedAt: '',
        updatedByUserId: null,
      },
    ])
    updateConfiguration.mockImplementation(async (key: string, value: string) => ({
      key,
      value,
      version: 2,
      updatedAt: '',
      updatedByUserId: null,
    }))
  })

  it('carrega e salva assunto, HTML e URL de imagem via API', async () => {
    const user = userEvent.setup()
    render(<ConfigurationsPage />)

    await waitFor(() => {
      expect(listConfigurations).toHaveBeenCalled()
    })

    const subjectInput = await screen.findByDisplayValue('Assunto antigo')
    expect(screen.getByDisplayValue('<p>Hi {{Name}}</p>')).toBeInTheDocument()

    // userEvent trata `{` como sintaxe especial; placeholders com {{Name}} precisam de change direto
    fireEvent.change(subjectInput, { target: { value: 'Bem-vindo, {{Name}}' } })

    const imageInput = screen.getByPlaceholderText('https://cdn.exemplo.com/banner.png')
    await user.type(imageInput, 'https://img.test/banner.jpg')

    await user.click(screen.getByTestId('welcome-email-save'))

    await waitFor(() => {
      expect(updateConfiguration).toHaveBeenCalledWith(EMAIL_WELCOME_TEMPLATE_KEYS.Subject, 'Bem-vindo, {{Name}}')
      expect(updateConfiguration).toHaveBeenCalledWith(EMAIL_WELCOME_TEMPLATE_KEYS.Html, '<p>Hi {{Name}}</p>')
      expect(updateConfiguration).toHaveBeenCalledWith(EMAIL_WELCOME_TEMPLATE_KEYS.ImageUrl, 'https://img.test/banner.jpg')
    })
  })

  it('bloqueia salvar com URL de imagem inválida', async () => {
    const user = userEvent.setup()
    render(<ConfigurationsPage />)

    await waitFor(() => expect(listConfigurations).toHaveBeenCalled())

    const imageInput = await screen.findByPlaceholderText('https://cdn.exemplo.com/banner.png')
    await user.clear(imageInput)
    await user.type(imageInput, 'javascript:alert(1)')
    await user.tab()

    expect(await screen.findByRole('alert')).toHaveTextContent(/http ou https/)

    const saveBtn = screen.getByTestId('welcome-email-save')
    expect(saveBtn).toBeDisabled()
  })
})
