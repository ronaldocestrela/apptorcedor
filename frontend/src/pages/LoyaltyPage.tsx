import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import {
  getAllTimeLoyaltyRanking,
  getMonthlyLoyaltyRanking,
  getMyLoyaltySummary,
  type TorcedorLoyaltyRankingPage,
  type TorcedorLoyaltySummary,
} from '../features/torcedor/torcedorLoyaltyApi'

function RankingBlock(props: {
  title: string
  page: TorcedorLoyaltyRankingPage | null
  currentUserId: string | undefined
}) {
  const { title, page, currentUserId } = props
  if (!page)
    return null

  return (
    <section style={{ marginTop: '1.5rem' }}>
      <h2 style={{ fontSize: '1.1rem' }}>{title}</h2>
      {page.totalCount === 0 ? (
        <p style={{ color: '#666' }}>Nenhum ponto registrado neste período.</p>
      ) : (
        <ol style={{ paddingLeft: '1.25rem', margin: '0.5rem 0' }}>
          {page.items.map((row) => {
            const isMe = currentUserId && row.userId === currentUserId
            return (
              <li
                key={`${row.rank}-${row.userId}`}
                style={{
                  marginBottom: 6,
                  fontWeight: isMe ? 700 : 400,
                  background: isMe ? '#e8f4fc' : undefined,
                  padding: isMe ? '4px 8px' : undefined,
                  borderRadius: 4,
                }}
              >
                {row.rank}. {row.userName || '(sem nome)'} — {row.totalPoints} pts {isMe ? ' (você)' : null}
              </li>
            )
          })}
        </ol>
      )}
      {page.me ? (
        <p style={{ fontSize: '0.9rem', color: '#333' }}>
          Sua posição: <strong>#{page.me.rank}</strong> — {page.me.totalPoints} pts
        </p>
      ) : (
        <p style={{ fontSize: '0.9rem', color: '#666' }}>
          Você ainda não aparece no ranking deste período (sem pontos ou saldo zerado).
        </p>
      )}
    </section>
  )
}

export function LoyaltyPage() {
  const { user } = useAuth()
  const [summary, setSummary] = useState<TorcedorLoyaltySummary | null>(null)
  const [monthly, setMonthly] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [allTime, setAllTime] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const periodLabel = useMemo(() => {
    const now = new Date()
    const y = now.getUTCFullYear()
    const m = now.getUTCMonth() + 1
    return `${String(m).padStart(2, '0')}/${y} (UTC)`
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const now = new Date()
        const year = now.getUTCFullYear()
        const month = now.getUTCMonth() + 1
        const [s, mPage, aPage] = await Promise.all([
          getMyLoyaltySummary(),
          getMonthlyLoyaltyRanking({ year, month, pageSize: 50 }),
          getAllTimeLoyaltyRanking({ pageSize: 50 }),
        ])
        if (!cancelled) {
          setSummary(s)
          setMonthly(mPage)
          setAllTime(aPage)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar fidelidade')
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
      <h1>Fidelidade</h1>
      {loading ? <p>Carregando...</p> : null}
      {error ? (
        <p style={{ color: '#b00020' }} role="alert">
          {error}
        </p>
      ) : null}
      {!loading && !error && summary ? (
        <>
          <section>
            <p style={{ fontSize: '0.95rem', color: '#555' }}>
              Atualizado em: {new Date(summary.asOfUtc).toLocaleString()}
            </p>
            <ul style={{ listStyle: 'none', padding: 0, margin: '1rem 0' }}>
              <li>
                <strong>Saldo total:</strong> {summary.totalPoints} pts
              </li>
              <li>
                <strong>Pontos no mês ({periodLabel}):</strong> {summary.monthlyPoints} pts
              </li>
              <li>
                <strong>Sua posição no mês:</strong>{' '}
                {summary.monthlyRank != null ? `#${summary.monthlyRank}` : '—'}
              </li>
              <li>
                <strong>Sua posição geral:</strong>{' '}
                {summary.allTimeRank != null ? `#${summary.allTimeRank}` : '—'}
              </li>
            </ul>
          </section>
          <RankingBlock
            title={`Ranking do mês (${periodLabel})`}
            page={monthly}
            currentUserId={user?.id}
          />
          <RankingBlock title="Ranking geral (todos os tempos)" page={allTime} currentUserId={user?.id} />
        </>
      ) : null}
    </main>
  )
}
