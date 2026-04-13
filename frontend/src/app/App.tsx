import { Route, Routes } from 'react-router-dom'
import { AdminIndexRedirect } from '../features/admin/pages/AdminIndexRedirect'
import { AdminDashboardPage } from '../features/admin/pages/AdminDashboardPage'
import { AuditLogsPage } from '../features/admin/pages/AuditLogsPage'
import { ConfigurationsPage } from '../features/admin/pages/ConfigurationsPage'
import { DiagnosticsPage } from '../features/admin/pages/DiagnosticsPage'
import { MembershipAdminPage } from '../features/admin/pages/MembershipAdminPage'
import { PlansAdminPage } from '../features/admin/pages/PlansAdminPage'
import { PaymentsAdminPage } from '../features/admin/pages/PaymentsAdminPage'
import { DigitalCardsAdminPage } from '../features/admin/pages/DigitalCardsAdminPage'
import { GamesAdminPage } from '../features/admin/pages/GamesAdminPage'
import { TicketsAdminPage } from '../features/admin/pages/TicketsAdminPage'
import { NewsAdminPage } from '../features/admin/pages/NewsAdminPage'
import { LegalDocumentsPage } from '../features/admin/pages/LegalDocumentsPage'
import { UserConsentsPage } from '../features/admin/pages/UserConsentsPage'
import { PrivacyOpsPage } from '../features/admin/pages/PrivacyOpsPage'
import { RolePermissionsPage } from '../features/admin/pages/RolePermissionsPage'
import { StaffManagementPage } from '../features/admin/pages/StaffManagementPage'
import { UserDetailPage } from '../features/admin/pages/UserDetailPage'
import { UsersListPage } from '../features/admin/pages/UsersListPage'
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
              <Route path="users" element={<UsersListPage />} />
              <Route path="users/:userId" element={<UserDetailPage />} />
              <Route path="diagnostics" element={<DiagnosticsPage />} />
              <Route path="configurations" element={<ConfigurationsPage />} />
              <Route path="audit-logs" element={<AuditLogsPage />} />
              <Route path="role-permissions" element={<RolePermissionsPage />} />
              <Route path="membership" element={<MembershipAdminPage />} />
              <Route path="plans" element={<PlansAdminPage />} />
              <Route path="payments" element={<PaymentsAdminPage />} />
              <Route path="digital-cards" element={<DigitalCardsAdminPage />} />
              <Route path="games" element={<GamesAdminPage />} />
              <Route path="tickets" element={<TicketsAdminPage />} />
              <Route path="news" element={<NewsAdminPage />} />
              <Route path="lgpd/documents" element={<LegalDocumentsPage />} />
              <Route path="lgpd/consents" element={<UserConsentsPage />} />
              <Route path="lgpd/privacy" element={<PrivacyOpsPage />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  )
}
