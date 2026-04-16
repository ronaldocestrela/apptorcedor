import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, CreditCard } from 'lucide-react'
import {
  getMyDigitalCardWithSource,
  type MyDigitalCardView,
  type MyDigitalCardViewState,
} from '../features/torcedor/torcedorDigitalCardApi'
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

export function DigitalCardPage() {
  const [data, setData] = useState<MyDigitalCardView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fromCache, setFromCache] = useState(false)

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

  return (
    <div className="digital-card-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Carteirinha digital</h1>
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {!loading && !error && data ? (
          <div className="digital-card-display">
            <p className="digital-card-display__state">
              {stateLabel(data.state)}
              {fromCache ? (
                <span style={{ marginLeft: '0.5rem', fontSize: '0.78rem', fontWeight: 400, color: '#6a9c78' }}>
                  (cache local)
                </span>
              ) : null}
            </p>
            <p className="digital-card-display__sub">
              Associação: {data.membershipStatus}
            </p>
            {data.message ? (
              <p className="digital-card-message">{data.message}</p>
            ) : null}
            {data.state === 'Active' && data.templatePreviewLines?.length ? (
              <div className="digital-card-display__template">
                {data.templatePreviewLines.join('\n')}
              </div>
            ) : null}
            {data.state === 'Active' && data.verificationToken ? (
              <p className="digital-card-display__token">
                <span style={{ color: '#9db7a7' }}>Token de verificação: </span>
                {data.verificationToken}
              </p>
            ) : null}
            {data.cacheValidUntilUtc ? (
              <p className="digital-card-cache-note">
                Cache até {new Date(data.cacheValidUntilUtc).toLocaleString('pt-BR')}. Dados podem ser usados offline nesse período.
              </p>
            ) : null}
          </div>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
