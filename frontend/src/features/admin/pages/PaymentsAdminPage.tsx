import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  cancelAdminPayment,
  conciliateAdminPayment,
  getAdminPayment,
  listAdminPayments,
  refundAdminPayment,
  type AdminPaymentDetail,
  type AdminPaymentListItem,
} from '../services/adminApi'

const statusFilterOptions = ['', 'Pending', 'Paid', 'Overdue', 'Cancelled', 'Refunded'] as const

export function PaymentsAdminPage() {
  const { user } = useAuth()
  const canView = hasPermission(user, ApplicationPermissions.PagamentosVisualizar)
  const canManage = hasPermission(user, ApplicationPermissions.PagamentosGerenciar)
  const canRefund = hasPermission(user, ApplicationPermissions.PagamentosEstornar)

  const [statusFilter, setStatusFilter] = useState<string>('')
  const [userIdFilter, setUserIdFilter] = useState('')
  const [membershipIdFilter, setMembershipIdFilter] = useState('')
  const [page, setPage] = useState(1)
  const [items, setItems] = useState<AdminPaymentListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detail, setDetail] = useState<AdminPaymentDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadList = useCallback(async () => {
    if (!canView)
      return
    setLoading(true)
    setError(null)
    try {
      const res = await listAdminPayments({
        status: statusFilter || undefined,
        userId: userIdFilter.trim() || undefined,
        membershipId: membershipIdFilter.trim() || undefined,
        page,
        pageSize: 20,
      })
      setItems(res.items)
      setTotalCount(res.totalCount)
    } catch {
      setError('Falha ao listar pagamentos.')
    } finally {
      setLoading(false)
    }
  }, [canView, statusFilter, userIdFilter, membershipIdFilter, page])

  useEffect(() => {
    void loadList()
  }, [loadList])

  const loadDetail = useCallback(
    async (paymentId: string) => {
      if (!canView)
        return
      setDetailLoading(true)
      setActionMessage(null)
      setActionError(null)
      try {
        const d = await getAdminPayment(paymentId)
        setDetail(d)
      } catch {
        setDetail(null)
        setActionError('Não foi possível carregar o pagamento.')
      } finally {
        setDetailLoading(false)
      }
    },
    [canView],
  )

  useEffect(() => {
    if (selectedId)
      void loadDetail(selectedId)
    else
      setDetail(null)
  }, [selectedId, loadDetail])

  async function onConciliate() {
    if (!selectedId || !detail)
      return
    setBusy(true)
    setActionError(null)
    setActionMessage(null)
    try {
      await conciliateAdminPayment(selectedId, {})
      setActionMessage('Pagamento conciliado.')
      await loadList()
      await loadDetail(selectedId)
    } catch (e) {
      if (axios.isAxiosError(e) && e.response?.status === 400)
        setActionError('Não foi possível conciliar (status inválido ou já pago).')
      else
        setActionError('Falha ao conciliar.')
    } finally {
      setBusy(false)
    }
  }

  async function onCancel() {
    if (!selectedId)
      return
    const reason = window.prompt('Motivo do cancelamento (opcional):') ?? ''
    setBusy(true)
    setActionError(null)
    setActionMessage(null)
    try {
      await cancelAdminPayment(selectedId, { reason: reason.trim() || null })
      setActionMessage('Cobrança cancelada.')
      await loadList()
      await loadDetail(selectedId)
    } catch {
      setActionError('Falha ao cancelar.')
    } finally {
      setBusy(false)
    }
  }

  async function onRefund() {
    if (!selectedId)
      return
    const reason = window.prompt('Motivo do estorno (opcional):') ?? ''
    setBusy(true)
    setActionError(null)
    setActionMessage(null)
    try {
      await refundAdminPayment(selectedId, { reason: reason.trim() || null })
      setActionMessage('Estorno registrado.')
      await loadList()
      await loadDetail(selectedId)
    } catch {
      setActionError('Falha ao estornar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate
      anyOf={[
        ApplicationPermissions.PagamentosVisualizar,
        ApplicationPermissions.PagamentosGerenciar,
        ApplicationPermissions.PagamentosEstornar,
      ]}
    >
      <h1 style={{ marginTop: 0 }}>Pagamentos (admin)</h1>
      <p style={{ color: '#444', maxWidth: 720 }}>
        Listagem de cobranças, conciliação manual, cancelamento e estorno. A inadimplência automática roda em segundo plano na API.
      </p>

      {canView ? (
        <section style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
            <label>
              Status
              <select
                value={statusFilter}
                onChange={(e) => {
                  setPage(1)
                  setStatusFilter(e.target.value)
                }}
                style={{ display: 'block', marginTop: 4 }}
              >
                {statusFilterOptions.map((s) => (
                  <option key={s || 'all'} value={s}>
                    {s || '(todos)'}
                  </option>
                ))}
              </select>
            </label>
            <label>
              UserId
              <input
                value={userIdFilter}
                onChange={(e) => {
                  setPage(1)
                  setUserIdFilter(e.target.value)
                }}
                style={{ display: 'block', marginTop: 4, width: 280 }}
                placeholder="GUID"
              />
            </label>
            <label>
              MembershipId
              <input
                value={membershipIdFilter}
                onChange={(e) => {
                  setPage(1)
                  setMembershipIdFilter(e.target.value)
                }}
                style={{ display: 'block', marginTop: 4, width: 280 }}
                placeholder="GUID"
              />
            </label>
            <button type="button" onClick={() => void loadList()}>
              Atualizar
            </button>
          </div>
        </section>
      ) : null}

      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando…</p> : null}

      {!loading && canView ? (
        <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'flex-start', flexWrap: 'wrap' }}>
          <div style={{ flex: '1 1 420px', overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
              <thead>
                <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                  <th>Vencimento</th>
                  <th>Valor</th>
                  <th>Status</th>
                  <th>Usuário</th>
                </tr>
              </thead>
              <tbody>
                {items.map((row) => (
                  <tr
                    key={row.paymentId}
                    onClick={() => setSelectedId(row.paymentId)}
                    style={{
                      cursor: 'pointer',
                      background: selectedId === row.paymentId ? '#e8f0fe' : undefined,
                      borderBottom: '1px solid #eee',
                    }}
                  >
                    <td style={{ padding: '6px 4px' }}>{new Date(row.dueDate).toLocaleString()}</td>
                    <td style={{ padding: '6px 4px' }}>{row.amount.toFixed(2)}</td>
                    <td style={{ padding: '6px 4px' }}>{row.status}</td>
                    <td style={{ padding: '6px 4px' }}>{row.userEmail}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <p style={{ fontSize: 13, color: '#666' }}>
              Total: {totalCount}
              {' '}
              — Página
              {' '}
              <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                ‹
              </button>
              {' '}
              {page}
              {' '}
              <button
                type="button"
                disabled={page * 20 >= totalCount}
                onClick={() => setPage((p) => p + 1)}
              >
                ›
              </button>
            </p>
          </div>

          <aside style={{ flex: '1 1 320px', border: '1px solid #ddd', padding: '1rem', borderRadius: 8, minWidth: 280 }}>
            <h2 style={{ marginTop: 0, fontSize: '1.1rem' }}>Detalhe</h2>
            {!selectedId ? <p>Selecione uma linha.</p> : null}
            {detailLoading ? <p>Carregando detalhe…</p> : null}
            {detail ? (
              <div style={{ fontSize: 14 }}>
                <p><strong>ID:</strong> {detail.paymentId}</p>
                <p><strong>Status:</strong> {detail.status}</p>
                <p><strong>Valor:</strong> {detail.amount.toFixed(2)}</p>
                <p><strong>Vencimento:</strong> {new Date(detail.dueDate).toLocaleString()}</p>
                <p><strong>Pago em:</strong> {detail.paidAt ? new Date(detail.paidAt).toLocaleString() : '—'}</p>
                <p><strong>Método:</strong> {detail.paymentMethod ?? '—'}</p>
                <p><strong>Ref. externa:</strong> {detail.externalReference ?? '—'}</p>
                <p><strong>Provedor:</strong> {detail.providerName ?? '—'}</p>
                <p><strong>Motivo / nota:</strong> {detail.statusReason ?? '—'}</p>
                <p><strong>Membership:</strong> {detail.membershipId}</p>
              </div>
            ) : null}
            {actionMessage ? <p style={{ color: 'green' }}>{actionMessage}</p> : null}
            {actionError ? <p style={{ color: 'crimson' }}>{actionError}</p> : null}
            {detail && (canManage || canRefund) ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 12 }}>
                <PermissionGate anyOf={[ApplicationPermissions.PagamentosGerenciar]}>
                  {(detail.status === 'Pending' || detail.status === 'Overdue') ? (
                    <>
                      <button type="button" disabled={busy} onClick={() => void onConciliate()}>
                        Conciliar (marcar pago)
                      </button>
                      <button type="button" disabled={busy} onClick={() => void onCancel()}>
                        Cancelar cobrança
                      </button>
                    </>
                  ) : null}
                </PermissionGate>
                <PermissionGate anyOf={[ApplicationPermissions.PagamentosEstornar]}>
                  {detail.status === 'Paid' ? (
                    <button type="button" disabled={busy} onClick={() => void onRefund()}>
                      Estornar
                    </button>
                  ) : null}
                </PermissionGate>
              </div>
            ) : null}
          </aside>
        </div>
      ) : null}

      {!canView ? (
        <p style={{ color: '#666' }}>Sem permissão de visualização. Peça <code>Pagamentos.Visualizar</code> ao administrador.</p>
      ) : null}
    </PermissionGate>
  )
}
