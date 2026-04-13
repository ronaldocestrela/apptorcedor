import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listEligibleBenefitOffers, type TorcedorEligibleBenefitOffer } from '../features/torcedor/torcedorBenefitsApi'

export function BenefitsEligiblePage() {
  const [items, setItems] = useState<TorcedorEligibleBenefitOffer[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const page = await listEligibleBenefitOffers({ pageSize: 50 })
        if (!cancelled) {
          setItems(page.items)
          setTotal(page.totalCount)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar benefícios')
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
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Benefícios para você</h1>
      <p style={{ color: '#555' }}>
        Ofertas ativas e vigentes alinhadas ao seu plano e status de sócio.
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error ? (
        <p style={{ color: '#555' }}>
          {total}
          {' '}
          oferta(s) elegível(is)
        </p>
      ) : null}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(item => (
          <li key={item.offerId} style={{ marginBottom: '1.25rem', borderBottom: '1px solid #eee', paddingBottom: '1rem' }}>
            <strong>{item.title}</strong>
            <div style={{ fontSize: '0.9rem', color: '#555', marginTop: 4 }}>
              {item.partnerName}
            </div>
            {item.description ? <p style={{ margin: '0.5rem 0 0' }}>{item.description}</p> : null}
            <p style={{ margin: '0.35rem 0 0', fontSize: '0.8rem', color: '#888' }}>
              Válido de
              {' '}
              {new Date(item.startAt).toLocaleDateString()}
              {' '}
              até
              {' '}
              {new Date(item.endAt).toLocaleDateString()}
            </p>
          </li>
        ))}
      </ul>
      {!loading && items.length === 0 ? <p>Nenhuma oferta elegível no momento.</p> : null}
    </main>
  )
}
