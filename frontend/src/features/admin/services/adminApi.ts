import { api } from '../../../shared/api/http'

/** Chaves em `AppConfigurationEntries` para o e-mail de boas-vindas (alinhado ao backend `EmailWelcomeTemplateKeys`). */
export const EMAIL_WELCOME_TEMPLATE_KEYS = {
  Subject: 'Email.Welcome.Subject',
  Html: 'Email.Welcome.Html',
  ImageUrl: 'Email.Welcome.ImageUrl',
} as const

export const EMAIL_WELCOME_TEMPLATE_KEY_SET = new Set<string>(Object.values(EMAIL_WELCOME_TEMPLATE_KEYS))

/**
 * Valida URL de banner opcional: vazio é permitido; caso contrário deve ser `http:` ou `https:` absoluto.
 * (O backend ainda revalida antes de injetar no HTML.)
 */
export function isValidWelcomeBannerImageUrl(raw: string): boolean {
  const t = raw.trim()
  if (t.length === 0)
    return true
  try {
    const u = new URL(t)
    return u.protocol === 'http:' || u.protocol === 'https:'
  }
  catch {
    return false
  }
}

/** Axios default is application/json; for FormData the browser must set multipart boundary. */
function formDataMultipartTransform(body: unknown, headers: Record<string, string>) {
  if (body instanceof FormData)
    delete headers['Content-Type']
  return body
}

export type DiagnosticsResult = {
  ok: boolean
  databaseConnected: boolean
}

export type AppConfigurationEntry = {
  key: string
  value: string
  version: number
  updatedAt: string
  updatedByUserId: string | null
}

export type RolePermissionRow = {
  roleName: string
  permissionName: string
}

export type AuditLogRow = {
  id: string
  actorUserId: string | null
  action: string
  entityType: string
  entityId: string
  oldValues: string | null
  newValues: string | null
  correlationId: string | null
  createdAt: string
}

export type MembershipStatus =
  | 'NaoAssociado'
  | 'Ativo'
  | 'Inadimplente'
  | 'Suspenso'
  | 'Cancelado'
  | 'PendingPayment'

export type AdminDashboardResult = {
  activeMembersCount: number
  delinquentMembersCount: number
  openSupportTickets: number
  totalFaturadoLast30Days: number
}

export type StaffInviteRow = {
  id: string
  email: string
  name: string
  roles: string[]
  createdAt: string
  expiresAt: string
}

export type StaffUserRow = {
  id: string
  email: string
  name: string
  isActive: boolean
  roles: string[]
}

export type CreateStaffInviteResult = {
  id: string
  token: string
  expiresAt: string
}

export async function getDiagnostics(): Promise<DiagnosticsResult> {
  const { data } = await api.get<DiagnosticsResult>('/api/diagnostics/admin-master-only')
  return data
}

export async function listConfigurations(): Promise<AppConfigurationEntry[]> {
  const { data } = await api.get<AppConfigurationEntry[]>('/api/admin/config')
  return data
}

export async function updateConfiguration(key: string, value: string): Promise<AppConfigurationEntry> {
  const { data } = await api.put<AppConfigurationEntry>(`/api/admin/config/${encodeURIComponent(key)}`, { value })
  return data
}

export async function uploadTeamShield(file: File): Promise<string> {
  const fd = new FormData()
  fd.append('file', file)
  const { data } = await api.post<{ teamShieldUrl: string }>('/api/admin/config/team-shield', fd, {
    transformRequest: formDataMultipartTransform,
  })
  return data.teamShieldUrl
}

export async function listAuditLogs(params: { entityType?: string; take?: number }): Promise<AuditLogRow[]> {
  const { data } = await api.get<AuditLogRow[]>('/api/admin/audit-logs', {
    params: {
      entityType: params.entityType || undefined,
      take: params.take ?? 50,
    },
  })
  return data
}

export async function listRolePermissions(): Promise<RolePermissionRow[]> {
  const { data } = await api.get<RolePermissionRow[]>('/api/admin/role-permissions')
  return data
}

export type AdminMembershipListItem = {
  membershipId: string
  userId: string
  userEmail: string
  userName: string
  status: string
  planId: string | null
  startDate: string
  endDate: string | null
  nextDueDate: string | null
}

export type AdminMembershipListPage = {
  totalCount: number
  items: AdminMembershipListItem[]
}

export type AdminMembershipDetail = AdminMembershipListItem

export type MembershipHistoryEvent = {
  id: string
  eventType: string
  fromStatus: string | null
  toStatus: string
  fromPlanId: string | null
  toPlanId: string | null
  reason: string
  actorUserId: string | null
  createdAt: string
}

