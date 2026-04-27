import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { requestPasswordReset } from '../features/auth/passwordResetApi'
import { TeamShieldLogo } from '../shared/branding/TeamShieldLogo'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import './LoginPage.css'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    document.title = 'Esqueci minha senha | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await requestPasswordReset(email)
      setDone(true)
    } catch {
      setError('Não foi possível enviar o e-mail. Tente novamente.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-page__panel">
        <div className="login-page__panel-content">
          <TeamShieldLogo className="login-page__logo" alt="Escudo do clube" width={56} height={56} />
          <h1 className="login-page__title">Esqueci minha senha</h1>
          <p className="login-page__subtitle">
            Informe seu e-mail. Se existir uma conta ativa, enviaremos um link para redefinir a senha.
          </p>

          {done ? (
            <p className="login-form__hint" role="status">
              Se o e-mail estiver cadastrado, você receberá as instruções em instantes. Verifique também a caixa de spam.
            </p>
          ) : (
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
              {error ? <p role="alert" className="login-form__error">{error}</p> : null}
              <button className="login-form__submit" type="submit" disabled={busy}>
                {busy ? 'Enviando...' : 'Enviar link'}
              </button>
            </form>
          )}

          <p className="login-page__register">
            <Link to="/login">Voltar ao login</Link>
          </p>
        </div>
      </section>
      <section className="login-page__hero" aria-hidden />
    </main>
  )
}
