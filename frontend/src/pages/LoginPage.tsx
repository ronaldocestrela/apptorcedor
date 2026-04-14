import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getRegistrationRequirements, type RegistrationRequirements } from '../features/account/accountApi'
import { loadGoogleScript } from '../features/account/loadGoogleScript'
import { useAuth } from '../features/auth/AuthContext'

export function LoginPage() {
  const { user, login, googleSignIn } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [legal, setLegal] = useState<RegistrationRequirements | null>(null)
  const [acceptTerms, setAcceptTerms] = useState(false)
  const [acceptPrivacy, setAcceptPrivacy] = useState(false)
  const googleBtnRef = useRef<HTMLDivElement>(null)
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim()

  useEffect(() => {
    if (!clientId)
      return
    let cancelled = false
    void (async () => {
      try {
        const r = await getRegistrationRequirements()
        if (!cancelled)
          setLegal(r)
      } catch {
        /* Google sign-in stays hidden if requirements fail */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [clientId])

  useEffect(() => {
    if (!clientId || !legal || !googleBtnRef.current)
      return
    const el = googleBtnRef.current
    if (!acceptTerms || !acceptPrivacy) {
      el.innerHTML = ''
      return
    }

    let cancelled = false
    void (async () => {
      try {
        await loadGoogleScript()
        if (cancelled || !el.isConnected)
          return
        const r = legal
        window.google!.accounts.id.initialize({
          client_id: clientId,
          callback: async (resp) => {
            try {
              setBusy(true)
              setError(null)
              await googleSignIn(resp.credential, [r.termsOfUseVersionId, r.privacyPolicyVersionId])
            } catch {
              setError('Não foi possível entrar com Google.')
            } finally {
              setBusy(false)
            }
          },
        })
        el.innerHTML = ''
        window.google!.accounts.id.renderButton(el, { theme: 'outline', size: 'large', width: 320 })
      } catch {
        setError('Falha ao preparar login Google.')
      }
    })()

    return () => {
      cancelled = true
      el.innerHTML = ''
    }
  }, [clientId, legal, acceptTerms, acceptPrivacy, googleSignIn])

  if (user)
    return <Navigate to="/" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await login(email, password)
    } catch {
      setError('Credenciais inválidas ou usuário inativo.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main style={{ maxWidth: 360, margin: '4rem auto', fontFamily: 'system-ui' }}>
      <h1>Entrar</h1>
      <form onSubmit={onSubmit}>
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
        <label style={{ display: 'block', marginBottom: 16 }}>
          Senha
          <input
            type="password"
            value={password}
            onChange={(ev) => setPassword(ev.target.value)}
            required
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
        <button type="submit" disabled={busy}>
          {busy ? 'Entrando...' : 'Entrar'}
        </button>
      </form>

      {clientId && legal ? (
        <section style={{ marginTop: 32 }}>
          <h2 style={{ fontSize: '1rem' }}>Ou continue com Google</h2>
          <fieldset style={{ border: '1px solid #ccc', padding: 12, marginBottom: 12 }}>
            <legend style={{ fontSize: '0.85rem' }}>LGPD</legend>
            <label style={{ display: 'flex', gap: 8, alignItems: 'flex-start', marginBottom: 8 }}>
              <input
                type="checkbox"
                checked={acceptTerms}
                onChange={(ev) => setAcceptTerms(ev.target.checked)}
              />
              <span>Aceito: {legal.termsTitle}</span>
            </label>
            <label style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
              <input
                type="checkbox"
                checked={acceptPrivacy}
                onChange={(ev) => setAcceptPrivacy(ev.target.checked)}
              />
              <span>Aceito: {legal.privacyTitle}</span>
            </label>
          </fieldset>
          <div ref={googleBtnRef} />
          {!acceptTerms || !acceptPrivacy ? (
            <p style={{ fontSize: '0.85rem', color: '#555' }}>Marque as caixas para habilitar o botão Google.</p>
          ) : null}
        </section>
      ) : null}

      <p style={{ marginTop: 24 }}>
        <Link to="/register">Criar conta</Link>
      </p>
    </main>
  )
}
