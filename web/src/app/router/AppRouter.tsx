import { Navigate, Route, Routes } from 'react-router-dom'
import { AdminBillingPage } from '../../pages/admin/AdminBillingPage'
import { AdminHomePage } from '../../pages/admin/AdminHomePage'
import { AdminPlansPage } from '../../pages/admin/AdminPlansPage'
import { AdminStripeConnectPage } from '../../pages/admin/AdminStripeConnectPage'
import { LoginPage } from '../../pages/auth/LoginPage'
import { RegisterPage } from '../../pages/auth/RegisterPage'
import { MemberBillingPage } from '../../pages/member/MemberBillingPage'
import { MemberHomePage } from '../../pages/member/MemberHomePage'
import { AppShell } from './AppShell'
import { RequireAuth } from './RequireAuth'

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<Navigate to="/member" replace />} />
          <Route path="admin" element={<AdminHomePage />} />
          <Route path="admin/plans" element={<AdminPlansPage />} />
          <Route path="admin/billing" element={<AdminBillingPage />} />
          <Route path="admin/stripe" element={<AdminStripeConnectPage />} />
          <Route path="member" element={<MemberHomePage />} />
          <Route path="member/billing" element={<MemberBillingPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/member" replace />} />
    </Routes>
  )
}
