import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  getAdminMembership,
  listAdminMemberships,
  listMembershipHistory,
  updateMembershipStatus,
  type AdminMembershipDetail,
  type AdminMembershipListItem,
  type MembershipHistoryEvent,
  type MembershipStatus,
} from '../services/adminApi'

const statuses: MembershipStatus[] = ['NaoAssociado', 'Ativo', 'Inadimplente', 'Suspenso', 'Cancelado']

export function MembershipAdminPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const userIdFilter = searchParams.get('userId') ?? ''
  const membershipIdParam = searchParams.get('membershipId') ?? ''

  const [filterStatus, setFilterStatus] = useState<MembershipStatus | ''>('')
  const [userIdInput, setUserIdInput] = useState(userIdFilter)
  const [page, setPage] = useState(1)
  const [list, setList] = useState<AdminMembershipListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loadingList, setLoadingList] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [selectedId, setSelectedId] = useState<string | null>(membershipIdParam || null)
  const [detail, setDetail] = useState<AdminMembershipDetail | null>(null)
  const [history, setHistory] = useState<MembershipHistoryEvent[]>([])
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState<string | null>(null)

  const [newStatus, setNewStatus] = useState<MembershipStatus>('Ativo')
  const [reason, setReason] = useState('')
  const [saveMessage, setSaveMessage] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadList = useCallback(async () => {
    setLoadingList(true)
    setListError(null)
    try {
      const res = await listAdminMemberships({
        status: filterStatus || undefined,
        userId: userIdInput.trim() || undefined,
        page,
        pageSize: 15,
      })
      setList(res.items)
      setTotalCount(res.totalCount)
    } catch {
      setListError('Falha ao carregar lista.')
    } finally {
      setLoadingList(false)
    }
  }, [filterStatus, userIdInput, page])

  useEffect(() => {
    void loadList()
  }, [loadList])

  useEffect(() => {
    setUserIdInput(userIdFilter)
  }, [userIdFilter])

  const loadDetail = useCallback(async (membershipId: string) => {
    setLoadingDetail(true)
    setDetailError(null)
    setSaveMessage(null)
    setSaveError(null)
    try {
      const [d, h] = await Promise.all([
        getAdminMembership(membershipId),
        listMembershipHistory(membershipId, 50),
      ])
      setDetail(d)
      setHistory(h)
      setNewStatus((d.status as MembershipStatus) ?? 'NaoAssociado')
    } catch {
      setDetail(null)
      setHistory([])
      setDetailError('Não foi possível carregar esta associação.')
    } finally {
      setLoadingDetail(false)
    }
  }, [])

  useEffect(() => {
    if (membershipIdParam) {
      setSelectedId(membershipIdParam)
      void loadDetail(membershipIdParam)
    }
  }, [membershipIdParam, loadDetail])

  function applyUserFilterToUrl() {
    const next = new URLSearchParams(searchParams)
    const t = userIdInput.trim()
    if (t)
      next.set('userId', t)
    else
      next.delete('userId')
    next.delete('membershipId')
    setSearchParams(next)
    setSelectedId(null)
    setDetail(null)
    setHistory([])
    setPage(1)
  }

  function selectRow(row: AdminMembershipListItem) {
    setSelectedId(row.membershipId)
    const next = new URLSearchParams(searchParams)
    next.set('membershipId', row.membershipId)
    setSearchParams(next)
    void loadDetail(row.membershipId)
  }

  async function onSubmitStatus(e: FormEvent) {
    e.preventDefault()
    if (!selectedId || !reason.trim()) {
      setSaveError('Informe o motivo da alteração.')
      return
    }
    setBusy(true)
    setSaveMessage(null)
    setSaveError(null)
    try {
      await updateMembershipStatus(selectedId, newStatus, reason.trim())
      setReason('')
      setSaveMessage('Status atualizado.')
      await loadDetail(selectedId)
      await loadList()
    } catch {
      setSaveError('Falha ao atualizar (verifique permissões ou se o status mudou).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.SociosGerenciar]}>
      <h1>Membership — administração</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        Consulta de associações, histórico operacional e alteração manual de status (com motivo obrigatório).
        Plano e datas são somente leitura nesta fase.
      </p>

      <section style={{ marginBottom: 24, display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
        <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
          Filtrar por status
          <select
            value={filterStatus}
            onChange={(e) => {
              setFilterStatus(e.target.value as MembershipStatus | '')
              setPage(1)
            }}
            style={{ marginTop: 4, minWidth: 180 }}
          >
            <option value="">Todos</option>
            {statuses.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </label>
        <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
          UserId (GUID)
          <input
            value={userIdInput}
            onChange={(e) => setUserIdInput(e.target.value)}
            placeholder="opcional"
            style={{ marginTop: 4, minWidth: 280 }}
          />
        </label>
        <button type="button" onClick={() => void applyUserFilterToUrl()}>Aplicar filtro usuário</button>
        <button type="button" onClick={() => { setPage(1); void loadList() }} disabled={loadingList}>Atualizar lista</button>
      </section>

      {listError ? <p style={{ color: 'crimson' }}>{listError}</p> : null}
      {loadingList ? <p>Carregando lista...</p> : null}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, alignItems: 'start' }}>
        <div style={{ overflowX: 'auto' }}>
          <p style={{ fontSize: 14, color: '#555' }}>
            Total:
            {' '}
            {totalCount}
          </p>
          <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 13 }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                <th style={{ padding: 6 }}>Membro</th>
                <th style={{ padding: 6 }}>Status</th>
                <th style={{ padding: 6 }}>Id</th>
              </tr>
            </thead>
            <tbody>
              {list.map((row) => (
                <tr
                  key={row.membershipId}
                  style={{
                    borderBottom: '1px solid #eee',
                    cursor: 'pointer',
                    background: row.membershipId === selectedId ? '#f0f7ff' : undefined,
                  }}
                  onClick={() => selectRow(row)}
                >
                  <td style={{ padding: 6 }}>
                    <div>{row.userName}</div>
                    <div style={{ color: '#666' }}>{row.userEmail}</div>
                  </td>
                  <td style={{ padding: 6 }}>{row.status}</td>
                  <td style={{ padding: 6, fontFamily: 'monospace', fontSize: 11 }}>{row.membershipId}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ marginTop: 12, display: 'flex', gap: 8, alignItems: 'center' }}>
            <button type="button" disabled={page <= 1 || loadingList} onClick={() => setPage((p) => Math.max(1, p - 1))}>Anterior</button>
            <span style={{ fontSize: 14 }}>Página {page}</span>
            <button
              type="button"
              disabled={loadingList || page * 15 >= totalCount}
              onClick={() => setPage((p) => p + 1)}
            >
              Próxima
            </button>
          </div>
        </div>

        <div>
          {!selectedId ? <p style={{ color: '#555' }}>Selecione uma linha na lista ou abra com ?membershipId= na URL.</p> : null}
          {detailError ? <p style={{ color: 'crimson' }}>{detailError}</p> : null}
          {loadingDetail ? <p>Carregando detalhe...</p> : null}
          {detail ? (
            <>
              <h2 style={{ marginTop: 0 }}>Detalhe</h2>
              <ul style={{ lineHeight: 1.6, fontSize: 14 }}>
                <li><strong>Usuário:</strong> {detail.userName} ({detail.userEmail})</li>
                <li><strong>UserId:</strong> {detail.userId}</li>
                <li><strong>Status:</strong> {detail.status}</li>
                <li><strong>Plano:</strong> {detail.planId ?? '—'}</li>
                <li><strong>Início:</strong> {new Date(detail.startDate).toLocaleString()}</li>
                <li><strong>Fim:</strong> {detail.endDate ? new Date(detail.endDate).toLocaleString() : '—'}</li>
                <li><strong>Próx. venc.:</strong> {detail.nextDueDate ? new Date(detail.nextDueDate).toLocaleString() : '—'}</li>
                <li>
                  <Link to={`/admin/users/${encodeURIComponent(detail.userId)}`}>Abrir ficha do usuário</Link>
                </li>
              </ul>

              <h3>Alterar status</h3>
              <form onSubmit={(e) => void onSubmitStatus(e)} style={{ display: 'grid', gap: 12, maxWidth: 400 }}>
                <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                  Novo status
                  <select value={newStatus} onChange={(e) => setNewStatus(e.target.value as MembershipStatus)} style={{ marginTop: 4 }}>
                    {statuses.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </label>
                <label style={{ display: 'flex', flexDirection: 'column', fontSize: 14 }}>
                  Motivo (obrigatório)
                  <textarea
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    rows={3}
                    required
                    style={{ marginTop: 4 }}
                  />
                </label>
                {saveMessage ? <p style={{ color: 'green' }}>{saveMessage}</p> : null}
                {saveError ? <p role="alert" style={{ color: 'crimson' }}>{saveError}</p> : null}
                <button type="submit" disabled={busy}>{busy ? 'Salvando...' : 'Aplicar alteração'}</button>
              </form>

              <h3>Histórico operacional</h3>
              <div style={{ overflowX: 'auto' }}>
                <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 12 }}>
                  <thead>
                    <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                      <th style={{ padding: 4 }}>Quando</th>
                      <th style={{ padding: 4 }}>De → Para</th>
                      <th style={{ padding: 4 }}>Motivo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {history.map((ev) => (
                      <tr key={ev.id} style={{ borderBottom: '1px solid #eee' }}>
                        <td style={{ padding: 4 }}>{new Date(ev.createdAt).toLocaleString()}</td>
                        <td style={{ padding: 4 }}>
                          {ev.fromStatus ?? '—'}
                          {' → '}
                          {ev.toStatus}
                        </td>
                        <td style={{ padding: 4 }}>{ev.reason}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {history.length === 0 ? <p style={{ fontSize: 14 }}>Nenhum evento registrado.</p> : null}
            </>
          ) : null}
        </div>
      </div>
    </PermissionGate>
  )
}
