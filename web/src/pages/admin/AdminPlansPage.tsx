import { useCallback, useEffect, useRef, useState, type FormEvent, type MouseEvent } from 'react'
import { getApiErrorMessage } from '../../shared/auth'
import {
  createMemberPlan,
  getMemberPlan,
  listMemberPlans,
  toggleMemberPlan,
  updateMemberPlan,
  type MemberPlanDto,
} from '../../shared/membership/plansApi'

const PAGE_SIZE = 10
const MODAL_TITLE_ID = 'admin-plans-modal-title'
const TOGGLE_CONFIRM_TITLE_ID = 'admin-plans-toggle-confirm-title'

function parseVantagensLines(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter((l) => l.length > 0)
}

function vantagensToText(v: MemberPlanDto['vantagens']): string {
  return v.map((x) => x.descricao).join('\n')
}

function validateForm(
  nome: string,
  descricao: string,
  precoStr: string,
  vantagensText: string,
): string | null {
  const nomeTrim = nome.trim()
  if (!nomeTrim) return 'Informe o nome do plano.'
  if (nomeTrim.length > 200) return 'Nome: no máximo 200 caracteres.'
  if (descricao.length > 2000) return 'Descrição: no máximo 2000 caracteres.'
  const preco = Number(precoStr.replace(',', '.'))
  if (Number.isNaN(preco) || preco < 0) return 'Preço deve ser um número maior ou igual a zero.'
  const lines = parseVantagensLines(vantagensText)
  for (const line of lines) {
    if (line.length > 300) return 'Cada vantagem: no máximo 300 caracteres.'
  }
  return null
}

