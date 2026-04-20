import { useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import { listConfigurations, updateConfiguration, type AppConfigurationEntry } from '../services/adminApi'

export function ConfigurationsPage() {
  const { user } = useAuth()
  const [rows, setRows] = useState<AppConfigurationEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingKey, setEditingKey] = useState<string | null>(null)
  const [draftValue, setDraftValue] = useState('')
  const [saving, setSaving] = useState(false)
  const canEdit = hasPermission(user, ApplicationPermissions.ConfiguracoesEditar)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      setRows(await listConfigurations())
    } catch {
      setError('Falha ao listar configurações.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function save(key: string) {
    setSaving(true)
    setError(null)
    try {
      await updateConfiguration(key, draftValue)
      setEditingKey(null)
      await load()
    } catch {
      setError('Falha ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ConfiguracoesVisualizar]}>
      <h1>Configurações</h1>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      {!loading && rows.length === 0 ? <p>Nenhuma entrada.</p> : null}
      <table style={{ borderCollapse: 'collapse', width: '100%', maxWidth: 900 }}>
        <thead>
          <tr>
            <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Chave</th>
            <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Versão</th>
            <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Valor</th>
            {canEdit ? <th style={{ borderBottom: '1px solid #ccc' }}>Ações</th> : null}
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.key}>
              <td style={{ padding: '8px 0', verticalAlign: 'top' }}>{r.key}</td>
              <td style={{ padding: '8px 0', verticalAlign: 'top' }}>{r.version}</td>
              <td style={{ padding: '8px 0', verticalAlign: 'top', wordBreak: 'break-all' }}>
                {editingKey === r.key ? (
                  <textarea
                    value={draftValue}
                    onChange={(e) => setDraftValue(e.target.value)}
                    rows={6}
                    style={{ width: '100%', fontFamily: 'monospace' }}
                  />
                ) : (
                  r.value
                )}
              </td>
              {canEdit ? (
                <td style={{ padding: '8px 0', verticalAlign: 'top' }}>
                  {editingKey === r.key ? (
                    <>
                      <button type="button" disabled={saving} onClick={() => void save(r.key)}>Salvar</button>
                      {' '}
                      <button type="button" disabled={saving} onClick={() => setEditingKey(null)}>Cancelar</button>
                    </>
                  ) : (
                    <button
                      type="button"
                      onClick={() => {
                        setEditingKey(r.key)
                        setDraftValue(r.value)
                      }}
                    >
                      Editar
                    </button>
                  )}
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>
      {canEdit ? (
        <section style={{ marginTop: '2rem' }}>
          <h2>Nova chave</h2>
          <NewConfigForm onCreated={() => void load()} />
        </section>
      ) : null}
    </PermissionGate>
  )
}

function NewConfigForm({ onCreated }: { onCreated: () => void }) {
  const [key, setKey] = useState('')
  const [value, setValue] = useState('')
  const [busy, setBusy] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setErr(null)
    setBusy(true)
    try {
      await updateConfiguration(key.trim(), value)
      setKey('')
      setValue('')
      onCreated()
    } catch {
      setErr('Falha ao criar/atualizar.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={(e) => void submit(e)}>
      <label style={{ display: 'block', marginBottom: 8 }}>
        Chave
        <input value={key} onChange={(e) => setKey(e.target.value)} required style={{ display: 'block', width: '100%', marginTop: 4 }} />
      </label>
      <label style={{ display: 'block', marginBottom: 8 }}>
        Valor
        <textarea value={value} onChange={(e) => setValue(e.target.value)} required rows={4} style={{ display: 'block', width: '100%', marginTop: 4, fontFamily: 'monospace' }} />
      </label>
      {err ? <p role="alert" style={{ color: 'crimson' }}>{err}</p> : null}
      <button type="submit" disabled={busy}>{busy ? 'Salvando...' : 'Salvar'}</button>
    </form>
  )
}
