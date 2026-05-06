import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Gift, Shirt, MapPin, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react'
import { resolvePublicAssetUrl } from '../features/account/accountApi'
import {
  getEligibleBenefitOfferDetail,
  redeemBenefitOffer,
  type TorcedorEligibleBenefitOfferDetail,
} from '../features/torcedor/torcedorBenefitsApi'
import { cepDigitsOnly, lookupViaCep } from '../features/torcedor/viaCep'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function isNotFoundError(e: unknown): boolean {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const r = (e as { response?: { status?: number } }).response
    return r?.status === 404
  }
  return false
}

function redeemErrorMessage(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const data = (e as { response?: { data?: { error?: string } } }).response?.data
    if (data?.error === 'already_redeemed')
      return 'Você já resgatou este benefício ou possui uma solicitação em análise.'
    if (data?.error === 'not_eligible')
      return 'Você não está elegível para este benefício no momento.'
    if (data?.error === 'validation_failed')
      return 'Verifique os dados da camisa, endereço de entrega e tente novamente.'
  }
  return 'Não foi possível concluir o resgate. Tente novamente.'
}

const shirtNumberOk = (v: string) => {
  const trimmed = v.trim()
  return /^(?:[0-9]|[1-9][0-9])$/.test(trimmed)
}
const shirtNameOk = (v: string) => /^[\p{L}0-9'\- ]{1,10}$/u.test(v.trim())

export function BenefitOfferDetailPage() {
  const { offerId } = useParams<{ offerId: string }>()
  const [detail, setDetail] = useState<TorcedorEligibleBenefitOfferDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [redeeming, setRedeeming] = useState(false)
  const [redeemError, setRedeemError] = useState<string | null>(null)

  const [shirtSize, setShirtSize] = useState('')
  const [shirtModel, setShirtModel] = useState('')
  const [shirtNumber, setShirtNumber] = useState('')
  const [shirtName, setShirtName] = useState('')

  const [deliveryCep, setDeliveryCep] = useState('')
  const [deliveryNeighborhood, setDeliveryNeighborhood] = useState('')
  const [deliveryStreet, setDeliveryStreet] = useState('')
  const [deliveryNumber, setDeliveryNumber] = useState('')
  const [deliveryCity, setDeliveryCity] = useState('')
  const [deliveryState, setDeliveryState] = useState('')
  const [cepHint, setCepHint] = useState<string | null>(null)
  const [cepBusy, setCepBusy] = useState(false)

  const load = useCallback(async () => {
    if (!offerId) {
      setNotFound(true)
      setLoading(false)
      return
    }
    try {
      setLoading(true)
      setError(null)
      setNotFound(false)
      const d = await getEligibleBenefitOfferDetail(offerId)
      setDetail(d)
    }
    catch (e) {
      if (isNotFoundError(e)) {
        setNotFound(true)
        setDetail(null)
      }
      else {
        setError(e instanceof Error ? e.message : 'Erro ao carregar benefício')
      }
    }
    finally {
      setLoading(false)
    }
  }, [offerId])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    const digits = cepDigitsOnly(deliveryCep)
    if (digits.length !== 8) {
      setCepHint(null)
      setCepBusy(false)
      return
    }

    const handle = window.setTimeout(() => {
      void (async () => {
        setCepBusy(true)
        setCepHint(null)
        try {
          const r = await lookupViaCep(digits)
          if (!r) {
            setCepHint('CEP não encontrado. Informe rua, bairro, cidade e UF manualmente.')
          }
          else {
            setDeliveryStreet(r.street)
            setDeliveryNeighborhood(r.neighborhood)
            setDeliveryCity(r.city)
            setDeliveryState(r.state)
            setCepHint('Dados preenchidos pelo CEP. Confira e ajuste se necessário.')
          }
        }
        catch {
          setCepHint('Não foi possível consultar o CEP. Preencha o endereço manualmente.')
        }
        finally {
          setCepBusy(false)
        }
      })()
    }, 450)

    return () => window.clearTimeout(handle)
  }, [deliveryCep])

  const canRequestShirt = useMemo(() => {
    if (!detail?.isShirtCustomizationOffer)
      return false
    const w = detail.redemptionWorkflowStatus?.toLowerCase() ?? 'none'
    return !detail.alreadyRedeemed && (w === 'none' || w === 'rejected')
  }, [detail])

  const shirtNumberTrimmed = shirtNumber.trim()
  const shirtNumberInvalid = shirtNumberTrimmed.length > 0 && !shirtNumberOk(shirtNumber)
  const shirtNameTrimmed = shirtName.trim()
  const shirtNameInvalid = shirtNameTrimmed.length > 0 && !shirtNameOk(shirtName)

  const deliveryCepDigits = cepDigitsOnly(deliveryCep)
  const deliveryCepInvalid = deliveryCep.trim().length > 0 && deliveryCepDigits.length !== 8
  const deliveryStateNorm = deliveryState.trim().toUpperCase()
  const stateInvalid = deliveryStateNorm.length > 0 && !/^[A-Z]{2}$/.test(deliveryStateNorm)

  const shirtBlockingMessage = useMemo(() => {
    if (!detail?.isShirtCustomizationOffer)
      return null
    const w = detail.redemptionWorkflowStatus?.toLowerCase() ?? 'none'
    if (w === 'pending')
      return 'Sua solicitação de camisa está em análise pela equipe do clube.'
    return null
  }, [detail])

  async function handleRedeem() {
    if (!offerId || !detail || detail.alreadyRedeemed)
      return
    try {
      setRedeeming(true)
      setRedeemError(null)
      if (detail.isShirtCustomizationOffer) {
        if (!canRequestShirt)
          return
        if (!shirtSize || !shirtModel || !shirtNumber.trim() || !shirtName.trim()) {
          setRedeemError('Preencha tamanho, modelo, número e nome para a camisa.')
          return
        }
        if (!shirtNumberOk(shirtNumber) || !shirtNameOk(shirtName)) {
          setRedeemError('Número (0 a 99) ou nome inválido (máx. 10 caracteres).')
          return
        }
        if (deliveryCepDigits.length !== 8) {
          setRedeemError('Informe um CEP válido (8 dígitos).')
          return
        }
        if (
          !deliveryNeighborhood.trim()
          || !deliveryStreet.trim()
          || !deliveryNumber.trim()
          || !deliveryCity.trim()
          || !/^[A-Z]{2}$/.test(deliveryStateNorm)
        ) {
          setRedeemError('Preencha bairro, rua, número, cidade e UF (2 letras).')
          return
        }
        await redeemBenefitOffer(offerId, {
          shirtSize,
          shirtModel,
          shirtNumber: shirtNumber.trim(),
          shirtDisplayName: shirtName.trim(),
          deliveryCep: deliveryCepDigits,
          deliveryNeighborhood: deliveryNeighborhood.trim(),
          deliveryStreet: deliveryStreet.trim(),
          deliveryNumber: deliveryNumber.trim(),
          deliveryCity: deliveryCity.trim(),
          deliveryState: deliveryStateNorm,
        })
      }
      else {
        await redeemBenefitOffer(offerId)
      }
      await load()
    }
    catch (e) {
      setRedeemError(redeemErrorMessage(e))
    }
    finally {
      setRedeeming(false)
    }
  }

  return (
    <div className="benefit-detail-root">
      <header className="subpage-header">
        <Link to="/benefits" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Detalhe do benefício</h1>
      </header>

      <main className="subpage-content">
        {loading ? (
          <div className="benefit-detail__loading">
            <Loader2 size={28} className="benefit-detail__loading-icon" />
            <span>Carregando…</span>
          </div>
        ) : null}

        {error ? (
          <div role="alert" className="benefit-detail__alert benefit-detail__alert--error">
            <AlertCircle size={16} />
            <span>{error}</span>
          </div>
        ) : null}

        {notFound ? (
          <p className="benefit-detail__not-found">
            Benefício não encontrado ou não disponível para você.
          </p>
        ) : null}

        {!loading && detail ? (
          <article className="benefit-detail-card">
            {/* ── Banner ou ícone ── */}
            {detail.bannerUrl?.trim() && resolvePublicAssetUrl(detail.bannerUrl) ? (
              <div className="benefit-detail-card__media">
                <img src={resolvePublicAssetUrl(detail.bannerUrl) ?? ''} alt="" loading="lazy" />
              </div>
            ) : (
              <div className="benefit-detail-card__header-row">
                <div className="benefit-detail-card__icon-wrap" aria-hidden>
                  <Gift size={28} />
                </div>
                <div>
                  <h2 className="benefit-detail-card__title">{detail.title}</h2>
                  <span className="benefit-detail-card__partner">{detail.partnerName}</span>
                </div>
              </div>
            )}

            {detail.bannerUrl?.trim() && resolvePublicAssetUrl(detail.bannerUrl) ? (
              <div className="benefit-detail-card__meta-row">
                <h2 className="benefit-detail-card__title">{detail.title}</h2>
                <span className="benefit-detail-card__partner">{detail.partnerName}</span>
              </div>
            ) : null}

            {detail.description ? (
              <p className="benefit-detail-card__description">{detail.description}</p>
            ) : null}

            <p className="benefit-detail-card__dates">
              Válido de
              {' '}
              <strong>{new Date(detail.startAt).toLocaleDateString('pt-BR')}</strong>
              {' '}
              até
              {' '}
              <strong>{new Date(detail.endAt).toLocaleDateString('pt-BR')}</strong>
            </p>

            {/* ── Status badges ── */}
            {shirtBlockingMessage ? (
              <div className="benefit-detail__alert benefit-detail__alert--info">
                <Loader2 size={15} className="benefit-detail__spin" />
                <span>{shirtBlockingMessage}</span>
              </div>
            ) : null}

            {detail.redemptionWorkflowStatus?.toLowerCase() === 'rejected' && !detail.alreadyRedeemed ? (
              <div className="benefit-detail__alert benefit-detail__alert--warning">
                <AlertCircle size={15} />
                <span>Sua solicitação anterior foi recusada. Você pode enviar uma nova personalização.</span>
              </div>
            ) : null}

            {detail.alreadyRedeemed && detail.redemptionDateUtc ? (
              <div className="benefit-detail__alert benefit-detail__alert--success">
                <CheckCircle2 size={15} />
                <span>
                  Benefício concluído em
                  {' '}
                  {new Date(detail.redemptionDateUtc).toLocaleString('pt-BR')}
                </span>
              </div>
            ) : null}

            {redeemError ? (
              <div role="alert" className="benefit-detail__alert benefit-detail__alert--error">
                <AlertCircle size={15} />
                <span>{redeemError}</span>
              </div>
            ) : null}

            {/* ── Formulário de camisa ── */}
            {detail.isShirtCustomizationOffer && canRequestShirt ? (
              <div className="benefit-shirt-form">
                {/* Personalização */}
                <div className="benefit-shirt-form__section">
                  <div className="benefit-shirt-form__section-header">
                    <Shirt size={16} />
                    <span>Personalização da camisa</span>
                  </div>

                  <div className="benefit-shirt-form__row-2">
                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Tamanho</span>
                      <select
                        className="app-select"
                        value={shirtSize}
                        onChange={(e) => setShirtSize(e.target.value)}
                        required
                      >
                        <option value="">Selecione…</option>
                        {detail.shirtSizes.map((s) => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </select>
                    </label>

                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Modelo</span>
                      <select
                        className="app-select"
                        value={shirtModel}
                        onChange={(e) => setShirtModel(e.target.value)}
                        required
                      >
                        <option value="">Selecione…</option>
                        {detail.shirtModels.map((m) => (
                          <option key={m} value={m}>{m}</option>
                        ))}
                      </select>
                    </label>
                  </div>

                  <div className="benefit-shirt-form__row-2">
                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Número</span>
                      <input
                        className="app-input"
                        type="number"
                        value={shirtNumber}
                        onChange={(e) => setShirtNumber(e.target.value)}
                        inputMode="numeric"
                        min={0}
                        max={99}
                        step={1}
                        placeholder="10"
                      />
                      <span className="benefit-shirt-form__hint">0 a 99</span>
                      {shirtNumberInvalid ? (
                        <span className="benefit-shirt-form__field-error">
                          Use um valor entre 0 e 99.
                        </span>
                      ) : null}
                    </label>

                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Nome na camisa</span>
                      <input
                        className="app-input"
                        value={shirtName}
                        onChange={(e) => setShirtName(e.target.value)}
                        maxLength={10}
                        placeholder="Como exibir"
                      />
                      <span className="benefit-shirt-form__hint">Máx. 10 caracteres</span>
                      {shirtNameInvalid ? (
                        <span className="benefit-shirt-form__field-error">
                          Nome inválido (máx. 10 caracteres).
                        </span>
                      ) : null}
                    </label>
                  </div>

                  {/* Preview da camisa */}
                  {(shirtSize || shirtModel || shirtNumberTrimmed || shirtNameTrimmed) ? (
                    <div className="benefit-shirt-form__preview">
                      <Shirt size={14} />
                      <span>
                        {[shirtModel, shirtSize, shirtNumberTrimmed && `#${shirtNumberTrimmed}`, shirtNameTrimmed].filter(Boolean).join(' · ')}
                      </span>
                    </div>
                  ) : null}
                </div>

                {/* Endereço de entrega */}
                <div className="benefit-shirt-form__section">
                  <div className="benefit-shirt-form__section-header">
                    <MapPin size={16} />
                    <span>Endereço de entrega</span>
                  </div>

                  <label className="benefit-shirt-form__field">
                    <span className="benefit-shirt-form__label">CEP</span>
                    <div className="benefit-shirt-form__cep-row">
                      <input
                        className="app-input"
                        value={deliveryCep}
                        onChange={(e) => setDeliveryCep(e.target.value)}
                        inputMode="numeric"
                        autoComplete="postal-code"
                        placeholder="00000-000"
                        maxLength={9}
                      />
                      {cepBusy ? (
                        <Loader2 size={16} className="benefit-shirt-form__cep-spin" />
                      ) : null}
                    </div>
                    {cepHint ? (
                      <span className={`benefit-shirt-form__hint${cepHint.startsWith('Dados') ? ' benefit-shirt-form__hint--ok' : ''}`}>
                        {cepHint}
                      </span>
                    ) : (
                      <span className="benefit-shirt-form__hint">
                        8 dígitos — buscamos o endereço automaticamente.
                      </span>
                    )}
                    {deliveryCepInvalid ? (
                      <span className="benefit-shirt-form__field-error">CEP deve ter 8 dígitos.</span>
                    ) : null}
                  </label>

                  <label className="benefit-shirt-form__field">
                    <span className="benefit-shirt-form__label">Rua / Avenida</span>
                    <input
                      className="app-input"
                      value={deliveryStreet}
                      onChange={(e) => setDeliveryStreet(e.target.value)}
                      maxLength={200}
                      autoComplete="street-address"
                      placeholder="Nome da via"
                    />
                  </label>

                  <div className="benefit-shirt-form__row-2">
                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Número</span>
                      <input
                        className="app-input"
                        value={deliveryNumber}
                        onChange={(e) => setDeliveryNumber(e.target.value)}
                        maxLength={20}
                        placeholder="123"
                      />
                    </label>

                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Bairro</span>
                      <input
                        className="app-input"
                        value={deliveryNeighborhood}
                        onChange={(e) => setDeliveryNeighborhood(e.target.value)}
                        maxLength={120}
                        placeholder="Bairro"
                      />
                    </label>
                  </div>

                  <div className="benefit-shirt-form__row-2">
                    <label className="benefit-shirt-form__field">
                      <span className="benefit-shirt-form__label">Cidade</span>
                      <input
                        className="app-input"
                        value={deliveryCity}
                        onChange={(e) => setDeliveryCity(e.target.value)}
                        maxLength={120}
                        placeholder="São Paulo"
                      />
                    </label>

                    <label className="benefit-shirt-form__field benefit-shirt-form__field--uf">
                      <span className="benefit-shirt-form__label">UF</span>
                      <input
                        className="app-input"
                        value={deliveryState}
                        onChange={(e) =>
                          setDeliveryState(e.target.value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 2))}
                        maxLength={2}
                        placeholder="SP"
                      />
                      {stateInvalid ? (
                        <span className="benefit-shirt-form__field-error">2 letras (ex.: SP).</span>
                      ) : null}
                    </label>
                  </div>
                </div>

                <p className="benefit-shirt-form__footer-note">
                  Após o envio, a equipe do clube analisará os dados antes da produção.
                </p>
              </div>
            ) : null}

            {/* ── Botão de ação ── */}
            {!detail.alreadyRedeemed && !shirtBlockingMessage && (detail.isShirtCustomizationOffer ? canRequestShirt : true) ? (
              <button
                type="button"
                className="btn-primary benefit-detail-redeem-btn"
                disabled={redeeming}
                onClick={() => void handleRedeem()}
              >
                {redeeming
                  ? (
                      <span className="benefit-detail__btn-inner">
                        <Loader2 size={16} className="benefit-detail__spin" />
                        Enviando…
                      </span>
                    )
                  : detail.isShirtCustomizationOffer
                    ? 'Enviar solicitação de camisa'
                    : 'Resgatar benefício'}
              </button>
            ) : null}
          </article>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
