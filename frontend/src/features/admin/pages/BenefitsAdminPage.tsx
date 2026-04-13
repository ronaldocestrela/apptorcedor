import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  createBenefitOffer,
  createBenefitPartner,
  listBenefitOffers,
  listBenefitPartners,
  listBenefitRedemptions,
  redeemBenefitOffer,
  updateBenefitPartner,
} from '../services/adminApi'

export function BenefitsAdminPage() {
  const [partners, setPartners] = useState<Awaited<ReturnType<typeof listBenefitPartners>>['items']>([])
  const [offers, setOffers] = useState<Awaited<ReturnType<typeof listBenefitOffers>>['items']>([])
  const [redemptions, setRedemptions] = useState<Awaited<ReturnType<typeof listBenefitRedemptions>>['items']>([])
  const [error, setError] = useState<string | null>(null)
  const [partnerName, setPartnerName] = useState('')
  const [partnerDesc, setPartnerDesc] = useState('')
  const [selectedPartnerId, setSelectedPartnerId] = useState<string | null>(null)
  const [offerTitle, setOfferTitle] = useState('')
  const [offerPartnerId, setOfferPartnerId] = useState('')
  const [redeemOfferId, setRedeemOfferId] = useState('')
  const [redeemUserId, setRedeemUserId] = useState('')

  const load = useCallback(async () => {
    setError(null)
    try {
      const [p, o, r] = await Promise.all([
        listBenefitPartners({ pageSize: 100 }),
        listBenefitOffers({ pageSize: 100 }),
        listBenefitRedemptions({ pageSize: 50 }),
      ])
      setPartners(p.items)
      setOffers(o.items)
      setRedemptions(r.items)
    } catch {
      setError('Falha ao carregar benefícios.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onCreatePartner(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await createBenefitPartner({ name: partnerName.trim(), description: partnerDesc.trim() || null, isActive: true })
      setPartnerName('')
      setPartnerDesc('')
      await load()
    } catch {
      setError('Falha ao criar parceiro.')
    }
  }

  async function onCreateOffer(e: FormEvent) {
    e.preventDefault()
    setError(null)
    const start = new Date()
    const end = new Date()
    end.setMonth(end.getMonth() + 3)
    try {
      await createBenefitOffer({
        partnerId: offerPartnerId,
        title: offerTitle.trim(),
        description: null,
        isActive: true,
        startAt: start.toISOString(),
        endAt: end.toISOString(),
        eligiblePlanIds: null,
        eligibleMembershipStatuses: null,
      })
      setOfferTitle('')
      await load()
    } catch {
      setError('Falha ao criar oferta.')
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.BeneficiosVisualizar, ApplicationPermissions.BeneficiosGerenciar]}>
      <h1>Benefícios e parceiros</h1>
      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}

      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Parceiros</h2>
        <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
          <form onSubmit={(e) => void onCreatePartner(e)} style={{ marginBottom: 12 }}>
            <input placeholder="Nome" value={partnerName} onChange={(e) => setPartnerName(e.target.value)} required />
            <input placeholder="Descrição" value={partnerDesc} onChange={(e) => setPartnerDesc(e.target.value)} style={{ marginLeft: 8 }} />
            <button type="submit" style={{ marginLeft: 8 }}>Criar</button>
          </form>
        </PermissionGate>
        <ul>
          {partners.map((p) => (
            <li key={p.partnerId}>
              {p.name}
              {selectedPartnerId === p.partnerId ? ' (selecionado)' : null}
              <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
                <button type="button" style={{ marginLeft: 8 }} onClick={() => { setSelectedPartnerId(p.partnerId); setOfferPartnerId(p.partnerId) }}>Usar em oferta</button>
                <button
                  type="button"
                  style={{ marginLeft: 8 }}
                  onClick={() => void updateBenefitPartner(p.partnerId, { name: p.name, description: null, isActive: !p.isActive }).then(() => load())}
                >
                  Toggle ativo
                </button>
              </PermissionGate>
            </li>
          ))}
        </ul>
      </section>

      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Ofertas</h2>
        <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
          <form onSubmit={(e) => void onCreateOffer(e)} style={{ marginBottom: 12 }}>
            <input placeholder="PartnerId GUID" value={offerPartnerId} onChange={(e) => setOfferPartnerId(e.target.value)} required style={{ minWidth: 280 }} />
            <input placeholder="Título" value={offerTitle} onChange={(e) => setOfferTitle(e.target.value)} required style={{ marginLeft: 8 }} />
            <button type="submit" style={{ marginLeft: 8 }}>Criar oferta (vigência 3 meses)</button>
          </form>
        </PermissionGate>
        <ul>
          {offers.map((o) => (
            <li key={o.offerId}>{o.title} — {o.partnerName}</li>
          ))}
        </ul>
      </section>

      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Resgate administrativo</h2>
        <PermissionGate anyOf={[ApplicationPermissions.BeneficiosGerenciar]}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
            <input placeholder="OfferId" value={redeemOfferId} onChange={(e) => setRedeemOfferId(e.target.value)} style={{ minWidth: 260 }} />
            <input placeholder="UserId" value={redeemUserId} onChange={(e) => setRedeemUserId(e.target.value)} style={{ minWidth: 260 }} />
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

      <section>
        <h2 style={{ fontSize: '1rem' }}>Últimos resgates</h2>
        <ul>
          {redemptions.map((r) => (
            <li key={r.redemptionId}>{r.offerTitle} — {r.userEmail} — {r.createdAt}</li>
          ))}
        </ul>
      </section>
    </PermissionGate>
  )
}
