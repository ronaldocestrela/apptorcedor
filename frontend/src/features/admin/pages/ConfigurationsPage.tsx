import { useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  EMAIL_WELCOME_TEMPLATE_KEYS,
  EMAIL_WELCOME_TEMPLATE_KEY_SET,
  isValidWelcomeBannerImageUrl,
  listConfigurations,
  updateConfiguration,
  uploadTeamShield,
  type AppConfigurationEntry,
} from '../services/adminApi'
import { TeamShieldLogo } from '../../../shared/branding/TeamShieldLogo'
import { resolvePublicAssetUrl } from '../../account/accountApi'

export function ConfigurationsPage() {
  const { user } = useAuth()
  const [rows, setRows] = useState<AppConfigurationEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingKey, setEditingKey] = useState<string | null>(null)
  const [draftValue, setDraftValue] = useState('')
  const [saving, setSaving] = useState(false)
  const [shieldFile, setShieldFile] = useState<File | null>(null)
  const [shieldBusy, setShieldBusy] = useState(false)
  const [shieldMessage, setShieldMessage] = useState<string | null>(null)
  const [shieldPreviewUrl, setShieldPreviewUrl] = useState<string | null>(null)
  const [welcomeSubject, setWelcomeSubject] = useState('')
  const [welcomeHtml, setWelcomeHtml] = useState('')
  const [welcomeImageUrl, setWelcomeImageUrl] = useState('')
  const [welcomeBannerUrlError, setWelcomeBannerUrlError] = useState<string | null>(null)
  const [welcomeSaving, setWelcomeSaving] = useState(false)
  const [welcomeMessage, setWelcomeMessage] = useState<string | null>(null)
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

  useEffect(() => {
    const sub = rows.find((r) => r.key === EMAIL_WELCOME_TEMPLATE_KEYS.Subject)?.value ?? ''
    const html = rows.find((r) => r.key === EMAIL_WELCOME_TEMPLATE_KEYS.Html)?.value ?? ''
    const img = rows.find((r) => r.key === EMAIL_WELCOME_TEMPLATE_KEYS.ImageUrl)?.value ?? ''
    setWelcomeSubject(sub)
    setWelcomeHtml(html)
    setWelcomeImageUrl(img)
    setWelcomeBannerUrlError(null)
    setWelcomeMessage(null)
  }, [rows])

  useEffect(() => {
    if (!shieldFile) {
      setShieldPreviewUrl(null)
      return
    }
    const url = URL.createObjectURL(shieldFile)
    setShieldPreviewUrl(url)
    return () => {
      URL.revokeObjectURL(url)
    }
  }, [shieldFile])

  async function submitShieldUpload() {
    if (!shieldFile)
      return
    setShieldBusy(true)
    setShieldMessage(null)
    setError(null)
    try {
      await uploadTeamShield(shieldFile)
      setShieldFile(null)
      setShieldMessage('Escudo atualizado. As telas do app passam a usar a nova imagem.')
      await load()
    }
    catch {
      setShieldMessage('Falha ao enviar o escudo. Verifique formato (JPEG, PNG ou WebP) e tamanho.')
    }
    finally {
      setShieldBusy(false)
    }
  }

  async function saveWelcomeTemplate() {
    setWelcomeBannerUrlError(null)
    setWelcomeMessage(null)
    if (!isValidWelcomeBannerImageUrl(welcomeImageUrl)) {
      setWelcomeBannerUrlError('Use uma URL absoluta http ou https (ou deixe em branco).')
      return
    }
    setWelcomeSaving(true)
    setError(null)
    try {
      await Promise.all([
        updateConfiguration(EMAIL_WELCOME_TEMPLATE_KEYS.Subject, welcomeSubject),
        updateConfiguration(EMAIL_WELCOME_TEMPLATE_KEYS.Html, welcomeHtml),
        updateConfiguration(EMAIL_WELCOME_TEMPLATE_KEYS.ImageUrl, welcomeImageUrl.trim()),
      ])
      setWelcomeMessage('Modelo de e-mail de boas-vindas salvo.')
      await load()
    }
    catch {
      setError('Falha ao salvar modelo de e-mail.')
    }
    finally {
      setWelcomeSaving(false)
    }
  }

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

  const shieldRow = rows.find((r) => r.key === 'Brand.TeamShieldUrl')
  const tableRows = rows.filter((r) => !EMAIL_WELCOME_TEMPLATE_KEY_SET.has(r.key))

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ConfiguracoesVisualizar]}>
      <h1>Configurações</h1>
      <section style={{ marginBottom: '2rem', maxWidth: 560 }}>
        <h2 style={{ fontSize: '1.1rem', marginBottom: '0.5rem' }}>Identidade — escudo do clube</h2>
        <p style={{ color: '#666', fontSize: '0.9rem', marginTop: 0 }}>
          Imagem exibida no app (login, área do torcedor, admin). Se não houver upload, é usado um placeholder.
          A URL fica em{' '}
          <code>Brand.TeamShieldUrl</code>
          {' '}
          (somente leitura aqui; use o arquivo abaixo para alterar).
        </p>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'flex-start' }}>
          <div>
            <p style={{ margin: '0 0 0.35rem', fontSize: '0.85rem', color: '#555' }}>Preview ao vivo</p>
            <div style={{ width: 72, height: 72, border: '1px solid #ddd', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f8f8f8' }}>
              <TeamShieldLogo key={shieldRow?.version ?? 'preview'} alt="Preview do escudo" width={64} height={64} />
            </div>
          </div>
          {shieldRow ? (
            <div style={{ flex: '1 1 200px', fontSize: '0.85rem', wordBreak: 'break-all' }}>
              <strong>URL atual</strong>
              <div>{shieldRow.value || '(placeholder até primeiro upload)'}</div>
              {shieldRow.value ? (
                <img
                  src={resolvePublicAssetUrl(shieldRow.value) ?? ''}
                  alt=""
                  width={64}
                  height={64}
                  style={{ marginTop: 8, objectFit: 'contain', display: 'block' }}
                />
              ) : null}
            </div>
          ) : null}
        </div>
        {canEdit ? (
          <div style={{ marginTop: '1rem' }}>
            <label style={{ display: 'block', marginBottom: 8 }}>
              Novo arquivo (JPEG, PNG ou WebP, até 5 MB)
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                style={{ display: 'block', marginTop: 6 }}
                onChange={(e) => {
                  setShieldMessage(null)
                  setShieldFile(e.target.files?.[0] ?? null)
                }}
              />
            </label>
            {shieldPreviewUrl ? (
              <img src={shieldPreviewUrl} alt="" width={56} height={56} style={{ objectFit: 'contain', display: 'block', marginBottom: 8 }} />
            ) : null}
            <button type="button" disabled={shieldBusy || !shieldFile} onClick={() => void submitShieldUpload()}>
              {shieldBusy ? 'Enviando...' : 'Enviar escudo'}
            </button>
          </div>
        ) : null}
        {shieldMessage ? <p role="status" style={{ marginTop: '0.75rem', color: '#0a5' }}>{shieldMessage}</p> : null}
      </section>
      <section style={{ marginBottom: '2rem', maxWidth: 720 }}>
        <h2 style={{ fontSize: '1.1rem', marginBottom: '0.5rem' }}>E-mail — boas-vindas (cadastro)</h2>
        <p style={{ color: '#666', fontSize: '0.9rem', marginTop: 0 }}>
          Assunto e corpo HTML enviados após o cadastro (e-mail/senha ou Google). Placeholders suportados pelo backend:{' '}
          <code>{'{{Name}}'}</code>
          {' '}
          (nome do torcedor, escapado no HTML) e{' '}
          <code>{'{{BannerImage}}'}</code>
          {' '}
          (bloco
          {' '}
          <code>&lt;img&gt;</code>
          {' '}
          quando houver URL de imagem válida abaixo). Se o HTML não incluir
          {' '}
          <code>{'{{BannerImage}}'}</code>
          , a imagem é colocada no início do corpo automaticamente.
        </p>
        <p style={{ color: '#666', fontSize: '0.85rem', marginTop: '0.5rem' }}>
          Chaves no banco:
          {' '}
          <code>{EMAIL_WELCOME_TEMPLATE_KEYS.Subject}</code>
          ,
          {' '}
          <code>{EMAIL_WELCOME_TEMPLATE_KEYS.Html}</code>
          ,
          {' '}
          <code>{EMAIL_WELCOME_TEMPLATE_KEYS.ImageUrl}</code>
          . Se algum valor estiver vazio ou inválido no servidor, o sistema usa o template padrão embutido (fallback).
        </p>
        <div style={{ display: 'grid', gap: '0.75rem', marginTop: '1rem' }}>
          <label style={{ display: 'block' }}>
            Assunto
            <input
              type="text"
              value={welcomeSubject}
              onChange={(e) => {
                setWelcomeSubject(e.target.value)
                setWelcomeMessage(null)
              }}
              readOnly={!canEdit}
              style={{ display: 'block', width: '100%', marginTop: 6, boxSizing: 'border-box' }}
            />
          </label>
          <label style={{ display: 'block' }}>
            Corpo HTML
            <textarea
              value={welcomeHtml}
              onChange={(e) => {
                setWelcomeHtml(e.target.value)
                setWelcomeMessage(null)
              }}
              readOnly={!canEdit}
              rows={10}
              placeholder={'Ex.: {{BannerImage}}<p>Olá, {{Name}}!</p><p>Obrigado por se cadastrar.</p>'}
              style={{ display: 'block', width: '100%', marginTop: 6, fontFamily: 'monospace', boxSizing: 'border-box' }}
            />
          </label>
          <label style={{ display: 'block' }}>
            URL pública da imagem (banner opcional)
            <input
              type="url"
              value={welcomeImageUrl}
              onChange={(e) => {
                setWelcomeImageUrl(e.target.value)
                setWelcomeBannerUrlError(null)
                setWelcomeMessage(null)
              }}
              onBlur={() => {
                if (welcomeImageUrl.trim().length > 0 && !isValidWelcomeBannerImageUrl(welcomeImageUrl))
                  setWelcomeBannerUrlError('Use uma URL absoluta http ou https.')
                else
                  setWelcomeBannerUrlError(null)
              }}
              readOnly={!canEdit}
              placeholder="https://cdn.exemplo.com/banner.png"
              style={{ display: 'block', width: '100%', marginTop: 6, boxSizing: 'border-box' }}
            />
          </label>
        </div>
        {welcomeBannerUrlError ? <p role="alert" style={{ color: 'crimson', marginTop: 8 }}>{welcomeBannerUrlError}</p> : null}
        {canEdit ? (
          <div style={{ marginTop: '1rem' }}>
            <button
              type="button"
              data-testid="welcome-email-save"
              disabled={welcomeSaving || !isValidWelcomeBannerImageUrl(welcomeImageUrl)}
              onClick={() => void saveWelcomeTemplate()}
            >
              {welcomeSaving ? 'Salvando...' : 'Salvar modelo de boas-vindas'}
            </button>
          </div>
        ) : null}
        {welcomeMessage ? <p role="status" style={{ marginTop: '0.75rem', color: '#0a5' }}>{welcomeMessage}</p> : null}
      </section>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      {!loading && tableRows.length === 0 ? <p>Nenhuma entrada (além do bloco de e-mail acima).</p> : null}
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
          {tableRows.map((r) => (
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
