import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  addLegalDocumentVersion,
  createLegalDocument,
  getLegalDocument,
  listLegalDocuments,
  publishLegalDocumentVersion,
  type LegalDocumentDetail,
  type LegalDocumentListItem,
  type LegalDocumentType,
} from '../services/lgpdApi'

export function LegalDocumentsPage() {
  const { user } = useAuth()
  const canView = hasPermission(user, ApplicationPermissions.LgpdDocumentosVisualizar)
  const canEdit = hasPermission(user, ApplicationPermissions.LgpdDocumentosEditar)
  const [rows, setRows] = useState<LegalDocumentListItem[]>([])
  const [selected, setSelected] = useState<LegalDocumentDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [newType, setNewType] = useState<LegalDocumentType>('TermsOfUse')
  const [newTitle, setNewTitle] = useState('')
  const [versionContent, setVersionContent] = useState('')
  const [busy, setBusy] = useState(false)

  const loadList = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setRows(await listLegalDocuments())
    } catch {
      setError('Falha ao listar documentos.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadList()
  }, [loadList])

  async function openDetail(id: string) {
    setError(null)
    try {
      setSelected(await getLegalDocument(id))
      setVersionContent('')
    } catch {
      setError('Falha ao carregar documento.')
    }
  }

  async function onCreate(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await createLegalDocument({ type: newType, title: newTitle.trim() })
      setNewTitle('')
      await loadList()
    } catch {
      setError('Não foi possível criar (tipo duplicado ou sem permissão).')
    } finally {
      setBusy(false)
    }
  }

  async function onAddVersion(e: FormEvent) {
    e.preventDefault()
    if (!selected)
      return
    setBusy(true)
    setError(null)
    try {
      await addLegalDocumentVersion(selected.id, versionContent)
      setVersionContent('')
      setSelected(await getLegalDocument(selected.id))
      await loadList()
    } catch {
      setError('Falha ao adicionar versão.')
    } finally {
      setBusy(false)
    }
  }

  async function onPublish(versionId: string) {
    setBusy(true)
    setError(null)
    try {
      await publishLegalDocumentVersion(versionId)
      if (selected)
        setSelected(await getLegalDocument(selected.id))
      await loadList()
    } catch {
      setError('Falha ao publicar versão.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.LgpdDocumentosVisualizar]}>
      <h1>LGPD — Documentos legais</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        Cadastro e versionamento de termos e política. Apenas uma versão publicada por documento por vez.
      </p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}

      {canEdit ? (
        <form onSubmit={(e) => void onCreate(e)} style={{ marginBottom: 24, maxWidth: 480 }}>
          <h2 style={{ fontSize: '1rem' }}>Novo documento</h2>
          <label style={{ display: 'block', marginBottom: 8 }}>
            Tipo
            <select
              value={newType}
              onChange={(e) => setNewType(e.target.value as LegalDocumentType)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            >
              <option value="TermsOfUse">Termos de uso</option>
              <option value="PrivacyPolicy">Política de privacidade</option>
            </select>
          </label>
          <label style={{ display: 'block', marginBottom: 8 }}>
            Título
            <input
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              required
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <button type="submit" disabled={busy}>Criar</button>
        </form>
      ) : null}

      {!loading && canView ? (
        <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap', alignItems: 'flex-start' }}>
          <div style={{ flex: '1 1 280px' }}>
            <h2 style={{ fontSize: '1rem' }}>Documentos</h2>
            <ul style={{ paddingLeft: 18 }}>
              {rows.map((r) => (
                <li key={r.id} style={{ marginBottom: 8 }}>
                  <button type="button" style={{ background: 'none', border: 'none', color: '#0b57d0', cursor: 'pointer', padding: 0, textAlign: 'left' }} onClick={() => void openDetail(r.id)}>
                    {r.title}
                    {' '}
                    (
                    {r.type}
                    )
                  </button>
                  {r.publishedVersionNumber != null ? (
                    <span style={{ color: '#666', fontSize: 12 }}>
                      {' '}
                      — publicada v
                      {r.publishedVersionNumber}
                    </span>
                  ) : (
                    <span style={{ color: '#666', fontSize: 12 }}> — sem versão publicada</span>
                  )}
                </li>
              ))}
            </ul>
            {rows.length === 0 ? <p>Nenhum documento cadastrado.</p> : null}
          </div>

          {selected ? (
            <div style={{ flex: '2 1 400px' }}>
              <h2 style={{ fontSize: '1rem' }}>{selected.title}</h2>
              <p style={{ fontSize: 13, color: '#666' }}>{selected.type}</p>
              <h3 style={{ fontSize: '0.95rem' }}>Versões</h3>
              <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 14 }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>v</th>
                    <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Status</th>
                    <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>Conteúdo</th>
                    {canEdit ? <th style={{ borderBottom: '1px solid #ccc' }}>Ação</th> : null}
                  </tr>
                </thead>
                <tbody>
                  {selected.versions.map((v) => (
                    <tr key={v.id}>
                      <td style={{ padding: '6px 0', verticalAlign: 'top' }}>{v.versionNumber}</td>
                      <td style={{ padding: '6px 0', verticalAlign: 'top' }}>{v.status}</td>
                      <td style={{ padding: '6px 0', verticalAlign: 'top', wordBreak: 'break-word', maxWidth: 360 }}>{v.content}</td>
                      {canEdit ? (
                        <td style={{ padding: '6px 0', verticalAlign: 'top' }}>
                          {v.status === 'Draft' ? (
                            <button type="button" disabled={busy} onClick={() => void onPublish(v.id)}>Publicar</button>
                          ) : null}
                        </td>
                      ) : null}
                    </tr>
                  ))}
                </tbody>
              </table>

              {canEdit ? (
                <form onSubmit={(e) => void onAddVersion(e)} style={{ marginTop: 16 }}>
                  <label style={{ display: 'block' }}>
                    Nova versão (rascunho)
                    <textarea
                      value={versionContent}
                      onChange={(e) => setVersionContent(e.target.value)}
                      required
                      rows={5}
                      style={{ display: 'block', width: '100%', marginTop: 4 }}
                    />
                  </label>
                  <button type="submit" disabled={busy} style={{ marginTop: 8 }}>Adicionar versão</button>
                </form>
              ) : null}
            </div>
          ) : null}
        </div>
      ) : null}
    </PermissionGate>
  )
}
