import { api } from '../../../shared/api/http'

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

export type AdminDashboardResult = {
  activeMembersCount: number
  delinquentMembersCount: number
  openSupportTickets: number | null
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
