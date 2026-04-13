import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DigitalCardPage } from './DigitalCardPage'

vi.mock('../features/torcedor/torcedorDigitalCardApi', () => ({
  getMyDigitalCardWithSource: vi.fn(),
}))

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
        templatePreviewLines: ['Titular: Teste'],
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
    expect(screen.getByText(/Titular: Teste/i)).toBeInTheDocument()
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
