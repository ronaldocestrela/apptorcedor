import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link, Navigate, useSearchParams } from 'react-router-dom'
import {
  formatResetPasswordApiErrorMessage,
  resetPassword,
} from '../features/auth/passwordResetApi'
import {
  evaluatePublicRegisterPasswordRules,
  publicRegisterPasswordMeetsAllRules,
} from '../features/account/registerPasswordRules'
import { TeamShieldLogo } from '../shared/branding/TeamShieldLogo'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import './LoginPage.css'
import './RegisterPage.css'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const emailParam = searchParams.get('email') ?? ''
  const tokenParam = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const passwordRuleStates = useMemo(() => evaluatePublicRegisterPasswordRules(password), [password])
  const allPasswordRulesMet = publicRegisterPasswordMeetsAllRules(password)

  useEffect(() => {
    document.title = 'Nova senha | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  const missingParams = !emailParam.trim() || !tokenParam.trim()

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!publicRegisterPasswordMeetsAllRules(password)) {
      setError('A senha não atende a todos os requisitos abaixo.')
      return
    }
    if (password !== confirm) {
      setError('As senhas não coincidem.')
      return
    }
    setBusy(true)
    try {
      await resetPassword(emailParam, tokenParam, password)
      setSuccess(true)
    } catch (err) {
      setError(formatResetPasswordApiErrorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  if (success)
    return <Navigate to="/login?reset=success" replace />

  return (
    <main className="login-page">
      <section className="login-page__panel">
        <div className="login-page__panel-content">
          <TeamShieldLogo className="login-page__logo" alt="Escudo do clube" width={56} height={56} />
          <h1 className="login-page__title">Definir nova senha</h1>
          <p className="login-page__subtitle">Escolha uma senha forte para sua conta.</p>

          {missingParams ? (
            <p role="alert" className="login-form__error">
              Link inválido ou incompleto. Solicite um novo e-mail de recuperação.
            </p>
          ) : (
            <form className="login-form" onSubmit={onSubmit}>
              <p className="login-form__hint">
                Conta:
                {' '}
                <strong>{emailParam}</strong>
              </p>
              <label className="login-form__field">
                Nova senha
                <input
                  type="password"
                  value={password}
                  onChange={(ev) => setPassword(ev.target.value)}
                  required
                  autoComplete="new-password"
                  aria-describedby="reset-password-rules"
                />
              </label>
              <ul id="reset-password-rules" className="register-password-rules" aria-live="polite">
                {passwordRuleStates.map((rule) => (
                  <li
                    key={rule.id}
                    className={
                      rule.met
                        ? 'register-password-rules__item register-password-rules__item--met'
                        : 'register-password-rules__item register-password-rules__item--pending'
                    }
                  >
                    <span className="register-password-rules__mark" aria-hidden>
                      {rule.met ? '✓' : '○'}
                    </span>
                    {rule.label}
                  </li>
                ))}
              </ul>
              <label className="login-form__field">
                Confirmar senha
                <input
                  type="password"
                  value={confirm}
                  onChange={(ev) => setConfirm(ev.target.value)}
                  required
                  autoComplete="new-password"
                />
              </label>
              {error ? <p role="alert" className="login-form__error">{error}</p> : null}
              <button
                className="login-form__submit"
                type="submit"
                disabled={busy || !allPasswordRulesMet}
              >
                {busy ? 'Salvando...' : 'Salvar nova senha'}
              </button>
            </form>
          )}

          <p className="login-page__register">
            <Link to="/login">Voltar ao login</Link>
            {' · '}
            <Link to="/forgot-password">Solicitar novo link</Link>
          </p>
        </div>
      </section>
      <section className="login-page__hero" aria-hidden />
    </main>
  )
}
