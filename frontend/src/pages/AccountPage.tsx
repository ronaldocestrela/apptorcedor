import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { getMyProfile, resolvePublicAssetUrl, upsertMyProfile, uploadProfilePhoto } from '../features/account/accountApi'
import { useAuth } from '../features/auth/AuthContext'

export function AccountPage() {
  const { user, refreshProfile } = useAuth()
  const [document, setDocument] = useState('')
  const [birthDate, setBirthDate] = useState('')
  const [address, setAddress] = useState('')
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const p = await getMyProfile()
        if (cancelled)
          return
        setDocument(p.document ?? '')
        setBirthDate(p.birthDate ?? '')
        setAddress(p.address ?? '')
        setPhotoUrl(p.photoUrl)
      } catch {
        if (!cancelled)
          setLoadError('Não foi possível carregar o perfil.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaveError(null)
    setBusy(true)
    try {
      await upsertMyProfile({
        document: document.trim() || null,
        birthDate: birthDate.trim() || null,
        address: address.trim() || null,
        photoUrl: photoUrl ?? null,
      })
      await refreshProfile()
    } catch {
      setSaveError('Falha ao salvar.')
    } finally {
      setBusy(false)
    }
  }

  async function onPhoto(ev: ChangeEvent<HTMLInputElement>) {
    const file = ev.target.files?.[0]
    if (!file)
      return
    setSaveError(null)
    setBusy(true)
    try {
      const url = await uploadProfilePhoto(file)
      setPhotoUrl(url)
      await upsertMyProfile({ photoUrl: url })
      await refreshProfile()
    } catch {
      setSaveError('Falha no envio da foto (tipo ou tamanho).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main style={{ maxWidth: 480, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>Minha conta</h1>
      <p>
        <strong>{user?.name}</strong> ({user?.email})
      </p>
      {user?.requiresProfileCompletion ? (
        <p style={{ color: '#856404', background: '#fff3cd', padding: 8 }}>
          Complete seu perfil (documento obrigatório para seguir).
        </p>
      ) : null}
      {loadError ? <p role="alert" style={{ color: 'crimson' }}>{loadError}</p> : null}
      {photoUrl ? (
        <p>
          <img
            src={resolvePublicAssetUrl(photoUrl)}
            alt="Foto"
            style={{ maxWidth: 160, borderRadius: 8 }}
          />
        </p>
      ) : null}
      <form onSubmit={onSubmit}>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Documento (CPF ou equivalente)
          <input
            value={document}
            onChange={(ev) => setDocument(ev.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Data de nascimento
          <input
            type="date"
            value={birthDate}
            onChange={(ev) => setBirthDate(ev.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Endereço
          <textarea
            value={address}
            onChange={(ev) => setAddress(ev.target.value)}
            rows={3}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 16 }}>
          Foto do perfil
          <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(ev) => void onPhoto(ev)} disabled={busy} />
        </label>
        {saveError ? <p role="alert" style={{ color: 'crimson' }}>{saveError}</p> : null}
        <button type="submit" disabled={busy}>
          {busy ? 'Salvando...' : 'Salvar perfil'}
        </button>
      </form>
      <p style={{ marginTop: 24 }}>
        <Link to="/">Voltar</Link>
      </p>
    </main>
  )
}
