import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { TicketsAdminPage } from './TicketsAdminPage'

const listAdminGames = vi.fn()
const listAdminTickets = vi.fn()
const getAdminTicket = vi.fn()
const patchAdminTicketRequestStatus = vi.fn()

const mockPermissions: string[] = []

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({ user: { permissions: mockPermissions } }),
}))

vi.mock('../services/adminApi', () => ({
  getAdminTicket: (...a: unknown[]) => getAdminTicket(...a),
  listAdminGames: (...a: unknown[]) => listAdminGames(...a),
  listAdminTickets: (...a: unknown[]) => listAdminTickets(...a),
  patchAdminTicketRequestStatus: (...a: unknown[]) => patchAdminTicketRequestStatus(...a),
  purchaseAdminTicket: vi.fn(),
  redeemAdminTicket: vi.fn(),
  reserveAdminTicket: vi.fn(),
  syncAdminTicket: vi.fn(),
}))

const gameId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const ticketId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

const sampleItem = {
  ticketId,
  userId: 'f6f6d6c8-0a1a-4c0a-8f0a-000000000001',
  userEmail: 'member@test',
  userName: 'Membro',
  gameId,
  opponent: 'Rival',
  competition: 'Camp',
  gameDate: '2025-12-01T00:00:00.000Z',
  status: 'Reserved',
  externalTicketId: 'ext1',
  qrCode: 'qr1',
  createdAt: '2025-01-01T00:00:00.000Z',
  redeemedAt: null as string | null,
  requestStatus: 'Pending' as const,
  membershipPlanName: 'Plano X',
}

describe('TicketsAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockPermissions.length = 0
    listAdminGames.mockResolvedValue({
      totalCount: 1,
      items: [{ gameId, opponent: 'Riv', competition: 'C', gameDate: '', isActive: true }],
    })
    listAdminTickets.mockResolvedValue({ totalCount: 1, items: [sampleItem] })
    getAdminTicket.mockResolvedValue({ ...sampleItem, updatedAt: '2025-01-01T00:00:00.000Z' })
  })

  it('exibe colunas de solicitação, nome, e-mail e plano', async () => {
    mockPermissions.push(ApplicationPermissions.IngressosVisualizar)
    render(
      <TicketsAdminPage />,
    )
    await waitFor(() => expect(listAdminTickets).toHaveBeenCalled())
    const table = await screen.findByRole('table')
    expect(within(table).getByText('Pendente')).toBeInTheDocument()
    expect(await screen.findByText('Membro')).toBeInTheDocument()
    expect(await screen.findByText('member@test')).toBeInTheDocument()
    expect(await screen.findByText('Plano X')).toBeInTheDocument()
  })

  it('com Ingressos.Gerenciar, altera solicitação para emitido', async () => {
    mockPermissions.push(ApplicationPermissions.IngressosGerenciar)
    render(<TicketsAdminPage />)
    await screen.findByText('Membro')
    const user = userEvent.setup()
    await user.click(screen.getByRole('radio'))
    await waitFor(() => expect(getAdminTicket).toHaveBeenCalled())
    patchAdminTicketRequestStatus.mockResolvedValue(undefined)
    await user.click(screen.getByRole('button', { name: 'Marcar como emitido' }))
    await waitFor(() => expect(patchAdminTicketRequestStatus).toHaveBeenCalledWith(ticketId, 'Issued'))
  })

  it('com só Visualizar, não mostra Marcar como emitido', async () => {
    mockPermissions.push(ApplicationPermissions.IngressosVisualizar)
    render(<TicketsAdminPage />)
    await screen.findByText('Plano X')
    expect(screen.queryByRole('button', { name: 'Marcar como emitido' })).toBeNull()
  })
})
