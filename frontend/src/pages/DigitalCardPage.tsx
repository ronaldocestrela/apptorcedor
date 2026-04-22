import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Settings } from 'lucide-react'
import {
  getMyDigitalCardWithSource,
  type MyDigitalCardView,
  type MyDigitalCardViewState,
} from '../features/torcedor/torcedorDigitalCardApi'
import { getMyProfile, resolvePublicAssetUrl } from '../features/account/accountApi'
import { useAuth } from '../features/auth/AuthContext'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function stateLabel(state: MyDigitalCardViewState): string {
  switch (state) {
    case 'NotAssociated':
      return 'Sem associação ativa'
    case 'MembershipInactive':
      return 'Associação não elegível'
    case 'AwaitingIssuance':
      return 'Aguardando emissão'
    case 'Active':
      return 'Carteirinha ativa'
    default:
      return state
  }
}

function initialsFromName(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0)
    return 'FS'
  if (parts.length === 1)
    return parts[0]!.slice(0, 2).toUpperCase()
  return (parts[0]![0] + parts[parts.length - 1]![0]).toUpperCase()
}

function planNameFromLines(lines: string[] | null | undefined): string {
  const raw = lines?.find(l => l.startsWith('Plano: '))
  if (!raw)
    return 'Sem plano'
  const v = raw.replace(/^Plano:\s*/, '').trim()
  return v || 'Sem plano'
}

function holderFromLines(lines: string[] | null | undefined): string | null {
  const raw = lines?.find(l => l.startsWith('Titular: '))
  if (!raw)
    return null
  return raw.replace(/^Titular:\s*/, '').trim() || null
}

function membershipStatusPt(status: string): string {
  switch (status) {
    case 'Ativo':
      return 'Ativo'
    case 'PendingPayment':
      return 'Aguardando pagamento'
    case 'Cancelado':
      return 'Cancelado'
    case 'Inadimplente':
      return 'Inadimplente'
    case 'Suspenso':
      return 'Suspenso'
    case 'NaoAssociado':
      return 'Não associado'
    default:
      return status
  }
}

function formatIngressDate(iso: string | null): string {
  if (!iso)
    return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime()))
    return '—'
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function tenureLabel(iso: string | null): string {
  if (!iso)
    return 'Sócio há —'
  const start = new Date(iso)
  if (Number.isNaN(start.getTime()))
    return 'Sócio há —'
  const now = new Date()
  const months = Math.floor((now.getTime() - start.getTime()) / (30.44 * 24 * 60 * 60 * 1000))
  if (months < 1)
    return 'Sócio há menos de um mês.'
  if (months < 12) {
    return `Sócio há ${months} ${months === 1 ? 'mês' : 'meses'}.`
  }
  const years = Math.floor(months / 12)
  return `Sócio há ${years} ${years === 1 ? 'ano' : 'anos'}.`
}

