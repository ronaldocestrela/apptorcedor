import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DigitalCardPage } from './DigitalCardPage'

vi.mock('../features/torcedor/torcedorDigitalCardApi', () => ({
  getMyDigitalCardWithSource: vi.fn(),
}))

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: '1',
      name: 'Teste User',
      email: 't@test.local',
      roles: [],
      permissions: [],
      requiresProfileCompletion: false,
    },
    loading: false,
    login: vi.fn(),
    register: vi.fn(),
    googleSignIn: vi.fn(),
    logout: vi.fn(),
    refreshProfile: vi.fn(),
  }),
}))

vi.mock('../features/account/accountApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/account/accountApi')>()
  return {
    ...actual,
    getMyProfile: vi.fn().mockResolvedValue({
      document: null,
      birthDate: null,
      photoUrl: null,
      address: null,
    }),
  }
})

import { getMyDigitalCardWithSource } from '../features/torcedor/torcedorDigitalCardApi'

describe('DigitalCardPage', () => {
  beforeEach(() => {
    vi.mocked(getMyDigitalCardWithSource).mockReset()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading then active card content', async () => {
    vi.mocked(getMyDigitalCardWithSource).mockResolvedValue({
      fromCache: false,
      data: {
        state: 'Active',
        membershipStatus: 'Ativo',
        message: null,
        membershipId: 'm',
        digitalCardId: 'c',
        version: 1,
        cardStatus: 'Active',
        issuedAt: '2026-01-01T00:00:00Z',
        verificationToken: 'ABC123',
        templatePreviewLines: ['Titular: Teste', 'Plano: Gold'],
        cacheValidUntilUtc: new Date(Date.now() + 60_000).toISOString(),
      },
    })
    render(
      <MemoryRouter>
        <DigitalCardPage />
      </MemoryRouter>,
    )
    expect(screen.getByText(/Carregando/i)).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByText(/Carteirinha ativa/i)).toBeInTheDocument()
    })
    expect(screen.getByText('Teste User')).toBeInTheDocument()
    expect(screen.getByText('Teste')).toBeInTheDocument()
    expect(screen.getByText('Gold')).toBeInTheDocument()
    expect(screen.getByText(/ABC123/)).toBeInTheDocument()
  })

  it('shows message when not associated', async () => {
    vi.mocked(getMyDigitalCardWithSource).mockResolvedValue({
      fromCache: false,
      data: {
        state: 'NotAssociated',
        membershipStatus: 'NaoAssociado',
        message: 'Sem associação',
        membershipId: null,
        digitalCardId: null,
        version: null,
        cardStatus: null,
        issuedAt: null,
        verificationToken: null,
        templatePreviewLines: null,
        cacheValidUntilUtc: null,
      },
    })
    render(
      <MemoryRouter>
        <DigitalCardPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/Sem associação ativa/i)).toBeInTheDocument()
    })
    expect(screen.getByText('Sem associação')).toBeInTheDocument()
  })

  it('shows error when API fails without cache', async () => {
    vi.mocked(getMyDigitalCardWithSource).mockRejectedValue(new Error('falhou'))
    render(
      <MemoryRouter>
        <DigitalCardPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/falhou/)).toBeInTheDocument()
    })
  })
})
