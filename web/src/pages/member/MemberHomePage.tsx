import axios from 'axios'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/auth/useAuth'
import { getApiErrorMessage } from '../../shared/auth'
import { decodeJwtBasicClaims } from '../../shared/auth/tokenStorage'
import { fetchMyMemberProfile, type MemberProfile } from '../../shared/members/membersApi'

const GENDER_LABELS: Record<number, string> = {
  0: 'Masculino',
  1: 'Feminino',
  2: 'Outro',
  3: 'Prefiro não informar',
}

const STATUS_LABELS: Record<number, string> = {
  0: 'Cadastro incompleto',
  1: 'Ativo',
  2: 'Inadimplente',
  3: 'Cancelado',
  4: 'Suspenso',
}

function formatCpf(digits: string): string {
  const d = digits.replace(/\D/g, '')
  if (d.length !== 11) return digits
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}

function formatDate(iso: string): string {
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return iso
  return new Date(t).toLocaleDateString('pt-BR', { timeZone: 'UTC' })
}

function formatDateTime(iso: string): string {
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return iso
  return new Date(t).toLocaleString('pt-BR')
}

export function MemberHomePage() {
  const { session, roles } = useAuth()
  const tokenClaims = useMemo(
    () => (session?.accessToken ? decodeJwtBasicClaims(session.accessToken) : {}),
    [session],
  )

  const [profile, setProfile] = useState<MemberProfile | null>(null)
  const [phase, setPhase] = useState<'loading' | 'profile' | 'no_profile' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      setPhase('loading')
      setErrorMessage(null)
      try {
        const p = await fetchMyMemberProfile()
        if (!cancelled) {
          setProfile(p)
          setPhase('profile')
        }
      } catch (e: unknown) {
        if (cancelled) return
        if (axios.isAxiosError(e) && e.response?.status === 404) {
          setProfile(null)
          setPhase('no_profile')
          return
        }
        setPhase('error')
        setErrorMessage(getApiErrorMessage(e, 'Não foi possível carregar seus dados.'))
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section className="member-home">
      <h1>Minha conta</h1>
      <p className="member-home__lead">
        Dados da sua conta neste clube. Para pagamentos e assinatura, acesse{' '}
        <Link to="/member/billing">Pagamentos</Link>.
      </p>

      {phase === 'loading' ? <p className="member-home__muted">Carregando…</p> : null}

      {phase === 'error' && errorMessage ? (
        <p className="member-home__error" role="alert">
          {errorMessage}
        </p>
      ) : null}

      {phase === 'no_profile' ? (
        <div className="billing-page__block member-home__block">
          <h2>Perfil de sócio</h2>
          <p className="member-home__muted">
            Ainda não há um perfil de sócio cadastrado para sua conta. Complete o cadastro pela API{' '}
            <code>POST /api/members</code> ou aguarde o fluxo no aplicativo.
          </p>
          <dl className="member-home__dl">
            <div className="member-home__row">
              <dt>E-mail (sessão)</dt>
              <dd>{tokenClaims.email ?? '—'}</dd>
            </div>
            <div className="member-home__row">
              <dt>Perfis no clube</dt>
              <dd>{roles.length ? roles.join(', ') : '—'}</dd>
            </div>
          </dl>
        </div>
      ) : null}

      {phase === 'profile' && profile ? (
        <>
          <div className="billing-page__block member-home__block">
            <h2>Identificação</h2>
            <dl className="member-home__dl">
              <div className="member-home__row">
                <dt>E-mail (sessão)</dt>
                <dd>{tokenClaims.email ?? '—'}</dd>
              </div>
              <div className="member-home__row">
                <dt>CPF</dt>
                <dd>{formatCpf(profile.cpfDigits)}</dd>
              </div>
              <div className="member-home__row">
                <dt>Data de nascimento</dt>
                <dd>{formatDate(profile.dateOfBirth)}</dd>
              </div>
              <div className="member-home__row">
                <dt>Gênero</dt>
                <dd>{GENDER_LABELS[profile.gender] ?? `Código ${profile.gender}`}</dd>
              </div>
              <div className="member-home__row">
                <dt>Telefone</dt>
                <dd>{profile.phone || '—'}</dd>
              </div>
              <div className="member-home__row">
                <dt>Status do sócio</dt>
                <dd>{STATUS_LABELS[profile.status] ?? `Código ${profile.status}`}</dd>
              </div>
              <div className="member-home__row">
                <dt>Perfis no clube</dt>
                <dd>{roles.length ? roles.join(', ') : '—'}</dd>
              </div>
            </dl>
          </div>

          <div className="billing-page__block member-home__block">
            <h2>Endereço</h2>
            <dl className="member-home__dl">
              <div className="member-home__row">
                <dt>Logradouro</dt>
                <dd>
                  {profile.address.street}, {profile.address.number}
                  {profile.address.complement ? ` — ${profile.address.complement}` : ''}
                </dd>
              </div>
              <div className="member-home__row">
                <dt>Bairro</dt>
                <dd>{profile.address.neighborhood}</dd>
              </div>
              <div className="member-home__row">
                <dt>Cidade / UF</dt>
                <dd>
                  {profile.address.city} / {profile.address.state}
                </dd>
              </div>
              <div className="member-home__row">
                <dt>CEP</dt>
                <dd>{profile.address.zipCode}</dd>
              </div>
            </dl>
          </div>

          <div className="billing-page__block member-home__block">
            <h2>Registro</h2>
            <dl className="member-home__dl">
              <div className="member-home__row">
                <dt>ID do perfil</dt>
                <dd>
                  <code className="member-home__code">{profile.id}</code>
                </dd>
              </div>
              <div className="member-home__row">
                <dt>ID do usuário</dt>
                <dd>
                  <code className="member-home__code">{profile.userId}</code>
                </dd>
              </div>
              <div className="member-home__row">
                <dt>Cadastrado em</dt>
                <dd>{formatDateTime(profile.createdAt)}</dd>
              </div>
              <div className="member-home__row">
                <dt>Atualizado em</dt>
                <dd>{profile.updatedAt ? formatDateTime(profile.updatedAt) : '—'}</dd>
              </div>
            </dl>
          </div>
        </>
      ) : null}
    </section>
  )
}
