import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { GamesPage } from './GamesPage'

vi.mock('../features/torcedor/torcedorGamesApi', () => ({
  listTorcedorGames: vi.fn(),
}))

vi.mock('../shared/branding/brandingApi', () => ({
  getPublicBranding: vi.fn(),
}))

import { listTorcedorGames } from '../features/torcedor/torcedorGamesApi'
import { getPublicBranding } from '../shared/branding/brandingApi'

describe('GamesPage', () => {
  beforeEach(() => {
    vi.mocked(listTorcedorGames).mockReset()
    vi.mocked(getPublicBranding).mockReset()
    vi.mocked(getPublicBranding).mockResolvedValue({ teamShieldUrl: null })
  })

  it('renders match cards with active featured game and muted others', async () => {
    vi.mocked(listTorcedorGames).mockResolvedValue({
      totalCount: 2,
      items: [
        {
          gameId: 'g-featured',
          opponent: 'SSA',
          competition: 'Arena Cajueiro',
          opponentLogoUrl: null,
          gameDate: '2099-04-18T15:00:00.000Z',
          createdAt: '2026-01-01T00:00:00.000Z',
        },
        {
          gameId: 'g-later',
          opponent: 'FLU',
          competition: 'Maracanã',
          opponentLogoUrl: null,
          gameDate: '2099-05-01T18:00:00.000Z',
          createdAt: '2026-01-01T00:00:00.000Z',
        },
      ],
    })

    const { container } = render(
      <MemoryRouter>
        <GamesPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.queryByText(/Carregando/i)).not.toBeInTheDocument()
    })

    expect(screen.getByRole('heading', { name: /Partidas/i })).toBeInTheDocument()
    expect(screen.getByText('Evento Próximo')).toBeInTheDocument()
    expect(screen.getByText('Ingresso disponível')).toBeInTheDocument()
    expect(screen.getByText('Ingresso indisponível')).toBeInTheDocument()

    const active = container.querySelector('.game-card-ev--active')
    const muted = container.querySelectorAll('.game-card-ev--muted')
    expect(active).toBeTruthy()
    expect(active?.textContent).toMatch(/FFC/)
    expect(active?.textContent).toMatch(/SSA/)
    expect(muted.length).toBe(1)
    expect(muted[0]?.textContent).toMatch(/FLU/)
  })

  it('groups two games on the same day under one date header', async () => {
    vi.mocked(listTorcedorGames).mockResolvedValue({
      totalCount: 2,
      items: [
        {
          gameId: 'g-a',
          opponent: 'AAA',
          competition: 'C1',
          opponentLogoUrl: null,
          gameDate: '2026-06-10T15:00:00.000Z',
          createdAt: '2026-01-01T00:00:00.000Z',
        },
        {
          gameId: 'g-b',
          opponent: 'BBB',
          competition: 'C2',
          opponentLogoUrl: null,
          gameDate: '2026-06-10T20:00:00.000Z',
          createdAt: '2026-01-01T00:00:00.000Z',
        },
      ],
    })

    const { container } = render(
      <MemoryRouter>
        <GamesPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.queryByText(/Carregando/i)).not.toBeInTheDocument()
    })

    expect(container.querySelectorAll('.games-day')).toHaveLength(1)
    expect(container.querySelectorAll('.game-card-ev')).toHaveLength(2)
  })

  it('shows empty state when there are no games', async () => {
    vi.mocked(listTorcedorGames).mockResolvedValue({ totalCount: 0, items: [] })

    render(
      <MemoryRouter>
        <GamesPage />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(
        screen.getByText(/Nenhum jogo disponível no momento/i),
      ).toBeInTheDocument()
    })
  })
})
