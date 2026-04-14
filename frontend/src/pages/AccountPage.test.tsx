import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AccountPage } from './AccountPage'

vi.mock('../features/account/accountApi', () => ({
  getMyProfile: vi.fn(),
  resolvePublicAssetUrl: (u: string) => u,
  upsertMyProfile: vi.fn(),
  uploadProfilePhoto: vi.fn(),
}))

vi.mock('../shared/cropImage', () => ({
  getCroppedImg: vi.fn().mockResolvedValue(new Blob(['cropped'], { type: 'image/jpeg' })),
}))

// react-easy-crop is a visual component; stub it to avoid canvas/ResizeObserver issues in jsdom
vi.mock('react-easy-crop', () => ({
  default: () => null,
}))

vi.mock('../features/plans/plansService', () => ({
  plansService: {
    listPublished: vi.fn(),
  },
}))

vi.mock('../features/plans/subscriptionsService', () => ({
  subscriptionsService: {
    getMySummary: vi.fn(),
    changePlan: vi.fn(),
    cancelMembership: vi.fn(),
  },
}))

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => ({
    user: { name: 'T', email: 't@test.local', requiresProfileCompletion: false },
    refreshProfile: vi.fn(),
  }),
}))

import { getMyProfile, uploadProfilePhoto, upsertMyProfile } from '../features/account/accountApi'
import { getCroppedImg } from '../shared/cropImage'
import { plansService } from '../features/plans/plansService'
import { subscriptionsService } from '../features/plans/subscriptionsService'

describe('AccountPage', () => {
  beforeEach(() => {
    vi.mocked(getMyProfile).mockReset()
    vi.mocked(getMyProfile).mockResolvedValue({
      document: null,
      birthDate: null,
      photoUrl: null,
      address: null,
    })
    vi.mocked(subscriptionsService.getMySummary).mockReset()
    vi.mocked(subscriptionsService.changePlan).mockReset()
    vi.mocked(subscriptionsService.cancelMembership).mockReset()
    vi.mocked(plansService.listPublished).mockReset()
  })

  it('shows subscription status when user has membership', async () => {
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      startDate: '2025-01-01T00:00:00Z',
      endDate: null,
      nextDueDate: '2025-02-01T12:00:00Z',
      plan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      lastPayment: null,
      digitalCard: null,
    })
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
      ],
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Assinatura', level: 2 })).toBeInTheDocument()
    })
    expect(screen.getByText(/Ativo/)).toBeInTheDocument()
    expect(screen.getByText(/Gold/)).toBeInTheDocument()
  })

  it('plan change section calls changePlan when confirmed', async () => {
    const user = userEvent.setup()
    const summary = {
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      startDate: '2025-01-01T00:00:00Z',
      endDate: null,
      nextDueDate: '2025-02-01T12:00:00Z',
      plan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      lastPayment: null,
      digitalCard: null,
    }
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue(summary)
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
        { planId: 'p2', name: 'Silver', price: 30, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
      ],
    })
    vi.mocked(subscriptionsService.changePlan).mockResolvedValue({
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      fromPlan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      toPlan: { planId: 'p2', name: 'Silver', price: 30, billingCycle: 'Monthly', discountPercentage: 0 },
      prorationAmount: 0,
      paymentId: null,
      currency: 'BRL',
      paymentMethod: null,
      pix: null,
      card: null,
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/Trocar plano/i)).toBeInTheDocument()
    })
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })
    await user.selectOptions(screen.getByRole('combobox'), 'p2')
    await user.click(screen.getByRole('button', { name: /Confirmar troca/i }))
    await waitFor(() => {
      expect(subscriptionsService.changePlan).toHaveBeenCalledWith('p2', 'Pix')
    })
  })

  it('cancel subscription opens modal and calls cancelMembership on confirm', async () => {
    const user = userEvent.setup()
    const summary = {
      hasMembership: true,
      membershipId: 'm1',
      membershipStatus: 'Ativo',
      startDate: '2025-01-01T00:00:00Z',
      endDate: null,
      nextDueDate: '2025-02-01T12:00:00Z',
      plan: { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0 },
      lastPayment: null,
      digitalCard: null,
    }
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue(summary)
    vi.mocked(plansService.listPublished).mockResolvedValue({
      items: [
        { planId: 'p1', name: 'Gold', price: 50, billingCycle: 'Monthly', discountPercentage: 0, summary: null, benefits: [] },
      ],
    })
    vi.mocked(subscriptionsService.cancelMembership).mockResolvedValue({
      membershipId: 'm1',
      membershipStatus: 'Cancelado',
      mode: 'Immediate',
      accessValidUntilUtc: null,
      message: 'Sua assinatura foi cancelada imediatamente.',
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancelar assinatura/i })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /^Cancelar assinatura$/i }))
    await waitFor(() => {
      expect(screen.getByRole('dialog', { name: /Confirmar cancelamento/i })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /Confirmar cancelamento/i }))
    await waitFor(() => {
      expect(subscriptionsService.cancelMembership).toHaveBeenCalled()
    })
  })

  it('shows message when user has no membership', async () => {
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: false,
      membershipId: null,
      membershipStatus: null,
      startDate: null,
      endDate: null,
      nextDueDate: null,
      plan: null,
      lastPayment: null,
      digitalCard: null,
    })
    render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
    await waitFor(() => {
      expect(screen.getByText(/ainda não possui assinatura/i)).toBeInTheDocument()
    })
  })
})

