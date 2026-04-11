/** Alinhado a `TenantStatus` do backend. */
export const TenantStatus = {
  Active: 0,
  Suspended: 1,
  Inactive: 2,
} as const
export type TenantStatus = (typeof TenantStatus)[keyof typeof TenantStatus]

/** Alinhado a `TenantPlanStatus` do backend. */
export const TenantPlanStatus = {
  Active: 0,
  Expired: 1,
  Revoked: 2,
} as const
export type TenantPlanStatus = (typeof TenantPlanStatus)[keyof typeof TenantPlanStatus]

/** Alinhado a `BillingCycle` do backend. */
export const BillingCycle = {
  Monthly: 0,
  Yearly: 1,
} as const
export type BillingCycle = (typeof BillingCycle)[keyof typeof BillingCycle]

/** Alinhado a `BillingSubscriptionStatus` do backend. */
export const BillingSubscriptionStatus = {
  Pending: 0,
  Active: 1,
  PastDue: 2,
  Canceled: 3,
} as const
export type BillingSubscriptionStatus =
  (typeof BillingSubscriptionStatus)[keyof typeof BillingSubscriptionStatus]

/** Alinhado a `BillingInvoiceStatus` do backend. */
export const BillingInvoiceStatus = {
  Draft: 0,
  Open: 1,
  Paid: 2,
  Void: 3,
  Uncollectible: 4,
} as const
export type BillingInvoiceStatus = (typeof BillingInvoiceStatus)[keyof typeof BillingInvoiceStatus]

export type PagedResult<T> = {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export type TenantDomainDto = {
  id: string
  origin: string
}

export type TenantSettingDto = {
  id: string
  key: string
  value: string
}

export type TenantListItemDto = {
  id: string
  name: string
  slug: string
  status: TenantStatus
  createdAt: string
  domainCount: number
}

export type TenantDetailDto = {
  id: string
  name: string
  slug: string
  connectionString: string
  status: TenantStatus
  createdAt: string
  domains: TenantDomainDto[]
  settings: TenantSettingDto[]
}

export type SaaSPlanFeatureDto = {
  key: string
  description: string | null
  value: string | null
}

export type SaaSPlanDto = {
  id: string
  name: string
  description: string | null
  monthlyPrice: number
  yearlyPrice: number | null
  maxMembers: number
  stripePriceMonthlyId: string | null
  stripePriceYearlyId: string | null
  isActive: boolean
}

export type SaaSPlanDetailDto = SaaSPlanDto & {
  createdAt: string
  updatedAt: string
  features: SaaSPlanFeatureDto[]
}

export type TenantPlanDto = {
  id: string
  tenantId: string
  saaSPlanId: string
  planName: string
  startDate: string
  endDate: string | null
  status: TenantPlanStatus
  billingCycle: BillingCycle
}

export type TenantPlanSummaryDto = {
  tenantId: string
  tenantName: string
  startDate: string
  status: TenantPlanStatus
}

export type TenantSaasBillingSubscriptionDto = {
  id: string
  tenantId: string
  tenantPlanId: string
  saaSPlanId: string
  billingCycle: BillingCycle
  recurringAmount: number
  currency: string
  status: BillingSubscriptionStatus
  externalCustomerId: string | null
  externalSubscriptionId: string | null
  nextBillingAtUtc: string | null
  createdAtUtc: string
}

export type TenantSaasBillingInvoiceDto = {
  id: string
  tenantBillingSubscriptionId: string
  amount: number
  currency: string
  dueAtUtc: string
  status: BillingInvoiceStatus
  externalInvoiceId: string | null
  paidAtUtc: string | null
  createdAtUtc: string
}

export type TenantSaasPortalSessionDto = {
  url: string
}

export type StripeOnboardingLinkDto = {
  url: string
}

export type StripeConnectStatusDto = {
  isConfigured: boolean
  stripeAccountId: string | null
  onboardingStatus: number
  chargesEnabled: boolean
  payoutsEnabled: boolean
  detailsSubmitted: boolean
}
