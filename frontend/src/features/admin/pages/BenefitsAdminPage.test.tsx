import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { BenefitsAdminPage } from './BenefitsAdminPage'

const listBenefitPartners = vi.fn()
const listBenefitOffers = vi.fn()
const listBenefitRedemptions = vi.fn()
const listAdminPlans = vi.fn()
const createBenefitOffer = vi.fn()
const getBenefitOffer = vi.fn()
const updateBenefitOffer = vi.fn()
const createBenefitPartner = vi.fn()
const getBenefitPartner = vi.fn()
const updateBenefitPartner = vi.fn()
const redeemBenefitOffer = vi.fn()
const uploadBenefitOfferBanner = vi.fn()
const deleteBenefitOfferBanner = vi.fn()
const approveBenefitRedemption = vi.fn()
const rejectBenefitRedemption = vi.fn()
const replaceBenefitShirtCatalog = vi.fn()

vi.mock('../../auth/PermissionGate', () => ({
  PermissionGate: ({ children }: { children: ReactNode }) => <>{children}</>,
}))

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      permissions: [
        ApplicationPermissions.BeneficiosGerenciar,
        ApplicationPermissions.PlanosVisualizar,
      ],
    },
  }),
}))

vi.mock('../services/adminApi', () => ({
  listBenefitPartners: (...a: unknown[]) => listBenefitPartners(...a),
  listBenefitOffers: (...a: unknown[]) => listBenefitOffers(...a),
  listBenefitRedemptions: (...a: unknown[]) => listBenefitRedemptions(...a),
  listAdminPlans: (...a: unknown[]) => listAdminPlans(...a),
  createBenefitOffer: (...a: unknown[]) => createBenefitOffer(...a),
  getBenefitOffer: (...a: unknown[]) => getBenefitOffer(...a),
  updateBenefitOffer: (...a: unknown[]) => updateBenefitOffer(...a),
  createBenefitPartner: (...a: unknown[]) => createBenefitPartner(...a),
  getBenefitPartner: (...a: unknown[]) => getBenefitPartner(...a),
  updateBenefitPartner: (...a: unknown[]) => updateBenefitPartner(...a),
  redeemBenefitOffer: (...a: unknown[]) => redeemBenefitOffer(...a),
  uploadBenefitOfferBanner: (...a: unknown[]) => uploadBenefitOfferBanner(...a),
  deleteBenefitOfferBanner: (...a: unknown[]) => deleteBenefitOfferBanner(...a),
  approveBenefitRedemption: (...a: unknown[]) => approveBenefitRedemption(...a),
  rejectBenefitRedemption: (...a: unknown[]) => rejectBenefitRedemption(...a),
  replaceBenefitShirtCatalog: (...a: unknown[]) => replaceBenefitShirtCatalog(...a),
}))

vi.mock('../../account/accountApi', () => ({
  resolvePublicAssetUrl: (path: string | null | undefined) =>
    path ? `https://resolved.test${path.startsWith('/') ? path : `/${path}`}` : undefined,
}))

const partnerA = { partnerId: '11111111-1111-1111-1111-111111111111', name: 'Parceiro A', isActive: true, createdAt: '' }

const mockPlanRow = (planId: string, name: string) => ({
  planId,
  name,
  price: 10,
  billingCycle: 'Monthly',
  discountPercentage: 0,
  isActive: true,
  isPublished: true,
  publishedAt: null as string | null,
  benefitCount: 0,
})

function setupDefaultListMocks() {
  listBenefitPartners.mockResolvedValue({ totalCount: 1, items: [partnerA] })
  listBenefitOffers.mockResolvedValue({ totalCount: 0, items: [] })
  listAdminPlans.mockResolvedValue({ totalCount: 0, items: [] })
  listBenefitRedemptions.mockImplementation((params: { status?: string }) => {
    if (params?.status === 'pending')
      return Promise.resolve({ totalCount: 0, items: [] })
    return Promise.resolve({ totalCount: 0, items: [] })
  })
}

function renderPage() {
  return render(
    <MemoryRouter>
      <BenefitsAdminPage />
    </MemoryRouter>,
  )
}

