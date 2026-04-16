import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import {
  cancelMySupportTicket,
  downloadMySupportAttachment,
  getMySupportTicket,
  reopenMySupportTicket,
  replyMySupportTicket,
  type TorcedorSupportDetail,
} from '../features/torcedor/torcedorSupportApi'
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

export function SupportTicketDetailPage() {
  const { ticketId = '' } = useParams()
  const [detail, setDetail] = useState<TorcedorSupportDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [replyBody, setReplyBody] = useState('')
  const [replyFiles, setReplyFiles] = useState<FileList | null>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!ticketId)
      return
    const d = await getMySupportTicket(ticketId)
    setDetail(d)
  }, [ticketId])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        await load()
        if (!cancelled)
          setError(null)
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Chamado não encontrado')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [load])

  async function onReply(e: React.FormEvent) {
    e.preventDefault()
    setActionError(null)
    if (!replyBody.trim() && (!replyFiles || replyFiles.length === 0)) {
      setActionError('Escreva uma mensagem ou anexe um arquivo.')
      return
    }
    setActionBusy(true)
    try {
      const fl = replyFiles ? Array.from(replyFiles) : []
      await replyMySupportTicket(ticketId, {
        body: replyBody.trim() || undefined,
        files: fl.length ? fl : undefined,
      })
      setReplyBody('')
      setReplyFiles(null)
      await load()
    }
    catch (err) {
      setActionError(err instanceof Error ? err.message : 'Não foi possível enviar')
    }
    finally {
      setActionBusy(false)
    }
  }

  async function onCancel() {
    setActionError(null)
    setActionBusy(true)
    try {
      await cancelMySupportTicket(ticketId)
      await load()
    }
    catch (err) {
      setActionError(err instanceof Error ? err.message : 'Não foi possível cancelar')
    }
    finally {
      setActionBusy(false)
    }
  }

  async function onReopen() {
    setActionError(null)
    setActionBusy(true)
    try {
      await reopenMySupportTicket(ticketId)
      await load()
    }
    catch (err) {
      setActionError(err instanceof Error ? err.message : 'Não foi possível reabrir')
    }
    finally {
      setActionBusy(false)
    }
  }

  if (loading) {
    return (
      <div className="support-detail-root">
        <header className="subpage-header">
          <Link to="/support" className="subpage-header__back" aria-label="Voltar">
            <ArrowLeft size={18} />
          </Link>
          <h1 className="subpage-header__title subpage-header__title--truncate">Chamado</h1>
          <span className="subpage-header__badge" aria-hidden>…</span>
        </header>
        <main className="subpage-content">
          <p className="app-muted">Carregando…</p>
        </main>
        <TorcedorBottomNav />
      </div>
    )
  }

  if (error || !detail) {
    return (
      <div className="support-detail-root">
        <header className="subpage-header">
          <Link to="/support" className="subpage-header__back" aria-label="Voltar">
            <ArrowLeft size={18} />
          </Link>
          <h1 className="subpage-header__title">Chamado</h1>
          <span className="subpage-header__badge" aria-hidden>—</span>
        </header>
        <main className="subpage-content">
          <p role="alert" className="games-page__error">{error ?? 'Não encontrado'}</p>
          <p style={{ marginTop: '1rem' }}>
            <Link to="/support" style={{ color: '#8cd392' }}>Voltar para Meus chamados</Link>
          </p>
        </main>
        <TorcedorBottomNav />
      </div>
    )
  }

  const canCancel = detail.status !== 'Closed'
  const canReopen = detail.status === 'Closed'

  return (
    <div className="support-detail-root">
      <header className="subpage-header">
        <Link to="/support" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title subpage-header__title--truncate" title={detail.subject}>
          {detail.subject}
        </h1>
        <span className={supportStatusBadgeClass(detail.status)} title="Status">
          {detail.status}
        </span>
      </header>

      <main className="subpage-content">
        <p className="support-detail-meta">
          Fila:
          {' '}
          <strong>{detail.queue}</strong>
          {' '}
          · Prioridade:
          {' '}
          <strong>{detail.priority}</strong>
        </p>
        {detail.isSlaBreached ? (
          <p role="status" className="support-form__error" style={{ marginBottom: '1rem' }}>
            SLA estourado
          </p>
        ) : null}

        <section>
          <h2 className="support-section-title">Mensagens</h2>
          <ul className="support-msg-list">
            {detail.messages.map(m => (
              <li key={m.messageId} className="support-msg-card">
                <p className="support-msg-card__body">{m.body}</p>
                <p className="support-msg-card__time">
                  {new Date(m.createdAtUtc).toLocaleString('pt-BR')}
                </p>
                {m.attachments.length > 0 ? (
                  <ul className="support-msg-card__attachments">
                    {m.attachments.map(a => (
                      <li key={a.attachmentId}>
                        <button
                          type="button"
                          onClick={() =>
                            void downloadMySupportAttachment(a.downloadUrl, a.fileName).catch(() => {
                              /* optional toast */
                            })}
                        >
                          {a.fileName}
                        </button>
                        {' '}
                        (
                        {a.contentType}
                        )
                      </li>
                    ))}
                  </ul>
                ) : null}
              </li>
            ))}
          </ul>
        </section>

        {detail.status !== 'Closed' ? (
          <section style={{ marginTop: '1.5rem' }}>
            <h2 className="support-section-title">Responder</h2>
            <form className="support-form" onSubmit={e => void onReply(e)}>
              <div className="support-form__row">
                <label htmlFor="support-reply">Sua mensagem</label>
                <textarea
                  id="support-reply"
                  value={replyBody}
                  onChange={e => setReplyBody(e.target.value)}
                  rows={3}
                  placeholder="Escreva aqui…"
                />
              </div>
              <div className="support-form__row">
                <label htmlFor="support-reply-files">Anexos</label>
                <input
                  id="support-reply-files"
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  onChange={e => setReplyFiles(e.target.files)}
                />
              </div>
              {actionError ? <p className="support-form__error" role="alert">{actionError}</p> : null}
              <button type="submit" className="support-form__submit" disabled={actionBusy}>
                {actionBusy ? 'Enviando…' : 'Enviar'}
              </button>
            </form>
          </section>
        ) : null}

        <div className="support-actions">
          {canCancel ? (
            <button
              type="button"
              className="support-btn-secondary"
              onClick={() => void onCancel()}
              disabled={actionBusy}
            >
              Cancelar chamado
            </button>
          ) : null}
          {canReopen ? (
            <button
              type="button"
              className="support-btn-secondary"
              onClick={() => void onReopen()}
              disabled={actionBusy}
            >
              Reabrir chamado
            </button>
          ) : null}
        </div>

        <section className="support-history">
          <h2 className="support-section-title">Histórico</h2>
          <ul className="support-history__list">
            {detail.history.map(h => (
              <li key={h.entryId}>
                <strong>{h.eventType}</strong>
                {h.reason ? ` — ${h.reason}` : ''}
                {' '}
                (
                {new Date(h.createdAtUtc).toLocaleString('pt-BR')}
                )
              </li>
            ))}
          </ul>
        </section>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
