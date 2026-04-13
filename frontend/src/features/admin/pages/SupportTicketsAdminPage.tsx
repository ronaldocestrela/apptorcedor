import { useCallback, useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  assignAdminSupportTicket,
  changeAdminSupportTicketStatus,
  createAdminSupportTicket,
  getAdminSupportTicket,
  listAdminSupportTickets,
  replyAdminSupportTicket,
  type AdminSupportTicketDetail,
  type AdminSupportTicketListItem,
  type SupportTicketPriority,
  type SupportTicketStatus,
} from '../services/adminApi'

const statusOptions: SupportTicketStatus[] = [
  'Open',
  'InProgress',
  'WaitingUser',
  'Resolved',
  'Closed',
]

const priorityOptions: SupportTicketPriority[] = ['Normal', 'High', 'Urgent']

export function SupportTicketsAdminPage() {
  const [items, setItems] = useState<AdminSupportTicketListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [queueFilter, setQueueFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<SupportTicketStatus | ''>('')
  const [unassignedOnly, setUnassignedOnly] = useState(false)
  const [slaBreachedOnly, setSlaBreachedOnly] = useState(false)
  const [page, setPage] = useState(1)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detail, setDetail] = useState<AdminSupportTicketDetail | null>(null)
  const [detailError, setDetailError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionOk, setActionOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [replyBody, setReplyBody] = useState('')
  const [replyInternal, setReplyInternal] = useState(false)
  const [assignAgentId, setAssignAgentId] = useState('')
  const [newStatus, setNewStatus] = useState<SupportTicketStatus>('InProgress')
  const [statusReason, setStatusReason] = useState('')

  const [createRequesterId, setCreateRequesterId] = useState('')
  const [createQueue, setCreateQueue] = useState('Geral')
  const [createSubject, setCreateSubject] = useState('')
  const [createPriority, setCreatePriority] = useState<SupportTicketPriority>('Normal')
  const [createMessage, setCreateMessage] = useState('')

  const loadList = useCallback(async () => {
    setLoading(true)
    setListError(null)
    try {
      const p = await listAdminSupportTickets({
        queue: queueFilter.trim() || undefined,
        status: statusFilter || undefined,
        unassignedOnly: unassignedOnly || undefined,
        slaBreachedOnly: slaBreachedOnly || undefined,
        page,
        pageSize: 20,
      })
      setItems(p.items)
      setTotalCount(p.totalCount)
    } catch {
      setListError('Falha ao listar chamados.')
    } finally {
      setLoading(false)
    }
  }, [queueFilter, statusFilter, unassignedOnly, slaBreachedOnly, page])

  const loadDetail = useCallback(async (id: string) => {
    setDetailError(null)
    try {
      setDetail(await getAdminSupportTicket(id))
    } catch {
      setDetail(null)
      setDetailError('Falha ao carregar detalhe do chamado.')
    }
  }, [])

  useEffect(() => {
    void loadList()
  }, [loadList])

  useEffect(() => {
    if (selectedId)
      void loadDetail(selectedId)
    else
      setDetail(null)
  }, [selectedId, loadDetail])

  async function onCreate() {
    if (!createRequesterId.trim() || !createQueue.trim() || !createSubject.trim())
      return
    setBusy(true)
    setActionError(null)
    setActionOk(null)
    try {
      const r = await createAdminSupportTicket({
        requesterUserId: createRequesterId.trim(),
        queue: createQueue.trim(),
        subject: createSubject.trim(),
        priority: createPriority,
        initialMessage: createMessage.trim() || null,
      })
      setActionOk(`Chamado criado: ${r.ticketId}`)
      setCreateSubject('')
      setCreateMessage('')
      setSelectedId(r.ticketId)
      await loadList()
    } catch {
      setActionError('Falha ao criar chamado.')
    } finally {
      setBusy(false)
    }
  }

  async function onReply() {
    if (!selectedId || !replyBody.trim())
      return
    setBusy(true)
    setActionError(null)
    setActionOk(null)
    try {
      await replyAdminSupportTicket(selectedId, { body: replyBody.trim(), isInternal: replyInternal })
      setReplyBody('')
      setActionOk('Resposta registrada.')
      await loadDetail(selectedId)
      await loadList()
    } catch {
      setActionError('Falha ao responder (verifique se o chamado não está fechado).')
    } finally {
      setBusy(false)
    }
  }

  async function onAssign() {
    if (!selectedId)
      return
    setBusy(true)
    setActionError(null)
    setActionOk(null)
    try {
      const id = assignAgentId.trim()
      await assignAdminSupportTicket(selectedId, id ? id : null)
      setActionOk('Atribuição atualizada.')
      await loadDetail(selectedId)
      await loadList()
    } catch {
      setActionError('Falha ao atribuir.')
    } finally {
      setBusy(false)
    }
  }

  async function onChangeStatus() {
    if (!selectedId)
      return
    setBusy(true)
    setActionError(null)
    setActionOk(null)
    try {
      await changeAdminSupportTicketStatus(selectedId, {
        status: newStatus,
        reason: statusReason.trim() || null,
      })
      setActionOk('Status atualizado.')
      await loadDetail(selectedId)
      await loadList()
    } catch {
      setActionError('Falha ao alterar status (transição inválida?).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ChamadosResponder]}>
      <h1>Chamados (suporte)</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        Filas operacionais, SLA, histórico e respostas. Permissão: Chamados.Responder.
      </p>

      {listError ? <p style={{ color: 'crimson' }}>{listError}</p> : null}
      {actionError ? <p style={{ color: 'crimson' }}>{actionError}</p> : null}
      {actionOk ? <p style={{ color: 'green' }}>{actionOk}</p> : null}

      <section style={{ marginTop: '1.5rem', padding: '1rem', border: '1px solid #ddd', borderRadius: 8 }}>
        <h2 style={{ fontSize: '1rem', marginTop: 0 }}>Abrir chamado (admin)</h2>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'flex-end' }}>
          <label>
            Requester userId
            <input
              style={{ display: 'block', width: 280 }}
              value={createRequesterId}
              onChange={(e) => setCreateRequesterId(e.target.value)}
              placeholder="UUID do usuário"
            />
          </label>
          <label>
            Fila
            <input style={{ display: 'block', width: 120 }} value={createQueue} onChange={(e) => setCreateQueue(e.target.value)} />
          </label>
          <label>
            Assunto
            <input
              style={{ display: 'block', width: 240 }}
              value={createSubject}
              onChange={(e) => setCreateSubject(e.target.value)}
            />
          </label>
          <label>
            Prioridade
            <select
              style={{ display: 'block' }}
              value={createPriority}
              onChange={(e) => setCreatePriority(e.target.value as SupportTicketPriority)}
            >
              {priorityOptions.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </label>
          <button type="button" disabled={busy} onClick={() => void onCreate()}>Criar</button>
        </div>
        <label style={{ display: 'block', marginTop: 8 }}>
          Mensagem inicial (opcional)
          <textarea
            style={{ display: 'block', width: '100%', maxWidth: 640 }}
            rows={2}
            value={createMessage}
            onChange={(e) => setCreateMessage(e.target.value)}
          />
        </label>
      </section>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1.5rem', marginTop: '1.5rem' }}>
        <section style={{ flex: '1 1 320px', minWidth: 280 }}>
          <h2 style={{ fontSize: '1rem' }}>Fila</h2>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 8 }}>
            <label>
              Fila
              <input style={{ display: 'block', width: 120 }} value={queueFilter} onChange={(e) => setQueueFilter(e.target.value)} />
            </label>
            <label>
              Status
              <select
                style={{ display: 'block' }}
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as SupportTicketStatus | '')}
              >
                <option value="">(todos)</option>
                {statusOptions.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 20 }}>
              <input type="checkbox" checked={unassignedOnly} onChange={(e) => setUnassignedOnly(e.target.checked)} />
              Só sem responsável
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 20 }}>
              <input type="checkbox" checked={slaBreachedOnly} onChange={(e) => setSlaBreachedOnly(e.target.checked)} />
              SLA estourado
            </label>
            <button type="button" onClick={() => void loadList()}>Atualizar</button>
          </div>
          {loading ? <p>Carregando...</p> : (
            <>
              <p style={{ fontSize: 13, color: '#666' }}>Total: {totalCount}</p>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0, maxHeight: 480, overflow: 'auto' }}>
                {items.map((row) => (
                  <li key={row.ticketId} style={{ marginBottom: 6 }}>
                    <button
                      type="button"
                      onClick={() => setSelectedId(row.ticketId)}
                      style={{
                        width: '100%',
                        textAlign: 'left',
                        padding: 8,
                        border: selectedId === row.ticketId ? '2px solid #0b57d0' : '1px solid #ccc',
                        borderRadius: 6,
                        background: row.isSlaBreached ? '#fff4f4' : '#fff',
                        cursor: 'pointer',
                      }}
                    >
                      <div style={{ fontWeight: 600 }}>{row.subject}</div>
                      <div style={{ fontSize: 12, color: '#555' }}>
                        {row.status}
                        {' · '}
                        {row.queue}
                        {row.isSlaBreached ? ' · SLA!' : ''}
                      </div>
                      <div style={{ fontSize: 11, color: '#888' }}>{row.ticketId}</div>
                    </button>
                  </li>
                ))}
              </ul>
              <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
                <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>Anterior</button>
                <span style={{ fontSize: 13 }}>Página {page}</span>
                <button
                  type="button"
                  disabled={page * 20 >= totalCount}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Próxima
                </button>
              </div>
            </>
          )}
        </section>

        <section style={{ flex: '2 1 400px', minWidth: 320 }}>
          <h2 style={{ fontSize: '1rem' }}>Detalhe</h2>
          {detailError ? <p style={{ color: 'crimson' }}>{detailError}</p> : null}
          {!selectedId ? <p>Selecione um chamado.</p> : null}
          {detail ? (
            <div>
              <p style={{ fontSize: 13 }}>
                <strong>Status:</strong>
                {' '}
                {detail.status}
                {' · '}
                <strong>SLA:</strong>
                {' '}
                {new Date(detail.slaDeadlineUtc).toLocaleString()}
                {detail.isSlaBreached ? ' (em atraso)' : ''}
              </p>
              <p style={{ fontSize: 13 }}>
                <strong>Solicitante:</strong>
                {' '}
                {detail.requesterUserId}
                {' · '}
                <strong>Responsável:</strong>
                {' '}
                {detail.assignedAgentUserId ?? '—'}
              </p>
              <h3 style={{ fontSize: '0.95rem' }}>Mensagens</h3>
              <ul style={{ fontSize: 13, paddingLeft: 18 }}>
                {detail.messages.map((m) => (
                  <li key={m.messageId} style={{ marginBottom: 6 }}>
                    <span style={{ color: '#666' }}>{new Date(m.createdAtUtc).toLocaleString()}</span>
                    {' — '}
                    {m.isInternal ? '[interno] ' : ''}
                    {m.body}
                  </li>
                ))}
              </ul>
              <h3 style={{ fontSize: '0.95rem' }}>Histórico</h3>
              <ul style={{ fontSize: 12, paddingLeft: 18, color: '#444' }}>
                {detail.history.map((h) => (
                  <li key={h.entryId} style={{ marginBottom: 4 }}>
                    {h.eventType}
                    {h.fromValue || h.toValue ? ` (${h.fromValue ?? ''} → ${h.toValue ?? ''})` : ''}
                    {h.reason ? ` — ${h.reason}` : ''}
                  </li>
                ))}
              </ul>

              <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #eee' }}>
                <h3 style={{ fontSize: '0.95rem' }}>Responder</h3>
                <textarea
                  style={{ width: '100%', minHeight: 72 }}
                  value={replyBody}
                  onChange={(e) => setReplyBody(e.target.value)}
                />
                <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 6 }}>
                  <input type="checkbox" checked={replyInternal} onChange={(e) => setReplyInternal(e.target.checked)} />
                  Nota interna
                </label>
                <button type="button" disabled={busy} style={{ marginTop: 8 }} onClick={() => void onReply()}>Enviar</button>
              </div>

              <div style={{ marginTop: '1rem' }}>
                <h3 style={{ fontSize: '0.95rem' }}>Atribuir agente (UUID)</h3>
                <input
                  style={{ width: '100%', maxWidth: 360 }}
                  value={assignAgentId}
                  onChange={(e) => setAssignAgentId(e.target.value)}
                  placeholder="Deixe vazio para remover"
                />
                <button type="button" disabled={busy} style={{ marginLeft: 8 }} onClick={() => void onAssign()}>Salvar</button>
              </div>

              <div style={{ marginTop: '1rem' }}>
                <h3 style={{ fontSize: '0.95rem' }}>Alterar status</h3>
                <select value={newStatus} onChange={(e) => setNewStatus(e.target.value as SupportTicketStatus)}>
                  {statusOptions.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
                <input
                  style={{ marginLeft: 8, width: 220 }}
                  value={statusReason}
                  onChange={(e) => setStatusReason(e.target.value)}
                  placeholder="Motivo (opcional)"
                />
                <button type="button" disabled={busy} style={{ marginLeft: 8 }} onClick={() => void onChangeStatus()}>Aplicar</button>
              </div>
            </div>
          ) : null}
        </section>
      </div>
    </PermissionGate>
  )
}