describe('BenefitsAdminPage', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  beforeEach(() => {
    vi.clearAllMocks()
    setupDefaultListMocks()
  })

  it('renders offer rows with derived status badges', async () => {
    vi.setSystemTime(new Date('2025-06-15T12:00:00.000Z'))
    listBenefitOffers.mockResolvedValue({
      totalCount: 4,
      items: [
        {
          offerId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Inativa',
          isActive: false,
          startAt: '2025-01-01T00:00:00.000Z',
          endAt: '2025-12-31T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
        {
          offerId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Programada',
          isActive: true,
          startAt: '2025-07-01T00:00:00.000Z',
          endAt: '2025-12-31T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
        {
          offerId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Vigente',
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2025-06-30T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
        {
          offerId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Expirada',
          isActive: true,
          startAt: '2025-01-01T00:00:00.000Z',
          endAt: '2025-05-01T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
      ],
    })

    renderPage()

    await waitFor(() => {
      expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument()
    })

    expect(screen.getByTestId('offer-badge-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')).toHaveTextContent('Inativa')
    expect(screen.getByTestId('offer-badge-bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')).toHaveTextContent('Programada')
    expect(screen.getByTestId('offer-badge-cccccccc-cccc-cccc-cccc-cccccccccccc')).toHaveTextContent('Vigente')
    expect(screen.getByTestId('offer-badge-dddddddd-dddd-dddd-dddd-dddddddddddd')).toHaveTextContent('Expirada')
    vi.useRealTimers()
  })

  it('submits create with startAt, endAt and isActive true', async () => {
    const user = userEvent.setup()
    listBenefitOffers.mockResolvedValue({ totalCount: 0, items: [] })
    createBenefitOffer.mockResolvedValue({ offerId: 'new-offer-id' })

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    await user.selectOptions(screen.getByTestId('offer-partner-select'), partnerA.partnerId)
    await user.type(screen.getByTestId('offer-title-input'), 'Nova promo')

    const startInput = screen.getByTestId('offer-start-input') as HTMLInputElement
    const endInput = screen.getByTestId('offer-end-input') as HTMLInputElement
    await user.clear(startInput)
    await user.type(startInput, '2026-01-10T08:00')
    await user.clear(endInput)
    await user.type(endInput, '2026-06-10T08:00')

    await user.click(screen.getByTestId('offer-submit'))

    await waitFor(() => {
      expect(createBenefitOffer).toHaveBeenCalled()
    })

    const call = createBenefitOffer.mock.calls[0][0]
    expect(call.partnerId).toBe(partnerA.partnerId)
    expect(call.title).toBe('Nova promo')
    expect(call.isActive).toBe(true)
    expect(call.startAt).toMatch(/2026-01-10/)
    expect(call.endAt).toMatch(/2026-06-10/)
    expect(call.eligiblePlanIds).toBeNull()
  })

  it('submits create with eligible plans selected from checkboxes', async () => {
    const user = userEvent.setup()
    const p1 = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    const p2 = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
    listBenefitOffers.mockResolvedValue({ totalCount: 0, items: [] })
    listAdminPlans.mockResolvedValue({
      totalCount: 2,
      items: [mockPlanRow(p1, 'Básico'), mockPlanRow(p2, 'Premium')],
    })
    createBenefitOffer.mockResolvedValue({ offerId: 'new-offer-id' })

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    await user.selectOptions(screen.getByTestId('offer-partner-select'), partnerA.partnerId)
    await user.type(screen.getByTestId('offer-title-input'), 'Só alguns planos')

    const startInput = screen.getByTestId('offer-start-input') as HTMLInputElement
    const endInput = screen.getByTestId('offer-end-input') as HTMLInputElement
    await user.clear(startInput)
    await user.type(startInput, '2026-01-10T08:00')
    await user.clear(endInput)
    await user.type(endInput, '2026-06-10T08:00')

    await user.click(screen.getByTestId(`offer-plan-${p1}`))
    await user.click(screen.getByTestId(`offer-plan-${p2}`))

    await user.click(screen.getByTestId('offer-submit'))

    await waitFor(() => {
      expect(createBenefitOffer).toHaveBeenCalled()
    })

    const call = createBenefitOffer.mock.calls[0][0]
    expect(call.eligiblePlanIds).toEqual(expect.arrayContaining([p1, p2]))
    expect(call.eligiblePlanIds).toHaveLength(2)
  })

  it('on create with banner file uploads banner after offer is created', async () => {
    const user = userEvent.setup()
    listBenefitOffers.mockResolvedValue({ totalCount: 0, items: [] })
    createBenefitOffer.mockResolvedValue({ offerId: 'new-offer-id' })
    uploadBenefitOfferBanner.mockResolvedValue({ bannerUrl: '/uploads/benefit-offer-banners/b.png' })

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    await user.selectOptions(screen.getByTestId('offer-partner-select'), partnerA.partnerId)
    await user.type(screen.getByTestId('offer-title-input'), 'Com banner create')

    const startInput = screen.getByTestId('offer-start-input') as HTMLInputElement
    const endInput = screen.getByTestId('offer-end-input') as HTMLInputElement
    await user.clear(startInput)
    await user.type(startInput, '2026-01-10T08:00')
    await user.clear(endInput)
    await user.type(endInput, '2026-06-10T08:00')

    const file = new File(['x'], 'ban.png', { type: 'image/png' })
    await user.upload(screen.getByTestId('offer-banner-input'), file)
    expect(await screen.findByTestId('offer-banner-preview')).toBeInTheDocument()

    await user.click(screen.getByTestId('offer-submit'))

    await waitFor(() => {
      expect(createBenefitOffer).toHaveBeenCalled()
      expect(uploadBenefitOfferBanner).toHaveBeenCalledWith('new-offer-id', expect.any(File))
    })
    expect(uploadBenefitOfferBanner.mock.invocationCallOrder[0] > createBenefitOffer.mock.invocationCallOrder[0]).toBe(
      true,
    )
  })

  it('shows validation error when end is before start', async () => {
    const user = userEvent.setup()
    listBenefitOffers.mockResolvedValue({ totalCount: 0, items: [] })

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    await user.selectOptions(screen.getByTestId('offer-partner-select'), partnerA.partnerId)
    await user.type(screen.getByTestId('offer-title-input'), 'X')

    const startInput = screen.getByTestId('offer-start-input') as HTMLInputElement
    const endInput = screen.getByTestId('offer-end-input') as HTMLInputElement
    await user.clear(startInput)
    await user.type(startInput, '2026-06-10T08:00')
    await user.clear(endInput)
    await user.type(endInput, '2026-01-10T08:00')

    await user.click(screen.getByTestId('offer-submit'))

    expect(createBenefitOffer).not.toHaveBeenCalled()
    expect(screen.getByTestId('benefits-admin-form-error')).toHaveTextContent(
      /data final deve ser maior ou igual/i,
    )
  })

  it('Desativar calls updateBenefitOffer with isActive false preserving eligibilities', async () => {
    const user = userEvent.setup()
    const offerId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    listBenefitOffers.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          offerId,
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Para desativar',
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2026-06-30T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
      ],
    })

    listAdminPlans.mockResolvedValue({
      totalCount: 1,
      items: [mockPlanRow('plan-1', 'Plano Um')],
    })

    getBenefitOffer.mockResolvedValue({
      offerId,
      partnerId: partnerA.partnerId,
      title: 'Para desativar',
      description: null,
      isActive: true,
      startAt: '2025-06-01T00:00:00.000Z',
      endAt: '2026-06-30T00:00:00.000Z',
      createdAt: '',
      updatedAt: '',
      eligiblePlanIds: ['plan-1'],
      eligibleMembershipStatuses: ['Ativo'],
      bannerUrl: null,
      isShirtCustomizationOffer: false,
      shirtSizes: [],
      shirtModels: [],
    })
    updateBenefitOffer.mockResolvedValue(undefined)

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    await user.click(screen.getByTestId(`offer-deactivate-${offerId}`))

    await waitFor(() => {
      expect(updateBenefitOffer).toHaveBeenCalledWith(
        offerId,
        expect.objectContaining({
          isActive: false,
          isShirtCustomizationOffer: false,
          eligiblePlanIds: ['plan-1'],
          eligibleMembershipStatuses: ['Ativo'],
        }),
      )
    })
  })

  it('Edit loads getBenefitOffer and PUT preserves eligibilities', async () => {
    const user = userEvent.setup()
    const offerId = 'ffffffff-ffff-ffff-ffff-ffffffffffff'
    listBenefitOffers.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          offerId,
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Edit me',
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2026-06-30T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
      ],
    })

    listAdminPlans.mockResolvedValue({
      totalCount: 1,
      items: [mockPlanRow('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Plano Elegível')],
    })

    const detail = {
      offerId,
      partnerId: partnerA.partnerId,
      title: 'Edit me',
      description: 'd',
      isActive: true,
      startAt: '2025-06-01T00:00:00.000Z',
      endAt: '2026-06-30T00:00:00.000Z',
      createdAt: '',
      updatedAt: '',
      eligiblePlanIds: ['aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'],
      eligibleMembershipStatuses: ['Ativo', 'PendingPayment'],
      bannerUrl: null,
      isShirtCustomizationOffer: false,
      shirtSizes: [],
      shirtModels: [],
    }
    getBenefitOffer.mockResolvedValue(detail)
    updateBenefitOffer.mockResolvedValue(undefined)

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    const row = screen.getByTestId(`offer-row-${offerId}`)
    await user.click(within(row).getByRole('button', { name: 'Editar' }))

    await waitFor(() => {
      expect(getBenefitOffer).toHaveBeenCalledWith(offerId)
    })

    await screen.findByText('Editar oferta')

    await user.clear(screen.getByTestId('offer-title-input'))
    await user.type(screen.getByTestId('offer-title-input'), 'Updated title')
    await user.click(screen.getByTestId('offer-submit'))

    await waitFor(() => {
      expect(updateBenefitOffer).toHaveBeenCalled()
    })

    expect(updateBenefitOffer).toHaveBeenCalledWith(
      offerId,
      expect.objectContaining({
        title: 'Updated title',
        isShirtCustomizationOffer: false,
        eligiblePlanIds: ['aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'],
        eligibleMembershipStatuses: expect.arrayContaining(['Ativo', 'PendingPayment']),
      }),
    )
  })

  it('when editing offer with bannerUrl shows preview and calls deleteBenefitOfferBanner on remove', async () => {
    const user = userEvent.setup()
    const offerId = '11111111-2222-3333-4444-555555555555'
    listBenefitOffers.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          offerId,
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Com banner',
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2026-06-30T00:00:00.000Z',
          createdAt: '',
          bannerUrl: '/uploads/benefit-offer-banners/x.webp',
        },
      ],
    })

    getBenefitOffer.mockResolvedValue({
      offerId,
      partnerId: partnerA.partnerId,
      title: 'Com banner',
      description: 'Detalhe',
      isActive: true,
      startAt: '2025-06-01T00:00:00.000Z',
      endAt: '2026-06-30T00:00:00.000Z',
      createdAt: '',
      updatedAt: '',
      eligiblePlanIds: [],
      eligibleMembershipStatuses: ['Ativo'],
      bannerUrl: '/uploads/benefit-offer-banners/x.webp',
      isShirtCustomizationOffer: false,
      shirtSizes: [],
      shirtModels: [],
    })
    deleteBenefitOfferBanner.mockResolvedValue(undefined)

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    const row = screen.getByTestId(`offer-row-${offerId}`)
    await user.click(within(row).getByRole('button', { name: 'Editar' }))

    await waitFor(() => {
      expect(getBenefitOffer).toHaveBeenCalledWith(offerId)
    })

    expect(await screen.findByTestId('offer-banner-preview')).toHaveAttribute(
      'src',
      'https://resolved.test/uploads/benefit-offer-banners/x.webp',
    )

    await user.click(screen.getByTestId('offer-banner-remove'))

    await waitFor(() => {
      expect(deleteBenefitOfferBanner).toHaveBeenCalledWith(offerId)
    })
    expect(screen.queryByTestId('offer-banner-preview')).not.toBeInTheDocument()
  })

  it('saves shirt catalog when editing shirt offer', async () => {
    const user = userEvent.setup()
    const offerId = 'aaaaaaaa-0000-0000-0000-aaaaaaaaaaaa'
    listBenefitOffers.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          offerId,
          partnerId: partnerA.partnerId,
          partnerName: 'Parceiro A',
          title: 'Camisa',
          isActive: true,
          startAt: '2025-06-01T00:00:00.000Z',
          endAt: '2026-06-30T00:00:00.000Z',
          createdAt: '',
          bannerUrl: null,
        },
      ],
    })

    getBenefitOffer.mockResolvedValue({
      offerId,
      partnerId: partnerA.partnerId,
      title: 'Camisa',
      description: null,
      isActive: true,
      startAt: '2025-06-01T00:00:00.000Z',
      endAt: '2026-06-30T00:00:00.000Z',
      createdAt: '',
      updatedAt: '',
      eligiblePlanIds: [],
      eligibleMembershipStatuses: [],
      bannerUrl: null,
      isShirtCustomizationOffer: true,
      shirtSizes: [],
      shirtModels: [],
    })
    replaceBenefitShirtCatalog.mockResolvedValue(undefined)

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    const row = screen.getByTestId(`offer-row-${offerId}`)
    await user.click(within(row).getByRole('button', { name: 'Editar' }))

    await waitFor(() => expect(screen.getByTestId('offer-shirt-catalog-block')).toBeInTheDocument())

    await user.type(screen.getByTestId('offer-shirt-sizes'), 'M\nG')
    await user.type(screen.getByTestId('offer-shirt-models'), 'Home')
    await user.click(screen.getByTestId('offer-shirt-catalog-save'))

    await waitFor(() => {
      expect(replaceBenefitShirtCatalog).toHaveBeenCalledWith(offerId, { sizes: ['M', 'G'], models: ['Home'] })
    })
  })

  it('shows approved shirt redemptions with personalization and delivery', async () => {
    const approvedId = 'cccccccc-0000-0000-0000-cccccccccccc'
    listBenefitRedemptions.mockImplementation((params: { status?: string }) => {
      if (params?.status === 'approved') {
        return Promise.resolve({
          totalCount: 1,
          items: [
            {
              redemptionId: approvedId,
              offerId: 'o1',
              offerTitle: 'Camisa aprovada',
              userId: 'u1',
              userEmail: 'fan@test',
              actorUserId: null,
              notes: null,
              createdAt: '2026-02-01',
              status: 'Approved',
              shirtSize: 'G',
              shirtModel: 'Third',
              shirtNumber: '7',
              shirtDisplayName: 'Silva',
              deliveryCep: '20000000',
              deliveryNeighborhood: 'Centro',
              deliveryStreet: 'Av B',
              deliveryNumber: '99',
              deliveryCity: 'Rio',
              deliveryState: 'RJ',
              shippingMethod: null,
              shippingCarrierId: null,
              shippingCarrierName: null,
              shippingServiceName: null,
              shippingPrice: null,
              shippingDeliveryDays: null,
              reviewedAtUtc: '2026-02-02T00:00:00.000Z',
              reviewedByUserId: 'admin-1',
              rejectionReason: null,
            },
          ],
        })
      }
      return Promise.resolve({ totalCount: 0, items: [] })
    })

    renderPage()

    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())

    expect(screen.getByTestId('benefits-approved-shirts-list')).toBeInTheDocument()
    expect(screen.getByTestId(`approved-shirt-${approvedId}`)).toHaveTextContent('Camisa aprovada')
    expect(screen.getByTestId(`approved-shirt-detail-${approvedId}`)).toHaveTextContent('Silva')
    expect(screen.getByTestId(`approved-shirt-delivery-${approvedId}`)).toHaveTextContent('20000000')
    expect(screen.getByTestId(`approved-shirt-delivery-${approvedId}`)).toHaveTextContent('Av B')
  })

  it('shows empty state for approved shirts when none', async () => {
    renderPage()
    await waitFor(() => expect(screen.queryByTestId('benefits-admin-loading')).not.toBeInTheDocument())
    expect(screen.getByTestId('benefits-approved-shirts-empty')).toHaveTextContent(/Nenhuma camisa aprovada/i)
  })

  it('approves pending shirt redemption from queue', async () => {
    const user = userEvent.setup()
    const rid = 'bbbbbbbb-0000-0000-0000-bbbbbbbbbbbb'
    listBenefitRedemptions.mockImplementation((params: { status?: string }) => {
      if (params?.status === 'pending') {
        return Promise.resolve({
          totalCount: 1,
          items: [
            {
              redemptionId: rid,
              offerId: 'o1',
              offerTitle: 'Camisa',
              userId: 'u1',
              userEmail: 'm@test',
              actorUserId: null,
              notes: null,
              createdAt: '2026-01-01',
              status: 'Pending',
              shirtSize: 'M',
              shirtModel: 'Home',
              shirtNumber: '10',
              shirtDisplayName: 'Fulano',
              deliveryCep: '01310100',
              deliveryNeighborhood: 'Centro',
              deliveryStreet: 'Rua A',
              deliveryNumber: '10',
              deliveryCity: 'São Paulo',
              deliveryState: 'SP',
              shippingMethod: null,
              shippingCarrierId: null,
              shippingCarrierName: null,
              shippingServiceName: null,
              shippingPrice: null,
              shippingDeliveryDays: null,
              reviewedAtUtc: null,
              reviewedByUserId: null,
              rejectionReason: null,
            },
          ],
        })
      }
      return Promise.resolve({ totalCount: 0, items: [] })
    })
    approveBenefitRedemption.mockResolvedValue(undefined)

    renderPage()

    await waitFor(() => expect(screen.getByTestId(`pending-redemption-${rid}`)).toBeInTheDocument())
    expect(screen.getByTestId(`pending-delivery-${rid}`)).toHaveTextContent('01310100')
    expect(screen.getByTestId('pending-delivery-line')).toHaveTextContent('Rua A')
    await user.click(screen.getByTestId(`approve-redemption-${rid}`))
    await waitFor(() => expect(approveBenefitRedemption).toHaveBeenCalledWith(rid))
  })
})
