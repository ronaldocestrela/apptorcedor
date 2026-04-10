import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/auth/useAuth'
import { getApiErrorMessage } from '../../shared/auth'

export function LoginPage() {
  const { login, isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const fromPath =
    (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/member'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to={fromPath} replace />
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await login({ email: email.trim(), password })
      navigate(fromPath, { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err, 'Não foi possível entrar.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="auth-card__title">Entrar</h1>
        <p className="auth-card__subtitle">Use sua conta do tenant atual.</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="auth-field">
            <span className="auth-field__label">E-mail</span>
            <input
              className="auth-field__input"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>
          <label className="auth-field">
            <span className="auth-field__label">Senha</span>
            <input
              className="auth-field__input"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>
          {error ? (
            <p className="auth-form__error" role="alert">
              {error}
            </p>
          ) : null}
          <button className="auth-form__submit" type="submit" disabled={loading}>
            {loading ? 'Entrando…' : 'Entrar'}
          </button>
        </form>
        <p className="auth-card__footer">
          Não tem conta? <Link to="/register">Cadastre-se</Link>
        </p>
      </div>
    </div>
  )
}
