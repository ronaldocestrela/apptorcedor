import axios from 'axios'
import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ThemeToggle } from '../../app/theme/ThemeToggle'
import { getApiErrorMessage } from '../../shared/auth'
import { getBackofficeApiKey, setBackofficeApiKey, validateBackofficeApiKey } from '../../shared/backoffice'

export function BackofficeLoginPage() {
  const navigate = useNavigate()
  const [apiKey, setApiKey] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (getBackofficeApiKey()) {
    return <Navigate to="/backoffice" replace />
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    const trimmed = apiKey.trim()
    if (!trimmed) {
      setError('Informe a chave de API.')
      return
    }
    setLoading(true)
    try {
      await validateBackofficeApiKey(trimmed)
      setBackofficeApiKey(trimmed)
      navigate('/backoffice', { replace: true })
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 503) {
        setError('Backoffice não configurado no servidor (API key ausente).')
      } else {
        setError(getApiErrorMessage(err, 'Chave inválida ou sem permissão.'))
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-page__toolbar">
        <ThemeToggle />
      </div>
      <div className="auth-card bo-login-card">
        <h1 className="auth-card__title">Backoffice SaaS</h1>
        <p className="auth-card__subtitle">
          Informe a chave configurada em <code>Backoffice:ApiKey</code> (header <code>X-Api-Key</code>). Não use
          login de tenant.
        </p>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="auth-field">
            <span className="auth-field__label">Chave de API</span>
            <input
              className="auth-field__input"
              type="password"
              autoComplete="off"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              disabled={loading}
            />
          </label>
          {error ? (
            <p className="billing-page__error" role="alert">
              {error}
            </p>
          ) : null}
          <div className="auth-form__actions">
            <button type="submit" disabled={loading}>
              {loading ? 'Validando…' : 'Entrar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
