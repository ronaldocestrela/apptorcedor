import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Gift } from 'lucide-react'
import {
  getEligibleBenefitOfferDetail,
  redeemBenefitOffer,
  type TorcedorEligibleBenefitOfferDetail,
} from '../features/torcedor/torcedorBenefitsApi'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function isNotFoundError(e: unknown): boolean {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const r = (e as { response?: { status?: number } }).response
    return r?.status === 404
  }
  return false
}

function redeemErrorMessage(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const data = (e as { response?: { data?: { error?: string } } }).response?.data
    if (data?.error === 'already_redeemed')
      return 'Você já resgatou este benefício.'
    if (data?.error === 'not_eligible')
      return 'Você não está elegível para este benefício no momento.'
  }
  return 'Não foi possível concluir o resgate. Tente novamente.'
}

export function BenefitOfferDetailPage() {
  const { offerId } = useParams<{ offerId: string }>()
  const [detail, setDetail] = useState<TorcedorEligibleBenefitOfferDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [redeeming, setRedeeming] = useState(false)
  const [redeemError, setRedeemError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!offerId) {
      setNotFound(true)
      setLoading(false)
      return
    }
    try {
      setLoading(true)
      setError(null)
      setNotFound(false)
      const d = await getEligibleBenefitOfferDetail(offerId)
      setDetail(d)
    }
    catch (e) {
      if (isNotFoundError(e)) {
        setNotFound(true)
        setDetail(null)
      }
      else {
        setError(e instanceof Error ? e.message : 'Erro ao carregar benefício')
      }
    }
    finally {
      setLoading(false)
    }
  }, [offerId])

  useEffect(() => {
    void load()
  }, [load])

  async function handleRedeem() {
    if (!offerId || !detail || detail.alreadyRedeemed)
      return
    try {
      setRedeeming(true)
      setRedeemError(null)
      await redeemBenefitOffer(offerId)
      await load()
    }
    catch (e) {
      setRedeemError(redeemErrorMessage(e))
    }
    finally {
      setRedeeming(false)
    }
  }

  return (
    <div className="benefit-detail-root">
      <header className="subpage-header">
        <Link to="/benefits" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Detalhe do benefício</h1>
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p role="alert" className="benefit-detail-error">
            {error}
          </p>
        ) : null}
        {notFound ? (
          <p className="benefits-empty">Benefício não encontrado ou não disponível para você.</p>
        ) : null}
        {!loading && detail ? (
          <article className="benefit-detail-card">
            <div className="benefit-detail-card__icon-wrap" aria-hidden>
              <Gift size={28} />
            </div>
            <h2 className="benefit-detail-card__title">{detail.title}</h2>
            <span className="benefit-detail-card__partner">{detail.partnerName}</span>
            {detail.description ? (
              <p className="benefit-detail-card__description">{detail.description}</p>
            ) : null}
            <p className="benefit-detail-card__dates">
              Válido de
              {' '}
              {new Date(detail.startAt).toLocaleDateString('pt-BR')}
              {' '}
              até
              {' '}
              {new Date(detail.endAt).toLocaleDateString('pt-BR')}
            </p>
            {detail.alreadyRedeemed && detail.redemptionDateUtc ? (
              <p className="benefit-detail-redeemed-badge">
                Já resgatado em
                {' '}
                {new Date(detail.redemptionDateUtc).toLocaleString('pt-BR')}
              </p>
            ) : null}
            {redeemError ? (
              <p role="alert" className="benefit-detail-error">
                {redeemError}
              </p>
            ) : null}
            {!detail.alreadyRedeemed ? (
              <button
                type="button"
                className="btn-primary benefit-detail-redeem-btn"
                disabled={redeeming}
                onClick={() => void handleRedeem()}
              >
                {redeeming ? 'Resgatando…' : 'Resgatar benefício'}
              </button>
            ) : null}
          </article>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
