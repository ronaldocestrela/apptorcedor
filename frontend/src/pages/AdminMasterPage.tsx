import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../shared/api/http'

export function AdminMasterPage() {
  const [ok, setOk] = useState<boolean | null>(null)

  useEffect(() => {
    const run = async () => {
      try {
        await api.get('/api/diagnostics/admin-master-only')
        setOk(true)
      } catch {
        setOk(false)
      }
    }
    void run()
  }, [])

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>Admin Master</h1>
      <p>
        <Link to="/">Voltar</Link>
      </p>
      {ok === null ? <p>Verificando permissão...</p> : null}
      {ok === true ? <p>Acesso autorizado à rota protegida.</p> : null}
      {ok === false ? <p>Sem permissão para esta rota.</p> : null}
    </main>
  )
}
