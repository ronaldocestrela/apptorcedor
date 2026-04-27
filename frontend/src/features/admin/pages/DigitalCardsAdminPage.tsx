import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  getAdminDigitalCard,
  invalidateAdminDigitalCard,
  issueAdminDigitalCard,
  listAdminDigitalCardIssueCandidates,
  listAdminDigitalCards,
  regenerateAdminDigitalCard,
  type AdminDigitalCardDetail,
  type AdminDigitalCardIssueCandidateItem,
  type AdminDigitalCardListItem,
} from '../services/adminApi'

type CardStatusFilter = '' | 'Active' | 'Invalidated'

type DigitalCardIssueFormProps = {
  onIssued: () => Promise<void>
  busy: boolean
  setBusy: (v: boolean) => void
  setActionMessage: (m: string | null) => void
  setActionError: (e: string | null) => void
}

function DigitalCardIssueForm({
  onIssued,
  busy,
  setBusy,
  setActionMessage,
  setActionError,
}: DigitalCardIssueFormProps) {
  const [candidates, setCandidates] = useState<AdminDigitalCardIssueCandidateItem[]>([])
  const [loadingCandidates, setLoadingCandidates] = useState(true)
  const [candidatesError, setCandidatesError] = useState<string | null>(null)
  const [selectedMembershipId, setSelectedMembershipId] = useState('')

  const loadCandidates = useCallback(async () => {
    setLoadingCandidates(true)
    setCandidatesError(null)
    try {
      const res = await listAdminDigitalCardIssueCandidates({ page: 1, pageSize: 200 })
      setCandidates(res.items)
      setSelectedMembershipId((prev) => {
        if (prev && res.items.some(i => i.membershipId === prev))
          return prev
        return res.items[0]?.membershipId ?? ''
      })
    }
    catch {
      setCandidatesError('Falha ao carregar associações elegíveis.')
      setCandidates([])
      setSelectedMembershipId('')
    }
    finally {
      setLoadingCandidates(false)
    }
  }, [])

  useEffect(() => {
    void loadCandidates()
  }, [loadCandidates])

  async function onIssue(e: FormEvent) {
    e.preventDefault()
    if (!selectedMembershipId)
      return
    setBusy(true)
    setActionMessage(null)
    setActionError(null)
    try {
      await issueAdminDigitalCard(selectedMembershipId)
      setActionMessage('Carteirinha emitida.')
      await loadCandidates()
      await onIssued()
    }
    catch {
      setActionError('Emissão falhou (verifique se a associação ainda está elegível).')
    }
    finally {
      setBusy(false)
    }
  }

  function formatCandidateLabel(row: AdminDigitalCardIssueCandidateItem): string {
    const plan = row.planName?.trim() ? ` · ${row.planName}` : ''
    return `${row.userName} — ${row.userEmail}${plan}`
  }

  return (
    <section style={{ marginBottom: '1.5rem', padding: '1rem', background: '#f5f5f5', borderRadius: 8 }}>
      <h2 style={{ fontSize: '1rem', marginTop: 0 }}>Emitir carteirinha</h2>
      <p style={{ marginTop: 0, fontSize: '0.875rem', color: '#555' }}>
        Associações <strong>Ativas</strong> sem carteirinha ativa no momento.
      </p>
      {loadingCandidates ? <p>Carregando elegíveis…</p> : null}
      {candidatesError ? <p style={{ color: 'crimson' }}>{candidatesError}</p> : null}
      {!loadingCandidates && !candidatesError && candidates.length === 0
        ? (
            <p style={{ color: '#666' }}>Nenhuma associação elegível para emissão.</p>
          )
        : null}
      <form onSubmit={e => void onIssue(e)} style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'flex-end' }}>
        <label>
          Associação
          <select
            style={{ display: 'block', minWidth: 320 }}
            value={selectedMembershipId}
            onChange={ev => setSelectedMembershipId(ev.target.value)}
            disabled={loadingCandidates || candidates.length === 0}
            required
          >
            {candidates.length === 0
              ? <option value="">—</option>
              : candidates.map(row => (
                  <option key={row.membershipId} value={row.membershipId}>
                    {formatCandidateLabel(row)}
                  </option>
                ))}
          </select>
        </label>
        <button type="button" onClick={() => void loadCandidates()} disabled={busy || loadingCandidates}>
          Atualizar lista
        </button>
        <button type="submit" disabled={busy || loadingCandidates || candidates.length === 0}>
          Emitir
        </button>
      </form>
    </section>
  )
}

