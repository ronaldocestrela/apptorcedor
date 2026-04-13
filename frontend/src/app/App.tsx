import { Route, Routes } from 'react-router-dom'
import { AdminIndexRedirect } from '../features/admin/pages/AdminIndexRedirect'
import { AdminDashboardPage } from '../features/admin/pages/AdminDashboardPage'
import { AuditLogsPage } from '../features/admin/pages/AuditLogsPage'
import { ConfigurationsPage } from '../features/admin/pages/ConfigurationsPage'
import { DiagnosticsPage } from '../features/admin/pages/DiagnosticsPage'
import { MembershipStatusPage } from '../features/admin/pages/MembershipStatusPage'
import { RolePermissionsPage } from '../features/admin/pages/RolePermissionsPage'
import { StaffManagementPage } from '../features/admin/pages/StaffManagementPage'
import { AdminLayout } from '../features/admin/layout/AdminLayout'
import { AuthProvider } from '../features/auth/AuthContext'
import { PermissionRoute } from '../features/auth/PermissionRoute'
import { ProtectedRoute } from '../features/auth/ProtectedRoute'
import { AcceptStaffInvitePage } from '../pages/AcceptStaffInvitePage'
import { DashboardPage } from '../pages/DashboardPage'
import { LoginPage } from '../pages/LoginPage'

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/accept-staff-invite" element={<AcceptStaffInvitePage />} />
        <Route element={<ProtectedRoute />}>
          <Route index element={<DashboardPage />} />
          <Route element={<PermissionRoute />}>
            <Route path="admin" element={<AdminLayout />}>
              <Route index element={<AdminIndexRedirect />} />
              <Route path="dashboard" element={<AdminDashboardPage />} />
              <Route path="staff" element={<StaffManagementPage />} />
              <Route path="diagnostics" element={<DiagnosticsPage />} />
              <Route path="configurations" element={<ConfigurationsPage />} />
              <Route path="audit-logs" element={<AuditLogsPage />} />
              <Route path="role-permissions" element={<RolePermissionsPage />} />
              <Route path="membership" element={<MembershipStatusPage />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  )
}