export async function listAdminMemberships(params: {
  status?: MembershipStatus | ''
  userId?: string
  page?: number
  pageSize?: number
}): Promise<AdminMembershipListPage> {
  const { data } = await api.get<AdminMembershipListPage>('/api/admin/memberships', {
    params: {
      status: params.status || undefined,
      userId: params.userId?.trim() || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminMembership(membershipId: string): Promise<AdminMembershipDetail> {
  const { data } = await api.get<AdminMembershipDetail>(`/api/admin/memberships/${encodeURIComponent(membershipId)}`)
  return data
}

export async function listMembershipHistory(membershipId: string, take = 50): Promise<MembershipHistoryEvent[]> {
  const { data } = await api.get<MembershipHistoryEvent[]>(
    `/api/admin/memberships/${encodeURIComponent(membershipId)}/history`,
    { params: { take } },
  )
  return data
}

export async function updateMembershipStatus(membershipId: string, status: MembershipStatus, reason: string): Promise<void> {
  await api.patch(`/api/admin/memberships/${membershipId}/status`, { status, reason })
}

export async function getAdminDashboard(): Promise<AdminDashboardResult> {
  const { data } = await api.get<AdminDashboardResult>('/api/admin/dashboard')
  return data
}

export async function createStaffInvite(body: {
  email: string
  name: string
  roles: string[]
}): Promise<CreateStaffInviteResult> {
  const { data } = await api.post<CreateStaffInviteResult>('/api/admin/staff/invites', body)
  return data
}

export async function listStaffInvites(): Promise<StaffInviteRow[]> {
  const { data } = await api.get<StaffInviteRow[]>('/api/admin/staff/invites')
  return data
}

export async function listStaffUsers(): Promise<StaffUserRow[]> {
  const { data } = await api.get<StaffUserRow[]>('/api/admin/staff/users')
  return data
}

export async function setStaffUserActive(userId: string, isActive: boolean): Promise<void> {
  await api.patch(`/api/admin/staff/users/${encodeURIComponent(userId)}/active`, { isActive })
}

export async function replaceStaffUserRoles(userId: string, roles: string[]): Promise<void> {
  await api.put(`/api/admin/staff/users/${encodeURIComponent(userId)}/roles`, { roles })
}

export async function replaceRolePermissions(roleName: string, permissionNames: string[]): Promise<void> {
  await api.put('/api/admin/role-permissions', { roleName, permissionNames })
}

export type AuthTokensResponse = {
  accessToken: string
  refreshToken: string
  expiresIn: number
  roles: string[]
}

export async function acceptStaffInvite(body: {
  token: string
  password: string
  name?: string | null
}): Promise<AuthTokensResponse> {
  const { data } = await api.post<AuthTokensResponse>('/api/auth/accept-staff-invite', body)
  return data
}

/** All accounts (torcedores, não associados, staff). Distinct from listStaffUsers (B.1). */
export type AdminUserListItem = {
  id: string
  email: string
  name: string
  isActive: boolean
  createdAt: string
  isStaff: boolean
  membershipStatus: string | null
  document: string | null
}

export type AdminUserListPage = {
  totalCount: number
  items: AdminUserListItem[]
}

export type AdminUserProfile = {
  document: string | null
  birthDate: string | null
  photoUrl: string | null
  address: string | null
  administrativeNote: string | null
}

export type AdminUserMembershipSummary = {
  membershipId: string
  status: string
  planId: string | null
  startDate: string
  endDate: string | null
  nextDueDate: string | null
}

export type AdminUserDetail = {
  id: string
  email: string
  name: string
  phoneNumber: string | null
  isActive: boolean
  createdAt: string
  isStaff: boolean
  roles: string[]
  profile: AdminUserProfile | null
  membership: AdminUserMembershipSummary | null
}

export async function listAdminUsers(params: {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}): Promise<AdminUserListPage> {
  const { data } = await api.get<AdminUserListPage>('/api/admin/users', {
    params: {
      search: params.search || undefined,
      isActive: params.isActive,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminUser(userId: string): Promise<AdminUserDetail> {
  const { data } = await api.get<AdminUserDetail>(`/api/admin/users/${encodeURIComponent(userId)}`)
  return data
}

export async function setUserAccountActive(userId: string, isActive: boolean): Promise<void> {
  await api.patch(`/api/admin/users/${encodeURIComponent(userId)}/active`, { isActive })
}

export async function upsertAdminUserProfile(
  userId: string,
  body: {
    document?: string | null
    birthDate?: string | null
    photoUrl?: string | null
    address?: string | null
    administrativeNote?: string | null
  },
): Promise<void> {
  await api.put(`/api/admin/users/${encodeURIComponent(userId)}/profile`, body)
}

export async function listUserAuditLogsForUser(userId: string, take?: number): Promise<AuditLogRow[]> {
  const { data } = await api.get<AuditLogRow[]>(`/api/admin/users/${encodeURIComponent(userId)}/audit-logs`, {
    params: { take: take ?? 50 },
  })
  return data
}

export type AdminPlanListItem = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
  isActive: boolean
  isPublished: boolean
  publishedAt: string | null
  benefitCount: number
}

export type AdminPlanListPage = {
  totalCount: number
  items: AdminPlanListItem[]
}

export type AdminPlanBenefit = {
  id: string
  sortOrder: number
  title: string
  description: string | null
}

export type AdminPlanDetail = {
  planId: string
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
  isActive: boolean
  isPublished: boolean
  publishedAt: string | null
  summary: string | null
  rulesNotes: string | null
  benefits: AdminPlanBenefit[]
}

export type UpsertPlanBody = {
  name: string
  price: number
  billingCycle: string
  discountPercentage: number
  isActive: boolean
  isPublished: boolean
  summary?: string | null
  rulesNotes?: string | null
  benefits: { sortOrder: number; title: string; description?: string | null }[]
}

export async function listAdminPlans(params: {
  search?: string
  isActive?: boolean
  isPublished?: boolean
  page?: number
  pageSize?: number
}): Promise<AdminPlanListPage> {
  const { data } = await api.get<AdminPlanListPage>('/api/admin/plans', {
    params: {
      search: params.search || undefined,
      isActive: params.isActive,
      isPublished: params.isPublished,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminPlan(planId: string): Promise<AdminPlanDetail> {
  const { data } = await api.get<AdminPlanDetail>(`/api/admin/plans/${encodeURIComponent(planId)}`)
  return data
}

export async function createAdminPlan(body: UpsertPlanBody): Promise<{ planId: string }> {
  const { data } = await api.post<{ planId: string }>('/api/admin/plans', body)
  return data
}

export async function updateAdminPlan(planId: string, body: UpsertPlanBody): Promise<void> {
  await api.put(`/api/admin/plans/${encodeURIComponent(planId)}`, body)
}

export type AdminPaymentListItem = {
  paymentId: string
  userId: string
  userEmail: string
  userName: string
  membershipId: string
  amount: number
  status: string
  dueDate: string
  paidAt: string | null
  paymentMethod: string | null
  externalReference: string | null
}

export type AdminPaymentListPage = {
  totalCount: number
  items: AdminPaymentListItem[]
}

export type AdminPaymentDetail = {
  paymentId: string
  userId: string
  userEmail: string
  userName: string
  membershipId: string
  amount: number
  status: string
  dueDate: string
  paidAt: string | null
  paymentMethod: string | null
  externalReference: string | null
  providerName: string | null
  cancelledAt: string | null
  refundedAt: string | null
  createdAt: string
  updatedAt: string
  lastProviderSyncAt: string | null
  statusReason: string | null
}

export async function listAdminPayments(params: {
  status?: string
  userId?: string
  membershipId?: string
  paymentMethod?: string
  dueFrom?: string
  dueTo?: string
  page?: number
  pageSize?: number
}): Promise<AdminPaymentListPage> {
  const { data } = await api.get<AdminPaymentListPage>('/api/admin/payments', {
    params: {
      status: params.status || undefined,
      userId: params.userId?.trim() || undefined,
      membershipId: params.membershipId?.trim() || undefined,
      paymentMethod: params.paymentMethod || undefined,
      dueFrom: params.dueFrom || undefined,
      dueTo: params.dueTo || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminPayment(paymentId: string): Promise<AdminPaymentDetail> {
  const { data } = await api.get<AdminPaymentDetail>(`/api/admin/payments/${encodeURIComponent(paymentId)}`)
  return data
}

export async function conciliateAdminPayment(paymentId: string, body?: { paidAt?: string | null }): Promise<void> {
  await api.post(`/api/admin/payments/${encodeURIComponent(paymentId)}/conciliate`, body ?? {})
}

export async function cancelAdminPayment(paymentId: string, body?: { reason?: string | null }): Promise<void> {
  await api.post(`/api/admin/payments/${encodeURIComponent(paymentId)}/cancel`, body ?? {})
}

export async function refundAdminPayment(paymentId: string, body?: { reason?: string | null }): Promise<void> {
  await api.post(`/api/admin/payments/${encodeURIComponent(paymentId)}/refund`, body ?? {})
}

export type AdminDigitalCardListItem = {
  digitalCardId: string
  userId: string
  membershipId: string
  version: number
  status: string
  issuedAt: string
  invalidatedAt: string | null
  userEmail: string
  membershipStatus: string
}

export type AdminDigitalCardListPage = {
  totalCount: number
  items: AdminDigitalCardListItem[]
}

export type AdminDigitalCardDetail = {
  digitalCardId: string
  userId: string
  membershipId: string
  version: number
  status: string
  token: string
  issuedAt: string
  invalidatedAt: string | null
  invalidationReason: string | null
  userEmail: string
  userName: string
  membershipStatus: string
  planId: string | null
  planName: string | null
  documentMasked: string | null
  templatePreviewLines: string[]
}

export async function listAdminDigitalCards(params: {
  userId?: string
  membershipId?: string
  status?: string
  page?: number
  pageSize?: number
}): Promise<AdminDigitalCardListPage> {
  const { data } = await api.get<AdminDigitalCardListPage>('/api/admin/digital-cards', {
    params: {
      userId: params.userId?.trim() || undefined,
      membershipId: params.membershipId?.trim() || undefined,
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminDigitalCard(digitalCardId: string): Promise<AdminDigitalCardDetail> {
  const { data } = await api.get<AdminDigitalCardDetail>(`/api/admin/digital-cards/${encodeURIComponent(digitalCardId)}`)
  return data
}

export async function issueAdminDigitalCard(membershipId: string): Promise<void> {
  await api.post('/api/admin/digital-cards/issue', { membershipId })
}

export async function regenerateAdminDigitalCard(digitalCardId: string, body?: { reason?: string | null }): Promise<void> {
  await api.post(`/api/admin/digital-cards/${encodeURIComponent(digitalCardId)}/regenerate`, body ?? {})
}

export async function invalidateAdminDigitalCard(digitalCardId: string, body: { reason: string }): Promise<void> {
  await api.post(`/api/admin/digital-cards/${encodeURIComponent(digitalCardId)}/invalidate`, body)
}

export type AdminGameListItem = {
  gameId: string
  opponent: string
  competition: string
  opponentLogoUrl: string | null
  gameDate: string
  isActive: boolean
  createdAt: string
}

export type AdminGameListPage = {
  totalCount: number
  items: AdminGameListItem[]
}

export type AdminGameDetail = {
  gameId: string
  opponent: string
  competition: string
  opponentLogoUrl: string | null
  gameDate: string
  isActive: boolean
  createdAt: string
}

export type UpsertGameBody = {
  opponent: string
  competition: string
  gameDate: string
  isActive: boolean
  opponentLogoUrl?: string | null
}

export type AdminOpponentLogoItem = {
  id: string
  url: string
  createdAt: string
}

export type AdminOpponentLogoListPage = {
  totalCount: number
  items: AdminOpponentLogoItem[]
}

export async function listAdminOpponentLogos(params?: {
  page?: number
  pageSize?: number
}): Promise<AdminOpponentLogoListPage> {
  const { data } = await api.get<AdminOpponentLogoListPage>('/api/admin/games/opponent-logos', {
    params: {
      page: params?.page ?? 1,
      pageSize: params?.pageSize ?? 50,
    },
  })
  return data
}

export async function uploadAdminOpponentLogo(file: File): Promise<string> {
  const fd = new FormData()
  fd.append('file', file)
  const { data } = await api.post<{ url: string }>('/api/admin/games/opponent-logos', fd, {
    transformRequest: formDataMultipartTransform,
  })
  return data.url
}

export async function listAdminGames(params: {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}): Promise<AdminGameListPage> {
  const { data } = await api.get<AdminGameListPage>('/api/admin/games', {
    params: {
      search: params.search?.trim() || undefined,
      isActive: params.isActive,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminGame(gameId: string): Promise<AdminGameDetail> {
  const { data } = await api.get<AdminGameDetail>(`/api/admin/games/${encodeURIComponent(gameId)}`)
  return data
}

export async function createAdminGame(body: UpsertGameBody): Promise<{ gameId: string }> {
  const { data } = await api.post<{ gameId: string }>('/api/admin/games', body)
  return data
}

export async function updateAdminGame(gameId: string, body: UpsertGameBody): Promise<void> {
  await api.put(`/api/admin/games/${encodeURIComponent(gameId)}`, body)
}

export async function deactivateAdminGame(gameId: string): Promise<void> {
  await api.delete(`/api/admin/games/${encodeURIComponent(gameId)}`)
}

export type AdminTicketListItem = {
  ticketId: string
  userId: string
  userEmail: string
  userName: string | null
  gameId: string
  opponent: string
  competition: string
  gameDate: string
  status: string
  externalTicketId: string | null
  qrCode: string | null
  createdAt: string
  redeemedAt: string | null
  requestStatus: 'Pending' | 'Issued'
  membershipPlanName: string | null
}

export type AdminTicketListPage = {
  totalCount: number
  items: AdminTicketListItem[]
}

export type AdminTicketDetail = {
  ticketId: string
  userId: string
  userEmail: string
  userName: string | null
  gameId: string
  opponent: string
  competition: string
  gameDate: string
  status: string
  externalTicketId: string | null
  qrCode: string | null
  createdAt: string
  updatedAt: string
  redeemedAt: string | null
  requestStatus: 'Pending' | 'Issued'
  membershipPlanName: string | null
}

export async function listAdminTickets(params: {
  userId?: string
  gameId?: string
  status?: string
  requestStatus?: 'Pending' | 'Issued'
  page?: number
  pageSize?: number
}): Promise<AdminTicketListPage> {
  const { data } = await api.get<AdminTicketListPage>('/api/admin/tickets', {
    params: {
      userId: params.userId?.trim() || undefined,
      gameId: params.gameId?.trim() || undefined,
      status: params.status || undefined,
      requestStatus: params.requestStatus || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminTicket(ticketId: string): Promise<AdminTicketDetail> {
  const { data } = await api.get<AdminTicketDetail>(`/api/admin/tickets/${encodeURIComponent(ticketId)}`)
  return data
}

export async function reserveAdminTicket(body: { userId: string; gameId: string }): Promise<{ ticketId: string }> {
  const { data } = await api.post<{ ticketId: string }>('/api/admin/tickets/reserve', body)
  return data
}

export async function purchaseAdminTicket(ticketId: string): Promise<void> {
  await api.post(`/api/admin/tickets/${encodeURIComponent(ticketId)}/purchase`, {})
}

export async function syncAdminTicket(ticketId: string): Promise<void> {
  await api.post(`/api/admin/tickets/${encodeURIComponent(ticketId)}/sync`, {})
}

export async function redeemAdminTicket(ticketId: string): Promise<void> {
  await api.post(`/api/admin/tickets/${encodeURIComponent(ticketId)}/redeem`, {})
}

export async function patchAdminTicketRequestStatus(
  ticketId: string,
  requestStatus: 'Pending' | 'Issued',
): Promise<void> {
  await api.patch(`/api/admin/tickets/${encodeURIComponent(ticketId)}/request-status`, { requestStatus })
}

export type NewsEditorialStatus = 'Draft' | 'Published' | 'Unpublished'

export type AdminNewsListItem = {
  newsId: string
  title: string
  status: NewsEditorialStatus
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  unpublishedAt: string | null
}

export type AdminNewsListPage = {
  totalCount: number
  items: AdminNewsListItem[]
}

export type AdminNewsDetail = {
  newsId: string
  title: string
  summary: string | null
  content: string
  status: NewsEditorialStatus
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  unpublishedAt: string | null
}

export type UpsertNewsBody = {
  title: string
  summary?: string | null
  content: string
}

export async function listAdminNews(params: {
  search?: string
  status?: NewsEditorialStatus | ''
  page?: number
  pageSize?: number
}): Promise<AdminNewsListPage> {
  const { data } = await api.get<AdminNewsListPage>('/api/admin/news', {
    params: {
      search: params.search?.trim() || undefined,
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminNews(newsId: string): Promise<AdminNewsDetail> {
  const { data } = await api.get<AdminNewsDetail>(`/api/admin/news/${encodeURIComponent(newsId)}`)
  return data
}

export async function createAdminNews(body: UpsertNewsBody): Promise<{ newsId: string }> {
  const { data } = await api.post<{ newsId: string }>('/api/admin/news', body)
  return data
}

export async function updateAdminNews(newsId: string, body: UpsertNewsBody): Promise<void> {
  await api.put(`/api/admin/news/${encodeURIComponent(newsId)}`, body)
}

export async function publishAdminNews(newsId: string): Promise<void> {
  await api.post(`/api/admin/news/${encodeURIComponent(newsId)}/publish`, {})
}

export async function unpublishAdminNews(newsId: string): Promise<void> {
  await api.post(`/api/admin/news/${encodeURIComponent(newsId)}/unpublish`, {})
}

export async function createNewsInAppNotifications(
  newsId: string,
  body: { scheduledAt?: string | null; userIds?: string[] | null },
): Promise<void> {
  await api.post(`/api/admin/news/${encodeURIComponent(newsId)}/notifications`, body)
}

/** Loyalty (B.10) */
export type LoyaltyCampaignStatus = 'Draft' | 'Published' | 'Unpublished'
export type LoyaltyPointRuleTrigger = 'PaymentPaid' | 'TicketPurchased' | 'TicketRedeemed'
export type LoyaltyPointSourceType = 'Payment' | 'TicketPurchase' | 'TicketRedeem' | 'Manual'

export type LoyaltyCampaignListItem = {
  campaignId: string
  name: string
  status: LoyaltyCampaignStatus
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  ruleCount: number
}

export type LoyaltyCampaignListPage = {
  totalCount: number
  items: LoyaltyCampaignListItem[]
}

export type LoyaltyPointRule = {
  ruleId: string
  trigger: LoyaltyPointRuleTrigger
  points: number
  sortOrder: number
}

export type LoyaltyCampaignDetail = {
  campaignId: string
  name: string
  description: string | null
  status: LoyaltyCampaignStatus
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  unpublishedAt: string | null
  rules: LoyaltyPointRule[]
}

export type UpsertLoyaltyCampaignBody = {
  name: string
  description?: string | null
  rules: { trigger: LoyaltyPointRuleTrigger; points: number; sortOrder: number }[]
}

export async function listLoyaltyCampaigns(params: {
  status?: LoyaltyCampaignStatus | ''
  page?: number
  pageSize?: number
}): Promise<LoyaltyCampaignListPage> {
  const { data } = await api.get<LoyaltyCampaignListPage>('/api/admin/loyalty/campaigns', {
    params: {
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getLoyaltyCampaign(campaignId: string): Promise<LoyaltyCampaignDetail> {
  const { data } = await api.get<LoyaltyCampaignDetail>(`/api/admin/loyalty/campaigns/${encodeURIComponent(campaignId)}`)
  return data
}

export async function createLoyaltyCampaign(body: UpsertLoyaltyCampaignBody): Promise<{ campaignId: string }> {
  const { data } = await api.post<{ campaignId: string }>('/api/admin/loyalty/campaigns', body)
  return data
}

export async function updateLoyaltyCampaign(campaignId: string, body: UpsertLoyaltyCampaignBody): Promise<void> {
  await api.put(`/api/admin/loyalty/campaigns/${encodeURIComponent(campaignId)}`, body)
}

export async function publishLoyaltyCampaign(campaignId: string): Promise<void> {
  await api.post(`/api/admin/loyalty/campaigns/${encodeURIComponent(campaignId)}/publish`, {})
}

export async function unpublishLoyaltyCampaign(campaignId: string): Promise<void> {
  await api.post(`/api/admin/loyalty/campaigns/${encodeURIComponent(campaignId)}/unpublish`, {})
}

export async function manualLoyaltyAdjust(
  userId: string,
  body: { points: number; reason: string; campaignId?: string | null },
): Promise<void> {
  await api.post(`/api/admin/loyalty/users/${encodeURIComponent(userId)}/manual-adjustments`, body)
}

export type LoyaltyLedgerEntry = {
  entryId: string
  userId: string
  campaignId: string | null
  ruleId: string | null
  points: number
  sourceType: LoyaltyPointSourceType
  sourceKey: string
  reason: string | null
  actorUserId: string | null
  createdAt: string
}

export type LoyaltyLedgerPage = {
  totalCount: number
  items: LoyaltyLedgerEntry[]
}

export async function listLoyaltyUserLedger(
  userId: string,
  params?: { page?: number; pageSize?: number },
): Promise<LoyaltyLedgerPage> {
  const { data } = await api.get<LoyaltyLedgerPage>(`/api/admin/loyalty/users/${encodeURIComponent(userId)}/ledger`, {
    params: { page: params?.page ?? 1, pageSize: params?.pageSize ?? 20 },
  })
  return data
}

export type LoyaltyRankingRow = {
  rank: number
  userId: string
  userEmail: string
  userName: string
  totalPoints: number
}

export type LoyaltyRankingPage = {
  totalCount: number
  items: LoyaltyRankingRow[]
}

export async function getLoyaltyMonthlyRanking(params: {
  year: number
  month: number
  page?: number
  pageSize?: number
}): Promise<LoyaltyRankingPage> {
  const { data } = await api.get<LoyaltyRankingPage>('/api/admin/loyalty/rankings/monthly', {
    params: {
      year: params.year,
      month: params.month,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getLoyaltyAllTimeRanking(params?: { page?: number; pageSize?: number }): Promise<LoyaltyRankingPage> {
  const { data } = await api.get<LoyaltyRankingPage>('/api/admin/loyalty/rankings/all-time', {
    params: { page: params?.page ?? 1, pageSize: params?.pageSize ?? 20 },
  })
  return data
}

/** Benefits (B.10) */
export type BenefitPartnerListItem = {
  partnerId: string
  name: string
  isActive: boolean
  createdAt: string
}

export type BenefitPartnerListPage = {
  totalCount: number
  items: BenefitPartnerListItem[]
}

export type BenefitPartnerDetail = {
  partnerId: string
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type UpsertBenefitPartnerBody = {
  name: string
  description?: string | null
  isActive: boolean
}

export async function listBenefitPartners(params: {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}): Promise<BenefitPartnerListPage> {
  const { data } = await api.get<BenefitPartnerListPage>('/api/admin/benefits/partners', {
    params: {
      search: params.search?.trim() || undefined,
      isActive: params.isActive,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getBenefitPartner(partnerId: string): Promise<BenefitPartnerDetail> {
  const { data } = await api.get<BenefitPartnerDetail>(`/api/admin/benefits/partners/${encodeURIComponent(partnerId)}`)
  return data
}

export async function createBenefitPartner(body: UpsertBenefitPartnerBody): Promise<{ partnerId: string }> {
  const { data } = await api.post<{ partnerId: string }>('/api/admin/benefits/partners', body)
  return data
}

export async function updateBenefitPartner(partnerId: string, body: UpsertBenefitPartnerBody): Promise<void> {
  await api.put(`/api/admin/benefits/partners/${encodeURIComponent(partnerId)}`, body)
}

export type BenefitOfferListItem = {
  offerId: string
  partnerId: string
  partnerName: string
  title: string
  isActive: boolean
  startAt: string
  endAt: string
  createdAt: string
  bannerUrl: string | null
}

export type BenefitOfferListPage = {
  totalCount: number
  items: BenefitOfferListItem[]
}

export type BenefitOfferDetail = {
  offerId: string
  partnerId: string
  title: string
  description: string | null
  isActive: boolean
  startAt: string
  endAt: string
  createdAt: string
  updatedAt: string
  eligiblePlanIds: string[]
  eligibleMembershipStatuses: string[]
  bannerUrl: string | null
}

export type UpsertBenefitOfferBody = {
  partnerId: string
  title: string
  description?: string | null
  isActive: boolean
  startAt: string
  endAt: string
  eligiblePlanIds?: string[] | null
  eligibleMembershipStatuses?: string[] | null
}

export async function listBenefitOffers(params: {
  partnerId?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}): Promise<BenefitOfferListPage> {
  const { data } = await api.get<BenefitOfferListPage>('/api/admin/benefits/offers', {
    params: {
      partnerId: params.partnerId || undefined,
      isActive: params.isActive,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getBenefitOffer(offerId: string): Promise<BenefitOfferDetail> {
  const { data } = await api.get<BenefitOfferDetail>(`/api/admin/benefits/offers/${encodeURIComponent(offerId)}`)
  return data
}

export async function createBenefitOffer(body: UpsertBenefitOfferBody): Promise<{ offerId: string }> {
  const { data } = await api.post<{ offerId: string }>('/api/admin/benefits/offers', body)
  return data
}

export async function updateBenefitOffer(offerId: string, body: UpsertBenefitOfferBody): Promise<void> {
  await api.put(`/api/admin/benefits/offers/${encodeURIComponent(offerId)}`, body)
}

export async function uploadBenefitOfferBanner(offerId: string, file: File): Promise<{ bannerUrl: string }> {
  const form = new FormData()
  form.append('file', file)
  const { data } = await api.post<{ bannerUrl: string }>(
    `/api/admin/benefits/offers/${encodeURIComponent(offerId)}/banner`,
    form,
    { transformRequest: formDataMultipartTransform },
  )
  return data
}

export async function deleteBenefitOfferBanner(offerId: string): Promise<void> {
  await api.delete(`/api/admin/benefits/offers/${encodeURIComponent(offerId)}/banner`)
}

export async function redeemBenefitOffer(offerId: string, body: { userId: string; notes?: string | null }): Promise<{ redemptionId: string }> {
  const { data } = await api.post<{ redemptionId: string }>(
    `/api/admin/benefits/offers/${encodeURIComponent(offerId)}/redeem`,
    body,
  )
  return data
}

export type BenefitRedemptionListItem = {
  redemptionId: string
  offerId: string
  offerTitle: string
  userId: string
  userEmail: string
  actorUserId: string | null
  notes: string | null
  createdAt: string
}

export type BenefitRedemptionListPage = {
  totalCount: number
  items: BenefitRedemptionListItem[]
}

export async function listBenefitRedemptions(params: {
  offerId?: string
  userId?: string
  page?: number
  pageSize?: number
}): Promise<BenefitRedemptionListPage> {
  const { data } = await api.get<BenefitRedemptionListPage>('/api/admin/benefits/redemptions', {
    params: {
      offerId: params.offerId || undefined,
      userId: params.userId || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

/** B.11 Support (chamados) — admin */
export type SupportTicketStatus =
  | 'Open'
  | 'InProgress'
  | 'WaitingUser'
  | 'Resolved'
  | 'Closed'

export type SupportTicketPriority = 'Normal' | 'High' | 'Urgent'

export type AdminSupportTicketListItem = {
  ticketId: string
  requesterUserId: string
  assignedAgentUserId: string | null
  queue: string
  subject: string
  priority: SupportTicketPriority
  status: SupportTicketStatus
  slaDeadlineUtc: string
  isSlaBreached: boolean
  firstResponseAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export type AdminSupportTicketListPage = {
  totalCount: number
  items: AdminSupportTicketListItem[]
}

export type SupportTicketMessageAttachment = {
  attachmentId: string
  fileName: string
  contentType: string
  downloadPath: string
}

export type SupportTicketMessage = {
  messageId: string
  authorUserId: string
  body: string
  isInternal: boolean
  createdAtUtc: string
  attachments: SupportTicketMessageAttachment[]
}

export type SupportTicketHistoryEntry = {
  entryId: string
  eventType: string
  fromValue: string | null
  toValue: string | null
  actorUserId: string
  reason: string | null
  createdAtUtc: string
}

export type AdminSupportTicketDetail = {
  ticketId: string
  requesterUserId: string
  assignedAgentUserId: string | null
  queue: string
  subject: string
  priority: SupportTicketPriority
  status: SupportTicketStatus
  slaDeadlineUtc: string
  isSlaBreached: boolean
  firstResponseAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
  messages: SupportTicketMessage[]
  history: SupportTicketHistoryEntry[]
}

export async function listAdminSupportTickets(params: {
  queue?: string
  status?: SupportTicketStatus
  assignedUserId?: string
  unassignedOnly?: boolean
  slaBreachedOnly?: boolean
  page?: number
  pageSize?: number
}): Promise<AdminSupportTicketListPage> {
  const { data } = await api.get<AdminSupportTicketListPage>('/api/admin/support/tickets', {
    params: {
      queue: params.queue || undefined,
      status: params.status,
      assignedUserId: params.assignedUserId || undefined,
      unassignedOnly: params.unassignedOnly,
      slaBreachedOnly: params.slaBreachedOnly,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAdminSupportTicket(ticketId: string): Promise<AdminSupportTicketDetail> {
  const { data } = await api.get<AdminSupportTicketDetail>(`/api/admin/support/tickets/${encodeURIComponent(ticketId)}`)
  return data
}

export async function createAdminSupportTicket(body: {
  requesterUserId: string
  queue: string
  subject: string
  priority: SupportTicketPriority
  initialMessage?: string | null
}): Promise<{ ticketId: string }> {
  const { data } = await api.post<{ ticketId: string }>('/api/admin/support/tickets', body)
  return data
}

export async function replyAdminSupportTicket(ticketId: string, body: { body: string; isInternal: boolean }): Promise<void> {
  await api.post(`/api/admin/support/tickets/${encodeURIComponent(ticketId)}/reply`, body)
}

export async function assignAdminSupportTicket(ticketId: string, agentUserId: string | null): Promise<void> {
  await api.post(`/api/admin/support/tickets/${encodeURIComponent(ticketId)}/assign`, { agentUserId })
}

export async function changeAdminSupportTicketStatus(
  ticketId: string,
  body: { status: SupportTicketStatus; reason?: string | null },
): Promise<void> {
  await api.post(`/api/admin/support/tickets/${encodeURIComponent(ticketId)}/status`, body)
}

export async function fetchAdminSupportAttachmentBlob(downloadPath: string): Promise<Blob> {
  const res = await api.get<Blob>(downloadPath, { responseType: 'blob' })
  return res.data
}

export async function downloadAdminSupportAttachment(downloadPath: string, fileName: string): Promise<void> {
  const blob = await fetchAdminSupportAttachmentBlob(downloadPath)
  const blobUrl = URL.createObjectURL(blob)
  try {
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = fileName
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
  }
  finally {
    URL.revokeObjectURL(blobUrl)
  }
}
