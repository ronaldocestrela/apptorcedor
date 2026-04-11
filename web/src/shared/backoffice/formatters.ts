import {
  BillingCycle,
  BillingInvoiceStatus,
  BillingSubscriptionStatus,
  TenantPlanStatus,
  TenantStatus,
  type BillingCycle as BillingCycleT,
} from './types'

export function formatTenantStatus(s: TenantStatus): string {
  switch (s) {
    case TenantStatus.Active:
      return 'Ativo'
    case TenantStatus.Suspended:
      return 'Suspenso'
    case TenantStatus.Inactive:
      return 'Inativo'
    default:
      return String(s)
  }
}

export function formatTenantPlanStatus(s: TenantPlanStatus): string {
  switch (s) {
    case TenantPlanStatus.Active:
      return 'Ativo'
    case TenantPlanStatus.Expired:
      return 'Expirado'
    case TenantPlanStatus.Revoked:
      return 'Revogado'
    default:
      return String(s)
  }
}

export function formatBillingCycle(c: BillingCycleT): string {
  switch (c) {
    case BillingCycle.Monthly:
      return 'Mensal'
    case BillingCycle.Yearly:
      return 'Anual'
    default:
      return String(c)
  }
}

export function formatBillingSubscriptionStatus(s: BillingSubscriptionStatus): string {
  switch (s) {
    case BillingSubscriptionStatus.Pending:
      return 'Pendente'
    case BillingSubscriptionStatus.Active:
      return 'Ativo'
    case BillingSubscriptionStatus.PastDue:
      return 'Em atraso'
    case BillingSubscriptionStatus.Canceled:
      return 'Cancelado'
    default:
      return String(s)
  }
}

export function formatBillingInvoiceStatus(s: BillingInvoiceStatus): string {
  switch (s) {
    case BillingInvoiceStatus.Draft:
      return 'Rascunho'
    case BillingInvoiceStatus.Open:
      return 'Aberta'
    case BillingInvoiceStatus.Paid:
      return 'Paga'
    case BillingInvoiceStatus.Void:
      return 'Anulada'
    case BillingInvoiceStatus.Uncollectible:
      return 'Incobrável'
    default:
      return String(s)
  }
}
