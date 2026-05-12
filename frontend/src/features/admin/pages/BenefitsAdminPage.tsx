import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import { resolvePublicAssetUrl } from '../../account/accountApi'
import {
  approveBenefitRedemption,
  createBenefitOffer,
  createBenefitPartner,
  deleteBenefitOfferBanner,
  getBenefitOffer,
  getBenefitPartner,
  listBenefitOffers,
  listBenefitPartners,
  listBenefitRedemptions,
  listAdminPlans,
  redeemBenefitOffer,
  rejectBenefitRedemption,
  replaceBenefitShirtCatalog,
  updateBenefitOffer,
  updateBenefitPartner,
  uploadBenefitOfferBanner,
  type AdminPlanListItem,
  type BenefitOfferDetail,
  type BenefitOfferListItem,
  type BenefitRedemptionListItem,
} from '../services/adminApi'
import {
  deriveOfferUiStatus,
  datetimeLocalValueToIso,
  isoToDatetimeLocalValue,
  MEMBERSHIP_STATUS_OPTIONS,
  type OfferUiStatus,
} from './benefitsAdminHelpers'

function defaultOfferEndLocal(): string {
  const end = new Date()
  end.setMonth(end.getMonth() + 3)
  return isoToDatetimeLocalValue(end.toISOString())
}

function defaultOfferStartLocal(): string {
  return isoToDatetimeLocalValue(new Date().toISOString())
}

type PartnerFormState = {
  partnerId: string | null
  name: string
  description: string
  isActive: boolean
}

type OfferFormState = {
  offerId: string | null
  partnerId: string
  title: string
  description: string
  startAtLocal: string
  endAtLocal: string
  isActive: boolean
  eligiblePlanIds: Set<string>
  eligibleMembershipStatuses: Set<string>
  bannerUrl: string | null
  isShirtCustomizationOffer: boolean
  shirtSizesCatalogRaw: string
  shirtModelsCatalogRaw: string
}

function emptyPartnerForm(): PartnerFormState {
  return { partnerId: null, name: '', description: '', isActive: true }
}

function emptyOfferForm(partnerId = ''): OfferFormState {
  return {
    offerId: null,
    partnerId,
    title: '',
    description: '',
    startAtLocal: defaultOfferStartLocal(),
    endAtLocal: defaultOfferEndLocal(),
    isActive: true,
    eligiblePlanIds: new Set(),
    eligibleMembershipStatuses: new Set(),
    bannerUrl: null,
    isShirtCustomizationOffer: false,
    shirtSizesCatalogRaw: '',
    shirtModelsCatalogRaw: '',
  }
}

function detailToOfferForm(d: BenefitOfferDetail): OfferFormState {
  return {
    offerId: d.offerId,
    partnerId: d.partnerId ?? '',
    title: d.title ?? '',
    description: d.description ?? '',
    startAtLocal: isoToDatetimeLocalValue(d.startAt),
    endAtLocal: isoToDatetimeLocalValue(d.endAt),
    isActive: d.isActive,
    eligiblePlanIds: new Set(d.eligiblePlanIds ?? []),
    eligibleMembershipStatuses: new Set(d.eligibleMembershipStatuses ?? []),
    bannerUrl: d.bannerUrl,
    isShirtCustomizationOffer: d.isShirtCustomizationOffer,
    shirtSizesCatalogRaw: d.shirtSizes?.length ? d.shirtSizes.join('\n') : '',
    shirtModelsCatalogRaw: d.shirtModels?.length ? d.shirtModels.join('\n') : '',
  }
}

function isApprovedShirtRedemption(item: BenefitRedemptionListItem): boolean {
  return item.status === 'Approved'
    && Boolean((item.shirtSize?.trim() ?? '') || (item.shirtModel?.trim() ?? ''))
}

function redemptionShippingAdminLine(r: BenefitRedemptionListItem): string | null {
  const m = r.shippingMethod?.trim().toLowerCase()
  if (m === 'pickup')
    return 'Entrega: retirada na loja'
  if (m === 'carrier') {
    const svc = [r.shippingServiceName, r.shippingCarrierName].filter(Boolean).join(' — ')
    const price =
      r.shippingPrice != null
        ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(r.shippingPrice)
        : ''
    const d = r.shippingDeliveryDays != null ? `até ${r.shippingDeliveryDays} dia(s) útil(eis)` : ''
    const paid =
      r.shippingPaidAtUtc
        ? ` — Frete pago em ${new Date(r.shippingPaidAtUtc).toLocaleString('pt-BR')}`
        : ' — Frete ainda não pago'
    return ['Frete:', svc, price, d, paid].filter(Boolean).join(' ')
  }
  return null
}

