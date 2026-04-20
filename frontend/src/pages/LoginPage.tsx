import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getRegistrationRequirements, type RegistrationRequirements } from '../features/account/accountApi'
import { TeamShieldLogo } from '../shared/branding/TeamShieldLogo'
import { loadGoogleScript } from '../features/account/loadGoogleScript'
import { useAuth } from '../features/auth/AuthContext'
import './LoginPage.css'

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
    <main className="login-page">
      <section className="login-page__panel">
        <div className="login-page__panel-content">
          <TeamShieldLogo className="login-page__logo" alt="Escudo do clube" width={56} height={56} />
          <h1 className="login-page__title">Acesse a sua Conta</h1>
          <p className="login-page__subtitle">Insira seu e-mail e senha para prosseguir.</p>

          <form className="login-form" onSubmit={onSubmit}>
            <label className="login-form__field">
              Email
              <input
                type="email"
                value={email}
                onChange={(ev) => setEmail(ev.target.value)}
                required
                autoComplete="email"
              />
            </label>
            <label className="login-form__field">
              Senha
              <input
                type="password"
                value={password}
                onChange={(ev) => setPassword(ev.target.value)}
                required
                autoComplete="current-password"
              />
            </label>
            <Link className="login-form__forgot" to="/support">Esqueceu sua senha?</Link>
            {error ? <p role="alert" className="login-form__error">{error}</p> : null}
            <button className="login-form__submit" type="submit" disabled={busy}>
              {busy ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <div className="login-page__divider"><span>Ou continue com</span></div>

          {clientId && legal ? (
            <>
              <fieldset className="login-consents">
                <legend>LGPD</legend>
                <label>
                  <input
                    type="checkbox"
                    checked={acceptTerms}
                    onChange={(ev) => setAcceptTerms(ev.target.checked)}
                  />
                  <span>Aceito: {legal.termsTitle}</span>
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={acceptPrivacy}
                    onChange={(ev) => setAcceptPrivacy(ev.target.checked)}
                  />
                  <span>Aceito: {legal.privacyTitle}</span>
                </label>
              </fieldset>
              <div className="login-form__google" ref={googleBtnRef} />
              {!acceptTerms || !acceptPrivacy ? (
                <p className="login-form__hint">Marque as caixas para habilitar o botão Google.</p>
              ) : null}
            </>
          ) : (
            <button className="login-form__social" type="button" disabled>
              Entrar com Google
            </button>
          )}
          <button className="login-form__social" type="button" disabled>
            Entrar com Apple
          </button>

          <p className="login-page__register">
            Não possui uma conta? <Link to="/register">Cadastrar-se</Link>
          </p>
        </div>
      </section>
      <section className="login-page__hero" aria-hidden />
    </main>
  )
}
