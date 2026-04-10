import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/auth/useAuth'
import { ThemeToggle } from '../../app/theme/ThemeToggle'
import { getApiErrorMessage } from '../../shared/auth'

export function RegisterPage() {
  const { register, isAuthenticated } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/member" replace />
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await register({
        email: email.trim(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      })
      navigate('/member', { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err, 'Não foi possível cadastrar.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-page__toolbar">
        <ThemeToggle />
      </div>
      <div className="auth-card">
        <h1 className="auth-card__title">Cadastro</h1>
        <p className="auth-card__subtitle">Crie sua conta neste clube (tenant).</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="auth-field">
            <span className="auth-field__label">Nome</span>
            <input
              className="auth-field__input"
              type="text"
              autoComplete="given-name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              required
            />
          </label>
          <label className="auth-field">
            <span className="auth-field__label">Sobrenome</span>
            <input
              className="auth-field__input"
              type="text"
              autoComplete="family-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              required
            />
          </label>
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
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
            />
          </label>
          {error ? (
            <p className="auth-form__error" role="alert">
              {error}
            </p>
          ) : null}
          <button className="auth-form__submit" type="submit" disabled={loading}>
            {loading ? 'Cadastrando…' : 'Cadastrar'}
          </button>
        </form>
        <p className="auth-card__footer">
          Já tem conta? <Link to="/login">Entrar</Link>
        </p>
      </div>
    </div>
  )
}
