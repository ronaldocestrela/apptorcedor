import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { WebhookTokensAdminPage } from './WebhookTokensAdminPage'

const listAdminPartnerApiKeys = vi.fn()
const createAdminPartnerApiKey = vi.fn()
const revokeAdminPartnerApiKey = vi.fn()

let currentPermissions: string[] = [
  ApplicationPermissions.WebhooksVisualizar,
  ApplicationPermissions.WebhooksGerenciar,
]

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      permissions: currentPermissions,
    },
  }),
}))

vi.mock('../services/adminApi', () => ({
  listAdminPartnerApiKeys: (...a: unknown[]) => listAdminPartnerApiKeys(...a),
  createAdminPartnerApiKey: (...a: unknown[]) => createAdminPartnerApiKey(...a),
  revokeAdminPartnerApiKey: (...a: unknown[]) => revokeAdminPartnerApiKey(...a),
}))

const sampleRows = [
  {
    id: '6bc79a8a-7f97-4ed0-8ca4-c6f2d57e0426',
    name: 'Parceiro Loja A',
    keyPrefix: 'sk_partner_abc123',
    isActive: true,
    createdAt: '2026-05-13T13:45:00Z',
    lastUsedAtUtc: null,
  },
]

function renderPage() {
  return render(
    <MemoryRouter>
      <WebhookTokensAdminPage />
    </MemoryRouter>,
  )
}

describe('WebhookTokensAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    currentPermissions = [
      ApplicationPermissions.WebhooksVisualizar,
      ApplicationPermissions.WebhooksGerenciar,
    ]
    listAdminPartnerApiKeys.mockResolvedValue(sampleRows)
    createAdminPartnerApiKey.mockResolvedValue({
      id: 'ecb25273-561b-4949-aad8-66e7a6d14f6e',
      name: 'Webhook Externo',
      keyPrefix: 'sk_partner_zxy987',
      plaintextKey: 'sk_partner_plaintext_secret_123456',
      createdAt: '2026-05-13T13:50:00Z',
    })
    revokeAdminPartnerApiKey.mockResolvedValue(undefined)
  })

  it('allows visualização sem ações de gerenciamento', async () => {
    currentPermissions = [ApplicationPermissions.WebhooksVisualizar]
    renderPage()

    await waitFor(() => {
      expect(listAdminPartnerApiKeys).toHaveBeenCalled()
    })

    expect(screen.getByText(/Parceiro Loja A/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /revogar/i })).not.toBeInTheDocument()

    const generateTab = screen.getByRole('tab', { name: /gerar token/i })
    expect(generateTab).toBeDisabled()
  })

  it('creates token and shows plaintext once in dialog', async () => {
    renderPage()

    await waitFor(() => {
      expect(listAdminPartnerApiKeys).toHaveBeenCalled()
    })

    await userEvent.click(screen.getByRole('tab', { name: /gerar token/i }))
    await userEvent.type(screen.getByPlaceholderText(/Loja Parceira XPTO/i), 'Webhook Externo')
    await userEvent.click(screen.getByRole('button', { name: /^gerar token$/i }))

    await waitFor(() => {
      expect(createAdminPartnerApiKey).toHaveBeenCalledWith('Webhook Externo')
    })

    expect(await screen.findByText(/Token gerado com sucesso/i)).toBeInTheDocument()
    expect(screen.getByText(/sk_partner_plaintext_secret_123456/i)).toBeInTheDocument()
  })

  it('revokes selected token', async () => {
    renderPage()

    await waitFor(() => {
      expect(listAdminPartnerApiKeys).toHaveBeenCalled()
    })

    await userEvent.click(screen.getByRole('button', { name: /revogar/i }))
    await userEvent.click(screen.getByRole('button', { name: /confirmar revogação/i }))

    await waitFor(() => {
      expect(revokeAdminPartnerApiKey).toHaveBeenCalledWith('6bc79a8a-7f97-4ed0-8ca4-c6f2d57e0426')
    })
  })
})
