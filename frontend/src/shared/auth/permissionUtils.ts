import type { ApplicationPermission } from './applicationPermissions'

export type UserWithPermissions = {
  permissions: string[]
}

export function hasPermission(user: UserWithPermissions | null, permission: string): boolean {
  return user?.permissions.includes(permission) ?? false
}

export function hasAnyPermission(user: UserWithPermissions | null, permissions: readonly string[]): boolean {
  if (!user?.permissions.length)
    return false
  return permissions.some((p) => user.permissions.includes(p))
}

export function canAccessAdminArea(user: UserWithPermissions | null, areaPermissions: readonly ApplicationPermission[]): boolean {
  return hasAnyPermission(user, areaPermissions)
}
