import { useEffect, useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getRegistrationRequirements, type RegistrationRequirements } from '../features/account/accountApi'
import { useAuth } from '../features/auth/AuthContext'

export function RegisterPage() {
  const { user, register } = useAuth()
  const [legal, setLegal] = useState<RegistrationRequirements | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [phone, setPhone] = useState('')
  const [acceptTerms, setAcceptTerms] = useState(false)
  const [acceptPrivacy, setAcceptPrivacy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const r = await getRegistrationRequirements()
        if (!cancelled)
          setLegal(r)
      } catch {
        if (!cancelled)
          setLoadError('Cadastro indisponível no momento (documentos legais).')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  if (user)
    return <Navigate to="/" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!legal) {
      setError('Requisitos legais não carregados.')
      return
    }
    if (!acceptTerms || !acceptPrivacy) {
      setError('Aceite os termos e a política de privacidade.')
      return
    }
    setBusy(true)
    try {
      await register({
        name,
        email,
        password,
        phoneNumber: phone.trim() || undefined,
        acceptedLegalDocumentVersionIds: [legal.termsOfUseVersionId, legal.privacyPolicyVersionId],
      })
    } catch {
      setError('Não foi possível concluir o cadastro. Verifique os dados ou tente outro e-mail.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main style={{ maxWidth: 420, margin: '3rem auto', fontFamily: 'system-ui' }}>
      <h1>Criar conta</h1>
      {loadError ? <p role="alert" style={{ color: 'crimson' }}>{loadError}</p> : null}
      {legal ? (
        <form onSubmit={onSubmit}>
          <label style={{ display: 'block', marginBottom: 8 }}>
            Nome
            <input
              value={name}
              onChange={(ev) => setName(ev.target.value)}
              required
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <label style={{ display: 'block', marginBottom: 8 }}>
            E-mail
            <input
              type="email"
              value={email}
              onChange={(ev) => setEmail(ev.target.value)}
              required
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <label style={{ display: 'block', marginBottom: 8 }}>
            Senha
            <input
              type="password"
              value={password}
              onChange={(ev) => setPassword(ev.target.value)}
              required
              minLength={8}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <label style={{ display: 'block', marginBottom: 16 }}>
            Celular (opcional)
            <input
              value={phone}
              onChange={(ev) => setPhone(ev.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4 }}
            />
          </label>
          <fieldset style={{ marginBottom: 16, border: '1px solid #ccc', padding: 12 }}>
            <legend>LGPD</legend>
            <label style={{ display: 'flex', gap: 8, alignItems: 'flex-start', marginBottom: 8 }}>
              <input
                type="checkbox"
                checked={acceptTerms}
                onChange={(ev) => setAcceptTerms(ev.target.checked)}
              />
              <span>Li e aceito: {legal.termsTitle}</span>
            </label>
            <label style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
              <input
                type="checkbox"
                checked={acceptPrivacy}
                onChange={(ev) => setAcceptPrivacy(ev.target.checked)}
              />
              <span>Li e aceito: {legal.privacyTitle}</span>
            </label>
          </fieldset>
          {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
          <button type="submit" disabled={busy || !legal}>
            {busy ? 'Cadastrando...' : 'Cadastrar'}
          </button>
        </form>
      ) : loadError ? null : <p>Carregando...</p>}
      <p style={{ marginTop: 24 }}>
        <Link to="/login">Já tenho conta</Link>
      </p>
    </main>
  )
}
