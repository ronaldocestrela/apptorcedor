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

export async function updateMembershipStatus(membershipId: string, status: MembershipStatus): Promise<void> {
  await api.patch(`/api/admin/memberships/${membershipId}/status`, { status })
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