export function DigitalCardsAdminPage() {
  const [userIdFilter, setUserIdFilter] = useState('')
  const [membershipIdFilter, setMembershipIdFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<CardStatusFilter>('')
  const [page, setPage] = useState(1)
  const [list, setList] = useState<AdminDigitalCardListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loadingList, setLoadingList] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detail, setDetail] = useState<AdminDigitalCardDetail | null>(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState<string | null>(null)

  const [regenerateReason, setRegenerateReason] = useState('')
  const [invalidateReason, setInvalidateReason] = useState('')
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadList = useCallback(async () => {
    setLoadingList(true)
    setListError(null)
    try {
      const res = await listAdminDigitalCards({
        userId: userIdFilter.trim() || undefined,
        membershipId: membershipIdFilter.trim() || undefined,
        status: statusFilter || undefined,
        page,
        pageSize: 15,
      })
      setList(res.items)
      setTotalCount(res.totalCount)
    } catch {
      setListError('Falha ao carregar carteirinhas.')
    } finally {
      setLoadingList(false)
    }
  }, [userIdFilter, membershipIdFilter, statusFilter, page])

  useEffect(() => {
    void loadList()
  }, [loadList])

  const loadDetail = useCallback(async (id: string) => {
    setLoadingDetail(true)
    setDetailError(null)
    setActionMessage(null)
    setActionError(null)
    try {
      const d = await getAdminDigitalCard(id)
      setDetail(d)
    } catch {
      setDetail(null)
      setDetailError('Não foi possível carregar o detalhe.')
    } finally {
      setLoadingDetail(false)
    }
  }, [])

  useEffect(() => {
    if (selectedId) void loadDetail(selectedId)
    else setDetail(null)
  }, [selectedId, loadDetail])

  async function onRegenerate(e: FormEvent) {
    e.preventDefault()
    if (!detail || detail.status !== 'Active') return
    setBusy(true)
    setActionMessage(null)
    setActionError(null)
    try {
      const membershipId = detail.membershipId
      await regenerateAdminDigitalCard(detail.digitalCardId, {
        reason: regenerateReason.trim() || undefined,
      })
      setRegenerateReason('')
      setActionMessage('Carteirinha regenerada (nova versão ativa).')
      await loadList()
      const active = await listAdminDigitalCards({
        membershipId,
        status: 'Active',
        page: 1,
        pageSize: 5,
      })
      const next = active.items[0]
      if (next) setSelectedId(next.digitalCardId)
    } catch {
      setActionError('Regeneração falhou.')
    } finally {
      setBusy(false)
    }
  }

  async function onInvalidate(e: FormEvent) {
    e.preventDefault()
    if (!detail || detail.status !== 'Active') return
    setBusy(true)
    setActionMessage(null)
    setActionError(null)
    try {
      await invalidateAdminDigitalCard(detail.digitalCardId, { reason: invalidateReason.trim() })
      setInvalidateReason('')
      setActionMessage('Carteirinha invalidada.')
      setSelectedId(null)
      await loadList()
    } catch {
      setActionError('Invalidação falhou.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.CarteirinhaVisualizar, ApplicationPermissions.CarteirinhaGerenciar]}>
      <div>
        <h1 style={{ marginTop: 0 }}>Carteirinha digital (B.7)</h1>
        <p style={{ color: '#555', maxWidth: 720 }}>
          Layout de exibição é <strong>fixo no código</strong>. Emissão e regeneração exigem associação{' '}
          <strong>Ativa</strong>; regeneração invalida a versão anterior (novo token).
        </p>

        <section style={{ marginBottom: '1.5rem' }}>
          <h2 style={{ fontSize: '1rem' }}>Filtros</h2>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'flex-end' }}>
            <label>
              UserId
              <input
                style={{ display: 'block', minWidth: 280 }}
                value={userIdFilter}
                onChange={(ev) => setUserIdFilter(ev.target.value)}
                placeholder="guid opcional"
              />
            </label>
            <label>
              MembershipId
              <input
                style={{ display: 'block', minWidth: 280 }}
                value={membershipIdFilter}
                onChange={(ev) => setMembershipIdFilter(ev.target.value)}
                placeholder="guid opcional"
              />
            </label>
            <label>
              Status
              <select
                style={{ display: 'block', minWidth: 160 }}
                value={statusFilter}
                onChange={(ev) => setStatusFilter(ev.target.value as CardStatusFilter)}
              >
                <option value="">Todos</option>
                <option value="Active">Active</option>
                <option value="Invalidated">Invalidated</option>
              </select>
            </label>
            <button type="button" onClick={() => void loadList()}>
              Atualizar lista
            </button>
          </div>
        </section>

        <PermissionGate anyOf={[ApplicationPermissions.CarteirinhaGerenciar]}>
          <DigitalCardIssueForm
            busy={busy}
            setBusy={setBusy}
            setActionMessage={setActionMessage}
            setActionError={setActionError}
            onIssued={async () => {
              await loadList()
              if (selectedId)
                await loadDetail(selectedId)
            }}
          />
        </PermissionGate>

        {actionMessage ? <p style={{ color: 'green' }}>{actionMessage}</p> : null}
        {actionError ? <p style={{ color: 'crimson' }}>{actionError}</p> : null}

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
          <section>
            <h2 style={{ fontSize: '1rem' }}>Lista</h2>
            {loadingList ? <p>Carregando…</p> : null}
            {listError ? <p style={{ color: 'crimson' }}>{listError}</p> : null}
            {!loadingList && !listError && list.length === 0 ? <p>Nenhum registro.</p> : null}
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {list.map((row) => (
                <li key={row.digitalCardId} style={{ marginBottom: 8 }}>
                  <button
                    type="button"
                    onClick={() => setSelectedId(row.digitalCardId)}
                    style={{
                      textAlign: 'left',
                      width: '100%',
                      padding: 8,
                      border: selectedId === row.digitalCardId ? '2px solid #0b57d0' : '1px solid #ccc',
                      borderRadius: 6,
                      background: 'white',
                      cursor: 'pointer',
                    }}
                  >
                    <strong>v{row.version}</strong> — {row.status} — {row.userEmail}
                    <br />
                    <small style={{ color: '#666' }}>
                      membership {row.membershipStatus} · emitida {new Date(row.issuedAt).toLocaleString()}
                    </small>
                  </button>
                </li>
              ))}
            </ul>
            <div style={{ marginTop: 12, display: 'flex', gap: 8, alignItems: 'center' }}>
              <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                Anterior
              </button>
              <span>
                Página {page} · {totalCount} registro(s)
              </span>
              <button
                type="button"
                disabled={page * 15 >= totalCount}
                onClick={() => setPage((p) => p + 1)}
              >
                Próxima
              </button>
            </div>
          </section>

          <section>
            <h2 style={{ fontSize: '1rem' }}>Detalhe / preview (template fixo)</h2>
            {!selectedId ? <p>Selecione uma carteirinha na lista.</p> : null}
            {loadingDetail ? <p>Carregando detalhe…</p> : null}
            {detailError ? <p style={{ color: 'crimson' }}>{detailError}</p> : null}
            {detail ? (
              <div>
                <p>
                  <strong>Token</strong> <code style={{ wordBreak: 'break-all' }}>{detail.token}</code>
                </p>
                <p>
                  <strong>Status emissão:</strong> {detail.status} · <strong>Associação:</strong> {detail.membershipStatus}
                </p>
                <div
                  style={{
                    border: '1px solid #ccc',
                    borderRadius: 8,
                    padding: '1rem',
                    background: 'linear-gradient(135deg, #f0f4ff 0%, #ffffff 100%)',
                    fontFamily: 'system-ui',
                  }}
                >
                  {detail.templatePreviewLines.map((line) => (
                    <div key={line} style={{ marginBottom: 6 }}>
                      {line}
                    </div>
                  ))}
                </div>

                <PermissionGate anyOf={[ApplicationPermissions.CarteirinhaGerenciar]}>
                  {detail.status === 'Active' ? (
                    <div style={{ marginTop: '1rem' }}>
                      <h3 style={{ fontSize: '0.95rem' }}>Regenerar</h3>
                      <form onSubmit={(e) => void onRegenerate(e)}>
                        <textarea
                          style={{ width: '100%', minHeight: 56 }}
                          value={regenerateReason}
                          onChange={(ev) => setRegenerateReason(ev.target.value)}
                          placeholder="Motivo (opcional; padrão: Regeneração administrativa)"
                        />
                        <button type="submit" disabled={busy} style={{ marginTop: 8 }}>
                          Regerar (nova versão)
                        </button>
                      </form>
                      <h3 style={{ fontSize: '0.95rem' }}>Invalidar</h3>
                      <form onSubmit={(e) => void onInvalidate(e)}>
                        <textarea
                          style={{ width: '100%', minHeight: 56 }}
                          value={invalidateReason}
                          onChange={(ev) => setInvalidateReason(ev.target.value)}
                          placeholder="Motivo obrigatório (fraude, troca de documento, etc.)"
                          required
                        />
                        <button type="submit" disabled={busy} style={{ marginTop: 8 }}>
                          Invalidar versão ativa
                        </button>
                      </form>
                    </div>
                  ) : (
                    <p style={{ color: '#666', marginTop: '1rem' }}>Somente versões ativas podem ser regeneradas ou invalidadas.</p>
                  )}
                </PermissionGate>
              </div>
            ) : null}
          </section>
        </div>
      </div>
    </PermissionGate>
  )
}
