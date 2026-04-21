import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import {
  createMySupportTicket,
  listMySupportTickets,
  type TorcedorSupportListItem,
} from '../features/torcedor/torcedorSupportApi'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function supportStatusBadgeClass(status: string): string {
  const s = status.replace(/\s+/g, '').toLowerCase()
  if (s === 'open')
    return 'support-status-badge support-status-badge--open'
  if (s === 'inprogress' || s === 'waitinguser')
    return 'support-status-badge support-status-badge--progress'
  if (s === 'resolved' || s === 'closed')
    return 'support-status-badge support-status-badge--closed'
  return 'support-status-badge support-status-badge--default'
}

export function SupportTicketsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<TorcedorSupportListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [queue, setQueue] = useState('Geral')
  const [subject, setSubject] = useState('')
  const [priority, setPriority] = useState('Normal')
  const [message, setMessage] = useState('')
  const [files, setFiles] = useState<FileList | null>(null)
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    const page = await listMySupportTickets({ pageSize: 50 })
    setItems(page.items)
    setTotal(page.totalCount)
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        await reload()
        if (!cancelled)
          setError(null)
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar chamados')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [reload])

  useEffect(() => {
    document.title = 'Suporte | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  async function onCreate(e: React.FormEvent) {
    e.preventDefault()
    setCreateError(null)
    if (!subject.trim()) {
      setCreateError('Informe o assunto.')
      return
    }
    if (!message.trim() && (!files || files.length === 0)) {
      setCreateError('Escreva uma mensagem ou anexe ao menos um arquivo.')
      return
    }
    setCreating(true)
    try {
      const fl = files ? Array.from(files) : []
      const { ticketId } = await createMySupportTicket({
        queue: queue.trim(),
        subject: subject.trim(),
        priority,
        initialMessage: message.trim() || undefined,
        files: fl.length ? fl : undefined,
      })
      setSubject('')
      setMessage('')
      setFiles(null)
      setShowForm(false)
      await reload()
      navigate(`/support/${ticketId}`)
    }
    catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Não foi possível abrir o chamado')
    }
    finally {
      setCreating(false)
    }
  }

  const isEmpty = !loading && !error && items.length === 0

  return (
    <div className="support-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Meus Chamados</h1>
        <button
          type="button"
          className="subpage-header__badge-btn"
          aria-expanded={showForm}
          aria-label={showForm ? 'Fechar formulário de novo chamado' : 'Abrir novo chamado'}
          onClick={() => setShowForm(v => !v)}
        >
          <Settings size={18} />
        </button>
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" className="games-page__error">{error}</p> : null}

        {showForm ? (
          <section className="support-form-panel" aria-label="Novo chamado">
            <h2 className="support-form-panel__title">Abrir chamado</h2>
            <form className="support-form" onSubmit={e => void onCreate(e)}>
              <div className="support-form__row">
                <label htmlFor="support-queue">Fila</label>
                <input
                  id="support-queue"
                  value={queue}
                  onChange={e => setQueue(e.target.value)}
                  autoComplete="off"
                />
              </div>
              <div className="support-form__row">
                <label htmlFor="support-subject">Assunto</label>
                <input
                  id="support-subject"
                  value={subject}
                  onChange={e => setSubject(e.target.value)}
                  autoComplete="off"
                />
              </div>
              <div className="support-form__row">
                <label htmlFor="support-priority">Prioridade</label>
                <select
                  id="support-priority"
                  value={priority}
                  onChange={e => setPriority(e.target.value)}
                >
                  <option value="Normal">Normal</option>
                  <option value="High">Alta</option>
                  <option value="Urgent">Urgente</option>
                </select>
              </div>
              <div className="support-form__row">
                <label htmlFor="support-message">Mensagem</label>
                <textarea
                  id="support-message"
                  value={message}
                  onChange={e => setMessage(e.target.value)}
                  rows={4}
                />
              </div>
              <div className="support-form__row">
                <label htmlFor="support-files">Anexos (opcional)</label>
                <input
                  id="support-files"
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  onChange={e => setFiles(e.target.files)}
                />
              </div>
              {createError ? <p className="support-form__error" role="alert">{createError}</p> : null}
              <button type="submit" className="support-form__submit" disabled={creating}>
                {creating ? 'Enviando…' : 'Abrir chamado'}
              </button>
            </form>
          </section>
        ) : null}

        {isEmpty ? (
          <div className="support-empty">
            <p className="support-empty__text">Você ainda não possui chamados abertos.</p>
            <button
              type="button"
              className="support-new-btn"
              onClick={() => setShowForm(true)}
            >
              Novo chamado
            </button>
          </div>
        ) : null}

        {!loading && !error && items.length > 0 ? (
          <ul className="support-list">
            {items.map(t => (
              <li key={t.ticketId}>
                <Link to={`/support/${t.ticketId}`} className="support-ticket-card">
                  <p className="support-ticket-card__subject">{t.subject}</p>
                  <div className="support-ticket-card__meta">
                    <span>
                      Fila:
                      {' '}
                      {t.queue}
                    </span>
                    <span className={supportStatusBadgeClass(t.status)}>{t.status}</span>
                    {t.isSlaBreached ? (
                      <span className="support-status-badge support-status-badge--sla">SLA</span>
                    ) : null}
                  </div>
                  <p className="support-ticket-card__date">
                    Atualizado em
                    {' '}
                    {new Date(t.updatedAtUtc).toLocaleString('pt-BR')}
                  </p>
                </Link>
              </li>
            ))}
          </ul>
        ) : null}

        {!loading && !error && items.length > 0 ? (
          <p className="app-muted" style={{ marginTop: '1rem', fontSize: '0.82rem' }}>
            {total}
            {' '}
            chamado(s)
          </p>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
