import { useCallback, useEffect, useState, type FormEvent, type MouseEvent } from 'react'
import { Link } from 'react-router-dom'
import { getApiErrorMessage } from '../../shared/auth'
import {
  createTenant,
  listTenants,
  type TenantListItemDto,
} from '../../shared/backoffice'
import { TenantStatus } from '../../shared/backoffice/types'
import { formatTenantStatus } from '../../shared/backoffice/formatters'

const PAGE_SIZE = 10
const MODAL_TITLE = 'bo-tenants-modal-title'

export function TenantsListPage() {
  const [items, setItems] = useState<TenantListItemDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [searchDraft, setSearchDraft] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [modalOpen, setModalOpen] = useState(false)
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [formBusy, setFormBusy] = useState(false)

  const loadList = useCallback(
    async (p: number) => {
      setListError(null)
      setListLoading(true)
      try {
        const res = await listTenants({
          page: p,
          pageSize: PAGE_SIZE,
          search: search || null,
          status: statusFilter === '' ? null : (Number(statusFilter) as TenantStatus),
        })
        setItems(res.items)
        setTotalCount(res.totalCount)
        setPage(res.page)
      } catch (e: unknown) {
        setListError(getApiErrorMessage(e, 'Não foi possível carregar os tenants.'))
        setItems([])
      } finally {
        setListLoading(false)
      }
    },
    [search, statusFilter],
  )

  useEffect(() => {
    void loadList(1)
  }, [loadList])

  function openCreate() {
    setName('')
    setSlug('')
    setFormError(null)
    setModalOpen(true)
  }

  function closeModal() {
    if (formBusy) return
    setModalOpen(false)
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    const n = name.trim()
    const s = slug.trim().toLowerCase()
    if (!n) {
      setFormError('Informe o nome.')
      return
    }
    if (!s) {
      setFormError('Informe o slug.')
      return
    }
    setFormBusy(true)
    try {
      await createTenant({ name: n, slug: s })
      setModalOpen(false)
      await loadList(1)
    } catch (err: unknown) {
      setFormError(getApiErrorMessage(err, 'Não foi possível criar o tenant.'))
    } finally {
      setFormBusy(false)
    }
  }

  function applySearch(e: FormEvent) {
    e.preventDefault()
    setSearch(searchDraft.trim())
  }

  function onBackdrop(e: MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget && !formBusy) closeModal()
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <section className="bo-page">
      <div className="bo-page__head">
        <h1 className="bo-page__title">Tenants</h1>
        <button type="button" className="bo-btn-primary" onClick={openCreate} disabled={modalOpen}>
          Novo tenant
        </button>
      </div>

      <form className="bo-filters" onSubmit={applySearch}>
        <label className="bo-field-inline">
          Busca
          <input
            type="search"
            value={searchDraft}
            onChange={(e) => setSearchDraft(e.target.value)}
            placeholder="Nome ou slug"
          />
        </label>
        <label className="bo-field-inline">
          Status
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">Todos</option>
            <option value={String(TenantStatus.Active)}>Ativo</option>
            <option value={String(TenantStatus.Suspended)}>Suspenso</option>
            <option value={String(TenantStatus.Inactive)}>Inativo</option>
          </select>
        </label>
        <button type="submit">Filtrar</button>
      </form>

      {listError ? (
        <p className="billing-page__error" role="alert">
          {listError}
        </p>
      ) : null}
      {listLoading ? <p className="bo-muted">Carregando…</p> : null}
      {!listLoading && items.length === 0 ? <p className="bo-muted">Nenhum tenant nesta página.</p> : null}
      {!listLoading && items.length > 0 ? (
        <div className="bo-table-wrap">
          <table className="bo-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Slug</th>
                <th>Status</th>
                <th>Criado</th>
                <th>Domínios</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr key={row.id}>
                  <td>
                    <strong>{row.name}</strong>
                  </td>
                  <td>
                    <code>{row.slug}</code>
                  </td>
                  <td>
                    <span className="bo-badge">{formatTenantStatus(row.status)}</span>
                  </td>
                  <td>{new Date(row.createdAt).toLocaleString()}</td>
                  <td>{row.domainCount}</td>
                  <td>
                    <Link to={`/backoffice/tenants/${row.id}`}>Detalhes</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {!listLoading && totalCount > 0 ? (
        <div className="bo-pager">
          <button type="button" disabled={page <= 1} onClick={() => void loadList(page - 1)}>
            Anterior
          </button>
          <span>
            Página {page} de {totalPages} ({totalCount})
          </span>
          <button type="button" disabled={page >= totalPages} onClick={() => void loadList(page + 1)}>
            Próxima
          </button>
        </div>
      ) : null}

      {modalOpen ? (
        <div className="admin-plans-modal" role="presentation" onClick={onBackdrop}>
          <div
            className="admin-plans-modal__panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby={MODAL_TITLE}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="admin-plans-modal__header">
              <h2 id={MODAL_TITLE} className="admin-plans-modal__title">
                Novo tenant
              </h2>
              <button type="button" className="admin-plans-modal__close" aria-label="Fechar" onClick={closeModal}>
                ×
              </button>
            </div>
            {formError ? (
              <p className="billing-page__error admin-plans-modal__error" role="alert">
                {formError}
              </p>
            ) : null}
            <form className="admin-plans-modal__form" onSubmit={handleCreate}>
              <label className="billing-page__field">
                Nome *
                <input
                  className="auth-field__input"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  disabled={formBusy}
                  required
                />
              </label>
              <label className="billing-page__field">
                Slug *
                <input
                  className="auth-field__input"
                  value={slug}
                  onChange={(e) => setSlug(e.target.value)}
                  disabled={formBusy}
                  required
                />
              </label>
              <div className="admin-plans-modal__footer billing-page__actions">
                <button type="submit" disabled={formBusy}>
                  Criar
                </button>
                <button type="button" className="admin-plans__btn-secondary" disabled={formBusy} onClick={closeModal}>
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </section>
  )
}
