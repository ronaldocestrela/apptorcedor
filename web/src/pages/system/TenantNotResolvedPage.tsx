import type { TenantResolutionFailureReason } from '../../shared/tenant'

const reasonMessages: Record<TenantResolutionFailureReason, string> = {
  empty_host: 'Não foi possível identificar o endereço do site.',
  localhost_or_ip:
    'Acesse pelo subdomínio do clube (ex.: flamengo.seudominio.com), não por localhost ou IP direto.',
  no_subdomain:
    'É necessário um subdomínio de tenant no endereço (ex.: flamengo.seudominio.com). O domínio raiz sem subdomínio não é válido.',
  www_reserved:
    'O prefixo www não identifica um clube. Use o subdomínio do tenant (ex.: flamengo.seudominio.com).',
}

type Props = {
  hostname: string
  reason: TenantResolutionFailureReason
}

export function TenantNotResolvedPage({ hostname, reason }: Props) {
  return (
    <div className="tenant-error">
      <h1 className="tenant-error__title">Tenant não identificado</h1>
      <p className="tenant-error__text">{reasonMessages[reason]}</p>
      <p className="tenant-error__meta">
        <strong>Host atual:</strong> {hostname || '(vazio)'}
      </p>
      <p className="tenant-error__hint">
        O backend espera o header <code>X-Tenant-Id</code> com o <strong>slug</strong> do clube; nesta
        aplicação ele é obtido automaticamente pelo subdomínio.
      </p>
    </div>
  )
}
