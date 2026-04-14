import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { acceptStaffInvite } from '../features/admin/services/adminApi'
import { authStorage } from '../shared/auth/authStorage'
import { useAuth } from '../features/auth/AuthContext'

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
    <div style={{ maxWidth: 420, margin: '2rem auto', fontFamily: 'system-ui', padding: 16 }}>
      <h1>Aceitar convite (staff)</h1>
      <p style={{ color: '#555' }}>Defina sua senha para concluir o cadastro interno.</p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      <form onSubmit={submit}>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Token
          <input
            required
            value={token}
            onChange={(e) => setToken(e.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
            autoComplete="off"
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Nome (opcional)
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Senha
          <input
            required
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
            autoComplete="new-password"
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Confirmar senha
          <input
            required
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
            autoComplete="new-password"
          />
        </label>
        {tokenMismatch ? <p style={{ color: 'crimson' }}>As senhas não conferem.</p> : null}
        <button type="submit" disabled={busy || tokenMismatch} style={{ marginTop: 8 }}>
          Concluir cadastro
        </button>
      </form>
      <p style={{ marginTop: '1.5rem' }}>
        <a href="/login">Ir para login</a>
      </p>
    </div>
  )
}
