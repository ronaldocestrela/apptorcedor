import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  createAdminGame,
  deactivateAdminGame,
  getAdminGame,
  listAdminGames,
  updateAdminGame,
  type AdminGameListItem,
  type UpsertGameBody,
} from '../services/adminApi'

function toLocalInputValue(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime()))
    return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function emptyForm(): {
  gameId: string | null
  opponent: string
  competition: string
  gameDateLocal: string
  isActive: boolean
} {
  const now = new Date()
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset())
  return {
    gameId: null,
    opponent: '',
    competition: '',
    gameDateLocal: toLocalInputValue(now.toISOString()),
    isActive: true,
  }
}

export function GamesAdminPage() {
   const { user } = useAuth()
  const canCreate = hasPermission(user, ApplicationPermissions.JogosCriar)
  const canEdit = hasPermission(user, ApplicationPermissions.JogosEditar)

  const [items, setItems] = useState<AdminGameListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState(() => emptyForm())

  const loadList = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await listAdminGames({ pageSize: 100 })
      setItems(page.items)
      setTotalCount(page.totalCount)
    } catch {
      setError('Falha ao listar jogos.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadList()
  }, [loadList])

  async function selectGame(gameId: string) {
    setError(null)
    try {
      const d = await getAdminGame(gameId)
      setForm({
        gameId: d.gameId,
        opponent: d.opponent,
        competition: d.competition,
        gameDateLocal: toLocalInputValue(d.gameDate),
        isActive: d.isActive,
      })
    } catch {
      setError('Falha ao carregar jogo.')
    }
  }

  function mapBody(): UpsertGameBody {
    const iso = new Date(form.gameDateLocal).toISOString()
    return {
      opponent: form.opponent.trim(),
      competition: form.competition.trim(),
      gameDate: iso,
      isActive: form.isActive,
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const body = mapBody()
      if (form.gameId) {
        await updateAdminGame(form.gameId, body)
      } else {
        const { gameId } = await createAdminGame(body)
        await selectGame(gameId)
      }
      await loadList()
    } catch {
      setError('Falha ao salvar jogo.')
    } finally {
      setSaving(false)
    }
  }

  async function onDeactivate() {
    if (!form.gameId)
      return
    setSaving(true)
    setError(null)
    try {
      await deactivateAdminGame(form.gameId)
      setForm(emptyForm())
      await loadList()
    } catch {
      setError('Falha ao desativar jogo.')
    } finally {
      setSaving(false)
    }
  }

  const canUseForm = (!form.gameId && canCreate) || (!!form.gameId && canEdit)

  return (
    <PermissionGate
      anyOf={[
        ApplicationPermissions.JogosVisualizar,
        ApplicationPermissions.JogosCriar,
        ApplicationPermissions.JogosEditar,
      ]}
    >
      <h1>Jogos</h1>
      <p style={{ color: '#555' }}>Total: {totalCount}</p>
      {loading ? <p>Carregando...</p> : null}
      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}

      <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
        <div style={{ flex: '1 1 320px' }}>
          <h2>Listagem</h2>
          <ul style={{ listStyle: 'none', padding: 0, maxHeight: 420, overflow: 'auto' }}>
            {items.map((g) => (
              <li key={g.gameId} style={{ borderBottom: '1px solid #eee', padding: '8px 0' }}>
                <button type="button" onClick={() => void selectGame(g.gameId)} style={{ fontWeight: 600 }}>
                  {g.opponent}
                </button>
                <div style={{ fontSize: 12, color: '#666' }}>
                  {g.competition} · {new Date(g.gameDate).toLocaleString()} · {g.isActive ? 'ativo' : 'inativo'}
                </div>
              </li>
            ))}
          </ul>
        </div>

        <div style={{ flex: '1 1 320px' }}>
          <h2>{form.gameId ? 'Editar' : 'Novo'}</h2>
          {canUseForm ? (
            <form onSubmit={(e) => void onSubmit(e)} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              <label>
                Adversário
                <input
                  value={form.opponent}
                  onChange={(e) => setForm((f) => ({ ...f, opponent: e.target.value }))}
                  style={{ width: '100%' }}
                  required
                />
              </label>
              <label>
                Competição
                <input
                  value={form.competition}
                  onChange={(e) => setForm((f) => ({ ...f, competition: e.target.value }))}
                  style={{ width: '100%' }}
                  required
                />
              </label>
              <label>
                Data do jogo
                <input
                  type="datetime-local"
                  value={form.gameDateLocal}
                  onChange={(e) => setForm((f) => ({ ...f, gameDateLocal: e.target.value }))}
                  style={{ width: '100%' }}
                  required
                />
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                />
                Ativo
              </label>
              <div style={{ display: 'flex', gap: 8 }}>
                <button type="submit" disabled={saving}>
                  Salvar
                </button>
                <button type="button" onClick={() => setForm(emptyForm())}>
                  Limpar
                </button>
                {form.gameId && canEdit ? (
                  <button type="button" onClick={() => void onDeactivate()} disabled={saving}>
                    Desativar
                  </button>
                ) : null}
              </div>
            </form>
          ) : (
            <p style={{ color: '#666' }}>
              {form.gameId
                ? 'Sem permissão para editar (Jogos.Editar).'
                : 'Sem permissão para criar (Jogos.Criar).'}
            </p>
          )}
        </div>
      </div>
    </PermissionGate>
  )
}
