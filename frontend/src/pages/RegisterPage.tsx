import { useEffect, useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getRegistrationRequirements, type RegistrationRequirements } from '../features/account/accountApi'
import { TeamShieldLogo } from '../shared/branding/TeamShieldLogo'
import { useAuth } from '../features/auth/AuthContext'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import './RegisterPage.css'

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

  useEffect(() => {
    document.title = 'Cadastro | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
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
    <main className="register-page">
      <section className="register-page__panel">
        <div className="register-page__panel-content">
          <TeamShieldLogo className="register-page__logo" alt="Escudo do clube" width={56} height={56} />
          <h1 className="register-page__title">Crie sua Conta</h1>
          <p className="register-page__subtitle">Preencha os dados abaixo para se registrar.</p>

          {loadError ? <p role="alert" className="register-form__load-error">{loadError}</p> : null}

          {!legal && !loadError ? (
            <p className="register-page__loading">Carregando...</p>
          ) : null}

          {legal ? (
            <form className="register-form" onSubmit={onSubmit}>
              <label className="register-form__field">
                Nome
                <input
                  value={name}
                  onChange={(ev) => setName(ev.target.value)}
                  required
                  autoComplete="name"
                />
              </label>
              <label className="register-form__field">
                E-mail
                <input
                  type="email"
                  value={email}
                  onChange={(ev) => setEmail(ev.target.value)}
                  required
                  autoComplete="email"
                />
              </label>
              <label className="register-form__field">
                Senha
                <input
                  type="password"
                  value={password}
                  onChange={(ev) => setPassword(ev.target.value)}
                  required
                  minLength={8}
                  autoComplete="new-password"
                />
              </label>
              <label className="register-form__field">
                Celular <span style={{ fontWeight: 400, color: '#9b9b9b' }}>(opcional)</span>
                <input
                  type="tel"
                  value={phone}
                  onChange={(ev) => setPhone(ev.target.value)}
                  autoComplete="tel"
                />
              </label>

              <fieldset className="register-consents">
                <legend>LGPD</legend>
                <label>
                  <input
                    type="checkbox"
                    checked={acceptTerms}
                    onChange={(ev) => setAcceptTerms(ev.target.checked)}
                  />
                  <span>Li e aceito: {legal.termsTitle}</span>
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={acceptPrivacy}
                    onChange={(ev) => setAcceptPrivacy(ev.target.checked)}
                  />
                  <span>Li e aceito: {legal.privacyTitle}</span>
                </label>
              </fieldset>

              {error ? <p role="alert" className="register-form__error">{error}</p> : null}

              <button className="register-form__submit" type="submit" disabled={busy}>
                {busy ? 'Cadastrando...' : 'Criar conta'}
              </button>
            </form>
          ) : null}

          <p className="register-page__login">
            Já possui uma conta? <Link to="/login">Entrar</Link>
          </p>
        </div>
      </section>
      <section className="register-page__hero" aria-hidden />
    </main>
  )
}