export function BenefitsAdminPage() {
  const { user } = useAuth()
  const canManage = hasPermission(user, ApplicationPermissions.BeneficiosGerenciar)

  const [partners, setPartners] = useState<Awaited<ReturnType<typeof listBenefitPartners>>['items']>([])
  const [offers, setOffers] = useState<BenefitOfferListItem[]>([])
  const [redemptions, setRedemptions] = useState<Awaited<ReturnType<typeof listBenefitRedemptions>>['items']>([])
  const [pendingRedemptions, setPendingRedemptions] = useState<BenefitRedemptionListItem[]>([])
  const [approvedShirtRedemptions, setApprovedShirtRedemptions] = useState<BenefitRedemptionListItem[]>([])
  const [cancelledRedemptions, setCancelledRedemptions] = useState<BenefitRedemptionListItem[]>([])
  const [redemptionStatusFilter, setRedemptionStatusFilter] = useState<'' | 'pending' | 'approved' | 'rejected' | 'cancelled_by_user'>('')
  /** Planos ativos para multiseleção de elegibilidade (`GET /api/admin/plans?isActive=true`; requer `Planos.Visualizar`). */
  const [activePlansForOffers, setActivePlansForOffers] = useState<AdminPlanListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const [partnerSearchInput, setPartnerSearchInput] = useState('')
  const [partnerSearchApplied, setPartnerSearchApplied] = useState('')
  const [partnerActiveFilter, setPartnerActiveFilter] = useState<'all' | 'true' | 'false'>('all')

  const [offerPartnerFilter, setOfferPartnerFilter] = useState('')
  const [offerStatusFilter, setOfferStatusFilter] = useState<OfferUiStatus | ''>('')

  const [partnerForm, setPartnerForm] = useState<PartnerFormState>(() => emptyPartnerForm())
  const [savingPartner, setSavingPartner] = useState(false)

  const [offerForm, setOfferForm] = useState<OfferFormState>(() => emptyOfferForm())
  /** Banner escolhido antes da oferta existir (create); enviado após POST retornar offerId */
  const [pendingBanner, setPendingBanner] = useState<{ file: File; objectUrl: string } | null>(null)
  const [savingOffer, setSavingOffer] = useState(false)

  function clearPendingBanner() {
    setPendingBanner((prev) => {
      if (prev) URL.revokeObjectURL(prev.objectUrl)
      return null
    })
  }

  const [redeemOfferId, setRedeemOfferId] = useState('')
  const [redeemUserId, setRedeemUserId] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [p, o, r, pend, appr, canc] = await Promise.all([
        listBenefitPartners({
          search: partnerSearchApplied.trim() || undefined,
          isActive:
            partnerActiveFilter === 'all' ? undefined : partnerActiveFilter === 'true',
          pageSize: 100,
        }),
        listBenefitOffers({ pageSize: 200 }),
        listBenefitRedemptions({ pageSize: 50 }),
        listBenefitRedemptions({ status: 'pending', pageSize: 100 }),
        listBenefitRedemptions({ status: 'approved', pageSize: 200 }),
        listBenefitRedemptions({ status: 'cancelled_by_user', pageSize: 100 }),
      ])
      setPartners(p.items)
      setOffers(o.items)
      setRedemptions(r.items)
      setPendingRedemptions(pend.items)
      setApprovedShirtRedemptions(appr.items.filter(isApprovedShirtRedemption))
      setCancelledRedemptions(canc.items)
      try {
        const pl = await listAdminPlans({ isActive: true, pageSize: 200 })
        setActivePlansForOffers(pl.items)
      } catch {
        setActivePlansForOffers([])
      }
    } catch {
      setError('Falha ao carregar benefícios.')
    } finally {
      setLoading(false)
    }
  }, [partnerSearchApplied, partnerActiveFilter])

  useEffect(() => {
    void load()
  }, [load])

  const filteredOffers = useMemo(() => {
    let rows = offers
    if (offerPartnerFilter.trim()) {
      rows = rows.filter((o) => o.partnerId === offerPartnerFilter.trim())
    }
    if (offerStatusFilter) {
      rows = rows.filter((o) => deriveOfferUiStatus(o) === offerStatusFilter)
    }
    return rows
  }, [offers, offerPartnerFilter, offerStatusFilter])

  const sortedActivePlansForOffers = useMemo(
    () =>
      [...activePlansForOffers].sort((a, b) =>
        (a.name ?? '').localeCompare(b.name ?? '', 'pt-BR'),
      ),
    [activePlansForOffers],
  )

  const orphanEligiblePlanIds = useMemo(() => {
    const known = new Set(activePlansForOffers.map((x) => x.planId))
    const ids = offerForm.eligiblePlanIds ?? new Set<string>()
    return [...ids].filter((id) => !known.has(id)).sort()
  }, [activePlansForOffers, offerForm.eligiblePlanIds])

  async function onPartnerSubmit(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    if (!partnerForm.name.trim()) {
      setFormError('Nome do parceiro é obrigatório.')
      return
    }
    setSavingPartner(true)
    try {
      const body = {
        name: partnerForm.name.trim(),
        description: partnerForm.description.trim() || null,
        isActive: partnerForm.isActive,
      }
      if (partnerForm.partnerId) {
        await updateBenefitPartner(partnerForm.partnerId, body)
      } else {
        await createBenefitPartner(body)
      }
      setPartnerForm(emptyPartnerForm())
      await load()
    } catch {
      setFormError('Falha ao salvar parceiro.')
    } finally {
      setSavingPartner(false)
    }
  }

  function startEditPartner(partnerId: string) {
    setFormError(null)
    void (async () => {
      try {
        const d = await getBenefitPartner(partnerId)
        setPartnerForm({
          partnerId: d.partnerId,
          name: d.name,
          description: d.description ?? '',
          isActive: d.isActive,
        })
      } catch {
        setFormError('Falha ao carregar parceiro.')
      }
    })()
  }

  function togglePartnerActive(partnerId: string) {
    setFormError(null)
    void (async () => {
      try {
        const d = await getBenefitPartner(partnerId)
        await updateBenefitPartner(partnerId, {
          name: d.name,
          description: d.description,
          isActive: !d.isActive,
        })
        await load()
      } catch {
        setFormError('Falha ao atualizar parceiro.')
      }
    })()
  }

  async function onOfferSubmit(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    const startIso = datetimeLocalValueToIso(offerForm.startAtLocal)
    const endIso = datetimeLocalValueToIso(offerForm.endAtLocal)
    if (!offerForm.partnerId.trim()) {
      setFormError('Selecione ou informe o parceiro.')
      return
    }
    if (!offerForm.title.trim()) {
      setFormError('Título da oferta é obrigatório.')
      return
    }
    if (!startIso || !endIso) {
      setFormError('Datas de início e fim são obrigatórias e devem ser válidas.')
      return
    }
    if (new Date(endIso).getTime() < new Date(startIso).getTime()) {
      setFormError('A data final deve ser maior ou igual à data inicial.')
      return
    }

    const statuses =
      offerForm.eligibleMembershipStatuses.size === 0
        ? null
        : [...offerForm.eligibleMembershipStatuses]
    const eligiblePlanIds =
      (offerForm.eligiblePlanIds?.size ?? 0) === 0 ? null : [...(offerForm.eligiblePlanIds ?? new Set<string>())]

    const body = {
      partnerId: offerForm.partnerId.trim(),
      title: offerForm.title.trim(),
      description: offerForm.description.trim() || null,
      isActive: offerForm.isActive,
      startAt: startIso,
      endAt: endIso,
      eligiblePlanIds,
      eligibleMembershipStatuses: statuses,
      isShirtCustomizationOffer: offerForm.isShirtCustomizationOffer,
    }

    setSavingOffer(true)
    try {
      if (offerForm.offerId) {
        await updateBenefitOffer(offerForm.offerId, body)
        setOfferForm(emptyOfferForm(offerForm.partnerId))
        await load()
      } else {
        const bannerFile = pendingBanner?.file
        const { offerId } = await createBenefitOffer(body)
        if (pendingBanner) clearPendingBanner()
        if (bannerFile) {
          try {
            await uploadBenefitOfferBanner(offerId, bannerFile)
          } catch {
            setFormError(
              'Oferta criada, mas o banner não foi enviado. Use “Editar” nesta oferta para enviar ou remover a imagem.',
            )
            try {
              const d = await getBenefitOffer(offerId)
              setOfferForm(detailToOfferForm(d))
            } catch {
              setOfferForm(emptyOfferForm(offerForm.partnerId))
            }
            await load()
            return
          }
        }
        setOfferForm(emptyOfferForm(offerForm.partnerId))
        await load()
      }
    } catch {
      setFormError('Falha ao salvar oferta.')
    } finally {
      setSavingOffer(false)
    }
  }

  function startEditOffer(offerId: string) {
    setFormError(null)
    clearPendingBanner()
    void (async () => {
      try {
        const d = await getBenefitOffer(offerId)
        setOfferForm(detailToOfferForm(d))
      } catch {
        setFormError('Falha ao carregar oferta.')
      }
    })()
  }

  function parseCatalogLines(raw: string): string[] {
    return raw
      .split(/[\n,]+/)
      .map((s) => s.trim())
      .filter(Boolean)
  }

  async function onSaveShirtCatalog() {
    if (!offerForm.offerId || !canManage)
      return
    setFormError(null)
    try {
      const sizes = parseCatalogLines(offerForm.shirtSizesCatalogRaw)
      const models = parseCatalogLines(offerForm.shirtModelsCatalogRaw)
      await replaceBenefitShirtCatalog(offerForm.offerId, { sizes, models })
      const d = await getBenefitOffer(offerForm.offerId)
      setOfferForm(detailToOfferForm(d))
      await load()
    } catch {
      setFormError('Falha ao salvar catálogo de camisa.')
    }
  }

  async function onApproveRedemption(redemptionId: string) {
    setError(null)
    try {
      await approveBenefitRedemption(redemptionId)
      await load()
    } catch {
      setError('Falha ao aprovar solicitação.')
    }
  }

  async function onRejectRedemption(redemptionId: string) {
    const reason = window.prompt('Motivo da recusa (opcional):') ?? undefined
    setError(null)
    try {
      await rejectBenefitRedemption(redemptionId, { reason: reason?.trim() || null })
      await load()
    } catch {
      setError('Falha ao recusar solicitação.')
    }
  }

  function cancelOfferForm() {
    setFormError(null)
    clearPendingBanner()
    setOfferForm(emptyOfferForm())
  }

  async function setOfferActiveFlag(offerId: string, isActive: boolean) {
    setFormError(null)
    try {
      const d = await getBenefitOffer(offerId)
      await updateBenefitOffer(offerId, {
        partnerId: d.partnerId,
        title: d.title,
        description: d.description,
        isActive,
        startAt: d.startAt,
        endAt: d.endAt,
        eligiblePlanIds: d.eligiblePlanIds.length ? d.eligiblePlanIds : null,
        eligibleMembershipStatuses: d.eligibleMembershipStatuses.length ? d.eligibleMembershipStatuses : null,
        isShirtCustomizationOffer: d.isShirtCustomizationOffer,
      })
      await load()
      if (offerForm.offerId === offerId) {
        setOfferForm(detailToOfferForm({ ...d, isActive }))
      }
    } catch {
      setFormError('Falha ao atualizar status da oferta.')
    }
  }

  async function softDeleteOffer(offerId: string) {
    const ok = window.confirm('Desativar esta oferta? (soft delete — histórico de resgates permanece.)')
    if (!ok) return
    await setOfferActiveFlag(offerId, false)
  }

  async function onBannerFileSelected(file: File) {
    setFormError(null)
    if (!offerForm.offerId) {
      clearPendingBanner()
      const objectUrl = URL.createObjectURL(file)
      setPendingBanner({ file, objectUrl })
      return
    }
    try {
      const { bannerUrl } = await uploadBenefitOfferBanner(offerForm.offerId, file)
      setOfferForm((f) => ({ ...f, bannerUrl }))
      await load()
    } catch {
      setFormError('Falha ao enviar banner.')
    }
  }

  async function removeOfferBanner() {
    setFormError(null)
    if (!offerForm.offerId) {
      clearPendingBanner()
      return
    }
    try {
      await deleteBenefitOfferBanner(offerForm.offerId)
      setOfferForm((f) => ({ ...f, bannerUrl: null }))
      await load()
    } catch {
      setFormError('Falha ao remover banner.')
    }
  }

  function toggleMembershipStatus(st: string) {
    setOfferForm((prev) => {
      const next = new Set(prev.eligibleMembershipStatuses)
      if (next.has(st)) next.delete(st)
      else next.add(st)
      return { ...prev, eligibleMembershipStatuses: next }
    })
  }

  function toggleEligiblePlan(planId: string) {
    setOfferForm((prev) => {
      const current = prev.eligiblePlanIds ?? new Set<string>()
      const next = new Set(current)
      if (next.has(planId)) next.delete(planId)
      else next.add(planId)
      return { ...prev, eligiblePlanIds: next }
    })
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.BeneficiosVisualizar, ApplicationPermissions.BeneficiosGerenciar]}>
      <div className="benefits-admin" data-testid="benefits-admin-root">
        <h1>Benefícios e parceiros</h1>
        {loading ? <p data-testid="benefits-admin-loading">Carregando…</p> : null}
        {error ? (
          <p className="benefits-admin__error" style={{ color: 'crimson' }} data-testid="benefits-admin-error">
            {error}
          </p>
        ) : null}
        {formError ? (
          <p className="benefits-admin__form-error" style={{ color: 'crimson' }} data-testid="benefits-admin-form-error">
            {formError}
          </p>
        ) : null}

        <section className="benefits-admin__section" style={{ marginBottom: '2rem' }}>
          <h2 style={{ fontSize: '1.1rem' }}>Parceiros</h2>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 12, alignItems: 'center' }}>
            <input
              placeholder="Filtrar nome"
              value={partnerSearchInput}
              onChange={(e) => setPartnerSearchInput(e.target.value)}
              aria-label="Filtrar parceiros"
            />
            <select
              value={partnerActiveFilter}
              onChange={(e) => setPartnerActiveFilter(e.target.value as 'all' | 'true' | 'false')}
              aria-label="Filtro ativo parceiro"
            >
              <option value="all">Todos</option>
              <option value="true">Ativos</option>
              <option value="false">Inativos</option>
            </select>
            <button type="button" onClick={() => setPartnerSearchApplied(partnerSearchInput)}>
              Aplicar filtro
            </button>
          </div>
          <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
            <form onSubmit={(e) => void onPartnerSubmit(e)} style={{ marginBottom: 16, maxWidth: 560 }}>
              <fieldset style={{ border: '1px solid var(--admin-border, #333)', padding: 12 }}>
                <legend>{partnerForm.partnerId ? `Editar parceiro` : 'Novo parceiro'}</legend>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <input
                    placeholder="Nome"
                    value={partnerForm.name}
                    onChange={(e) => setPartnerForm((p) => ({ ...p, name: e.target.value }))}
                    required
                    data-testid="partner-name-input"
                  />
                  <textarea
                    placeholder="Descrição"
                    value={partnerForm.description}
                    onChange={(e) => setPartnerForm((p) => ({ ...p, description: e.target.value }))}
                    rows={2}
                  />
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <input
                      type="checkbox"
                      checked={partnerForm.isActive}
                      onChange={(e) => setPartnerForm((p) => ({ ...p, isActive: e.target.checked }))}
                      data-testid="partner-active-checkbox"
                    />
                    Ativo
                  </label>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button type="submit" disabled={savingPartner || !canManage}>
                      {partnerForm.partnerId ? 'Salvar' : 'Criar'}
                    </button>
                    {partnerForm.partnerId ? (
                      <button type="button" onClick={() => setPartnerForm(emptyPartnerForm())}>
                        Cancelar edição
                      </button>
                    ) : null}
                  </div>
                </div>
              </fieldset>
            </form>
          </PermissionGate>
          <ul data-testid="benefits-partners-list">
            {partners.map((p) => (
              <li key={p.partnerId} data-testid={`partner-row-${p.partnerId}`}>
                <strong>{p.name}</strong>
                {p.isActive ? ' · ativo' : ' · inativo'}
                {canManage ? (
                  <>
                    <button type="button" style={{ marginLeft: 8 }} onClick={() => startEditPartner(p.partnerId)}>
                      Editar
                    </button>
                    <button type="button" style={{ marginLeft: 8 }} onClick={() => togglePartnerActive(p.partnerId)}>
                      Ativar/desativar
                    </button>
                  </>
                ) : null}
              </li>
            ))}
          </ul>
        </section>

        <section className="benefits-admin__section" style={{ marginBottom: '2rem' }}>
          <h2 style={{ fontSize: '1.1rem' }}>Ofertas (benefícios)</h2>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 12, alignItems: 'center' }}>
            <select
              value={offerPartnerFilter}
              onChange={(e) => setOfferPartnerFilter(e.target.value)}
              aria-label="Filtrar por parceiro"
              data-testid="offer-filter-partner"
            >
              <option value="">Todos os parceiros</option>
              {partners.map((p) => (
                <option key={p.partnerId} value={p.partnerId}>
                  {p.name}
                </option>
              ))}
            </select>
            <select
              value={offerStatusFilter}
              onChange={(e) => setOfferStatusFilter(e.target.value as OfferUiStatus | '')}
              aria-label="Filtrar por status da oferta"
              data-testid="offer-filter-status"
            >
              <option value="">Todos os status</option>
              <option value="Vigente">Vigente</option>
              <option value="Programada">Programada</option>
              <option value="Expirada">Expirada</option>
              <option value="Inativa">Inativa</option>
            </select>
          </div>

          <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
            <form onSubmit={(e) => void onOfferSubmit(e)} style={{ marginBottom: 16, maxWidth: 640 }}>
              <fieldset style={{ border: '1px solid var(--admin-border, #333)', padding: 12 }}>
                <legend>{offerForm.offerId ? 'Editar oferta' : 'Nova oferta'}</legend>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <label>
                    Parceiro
                    <select
                      value={offerForm.partnerId}
                      onChange={(e) => setOfferForm((f) => ({ ...f, partnerId: e.target.value }))}
                      required
                      data-testid="offer-partner-select"
                    >
                      <option value="">Selecione…</option>
                      {partners.map((p) => (
                        <option key={p.partnerId} value={p.partnerId}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <input
                    placeholder="Título"
                    value={offerForm.title}
                    onChange={(e) => setOfferForm((f) => ({ ...f, title: e.target.value }))}
                    required
                    data-testid="offer-title-input"
                  />
                  <textarea
                    placeholder="Descrição"
                    value={offerForm.description}
                    onChange={(e) => setOfferForm((f) => ({ ...f, description: e.target.value }))}
                    rows={2}
                    data-testid="offer-description-input"
                  />
                  <label>
                    Início da vigência
                    <input
                      type="datetime-local"
                      value={offerForm.startAtLocal}
                      onChange={(e) => setOfferForm((f) => ({ ...f, startAtLocal: e.target.value }))}
                      required
                      data-testid="offer-start-input"
                    />
                  </label>
                  <label>
                    Fim da vigência
                    <input
                      type="datetime-local"
                      value={offerForm.endAtLocal}
                      onChange={(e) => setOfferForm((f) => ({ ...f, endAtLocal: e.target.value }))}
                      required
                      data-testid="offer-end-input"
                    />
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <input
                      type="checkbox"
                      checked={offerForm.isActive}
                      onChange={(e) => setOfferForm((f) => ({ ...f, isActive: e.target.checked }))}
                      data-testid="offer-active-checkbox"
                    />
                    Oferta ativa
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <input
                      type="checkbox"
                      checked={offerForm.isShirtCustomizationOffer}
                      onChange={(e) =>
                        setOfferForm((f) => ({ ...f, isShirtCustomizationOffer: e.target.checked }))}
                      data-testid="offer-shirt-checkbox"
                    />
                    Oferta de camisa personalizada (torcedor envia dados; aprovação manual)
                  </label>
                  <div data-testid="offer-plans-field">
                    <span>Planos elegíveis (vazio = todos)</span>
                    {sortedActivePlansForOffers.length === 0 ? (
                      <p style={{ margin: '6px 0 0', fontSize: '0.85rem', opacity: 0.9 }} data-testid="offer-plans-empty-hint">
                        Nenhum plano ativo listado. Cadastre planos ativos ou confirme a permissão Planos.Visualizar.
                      </p>
                    ) : (
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 6 }}>
                        {sortedActivePlansForOffers.map((pl) => (
                          <label
                            key={pl.planId}
                            style={{ display: 'flex', alignItems: 'center', gap: 8 }}
                          >
                            <input
                              type="checkbox"
                              checked={(offerForm.eligiblePlanIds ?? new Set<string>()).has(pl.planId)}
                              onChange={() => toggleEligiblePlan(pl.planId)}
                              data-testid={`offer-plan-${pl.planId}`}
                            />
                            <span>{pl.name}</span>
                          </label>
                        ))}
                      </div>
                    )}
                    {orphanEligiblePlanIds.length > 0 ? (
                      <div style={{ marginTop: 10, fontSize: '0.85rem', opacity: 0.95 }} data-testid="offer-plans-orphans">
                        <span>Planos elegíveis ainda associados, mas fora da lista de ativos (remova se não fizer mais sentido):</span>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 6 }}>
                          {orphanEligiblePlanIds.map((pid) => (
                            <label key={pid} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                              <input
                                type="checkbox"
                                checked
                                onChange={() => toggleEligiblePlan(pid)}
                                data-testid={`offer-plan-orphan-${pid}`}
                              />
                              <span>{pid}</span>
                            </label>
                          ))}
                        </div>
                      </div>
                    ) : null}
                  </div>
                  <div>
                    <span>Status de membership elegíveis (vazio = todos)</span>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 4 }}>
                      {MEMBERSHIP_STATUS_OPTIONS.map((st) => (
                        <label key={st} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                          <input
                            type="checkbox"
                            checked={offerForm.eligibleMembershipStatuses.has(st)}
                            onChange={() => toggleMembershipStatus(st)}
                            data-testid={`offer-status-${st}`}
                          />
                          {st}
                        </label>
                      ))}
                    </div>
                  </div>
                  {offerForm.offerId && offerForm.isShirtCustomizationOffer && canManage ? (
                    <div
                      style={{
                        borderTop: '1px solid rgba(255,255,255,0.12)',
                        paddingTop: 10,
                        display: 'flex',
                        flexDirection: 'column',
                        gap: 8,
                      }}
                      data-testid="offer-shirt-catalog-block"
                    >
                      <p style={{ margin: 0, fontSize: '0.88rem' }}>
                        Catálogo de camisa (valores exatos que o torcedor poderá escolher)
                      </p>
                      <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: '0.85rem' }}>
                        Tamanhos (um por linha ou vírgula)
                        <textarea
                          value={offerForm.shirtSizesCatalogRaw}
                          onChange={(e) =>
                            setOfferForm((f) => ({ ...f, shirtSizesCatalogRaw: e.target.value }))}
                          rows={3}
                          data-testid="offer-shirt-sizes"
                        />
                      </label>
                      <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: '0.85rem' }}>
                        Modelos (um por linha ou vírgula)
                        <textarea
                          value={offerForm.shirtModelsCatalogRaw}
                          onChange={(e) =>
                            setOfferForm((f) => ({ ...f, shirtModelsCatalogRaw: e.target.value }))}
                          rows={3}
                          data-testid="offer-shirt-models"
                        />
                      </label>
                      <button
                        type="button"
                        data-testid="offer-shirt-catalog-save"
                        onClick={() => void onSaveShirtCatalog()}
                      >
                        Salvar catálogo
                      </button>
                    </div>
                  ) : null}
                  {canManage ? (
                    <div style={{ borderTop: '1px solid rgba(255,255,255,0.12)', paddingTop: 10 }}>
                      <p style={{ margin: '0 0 8px', fontSize: '0.88rem' }}>
                        Banner da oferta (proporção recomendada 300×148 px; JPG, PNG ou WebP
                        {offerForm.offerId ? '' : '; será enviado ao criar a oferta'})
                      </p>
                      {offerForm.bannerUrl ? (
                        <img
                          src={resolvePublicAssetUrl(offerForm.bannerUrl) ?? ''}
                          alt=""
                          data-testid="offer-banner-preview"
                          style={{ maxWidth: 280, borderRadius: 8, display: 'block', marginBottom: 8 }}
                        />
                      ) : pendingBanner ? (
                        <img
                          src={pendingBanner.objectUrl}
                          alt=""
                          data-testid="offer-banner-preview"
                          style={{ maxWidth: 280, borderRadius: 8, display: 'block', marginBottom: 8 }}
                        />
                      ) : null}
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
                        <label style={{ fontSize: '0.85rem' }}>
                          Enviar imagem
                          <input
                            type="file"
                            accept="image/jpeg,image/png,image/webp"
                            data-testid="offer-banner-input"
                            style={{ display: 'block', marginTop: 4 }}
                            onChange={(e) => {
                              const f = e.target.files?.[0]
                              if (f) void onBannerFileSelected(f)
                              e.target.value = ''
                            }}
                          />
                        </label>
                        {offerForm.bannerUrl || pendingBanner ? (
                          <button type="button" data-testid="offer-banner-remove" onClick={() => void removeOfferBanner()}>
                            Remover banner
                          </button>
                        ) : null}
                      </div>
                    </div>
                  ) : null}
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button type="submit" disabled={savingOffer || !canManage} data-testid="offer-submit">
                      {offerForm.offerId ? 'Salvar oferta' : 'Criar oferta'}
                    </button>
                    {offerForm.offerId ? (
                      <button type="button" onClick={cancelOfferForm}>
                        Cancelar
                      </button>
                    ) : null}
                  </div>
                </div>
              </fieldset>
            </form>
          </PermissionGate>

          <ul data-testid="benefits-offers-list">
            {filteredOffers.map((o) => {
              const badge = deriveOfferUiStatus(o)
              return (
                <li key={o.offerId} data-testid={`offer-row-${o.offerId}`}>
                  <span data-testid={`offer-badge-${o.offerId}`}>{badge}</span> — <strong>{o.title}</strong> — {o.partnerName}
                  <span style={{ marginLeft: 8, opacity: 0.85 }}>
                    {o.startAt} → {o.endAt}
                  </span>
                  {canManage ? (
                    <>
                      <button type="button" style={{ marginLeft: 8 }} onClick={() => startEditOffer(o.offerId)}>
                        Editar
                      </button>
                      {o.isActive ? (
                        <button
                          type="button"
                          style={{ marginLeft: 8 }}
                          data-testid={`offer-deactivate-${o.offerId}`}
                          onClick={() => void setOfferActiveFlag(o.offerId, false)}
                        >
                          Desativar
                        </button>
                      ) : (
                        <button type="button" style={{ marginLeft: 8 }} onClick={() => void setOfferActiveFlag(o.offerId, true)}>
                          Ativar
                        </button>
                      )}
                      <button type="button" style={{ marginLeft: 8 }} onClick={() => void softDeleteOffer(o.offerId)}>
                        Excluir (soft)
                      </button>
                    </>
                  ) : null}
                </li>
              )
            })}
          </ul>
        </section>

        <section className="benefits-admin__section" style={{ marginBottom: '2rem' }}>
          <h2 style={{ fontSize: '1rem' }}>Resgate administrativo</h2>
          <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
              <input
                placeholder="OfferId"
                value={redeemOfferId}
                onChange={(e) => setRedeemOfferId(e.target.value)}
                style={{ minWidth: 260 }}
              />
              <input
                placeholder="UserId"
                value={redeemUserId}
                onChange={(e) => setRedeemUserId(e.target.value)}
                style={{ minWidth: 260 }}
              />
              <button
                type="button"
                onClick={() => void (async () => {
                  try {
                    await redeemBenefitOffer(redeemOfferId.trim(), { userId: redeemUserId.trim(), notes: 'admin' })
                    await load()
                  } catch {
                    setError('Falha no resgate.')
                  }
                })()}
              >
                Resgatar
              </button>
            </div>
          </PermissionGate>
        </section>

        <section data-testid="benefits-pending-section">
          <h2 style={{ fontSize: '1rem' }}>Solicitações de camisa pendentes</h2>
          {pendingRedemptions.length === 0 ? (
            <p data-testid="benefits-pending-empty">Nenhuma solicitação pendente.</p>
          ) : (
            <ul data-testid="benefits-pending-redemptions">
              {pendingRedemptions.map((pr) => (
                <li key={pr.redemptionId} data-testid={`pending-redemption-${pr.redemptionId}`}>
                  <strong>{pr.offerTitle}</strong>
                  {' '}
                  —
                  {' '}
                  {pr.userEmail}
                  {' '}
                  — tam.
                  {pr.shirtSize}
                  {' '}
                  / mod.
                  {pr.shirtModel}
                  {' '}
                  — #
                  {pr.shirtNumber}
                  {' '}
                  {pr.shirtDisplayName}
                  {redemptionShippingAdminLine(pr) ? (
                    <div style={{ marginTop: 6, fontSize: '0.85rem', opacity: 0.95 }} data-testid={`pending-shipping-${pr.redemptionId}`}>
                      {redemptionShippingAdminLine(pr)}
                    </div>
                  ) : null}
                  {pr.deliveryStreet || pr.deliveryCep ? (
                    <div style={{ marginTop: 6, fontSize: '0.85rem', opacity: 0.95 }} data-testid={`pending-delivery-${pr.redemptionId}`}>
                      <div data-testid="pending-delivery-line">
                        Entrega: CEP
                        {' '}
                        {pr.deliveryCep ?? '—'}
                        {' '}
                        —
                        {' '}
                        {pr.deliveryStreet ?? ''}
                        {', '}
                        {pr.deliveryNumber ?? ''}
                        {' '}
                        —
                        {' '}
                        {pr.deliveryNeighborhood ?? ''}
                        {' '}
                        —
                        {' '}
                        {pr.deliveryCity ?? ''}
                        /
                        {pr.deliveryState ?? ''}
                      </div>
                    </div>
                  ) : null}
                  {canManage ? (
                    <span style={{ marginLeft: 8 }}>
                      <button
                        type="button"
                        data-testid={`approve-redemption-${pr.redemptionId}`}
                        onClick={() => void onApproveRedemption(pr.redemptionId)}
                      >
                        Aprovar
                      </button>
                      <button
                        type="button"
                        style={{ marginLeft: 8 }}
                        data-testid={`reject-redemption-${pr.redemptionId}`}
                        onClick={() => void onRejectRedemption(pr.redemptionId)}
                      >
                        Recusar
                      </button>
                    </span>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </section>

        <section data-testid="benefits-approved-shirts-section">
          <h2 style={{ fontSize: '1rem' }}>Camisas aprovadas</h2>
          {approvedShirtRedemptions.length === 0 ? (
            <p data-testid="benefits-approved-shirts-empty">Nenhuma camisa aprovada recentemente.</p>
          ) : (
            <ul data-testid="benefits-approved-shirts-list">
              {approvedShirtRedemptions.map((ar) => (
                <li key={ar.redemptionId} data-testid={`approved-shirt-${ar.redemptionId}`}>
                  <strong>{ar.offerTitle}</strong>
                  {' '}
                  —
                  {' '}
                  {ar.userEmail}
                  {' '}
                  —
                  {' '}
                  {ar.createdAt}
                  <div style={{ marginTop: 6, fontSize: '0.88rem' }} data-testid={`approved-shirt-detail-${ar.redemptionId}`}>
                    <div>
                      Personalização: tam.
                      {' '}
                      {ar.shirtSize}
                      {' '}
                      / mod.
                      {' '}
                      {ar.shirtModel}
                      {' '}
                      — #
                      {ar.shirtNumber}
                      {' '}
                      “
                      {ar.shirtDisplayName}
                      ”
                    </div>
                    {redemptionShippingAdminLine(ar) ? (
                      <div style={{ marginTop: 4, opacity: 0.95 }} data-testid={`approved-shirt-shipping-${ar.redemptionId}`}>
                        {redemptionShippingAdminLine(ar)}
                      </div>
                    ) : null}
                    {ar.deliveryCep || ar.deliveryStreet ? (
                      <div style={{ marginTop: 4, opacity: 0.95 }} data-testid={`approved-shirt-delivery-${ar.redemptionId}`}>
                        Entrega: CEP
                        {' '}
                        {ar.deliveryCep ?? '—'}
                        {' '}
                        —
                        {' '}
                        {ar.deliveryStreet ?? ''}
                        {', '}
                        {ar.deliveryNumber ?? ''}
                        {' '}
                        —
                        {' '}
                        {ar.deliveryNeighborhood ?? ''}
                        {' '}
                        —
                        {' '}
                        {ar.deliveryCity ?? ''}
                        /
                        {ar.deliveryState ?? ''}
                      </div>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section>
          <h2 style={{ fontSize: '1rem' }}>Últimos resgates</h2>
          <div style={{ marginBottom: '0.75rem' }}>
            <label style={{ fontSize: '0.85rem', marginRight: '0.5rem', color: '#a6b0bf' }}>Filtrar por status:</label>
            <select
              value={redemptionStatusFilter}
              onChange={(e) => setRedemptionStatusFilter(e.target.value as typeof redemptionStatusFilter)}
              style={{ fontSize: '0.85rem', background: '#1a2332', color: '#f5f7fa', border: '1px solid rgba(255,255,255,0.12)', borderRadius: 8, padding: '0.3rem 0.6rem' }}
            >
              <option value="">Todos</option>
              <option value="pending">Pendente</option>
              <option value="approved">Aprovado</option>
              <option value="rejected">Recusado</option>
              <option value="cancelled_by_user">Cancelado pelo torcedor</option>
            </select>
          </div>
          <ul>
            {(redemptionStatusFilter === 'cancelled_by_user'
              ? cancelledRedemptions
              : redemptionStatusFilter
                ? redemptions.filter((r) => {
                    const map: Record<string, string> = { pending: 'Pending', approved: 'Approved', rejected: 'Rejected' }
                    return r.status === (map[redemptionStatusFilter] ?? redemptionStatusFilter)
                  })
                : redemptions
            ).map((r) => (
              <li key={r.redemptionId}>
                {r.offerTitle}
                {' '}—{' '}
                {r.userEmail}
                {' '}—{' '}
                <span
                  style={{
                    display: 'inline-block',
                    padding: '1px 8px',
                    borderRadius: 8,
                    fontSize: '0.78rem',
                    fontWeight: 600,
                    background:
                      r.status === 'Pending' ? 'rgba(255,193,7,0.15)'
                      : r.status === 'Approved' ? 'rgba(140,211,146,0.15)'
                      : r.status === 'Rejected' ? 'rgba(255,112,112,0.15)'
                      : r.status === 'CancelledByUser' ? 'rgba(166,176,191,0.18)'
                      : 'rgba(166,176,191,0.12)',
                    color:
                      r.status === 'Pending' ? '#ffc107'
                      : r.status === 'Approved' ? '#8cd392'
                      : r.status === 'Rejected' ? '#ff7070'
                      : '#a6b0bf',
                  }}
                >
                  {r.status === 'Pending' ? 'Pendente'
                    : r.status === 'Approved' ? 'Aprovado'
                    : r.status === 'Rejected' ? 'Recusado'
                    : r.status === 'CancelledByUser' ? 'Cancelado pelo torcedor'
                    : String(r.status)}
                </span>
                {' '}—{' '}
                {r.createdAt}
              </li>
            ))}
          </ul>
        </section>
      </div>
    </PermissionGate>
  )
}
