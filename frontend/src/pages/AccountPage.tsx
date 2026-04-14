import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { Home, Newspaper, Calendar, CreditCard, User } from 'lucide-react'
import axios from 'axios'
import { getMyProfile, resolvePublicAssetUrl, upsertMyProfile, uploadProfilePhoto } from '../features/account/accountApi'
import { plansService } from '../features/plans/plansService'
import {
  subscriptionsService,
  type CancelMembershipResponse,
  type ChangePlanResponse,
  type MySubscriptionSummary,
  type SubscriptionPaymentMethod,
} from '../features/plans/subscriptionsService'
import { useAuth } from '../features/auth/AuthContext'
import './AppShell.css'

const BOTTOM_NAV = [
  { to: '/', label: 'Início', icon: <Home size={22} /> },
  { to: '/news', label: 'Notícias', icon: <Newspaper size={22} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={22} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={22} /> },
  { to: '/account', label: 'Conta', icon: <User size={22} /> },
]

export function AccountPage() {
  const { user, refreshProfile } = useAuth()
  const [document, setDocument] = useState('')
  const [birthDate, setBirthDate] = useState('')
  const [address, setAddress] = useState('')
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [subscription, setSubscription] = useState<MySubscriptionSummary | null>(null)
  const [subscriptionError, setSubscriptionError] = useState<string | null>(null)
  const [publishedPlans, setPublishedPlans] = useState<Awaited<ReturnType<typeof plansService.listPublished>>['items'] | null>(null)
  const [plansLoadError, setPlansLoadError] = useState<string | null>(null)
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [changePaymentMethod, setChangePaymentMethod] = useState<SubscriptionPaymentMethod>('Pix')
  const [changeBusy, setChangeBusy] = useState(false)
  const [changeError, setChangeError] = useState<string | null>(null)
  const [changeResult, setChangeResult] = useState<ChangePlanResponse | null>(null)
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [cancelBusy, setCancelBusy] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)
  const [cancelResult, setCancelResult] = useState<CancelMembershipResponse | null>(null)

  const scheduledCancellation = useMemo(() => {
    if (!subscription?.hasMembership)
      return false
    return subscription.membershipStatus === 'Ativo' && !!subscription.endDate
  }, [subscription])

  const canRequestCancellation = useMemo(() => {
    if (!subscription?.hasMembership)
      return false
    const st = subscription.membershipStatus
    if (st === 'Cancelado')
      return false
    if (scheduledCancellation)
      return false
    return st === 'Ativo' || st === 'PendingPayment'
  }, [subscription, scheduledCancellation])

  const otherPlans = useMemo(() => {
    if (!publishedPlans || !subscription?.plan)
      return []
    return publishedPlans.filter(p => p.planId !== subscription.plan!.planId)
  }, [publishedPlans, subscription?.plan])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const p = await getMyProfile()
        if (cancelled)
          return
        setDocument(p.document ?? '')
        setBirthDate(p.birthDate ?? '')
        setAddress(p.address ?? '')
        setPhotoUrl(p.photoUrl)
      } catch {
        if (!cancelled)
          setLoadError('Não foi possível carregar o perfil.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const s = await subscriptionsService.getMySummary()
        if (!cancelled)
          setSubscription(s)
      }
      catch {
        if (!cancelled)
          setSubscriptionError('Não foi possível carregar dados da assinatura.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const currentMembershipStatus = subscription?.membershipStatus ?? null
  const currentPlanId = subscription?.plan?.planId ?? null

  useEffect(() => {
    if (currentMembershipStatus !== 'Ativo' || currentPlanId === null) {
      setPublishedPlans(null)
      setPlansLoadError(null)
      setSelectedPlanId('')
      setChangeResult(null)
      return
    }

    let cancelled = false
    setPlansLoadError(null)
    void (async () => {
      try {
        const cat = await plansService.listPublished()
        if (!cancelled)
          setPublishedPlans(cat.items)
      }
      catch {
        if (!cancelled)
          setPlansLoadError('Não foi possível carregar o catálogo de planos.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [currentMembershipStatus, currentPlanId])

  async function refreshSubscription() {
    try {
      const s = await subscriptionsService.getMySummary()
      setSubscription(s)
    }
    catch {
      setSubscriptionError('Não foi possível carregar dados da assinatura.')
    }
  }

  async function onConfirmPlanChange() {
    if (!selectedPlanId)
      return
    setChangeError(null)
    setChangeResult(null)
    setChangeBusy(true)
    try {
      const r = await subscriptionsService.changePlan(selectedPlanId, changePaymentMethod)
      setChangeResult(r)
      await refreshSubscription()
    }
    catch (err) {
      if (axios.isAxiosError(err)) {
        const code = (err.response?.data as { error?: string } | undefined)?.error
        const map: Record<string, string> = {
          membership_not_found: 'Assinatura não encontrada.',
          membership_not_active: 'Só é possível trocar com assinatura ativa.',
          missing_billing_context: 'Não há dados de ciclo de cobrança para calcular o proporcional.',
          plan_not_available: 'Plano indisponível.',
          same_plan: 'Selecione um plano diferente do atual.',
        }
        setChangeError(code ? (map[code] ?? 'Não foi possível concluir a troca.') : 'Não foi possível concluir a troca.')
      }
      else {
        setChangeError('Não foi possível concluir a troca.')
      }
    }
    finally {
      setChangeBusy(false)
    }
  }

  async function onConfirmCancelSubscription() {
    setCancelError(null)
    setCancelBusy(true)
    try {
      const r = await subscriptionsService.cancelMembership()
      setCancelResult(r)
      setShowCancelModal(false)
      await refreshSubscription()
    }
    catch (err) {
      if (axios.isAxiosError(err)) {
        const code = (err.response?.data as { error?: string } | undefined)?.error
        const map: Record<string, string> = {
          membership_not_found: 'Assinatura não encontrada.',
          membership_already_cancelled: 'Esta assinatura já está cancelada.',
          cancellation_already_scheduled: 'O cancelamento já está agendado.',
          membership_not_cancellable: 'Não é possível cancelar esta assinatura no momento.',
          missing_billing_context: 'Não há dados suficientes do ciclo para agendar o cancelamento.',
        }
        setCancelError(code ? (map[code] ?? 'Não foi possível cancelar.') : 'Não foi possível cancelar.')
      }
      else {
        setCancelError('Não foi possível cancelar.')
      }
    }
    finally {
      setCancelBusy(false)
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaveError(null)
    setBusy(true)
    try {
      await upsertMyProfile({
        document: document.trim() || null,
        birthDate: birthDate.trim() || null,
        address: address.trim() || null,
        photoUrl: photoUrl ?? null,
      })
      await refreshProfile()
    } catch {
      setSaveError('Falha ao salvar.')
    } finally {
      setBusy(false)
    }
  }

  async function onPhoto(ev: ChangeEvent<HTMLInputElement>) {
    const file = ev.target.files?.[0]
    if (!file)
      return
    setSaveError(null)
    setBusy(true)
    try {
      const url = await uploadProfilePhoto(file)
      setPhotoUrl(url)
      await upsertMyProfile({ photoUrl: url })
      await refreshProfile()
    } catch {
      setSaveError('Falha no envio da foto (tipo ou tamanho).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
    <main className="app-shell app-shell--narrow account-page" style={{ paddingBottom: '5rem' }}>
      <section className="app-surface">
        <h1 className="app-title">Minha conta</h1>
        <p className="app-muted">
          <strong>{user?.name}</strong>
          {' '}
          ({user?.email})
        </p>
      </section>
      {user?.requiresProfileCompletion ? (
        <p className="account-page__alert-warning">
          Complete seu perfil (documento obrigatório para seguir).
        </p>
      ) : null}
      {subscriptionError ? <p style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{subscriptionError}</p> : null}
      {subscription?.hasMembership ? (
        <section className="app-surface account-page__subscription">
          <h2 className="account-page__section-title" style={{ fontSize: '1.05rem' }}>Assinatura</h2>
          <p style={{ margin: '0.25rem 0' }}>
            <strong>Status:</strong>
            {' '}
            {subscription.membershipStatus ?? '—'}
          </p>
          <p style={{ margin: '0.25rem 0' }}>
            <strong>Próximo vencimento:</strong>
            {' '}
            {subscription.nextDueDate
              ? new Date(subscription.nextDueDate).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
              : '—'}
          </p>
          {subscription.plan ? (
            <p className="app-muted" style={{ margin: '0.25rem 0', fontSize: '0.9rem' }}>
              Plano:
              {' '}
              {subscription.plan.name}
            </p>
          ) : null}
          <p style={{ margin: '0.75rem 0 0' }}>
            <Link to="/digital-card" className="app-back-link">Carteirinha digital</Link>
            {' · '}
            <Link to="/plans" className="app-back-link">Planos</Link>
          </p>
          {subscription.membershipStatus === 'Cancelado' ? (
            <p className="app-muted" style={{ margin: '0.75rem 0 0', fontSize: '0.9rem' }}>
              Assinatura cancelada.
            </p>
          ) : null}
          {scheduledCancellation ? (
            <p className="app-muted" style={{ margin: '0.75rem 0 0', fontSize: '0.9rem' }}>
              Cancelamento agendado. Acesso até
              {' '}
              {subscription.endDate
                ? new Date(subscription.endDate).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
                : '—'}
            </p>
          ) : null}
          {canRequestCancellation ? (
            <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid rgba(119, 177, 137, 0.28)' }}>
              <h3 style={{ margin: '0 0 0.5rem', fontSize: '1rem' }}>Cancelar assinatura</h3>
              <p className="app-muted" style={{ margin: '0 0 0.5rem', fontSize: '0.85rem' }}>
                O clube pode oferecer prazo de arrependimento (configurável). Dentro desse prazo o cancelamento é imediato;
                depois dele, o acesso segue até a data do fim do ciclo atual.
              </p>
              <button
                type="button"
                onClick={() => {
                  setCancelError(null)
                  setShowCancelModal(true)
                }}
                className="btn-secondary"
              >
                Cancelar assinatura
              </button>
              {cancelResult ? (
                <div style={{ marginTop: 12, fontSize: '0.9rem', background: 'rgba(14, 29, 22, 0.6)', padding: 8, borderRadius: 6 }}>
                  <p style={{ margin: 0 }}>{cancelResult.message}</p>
                  {cancelResult.mode === 'ScheduledEndOfCycle' && cancelResult.accessValidUntilUtc ? (
                    <p style={{ margin: '0.5rem 0 0' }}>
                      Acesso até
                      {' '}
                      {new Date(cancelResult.accessValidUntilUtc).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                    </p>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}
          {subscription.membershipStatus === 'Ativo' && subscription.plan ? (
            <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid rgba(119, 177, 137, 0.28)' }}>
              <h3 style={{ margin: '0 0 0.5rem', fontSize: '1rem' }}>Trocar plano</h3>
              {plansLoadError ? <p style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{plansLoadError}</p> : null}
              {!plansLoadError && publishedPlans === null ? (
                <p className="app-muted" style={{ fontSize: '0.9rem' }}>Carregando planos…</p>
              ) : null}
              {!plansLoadError && publishedPlans && otherPlans.length === 0 ? (
                <p className="app-muted" style={{ fontSize: '0.9rem' }}>Não há outros planos publicados para troca.</p>
              ) : null}
              {!plansLoadError && otherPlans.length > 0 ? (
                <>
                  <label className="account-page__field" style={{ display: 'block', marginBottom: 8, fontSize: '0.9rem' }}>
                    Outro plano
                    <select
                      value={selectedPlanId}
                      onChange={(ev) => {
                        setSelectedPlanId(ev.target.value)
                        setChangeResult(null)
                      }}
                      className="app-select"
                    >
                      <option value="">Selecione…</option>
                      {otherPlans.map(p => (
                        <option key={p.planId} value={p.planId}>
                          {p.name}
                          {' '}
                          —
                          {' '}
                          {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(p.price)}
                          {' '}
                          (
                          {p.billingCycle}
                          )
                        </option>
                      ))}
                    </select>
                  </label>
                  <fieldset style={{ border: 'none', padding: 0, margin: '0 0 8px' }}>
                    <legend style={{ fontSize: '0.85rem', marginBottom: 4 }}>Pagamento do proporcional</legend>
                    <label style={{ marginRight: 12, fontSize: '0.9rem' }}>
                      <input
                        type="radio"
                        name="changePlanPm"
                        checked={changePaymentMethod === 'Pix'}
                        onChange={() => setChangePaymentMethod('Pix')}
                      />
                      {' '}
                      Pix
                    </label>
                    <label style={{ fontSize: '0.9rem' }}>
                      <input
                        type="radio"
                        name="changePlanPm"
                        checked={changePaymentMethod === 'Card'}
                        onChange={() => setChangePaymentMethod('Card')}
                      />
                      {' '}
                      Cartão
                    </label>
                  </fieldset>
                  <button
                    type="button"
                    disabled={!selectedPlanId || changeBusy}
                    onClick={() => void onConfirmPlanChange()}
                    className="btn-primary"
                  >
                    {changeBusy ? 'Processando…' : 'Confirmar troca'}
                  </button>
                </>
              ) : null}
              {changeError ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem', marginTop: 8 }}>{changeError}</p> : null}
              {changeResult ? (
                <div style={{ marginTop: 12, fontSize: '0.9rem', background: 'rgba(14, 29, 22, 0.6)', padding: 8, borderRadius: 6 }}>
                  <p style={{ margin: '0 0 0.5rem' }}>
                    <strong>Troca registrada.</strong>
                    {' '}
                    Proporcional:
                    {' '}
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: changeResult.currency || 'BRL' }).format(changeResult.prorationAmount)}
                  </p>
                  {changeResult.prorationAmount > 0 && changeResult.pix ? (
                    <div>
                      <p style={{ margin: '0.25rem 0' }}>PIX — copia e cola:</p>
                      <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all', fontSize: '0.75rem' }}>
                        {changeResult.pix.copyPasteKey ?? changeResult.pix.qrCodePayload}
                      </pre>
                    </div>
                  ) : null}
                  {changeResult.prorationAmount > 0 && changeResult.card ? (
                    <p style={{ margin: '0.5rem 0 0' }}>
                      <a href={changeResult.card.checkoutUrl} target="_blank" rel="noreferrer" className="app-back-link">
                        Abrir checkout do cartão
                      </a>
                    </p>
                  ) : null}
                  {changeResult.prorationAmount === 0 ? (
                    <p className="app-muted" style={{ margin: 0 }}>Sem cobrança proporcional. Seu plano já foi atualizado.</p>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}
        </section>
      ) : !subscriptionError && subscription && !subscription.hasMembership ? (
        <p className="app-muted" style={{ fontSize: '0.95rem' }}>
          Você ainda não possui assinatura de sócio.
          {' '}
          <Link to="/plans" className="app-back-link">Ver planos</Link>
        </p>
      ) : null}
      {loadError ? <p role="alert" style={{ color: '#ffc6c6' }}>{loadError}</p> : null}
      {photoUrl ? (
        <p style={{ textAlign: 'center' }}>
          <img
            src={resolvePublicAssetUrl(photoUrl)}
            alt="Foto"
            style={{ maxWidth: 160, borderRadius: 8 }}
          />
        </p>
      ) : null}
      <form onSubmit={onSubmit} className="app-surface account-page__form">
        <label className="account-page__field">
          Documento (CPF ou equivalente)
          <input
            value={document}
            onChange={(ev) => setDocument(ev.target.value)}
            className="app-input"
          />
        </label>
        <label className="account-page__field">
          Data de nascimento
          <input
            type="date"
            value={birthDate}
            onChange={(ev) => setBirthDate(ev.target.value)}
            className="app-input"
          />
        </label>
        <label className="account-page__field">
          Endereço
          <textarea
            value={address}
            onChange={(ev) => setAddress(ev.target.value)}
            rows={3}
            className="app-textarea"
          />
        </label>
        <label className="account-page__field" style={{ marginBottom: 4 }}>
          Foto do perfil
          <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(ev) => void onPhoto(ev)} disabled={busy} />
        </label>
        {saveError ? <p role="alert" style={{ color: '#ffc6c6' }}>{saveError}</p> : null}
        <button type="submit" disabled={busy} className="btn-primary">
          {busy ? 'Salvando...' : 'Salvar perfil'}
        </button>
      </form>
      <p style={{ marginTop: 24 }}>
        <Link to="/" className="app-back-link">Voltar</Link>
      </p>
      {showCancelModal ? (
        <div
          role="presentation"
          className="account-page__modal-overlay"
          onClick={() => !cancelBusy && setShowCancelModal(false)}
          onKeyDown={(e) => {
            if (e.key === 'Escape' && !cancelBusy)
              setShowCancelModal(false)
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="cancel-dialog-title"
            className="account-page__modal"
            onClick={e => e.stopPropagation()}
            onKeyDown={e => e.stopPropagation()}
          >
            <h2 id="cancel-dialog-title" style={{ margin: '0 0 0.75rem', fontSize: '1.1rem' }}>
              Confirmar cancelamento
            </h2>
            <p style={{ margin: '0 0 1rem', fontSize: '0.9rem' }}>
              Tem certeza? Se você estiver no prazo de arrependimento, o cancelamento será imediato. Caso contrário, você
              mantém o acesso até o fim do período já pago (próximo vencimento).
            </p>
            {cancelError ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem', margin: '0 0 1rem' }}>{cancelError}</p> : null}
            <div className="account-page__modal-actions">
              <button type="button" className="btn-secondary" disabled={cancelBusy} onClick={() => setShowCancelModal(false)}>
                Voltar
              </button>
              <button
                type="button"
                disabled={cancelBusy}
                onClick={() => void onConfirmCancelSubscription()}
                className="btn-danger"
              >
                {cancelBusy ? 'Processando…' : 'Confirmar cancelamento'}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
    <nav className="dash-bottom-nav" aria-label="Navegação principal">
      {BOTTOM_NAV.map(item => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/'}
          className={({ isActive }) =>
            `dash-bottom-nav__item${isActive ? ' active' : ''}`
          }
        >
          {item.icon}
          {item.label}
        </NavLink>
      ))}
    </nav>
    </>
  )
}
