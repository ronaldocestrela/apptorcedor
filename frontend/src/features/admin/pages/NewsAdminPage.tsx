import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  createAdminNews,
  createNewsInAppNotifications,
  getAdminNews,
  listAdminNews,
  publishAdminNews,
  unpublishAdminNews,
  updateAdminNews,
  type AdminNewsListItem,
  type NewsEditorialStatus,
  type UpsertNewsBody,
} from '../services/adminApi'

function emptyForm(): {
  newsId: string | null
  title: string
  summary: string
  content: string
  status: NewsEditorialStatus | null
  notifyScheduleLocal: string
  notifyUserIds: string
} {
  return {
    newsId: null,
    title: '',
    summary: '',
    content: '',
    status: null,
    notifyScheduleLocal: '',
    notifyUserIds: '',
  }
}

function parseUserIds(raw: string): string[] | null {
  const parts = raw
    .split(/[,;\s]+/)
    .map((s) => s.trim())
    .filter(Boolean)
  if (parts.length === 0)
    return null
  return parts
}

export function NewsAdminPage() {
  const [items, setItems] = useState<AdminNewsListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [statusFilter, setStatusFilter] = useState<NewsEditorialStatus | ''>('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState(() => emptyForm())

  const loadList = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await listAdminNews({ pageSize: 100, status: statusFilter })
      setItems(page.items)
      setTotalCount(page.totalCount)
    } catch {
      setError('Falha ao listar notícias.')
    } finally {
      setLoading(false)
    }
  }, [statusFilter])

  useEffect(() => {
    void loadList()
  }, [loadList])

  async function selectNews(newsId: string) {
    setError(null)
    try {
      const d = await getAdminNews(newsId)
      setForm({
        newsId: d.newsId,
        title: d.title,
        summary: d.summary ?? '',
        content: d.content,
        status: d.status,
        notifyScheduleLocal: '',
        notifyUserIds: '',
      })
    } catch {
      setError('Falha ao carregar notícia.')
    }
  }

  function mapBody(): UpsertNewsBody {
    return {
      title: form.title.trim(),
      summary: form.summary.trim() || null,
      content: form.content.trim(),
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const body = mapBody()
      if (form.newsId) {
        await updateAdminNews(form.newsId, body)
      } else {
        const { newsId } = await createAdminNews(body)
        await selectNews(newsId)
      }
      await loadList()
    } catch {
      setError('Falha ao salvar notícia.')
    } finally {
      setSaving(false)
    }
  }

  async function onPublish() {
    if (!form.newsId)
      return
    setSaving(true)
    setError(null)
    try {
      await publishAdminNews(form.newsId)
      await selectNews(form.newsId)
      await loadList()
    } catch {
      setError('Falha ao publicar.')
    } finally {
      setSaving(false)
    }
  }

  async function onUnpublish() {
    if (!form.newsId)
      return
    setSaving(true)
    setError(null)
    try {
      await unpublishAdminNews(form.newsId)
      await selectNews(form.newsId)
      await loadList()
    } catch {
      setError('Falha ao despublicar.')
    } finally {
      setSaving(false)
    }
  }

  async function onNotify() {
    if (!form.newsId)
      return
    if (form.status !== 'Published') {
      setError('Publique a notícia antes de enviar notificações.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      const local = form.notifyScheduleLocal.trim()
      const scheduledAt = local ? new Date(local).toISOString() : null
      const userIds = parseUserIds(form.notifyUserIds)
      await createNewsInAppNotifications(form.newsId, { scheduledAt, userIds })
      setForm((f) => ({ ...f, notifyScheduleLocal: '', notifyUserIds: '' }))
    } catch {
      setError('Falha ao agendar/disparar notificações.')
    } finally {
      setSaving(false)
    }
  }

  const canPublish = form.newsId && form.status !== 'Published'
  const canUnpublish = form.newsId && form.status === 'Published'

  return (
    <PermissionGate anyOf={[ApplicationPermissions.NoticiasPublicar]}>
      <h1>Notícias</h1>
      <p style={{ color: '#555' }}>Total: {totalCount}</p>
      {loading ? <p>Carregando...</p> : null}
      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}

      <div style={{ marginBottom: 16 }}>
        <label>
          Filtrar status{' '}
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as NewsEditorialStatus | '')}
          >
            <option value="">Todos</option>
            <option value="Draft">Rascunho</option>
            <option value="Published">Publicada</option>
            <option value="Unpublished">Despublicada</option>
          </select>
        </label>
      </div>

      <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
        <div style={{ flex: '1 1 320px' }}>
          <h2>Listagem</h2>
          <button type="button" onClick={() => setForm(emptyForm())} style={{ marginBottom: 8 }}>
            Nova notícia
          </button>
          <ul style={{ listStyle: 'none', padding: 0, maxHeight: 420, overflow: 'auto' }}>
            {items.map((n) => (
              <li key={n.newsId} style={{ borderBottom: '1px solid #eee', padding: '8px 0' }}>
                <button type="button" onClick={() => void selectNews(n.newsId)} style={{ fontWeight: 600 }}>
                  {n.title}
                </button>
                <div style={{ fontSize: 12, color: '#666' }}>
                  {n.status}
                  {' · '}
                  {new Date(n.updatedAt).toLocaleString()}
                </div>
              </li>
            ))}
          </ul>
        </div>

        <div style={{ flex: '1 1 360px' }}>
          <h2>{form.newsId ? 'Editar' : 'Nova'}</h2>
          {form.status ? (
            <p style={{ fontSize: 14, color: '#444' }}>
              Status: <strong>{form.status}</strong>
            </p>
          ) : null}

          <form onSubmit={(e) => void onSubmit(e)} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <label>
              Título
              <input
                value={form.title}
                onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                style={{ width: '100%' }}
                required
              />
            </label>
            <label>
              Resumo
              <textarea
                value={form.summary}
                onChange={(e) => setForm((f) => ({ ...f, summary: e.target.value }))}
                style={{ width: '100%', minHeight: 64 }}
              />
            </label>
            <label>
              Conteúdo
              <textarea
                value={form.content}
                onChange={(e) => setForm((f) => ({ ...f, content: e.target.value }))}
                style={{ width: '100%', minHeight: 160 }}
                required
              />
            </label>
            <button type="submit" disabled={saving}>
              Salvar
            </button>
          </form>

          {form.newsId ? (
            <div style={{ marginTop: 16, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {canPublish ? (
                <button type="button" disabled={saving} onClick={() => void onPublish()}>
                  Publicar
                </button>
              ) : null}
              {canUnpublish ? (
                <button type="button" disabled={saving} onClick={() => void onUnpublish()}>
                  Despublicar
                </button>
              ) : null}
            </div>
          ) : null}

          {form.newsId && form.status === 'Published' ? (
            <div style={{ marginTop: 24, padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
              <h3 style={{ marginTop: 0 }}>Notificação in-app</h3>
              <p style={{ fontSize: 13, color: '#555' }}>
                Vazio = envio imediato para contas ativas. Opcional: agendar data/hora local ou restringir a IDs de
                usuário.
              </p>
              <label>
                Agendar (opcional)
                <input
                  type="datetime-local"
                  value={form.notifyScheduleLocal}
                  onChange={(e) => setForm((f) => ({ ...f, notifyScheduleLocal: e.target.value }))}
                  style={{ width: '100%' }}
                />
              </label>
              <label style={{ display: 'block', marginTop: 8 }}>
                IDs de usuário (opcional, separados por vírgula)
                <input
                  value={form.notifyUserIds}
                  onChange={(e) => setForm((f) => ({ ...f, notifyUserIds: e.target.value }))}
                  style={{ width: '100%' }}
                  placeholder="ex.: f6f6d6c8-0a1a-4c0a-8f0a-000000000001"
                />
              </label>
              <button type="button" style={{ marginTop: 8 }} disabled={saving} onClick={() => void onNotify()}>
                Disparar / agendar notificação
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </PermissionGate>
  )
}