export function DigitalCardPage() {
  const { user } = useAuth()
  const [data, setData] = useState<MyDigitalCardView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fromCache, setFromCache] = useState(false)
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        setFromCache(false)
        const { data: view, fromCache } = await getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })
        if (!cancelled) {
          setData(view)
          setFromCache(fromCache)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar carteirinha')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    document.title = 'Carteirinha Digital | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  useEffect(() => {
    if (!user)
      return
    let cancelled = false
    void (async () => {
      try {
        const p = await getMyProfile()
        if (!cancelled)
          setPhotoUrl(p.photoUrl)
      }
      catch {
        /* foto opcional */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [user])

  const displayName = user?.name?.trim() || holderFromLines(data?.templatePreviewLines ?? null) || '—'
  const holderOnCard = holderFromLines(data?.templatePreviewLines ?? null) ?? displayName
  const planLabel = planNameFromLines(data?.templatePreviewLines ?? null)
  const photoSrc = photoUrl ? resolvePublicAssetUrl(photoUrl) : undefined
  const avatarInitials = initialsFromName(displayName === '—' ? 'Sócio' : displayName)

  return (
    <div className="digital-card-root">
      <div className="plans-figma-starfield" aria-hidden="true" />
      <header className="digital-card-page__header">
        <div className="digital-card-page__user">
          <div className="digital-card-page__avatar">
            {photoSrc ? (
              <img src={photoSrc} alt="" className="digital-card-page__avatar-img" />
            ) : (
              <span className="digital-card-page__avatar-fallback">{avatarInitials}</span>
            )}
          </div>
          <p className="digital-card-page__name">{displayName}</p>
        </div>
        <Link to="/account" className="digital-card-page__settings" aria-label="Configurações">
          <Settings size={22} strokeWidth={2} />
        </Link>
      </header>

      <main className="subpage-content digital-card-page__main">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" className="digital-card-page__error">{error}</p> : null}

        {!loading && !error && data?.state === 'Active' ? (
          <>
            <section className="digital-card-figma" aria-label="Carteirinha digital">
              {/*
                Card 375×703 px — posições absolutas extraídas do Figma (node 374:242).
                Referência: card centralizado na tela 440×956, top-left do card = (33, 127).
                Todos os elementos abaixo usam coords relativas ao card.
              */}
              <div className="digital-card-figma__card">
                {/* BG: dentro do card, top=3, 375×700 */}
                <img
                  className="digital-card-figma__bg"
                  src="/carteirinha-bg.png"
                  alt=""
                />

                {/* Facts — left:60 top:71  w:60 h:217 → ul rotate(90deg) width:217 */}
                <div className="digital-card-figma__facts-wrap">
                  <ul className="digital-card-figma__facts">
                    <li>{tenureLabel(data.issuedAt)}</li>
                    <li>
                      Ingressou
                      {' '}
                      {formatIngressDate(data.issuedAt)}
                      .
                    </li>
                    <li>
                      Status da Associação:
                      {' '}
                      {membershipStatusPt(data.membershipStatus)}
                    </li>
                  </ul>
                </div>

                {/* Plano — left:136 top:81  w:52 h:327 → texto rotate(90deg) */}
                <div className="digital-card-figma__plan-wrap">
                  <div className="digital-card-figma__plan-inner">
                    <span className="digital-card-figma__socio-label">Sócio Torcedor</span>
                    <br />
                    <span className="digital-card-figma__plan-title">{planLabel}</span>
                  </div>
                </div>

                {/* Avatar/badge — left:214 top:81  w:105 h:109 */}
                <div className="digital-card-figma__badge">
                  {photoSrc ? (
                    <img src={photoSrc} alt="" className="digital-card-figma__badge-img" />
                  ) : (
                    <span className="digital-card-figma__badge-initials">{avatarInitials}</span>
                  )}
                </div>

                {/* Nome titular — left:253 top:215  w:34 h:327 → texto rotate(90deg) */}
                <div className="digital-card-figma__name-wrap">
                  <p className="digital-card-figma__name-inner">{holderOnCard}</p>
                </div>

                {/* Escudo — left:117 top:483  w:141 h:141 → rotate(90deg) + shadow */}
                {/* <div className="digital-card-figma__shield-wrap">
                  <img
                    className="digital-card-figma__shield"
                    src="/logos/ESCUDO_FFC_PNG.png"
                    alt=""
                  />
                </div> */}
              </div>
            </section>

            <p className="digital-card-figma__state-pill">
              {stateLabel(data.state)}
              {fromCache ? (
                <span className="digital-card-figma__cache-tag"> (cache local)</span>
              ) : null}
            </p>
            {data.verificationToken ? (
              <p className="digital-card-figma__token">
                <span className="digital-card-figma__token-label">Token de verificação: </span>
                {data.verificationToken}
              </p>
            ) : null}
            {data.cacheValidUntilUtc ? (
              <p className="digital-card-figma__cache-note">
                Cache até
                {' '}
                {new Date(data.cacheValidUntilUtc).toLocaleString('pt-BR')}
                . Dados podem ser usados offline nesse período.
              </p>
            ) : null}
          </>
        ) : null}

        {!loading && !error && data && data.state !== 'Active' ? (
          <div className="digital-card-display digital-card-display--fallback">
            <p className="digital-card-display__state">{stateLabel(data.state)}</p>
            <p className="digital-card-display__sub">
              Associação: {data.membershipStatus}
            </p>
            {data.message ? (
              <p className="digital-card-message">{data.message}</p>
            ) : null}
            {data.templatePreviewLines?.length ? (
              <div className="digital-card-display__template">
                {data.templatePreviewLines.join('\n')}
              </div>
            ) : null}
          </div>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
