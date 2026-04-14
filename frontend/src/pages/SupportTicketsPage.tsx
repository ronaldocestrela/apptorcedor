import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  createMySupportTicket,
  listMySupportTickets,
  type TorcedorSupportListItem,
} from '../features/torcedor/torcedorSupportApi'

export function SupportTicketsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<TorcedorSupportListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
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

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Meus chamados</h1>

      <section style={{ marginBottom: '2rem', padding: '1rem', background: '#f8f9fa', borderRadius: 8 }}>
        <h2 style={{ marginTop: 0, fontSize: '1.1rem' }}>Abrir chamado</h2>
        <form onSubmit={e => void onCreate(e)}>
          <div style={{ marginBottom: 8 }}>
            <label htmlFor="queue">Fila</label>
            <input
              id="queue"
              value={queue}
              onChange={e => setQueue(e.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </div>
          <div style={{ marginBottom: 8 }}>
            <label htmlFor="subject">Assunto</label>
            <input
              id="subject"
              value={subject}
              onChange={e => setSubject(e.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </div>
          <div style={{ marginBottom: 8 }}>
            <label htmlFor="priority">Prioridade</label>
            <select
              id="priority"
              value={priority}
              onChange={e => setPriority(e.target.value)}
              style={{ display: 'block', marginTop: 4 }}
            >
              <option value="Normal">Normal</option>
              <option value="High">Alta</option>
              <option value="Urgent">Urgente</option>
            </select>
          </div>
          <div style={{ marginBottom: 8 }}>
            <label htmlFor="message">Mensagem</label>
            <textarea
              id="message"
              value={message}
              onChange={e => setMessage(e.target.value)}
              rows={4}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </div>
          <div style={{ marginBottom: 8 }}>
            <label htmlFor="files">Anexos (opcional)</label>
            <input
              id="files"
              type="file"
              multiple
              accept="image/jpeg,image/png,image/webp,application/pdf"
              onChange={e => setFiles(e.target.files)}
              style={{ display: 'block', marginTop: 4 }}
            />
          </div>
          {createError ? <p style={{ color: '#721c24' }}>{createError}</p> : null}
          <button type="submit" disabled={creating}>
            {creating ? 'Enviando…' : 'Abrir chamado'}
          </button>
        </form>
      </section>

      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error ? (
        <p style={{ color: '#555' }}>
          {total}
          {' '}
          chamado(s)
        </p>
      ) : null}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(t => (
          <li
            key={t.ticketId}
            style={{
              marginBottom: '1.25rem',
              borderBottom: '1px solid #eee',
              paddingBottom: '1rem',
            }}
          >
            <Link to={`/support/${t.ticketId}`}>
              <strong>{t.subject}</strong>
            </Link>
            <p style={{ margin: '0.35rem 0 0', fontSize: '0.9rem', color: '#555' }}>
              Fila:
              {' '}
              {t.queue}
              {' '}
              · Status:
              {' '}
              <strong>{t.status}</strong>
              {t.isSlaBreached ? <span style={{ color: '#c00', marginLeft: 8 }}>SLA estourado</span> : null}
            </p>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.85rem', color: '#888' }}>
              Atualizado em
              {' '}
              {new Date(t.updatedAtUtc).toLocaleString()}
            </p>
          </li>
        ))}
      </ul>
    </main>
  )
}
