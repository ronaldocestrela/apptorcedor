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
