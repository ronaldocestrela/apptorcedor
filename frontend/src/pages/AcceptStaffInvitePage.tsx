import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { acceptStaffInvite } from '../features/admin/services/adminApi'
import { authStorage } from '../shared/auth/authStorage'
import { useAuth } from '../features/auth/AuthContext'
import './AppShell.css'

export function AcceptStaffInvitePage() {
  const [params] = useSearchParams()
  const tokenFromQuery = params.get('token') ?? ''
  const [token, setToken] = useState(tokenFromQuery)

  useEffect(() => {
    if (tokenFromQuery)
      setToken(tokenFromQuery)
  }, [tokenFromQuery])
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const navigate = useNavigate()
  const { refreshProfile } = useAuth()

  const tokenMismatch = useMemo(() => password.length > 0 && password !== confirm, [password, confirm])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (password !== confirm) {
      setError('As senhas não conferem.')
      return
    }
    setBusy(true)
    try {
      const data = await acceptStaffInvite({
        token: token.trim(),
        password,
        name: name.trim() || null,
      })
      authStorage.setTokens(data.accessToken, data.refreshToken)
      await refreshProfile()
      navigate('/', { replace: true })
    } catch {
      setError('Convite inválido, expirado ou senha não atende às regras.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="accept-staff-invite-root">
      <main className="accept-staff-invite-page">
        <h1 className="accept-staff-invite-page__title">Aceitar convite (staff)</h1>
        <p className="accept-staff-invite-page__lead">Defina sua senha para concluir o cadastro interno.</p>
        {error ? <p className="accept-staff-invite-page__error" role="alert">{error}</p> : null}
        <form className="accept-staff-invite-page__form" onSubmit={submit}>
          <label>
            Token
            <input
              required
              value={token}
              onChange={(e) => setToken(e.target.value)}
              autoComplete="off"
            />
          </label>
          <label>
            Nome (opcional)
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </label>
          <label>
            Senha
            <input
              required
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="new-password"
            />
          </label>
          <label>
            Confirmar senha
            <input
              required
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              autoComplete="new-password"
            />
          </label>
          {tokenMismatch ? <p className="accept-staff-invite-page__warning">As senhas não conferem.</p> : null}
          <button type="submit" className="accept-staff-invite-page__submit" disabled={busy || tokenMismatch}>
            Concluir cadastro
          </button>
        </form>
        <p className="accept-staff-invite-page__footer">
          <a href="/login">Ir para login</a>
        </p>
      </main>
    </div>
  )
}
