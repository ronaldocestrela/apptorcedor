import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Settings, Trophy } from 'lucide-react'
import {
  getAllTimeLoyaltyRanking,
  getMonthlyLoyaltyRanking,
  getMyLoyaltySummary,
  type TorcedorLoyaltyRankingPage,
  type TorcedorLoyaltyRankingRow,
  type TorcedorLoyaltySummary,
} from '../features/torcedor/torcedorLoyaltyApi'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function formatStatPoints(pts: number): string {
  return `${pts} pts`
}

function formatRankPoints(pts: number): string {
  if (pts === 0)
    return '—'
  return `${pts} pts`
}

function formatStanding(rank: number | null): string {
  if (rank == null)
    return '-'
  return `#${rank}`
}

function rankRowFadeClass(index: number): string {
  if (index === 4)
    return 'loyalty-figma-rank-row--fade30'
  if (index === 5)
    return 'loyalty-figma-rank-row--fade15'
  if (index >= 6)
    return 'loyalty-figma-rank-row--fade05'
  return ''
}

export function LoyaltyPage() {
  const [summary, setSummary] = useState<TorcedorLoyaltySummary | null>(null)
  const [monthly, setMonthly] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [allTime, setAllTime] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [rankingTab, setRankingTab] = useState<'month' | 'all'>('month')

  const rankingPage: TorcedorLoyaltyRankingPage | null = rankingTab === 'month' ? monthly : allTime

  const rankingRows = useMemo((): TorcedorLoyaltyRankingRow[] => {
    if (!rankingPage?.items?.length)
      return []
    return rankingPage.items.slice(0, 7)
  }, [rankingPage])

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

  useEffect(() => {
    document.title = 'Fidelidade | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  const rankingTitle = rankingTab === 'month' ? 'Ranking do Mês' : 'Ranking Geral'

  return (
    <div className="loyalty-root">
      <header className="subpage-header subpage-header--tri loyalty-page__header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={24} strokeWidth={2} aria-hidden="true" />
        </Link>
        <h1 className="subpage-header__title">Fidelidade</h1>
        <Link
          to="/account"
          className="subpage-header__badge-btn"
          aria-label="Conta e configurações"
        >
          <Settings size={24} strokeWidth={2} aria-hidden="true" />
        </Link>
      </header>

      <main className="subpage-content loyalty-page">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p role="alert" className="loyalty-page__error">{error}</p>
        ) : null}
        {!loading && !error && summary ? (
          <>
            <section className="loyalty-figma-panel" aria-label="Resumo de fidelidade">
              <div className="loyalty-figma-stat-row">
                <span className="loyalty-figma-stat-label">Saldo total:</span>
                <span className="loyalty-figma-stat-value loyalty-figma-stat-value--accent">
                  {formatStatPoints(summary.totalPoints)}
                </span>
              </div>
              <div className="loyalty-figma-stat-row">
                <span className="loyalty-figma-stat-label">Pontos no mês:</span>
                <span className="loyalty-figma-stat-value">{formatStatPoints(summary.monthlyPoints)}</span>
              </div>
              <div className="loyalty-figma-stat-row">
                <span className="loyalty-figma-stat-label">Posição geral:</span>
                <span className="loyalty-figma-stat-value">{formatStanding(summary.allTimeRank)}</span>
              </div>
              <div className="loyalty-figma-stat-row loyalty-figma-stat-row--last">
                <span className="loyalty-figma-stat-label">Posição no mês:</span>
                <span className="loyalty-figma-stat-value">{formatStanding(summary.monthlyRank)}</span>
              </div>
            </section>

            <section className="loyalty-figma-ranking" aria-labelledby="loyalty-ranking-title">
              <div className="loyalty-figma-ranking__trophy" aria-hidden="true">
                <Trophy size={27} stroke="#8cd392" strokeWidth={1.75} fill="none" />
              </div>
              <h2 id="loyalty-ranking-title" className="loyalty-figma-ranking__title">
                {rankingTitle}
              </h2>
              <div className="loyalty-figma-rank-list">
                {rankingRows.length === 0 ? (
                  <p className="loyalty-figma-rank-empty app-muted">Nenhum ponto registrado neste período.</p>
                ) : (
                  rankingRows.map((row, idx) => (
                    <div
                      key={`${row.rank}-${row.userId}`}
                      className={`loyalty-figma-rank-row ${rankRowFadeClass(idx)}`}
                    >
                      <span className="loyalty-figma-rank-row__name">{row.userName || '—'}</span>
                      <span
                        className={
                          row.rank === 1
                            ? 'loyalty-figma-rank-row__pts loyalty-figma-rank-row__pts--accent'
                            : 'loyalty-figma-rank-row__pts'
                        }
                      >
                        {formatRankPoints(row.totalPoints)}
                      </span>
                    </div>
                  ))
                )}
              </div>
            </section>

            <div className="loyalty-figma-switch-wrap">
              <div className="loyalty-figma-switch" role="group" aria-label="Período do ranking">
                <button
                  type="button"
                  className={
                    rankingTab === 'month'
                      ? 'loyalty-figma-switch__btn loyalty-figma-switch__btn--active'
                      : 'loyalty-figma-switch__btn loyalty-figma-switch__btn--idle'
                  }
                  onClick={() => setRankingTab('month')}
                >
                  Mês
                </button>
                <span className="loyalty-figma-switch__divider" aria-hidden="true" />
                <button
                  type="button"
                  className={
                    rankingTab === 'all'
                      ? 'loyalty-figma-switch__btn loyalty-figma-switch__btn--active'
                      : 'loyalty-figma-switch__btn loyalty-figma-switch__btn--idle'
                  }
                  onClick={() => setRankingTab('all')}
                >
                  Geral
                </button>
              </div>
            </div>
          </>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
