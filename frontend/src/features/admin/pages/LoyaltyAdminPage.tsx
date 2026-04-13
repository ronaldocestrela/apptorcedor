import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  createLoyaltyCampaign,
  getLoyaltyAllTimeRanking,
  getLoyaltyCampaign,
  getLoyaltyMonthlyRanking,
  listLoyaltyCampaigns,
  listLoyaltyUserLedger,
  manualLoyaltyAdjust,
  publishLoyaltyCampaign,
  unpublishLoyaltyCampaign,
  updateLoyaltyCampaign,
  type LoyaltyCampaignListItem,
  type LoyaltyPointRuleTrigger,
} from '../services/adminApi'

const triggers: LoyaltyPointRuleTrigger[] = ['PaymentPaid', 'TicketPurchased', 'TicketRedeemed']

export function LoyaltyAdminPage() {
  const [items, setItems] = useState<LoyaltyCampaignListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [rules, setRules] = useState<{ trigger: LoyaltyPointRuleTrigger; points: number; sortOrder: number }[]>([
    { trigger: 'PaymentPaid', points: 10, sortOrder: 0 },
  ])
  const [ledgerUserId, setLedgerUserId] = useState('')
  const [ledgerJson, setLedgerJson] = useState('')
  const [rankMonth, setRankMonth] = useState(() => {
    const d = new Date()
    return { year: d.getFullYear(), month: d.getMonth() + 1 }
  })
  const [rankMonthlyJson, setRankMonthlyJson] = useState('')
  const [rankAllJson, setRankAllJson] = useState('')
  const [manualUserId, setManualUserId] = useState('')
  const [manualPoints, setManualPoints] = useState(0)
  const [manualReason, setManualReason] = useState('')

  const loadList = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await listLoyaltyCampaigns({ pageSize: 100 })
      setItems(page.items)
    } catch {
      setError('Falha ao listar campanhas.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadList()
  }, [loadList])

  async function selectCampaign(id: string) {
    setError(null)
    setSelectedId(id)
    try {
      const d = await getLoyaltyCampaign(id)
      setName(d.name)
      setDescription(d.description ?? '')
      setRules(
        d.rules.length > 0
          ? d.rules.map((r) => ({ trigger: r.trigger, points: r.points, sortOrder: r.sortOrder }))
          : [{ trigger: 'PaymentPaid', points: 10, sortOrder: 0 }],
      )
    } catch {
      setError('Falha ao carregar campanha.')
    }
  }

  function resetNew() {
    setSelectedId(null)
    setName('')
    setDescription('')
    setRules([{ trigger: 'PaymentPaid', points: 10, sortOrder: 0 }])
  }

  async function onSave(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const body = {
        name: name.trim(),
        description: description.trim() || null,
        rules: rules.map((r, i) => ({ ...r, sortOrder: i })),
      }
      if (selectedId)
        await updateLoyaltyCampaign(selectedId, body)
      else
        await createLoyaltyCampaign(body)
      await loadList()
      resetNew()
    } catch {
      setError('Falha ao salvar campanha.')
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.FidelidadeVisualizar, ApplicationPermissions.FidelidadeGerenciar]}>
      <h1>Fidelidade</h1>
      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}

      <div style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
        <section style={{ minWidth: 220 }}>
          <h2 style={{ fontSize: '1rem' }}>Campanhas</h2>
          <ul style={{ paddingLeft: 16 }}>
            {items.map((c) => (
              <li key={c.campaignId}>
                <button type="button" style={{ background: 'none', border: 'none', color: '#0b57d0', cursor: 'pointer' }} onClick={() => void selectCampaign(c.campaignId)}>
                  {c.name}
                </button>
                <span style={{ fontSize: 12, color: '#666' }}> ({c.status})</span>
              </li>
            ))}
          </ul>
          <PermissionGate anyOf={[ApplicationPermissions.FidelidadeGerenciar]}>
            <button type="button" onClick={resetNew}>Nova campanha</button>
          </PermissionGate>
        </section>

        <section style={{ flex: 1, minWidth: 320 }}>
          <h2 style={{ fontSize: '1rem' }}>Editor</h2>
          <PermissionGate anyOf={[ApplicationPermissions.FidelidadeGerenciar]}>
            <form onSubmit={(e) => void onSave(e)}>
              <div style={{ marginBottom: 8 }}>
                <label>
                  Nome
                  <input value={name} onChange={(e) => setName(e.target.value)} required style={{ display: 'block', width: '100%' }} />
                </label>
              </div>
              <div style={{ marginBottom: 8 }}>
                <label>
                  Descrição
                  <textarea value={description} onChange={(e) => setDescription(e.target.value)} style={{ display: 'block', width: '100%' }} rows={2} />
                </label>
              </div>
              <p style={{ fontSize: 14 }}>Regras de pontos</p>
              {rules.map((r, idx) => (
                <div key={idx} style={{ display: 'flex', gap: 8, marginBottom: 8, flexWrap: 'wrap' }}>
                  <select value={r.trigger} onChange={(e) => {
                    const next = [...rules]
                    next[idx] = { ...r, trigger: e.target.value as LoyaltyPointRuleTrigger }
                    setRules(next)
                  }}
 >
                    {triggers.map((t) => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                  <input
                    type="number"
                    value={r.points}
                    onChange={(e) => {
                      const next = [...rules]
                      next[idx] = { ...r, points: Number(e.target.value) }
                      setRules(next)
                    }}
                  />
                  <button
                    type="button"
                    onClick={() => setRules(rules.filter((_, i) => i !== idx))}
                  >
                    Remover
                  </button>
                </div>
              ))}
              <button type="button" onClick={() => setRules([...rules, { trigger: 'PaymentPaid', points: 1, sortOrder: rules.length }])}>
                Adicionar regra
              </button>
              <div style={{ marginTop: 12 }}>
                <button type="submit">Salvar</button>
                {selectedId ? (
                  <>
                    <button type="button" style={{ marginLeft: 8 }} onClick={() => void publishLoyaltyCampaign(selectedId).then(() => loadList())}>Publicar</button>
                    <button type="button" style={{ marginLeft: 8 }} onClick={() => void unpublishLoyaltyCampaign(selectedId).then(() => loadList())}>Despublicar</button>
                  </>
                ) : null}
              </div>
            </form>
          </PermissionGate>
        </section>
      </div>

      <section style={{ marginTop: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Extrato por usuário</h2>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
          <input placeholder="UserId (GUID)" value={ledgerUserId} onChange={(e) => setLedgerUserId(e.target.value)} style={{ minWidth: 280 }} />
          <button
            type="button"
            onClick={() => void (async () => {
              setLedgerJson('')
              try {
                const p = await listLoyaltyUserLedger(ledgerUserId.trim(), { pageSize: 50 })
                setLedgerJson(JSON.stringify(p, null, 2))
              } catch {
                setLedgerJson('Erro ao carregar extrato.')
              }
            })()}
          >
            Carregar
          </button>
        </div>
        {ledgerJson ? <pre style={{ background: '#f5f5f5', padding: 8, maxHeight: 240, overflow: 'auto' }}>{ledgerJson}</pre> : null}
      </section>

      <section style={{ marginTop: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Ranking</h2>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center', marginBottom: 8 }}>
          <input type="number" value={rankMonth.year} onChange={(e) => setRankMonth((m) => ({ ...m, year: Number(e.target.value) }))} />
          <input type="number" value={rankMonth.month} onChange={(e) => setRankMonth((m) => ({ ...m, month: Number(e.target.value) }))} />
          <button
            type="button"
            onClick={() => void (async () => {
              setRankMonthlyJson('')
              try {
                const p = await getLoyaltyMonthlyRanking({ year: rankMonth.year, month: rankMonth.month, pageSize: 50 })
                setRankMonthlyJson(JSON.stringify(p, null, 2))
              } catch {
                setRankMonthlyJson('Erro.')
              }
            })()}
          >
            Mensal
          </button>
          <button
            type="button"
            onClick={() => void (async () => {
              setRankAllJson('')
              try {
                const p = await getLoyaltyAllTimeRanking({ pageSize: 50 })
                setRankAllJson(JSON.stringify(p, null, 2))
              } catch {
                setRankAllJson('Erro.')
              }
            })()}
          >
            Acumulado
          </button>
        </div>
        {rankMonthlyJson ? <pre style={{ background: '#f5f5f5', padding: 8, maxHeight: 200, overflow: 'auto' }}>{rankMonthlyJson}</pre> : null}
        {rankAllJson ? <pre style={{ background: '#f5f5f5', padding: 8, maxHeight: 200, overflow: 'auto' }}>{rankAllJson}</pre> : null}
      </section>

      <section style={{ marginTop: '2rem' }}>
        <h2 style={{ fontSize: '1rem' }}>Ajuste manual</h2>
        <PermissionGate anyOf={[ApplicationPermissions.FidelidadeGerenciar]}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8, maxWidth: 400 }}>
            <input placeholder="UserId" value={manualUserId} onChange={(e) => setManualUserId(e.target.value)} />
            <input type="number" placeholder="Pontos (+/-)" value={manualPoints} onChange={(e) => setManualPoints(Number(e.target.value))} />
            <input placeholder="Motivo" value={manualReason} onChange={(e) => setManualReason(e.target.value)} />
            <button
              type="button"
              onClick={() => void (async () => {
                try {
                  await manualLoyaltyAdjust(manualUserId.trim(), { points: manualPoints, reason: manualReason.trim() })
                  setManualReason('')
                } catch {
                  setError('Falha no ajuste manual.')
                }
              })()}
            >
              Aplicar
            </button>
          </div>
        </PermissionGate>
      </section>
    </PermissionGate>
  )
}
