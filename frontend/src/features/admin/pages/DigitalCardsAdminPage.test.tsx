import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { DigitalCardsAdminPage } from './DigitalCardsAdminPage'

const listAdminDigitalCards = vi.fn()
const getAdminDigitalCard = vi.fn()
const listAdminDigitalCardIssueCandidates = vi.fn()
const issueAdminDigitalCard = vi.fn()
const regenerateAdminDigitalCard = vi.fn()
const invalidateAdminDigitalCard = vi.fn()

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      permissions: [
        ApplicationPermissions.CarteirinhaVisualizar,
        ApplicationPermissions.CarteirinhaGerenciar,
      ],
    },
  }),
}))

vi.mock('../services/adminApi', () => ({
  listAdminDigitalCards: (...a: unknown[]) => listAdminDigitalCards(...a),
  getAdminDigitalCard: (...a: unknown[]) => getAdminDigitalCard(...a),
  listAdminDigitalCardIssueCandidates: (...a: unknown[]) => listAdminDigitalCardIssueCandidates(...a),
  issueAdminDigitalCard: (...a: unknown[]) => issueAdminDigitalCard(...a),
  regenerateAdminDigitalCard: (...a: unknown[]) => regenerateAdminDigitalCard(...a),
  invalidateAdminDigitalCard: (...a: unknown[]) => invalidateAdminDigitalCard(...a),
}))

const candidateA: {
  membershipId: string
  userId: string
  userName: string
  userEmail: string
  planId: string | null
  planName: string | null
} = {
  membershipId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  userName: 'Fulano',
  userEmail: 'fulano@test.local',
  planId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  planName: 'Plano Ouro',
}

function setupListMocks() {
  listAdminDigitalCards.mockResolvedValue({ totalCount: 0, items: [] })
  listAdminDigitalCardIssueCandidates.mockResolvedValue({
    totalCount: 1,
    items: [candidateA],
  })
}

function renderPage() {
  return render(
    <MemoryRouter>
      <DigitalCardsAdminPage />
    </MemoryRouter>,
  )
}

describe('DigitalCardsAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setupListMocks()
  })

  it('loads issue candidates and emits for selected membership', async () => {
    issueAdminDigitalCard.mockResolvedValue(undefined)
    renderPage()

    await waitFor(() => {
      expect(listAdminDigitalCardIssueCandidates).toHaveBeenCalled()
    })

    const select = await screen.findByRole('combobox', { name: /associação/i })
    expect(select).toHaveValue(candidateA.membershipId)
    expect(select).toHaveTextContent(/Fulano — fulano@test\.local · Plano Ouro/)

    await userEvent.click(screen.getByRole('button', { name: /^emitir$/i }))

    await waitFor(() => {
      expect(issueAdminDigitalCard).toHaveBeenCalledWith(candidateA.membershipId)
    })
    expect(listAdminDigitalCardIssueCandidates.mock.calls.length).toBeGreaterThanOrEqual(2)
  })
})