export function AdminPlansPage() {
  const [items, setItems] = useState<MemberPlanDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [nome, setNome] = useState('')
  const [descricao, setDescricao] = useState('')
  const [preco, setPreco] = useState('0')
  const [vantagensText, setVantagensText] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [formBusy, setFormBusy] = useState(false)

  const [toggleConfirm, setToggleConfirm] = useState<{
    id: string
    nome: string
    isActive: boolean
  } | null>(null)
  const [toggleBusy, setToggleBusy] = useState(false)

  const firstFieldRef = useRef<HTMLInputElement>(null)
  const toggleCancelRef = useRef<HTMLButtonElement>(null)

  const loadList = useCallback(async (p: number) => {
    setListError(null)
    setListLoading(true)
    try {
      const res = await listMemberPlans(p, PAGE_SIZE)
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
    setNome('')
    setDescricao('')
    setPreco('0')
    setVantagensText('')
    setFormError(null)
  }

  const closeModal = useCallback(() => {
    if (formBusy) return
    setModalOpen(false)
    resetForm()
  }, [formBusy])

  function openCreateModal() {
    resetForm()
    setModalOpen(true)
  }

  useEffect(() => {
    if (!modalOpen) return
    const t = window.setTimeout(() => firstFieldRef.current?.focus(), 0)
    return () => window.clearTimeout(t)
  }, [modalOpen])

  useEffect(() => {
    if (!toggleConfirm) return
    const t = window.setTimeout(() => toggleCancelRef.current?.focus(), 0)
    return () => window.clearTimeout(t)
  }, [toggleConfirm])

  const closeToggleConfirm = useCallback(() => {
    if (toggleBusy) return
    setToggleConfirm(null)
  }, [toggleBusy])

  useEffect(() => {
    if (!modalOpen && !toggleConfirm) return
    function onKey(e: KeyboardEvent) {
      if (e.key !== 'Escape') return
      if (toggleConfirm && !toggleBusy) {
        closeToggleConfirm()
        return
      }
      if (modalOpen) closeModal()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [modalOpen, toggleConfirm, toggleBusy, closeModal, closeToggleConfirm])

  useEffect(() => {
    if (!modalOpen && !toggleConfirm) return
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = prev
    }
  }, [modalOpen, toggleConfirm])

  async function startEdit(planId: string) {
    setFormError(null)
    setFormBusy(true)
    try {
      const p = await getMemberPlan(planId)
      setEditingId(p.id)
      setNome(p.nome)
      setDescricao(p.descricao ?? '')
      setPreco(String(p.preco))
      setVantagensText(vantagensToText(p.vantagens))
      setModalOpen(true)
    } catch (e: unknown) {
      setListError(getApiErrorMessage(e, 'Não foi possível carregar o plano para edição.'))
    } finally {
      setFormBusy(false)
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setFormError(null)
    const err = validateForm(nome, descricao, preco, vantagensText)
    if (err) {
      setFormError(err)
      return
    }
    const precoNum = Number(preco.replace(',', '.'))
    const vantagens = parseVantagensLines(vantagensText)
    const body = {
      nome: nome.trim(),
      descricao: descricao.trim() || null,
      preco: precoNum,
      vantagens: vantagens.length ? vantagens : null,
    }
    setFormBusy(true)
    try {
      if (editingId) {
        await updateMemberPlan(editingId, body)
      } else {
        await createMemberPlan(body)
      }
      setModalOpen(false)
      resetForm()
      await loadList(page)
    } catch (e: unknown) {
      setFormError(getApiErrorMessage(e, editingId ? 'Não foi possível atualizar o plano.' : 'Não foi possível criar o plano.'))
    } finally {
      setFormBusy(false)
    }
  }

  async function confirmToggle() {
    if (!toggleConfirm) return
    setToggleBusy(true)
    setListError(null)
    try {
      await toggleMemberPlan(toggleConfirm.id)
      setToggleConfirm(null)
      await loadList(page)
    } catch (e: unknown) {
      setListError(getApiErrorMessage(e, 'Não foi possível alterar o status do plano.'))
    } finally {
      setToggleBusy(false)
    }
  }

  function onBackdropClick(e: MouseEvent<HTMLDivElement>) {
    if (e.target !== e.currentTarget || formBusy) return
    closeModal()
  }

  function onToggleConfirmBackdrop(e: MouseEvent<HTMLDivElement>) {
    if (e.target !== e.currentTarget || toggleBusy) return
    closeToggleConfirm()
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <section className="admin-plans">
      <h1>Planos de sócio</h1>
      <p className="admin-plans__hint">
        Cadastre e gerencie os planos oferecidos pelo clube. Apenas administradores podem criar, editar e
        ativar/desativar planos.
      </p>

      <div className="billing-page__block admin-plans__list-block">
        <div className="admin-plans__list-toolbar">
          <h2 className="admin-plans__list-heading">Planos cadastrados</h2>
          <button
            type="button"
            className="admin-plans__btn-new"
            disabled={formBusy || toggleConfirm !== null || modalOpen}
            onClick={openCreateModal}
          >
            Novo plano
          </button>
        </div>
        {listError ? (
          <p className="billing-page__error" role="alert">
            {listError}
          </p>
        ) : null}
        {listLoading ? <p className="admin-plans__muted">Carregando…</p> : null}
        {!listLoading && items.length === 0 ? (
          <p className="admin-plans__muted">Nenhum plano encontrado nesta página.</p>
        ) : null}
        {!listLoading && items.length > 0 ? (
          <div className="admin-plans__table-wrap">
            <table className="billing-page__table admin-plans__table">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Preço</th>
                  <th>Status</th>
                  <th>Vantagens</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {items.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <strong>{row.nome}</strong>
                      {row.descricao ? (
                        <div className="admin-plans__cell-sub">{row.descricao}</div>
                      ) : null}
                    </td>
                    <td>{row.preco.toFixed(2)} BRL</td>
                    <td>
                      <span className={row.isActive ? 'admin-plans__badge admin-plans__badge--on' : 'admin-plans__badge'}>
                        {row.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td>{row.vantagens.length}</td>
                    <td>
                      <div className="admin-plans__row-actions">
                        <button
                          type="button"
                          disabled={formBusy || toggleConfirm !== null || modalOpen}
                          onClick={() => void startEdit(row.id)}
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          disabled={toggleBusy || toggleConfirm !== null || modalOpen}
                          onClick={() => {
                            setListError(null)
                            setToggleConfirm({
                              id: row.id,
                              nome: row.nome,
                              isActive: row.isActive,
                            })
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
          <div className="admin-plans__pager">
            <button
              type="button"
              disabled={page <= 1 || listLoading}
              onClick={() => void loadList(page - 1)}
            >
              Anterior
            </button>
            <span className="admin-plans__pager-info">
              Página {page} de {totalPages} ({totalCount} planos)
            </span>
            <button
              type="button"
              disabled={page >= totalPages || listLoading}
              onClick={() => void loadList(page + 1)}
            >
              Próxima
            </button>
          </div>
        ) : null}
      </div>

      {modalOpen ? (
        <div
          className="admin-plans-modal"
          role="presentation"
          onClick={onBackdropClick}
        >
          <div
            className="admin-plans-modal__panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby={MODAL_TITLE_ID}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="admin-plans-modal__header">
              <h2 id={MODAL_TITLE_ID} className="admin-plans-modal__title">
                {editingId ? 'Editar plano' : 'Novo plano'}
              </h2>
              <button
                type="button"
                className="admin-plans-modal__close"
                aria-label="Fechar"
                disabled={formBusy}
                onClick={closeModal}
              >
                ×
              </button>
            </div>
            {formError ? (
              <p className="billing-page__error admin-plans-modal__error" role="alert">
                {formError}
              </p>
            ) : null}
            <form className="admin-plans__form admin-plans-modal__form" onSubmit={handleSubmit}>
              <label className="billing-page__field">
                Nome *
                <input
                  ref={firstFieldRef}
                  className="auth-field__input"
                  type="text"
                  value={nome}
                  onChange={(e) => setNome(e.target.value)}
                  maxLength={200}
                  disabled={formBusy}
                  required
                />
              </label>
              <label className="billing-page__field">
                Descrição
                <textarea
                  className="admin-plans__textarea"
                  value={descricao}
                  onChange={(e) => setDescricao(e.target.value)}
                  maxLength={2000}
                  rows={3}
                  disabled={formBusy}
                />
              </label>
              <label className="billing-page__field">
                Preço (BRL) *
                <input
                  className="auth-field__input"
                  type="text"
                  inputMode="decimal"
                  value={preco}
                  onChange={(e) => setPreco(e.target.value)}
                  disabled={formBusy}
                  required
                />
              </label>
              <label className="billing-page__field">
                Vantagens (uma por linha, até 300 caracteres cada)
                <textarea
                  className="admin-plans__textarea"
                  value={vantagensText}
                  onChange={(e) => setVantagensText(e.target.value)}
                  rows={5}
                  disabled={formBusy}
                />
              </label>
              <div className="billing-page__actions admin-plans__form-actions admin-plans-modal__footer">
                <button type="submit" disabled={formBusy}>
                  {editingId ? 'Salvar alterações' : 'Cadastrar plano'}
                </button>
                <button
                  type="button"
                  className="admin-plans__btn-secondary"
                  disabled={formBusy}
                  onClick={closeModal}
                >
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
          onClick={onToggleConfirmBackdrop}
        >
          <div
            className="admin-plans-modal__panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby={TOGGLE_CONFIRM_TITLE_ID}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="admin-plans-modal__header">
              <h2 id={TOGGLE_CONFIRM_TITLE_ID} className="admin-plans-modal__title">
                {toggleConfirm.isActive ? 'Desativar plano' : 'Ativar plano'}
              </h2>
              <button
                type="button"
                className="admin-plans-modal__close"
                aria-label="Fechar"
                disabled={toggleBusy}
                onClick={closeToggleConfirm}
              >
                ×
              </button>
            </div>
            <p className="admin-plans-modal__confirm-body">
              {toggleConfirm.isActive ? (
                <>
                  Deseja <strong>desativar</strong> o plano <strong>{toggleConfirm.nome}</strong>? Ele deixará de aparecer
                  como opção ativa para novas assinaturas.
                </>
              ) : (
                <>
                  Deseja <strong>ativar</strong> o plano <strong>{toggleConfirm.nome}</strong>?
                </>
              )}
            </p>
            <div className="admin-plans-modal__confirm-actions">
              <button type="button" disabled={toggleBusy} onClick={() => void confirmToggle()}>
                Confirmar
              </button>
              <button
                ref={toggleCancelRef}
                type="button"
                className="admin-plans__btn-secondary"
                disabled={toggleBusy}
                onClick={closeToggleConfirm}
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  )
}
