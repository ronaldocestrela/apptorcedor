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
import { LoyaltyAdminPage } from '../features/admin/pages/LoyaltyAdminPage'
import { BenefitsAdminPage } from '../features/admin/pages/BenefitsAdminPage'
import { SupportTicketsAdminPage } from '../features/admin/pages/SupportTicketsAdminPage'
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
import { AccountPage } from '../pages/AccountPage'
import { BenefitOfferDetailPage } from '../pages/BenefitOfferDetailPage'
import { BenefitsEligiblePage } from '../pages/BenefitsEligiblePage'
import { DigitalCardPage } from '../pages/DigitalCardPage'
import { GamesPage } from '../pages/GamesPage'
import { DashboardPage } from '../pages/DashboardPage'
import { LoyaltyPage } from '../pages/LoyaltyPage'
import { MyTicketsPage } from '../pages/MyTicketsPage'
import { NewsDetailPage } from '../pages/NewsDetailPage'
import { NewsFeedPage } from '../pages/NewsFeedPage'
import { PlanDetailsPage } from '../pages/PlanDetailsPage'
import { SubscriptionCheckoutPage } from '../pages/SubscriptionCheckoutPage'
import { SubscriptionConfirmationPage } from '../pages/SubscriptionConfirmationPage'
import { PlansPage } from '../pages/PlansPage'
import { ForgotPasswordPage } from '../pages/ForgotPasswordPage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { ResetPasswordPage } from '../pages/ResetPasswordPage'
import { SupportTicketDetailPage } from '../pages/SupportTicketDetailPage'
import { SupportTicketsPage } from '../pages/SupportTicketsPage'

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/accept-staff-invite" element={<AcceptStaffInvitePage />} />
        <Route path="/news" element={<NewsFeedPage />} />
        <Route path="/news/:newsId" element={<NewsDetailPage />} />
        <Route path="/plans" element={<PlansPage />} />
        <Route path="/plans/:planId" element={<PlanDetailsPage />} />
        <Route element={<ProtectedRoute />}>
          <Route index element={<DashboardPage />} />
          <Route path="account" element={<AccountPage />} />
          <Route path="benefits/:offerId" element={<BenefitOfferDetailPage />} />
          <Route path="benefits" element={<BenefitsEligiblePage />} />
          <Route path="plans/:planId/checkout" element={<SubscriptionCheckoutPage />} />
          <Route path="subscription/confirmation" element={<SubscriptionConfirmationPage />} />
          <Route path="digital-card" element={<DigitalCardPage />} />
          <Route path="games" element={<GamesPage />} />
          <Route path="tickets" element={<MyTicketsPage />} />
          <Route path="loyalty" element={<LoyaltyPage />} />
          <Route path="support" element={<SupportTicketsPage />} />
          <Route path="support/:ticketId" element={<SupportTicketDetailPage />} />
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
              <Route path="loyalty" element={<LoyaltyAdminPage />} />
              <Route path="benefits" element={<BenefitsAdminPage />} />
              <Route path="support" element={<SupportTicketsAdminPage />} />
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
