import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Newspaper,
  Calendar,
  CreditCard,
  Trophy,
  Gift,
  Ticket,
  Headphones,
  LogOut,
  ShieldCheck,
  AlertTriangle,
  ChevronRight,
} from 'lucide-react'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import { ADMIN_AREA_PERMISSIONS } from '../shared/auth/applicationPermissions'
import { canAccessAdminArea } from '../shared/auth/permissionUtils'
import { useAuth } from '../features/auth/AuthContext'
import { TeamShieldLogo } from '../shared/branding/TeamShieldLogo'
import { resolvePublicAssetUrl } from '../features/account/accountApi'
import {
  listEligibleBenefitOffers,
  type TorcedorEligibleBenefitOffer,
} from '../features/torcedor/torcedorBenefitsApi'
import './AppShell.css'

function formatBenefitPeriod(startAt: string, endAt: string) {
  return `Válido de ${new Date(startAt).toLocaleDateString('pt-BR')} até ${new Date(endAt).toLocaleDateString('pt-BR')}`
}

const QUICK_LINKS = [
  { to: '/news', label: 'Notícias', icon: <Newspaper size={20} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={20} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={20} /> },
  { to: '/plans', label: 'Planos', icon: <ShieldCheck size={20} /> },
  { to: '/loyalty', label: 'Fidelidade', icon: <Trophy size={20} /> },
  { to: '/benefits', label: 'Benefícios', icon: <Gift size={20} /> },
  { to: '/tickets', label: 'Ingressos', icon: <Ticket size={20} /> },
  { to: '/support', label: 'Chamados', icon: <Headphones size={20} /> },
]

export function DashboardPage() {
  const { user, logout } = useAuth()
  const showAdmin = canAccessAdminArea(user, ADMIN_AREA_PERMISSIONS)
  const firstName = user?.name?.split(' ')[0] ?? ''
  const [benefitBanners, setBenefitBanners] = useState<TorcedorEligibleBenefitOffer[]>([])
  const [benefitsLoading, setBenefitsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setBenefitsLoading(true)
        const page = await listEligibleBenefitOffers({ page: 1, pageSize: 10 })
        if (!cancelled)
          setBenefitBanners(page.items)
      }
      catch {
        if (!cancelled)
          setBenefitBanners([])
      }
      finally {
        if (!cancelled)
          setBenefitsLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="dash-root">
      <header className="dash-header">
        <div className="dash-header__brand">
          <TeamShieldLogo className="dash-header__logo" alt="Escudo do clube" width={36} height={36} />
          <span className="dash-header__logo-text">AppTorcedor</span>
        </div>
        <div className="dash-header__right">
          <button
            type="button"
            className="dash-header__logout"
            aria-label="Sair"
            onClick={() => void logout()}
          >
            <LogOut size={18} />
          </button>
        </div>
      </header>

      <main className="dash-content">
        <div className="dash-hero">
          <p className="dash-hero__greeting">Olá,</p>
          <h1 className="dash-hero__name">{firstName}</h1>
        </div>

        {user?.requiresProfileCompletion ? (
          <Link to="/account" className="dash-alert">
            <AlertTriangle size={16} />
            Complete seu perfil para liberar todas as funcionalidades.
          </Link>
        ) : null}

        {showAdmin ? (
          <Link to="/admin" className="dash-admin-badge">
            <ShieldCheck size={16} />
            Painel administrativo
          </Link>
        ) : null}

        {benefitsLoading ? (
          <section className="dash-benefits-section" aria-label="Benefícios em destaque">
            <p className="dash-section-title">Benefícios</p>
            <div className="dash-benefits-carousel" role="presentation">
              <div className="dash-benefit-banner-skeleton" />
              <div className="dash-benefit-banner-skeleton" />
            </div>
          </section>
        ) : null}
        {!benefitsLoading && benefitBanners.length > 0 ? (
          <section className="dash-benefits-section" aria-label="Benefícios em destaque">
            <p className="dash-section-title">Benefícios</p>
            <div className="dash-benefits-carousel">
              {benefitBanners.map((item) => {
                const hasBanner = Boolean(item.bannerUrl?.trim())
                const bannerSrc = resolvePublicAssetUrl(item.bannerUrl)
                return (
                  <Link
                    key={item.offerId}
                    to={`/benefits/${item.offerId}`}
                    className={hasBanner ? 'dash-benefit-banner dash-benefit-banner--visual' : 'dash-benefit-banner'}
                  >
                    {hasBanner && bannerSrc ? (
                      <>
                        <div className="dash-benefit-banner__media">
                          <img src={bannerSrc} alt="" loading="lazy" />
                        </div>
                        {item.description ? (
                          <p className="dash-benefit-banner__description">{item.description}</p>
                        ) : null}
                        <span className="dash-benefit-banner__dates">{formatBenefitPeriod(item.startAt, item.endAt)}</span>
                      </>
                    ) : (
                      <>
                        <span className="dash-benefit-banner__eyebrow">
                          <Gift size={16} />
                          Resgatar
                        </span>
                        <span className="dash-benefit-banner__title">{item.title}</span>
                        <span className="dash-benefit-banner__partner">{item.partnerName}</span>
                        <span className="dash-benefit-banner__dates">
                          Até
                          {' '}
                          {new Date(item.endAt).toLocaleDateString('pt-BR')}
                        </span>
                        <span className="dash-benefit-banner__cta">
                          Ver detalhes
                          <ChevronRight size={16} />
                        </span>
                      </>
                    )}
                  </Link>
                )
              })}
            </div>
          </section>
        ) : null}

        <p className="dash-section-title">Acessos rápidos</p>
        <nav className="dash-quick-grid" aria-label="Acessos rápidos">
          {QUICK_LINKS.map(link => (
            <Link key={link.to} to={link.to} className="dash-quick-card">
              <span className="dash-quick-card__icon">{link.icon}</span>
              <span className="dash-quick-card__label">{link.label}</span>
            </Link>
          ))}
        </nav>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
