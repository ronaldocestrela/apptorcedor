import { useCallback, useEffect, useRef, useState, type FormEvent, type MouseEvent } from 'react'
import { getApiErrorMessage } from '../../shared/auth'
import {
  createSaaSPlan,
  getSaaSPlanById,
  listSaaSPlans,
  toggleSaaSPlan,
  updateSaaSPlan,
  type SaaSPlanDto,
  type SaaSPlanFeatureDto,
} from '../../shared/backoffice'

const PAGE_SIZE = 10
const MODAL_TITLE = 'bo-saas-modal-title'
const TOGGLE_TITLE = 'bo-saas-toggle-title'

function parseFeaturesText(text: string): { key: string; description?: string | null; value?: string | null }[] {
  return text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter((l) => l.length > 0)
    .map((line) => {
      const parts = line.split('|').map((p) => p.trim())
      const key = parts[0] ?? ''
      if (!key) return null
      return {
        key,
        description: parts[1] || null,
        value: parts[2] || null,
      }
    })
    .filter((x): x is NonNullable<typeof x> => x != null)
}

function featuresToText(features: SaaSPlanFeatureDto[]): string {
  return features.map((f) => [f.key, f.description ?? '', f.value ?? ''].join(' | ')).join('\n')
}

export function SaaSPlansListPage() {
  const [items, setItems] = useState<SaaSPlanDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [monthlyPrice, setMonthlyPrice] = useState('0')
  const [yearlyPrice, setYearlyPrice] = useState('')
  const [maxMembers, setMaxMembers] = useState('100')
  const [stripeMonthly, setStripeMonthly] = useState('')
  const [stripeYearly, setStripeYearly] = useState('')
  const [featuresText, setFeaturesText] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [formBusy, setFormBusy] = useState(false)

  const [toggleConfirm, setToggleConfirm] = useState<{ id: string; name: string; isActive: boolean } | null>(null)
  const [toggleBusy, setToggleBusy] = useState(false)

  const firstRef = useRef<HTMLInputElement>(null)

  const loadList = useCallback(async (p: number) => {
    setListError(null)
    setListLoading(true)
    try {
      const res = await listSaaSPlans(p, PAGE_SIZE)
      setItems(res.items)
      setTotalCount(res.totalCount)
      setPage(res.page)
    } catch (e: unknown) {
      setListError(getApiErrorMessage(e, 'Não foi possível carregar os planos.'))
      setItems([])
    } finally {
      setListLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadList(1)
  }, [loadList])

  function resetForm() {
    setEditingId(null)
    setName('')
    setDescription('')
    setMonthlyPrice('0')
    setYearlyPrice('')
    setMaxMembers('100')
    setStripeMonthly('')
    setStripeYearly('')
    setFeaturesText('')
    setFormError(null)
  }

  const closeModal = useCallback(() => {
    if (formBusy) return
    setModalOpen(false)
    resetForm()
  }, [formBusy])

  function openCreate() {
    resetForm()
    setModalOpen(true)
  }

  useEffect(() => {
    if (!modalOpen) return
    const t = window.setTimeout(() => firstRef.current?.focus(), 0)
    return () => window.clearTimeout(t)
  }, [modalOpen])

  async function startEdit(planId: string) {
    setFormError(null)
    setFormBusy(true)
    try {
      const p = await getSaaSPlanById(planId)
      setEditingId(p.id)
      setName(p.name)
      setDescription(p.description ?? '')
      setMonthlyPrice(String(p.monthlyPrice))
      setYearlyPrice(p.yearlyPrice != null ? String(p.yearlyPrice) : '')
      setMaxMembers(String(p.maxMembers))
      setStripeMonthly(p.stripePriceMonthlyId ?? '')
      setStripeYearly(p.stripePriceYearlyId ?? '')
      setFeaturesText(featuresToText(p.features))
      setModalOpen(true)
    } catch (e: unknown) {
      setListError(getApiErrorMessage(e, 'Não foi possível carregar o plano.'))
    } finally {
      setFormBusy(false)
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    const nameTrim = name.trim()
    if (!nameTrim) {
      setFormError('Informe o nome.')
      return
    }
    const monthly = Number(monthlyPrice.replace(',', '.'))
    if (Number.isNaN(monthly) || monthly < 0) {
      setFormError('Preço mensal inválido.')
      return
    }
    const yearly =
      yearlyPrice.trim() === '' ? null : Number(yearlyPrice.replace(',', '.'))
    if (yearly != null && (Number.isNaN(yearly) || yearly < 0)) {
      setFormError('Preço anual inválido.')
      return
    }
    const maxM = Number(maxMembers)
    if (!Number.isInteger(maxM) || maxM < 0) {
      setFormError('Máx. de sócios deve ser inteiro ≥ 0.')
      return
    }
    const features = parseFeaturesText(featuresText)
    const body = {
      name: nameTrim,
      description: description.trim() || null,
      monthlyPrice: monthly,
      yearlyPrice: yearly,
      maxMembers: maxM,
      stripePriceMonthlyId: stripeMonthly.trim() || null,
      stripePriceYearlyId: stripeYearly.trim() || null,
      features: features.length ? features : null,
    }
    setFormBusy(true)
    try {
      if (editingId) {
        await updateSaaSPlan(editingId, body)
      } else {
        await createSaaSPlan(body)
      }
      setModalOpen(false)
      resetForm()
      await loadList(page)
    } catch (err: unknown) {
      setFormError(getApiErrorMessage(err, editingId ? 'Falha ao atualizar.' : 'Falha ao criar.'))
    } finally {
      setFormBusy(false)
    }
  }

  async function confirmToggle() {
    if (!toggleConfirm) return
    setToggleBusy(true)
    setListError(null)
    try {
      await toggleSaaSPlan(toggleConfirm.id)
      setToggleConfirm(null)
      await loadList(page)
    } catch (e: unknown) {
      setListError(getApiErrorMessage(e, 'Falha ao alternar status.'))
    } finally {
      setToggleBusy(false)
    }
  }

  function onBackdrop(e: MouseEvent<HTMLDivElement>) {
    if (e.target !== e.currentTarget || formBusy) return
    closeModal()
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <section className="bo-page admin-plans">
      <div className="bo-page__head">
        <h1 className="bo-page__title">Planos SaaS</h1>
        <button type="button" className="bo-btn-primary" onClick={openCreate} disabled={modalOpen}>
          Novo plano
        </button>
      </div>
      <p className="bo-muted">Planos cobrados pela plataforma dos clubes (master). Stripe Price IDs opcionais.</p>

      {listError ? (
        <p className="billing-page__error" role="alert">
          {listError}
        </p>
      ) : null}
      {listLoading ? <p className="bo-muted">Carregando…</p> : null}
      {!listLoading && items.length === 0 ? <p className="bo-muted">Nenhum plano nesta página.</p> : null}
      {!listLoading && items.length > 0 ? (
        <div className="bo-table-wrap">
          <table className="bo-table billing-page__table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Mensal</th>
                <th>Máx. sócios</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr key={row.id}>
                  <td>
                    <strong>{row.name}</strong>
                    {row.description ? <div className="admin-plans__cell-sub">{row.description}</div> : null}
                  </td>
                  <td>{row.monthlyPrice}</td>
                  <td>{row.maxMembers}</td>
                  <td>
                    <span className={row.isActive ? 'admin-plans__badge admin-plans__badge--on' : 'admin-plans__badge'}>
                      {row.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td>
                    <div className="admin-plans__row-actions">
                      <button type="button" disabled={formBusy || modalOpen} onClick={() => void startEdit(row.id)}>
                        Editar
                      </button>
                      <button
                        type="button"
                        disabled={toggleBusy || toggleConfirm !== null}
                        onClick={() => {
                          setListError(null)
                          setToggleConfirm({ id: row.id, name: row.name, isActive: row.isActive })
                        }}
                      >
                        {row.isActive ? 'Desativar' : 'Ativar'}
                      </button>
                    </div>
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
                {editingId ? 'Editar plano SaaS' : 'Novo plano SaaS'}
              </h2>
              <button type="button" className="admin-plans-modal__close" aria-label="Fechar" disabled={formBusy} onClick={closeModal}>
                ×
              </button>
            </div>
            {formError ? (
              <p className="billing-page__error admin-plans-modal__error" role="alert">
                {formError}
              </p>
            ) : null}
            <form className="admin-plans-modal__form" onSubmit={handleSubmit}>
              <label className="billing-page__field">
                Nome *
                <input ref={firstRef} className="auth-field__input" value={name} onChange={(e) => setName(e.target.value)} disabled={formBusy} required />
              </label>
              <label className="billing-page__field">
                Descrição
                <textarea className="admin-plans__textarea" rows={2} value={description} onChange={(e) => setDescription(e.target.value)} disabled={formBusy} />
              </label>
              <label className="billing-page__field">
                Preço mensal *
                <input className="auth-field__input" inputMode="decimal" value={monthlyPrice} onChange={(e) => setMonthlyPrice(e.target.value)} disabled={formBusy} required />
              </label>
              <label className="billing-page__field">
                Preço anual (opcional)
                <input className="auth-field__input" inputMode="decimal" value={yearlyPrice} onChange={(e) => setYearlyPrice(e.target.value)} disabled={formBusy} />
              </label>
              <label className="billing-page__field">
                Máx. sócios *
                <input className="auth-field__input" inputMode="numeric" value={maxMembers} onChange={(e) => setMaxMembers(e.target.value)} disabled={formBusy} required />
              </label>
              <label className="billing-page__field">
                Stripe Price ID mensal
                <input className="auth-field__input" value={stripeMonthly} onChange={(e) => setStripeMonthly(e.target.value)} disabled={formBusy} />
              </label>
              <label className="billing-page__field">
                Stripe Price ID anual
                <input className="auth-field__input" value={stripeYearly} onChange={(e) => setStripeYearly(e.target.value)} disabled={formBusy} />
              </label>
              <label className="billing-page__field">
                Features (uma por linha: chave | descrição | valor)
                <textarea className="admin-plans__textarea" rows={4} value={featuresText} onChange={(e) => setFeaturesText(e.target.value)} disabled={formBusy} />
              </label>
              <div className="admin-plans-modal__footer billing-page__actions">
                <button type="submit" disabled={formBusy}>
                  {editingId ? 'Salvar' : 'Criar'}
                </button>
                <button type="button" className="admin-plans__btn-secondary" disabled={formBusy} onClick={closeModal}>
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {toggleConfirm ? (
        <div
          className="admin-plans-modal admin-plans-modal--confirm"
          role="presentation"
          onClick={(e) => {
            if (e.target === e.currentTarget && !toggleBusy) setToggleConfirm(null)
          }}
        >
          <div
            className="admin-plans-modal__panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby={TOGGLE_TITLE}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="admin-plans-modal__header">
              <h2 id={TOGGLE_TITLE} className="admin-plans-modal__title">
                {toggleConfirm.isActive ? 'Desativar plano' : 'Ativar plano'}
              </h2>
              <button
                type="button"
                className="admin-plans-modal__close"
                aria-label="Fechar"
                disabled={toggleBusy}
                onClick={() => setToggleConfirm(null)}
              >
                ×
              </button>
            </div>
            <p className="admin-plans-modal__confirm-body">
              Confirmar alteração de status para <strong>{toggleConfirm.name}</strong>?
            </p>
            <div className="admin-plans-modal__confirm-actions">
              <button type="button" disabled={toggleBusy} onClick={() => void confirmToggle()}>
                Confirmar
              </button>
              <button type="button" className="admin-plans__btn-secondary" disabled={toggleBusy} onClick={() => setToggleConfirm(null)}>
                Cancelar
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  )
}
