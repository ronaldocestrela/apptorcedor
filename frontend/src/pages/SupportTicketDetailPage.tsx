import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  cancelMySupportTicket,
  downloadMySupportAttachment,
  getMySupportTicket,
  reopenMySupportTicket,
  replyMySupportTicket,
  type TorcedorSupportDetail,
} from '../features/torcedor/torcedorSupportApi'

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

  if (loading)
    return <p style={{ margin: '2rem', fontFamily: 'system-ui' }}>Carregando…</p>
  if (error || !detail) {
    return (
      <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
        <p>
          <Link to="/support">← Meus chamados</Link>
        </p>
        <p style={{ color: '#721c24' }}>{error ?? 'Não encontrado'}</p>
      </main>
    )
  }

  const canCancel = detail.status !== 'Closed'
  const canReopen = detail.status === 'Closed'

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/support">← Meus chamados</Link>
      </p>
      <h1>{detail.subject}</h1>
      <p style={{ color: '#555' }}>
        Fila:
        {' '}
        {detail.queue}
        {' '}
        · Status:
        {' '}
        <strong>{detail.status}</strong>
        {' '}
        · Prioridade:
        {' '}
        {detail.priority}
      </p>
      {detail.isSlaBreached ? <p style={{ color: '#c00' }}>SLA estourado</p> : null}

      <section style={{ marginTop: '1.5rem' }}>
        <h2 style={{ fontSize: '1.05rem' }}>Mensagens</h2>
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {detail.messages.map(m => (
            <li
              key={m.messageId}
              style={{
                marginBottom: '1rem',
                padding: '0.75rem',
                background: '#fafafa',
                borderRadius: 6,
              }}
            >
              <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{m.body}</p>
              <p style={{ margin: '0.5rem 0 0', fontSize: '0.8rem', color: '#888' }}>
                {new Date(m.createdAtUtc).toLocaleString()}
              </p>
              {m.attachments.length > 0 ? (
                <ul style={{ margin: '0.5rem 0 0', paddingLeft: '1.2rem' }}>
                  {m.attachments.map(a => (
                    <li key={a.attachmentId}>
                      <button
                        type="button"
                        style={{
                          background: 'none',
                          border: 'none',
                          color: '#0645ad',
                          cursor: 'pointer',
                          textDecoration: 'underline',
                          padding: 0,
                          font: 'inherit',
                        }}
                        onClick={() =>
                          void downloadMySupportAttachment(a.downloadUrl, a.fileName).catch(() => {
                            /* toast optional */
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
          <h2 style={{ fontSize: '1.05rem' }}>Responder</h2>
          <form onSubmit={e => void onReply(e)}>
            <textarea
              value={replyBody}
              onChange={e => setReplyBody(e.target.value)}
              rows={3}
              placeholder="Sua mensagem"
              style={{ display: 'block', width: '100%' }}
            />
            <input
              type="file"
              multiple
              accept="image/jpeg,image/png,image/webp,application/pdf"
              onChange={e => setReplyFiles(e.target.files)}
              style={{ display: 'block', marginTop: 8 }}
            />
            {actionError ? <p style={{ color: '#721c24' }}>{actionError}</p> : null}
            <button type="submit" disabled={actionBusy} style={{ marginTop: 8 }}>
              Enviar
            </button>
          </form>
        </section>
      ) : null}

      <section style={{ marginTop: '1.5rem' }}>
        {canCancel ? (
          <button type="button" onClick={() => void onCancel()} disabled={actionBusy}>
            Cancelar chamado
          </button>
        ) : null}
        {canReopen ? (
          <button
            type="button"
            onClick={() => void onReopen()}
            disabled={actionBusy}
            style={{ marginLeft: canCancel ? 8 : 0 }}
          >
            Reabrir chamado
          </button>
        ) : null}
      </section>

      <section style={{ marginTop: '2rem' }}>
        <h2 style={{ fontSize: '1.05rem' }}>Histórico</h2>
        <ul style={{ fontSize: '0.85rem', color: '#555', paddingLeft: '1.2rem' }}>
          {detail.history.map(h => (
            <li key={h.entryId} style={{ marginBottom: 6 }}>
              <strong>{h.eventType}</strong>
              {h.reason ? ` — ${h.reason}` : ''}
              {' '}
              (
              {new Date(h.createdAtUtc).toLocaleString()}
              )
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}