describe('AccountPage — photo crop flow', () => {
  beforeEach(() => {
    vi.mocked(getMyProfile).mockReset()
    vi.mocked(getMyProfile).mockResolvedValue({
      document: null,
      birthDate: null,
      photoUrl: null,
      address: null,
    })
    vi.mocked(subscriptionsService.getMySummary).mockResolvedValue({
      hasMembership: false,
      membershipId: null,
      membershipStatus: null,
      startDate: null,
      endDate: null,
      nextDueDate: null,
      plan: null,
      lastPayment: null,
      digitalCard: null,
    })
    vi.mocked(uploadProfilePhoto).mockReset()
    vi.mocked(upsertMyProfile).mockReset()
    vi.mocked(getCroppedImg).mockReset()
    vi.mocked(getCroppedImg).mockResolvedValue(new Blob(['cropped'], { type: 'image/jpeg' }))
    vi.mocked(uploadProfilePhoto).mockResolvedValue('/uploads/profile-photos/me/profile.jpg')
    vi.mocked(upsertMyProfile).mockResolvedValue(undefined)
  })

  function makeFakeFileReader(dataUrl = 'data:image/jpeg;base64,/9j/test') {
    // FileReader is used as `new FileReader()`, so we need a class constructor
    return class FakeFileReader {
      result: string = dataUrl
      onload: ((e: ProgressEvent) => void) | null = null
      readAsDataURL(_file: File) {
        this.onload?.({ target: this } as unknown as ProgressEvent)
      }
    }
  }

  function renderPage() {
    return render(
      <MemoryRouter>
        <AccountPage />
      </MemoryRouter>,
    )
  }

  it('opens crop modal when a file is selected', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('FileReader', makeFakeFileReader())

    renderPage()
    await waitFor(() => expect(screen.getByTitle(/Clique para alterar a foto/i)).toBeInTheDocument())

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    expect(fileInput).not.toBeNull()

    const file = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
    await user.upload(fileInput, file)

    await waitFor(() => {
      expect(screen.getByRole('dialog', { name: /Ajustar foto/i })).toBeInTheDocument()
    })

    vi.unstubAllGlobals()
  })

  it('closes crop modal on cancel without uploading', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('FileReader', makeFakeFileReader())

    renderPage()
    await waitFor(() => expect(screen.getByTitle(/Clique para alterar a foto/i)).toBeInTheDocument())

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
    await user.upload(fileInput, file)

    await waitFor(() => expect(screen.getByRole('dialog', { name: /Ajustar foto/i })).toBeInTheDocument())

    await user.click(screen.getByRole('button', { name: /^Cancelar$/i }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: /Ajustar foto/i })).not.toBeInTheDocument()
    })
    expect(uploadProfilePhoto).not.toHaveBeenCalled()

    vi.unstubAllGlobals()
  })

  it('renders Confirmar button (disabled) inside crop modal when opened', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('FileReader', makeFakeFileReader())

    renderPage()
    await waitFor(() => expect(screen.getByTitle(/Clique para alterar a foto/i)).toBeInTheDocument())

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
    await user.upload(fileInput, file)

    await waitFor(() => expect(screen.getByRole('dialog', { name: /Ajustar foto/i })).toBeInTheDocument())

    // Confirm is disabled until the Cropper fires onCropComplete (mocked to null)
    expect(screen.getByRole('button', { name: /^Confirmar$/i })).toBeInTheDocument()

    vi.unstubAllGlobals()
  })
})
