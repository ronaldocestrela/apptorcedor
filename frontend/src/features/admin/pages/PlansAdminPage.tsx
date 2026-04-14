import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  createAdminPlan,
  getAdminPlan,
  listAdminPlans,
  updateAdminPlan,
  type AdminPlanListItem,
  type UpsertPlanBody,
} from '../services/adminApi'

const billingOptions = ['Monthly', 'Yearly', 'Quarterly'] as const

type BenefitDraft = { sortOrder: number; title: string; description: string }

function emptyForm(): {
  planId: string | null
  name: string
  price: string
  billingCycle: string
  discountPercentage: string
  isActive: boolean
  isPublished: boolean
  summary: string
  rulesNotes: string
  benefits: BenefitDraft[]
} {
  return {
    planId: null,
    name: '',
    price: '0',
    billingCycle: 'Monthly',
    discountPercentage: '0',
    isActive: true,
    isPublished: false,
    summary: '',
    rulesNotes: '',
    benefits: [],
  }
}

export function PlansAdminPage() {
  const { user } = useAuth()
  const canCreate = hasPermission(user, ApplicationPermissions.PlanosCriar)
  const canEdit = hasPermission(user, ApplicationPermissions.PlanosEditar)

  const [items, setItems] = useState<AdminPlanListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const [form, setForm] = useState(() => emptyForm())

  const loadList = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await listAdminPlans({ pageSize: 100 })
      setItems(page.items)
      setTotalCount(page.totalCount)
    } catch {
      setError('Falha ao listar planos.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadList()
  }, [loadList])

  async function selectPlan(planId: string) {
    setError(null)
    try {
      const d = await getAdminPlan(planId)
      setForm({
        planId: d.planId,
        name: d.name,
        price: String(d.price),
        billingCycle: d.billingCycle,
        discountPercentage: String(d.discountPercentage),
        isActive: d.isActive,
        isPublished: d.isPublished,
        summary: d.summary ?? '',
        rulesNotes: d.rulesNotes ?? '',
        benefits: d.benefits.map((b) => ({
          sortOrder: b.sortOrder,
          title: b.title,
          description: b.description ?? '',
        })),
      })
    } catch {
      setError('Falha ao carregar plano.')
    }
  }

  function startNew() {
    setForm(emptyForm())
    setError(null)
  }

  function toBody(): UpsertPlanBody {
    const benefits = form.benefits.map((b, i) => ({
      sortOrder: b.sortOrder || i,
      title: b.title.trim(),
      description: b.description.trim() || null,
    }))
    return {
      name: form.name.trim(),
      price: Number(form.price.replace(',', '.')),
      billingCycle: form.billingCycle,
      discountPercentage: Number(form.discountPercentage.replace(',', '.')),
      isActive: form.isActive,
      isPublished: form.isPublished,
      summary: form.summary.trim() || null,
      rulesNotes: form.rulesNotes.trim() || null,
      benefits,
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const body = toBody()
      if (!body.name)
        throw new Error('Nome é obrigatório.')
      if (form.planId) {
        if (!canEdit)
          throw new Error('Sem permissão para editar.')
        await updateAdminPlan(form.planId, body)
      } else {
        if (!canCreate)
          throw new Error('Sem permissão para criar.')
        const { planId } = await createAdminPlan(body)
        await loadList()
        await selectPlan(planId)
        return
      }
      await loadList()
    } catch (err: unknown) {
      let msg = 'Falha ao salvar.'
      if (axios.isAxiosError(err) && err.response?.data && typeof err.response.data === 'object' && err.response.data !== null) {
        const d = err.response.data as { error?: string; title?: string }
        if (typeof d.error === 'string')
          msg = d.error
        else if (typeof d.title === 'string')
          msg = d.title
      } else if (err instanceof Error)
        msg = err.message
      setError(msg)
    } finally {
      setSaving(false)
    }
  }

  function addBenefit() {
    const nextOrder = form.benefits.length === 0 ? 0 : Math.max(...form.benefits.map((b) => b.sortOrder)) + 1
    setForm((f) => ({
      ...f,
      benefits: [...f.benefits, { sortOrder: nextOrder, title: '', description: '' }],
    }))
  }

  function removeBenefit(index: number) {
    setForm((f) => ({
      ...f,
      benefits: f.benefits.filter((_, i) => i !== index),
    }))
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.PlanosVisualizar]}>
      <h1>Planos (oferta)</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        <strong>Ativo</strong> controla se o plano pode ser usado na operação; <strong>Publicado</strong> define visibilidade no catálogo do torcedor (Parte D).
        Não é possível publicar um plano inativo.
      </p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      <div style={{ display: 'flex', gap: 24, alignItems: 'flex-start', flexWrap: 'wrap' }}>
        <section style={{ flex: '1 1 320px', minWidth: 280 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h2 style={{ fontSize: '1rem' }}>Lista ({totalCount})</h2>
            {canCreate ? (
              <button type="button" onClick={() => startNew()}>Novo plano</button>
            ) : null}
          </div>
          {loading ? <p>Carregando...</p> : null}
          {!loading && items.length === 0 ? <p>Nenhum plano cadastrado.</p> : null}
          <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 13 }}>
            <thead>
              <tr>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Nome</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Preço</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Ativo</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Publ.</th>
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr
                  key={row.planId}
                  style={{
                    cursor: 'pointer',
                    background: form.planId === row.planId ? '#f0f7ff' : undefined,
                  }}
                >
                  <td
                    style={{ padding: 6 }}
                    onClick={() => void selectPlan(row.planId)}
                    onKeyDown={(e) => e.key === 'Enter' && void selectPlan(row.planId)}
                    role="button"
                    tabIndex={0}
                  >
                    {row.name}
                  </td>
                  <td style={{ padding: 6 }} onClick={() => void selectPlan(row.planId)}>{row.price}</td>
                  <td style={{ padding: 6 }} onClick={() => void selectPlan(row.planId)}>{row.isActive ? 'sim' : 'não'}</td>
                  <td style={{ padding: 6 }} onClick={() => void selectPlan(row.planId)}>{row.isPublished ? 'sim' : 'não'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section style={{ flex: '1 1 400px', minWidth: 300, maxWidth: 560 }}>
          <h2 style={{ fontSize: '1rem' }}>{form.planId ? 'Editar plano' : 'Novo plano'}</h2>
          <form onSubmit={(e) => void onSubmit(e)} style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            <label>
              Nome
              <input
                style={{ display: 'block', width: '100%' }}
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              />
            </label>
            <label>
              Preço
              <input
                style={{ display: 'block', width: '100%' }}
                value={form.price}
                onChange={(e) => setForm((f) => ({ ...f, price: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              />
            </label>
            <label>
              Ciclo de cobrança
              <select
                style={{ display: 'block', width: '100%' }}
                value={form.billingCycle}
                onChange={(e) => setForm((f) => ({ ...f, billingCycle: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              >
                {billingOptions.map((o) => (
                  <option key={o} value={o}>{o}</option>
                ))}
              </select>
            </label>
            <label>
              Desconto (%)
              <input
                style={{ display: 'block', width: '100%' }}
                value={form.discountPercentage}
                onChange={(e) => setForm((f) => ({ ...f, discountPercentage: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              />
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                disabled={!!form.planId && !canEdit}
              />
              Ativo
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="checkbox"
                checked={form.isPublished}
                onChange={(e) => setForm((f) => ({ ...f, isPublished: e.target.checked }))}
                disabled={!!form.planId && !canEdit}
              />
              Publicado (catálogo torcedor)
            </label>
            <label>
              Resumo (catálogo)
              <textarea
                style={{ display: 'block', width: '100%' }}
                rows={3}
                value={form.summary}
                onChange={(e) => setForm((f) => ({ ...f, summary: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              />
            </label>
            <label>
              Regras / notas operacionais
              <textarea
                style={{ display: 'block', width: '100%' }}
                rows={3}
                value={form.rulesNotes}
                onChange={(e) => setForm((f) => ({ ...f, rulesNotes: e.target.value }))}
                disabled={!!form.planId && !canEdit}
              />
            </label>

            <div>
              <strong>Benefícios</strong>
              {canEdit || !form.planId ? (
                <button type="button" onClick={addBenefit} style={{ marginLeft: 8 }}>+ item</button>
              ) : null}
              {form.benefits.map((b, idx) => (
                <div key={idx} style={{ border: '1px solid #ddd', padding: 8, marginTop: 8, borderRadius: 4 }}>
                  <label>
                    Ordem
                    <input
                      type="number"
                      style={{ display: 'block', width: '100%' }}
                      value={b.sortOrder}
                      onChange={(e) => setForm((f) => {
                        const benefits = [...f.benefits]
                        benefits[idx] = { ...benefits[idx], sortOrder: Number(e.target.value) }
                        return { ...f, benefits }
                      })}
                      disabled={!!form.planId && !canEdit}
                    />
                  </label>
                  <label>
                    Título
                    <input
                      style={{ display: 'block', width: '100%' }}
                      value={b.title}
                      onChange={(e) => setForm((f) => {
                        const benefits = [...f.benefits]
                        benefits[idx] = { ...benefits[idx], title: e.target.value }
                        return { ...f, benefits }
                      })}
                      disabled={!!form.planId && !canEdit}
                    />
                  </label>
                  <label>
                    Descrição
                    <input
                      style={{ display: 'block', width: '100%' }}
                      value={b.description}
                      onChange={(e) => setForm((f) => {
                        const benefits = [...f.benefits]
                        benefits[idx] = { ...benefits[idx], description: e.target.value }
                        return { ...f, benefits }
                      })}
                      disabled={!!form.planId && !canEdit}
                    />
                  </label>
                  {(canEdit || !form.planId) ? (
                    <button type="button" onClick={() => removeBenefit(idx)}>Remover</button>
                  ) : null}
                </div>
              ))}
            </div>

            {(!form.planId && canCreate) || (form.planId && canEdit) ? (
              <button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Salvar'}</button>
            ) : (
              <p style={{ color: '#666', fontSize: 13 }}>Somente leitura (sem Planos.Criar / Planos.Editar).</p>
            )}
          </form>
        </section>
      </div>
    </PermissionGate>
  )
}
