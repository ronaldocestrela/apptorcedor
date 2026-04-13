import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getMyDigitalCardWithSource,
  type MyDigitalCardView,
  type MyDigitalCardViewState,
} from '../features/torcedor/torcedorDigitalCardApi'

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
    <main style={{ maxWidth: 560, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Carteirinha digital</h1>
      <p style={{ color: '#555' }}>
        Exibição para o torcedor autenticado. O token de verificação pertence à sua emissão ativa.
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error && data ? (
        <>
          <p style={{ marginBottom: 0 }}>
            <strong>{stateLabel(data.state)}</strong>
            {fromCache ? (
              <span style={{ marginLeft: 8, fontSize: '0.85rem', color: '#666' }}>
                (dados em cache local)
              </span>
            ) : null}
          </p>
          <p style={{ marginTop: 4, fontSize: '0.9rem', color: '#555' }}>
            Status da associação (sistema):
            {' '}
            {data.membershipStatus}
          </p>
          {data.message ? <p style={{ color: '#856404' }}>{data.message}</p> : null}
          {data.state === 'Active' && data.templatePreviewLines?.length ? (
            <section
              style={{
                marginTop: '1.25rem',
                padding: '1rem',
                border: '1px solid #ccc',
                borderRadius: 8,
                background: '#fafafa',
                fontFamily: 'ui-monospace, monospace',
                fontSize: '0.9rem',
                whiteSpace: 'pre-wrap',
              }}
            >
              {data.templatePreviewLines.join('\n')}
            </section>
          ) : null}
          {data.state === 'Active' && data.verificationToken ? (
            <p style={{ marginTop: '1rem', fontSize: '0.85rem', wordBreak: 'break-all' }}>
              <strong>Token de verificação:</strong>
              {' '}
              {data.verificationToken}
            </p>
          ) : null}
          {data.cacheValidUntilUtc ? (
            <p style={{ marginTop: '1rem', fontSize: '0.75rem', color: '#888' }}>
              Cache sugerido pela API até
              {' '}
              {new Date(data.cacheValidUntilUtc).toLocaleString()}
              . Os dados podem ser guardados localmente nesse período para uso offline limitado.
            </p>
          ) : null}
        </>
      ) : null}
    </main>
  )
}
